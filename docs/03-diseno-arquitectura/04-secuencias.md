# Diagramas de Secuencia — Flujos Críticos

## 1. Login con emisión de JWT + Refresh Token (HU-002)

```mermaid
sequenceDiagram
    actor U as Usuario
    participant SPA as Web App (Vue)
    participant API as API (Auth Endpoint)
    participant DB as SQL Server

    U->>SPA: Ingresa email + password
    SPA->>API: POST /api/auth/login
    API->>DB: Buscar usuario por email + tenant activo
    DB-->>API: Usuario + hash de password + roles
    API->>API: Verificar password (hash)
    API->>DB: Resolver permisos efectivos (roles -> permisos)
    DB-->>API: Lista de permisos
    API->>API: Emitir Access Token (claims: sub, tenant_id, permissions[], exp corto)
    API->>DB: Persistir Refresh Token (hash, expiración, no usado)
    API-->>SPA: 200 { accessToken } + Set-Cookie: refreshToken (HttpOnly, Secure, SameSite=Strict)
    SPA->>SPA: Guardar accessToken (localStorage) — el refresh token nunca toca JS
```

Nota: los permisos se "aplanan" en el Access Token en el momento del login para
evitar una consulta a base de datos en cada request subsecuente. Trade-off
documentado en ADR-0004: si los permisos de un usuario cambian mientras su
token sigue vigente, el cambio no se refleja hasta el siguiente refresh —
mitigado con una vida corta del Access Token (minutos, no horas).

Actualizado 2026-07-07: el diagrama original (Sprint 2) ya mostraba el
refresh token viajando aparte de forma segura; la implementación de Sprint 7a
lo devolvía en el mismo body JSON que el access token, y el frontend lo
guardaba en `localStorage`. La revisión de seguridad ad-hoc de esa fecha lo
corrigió a cookie `HttpOnly` — ver [ADR-0007](../adr/ADR-0007-refresh-token-en-cookie-httponly.md).

## 2. Renovación de sesión (Refresh Token con rotación)

```mermaid
sequenceDiagram
    actor SPA as Web App
    participant API as API (Auth Endpoint)
    participant DB as SQL Server

    SPA->>API: POST /api/auth/refresh (cookie refreshToken, sin body)
    API->>DB: Buscar Refresh Token por hash
    alt token no existe, expiró o ya fue usado
        API->>DB: Revocar todo el linaje del token reusado (ver nota)
        API-->>SPA: 401 Unauthorized (forzar re-login)
    else token válido y no usado
        API->>DB: Marcar token actual como usado
        API->>DB: Emitir y persistir nuevo Refresh Token (rotación)
        API->>API: Emitir nuevo Access Token
        API-->>SPA: 200 { accessToken } + Set-Cookie: refreshToken (rotado)
    end
```

La rotación con detección de reuso (si un Refresh Token ya marcado "usado" se
presenta de nuevo, se revocan todos los tokens de la familia) es la mitigación
estándar contra robo de Refresh Token — se implementa así en vez de un
Refresh Token estático de larga duración.

Nota (2026-07-07): este diagrama ya documentaba desde Sprint 2 que el reuso
debía revocar "todos los tokens de la familia" — la implementación de
Sprint 7a revocaba solo el token reusado, no su cadena de descendientes,
divergiendo silenciosamente del diseño. Corregido en la revisión de seguridad
ad-hoc; ver [docs/08a-seguridad.md](../08a-seguridad.md), hallazgo #5.

## 3. Request autenticada: resolución de tenant + autorización por permiso

```mermaid
sequenceDiagram
    actor SPA as Web App
    participant MW as Middleware (JWT + Tenant Resolution)
    participant PIPE as MediatR Pipeline
    participant AUTHZ as AuthorizationBehavior
    participant H as Command/Query Handler
    participant DB as SQL Server (con Query Filter TenantId)

    SPA->>MW: GET /api/projects/{id}  (Authorization: Bearer <token>)
    MW->>MW: Validar firma/expiración del JWT
    MW->>MW: Extraer tenant_id del claim -> ICurrentTenantService
    MW->>MW: Extraer permissions[] del claim -> ICurrentUserService
    MW->>PIPE: Despachar GetProjectByIdQuery
    PIPE->>AUTHZ: Verificar permiso requerido ("projects.read")
    alt permiso ausente
        AUTHZ-->>SPA: 403 Forbidden
    else permiso presente
        AUTHZ->>H: Continuar
        H->>DB: Query (EF Core aplica WHERE TenantId = @current automáticamente)
        DB-->>H: Proyecto (solo si pertenece al tenant actual)
        H-->>SPA: 200 { proyecto }
    end
```

