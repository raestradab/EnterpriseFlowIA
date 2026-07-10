# Release 3, Sprint 7c — Backend: más herramientas del asistente + HU-093

Última de tres sub-partes de Sprint 7 (Backend) de Release 3 — cierra
Sprint 7 en su totalidad. Cubre lo único que quedaba pendiente del
backlog de este Release: la parte de HU-092 sobre tareas atrasadas (el
Gherkin de esa historia la menciona explícitamente) y HU-093 (resúmenes
ejecutivos).

## Qué se agregó

- **`GetMyOverdueTasksQuery`/Handler** (`Features/ProjectTasks/GetMyOverdueTasks`) —
  tareas asignadas al usuario actual, con `DueDate` en el pasado y
  `Status` distinto de `Completed`/`Cancelled`. Sin `IRequirePermission`
  (mismo criterio que `GetMyCalendarQuery`: siempre son datos propios).
  **Deliberadamente no reutiliza `GetMyCalendarQuery`** — esa Query no
  filtra por `Status`, así que una tarea ya completada con vencimiento
  pasado aparecería como "atrasada" si simplemente se le pidiera un rango
  amplio. HU-092 exige que "atrasada" sea un hecho resuelto por una Query
  real, no algo que el modelo calcule mirando una lista cruda — construir
  una Query nueva, precisa, es lo que esa regla pide.
- **Herramienta nueva**: `get_my_overdue_tasks` en `AssistantToolCatalog`,
  con su caso correspondiente en
  `SendAssistantMessageCommandHandler.InvokeToolAsync`.
- **Ningún código nuevo para HU-093** — los resúmenes ejecutivos no son un
  mecanismo aparte: son el modelo sintetizando en lenguaje natural el
  resultado de las mismas herramientas que HU-092 ya ancla (p. ej.
  `get_my_projects`). El sistema ya soportaba esto desde Sprint 4; este
  Sprint solo lo **verifica** con una prueba real, no agrega una ruta de
  código distinta para "resumir" — hacerlo habría sido una segunda forma
  de llegar al mismo dato, contradiciendo la regla central de ADR-0013.

## Verificación

- **3 tests de integración nuevos** (`AssistantOverdueTasksTests`), con
  el flujo real de registro/login (no un token fabricado directamente) —
  `AssignTask` valida que el usuario asignado exista de verdad, mismo
  motivo que ya obligó a ese flujo en `ProjectTasksEndpointsTests`
  (Release 2, Sprint 9):
  - Preguntarle al asistente "¿cuántas tareas tengo atrasadas?" responde
    con el título real de una tarea vencida y sin completar.
  - Una tarea completada con fecha de vencimiento pasada **no** aparece
    como atrasada — prueba directa de que el filtro de `Status` (la razón
    por la que no se reutilizó `GetMyCalendarQuery`) funciona de verdad.
  - HU-093: pedirle al asistente "resumime el estado de mis proyectos
    activos" responde con el nombre real de un Proyecto sembrado en la
    base — no un texto genérico — confirmando que el mecanismo existente
    ya cumple la historia sin necesitar código nuevo.
- **Un error real en la propia escritura del test, no en el producto**: el
  primer intento de estos tests devolvía siempre cero tareas atrasadas
  aunque la tarea sí estaba vencida — no porque `GetMyOverdueTasksQuery`
  estuviera mal, sino porque el test nunca agregó al usuario como miembro
  del Proyecto antes de asignarle la tarea, un prerrequisito que
  `AssignTaskCommandHandler` ya exigía desde Release 1 y que el test
  reproductor (`Calendar_Returns_Only_The_Caller_Own_Assigned_Tasks_In_Range`,
  Release 2 Sprint 9) sí incluía — se copió el flujo de registro/login sin
  copiar ese paso. Corregido agregando `POST /api/projects/{id}/members`
  antes de asignar, y verificando el código de estado de cada llamada de
  `assign`/`complete` (`EnsureSuccessStatusCode()`), que antes se ignoraba
  silenciosamente.
- Suite completa: **276/276 tests** (141+32+20+6+77).
  `dotnet format --verify-no-changes` limpio.

## Cierre de Sprint 7 (Backend) — resumen de las tres sub-partes

| Sub-parte | Qué construyó |
|---|---|
| 7a | `OpenAiChatClient`/`AnthropicChatClient`/`OpenAiEmbeddingClient` reales (ADR-0013) |
| 7b | Pipeline completo de RAG: extracción de texto, chunking, embeddings, indexación síncrona, `search_my_documents` (F10) |
| 7c | `get_my_overdue_tasks` (cierra HU-092) + verificación de HU-093 (resúmenes, sin código nuevo) |

Con esto, las 12 Historias de Usuario de Release 3 (HU-090 a HU-101) tienen
backend real y probado. Ninguna herramienta más queda pendiente del
backlog actual — el catálogo del asistente tiene 3 entradas:
`get_my_projects`, `search_my_documents`, `get_my_overdue_tasks`.

## Qué no se hizo en este sprint (a propósito)

- Streaming de la respuesta — diferido desde Sprint 1.
- Endpoints de Api para el frontend (chat UI) — Sprint de Frontend.
- Cualquier herramienta fuera de las 12 HU de este Release (p. ej.
  "modificar una Tarea desde el chat") — ADR-0013 restringe el tool-use a
  Queries de solo lectura; ninguna HU pide mutaciones vía lenguaje natural.
