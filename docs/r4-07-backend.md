# Release 4, Sprint 7 — Backend

A diferencia de Sprint 5/6 (confirmación), este Sprint sí tiene trabajo
real: completar HU-102 para la segunda entidad que la propia historia
nombra, y construir el proyecto de benchmarks que F12.4 pide.

## Completar HU-102: historial de `ProjectTask`

Sprint 4 (Validación) solo construyó y probó `Project` — la HU en sí
("Como administrador de tenant, quiero poder consultar el estado
completo de un **Proyecto o una Tarea**...") nombra las dos entidades.
Este Sprint agrega la segunda mitad, mismo patrón exacto que la primera:

- `IAppDbContext.GetProjectTasksAsOf(DateTimeOffset asOf)` +
  implementación en `AppDbContext` (mismo *seam* que `GetProjectsAsOf`,
  Sprint 4 — `TemporalAsOf` sigue siendo específico de
  `Microsoft.EntityFrameworkCore.SqlServer`, que Application no puede
  referenciar).
- `GetTaskHistoryQuery`/Handler (`Features/ProjectTasks/GetTaskHistory`).
- `GET /api/tasks/{id}/history?asOf=...` — mismo permiso que leer la
  Tarea actual (`tasks.read`).

## Proyecto de benchmarks (F12.4)

`benchmarks/EnterpriseFlow.Benchmarks/` — proyecto de consola nuevo,
agregado a la solución en su propia carpeta (mismo nivel que `src/`/
`tests/`), con BenchmarkDotNet 0.15.8. Dos benchmarks reales, elegidos
por ser los únicos cálculos de costo no trivial en un camino caliente
real del sistema (no una lista arbitraria de "cosas que se podrían
medir"):

- **`CosineSimilarityBenchmarks`**: `CosineSimilarity.Compute` (ADR-0014)
  sobre dos vectores de 1536 dimensiones — la dimensión real que
  `text-embedding-3-small` (`OpenAiEmbeddingClient`, Release 3) produce,
  no un número redondo elegido al azar. Corre una vez por chunk de todo
  el corpus de un tenant, en cada pregunta que el asistente resuelve vía
  `search_my_documents`.
- **`TextChunkerBenchmarks`**: `TextChunker.Split` (Sprint 7b, Release 3)
  sobre un documento de 25.000 caracteres (~10 páginas de texto extraído
  de un PDF/Word real). A diferencia de la similitud de coseno, este
  corre **dentro** del request de subida de un Documento (Sprint 7b:
  indexación síncrona) — su costo se suma directamente a cuánto espera el
  usuario, no es un cálculo fuera del camino crítico de ninguna escritura.

## Verificación

**Corridas reales, no solo compiladas:**

- `GET /api/tasks/{id}/history` verificado contra LocalDB real, mismo
  método que Sprint 4 usó para `Project`: se creó una Tarea real
  (`status=0`, Por hacer), se canceló (`status=3`, Cancelada), y
  `history?asOf=<antes de cancelar>` devolvió `status=0` mientras
  `asOf=<después>` devolvió `status=3`.
  - **Un hallazgo real de metodología de prueba, no del código**: el
    primer intento devolvió `404` inesperadamente en el caso "antes" —
    investigado con `SELECT ... FOR SYSTEM_TIME ALL` directo contra
    LocalDB, se confirmó que el `PeriodStart` real de la fila tenía
    milisegundos posteriores al timestamp que el script de verificación
    había capturado con precisión de un segundo entero
    (`date -u +"%Y-%m-%dT%H:%M:%S.000Z"`) — el timestamp pasado a
    `asOf` caía *antes* de que la fila existiera, así que el `404` era
    la respuesta correcta dado ese dato de entrada, no un bug. Un
    segundo intento con más margen entre pasos confirmó el
    comportamiento esperado. Un intento posterior también reveló un
    segundo error de script (una variable de shell no persistida entre
    invocaciones de Bash, causando un `projectId` vacío) — ninguno de
    los dos afectó código de producción, ambos quedan documentados aquí
    por la misma disciplina de transparencia que el proyecto aplica a
    los hallazgos reales de código.
- **Benchmarks corridos de verdad** (`dotnet run -c Release -- --filter "*" --job short`,
  no solo compilados): `ComputeSimilarity` → **1.570 μs de media, sin
  asignaciones de memoria**; `SplitDocument` → **7.280 μs de media,
  ~55 KB asignados por llamada**. Números reales de esta máquina, no
  estimados — quedan en `BenchmarkDotNet.Artifacts/` (gitignored, se
  regeneran en cada corrida).
- `dotnet build`/`dotnet test EnterpriseFlow.slnx` — **281/281** — y
  `dotnet format --verify-no-changes` limpios. El proyecto de benchmarks
  compila limpio en Debug y Release, con el mismo nivel de
  StyleCop/`TreatWarningsAsErrors` que el resto de `src/` (encontró y
  corrigió un `SA1407` real: precedencia aritmética explícita).

## Qué no se hizo en este sprint (a propósito)

- Ningún span manual de OpenTelemetry sobre el loop de tool-use del
  asistente — sin un caso concreto de "no podemos ver X" que lo
  justifique todavía (ADR-0016 lo dejó como "si se justifica", no como
  compromiso).
- CodeQL, Dependabot, Semantic Versioning — Sprint 11 (DevOps), donde
  corresponden.
