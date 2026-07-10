# Release 2, Sprint 6 — Base de Datos

Mismo alcance que Sprint 6 de Release 1: configuración de EF Core
(`IEntityTypeConfiguration<T>`), migración, índices — para las tres
entidades que Sprint 5 (Modelo de Dominio) dejó listas: `Document` (F5),
`WorkflowDefinition`/`WorkflowState`/`WorkflowTransition` (F8.1),
`Notification` (F6.3). `CatalogDefinition`/`CatalogItem` (F8.2) ya tenían su
configuración desde Sprint 4 (Validación) — este Sprint aprovecha para
incorporarlas también al diagrama ER consolidado (`06-base-de-datos.md`),
que hasta ahora no las tenía documentadas ahí (quedaban solo en
`r2-04-validacion.md`).

## Qué se agregó

Cinco `IEntityTypeConfiguration<T>` nuevas
(`DocumentConfiguration`, `WorkflowDefinitionConfiguration`,
`WorkflowStateConfiguration`, `WorkflowTransitionConfiguration`,
`NotificationConfiguration`) y una migración (`AddDocumentsWorkflowsNotifications`,
5 tablas). Mismo mecanismo de Sprint 6 de Release 1: la generalización del
filtro multi-tenant + soft-delete por reflexión sobre
`ITenantScoped`/`ISoftDeletable` (`AppDbContext.OnModelCreating`) cubre las
entidades nuevas automáticamente — ninguna requirió tocar ese mecanismo,
confirmando otra vez que generalizarlo en su momento (Sprint 6 de Release 1)
fue la decisión correcta.

## Decisiones de índices (detalle completo en `06-base-de-datos.md`)

- **`Documents (TenantId, OwnerType, OwnerId)`**: la única consulta real que
  HU-050 pide — "documentos de este Proyecto/Cliente/Tarea", la pestaña
  Documentos de una página de detalle.
- **`Notifications (TenantId, UserId, IsRead)`**: el único patrón de lectura
  de HU-062 — el centro de notificaciones de un usuario, filtrado por
  leído/no leído.
- **`WorkflowTransitions (WorkflowDefinitionId, FromStateId, ToStateId)`
  único**: refuerza a nivel de BD la invariante que `WorkflowDefinition.AddTransition`
  ya revisa en memoria (Sprint 5) — mismo patrón de defensa en profundidad
  que `ProjectMembers`/`RolePermissions`.
- **Sin FK física** en `Documents.OwnerId`, `Documents.CurrentWorkflowStateId`,
  `Notifications.UserId`, `WorkflowTransitions.FromStateId`/`ToStateId` —
  todas ya justificadas en ADR-0005/ADR-0009/ADR-0010; `06-base-de-datos.md`
  tiene el detalle de por qué cada una, incluyendo el caso de
  `WorkflowTransitions` (una FK real habría exigido una FK compuesta contra
  `(WorkflowDefinitionId, Id)` en `WorkflowStates`, complejidad de esquema
  que el chequeo en Domain ya cubre).

## Verificación

Migración generada y **aplicada contra una base LocalDB real** (no solo
generada y dada por buena): `InitialCreate` → `AddCatalogs` →
`AddDocumentsWorkflowsNotifications`, las tres limpias en secuencia contra
una base nueva, sin intervención manual. Suite completa sin cambios de
comportamiento: **180/180 tests** (el esquema de pruebas usa SQLite vía
`EnsureCreated()`, que reconstruye el modelo actual en cada ejecución —
no ejercita las migraciones en sí, por eso la verificación contra LocalDB
real es la que prueba que las migraciones aplican, no solo que el modelo
final es consistente). `dotnet format --verify-no-changes` limpio.
