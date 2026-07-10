# Historias de Usuario y Casos de Uso — Release 2

Mismo formato que [`historias-usuario-mvp.md`](./historias-usuario-mvp.md):
`Como <rol>, quiero <acción>, para <beneficio>`, con criterios de aceptación
en Gherkin donde hay una regla de negocio real que lo justifica. Cada
historia referencia su Feature en [`epics.md`](./epics.md) y continúa la
numeración de HU de Release 1 (dejando huecos deliberados por Epic, igual
que en el documento de Release 1, para poder insertar HUs nuevas sin
renumerar). Contexto de alcance completo en
[`../r2-01-vision-y-alcance.md`](../r2-01-vision-y-alcance.md).

---

## E4 (avanzado) — Dashboard Ejecutivo y Reportes

### HU-032 — Reportes exportables (F4.3)
Como usuario con acceso al Dashboard, quiero exportar los indicadores y
listados (Proyectos, Tareas, Clientes) a CSV o Excel, para analizarlos fuera
del sistema o compartirlos con quien no tiene acceso a la plataforma.

```gherkin
Dado un listado de Proyectos filtrado por estado "Activo"
Cuando solicito exportarlo a Excel
Entonces recibo un archivo con exactamente las filas visibles en el listado filtrado,
  no el listado completo sin filtrar
```
Regla: la exportación respeta los filtros/permisos aplicados en el momento
de la solicitud — nunca expone más datos de los que el usuario ya podía ver
en pantalla.

### HU-033 — Mapa y alertas (F4.4)
Como usuario con acceso al Dashboard, quiero ver en un mapa la ubicación de
Empresas/Clientes que tienen dirección registrada, y alertas de indicadores
fuera de rango (p. ej. proyectos con más de N tareas vencidas), para
detectar problemas operativos de un vistazo.

---

## E5 — Documentos

### HU-050 — Subir, descargar y eliminar Documentos (F5.1, F5.2, F5.6)
Como usuario con permiso `documents.manage`, quiero subir, descargar y
eliminar archivos asociados a un Proyecto/Cliente/Tarea, sin que me importe
en qué proveedor físico de almacenamiento está configurado el sistema, para
centralizar los archivos de trabajo de cada entidad.

```gherkin
Dado el proveedor de almacenamiento configurado como "Local"
Cuando subo un archivo a un Proyecto
Entonces puedo descargarlo de vuelta con el mismo contenido exacto

Dado el mismo escenario pero con el proveedor configurado como "AzureBlobStorage"
  (sin ningún cambio de código, solo configuración)
Cuando repito la misma operación
Entonces el comportamiento observable es idéntico
```
Regla de negocio (arquitectónica): `Application` depende de una interfaz
`IDocumentStorageProvider`, nunca de un SDK de proveedor específico — el
proveedor activo se resuelve por Dependency Injection según configuración
(F5.3/F5.4/F5.5 son implementaciones adicionales de la misma interfaz, no
historias de usuario distintas: el usuario nunca percibe cuál está activo).

### HU-051 — Validación de archivos subidos (F5.7)
Como sistema, quiero rechazar archivos que no cumplan las reglas de tipo,
tamaño y extensión permitida antes de persistirlos, para prevenir abuso de
almacenamiento y archivos potencialmente peligrosos.

```gherkin
Dado un límite de tamaño configurado de 25 MB
Cuando intento subir un archivo de 40 MB
Entonces la operación se rechaza antes de escribir ningún byte al proveedor de storage

Dado una lista de extensiones permitidas que no incluye ".exe"
Cuando intento subir "instalador.exe"
Entonces la operación se rechaza con un mensaje que indica el tipo no permitido,
  verificado por el contenido real del archivo (magic bytes), no solo por la extensión del nombre
```
Regla de seguridad: la validación de tipo se hace por firma binaria real del
archivo, no confiando en la extensión ni en el `Content-Type` declarado por
el cliente — ambos son controlados por quien sube el archivo y no son una
fuente confiable (mismo principio de "no confiar en input del cliente" que
guio la revisión de seguridad de Release 1, ver `08a-seguridad.md`).

---

## E6 — Notificaciones

### HU-060 — Notificaciones in-app en tiempo real (F6.1)
Como usuario autenticado, quiero recibir notificaciones en tiempo real
mientras tengo la aplicación abierta (nueva asignación de tarea, documento
aprobado/rechazado), sin necesitar refrescar la página, para enterarme de
eventos relevantes al momento.
Regla: la conexión SignalR se autentica con el mismo JWT de la sesión — un
usuario solo recibe notificaciones dirigidas a su propio `UserId` dentro de
su propio `TenantId`, nunca las de otro usuario u otro tenant.

