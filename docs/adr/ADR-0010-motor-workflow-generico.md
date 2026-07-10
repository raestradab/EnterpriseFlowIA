# ADR-0010: Motor de Workflow como máquina de estados genérica orientada a datos

- Estado: Aceptado
- Fecha: 2026-07-08
- Relacionado: ADR-0005 (mismo patrón de "hechos inyectados" para invariantes
  cross-aggregate), HU-080/HU-081

## Contexto

F8.1 pide explícitamente un motor de Workflow **configurable por el tenant
sin cambio de código** — a diferencia de `ProjectStatus`/`ProjectTaskStatus`
(Release 1), que son enums de C# con transiciones fijas en el código
(`Project.Close()`, `ProjectTask.Complete()`/`Cancel()`), un enum no puede
satisfacer este requisito: agregar o modificar un estado/transición con un
enum exige recompilar y desplegar. El primer (y único, en Release 2)
consumidor real es la aprobación de Documentos (HU-081): Borrador → En
Revisión → Aprobado/Rechazado.

## Decisión

Tres entidades nuevas en `Domain` (agregado propio, `WorkflowDefinition` como
raíz):

- **`WorkflowDefinition`** (`Id`, `Name`, `TenantId`) — p. ej. "Aprobación de
  Documentos".
- **`WorkflowState`** (`Id`, `WorkflowDefinitionId`, `Name`, `IsInitial`,
  `IsFinal`) — p. ej. "Borrador" (`IsInitial=true`), "Aprobado"/"Rechazado"
  (`IsFinal=true`).
- **`WorkflowTransition`** (`Id`, `WorkflowDefinitionId`, `FromStateId`,
  `ToStateId`, `Name`) — p. ej. "Enviar a revisión" (Borrador→En Revisión).

Una entidad participante (`Document`) referencia su estado actual con
`CurrentWorkflowStateId` (misma referencia cross-aggregate sin FK física que
ADR-0009/ADR-0005 ya establecieron). El intento de transición
(`DocumentTransitionCommandHandler`) resuelve si existe una
`WorkflowTransition` que conecte el estado actual con el estado destino
solicitado — un hecho consultado en `Application` — y lo pasa como parámetro
a `Document.TransitionTo(targetStateId, bool transitionIsAllowed)`, que
lanza si `transitionIsAllowed` es `false`. Es literalmente el mismo patrón
que `Project.Close(bool hasOpenTasks)` (ADR-0005): el agregado decide sobre
un hecho ya resuelto, nunca consulta otro agregado directamente.

## Alternativas consideradas

- **Enum `DocumentStatus` fijo + métodos `SendToReview()`/`Approve()`/
  `Reject()` como Release 1 hizo con `ProjectTaskStatus`**: rechazada — es
  exactamente lo que F8.1 pide evitar (transiciones fijas en código). Se
  habría cumplido HU-081 (aprobación de documentos) pero no HU-080 (motor
  configurable) — la propia razón de que F8.1 exista como Feature separada
  de F5 en `epics.md`.
- **Motor de Workflow de terceros (p. ej. Elsa Workflows)**: rechazada para
  Release 2 — resuelve un problema mucho más amplio (workflows de larga
  duración, actividades compuestas, persistencia de estado intermedio) que
  el caso de uso real (una máquina de estados simple con un único
  consumidor). Adoptar una librería completa para usar una fracción mínima
  de su superficie contradice la misma disciplina de YAGNI que ADR-0001/
  ADR-0008 ya aplicaron a Redis/Hangfire — se revisita si un Release futuro
  necesita workflows genuinamente más complejos (aprobaciones paralelas,
  timers, compensación).
- **Guardar las transiciones válidas como JSON dentro de una sola fila de
  `WorkflowDefinition`** (en vez de una tabla `WorkflowTransition` normalizada):
  rechazada — consultar "¿existe una transición de X a Y?" con SQL sobre una
  tabla normalizada es una query directa e indexable; con JSON requeriría
  deserializar y filtrar en memoria en cada intento de transición, o
  funciones JSON específicas de SQL Server menos portables y más difíciles
  de razonar que un `WHERE FromStateId = @x AND ToStateId = @y`.

## Consecuencias

- Positivo: un Release futuro que necesite workflow para otra entidad
  (p. ej. aprobación de Proyectos) solo necesita una nueva fila
  `WorkflowDefinition` con sus estados/transiciones — cero cambios de
  código en el motor mismo, solo en el `CommandHandler` de la entidad
  participante (que ya tenía que existir de todas formas).
- Positivo: consistente con el patrón ya establecido en ADR-0005 — no se
  introduce un segundo estilo de "invariante cross-aggregate" para este caso,
  se reutiliza el que ya existe y ya está probado.
- Negativo: dos niveles de indirección para entender "por qué esta
  transición fue rechazada" (hay que mirar los datos de `WorkflowTransition`,
  no solo el código) — trade-off inherente a cualquier motor orientado a
  datos frente a uno hardcodeado; mitigado con un mensaje de error que
  incluye el nombre del estado actual y el destino solicitado, no solo un
  booleano.
- Seguimiento: si Release 2 termina con un solo consumidor real (Documentos)
  y ningún caso de uso adicional aparece en Release 3/4, se revisita si el
  motor genérico se justificó frente a la alternativa más simple — la
  respuesta ya está anticipada en `r2-01-vision-y-alcance.md` (sección 3):
  la genericidad se pagó una vez, cualquier consumidor adicional es gratis.
