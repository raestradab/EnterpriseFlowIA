# Release 4, Sprint 6 — Base de Datos

Mismo criterio que Sprint 5: **confirmación, no introducción** — la
migración de Temporal Tables (`AddTemporalTablesToProjectAndProjectTask`)
ya se generó y se verificó de punta a punta contra LocalDB real en Sprint
4 (Validación), incluida la consulta directa a `sys.tables` confirmando
`temporal_type_desc = SYSTEM_VERSIONED_TEMPORAL_TABLE` para ambas tablas.
No queda ningún DDL nuevo por escribir en este Sprint.

## El hueco real que sí encontró este Sprint

`docs/06-base-de-datos.md` (el diagrama ER y la referencia de base de
datos consolidada) **nunca se actualizó** cuando Sprint 4 activó Temporal
Tables — quedó documentado en `r4-04-validacion.md` (la bitácora del
Sprint) pero no en la referencia canónica que alguien consultaría para
entender el esquema completo hoy. Mismo tipo de gap que las auditorías de
Sprint 10 de Releases anteriores ya encontraron más de una vez (el índice
de `Companies` faltante en Release 2, por ejemplo) — encontrado esta vez
en Sprint 6 en vez de esperar a la auditoría de Sprint 10, porque
"actualizar la referencia de base de datos" es exactamente el trabajo que
le corresponde a este Sprint específico.

**Corregido**: nueva sección "Historial de cambios (Temporal Tables) —
Release 4" en `docs/06-base-de-datos.md`, explicando qué tablas son
temporales, por qué solo esas dos (no las 24 restantes), y cómo se
verificó. El diagrama ER en sí (`erDiagram` de Mermaid) no gana columnas
`PeriodStart`/`PeriodEnd` — es un patrón ya establecido: ese diagrama
tampoco muestra `CreatedAtUtc`/`CreatedBy`/etc. para ninguna entidad
(confirmado revisando el bloque de `Projects` y el resto), así que
agregarlas solo para Temporal Tables habría sido inconsistente con cómo
ya se documentan los demás campos de auditoría — quedan en la sección de
prosa, igual que el resto.

## Verificación

- Re-confirmado (no solo asumido de Sprint 4): `sys.tables` contra
  LocalDB real sigue mostrando `Projects`/`ProjectTasks` como
  `SYSTEM_VERSIONED_TEMPORAL_TABLE`, con `ProjectsHistory`/
  `ProjectTasksHistory` como tablas de historial.
- `dotnet build`/`dotnet test EnterpriseFlow.slnx` — sin cambios de
  código en este Sprint (solo `docs/06-base-de-datos.md`), pero corridos
  de todos modos para confirmar que el estado del repo sigue intacto:
  **281/281 tests**. `dotnet format --verify-no-changes` limpio.

## Qué no se hizo en este sprint (a propósito)

- Ninguna migración nueva — no hay DDL pendiente para Release 4.
- Ninguna política de retención/purga del historial — ya señalada como
  fuera de alcance en ADR-0015 ("sin política de retención/purga en este
  Release — ninguna HU la pide todavía").