Punto de diseño central: **el filtrado por tenant no depende de que cada
handler recuerde añadir `WHERE TenantId = ...`** — se aplica como Global Query
Filter en `AppDbContext` (ver ADR-0003), de modo que olvidarlo en un handler
nuevo no puede filtrar datos entre tenants. Es un control a nivel de
infraestructura, no una convención de código que dependa de disciplina humana.

## 4. Subida de Documento + transición de Workflow (HU-050, HU-081) — Release 2

```mermaid
sequenceDiagram
    actor U as Usuario
    participant SPA as Web App
    participant API as API
    participant STORAGE as IDocumentStorageProvider (proveedor activo)
    participant DB as SQL Server

    U->>SPA: Selecciona archivo y lo sube a un Proyecto
    SPA->>API: POST /api/documents (multipart, ownerType=Project, ownerId)
    API->>API: Validar tipo/tamaño por firma binaria real (HU-051)
    alt archivo inválido
        API-->>SPA: 400 Bad Request (tipo/tamaño no permitido)
    else archivo válido
        API->>DB: Verificar que el propietario existe en el tenant actual
        API->>STORAGE: UploadAsync(stream, key)
        STORAGE-->>API: clave de storage (opaca, ADR-0009)
        API->>DB: Crear Document (OwnerType, OwnerId, storageKey)
        API->>DB: Crear WorkflowInstance en estado inicial "Borrador" (ADR-0010)
        API-->>SPA: 201 Created
    end

    U->>SPA: Envía el Documento a revisión
    SPA->>API: POST /api/documents/{id}/transition (targetState="En Revisión")
    API->>DB: ¿Existe WorkflowTransition Borrador→En Revisión?
    alt transición no definida
        API-->>SPA: 400 Bad Request
    else transición válida
        API->>DB: Actualizar WorkflowInstance.CurrentStateId
        API-->>SPA: 204 No Content
    end
```

Mismo patrón de "hecho inyectado" que el diagrama 3 usa para autorización:
`Document.TransitionTo(...)` nunca decide *si puede* transicionar consultando
otro agregado directamente — recibe el resultado de esa consulta ya resuelto
por `Application` (ADR-0010).

## 5. Notificación in-app + correo tras aprobar un Documento (HU-060, HU-061) — Release 2

```mermaid
sequenceDiagram
    actor R as Revisor
    participant API as API
    participant DB as SQL Server
    participant PUB as IPublisher (Domain Events)
    participant HUB as SignalR Hub
    participant JOBS as Hangfire
    actor U as Usuario (dueño del Documento)
    participant SMTP as Proveedor de Correo

    R->>API: POST /api/documents/{id}/transition (targetState="Aprobado")
    API->>DB: Validar transición + actualizar WorkflowInstance
    API->>DB: SaveChangesAsync
    DB-->>API: OK
    API->>PUB: Publicar DocumentApprovedDomainEvent (post-SaveChanges)
    PUB->>HUB: NotificationHandler: Clients.User(ownerId).SendAsync(...)
    HUB-->>U: Notificación en tiempo real (si está conectado)
    PUB->>DB: NotificationHandler: persistir Notification (F6.3, historial)
    PUB->>JOBS: NotificationHandler: BackgroundJob.Enqueue(IEmailSender.SendAsync)
    API-->>R: 204 No Content
    Note over JOBS,SMTP: Fuera del ciclo de la request original (ADR-0011)
    JOBS->>SMTP: Enviar correo (async, con reintentos de Hangfire)
```

Los tres efectos (in-app, historial, correo) son handlers independientes del
mismo evento — ninguno depende de que los otros dos tengan éxito (ADR-0011).
La respuesta al Revisor (`204 No Content`) no espera al envío de correo.

## 6. Pregunta al asistente de IA con tool-use (HU-092) — Release 3

