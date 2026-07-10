# Release 2, Sprint 9 — Pruebas

Mismo alcance que Sprint 9 de Release 1: no un sprint que agrega tests
sprint-por-sprint (eso ya venía pasando desde Sprint 4), sino el punto donde
se **mide** la cobertura real acumulada de Release 2 como conjunto y se
cierran los gaps reales que la medición encuentra — en vez de seguir
confiando en que "cada sprint agregó sus tests" sea lo mismo que "el
sistema está bien probado".

## Medición inicial

`dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"`
+ `reportgenerator`, misma herramienta que Release 1 (sin paquete nuevo).

Antes de escribir un test nuevo, con la exclusión de migraciones ya heredada
de Release 1: **91.4% de líneas** global, **82% de branches**, **90.1% de
métodos** — ya por encima del ≥90% que exige la especificación, pero
engañoso otra vez, igual que en Release 1: el número agregado escondía
gaps reales concentrados en unos pocos lugares, y una parte del déficit
restante no era código sin probar sino código genuinamente no verificable
en este entorno.

## Gaps reales encontrados

- **`LoginCommandHandler`/`RefreshAccessTokenCommandHandler` (Identidad,
  Release 1)**: la rama de mitigación de timing attack (`user is null` →
  hashear una contraseña de mentira de todos modos, HU-001/ADR-0006) y el
  camino de "refresh token que nunca existió" (distinto de uno robado/
  reusado) estaban en **0%** — cada test de login/refresh existente partía
  siempre de un usuario o token real. Un hueco de seguridad sin cubrir que
  llevaba desde Release 1 sin que nadie lo notara hasta ahora.
