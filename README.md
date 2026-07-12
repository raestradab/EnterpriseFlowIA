# EnterpriseFlow AI

[![CI](https://github.com/raestradab/EnterpriseFlowIA/actions/workflows/ci.yml/badge.svg)](https://github.com/raestradab/EnterpriseFlowIA/actions/workflows/ci.yml)
[![CodeQL](https://github.com/raestradab/EnterpriseFlowIA/actions/workflows/codeql.yml/badge.svg)](https://github.com/raestradab/EnterpriseFlowIA/actions/workflows/codeql.yml)

Plataforma SaaS multi-tenant de gestión de proyectos, clientes y equipos, con
un asistente de IA embebido (RAG sobre los Documentos de cada tenant — un
servidor MCP propio quedó diferido, ver [`docs/backlog/epics.md`](./docs/backlog/epics.md),
E11). Construida como ejercicio de portafolio técnico siguiendo prácticas de
Clean Architecture, DDD táctico, CQRS y Vertical Slice Architecture sobre
.NET 8 y Vue 3.

> El proyecto se construye por Releases incrementales, no de una vez. El
> estado actual, el alcance del MVP y el roadmap completo están documentados
> en [`docs/`](./docs/01-vision-y-alcance.md) — léelo antes de asumir que
> falta algo: probablemente ya está en el backlog con su Release asignado.

## Estado actual

**Release 1 (MVP) completo** (Sprints 1-11). **Sprint 8 (Frontend) verificado
en navegador real** de punta a punta contra la Api real (registro → login →
CRUD de Empresas/Clientes/Proyectos/Tareas → cierre de proyecto con
validación de dominio → dark mode → responsive → i18n → logout) — ver
[`docs/08-frontend.md`](./docs/08-frontend.md). Se encontraron y corrigieron
tres bugs reales durante Sprint 8 (uno de autenticación vía smoke test con
`curl`, dos de UI vía verificación visual en navegador).

Adicionalmente, se completó una **revisión de seguridad ad-hoc** (2026-07-07,
pedida explícitamente por el usuario, fuera de la secuencia de Sprints) contra
la sección SEGURIDAD de la especificación — 10 hallazgos corregidos (secretos,
rate limiting, enumeración de usuarios, canal de tiempo en login, revocación
en cadena de refresh tokens, headers de seguridad, refresh token movido a
cookie HttpOnly, CORS explícito, HSTS). Ver
[`docs/08a-seguridad.md`](./docs/08a-seguridad.md).

**Sprint 9 (Pruebas) completo**: cobertura medida por primera vez (antes
implícita) y empujada de 44.4% a **95.8%** de líneas — 118 tests totales (70
existentes + 48 nuevos), cerrando gaps reales en endpoints de lectura
(Get-by-id/listados de los 4 módulos de negocio + calendario HU-024) y en
guard clauses de entidades de dominio que ningún test alcanzaba directamente.
Ver [`docs/09-pruebas.md`](./docs/09-pruebas.md).

**Sprint 10 (Documentación) completo**: auditoría de `docs/` contra el estado
real del código — el diagrama ER tenía solo 6 de las 12 tablas actuales
(faltaba todo el esquema de Identidad de Sprint 7a), dos documentos tenían
secciones "qué falta" de sprints ya cerrados hace tiempo, y no existía un
índice consolidado de ADRs. Todo corregido; detalle en
[`docs/10-documentacion.md`](./docs/10-documentacion.md).

**Release 1 (MVP) completo** — Sprint 11 (DevOps) cierra la secuencia:
Dockerfiles multi-stage para Api y Frontend, `docker-compose.yml` (SQL Server
+ Api + Frontend, migraciones automáticas al arrancar), GitHub Actions
(lint + build + test para backend y frontend), y `dotnet format` alineado
con el `.editorconfig` existente (dos reglas de naming/documentación que
nunca se habían configurado realmente, encontradas al conectar `dotnet
format` a CI por primera vez). Detalle en
[`docs/11-devops.md`](./docs/11-devops.md).

**Release 2 (Colaboración y Operación) — completa (Sprints 1-11).** Sprint 1
(Análisis): alcance detallado (Documentos con 4 proveedores de storage
intercambiables, Notificaciones in-app/correo, Workflow de aprobación,
Catálogos, Reportes exportables) y activación justificada de Redis/Hangfire/
Response Compression atada a casos de uso reales de este Release, no a la
lista de la especificación original. Ver
[`docs/r2-01-vision-y-alcance.md`](./docs/r2-01-vision-y-alcance.md),
[`docs/backlog/historias-usuario-release2.md`](./docs/backlog/historias-usuario-release2.md)
y [ADR-0008](./docs/adr/ADR-0008-activacion-redis-hangfire-response-compression.md).

Sprint 2 (Diseño): vistas C4 de Contexto/Contenedores actualizadas in-place
al estado de Release 2, 2 diagramas de secuencia nuevos (subida+aprobación de
Documento, notificación in-app+correo) y 4 ADRs nuevos (0009-0012) — storage
de Documentos, motor de Workflow, entrega de notificaciones reusando el
pipeline de Domain Events existente, y cache-aside de Catálogos como Pipeline
Behavior de MediatR. Ver [`docs/03-diseno-arquitectura/00-resumen.md`](./docs/03-diseno-arquitectura/00-resumen.md).

Sprint 3 (Arquitectura): esqueleto de infraestructura activado y **verificado
en ejecución real** (no solo revisión de código) — Hangfire creó su esquema
contra una base LocalDB real y arrancó su servidor de jobs; Redis usa
`AddDistributedMemoryCache()` como *fallback* mientras no hay una instancia
real alcanzable en este entorno (dicho explícitamente, no asumido probado).
SignalR wireado con autenticación JWT vía query string (patrón estándar,
los navegadores no pueden fijar headers en WebSocket nativo) y un
`IUserIdProvider` propio — sin él, las notificaciones nunca habrían
encontrado ninguna conexión, por la misma razón de claims que ya causó un
bug real en Sprint 7a. Ver [`docs/r2-03-arquitectura.md`](./docs/r2-03-arquitectura.md).

Sprint 4 (Validación): primer slice vertical real de Release 2 — **Catálogos
(F8.2)**, elegido por ser el único consumidor real posible de
`CachingBehavior`/`CacheInvalidationBehavior` sin depender de Documentos ni
Workflow (que todavía no existen). Encontró y corrigió un gap real: la
prefijación por tenant de las claves de caché vivía como convención en cada
Query en vez de como garantía estructural — movida a los propios Behaviors
(ADR-0012). Prueba de integración deliberadamente *black-box*: puebla el
caché, muta la fila directamente en la base de datos evitando la Api,
confirma que la siguiente lectura sigue sirviendo el valor cacheado (no una
lectura fresca), y confirma que una escritura real invalida y refresca de
inmediato — 142/142 tests. Ver [`docs/r2-04-validacion.md`](./docs/r2-04-validacion.md).

Sprint 5 (Modelo de Dominio): las tres entidades restantes de Release 2 —
`Document` (F5, referencia polimórfica a su propietario sin FK física,
ADR-0009), `WorkflowDefinition`/`WorkflowState`/`WorkflowTransition` (F8.1,
máquina de estados orientada a datos, ADR-0010) y `Notification` (F6.3).
`Document.TransitionTo` levanta su evento de dominio en toda transición
exitosa, no solo en las "significativas" — no puede saber cuáles lo son,
los nombres de estado son datos configurables por tenant, no algo que
Domain reconozca; esa decisión queda para el handler de notificaciones que
se construya más adelante. 38 pruebas nuevas, 180/180 en total. Ver
[`docs/r2-05-modelo-dominio.md`](./docs/r2-05-modelo-dominio.md).

Sprint 6 (Base de datos): configuración EF Core + migración
(`AddDocumentsWorkflowsNotifications`, 5 tablas) para las entidades de
Sprint 5, **aplicada y verificada contra LocalDB real** en secuencia junto a
las migraciones previas. El diagrama ER consolidado
([`docs/06-base-de-datos.md`](./docs/06-base-de-datos.md)) se actualizó con
las 7 tablas de Release 2 hasta ahora (Catálogos incluidos, que databan de
Sprint 4 pero no estaban documentadas ahí todavía). Ver
[`docs/r2-06-base-de-datos.md`](./docs/r2-06-base-de-datos.md).

Sprint 7a (Backend — Workflow, F8.1): `CreateWorkflow`/`AddWorkflowState`/
`AddWorkflowTransition`/`GetWorkflowById`/`GetWorkflows` — primera de tres
sub-partes de Sprint 7 (mismo criterio que el split 7a/7b de Release 1:
Workflow antes que Documentos, porque un Documento necesita un Workflow real
para crearse). La prueba central construye un Workflow completo únicamente
por HTTP (crear → 3 estados → 2 transiciones) y confirma que una transición
entre estados de Workflows distintos se rechaza — HU-080 ("transiciones son
datos, no código") verificado de punta a punta, no solo a nivel de entidad.
186/186 tests. Ver [`docs/r2-07a-backend-workflow.md`](./docs/r2-07a-backend-workflow.md).

Sprint 7b (Backend — Documentos, F5): los 4 proveedores de storage
comprometidos en el análisis (`LocalStorageProvider`, `AzureBlobStorageProvider`,
`AmazonS3StorageProvider`, `GoogleCloudStorageProvider` — SDKs reales, uno
activo por instancia vía `Documents:Provider`) + `UploadDocument`/`GetDocumentById`/
`GetDocuments`/`DownloadDocument`/`TransitionDocument`/`DeleteDocument`.
HU-051 (validar tipo real de archivo) se prueba subiendo el header real de
un ejecutable de Windows renombrado `invoice.pdf` y confirmando el rechazo —
la extensión sola no lo habría detectado. Subida→descarga se verificó con
E/S de disco real contra `LocalStorageProvider` (bytes idénticos de vuelta,
no solo la fila creada); los 3 proveedores cloud no se pudieron verificar en
ejecución en este entorno (sin Docker/cuentas reales), mismo tipo de
limitación ya declarado para Redis en Sprint 3. 203/203 tests. Ver
[`docs/r2-07b-backend-documentos.md`](./docs/r2-07b-backend-documentos.md).

Sprint 7c (Backend — Notificaciones, F6, cierra Sprint 7): un
`INotificationHandler` reacciona a `DocumentWorkflowTransitionedDomainEvent`
(evento levantado desde Sprint 5) — mismo pipeline de eventos de dominio de
Release 1 (HU-012), sin bus de mensajería nuevo (ADR-0011). Destinatarios:
miembros del Proyecto dueño del Documento (o del Proyecto de su Tarea
dueña); un Documento de un Cliente no genera destinatarios porque `Client`
no tiene concepto de "miembros" en este dominio. `IEmailQueue`/`IEmailSender`
nuevas (mismo patrón que `IRealtimeNotifier`), con degradación elegante a
un no-op cuando Hangfire no está configurado. La propia suite de pruebas
encontró un bug real: `GetMyNotificationsQueryHandler` ordenaba por
`DateTimeOffset` dentro de la consulta — válido en SQL Server, pero SQLite
(el motor de las pruebas de integración) no sabe traducir ese `ORDER BY` y
lanzaba un 500; corregido ordenando en memoria tras materializar. 207/207
tests. Ver [`docs/r2-07c-backend-notificaciones.md`](./docs/r2-07c-backend-notificaciones.md).

Sprint 8a (Frontend — Catálogos y Workflow): primera de tres sub-partes de
Sprint 8, mismo criterio que el split 7a/7b/7c — cubre solo lo que ya tiene
backend (Reportes/Configuración/Mapa de `r2-01-vision-y-alcance.md` no lo
tienen todavía, quedan pendientes). Pantallas Vuetify siguiendo el patrón
exacto de `ProjectsListView`/`ProjectDetailView` de Release 1. **Verificado
en un navegador real contra la Api real**, no solo `vue-tsc` limpio: crear
catálogo → agregar/editar/eliminar un elemento; crear Workflow → agregar 2
estados → agregar una transición → confirmar que persiste tras recargar;
cambio de idioma ES↔EN confirmado en ambas pantallas nuevas; sin errores de
consola. Ver [`docs/r2-08a-frontend-catalogos-workflow.md`](./docs/r2-08a-frontend-catalogos-workflow.md).

Sprint 8b (Frontend — Documentos): tarjeta "Documentos" en
`ProjectDetailView` — subir, descargar, transicionar de estado y eliminar.
Encontró y corrigió un gap real de Sprint 7b: `DocumentDto`/`DocumentListItemDto`
exponían `CurrentWorkflowStateId` como GUID crudo, sin nombre ni forma de
saber a qué Workflow pertenece — imposible de mostrar en una UI legible o
de resolver transiciones válidas. Extendidos con `CurrentWorkflowStateName`
y `WorkflowDefinitionId`, resueltos con el mismo join que
`TransitionDocumentCommandHandler` ya usaba. **Verificado con un archivo
real subido, descargado (bytes idénticos), transicionado y eliminado**
contra la Api real. 207/207 tests de backend siguen pasando (cambio
aditivo). Ver [`docs/r2-08b-frontend-documentos.md`](./docs/r2-08b-frontend-documentos.md).

Sprint 8c (Frontend — Notificaciones, cierra Sprint 8): cliente de SignalR
(`@microsoft/signalr`, paquete nuevo) conectado a `/hubs/notifications`
(Sprint 7c), campana con contador de no leídas en la app-bar, store Pinia
`notificationCenter` — nombrado deliberadamente distinto del store
`notifications` ya existente (que es la cola de toasts de Release 1, no el
centro de notificaciones de F6; colisión ya anotada en 8a). **Verificado
con el push en vivo observado sin recargar la página**: se agregó al propio
admin como miembro de un Proyecto, se transicionó un Documento por Api
real, y el badge de la campana pasó de 0 a 1 sin ninguna recarga — prueba
directa de extremo a extremo del pipeline evento de dominio → SignalR →
UI reactiva. Marcar como leída confirmado persistido en la base de datos
real. Ver [`docs/r2-08c-frontend-notificaciones.md`](./docs/r2-08c-frontend-notificaciones.md).

Sprint 9 (Pruebas): medición real de cobertura acumulada de Release 2
(coverlet + reportgenerator, misma herramienta que Release 1) — 91.4%
inicial, ya por encima del 90% exigido, pero escondía gaps reales: las
ramas de mitigación de timing attack en Login/Refresh (Identidad, Release 1,
0% desde siempre), los guard clauses de Workflow/Catálogos, y el hallazgo
más importante — **`NotifyOnDocumentWorkflowTransitionedHandler` nunca
había verificado sus propios efectos reales** (`IRealtimeNotifier`/
`IEmailQueue` en 0%). Al cerrar ese gap se encontró un segundo problema en
la propia infraestructura de pruebas: los fixtures agregaban miembros de
Proyecto con un `userId` inventado, sin un `User` real detrás — invisible
para la persistencia de notificaciones, pero silenciosamente sin
destinatarios para el paso que busca el email. Corregido con un fixture que
siembra un `User` real y con fakes inyectables (`FakeRealtimeNotifier`/
`FakeEmailQueue`) para verificar las llamadas de verdad. 218/218 tests,
cobertura final 95.5% líneas / 86.2% branches, los 4 ensamblados por encima
del 90%. Ver [`docs/r2-09-pruebas.md`](./docs/r2-09-pruebas.md).

Sprint 10 (Documentación): auditoría de la documentación acumulada de los 9
sprints anteriores contra el estado real del código — no escritura desde
cero. Encontró y corrigió: un índice de `Companies` que faltaba en la tabla
de índices de `docs/06-base-de-datos.md` (existía en código desde Sprint 4
de *Release 1*, nunca se había documentado); `docs/02-roadmap.md` con la
sección de Release 2 congelada en el Sprint 1 pese a que los 9 sprints ya
estaban completos; y el endpoint de subida de Documentos
(`POST /api/documents`, `multipart/form-data` parseado a mano, sin DTO
tipado) sin resumen en Swagger — el único endpoint nuevo de Release 2 cuyo
contrato no es evidente desde la ruta, mismo criterio que Release 1 aplicó
a `AuthEndpoints.cs`. El índice de ADRs y los enlaces de este mismo README
ya estaban al día — verificado, no asumido. Ver
[`docs/r2-10-documentacion.md`](./docs/r2-10-documentacion.md).

Sprint 11 (DevOps, cierra Release 2): `docker-compose.yml` extendido con lo
que Release 2 activó — servicio `redis` nuevo (aditivo, degrada a caché en
memoria si no está disponible, igual que ya hacía sin Docker), volumen
`documents-data` para que los archivos subidos (F5) sobrevivan a un
`docker-compose up` reconstruido, y `ConnectionStrings__Hangfire` apuntando
a la *misma* base que la app en vez de una separada — decisión que salió de
**verificar Hangfire en ejecución real** contra LocalDB con esa
configuración exacta (log confirmado: `"Hangfire SQL objects installed"`),
no del plan original, evitando el mismo problema de "base inexistente" ya
encontrado en Sprint 3. `nginx.conf` ganó un bloque para el upgrade a
WebSocket que SignalR necesita (`/hubs/`), mismo problema que
`vite.config.ts` ya resolvió en Sprint 8c para desarrollo. CI no necesitó
cambios. Ver [`docs/r2-11-devops.md`](./docs/r2-11-devops.md).

**Release 3 (Inteligencia Artificial) — completo (Sprints 1-11).**
Redefinido respecto al plan original antes de escribir código: en vez de
tooling de desarrollo (generar historias/tests/SQL/DTOs, servidor MCP
propio sobre este repo), un **asistente de IA de cara al usuario final**
— decisión explícita del usuario, coherente con que el resto del
portafolio son features de producto real, no herramientas internas del
equipo. Alcance: integración intercambiable con OpenAI/Anthropic
(`IAiChatClient`, mismo patrón que `IDocumentStorageProvider`/ADR-0009),
chat con respuestas ancladas en los datos reales del tenant vía las
Queries de Application ya existentes — nunca acceso directo a SQL — y RAG
sobre los Documentos que cada tenant sube (F5, Release 2) en vez de sobre
la documentación del propio proyecto. Sin claves de API reales de OpenAI/
Anthropic disponibles en este entorno — mismo tipo de límite ya declarado
para Redis y los SDKs cloud de Documentos en Release 2, dicho por
adelantado en vez de descubierto al final. Ver
[`docs/r3-01-vision-y-alcance.md`](./docs/r3-01-vision-y-alcance.md).

Sprint 2 (Diseño): las vistas C4 (`03-diseno-arquitectura/`) se
actualizaron in-place — esta vez, a diferencia de sprints anteriores, hubo
que **remover** elementos, no solo agregar: los contenedores `mcp` (Servidor
MCP) y `rag` (Motor RAG como proceso propio), dibujados desde el Sprint 2 de
Release 1 anticipando el plan original, se quitaron por completo — el
servidor MCP queda diferido sin Release asignado, y RAG se pliega dentro
del contenedor `api` existente como una Vertical Slice más, mismo criterio
que ya aplicaba a Documentos/Workflow/Notificaciones. Nuevo:
[ADR-0013](./docs/adr/ADR-0013-abstraccion-ia-y-limite-tool-use.md) —
`IAiChatClient` (mismo patrón que ADR-0009) y el límite de seguridad
central del asistente: cada "herramienta" que el modelo puede invocar es
una Query de Application ya existente, con su propio
`AuthorizationBehavior` y filtro de tenant — nunca una ruta directa a
`IAppDbContext` ni SQL generado por el modelo. Dos diagramas de secuencia
nuevos (pregunta al asistente con tool-use, indexación de un Documento
para RAG) en
[`docs/03-diseno-arquitectura/04-secuencias.md`](./docs/03-diseno-arquitectura/04-secuencias.md).

Sprint 3 (Arquitectura, esqueleto): cerró las dos decisiones que Sprint 1
dejó pendientes — [ADR-0014](./docs/adr/ADR-0014-almacen-de-vectores-para-rag.md)
(vectores de RAG en una tabla más de SQL Server, similitud coseno en
código de aplicación filtrada por tenant, sin servicio de vectores
dedicado) y una corrección a ADR-0013: `IAiChatClient` se dividió en
`IAiChatClient`/`IEmbeddingClient` — Anthropic no ofrece embeddings, así
que una sola interfaz para ambas capacidades habría dejado sin RAG a quien
eligiera Claude para el chat. Interfaces nuevas en `Application.Abstractions`,
con implementaciones `Null*` de respaldo (mismo patrón que `NullEmailQueue`)
cuando no hay proveedor configurado — verificado arrancando la Api real sin
ninguna clave de API, `/health` respondiendo `Healthy`. Las implementaciones
reales de OpenAI/Anthropic quedan para el Sprint de Backend, mismo split
que Documentos ya usó en Release 2. 218/218 tests (sin cambios — los tests
de integración ya construyen el contenedor de DI completo, verificando
implícitamente que el wiring no rompe nada). Ver
[`docs/r3-03-arquitectura.md`](./docs/r3-03-arquitectura.md).

Sprint 4 (Validación): primer slice vertical real — chat con tool-use
(HU-091/HU-092), no RAG, porque F10 depende del mecanismo de tool-use que
este Sprint valida. `AssistantMessage` (Domain, sin agregado `Conversation`
separado — ninguna HU pide administrar hilos con nombre), el loop completo
de `SendAssistantMessageCommandHandler` (arma historial + catálogo de
herramientas, llama a `IAiChatClient`, resuelve cada `AiToolCallRequest`
invocando la Query real vía `ISender` — mismo `AuthorizationBehavior` que
cualquier otro caller). Probado con `FakeAiChatClient`, un doble que corre
un **loop real de dos idas y vueltas** (pide la herramienta, después
responde con su resultado real) — no una respuesta enlatada. Verificado
con datos reales: la respuesta contiene el nombre real de un Proyecto
sembrado en la base; un usuario sin `projects.read` recibe una denegación
legible (`200`, no `500`) sin fuga de datos; un Proyecto de un tenant
nunca aparece en la respuesta a otro tenant. Encontró el mismo bug de
`ORDER BY DateTimeOffset` de Sprint 9 — esta vez en el propio Command
handler, no solo en la Query de historial (ya corregida ahí, se me pasó en
el otro lugar). 228/228 tests. Ver
[`docs/r3-04-validacion.md`](./docs/r3-04-validacion.md).

Sprint 5 (Modelo de dominio): `DocumentChunk` (F10, RAG) — un aggregate
root propio, no hijo de `Document` (indexar ocurre después de subir, en
una transacción separada, mismo criterio que ya separó `ProjectTask` de
`Project` en Release 1). Guarda el chunk de texto y su embedding
serializado como `byte[]` (ADR-0014); el cálculo de similitud en sí queda
para el Sprint de Backend — esta entidad fija la forma de almacenamiento,
no el algoritmo de recuperación. 228/228 tests. Ver
[`docs/r3-05-modelo-dominio.md`](./docs/r3-05-modelo-dominio.md).

Sprint 6 (Base de datos): configuración EF Core + migración
(`AddDocumentChunks`) para `DocumentChunk` — `Embedding` mapeado a
`varbinary(max)` (ADR-0014). Aprovechó para incorporar también
`AssistantMessages` (Sprint 4) al diagrama ER consolidado
([`docs/06-base-de-datos.md`](./docs/06-base-de-datos.md)), que no la
tenía documentada todavía. **Verificado contra LocalDB real** en dos
pasos: incremental sobre la base de desarrollo existente, y la cadena
completa de 5 migraciones sobre una base nueva. 236/236 tests. Ver
[`docs/r3-06-base-de-datos.md`](./docs/r3-06-base-de-datos.md).

Sprint 7a (Backend — proveedores de IA): `OpenAiChatClient`/
`OpenAiEmbeddingClient` (SDK oficial `OpenAI`) y `AnthropicChatClient`
(sin SDK oficial de .NET — implementado contra la REST API `Messages` con
`HttpClient` directamente). Primera de tres sub-partes de Sprint 7 (mismo
criterio 7a/7b/7c de Release 2) — la capa fundacional que 7b (RAG) y 7c
(más herramientas del asistente) van a usar. Construir ambos clientes
expuso una diferencia real de protocolo: Anthropic exige que todos los
resultados de herramientas de un mismo turno vayan en un único mensaje
`user`, no varios separados — y reveló un bug real en el propio diseño de
Sprint 4 (`SendAssistantMessageCommandHandler` nunca reproducía el turno
del asistente que pidió la herramienta, algo que OpenAI también rechaza).
Corregido extendiendo `AiChatMessage`. Proyecto nuevo
`EnterpriseFlow.Infrastructure.UnitTests` (13 tests) para la lógica de
traducción de protocolo, pura y sin red — no tiene sentido pagar el costo
de un `WebApplicationFactory` completo para probarla. Verificado en real:
arranque de la Api con cada proveedor configurado (clave *dummy*) resuelve
sin error vía DI; falla rápido con `OptionsValidationException` si falta
la clave. Sin llamadas reales a las APIs — no hay claves disponibles en
este entorno. 249/249 tests. Ver
[`docs/r3-07a-backend-ia-providers.md`](./docs/r3-07a-backend-ia-providers.md).

Sprint 7b (Backend — RAG): pipeline completo de F10 — `DocumentTextExtractor`
real (`.txt`/`.pdf` vía `PdfPig`/`.docx` vía `DocumentFormat.OpenXml`, sin
*fallback* Null porque extraer texto no necesita clave de API),
`TextChunker`/`EmbeddingSerializer`/`CosineSimilarity` (utilidades puras),
`DocumentUploadedDomainEvent` nuevo + `IndexDocumentOnUploadHandler`
(indexación síncrona dentro del mismo request de subida, un fallo nunca
tumba la subida) y `SearchDocumentChunksQuery` + la herramienta
`search_my_documents` del asistente (HU-101), gateada por `documents.read`.
Probado de punta a punta con `FakeEmbeddingClient` (vocabulario fijo, no un
modelo semántico real, pero con señal real de similitud): subir un `.txt`
indexa un chunk real de forma síncrona (verificado leyendo la base
directo); preguntarle al asistente por el contenido de un Documento
responde con texto que viene genuinamente de ese chunk; el contenido de un
tenant nunca aparece en la respuesta a otro tenant. 273/273 tests. Ver
[`docs/r3-07b-backend-rag.md`](./docs/r3-07b-backend-rag.md).

Sprint 7c (Backend — más herramientas, cierra Sprint 7): `GetMyOverdueTasksQuery`
nueva (no reutiliza `GetMyCalendarQuery` — esa no filtra por `Status`, y
HU-092 exige que "atrasada" sea un hecho resuelto por una Query real, no
algo que el modelo derive de una lista cruda) + herramienta
`get_my_overdue_tasks`. HU-093 (resúmenes ejecutivos) no necesitó código
nuevo — es el mismo mecanismo de tool-use sintetizando en lenguaje natural
lo que las herramientas ya existentes devuelven; este Sprint solo lo
verificó con una prueba real. Encontró un error real de escritura del
propio test (no del producto): las pruebas nuevas devolvían cero tareas
atrasadas hasta notar que faltaba agregar al usuario como miembro del
Proyecto antes de asignarle la tarea, un paso que sí estaba en el test que
sirvió de referencia. Con esto, las 12 Historias de Usuario de Release 3
(HU-090 a HU-101) tienen backend real y probado — catálogo del asistente:
`get_my_projects`, `search_my_documents`, `get_my_overdue_tasks`.
276/276 tests. Ver
[`docs/r3-07c-backend-mas-herramientas.md`](./docs/r3-07c-backend-mas-herramientas.md).

Sprint 8 (Frontend): un solo Sprint, sin sub-partes — F9 (asistente) es
la única feature con UI nueva; F10 (RAG) participa de forma invisible a
través del chat, sin interfaz propia. `AssistantView.vue` es la primera
vista sin precedente directo en el proyecto (ningún chat existía antes) —
compone primitivas de Vuetify (burbujas en `v-card`, envío optimista sin
streaming) sobre el mismo esqueleto ya establecido (`script setup`,
`useI18n`, manejo de errores). Probado en Chrome real, no solo con
`npm run build`: mensaje enviado, respuesta del `NullAiChatClient` real
renderizada ("El asistente de IA no está configurado en este entorno." —
sin claves de API en este entorno, dicho desde Sprint 1), historial
persistido correctamente tras recargar. En el camino, una pestaña con
varias interacciones previas quedó con capturas de pantalla que agotaban
el tiempo de espera — no se asumió que era un bug del componente: se
verificó el round-trip real vía `fetch` directo, y el flujo completo
funcionó al primer intento en una pestaña nueva, confirmando que era una
limitación de la herramienta de automatización (mismo tipo de problema ya
documentado con `v-select` en Release 2), no del código. Ver
[`docs/r3-08-frontend.md`](./docs/r3-08-frontend.md).

Sprint 9 (Pruebas): cobertura real medida (94.7% líneas antes de este
Sprint, ya arriba de la meta del 90%) usada para encontrar huecos
concretos, no perseguir el número. El más importante: **ningún test
existente ejercitaba nunca `NullAiChatClient`/`NullEmbeddingClient`
reales** — todos los tests de asistente/RAG los reemplazan por *fakes*
para tener una señal con la que trabajar, dejando el camino de
degradación elegante (el que realmente corre en este entorno, sin claves
de API) sin ninguna prueba automatizada. Se agregó `NullAiWebApplicationFactory`
(subclase de `CustomWebApplicationFactory`, que perdió su `sealed` para
permitirlo) probando por HTTP real que preguntarle al asistente sin
proveedor configurado responde con el mensaje real (no un 500), y que
subir un Documento sin proveedor de embeddings indexa cero chunks sin
que la subida falle. También se cerró el hueco de subir un archivo con
extensión no soportada (ninguna prueba de Sprint 7b lo cubría). Se
revisó el resto del código por el mismo bug de `ORDER BY DateTimeOffset`
ya encontrado dos veces antes — sin ningún caso nuevo. 281/281 tests,
cobertura subió a 94.9%. Ver
[`docs/r3-09-pruebas.md`](./docs/r3-09-pruebas.md).

Sprint 10 (Documentación): auditoría, no documentación nueva desde cero
— mismo criterio que Release 2. Único hueco real encontrado: la sección
de Release 3 en `docs/02-roadmap.md` solo describía la redefinición de
Sprint 1, sin índice de los 9 sprints — exactamente el mismo tipo de gap
que la auditoría de Release 2 ya había encontrado en su propia sección.
Completado con los enlaces a los 9 sprints. El resto (tabla de índices de
`06-base-de-datos.md`, `docs/adr/README.md`, enlaces de este mismo
README, resúmenes de Swagger) se verificó y ya estaba al día. Ver
[`docs/r3-10-documentacion.md`](./docs/r3-10-documentacion.md).

Sprint 11 (DevOps, cierra Release 3): a diferencia de Release 2, ningún
servicio ni volumen nuevo en `docker-compose.yml` — `AssistantMessages`/
`DocumentChunks` son tablas más en el mismo SQL Server (ADR-0014, sin
almacén de vectores dedicado), y no hay contenedor local que sustituya a
OpenAI/Anthropic. Un comentario nuevo documenta por qué
`Ai:ChatProvider`/`Ai:EmbeddingProvider` se dejan sin configurar (mismo
criterio que Documentos/SMTP en Release 2), más un bloque comentado en
`.env.example` para quien quiera activar un proveedor real. `nginx.conf` y
el `Dockerfile` de la Api no necesitaron cambios (el chat es HTTP normal,
sin WebSocket; los paquetes nuevos se resuelven con el `dotnet restore`
que ya existía). **Verificado en real**: se corrió la Api contra una base
LocalDB completamente nueva y se confirmó que las 5 migraciones de los
tres Releases se aplican automáticamente en orden al arrancar — el mismo
mecanismo que `docker compose up` ejercitaría contra un SQL Server recién
creado. `docker-compose.yml` en sí no se pudo levantar de punta a punta en
este entorno (sin daemon de Docker aquí, mismo límite ya declarado en
Release 1/2) — validado como YAML sintácticamente correcto. 281/281
tests. Ver [`docs/r3-11-devops.md`](./docs/r3-11-devops.md).

**Con esto, Release 3 (Inteligencia Artificial) queda cerrada** — 11
Sprints, 12 Historias de Usuario (HU-090 a HU-101), asistente conversacional
con 3 herramientas reales (`get_my_projects`, `search_my_documents`,
`get_my_overdue_tasks`) y RAG completo sobre Documentos del tenant, sin
ninguna clave de API real disponible en ningún momento de la construcción
— cada límite de verificación declarado explícitamente en el momento en
que se encontró.

**Release 4 (Hardening Empresarial) — completa (Sprints 1-11).**
Redirección de alcance decidida por el usuario al iniciar el Sprint (ver
[`docs/r4-01-vision-y-alcance.md`](./docs/r4-01-vision-y-alcance.md),
sección 0): tres piezas del plan original (RabbitMQ/MassTransit, Elastic/
Application Insights, SignalR a escala) necesitaban infraestructura real
que este entorno no tiene y ningún caso de uso probado en el producto
todavía — diferidas sin Release asignado, mismo criterio que ya difirió
el servidor MCP propio en Release 3. Foco del Release: Temporal Tables
(`Project`/`ProjectTask`, historial completo de cambios vía HU-102),
OpenTelemetry con exportador local, BenchmarkDotNet, CodeQL (SAST nativo
de GitHub — SonarCloud/SonarQube también diferido por necesitar cuenta
externa), Dependabot, y Conventional Commits + Semantic Versioning +
Release Notes automáticas. La cobertura de pruebas ≥90% ya está alcanzada
(94.9% desde Release 3) — este Release audita cómo mantenerla, no la
persigue desde cero. Auditoría del backlog al iniciar el Sprint encontró
que "SonarLint" (`epics.md`, F13.3) nunca se había configurado pese a
estar etiquetado como cubierto desde Release 1-2 — corregido.

Sprint 2 (Diseño): confirma, no introduce — ningún contenedor nuevo hace
falta en `03-diseno-arquitectura/c4-02-contenedores.md` (Temporal Tables
vive dentro del mismo SQL Server ya modelado; OpenTelemetry corre
in-process con exportador local; BenchmarkDotNet/CodeQL/Dependabot corren
on-demand o en CI, fuera del sistema en ejecución que un diagrama C4
describe). Un diagrama de secuencia nuevo (consultar historial de un
Proyecto vía `FOR SYSTEM_TIME AS OF`, HU-102) en `04-secuencias.md`. Ver
[`docs/r4-01-vision-y-alcance.md`](./docs/r4-01-vision-y-alcance.md).

Sprint 3 (Arquitectura): [ADR-0015](./docs/adr/ADR-0015-temporal-tables-historial-de-cambios.md)
(Temporal Tables, comparado contra tabla de auditoría manual y Event
Sourcing) y [ADR-0016](./docs/adr/ADR-0016-opentelemetry-exportador-local.md)
(OpenTelemetry con exportador `Console` por defecto, `Otlp` configurable
— en vez de acoplarse directo a Application Insights/Elastic). *Wiring*
real de OpenTelemetry sobre ASP.NET Core/HttpClient/EF Core.
**Verificado en ejecución real**: se corrió la Api y se confirmaron spans
reales en consola — `GET /health` con `TraceId`/`http.route` completos, y
`POST /api/auth/login` con un span hijo de EF Core mostrando el SQL real
ejecutado y correlacionado con el span HTTP padre (trazabilidad
distribuida genuina). Tráfico orgánico del propio frontend generó spans
también, sin intervención especial. 281/281 tests. Ver
[`docs/r4-03-arquitectura.md`](./docs/r4-03-arquitectura.md).

Sprint 4 (Validación): `GET /api/projects/{id}/history?asOf=...` (HU-102)
— primer consumidor real de Temporal Tables. Encontró un hallazgo real de
arquitectura: `TemporalAsOf` es una extensión de
`Microsoft.EntityFrameworkCore.SqlServer`, que Application no puede
referenciar (ADR-0002) — corregido agregando
`IAppDbContext.GetProjectsAsOf(DateTimeOffset)` como el seam correcto,
mismo patrón que ya usan `IDocumentStorageProvider`/`IAiChatClient`.
**Verificado contra LocalDB real con datos reales**: se creó un Proyecto,
se cerró (cambiando su estado), y `history?asOf=<antes del cierre>`
devolvió el estado viejo mientras `asOf=<ahora>` devolvió el nuevo —
además confirmado que el aislamiento de tenant también protege las
consultas temporales (un tenant distinto pidiendo el mismo Id recibe
404). SQLite no tiene Temporal Tables, así que esta feature queda sin
cobertura en la suite automatizada — decisión explícita, no un
descuido, documentada junto con las alternativas consideradas (SQL
Server como servicio de CI, evaluado para Sprint 11). 281/281 tests. Ver
[`docs/r4-04-validacion.md`](./docs/r4-04-validacion.md).

Sprint 5 (Modelo de dominio): confirmación, no introducción — Release 4
no agrega ninguna entidad de Domain. Temporal Tables es una capacidad de
persistencia pura (`Project`/`ProjectTask` no cambiaron ni una línea);
OpenTelemetry/BenchmarkDotNet/CodeQL/Dependabot/SemVer no modelan ningún
concepto de negocio. 141/141 tests de Domain — el mismo número exacto
que al cierre de Release 3, confirmando que no hubo ningún cambio que
verificar. Ver [`docs/r4-05-modelo-dominio.md`](./docs/r4-05-modelo-dominio.md).

Sprint 6 (Base de datos): también confirmación — la migración de
Temporal Tables ya se había generado y verificado contra LocalDB real en
Sprint 4. El hueco real que sí encontró: `docs/06-base-de-datos.md` (la
referencia canónica del esquema) nunca se había actualizado con Temporal
Tables — quedó documentado en la bitácora del Sprint 4, no en la
referencia consolidada. Corregido con una sección nueva explicando qué
tablas son temporales y por qué solo esas dos. Re-confirmado contra
LocalDB real: `Projects`/`ProjectTasks` siguen como
`SYSTEM_VERSIONED_TEMPORAL_TABLE`. 281/281 tests. Ver
[`docs/r4-06-base-de-datos.md`](./docs/r4-06-base-de-datos.md).

Sprint 7 (Backend): completa HU-102 con `GET /api/tasks/{id}/history`
(la HU nombra "un Proyecto o una Tarea", Sprint 4 solo había construido
la primera) — mismo patrón, mismo *seam* (`IAppDbContext.GetProjectTasksAsOf`).
Proyecto nuevo `benchmarks/EnterpriseFlow.Benchmarks/` (BenchmarkDotNet,
F12.4) con dos benchmarks reales sobre caminos calientes genuinos:
`CosineSimilarity.Compute` (RAG, vector de 1536 dimensiones — la
dimensión real de `text-embedding-3-small`) y `TextChunker.Split`
(indexación de Documentos, que corre dentro del request de subida).
**Corridos de verdad, no solo compilados**: 1.570 μs sin asignaciones
para la similitud de coseno, 7.280 μs / ~55 KB para el chunking —
números reales de esta máquina. La verificación del historial de Tareas
encontró dos errores reales de metodología en el propio script de
prueba (timestamp con precisión insuficiente, una variable de shell no
persistida) — investigados a fondo con SQL directo contra LocalDB antes
de concluir que no eran bugs de producción, documentados igual por
transparencia. 281/281 tests. Ver
[`docs/r4-07-backend.md`](./docs/r4-07-backend.md).

Sprint 8 (Frontend): sin UI nueva — `r4-01-vision-y-alcance.md` (sección
3) ya había decidido en Sprint 1 que el historial de Temporal Tables no
la pide ninguna Historia de Usuario. Auditando el frontend con el mismo
criterio de *hardening* del Release sí encontró un gap real:
`SecurityHeadersMiddleware` (Api) cubre la API JSON por diseño, pero
nunca corría para la SPA real servida por `nginx.conf` en producción —
la superficie de ataque real del navegador quedaba sin
`X-Frame-Options`/CSP/etc. Corregido con cabeceras adaptadas a la SPA
(no copiadas de la API): `style-src 'self' 'unsafe-inline'` en vez de
estricto, porque Vuetify inyecta un `<style>` de tema en runtime sin
wiring de nonce en este stack; el resto de orígenes en `'self'` — un
`grep` de literales `http(s)://` en todo `src/` confirmó cero
dependencias de terceros. **Verificado sirviendo el build de producción
real** (`npm run build`) con esas cabeceras exactas y abriéndolo en un
navegador real: login renderiza con el tema de Vuetify aplicado, cero
errores de CSP en consola, cabeceras confirmadas con `curl`. `npm audit`
(frontend) también auditado: 0 vulnerabilidades. Ver
[`docs/r4-08-frontend.md`](./docs/r4-08-frontend.md).

Sprint 9 (Pruebas): auditoría de cobertura — 94.1% (281 tests), baja
0.8pt desde el 94.9% de Release 3 por las 6 clases de HU-102 en 0%
(SQLite no soporta Temporal Tables). En vez de diferir ese gap una
tercera vez, se cerró: proyecto nuevo
`tests/EnterpriseFlow.Infrastructure.SqlServerTests/` corriendo contra
LocalDB real (base propia, migrada de verdad), 4 pruebas replicando
los tres escenarios que Sprints 4/7 ya habían verificado a mano
(estado antes/después de un cambio, aislamiento de tenant bajo
`TemporalAsOf`, punto anterior a la creación). **4/4 reales, además
confirmadas con SQL directo** (`SYSTEM_VERSIONED_TEMPORAL_TABLE`, filas
de historial generadas). Cobertura local con la suite completa
(285/285): **94.6%**. Como LocalDB es específico de Windows y `ci.yml`
corre en `ubuntu-latest`, el proyecto nuevo queda excluido del barrido
por defecto vía `[Trait("Category", "RequiresSqlServer")]` +
`--filter` — preserva la propiedad de que `dotnet test
EnterpriseFlow.slnx` no necesita ninguna dependencia externa; Sprint 11
(DevOps) es donde corresponde agregar un servicio SQL Server a CI y
quitar el filtro. También se auditó `ci.yml` y se encontró que nunca
exigía un mínimo de cobertura — agregado un gate real (recolecta
cobertura, genera el resumen, falla el job si baja de 90%),
**verificado simulando localmente tanto el caso que pasa (94.1%) como
uno fabricado que falla (85%)**. 281/281 en el barrido por defecto,
285/285 local completo, formato limpio. Ver
[`docs/r4-09-pruebas.md`](./docs/r4-09-pruebas.md).

Sprint 10 (Documentación): con `especificcion.md` (el spec maestro) de
nuevo a la vista, la auditoría se extendió más atrás que solo los docs de
este Release — encontró cuatro gaps reales cruzando el spec completo contra
el código, uno de ellos en la primera frase del propio README (describía el
servidor MCP como construido; corregido para reflejar que sigue diferido,
E11). **Response Compression**: documentado como activo desde
Release 2 (ADR-0008) pero nunca registrado en `Program.cs` — corregido, y
al hacerlo se encontró un segundo bug (registrado después de `UseSwagger()`,
middleware terminal, lo dejaba sin efecto ahí); verificado con una petición
real: `/swagger/v1/swagger.json` de 35 KB a 6 KB con Brotli. **ADR-0001,
punto 6** (Mapster vs. AutoMapper): comprometía un benchmark que nunca se
hizo — resuelto documentando por qué nunca hizo falta (proyección directa
`IQueryable.Select` en cada Query Handler, sin paso intermedio de mapeo).
**Virtual Scrolling**: pedido por el spec, nunca construido ni disclosed —
resuelto como no-necesario (todos los listados usan `VDataTable` paginado
de Vuetify, no *feeds* sin paginar), documentado en `epics.md`. También se
creó `CHANGELOG.md` (F14.4, pedido desde Release 1, nunca había existido),
reconstruido desde el historial real ya documentado. ADR index y diagramas
de arquitectura auditados: sin gaps. 281/281 tests, formato limpio. Ver
[`docs/r4-10-documentacion.md`](./docs/r4-10-documentacion.md).

Sprint 11 (DevOps, cierra Release 4): las tres piezas de madurez de DevOps
del Release — `.github/workflows/codeql.yml` (SAST nativo, matriz
`csharp`/`javascript-typescript`), `.github/dependabot.yml` (nuget/npm/
github-actions) y Conventional Commits + SemVer + Release Notes
automáticas (F13.5: `CONTRIBUTING.md` + `release-please`, elegido sobre
`semantic-release` por no necesitar una cadena de plugins Node) — más
cerrar el diferido explícito de Sprint 9: `ci.yml` ahora corre un servicio
real `mssql/server` (Linux) y `EnterpriseFlow.Infrastructure.SqlServerTests`
perdió su filtro `RequiresSqlServer`. El mecanismo de *override* de
cadena de conexión (variable de entorno con *fallback* a LocalDB) se
verificó con dos casos reales: apuntado a la misma LocalDB (4/4 pasan) y
apuntado a un servidor con credenciales incorrectas (4/4 fallan con un
error de conexión real) — confirma que la variable se lee de verdad, no
un *fallback* silencioso. **Límite de verificación declarado
explícitamente**: este directorio no es un repositorio Git real (sin
remoto, sin Docker daemon en este entorno), así que ningún workflow nuevo
corrió de punta a punta contra GitHub Actions — todos quedan validados
como YAML/JSON sintácticamente correctos, mismo límite que
`docs/11-devops.md` ya declaró desde Release 1 para `ci.yml`. Al cerrar
este Sprint se encontró además que `docs/02-roadmap.md` seguía marcando a
Release 4 como "Sprint 1 completo" (y a Release 3 como "Sprints 1-9
completos") pese a que ambos ya habían cerrado — corregido, junto con una
afirmación falsa en el propio `CHANGELOG.md` que decía haberlo arreglado
en Sprint 10 sin haberlo hecho. 285/285 tests localmente (LocalDB),
formato limpio. Ver [`docs/r4-11-devops.md`](./docs/r4-11-devops.md).

**Con esto, Release 4 (Hardening Empresarial) queda cerrada** — 11
Sprints, historial de cambios vía Temporal Tables (HU-102), tracing
distribuido con OpenTelemetry, BenchmarkDotNet sobre caminos calientes
reales, gate de cobertura real en CI, dos gaps reales corregidos al
auditar contra `especificcion.md` (Response Compression nunca activado
pese al ADR que lo daba por hecho; ADR-0001 punto 6 nunca resuelto),
`CHANGELOG.md` (pedido desde Release 1, nunca existió hasta ahora), y
CodeQL/Dependabot/Conventional Commits+SemVer como madurez final de
DevOps — con RabbitMQ/MassTransit, Elastic/Application Insights y SignalR
a escala redirigidos con la misma trazabilidad que ya recibió el servidor
MCP propio en Release 3, nunca descartados en silencio.

Ver [`docs/02-roadmap.md`](./docs/02-roadmap.md) para el detalle de fases y
[`docs/backlog/epics.md`](./docs/backlog/epics.md) para el alcance completo
por Epic/Release.

## Estructura de la solución

```
EnterpriseFlow.slnx
src/
  EnterpriseFlow.Domain          # Entidades, invariantes, eventos de dominio. Sin dependencias.
  EnterpriseFlow.Application     # Casos de uso (Vertical Slices) vía MediatR/CQRS + FluentValidation.
  EnterpriseFlow.Infrastructure  # EF Core, persistencia, servicios externos.
  EnterpriseFlow.Api             # ASP.NET Core Minimal APIs, composición/DI, Swagger.
tests/
  EnterpriseFlow.Domain.UnitTests
  EnterpriseFlow.Application.UnitTests
  EnterpriseFlow.Architecture.Tests   # Reglas de dependencia entre capas (NetArchTest).
  EnterpriseFlow.Api.IntegrationTests
docs/
  01-vision-y-alcance.md
  02-roadmap.md
  03-diseno-arquitectura/    # Vistas C4 + diagramas de secuencia
  backlog/                  # Epics, Features, Historias de Usuario
  adr/                      # Architecture Decision Records
```

La regla de dependencias (Domain ← Application ← Infrastructure/Api) y la
razón para combinar Clean Architecture con Vertical Slice están justificadas
en [ADR-0002](./docs/adr/ADR-0002-clean-architecture-vertical-slices.md).

## Requisitos

- .NET 8 SDK
- SQL Server, o Docker + Docker Compose (ver abajo)

## Compilar y probar

```bash
dotnet build EnterpriseFlow.slnx
dotnet test EnterpriseFlow.slnx
```

## Ejecutar todo con Docker Compose (recomendado)

Levanta SQL Server + Api + Frontend con un solo comando, sin instalar nada más
que Docker — la Api aplica las migraciones pendientes automáticamente al
arrancar (`Program.cs`, Sprint 11) contra un contenedor de SQL Server recién
creado y vacío.

```bash
cp .env.example .env
# editar .env: SA_PASSWORD (cumple la política de complejidad de SQL Server) y
# JWT_SIGNING_KEY (32+ caracteres; nunca reutilizar el valor de ejemplo)

docker compose up --build
```

- Frontend: http://localhost:8081
- Api + Swagger: http://localhost:5050/swagger
- SQL Server: `localhost,1433` (mismas credenciales que en `.env`)

El frontend habla con la Api a través de un proxy de nginx (`src/EnterpriseFlow.Web/nginx.conf`)
que reenvía `/api/*` al contenedor de la Api internamente — el navegador solo
ve un origen (`localhost:8081`), igual que con el proxy de Vite en desarrollo,
así que no hace falta configurar CORS para este stack. Detalle completo,
incluyendo por qué corre en `Development` (Swagger habilitado, sin HSTS —
este stack no tiene terminación TLS delante) en
[docs/11-devops.md](./docs/11-devops.md).

## Ejecutar la API (sin Docker)

```bash
dotnet run --project src/EnterpriseFlow.Api
```

Expone `/health` (Health Checks), `/api/auth/*` (registro/login/refresh/roles,
F1.x) y `/api/companies` (F2.1), y en `Development`, Swagger UI (con soporte
para pegar el Bearer token). Flujo típico:

```bash
curl -X POST http://localhost:5000/api/auth/register-tenant -H "Content-Type: application/json" \
  -d '{"tenantName":"Acme","tenantSlug":"acme","adminEmail":"admin@acme.test","adminPassword":"SuperSecret123!"}'

curl -X POST http://localhost:5000/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"admin@acme.test","password":"SuperSecret123!"}'
# -> usar el accessToken devuelto como "Authorization: Bearer <token>" en el resto de llamadas
```

El tenant/usuario/permisos actuales se resuelven de los claims del JWT
validado (`EnterpriseFlow.Infrastructure/Identity/JwtCurrentTenantService.cs`
y `JwtCurrentUserService.cs`), no de headers — ese stub temporal de Sprint 4
se retiró en Sprint 7a.

## Ejecutar el Frontend (sin Docker)

```bash
cd src/EnterpriseFlow.Web
npm install
npm run dev
```

Sirve en `http://localhost:5173` con proxy de `/api` hacia la Api en
`http://localhost:5050` (ver `vite.config.ts`) — no hace falta configurar CORS
para desarrollo local. Requiere Node ≥20.19 o ≥22.12 idealmente; con Node
20.18 funciona igual pero fuerza `vite@^6` en vez de la última versión (ver
`docs/08-frontend.md`, sección "Deuda técnica reconocida").

## Migraciones de EF Core

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add <Nombre> --project src/EnterpriseFlow.Infrastructure --startup-project src/EnterpriseFlow.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/EnterpriseFlow.Infrastructure --startup-project src/EnterpriseFlow.Api
```

## Documentación

- [Visión y alcance](./docs/01-vision-y-alcance.md)
- [Roadmap por Releases](./docs/02-roadmap.md)
- [Backlog: Epics y Features](./docs/backlog/epics.md)
- [Historias de Usuario del MVP](./docs/backlog/historias-usuario-mvp.md)
- [Diseño de arquitectura (C4 + secuencias)](./docs/03-diseno-arquitectura/00-resumen.md)
- [Validación de arquitectura](./docs/04-validacion-arquitectura.md)
- [Modelo de dominio](./docs/05-modelo-dominio.md)
- [Base de datos (esquema ER, índices)](./docs/06-base-de-datos.md)
- [Backend — Identidad](./docs/07a-identidad.md) · [Módulos de negocio](./docs/07b-modulos-negocio.md)
- [Frontend](./docs/08-frontend.md) · [Revisión de seguridad ad-hoc](./docs/08a-seguridad.md)
- [Pruebas y cobertura](./docs/09-pruebas.md)
- [Documentación (auditoría Sprint 10)](./docs/10-documentacion.md)
- [DevOps (Docker, CI)](./docs/11-devops.md)
- [ADRs (índice consolidado)](./docs/adr/README.md)
- **Release 2**: [Visión y alcance](./docs/r2-01-vision-y-alcance.md) ·
  [Historias de Usuario](./docs/backlog/historias-usuario-release2.md) ·
  [Arquitectura (esqueleto Sprint 3)](./docs/r2-03-arquitectura.md) ·
  [Validación (Catálogos, Sprint 4)](./docs/r2-04-validacion.md) ·
  [Modelo de dominio (Sprint 5)](./docs/r2-05-modelo-dominio.md) ·
  [Base de datos (Sprint 6)](./docs/r2-06-base-de-datos.md) ·
  [Backend — Workflow (Sprint 7a)](./docs/r2-07a-backend-workflow.md) ·
  [Backend — Documentos (Sprint 7b)](./docs/r2-07b-backend-documentos.md) ·
  [Backend — Notificaciones (Sprint 7c)](./docs/r2-07c-backend-notificaciones.md) ·
  [Frontend — Catálogos y Workflow (Sprint 8a)](./docs/r2-08a-frontend-catalogos-workflow.md) ·
  [Frontend — Documentos (Sprint 8b)](./docs/r2-08b-frontend-documentos.md) ·
  [Frontend — Notificaciones (Sprint 8c)](./docs/r2-08c-frontend-notificaciones.md) ·
  [Pruebas (Sprint 9)](./docs/r2-09-pruebas.md) ·
  [Documentación (Sprint 10)](./docs/r2-10-documentacion.md) ·
  [DevOps (Sprint 11)](./docs/r2-11-devops.md)
- **Release 3**: [Visión y alcance](./docs/r3-01-vision-y-alcance.md) ·
  [Historias de Usuario](./docs/backlog/historias-usuario-release3.md) ·
  [Arquitectura (esqueleto Sprint 3)](./docs/r3-03-arquitectura.md) ·
  [Validación (Sprint 4)](./docs/r3-04-validacion.md) ·
  [Modelo de dominio (Sprint 5)](./docs/r3-05-modelo-dominio.md) ·
  [Base de datos (Sprint 6)](./docs/r3-06-base-de-datos.md) ·
  [Backend — proveedores de IA (Sprint 7a)](./docs/r3-07a-backend-ia-providers.md) ·
  [Backend — RAG (Sprint 7b)](./docs/r3-07b-backend-rag.md) ·
  [Backend — más herramientas (Sprint 7c)](./docs/r3-07c-backend-mas-herramientas.md) ·
  [Frontend (Sprint 8)](./docs/r3-08-frontend.md) ·
  [Pruebas (Sprint 9)](./docs/r3-09-pruebas.md) ·
  [Documentación (Sprint 10)](./docs/r3-10-documentacion.md) ·
  [DevOps (Sprint 11)](./docs/r3-11-devops.md)
- **Release 4**: [Visión y alcance](./docs/r4-01-vision-y-alcance.md) ·
  [Historias de Usuario](./docs/backlog/historias-usuario-release4.md) ·
  [Arquitectura (Sprint 3)](./docs/r4-03-arquitectura.md) ·
  [Validación (Sprint 4)](./docs/r4-04-validacion.md) ·
  [Modelo de dominio (Sprint 5)](./docs/r4-05-modelo-dominio.md) ·
  [Base de datos (Sprint 6)](./docs/r4-06-base-de-datos.md) ·
  [Backend (Sprint 7)](./docs/r4-07-backend.md) ·
  [Frontend (Sprint 8)](./docs/r4-08-frontend.md) ·
  [Pruebas (Sprint 9)](./docs/r4-09-pruebas.md) ·
  [Documentación (Sprint 10)](./docs/r4-10-documentacion.md) ·
  [DevOps (Sprint 11)](./docs/r4-11-devops.md)
- [Publicación en GitHub — CI, PRs, releases y CD](./docs/12-publicacion.md)