```mermaid
sequenceDiagram
    actor U as Usuario
    participant SPA as Web App
    participant API as API
    participant AI as IAiChatClient (proveedor activo)
    participant Q as Query existente (p. ej. GetTasksQuery)
    participant DB as SQL Server (con Query Filter TenantId)

    U->>SPA: "¿Cuántas tareas tengo atrasadas?"
    SPA->>API: POST /api/assistant/messages
    API->>AI: Enviar mensaje + catálogo de herramientas disponibles
    AI-->>API: Solicita invocar herramienta "get_my_overdue_tasks"
    Note over API,Q: La "herramienta" es una Query de Application ya existente —<br/>nunca SQL directo generado por el modelo
    API->>Q: Send(GetTasksQuery filtrada por vencidas + usuario actual)
    Q->>DB: EF Core aplica WHERE TenantId = @current automáticamente (ADR-0003)
    DB-->>Q: Tareas vencidas del usuario, solo de su tenant
    Q-->>API: Resultado tipado (mismo DTO que ya usa el Dashboard)
    API->>AI: Enviar resultado de la herramienta
    AI-->>API: Respuesta final en lenguaje natural, anclada en ese resultado
    API-->>SPA: 200 { respuesta }
    SPA-->>U: Muestra la respuesta en el chat
```

Punto de diseño central (mismo principio que el diagrama 3 aplica a
autorización, y el 4 a Workflow): el modelo de IA **nunca decide qué datos
ver por sí mismo** — solo puede invocar herramientas que son, una a una,
Queries de Application que ya pasan por `AuthorizationBehavior` y el filtro
de tenant. No existe una ruta donde el modelo reciba un `IAppDbContext` o
genere SQL — la superficie de "lo que el asistente puede consultar" es
exactamente la superficie de Queries que Application ya expone a cualquier
otro caller, ni un bit más.

## 7. Indexación de un Documento para RAG (HU-100) — Release 3

```mermaid
sequenceDiagram
    actor U as Usuario
    participant API as API
    participant STORAGE as IDocumentStorageProvider
    participant EXTRACT as Extractor de texto
    participant AI as IAiChatClient (embeddings)
    participant DB as SQL Server / almacén de vectores (TBD, Sprint 3)

    U->>API: POST /api/documents (sube un PDF/Word/texto a un Proyecto)
    Note over API: Flujo de subida sin cambios (F5, Release 2) —<br/>la indexación se dispara después, no lo reemplaza
    API->>STORAGE: UploadAsync(stream, key)
    STORAGE-->>API: clave de storage
    API->>DB: Crear Document (F5)
    API->>EXTRACT: Extraer texto del contenido subido
    alt el archivo no tiene texto extraíble (p. ej. PDF escaneado sin capa de texto)
        EXTRACT-->>API: Sin contenido indexable
        Note over API: El Documento queda guardado igual (F5 no cambia) —<br/>simplemente no participa en RAG (r3-01-vision-y-alcance.md, sección 4)
    else texto extraído
        EXTRACT-->>API: Texto plano
        API->>AI: Generar embeddings del texto (chunked)
        AI-->>API: Vectores
        API->>DB: Persistir vectores asociados al Document.Id y su TenantId
    end
```

El `TenantId` viaja con cada vector indexado por la misma razón que
cualquier otra fila del sistema (ADR-0003) — cuando el asistente responda
preguntas ancladas en Documentos (HU-101), la búsqueda de similitud debe
filtrar por tenant *antes* de devolver resultados al modelo, nunca después
de generar la respuesta.

## 8. Consultar el historial de cambios de un Proyecto (HU-102) — Release 4

```mermaid
sequenceDiagram
    actor U as Administrador de Tenant
    participant API as API
    participant DB as SQL Server (Temporal Table)

    U->>API: GET /api/projects/{id}/history?asOf=2026-07-07T00:00:00Z
    Note over API: Application no reconstruye el historial a mano —<br/>SQL Server ya lo retiene (System-Versioned Temporal Tables)
    API->>DB: SELECT ... FROM Projects FOR SYSTEM_TIME AS OF @asOf WHERE Id = @id
    Note over DB: El filtro global de tenant (ADR-0003) sigue aplicando<br/>igual que en cualquier otra consulta — la tabla de historial<br/>no es una excepción al aislamiento multi-tenant
    DB-->>API: La fila tal como estaba en ese momento
    API-->>U: Estado histórico del Proyecto
```

La retención de versiones es responsabilidad de SQL Server, no de código
de aplicación — `AppDbContext` sigue leyendo/escribiendo la tabla
`Projects` igual que siempre; `FOR SYSTEM_TIME AS OF` es una cláusula SQL
adicional que EF Core traduce vía `.TemporalAsOf(date)`, no un mecanismo
paralelo que Application tenga que mantener sincronizado a mano.