- **`WorkflowDefinition.AddTransition` / `CatalogDefinition.AddItem`**:
  los guard clauses de nombre/label vacío (Release 2, Sprints 4-5) nunca se
  probaron — mismo patrón que Release 1 ya documentó ("el lado de
  escritura tiene tests para las invariantes 'interesantes', los guard
  clauses triviales se cubren mucho menos").
- **El hallazgo más importante: `NotifyOnDocumentWorkflowTransitionedHandler`
  (F6, Sprint 7c) nunca verificó sus propios efectos reales.** Las pruebas
  de Sprint 7c solo comprobaban que aparecía una fila `Notification` — la
  llamada real a `IRealtimeNotifier.NotifyUserAsync` y a
  `IEmailQueue.Enqueue` estaba en **0%**, igual que la rama completa de
  `DocumentOwnerType.Task` (resolver destinatarios vía el Proyecto de una
  Tarea). Al intentar cerrar este gap con un test nuevo se encontró un
  **segundo problema, esta vez en la propia infraestructura de pruebas**:
  los tests anteriores agregaban un `ProjectMember` con un `userId`
  inventado, sin una fila `User` real detrás — la persistencia de
  `Notification` no lo necesita, pero el paso que busca el email del
  destinatario (`db.Users.Where(u => recipientUserIds.Contains(u.Id))`) sí,
  y silenciosamente no encontraba a nadie. En producción esto nunca pasa
  (`AddProjectMemberValidator` exige que el usuario exista antes de
  agregarlo como miembro) — pero significa que, hasta este sprint, **nunca
  se había probado de verdad** que el push en tiempo real y el encolado de
  correo se dispararan. Corregido agregando un `User` real al fixture de
  prueba (`SeedProjectWithRealMemberAsync`) y verificando las llamadas con
  fakes inyectables (`FakeRealtimeNotifier`/`FakeEmailQueue`, registrados
  en `CustomWebApplicationFactory` reemplazando a los reales).
- **`DocumentsEndpoints.UploadAsync`**: al parsear el multipart a mano (sin
  binding automático, decisión de Sprint 7b), sus tres guard clauses
  (cuerpo no-multipart, sin archivo, campos de formulario malformados)
  estaban en 0% — todas las pruebas de subida existentes siempre mandaban
  una petición bien formada.

## Qué se agregó

- **`tests/EnterpriseFlow.Api.IntegrationTests`**: `Fakes/FakeRealtimeNotifier.cs`,
  `Fakes/FakeEmailQueue.cs` (nuevos, registrados como singletons en
  `CustomWebApplicationFactory` reemplazando `SignalRNotifier`/
  `NullEmailQueue`). `IdentityEndpointsTests` ganó `Login_With_Unknown_Email_Returns_Unauthorized`
  y `Refresh_With_A_Token_That_Was_Never_Issued_Returns_Unauthorized`.
  `NotificationsEndpointsTests` ganó `Transitioning_A_Document_Really_Invokes_The_Realtime_Notifier_And_Email_Queue`
  y `Transitioning_A_Task_Owned_Document_Notifies_The_Tasks_Project_Members`
  (más el helper `SeedProjectWithRealMemberAsync`). `DocumentsEndpointsTests`
  ganó `Upload_With_A_NonMultipart_Body_Returns_BadRequest`,
  `Upload_Without_A_File_Returns_BadRequest`,
  `Upload_With_Malformed_Form_Fields_Returns_BadRequest`.
- **`tests/EnterpriseFlow.Domain.UnitTests`**: `WorkflowDefinitionTests.AddTransition_With_Missing_Name_Throws`,
  `CatalogDefinitionTests.AddItem_With_Missing_Label_Throws`.

## Exclusiones de cobertura ampliadas

Mismo criterio que la exclusión de migraciones EF Core en Release 1 —
código real, pero no verificable de forma significativa en este entorno:
los 3 proveedores cloud de Documentos (`AmazonS3StorageProvider`,
`AzureBlobStorageProvider`, `GoogleCloudStorageProvider`, F5/ADR-0009) y el
envío SMTP (`SmtpEmailSender`, F6.2), junto con sus clases de opciones,
excluidos vía `coverlet.runsettings`. Ya declarado explícitamente al
construirlos (`r2-07b-backend-documentos.md`, `r2-07c-backend-notificaciones.md`):
envuelven SDKs reales de terceros que necesitan una cuenta/servidor real
para probarse con sentido — un test con Moq solo probaría "este código
llama al método del SDK que llama", no el comportamiento real de subida/
descarga/envío, que es lo que de verdad importa y que `LocalStorageProvider`
(dentro del número de cobertura, 100%, con E/S de disco real) ya prueba que
el patrón funciona.

## Resultado

| | Antes | Después |
|---|---|---|
| Tests totales | 207 | **218** |
| Cobertura de líneas (global) | 91.4%* | **95.5%** |
| Cobertura de branches | 82% | **86.2%** |
| Cobertura de métodos | 90.1% | 93.6% |

\* Medido solo con la exclusión de migraciones (heredada de Release 1); no
directamente comparable con el 95.5% final, que además excluye los SDKs
cloud/SMTP — el número que importa es que, tras excluir lo genuinamente no
verificable, todos los gaps reales de comportamiento quedaron cerrados.

Por ensamblado: `EnterpriseFlow.Application` **99.6%**, `EnterpriseFlow.Domain`
**94.6%**, `EnterpriseFlow.Api` **93.1%**, `EnterpriseFlow.Infrastructure`
**91.7%** — los cuatro por encima del ≥90% global exigido.

## Explícitamente no perseguido hasta el 100%

- **`Infrastructure.DependencyInjection` (58.2%)**: las ramas condicionales
  de selección de proveedor (Redis/Hangfire/AzureBlob/S3/Gcs) solo se
  toman cuando esa configuración específica está presente — el entorno
  "Testing" usa exactamente una combinación (Local storage, sin Hangfire,
  caché en memoria) de las varias posibles. Mismo razonamiento que
  `Program.cs` en Release 1 (código de composición de arranque, no lógica
  de negocio).
- **`Email.HangfireEmailQueue`/`Email.NullEmailQueue` (0%)**: un no-op y
  una única línea que delega en la API de Hangfire — no forman parte de la
  exclusión (no envuelven un SDK externo, son código propio simple), pero
  tampoco se persiguieron con un test dedicado: su valor incremental frente
  al esfuerzo no lo justifica.
- **`Realtime.JwtSubUserIdProvider`/`Realtime.SignalRNotifier` (0%)**: no
  triviales de probar de forma aislada (`HubConnectionContext` no es
  fácilmente construible fuera de una conexión SignalR real), pero **sí
  verificados en ejecución real**: la prueba en navegador de Sprint 8c
  (`r2-08c-frontend-notificaciones.md`) demostró el push llegando al
  usuario correcto — algo que solo puede pasar si `JwtSubUserIdProvider`
  extrajo bien el `sub` del token. Verificado en vivo, no por test
  automatizado.
- **Getters de auditoría en Domain (`CreatedAtUtc`/`ModifiedAtUtc`/
  `ModifiedBy`)**: mismo patrón ya aceptado en Release 1 — son
  propiedades triviales sin lógica, asignadas por
  `AuditableEntitySaveChangesInterceptor`, no por las propias entidades;
  ningún test de Domain las lee directamente, y no hay invariante que
  verificar ahí.
- Mismo criterio que Release 1: no se introdujo mutation testing
  (Stryker.NET) — sigue siendo candidato de una release futura si se
  justifica.

## Verificación

`dotnet test EnterpriseFlow.slnx` — **218/218 passing**. Reporte HTML en
`coverage-report/index.html` (no versionado, regenerable):

```bash
dotnet test EnterpriseFlow.slnx --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory ./coverage-results
reportgenerator -reports:"coverage-results/**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:"Html;TextSummary"
```
