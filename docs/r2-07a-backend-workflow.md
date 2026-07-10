# Release 2, Sprint 7a — Backend: Workflow (F8.1)

Primera sub-parte de Sprint 7 (Backend) de Release 2 — mismo criterio que
Release 1 aplicó con 7a (Identidad) / 7b (Módulos de negocio): dividir un
Sprint grande en partes completas e independientemente verificables en vez
de avanzar todo a medias. Workflow va primero porque Documentos (7b, HU-050/
HU-081) necesita un Workflow ya existente para poder crear un Documento —
construir Documentos antes habría dejado el flujo principal sin poder
probarse de punta a punta.

## Qué se implementó

**Application** (`Features/Workflows/`): `CreateWorkflow`,
`AddWorkflowState`, `AddWorkflowTransition`, `GetWorkflowById`,
`GetWorkflows` — cinco slices verticales sobre el mismo pipeline
(Authorization + Validation) que el resto del sistema.
`Permissions.Workflows.{Read,Manage}` nuevos.

**Api**: `WorkflowsEndpoints` (`/api/workflows`, con `/states` y
`/transitions` anidados).

Sin cambios en Domain (Sprint 5) ni en Infrastructure/migraciones
(Sprint 6) — este sprint es estrictamente la capa de casos de uso y
endpoints sobre lo ya construido, igual que Sprint 7 de Release 1.

## Prueba central: HU-080 verificada por HTTP, no solo por unit test

`WorkflowsEndpointsTests.Build_A_Workflow_From_States_And_Transitions_Then_Read_It_Back`
construye un Workflow completo únicamente a través de requests HTTP (crear →
agregar 3 estados → agregar 2 transiciones) y confirma que la lectura
posterior refleja exactamente esa estructura — la prueba de que "las
transiciones son datos, no código" (la razón de ser de F8.1 frente a un
enum, ver ADR-0010) funciona de punta a punta, no solo a nivel de
`WorkflowDefinition` en aislamiento (eso ya lo probó Sprint 5).

`AddWorkflowTransition_Referencing_A_State_From_Another_Workflow_Returns_BadRequest`
confirma que la invariante de Domain (un estado debe pertenecer al mismo
Workflow que la transición) sigue aplicándose cuando se llega por HTTP real,
con dos Workflows reales creados por el propio test — no un mock.

## Verificación

- **Suite completa: 186/186 tests** (122+12+6+46 — 6 pruebas de integración
  nuevas para `WorkflowsEndpoints`).
- `dotnet format --verify-no-changes` limpio.
- No se hizo verificación manual en navegador para este sprint — la
  cobertura de integración (HTTP real contra SQLite, aislamiento de tenant
  incluido) ya prueba el mismo camino que probaría un smoke test manual;
  Documentos (7b) sí tendrá verificación en vivo dado que involucra subida
  real de archivos contra un proveedor de storage.

## Qué sigue (7b, Documentos)

Con Workflow ya construido, 7b puede crear un Documento real que arranca en
el estado inicial de un Workflow real y probar `TransitionTo` end-to-end —
sin esto, 7b habría tenido que simular o hardcodear un `WorkflowStateId`
sin validación real detrás.
