# Release 2, Sprint 7c — Backend: Notificaciones (F6)

Tercera y última sub-parte de Sprint 7 (Backend) de Release 2 — cierra el
Sprint completo.

## Qué se implementó

**Application** (`Features/Documents/NotifyOnWorkflowTransition/`):
`NotifyOnDocumentWorkflowTransitionedHandler`, un
`INotificationHandler<DomainEventNotification<DocumentWorkflowTransitionedDomainEvent>>`
— exactamente el mismo pipeline de eventos de dominio que
`CascadeContactsOnClientDeactivatedHandler` ya usa desde Release 1 (HU-012).
Ningún bus de mensajería nuevo (ADR-0011): Sprint 5 ya hizo que
`Document.TransitionTo` levantara el evento; este sprint solo agrega un
handler que reacciona a él.

**Application** (`Features/Notifications/`): `GetMyNotifications`,
`MarkNotificationRead` (HU-062) — sin permiso propio en el catálogo, ambos
se limitan a los datos del propio usuario autenticado
(`ICurrentUserService.UserId`), así que estar autenticado es el único
requisito.

**Application** (`Abstractions/IEmailSender.cs`, `IEmailQueue.cs`):
abstracciones nuevas para F6.2, mismo patrón que `IRealtimeNotifier` ya
estableció en Sprint 3 — Application nunca sabe que Hangfire ni SMTP
existen.

**Infrastructure** (`Email/`): `SmtpEmailSender` (SMTP real vía
`System.Net.Mail.SmtpClient`), `HangfireEmailQueue` (encola sobre el mismo
storage de Hangfire ya verificado en Sprint 3), `NullEmailQueue`
(degradación elegante cuando Hangfire no está configurado — mismo patrón
que el fallback de Redis a caché en memoria).

## Cómo se resuelven los destinatarios

Un `Document` no tiene lista de "interesados" propia. Se decidió: los
destinatarios son los miembros del `Project` dueño (`OwnerType.Project`) o
del Proyecto de la Tarea dueña (`OwnerType.Task`, resuelto vía
`ProjectTask.ProjectId`) — el caso real que HU-081 describe (un revisor de
proyecto se entera cuando un documento cambia). Un `Document` con
`OwnerType.Client` **no genera destinatarios**: `Client` no tiene concepto
de "miembros" en este dominio (a diferencia de `Project`, no está dotado de
personal) y ninguna HU pidió inventar una regla ahí — el handler
simplemente resuelve una lista vacía y no hace nada, en vez de forzar una
regla no solicitada. Probado explícitamente
(`Transitioning_A_Client_Owned_Document_Creates_No_Notification`).

## Un bug real encontrado por la propia suite de pruebas

`GetMyNotificationsQueryHandler` originalmente ordenaba con
`.OrderByDescending(n => n.CreatedAtUtc)` **dentro** de la consulta LINQ.
Contra SQL Server (producción) esto es válido — `datetimeoffset` es
ordenable de forma nativa en T-SQL. Contra SQLite (la suite de integración,
elegida deliberadamente en vez del proveedor InMemory precisamente para
detectar este tipo de problema de traducción real — ver `docs/09-pruebas.md`)
el proveedor de EF Core no sabe traducir un `ORDER BY` sobre
`DateTimeOffset` y lanza `NotSupportedException` — capturado como un 500 en
la primera corrida de estas pruebas, no anticipado de antemano. Corregido
materializando la consulta primero (`ToListAsync()`) y ordenando en memoria
después — funciona igual en ambos motores, y una lista de notificaciones
por usuario es lo bastante chica para que el costo no importe. Es
exactamente el tipo de gap que la elección de SQLite sobre InMemory
(documentada desde Sprint 6 de Release 1) existe para atrapar.

## Qué se verificó de verdad — y qué no

- **La cadena completa evento de dominio → Notification persistida → HTTP**
  se prueba de punta a punta:
  `Transitioning_A_Project_Owned_Document_Notifies_Every_Project_Member`
  sube un Documento real, lo transiciona vía el endpoint de 7b, y confirma
  que el miembro del Proyecto ve la notificación en `GET /api/notifications`
  — sin ningún atajo directo entre `TransitionDocumentCommandHandler` y
  Notificaciones, solo el pipeline de eventos ya existente.
- **Protección IDOR verificada explícitamente**:
  `MarkNotificationRead_On_Another_Users_Notification_Returns_NotFound`
  confirma que un usuario del mismo tenant no puede marcar como leída una
  notificación ajena — 404, no 403 (no revela si el recurso existe).
- **`HangfireEmailQueue`/`SmtpEmailSender` no se ejercitaron en ejecución
  real en este sprint**: el entorno "Testing" (`CustomWebApplicationFactory`)
  no configura una cadena de conexión de Hangfire, así que
  `NullEmailQueue` es lo que las pruebas de integración realmente usan — el
  mismo comportamiento que un despliegue local sin Hangfire configurado
  tendría. El código de encolado en sí reutiliza el storage de Hangfire ya
  verificado contra LocalDB real en Sprint 3 (creación de esquema,
  conectividad) y es una única llamada estándar de la API de Hangfire
  (`Enqueue<T>(...)`), sin infraestructura nueva que verificar; el envío
  SMTP real no se pudo probar por no existir un servidor de correo
  disponible en este entorno — mismo tipo de límite ya declarado para
  Redis (Sprint 3) y los proveedores cloud de Documentos (Sprint 7b).

## Verificación

**Suite completa: 207/207 tests** (122+20+6+59 — 4 pruebas de integración
nuevas para `NotificationsEndpoints`, sin regresiones en las 203 previas).
`dotnet format --verify-no-changes` limpio.

## Cierre de Sprint 7 (Backend) de Release 2

Con 7a (Workflow), 7b (Documentos) y 7c (Notificaciones) completos, Sprint 7
de Release 2 queda cerrado. Backend real y probado para las tres features
que Sprint 1 (Análisis) prometió y Sprint 5 (Modelo de Dominio) modeló.
Sigue Sprint 8 (Frontend).