### HU-061 — Notificaciones por correo (F6.2)
Como usuario, quiero recibir por correo los eventos importantes que ocurren
fuera de mi sesión activa (p. ej. mientras no tengo la aplicación abierta),
para no depender de estar conectado para enterarme.

```gherkin
Dado un evento que dispara notificación por correo (p. ej. documento aprobado)
Cuando el evento se publica
Entonces se encola un background job (Hangfire) que envía el correo de forma asíncrona
Y la respuesta HTTP de la operación que originó el evento no espera a que el correo se envíe
```
Regla (ADR-0008): ninguna operación síncrona de la Api debe bloquearse
esperando una respuesta de un proveedor de correo externo.

### HU-062 — Centro de notificaciones (F6.3)
Como usuario, quiero ver un historial de mis notificaciones (leídas y no
leídas) y marcarlas como leídas, para poder repasar eventos que me perdí.

---

## E7 (ampliación) — Auditoría, Logs y Observabilidad

### HU-042 — Vista de Logs en UI (F7.4)
Como administrador de tenant, quiero ver los logs de auditoría (HU-040) y
eventos relevantes del sistema en una vista dentro de la propia aplicación,
sin necesitar acceso a la infraestructura del servidor, para investigar
incidentes o cambios sin depender de otro equipo.
Regla: un administrador de tenant solo ve logs/auditoría de su propio
tenant — mismo aislamiento que cualquier otro dato (ADR-0003).

### HU-043 — Health Checks avanzados (F7.8)
Como operador, quiero que `/health` reporte el estado individual de cada
dependencia externa (SQL Server, Redis, storage de Hangfire, proveedor de
Documentos activo), no solo un estado agregado, para saber exactamente cuál
falló sin tener que revisar logs.

---

## E8 — Workflow y Catálogos

### HU-080 — Motor de Workflow configurable (F8.1)
Como administrador de tenant, quiero definir un flujo de estados y
transiciones permitidas para un tipo de entidad, para modelar procesos de
aprobación propios de mi organización sin necesitar un cambio de código.

```gherkin
Dado un Workflow definido con estados "Borrador", "En Revisión", "Aprobado", "Rechazado"
  y transiciones permitidas: Borrador→En Revisión, En Revisión→Aprobado, En Revisión→Rechazado
Cuando intento mover una entidad de "Borrador" directamente a "Aprobado"
Entonces la operación se rechaza (transición no definida en el Workflow)
```
Regla de negocio central (la que distingue esto de una máquina de estados
hardcodeada): las transiciones válidas son datos, no código — agregar un
nuevo Workflow o modificar uno existente no requiere una nueva versión de la
Api.

### HU-081 — Aprobación de Documentos vía Workflow (F5 + F8.1)
Como usuario con permiso `documents.approve`, quiero que un Documento subido
pase por un flujo de aprobación (Borrador → En Revisión → Aprobado/Rechazado)
antes de considerarse definitivo, para que exista control sobre qué
documentos son válidos para el Proyecto.
Es el primer (y único, en este Release) consumidor real del motor de F8.1 —
ver `r2-01-vision-y-alcance.md`, sección 3, sobre por qué no se inventan
consumidores adicionales especulativamente.

### HU-082 — Catálogos genéricos (F8.2)
Como administrador de tenant, quiero definir y editar listas de referencia
propias de mi organización (p. ej. Categorías de Documento), para
adaptar el sistema a mi vocabulario de negocio sin pedir un cambio de código.

```gherkin
Dado un Catálogo "Categorías de Documento" con los ítems "Contrato", "Factura"
Cuando un administrador agrega el ítem "Propuesta"
Entonces las lecturas subsecuentes del catálogo (cacheadas en Redis) reflejan
  el nuevo ítem de inmediato, no tras expirar un TTL
```
Regla (ADR-0008): la invalidación de cache es explícita en cada escritura,
no solo por expiración temporal — un catálogo que tarda en reflejar un
cambio reciente confundiría al administrador que acaba de editarlo.

### HU-083 — Módulo de Configuración (F8.3)
Como administrador de tenant, quiero editar parámetros de configuración de
mi organización (p. ej. tamaño máximo de archivo, remitente de correo) desde
la UI, para ajustar el comportamiento del sistema sin depender de un
despliegue.

---

## Fuera de alcance de Release 2 (recordatorio)

AI Assistant, RAG, servidor MCP propio, MassTransit/RabbitMQ, OpenTelemetry,
Elastic Search, Application Insights **no** tienen historias de usuario en
este documento — Release 3/4, ver `02-roadmap.md` y ADR-0008 ("Explícitamente
no se activan").
