# Release 4, Sprint 10 — Documentación

Auditoría de `docs/` contra el estado real del código, mismo criterio que
`10-documentacion.md`/`r2-10-documentacion.md`/`r3-10-documentacion.md`
aplicaron al cierre de cada Release anterior — pero esta vez, motivada por
`especificcion.md` (el spec maestro) volviendo a quedar a la vista, la
auditoría se extendió más atrás: no solo "¿los docs de Release 4 están al
día?" sino "¿todo lo que el documento original pidió sigue teniendo un
destino documentado, y lo que se dijo construido está construido de
verdad?".

## 1. Auditoría línea por línea de `especificcion.md`

`docs/backlog/epics.md` ya afirmaba trazabilidad completa contra el spec
maestro. Verificado, no solo confiado: recorridas las 15 secciones del
documento contra `epics.md` — la afirmación se sostiene, con tres
excepciones reales encontradas al cruzar contra el código (no contra el
backlog, que ya las tenía bien clasificadas donde correspondía verlas):

### 1.1 Response Compression — documentado como activo, nunca lo estuvo

[ADR-0008](./adr/ADR-0008-activacion-redis-hangfire-response-compression.md)
(Release 2) documenta a Response Compression como activado globalmente. Un
`grep` de `UseResponseCompression`/`AddResponseCompression` en todo
`src/EnterpriseFlow.Api` no encontró nada — cero coincidencias reales, solo
ruido de `System.IO.Compression` (una librería sin relación, arrastrada por
otro paquete NuGet).

**Corregido en este mismo Sprint** (no solo anotado como gap, siguiendo el
mismo criterio que Sprints 6/8/9 ya aplicaron): agregado
`AddResponseCompression`/`UseResponseCompression` a `Program.cs`, con
Brotli y Gzip, `EnableForHttps = true` (justificado inline: riesgo BREACH
descartado porque esta Api nunca refleja un secreto dentro de un cuerpo de
respuesta que también contenga entrada controlada por el atacante).

**Segundo hallazgo, encontrado solo al verificar contra una respuesta
real**: la primera ubicación (justo después de `SecurityHeadersMiddleware`,
antes de `UseExceptionHandler`) no comprimía nada — `curl -H
"Accept-Encoding: gzip, br" http://localhost:5050/swagger/v1/swagger.json`
devolvía el JSON sin `Content-Encoding`. Investigado: `UseSwagger()` se
registra *antes* en el pipeline y es middleware terminal (no llama a
`next()` para su propia ruta), así que todo lo registrado después de él
simplemente nunca se ejecuta para esa respuesta. Reordenado al principio
absoluto del pipeline (antes del bloque de Swagger). Re-verificado contra
una instancia real corriendo: `/swagger/v1/swagger.json` pasó de 35.167
bytes sin comprimir a 6.159 bytes con Brotli — una reducción real del 82%,
no solo la cabecera presente. Suite completa (281/281) corrida de nuevo
después del cambio, sin regresiones — incluyendo `Api.IntegrationTests`,
que ejercita las mismas rutas de notificaciones/SignalR que podrían haberse
visto afectadas por un middleware transversal nuevo.

### 1.2 ADR-0001, punto 6 (Mapster vs. AutoMapper) — nunca resuelto, hasta ahora

[ADR-0001](./adr/ADR-0001-alcance-y-estrategia-de-entrega.md) comprometía
explícitamente un benchmark con BenchmarkDotNet en el Sprint de Backend del
MVP para decidir entre Mapster y AutoMapper. Ninguno de los dos aparece en
ningún `.csproj` ni en ningún `using` de `src/`. No era un olvido puntual:
el propio ADR incluye una cláusula de seguimiento ("se revisa al cierre de
cada Release") que tampoco se había ejercido sobre este punto específico en
tres Releases.

Investigado el motivo real (leyendo Query Handlers, no solo buscando el
paquete): cada Query proyecta directo de `IQueryable<Entity>` a un DTO
record vía `.Select(e => new Dto(...))` (p. ej.
`GetProjectByIdQueryHandler`), que EF Core traduce a un `SELECT` de columnas
específicas — nunca existe el paso de "entidad completa materializada →
copiar campo a campo" que AutoMapper/Mapster resuelven. En escritura, los
Command Handlers construyen agregados vía sus factory methods
(`Project.Create(...)`), nunca por reflexión genérica — intencional, un
mapper no puede conocer las invariantes de dominio que un factory method sí
aplica. Comparar rendimiento entre ambas librerías habría sido optimizar un
paso que el propio diseño CQRS/Vertical Slice ya elimina.

**Corregido documentando la resolución real**, no ejecutando el benchmark
tardíamente sobre un problema que no existe: ADR-0001 punto 6 y su cláusula
de seguimiento actualizados con el hallazgo.

### 1.3 Virtual Scrolling — nunca construido, nunca disclosed

`especificcion.md` (FRONTEND) lo pide explícitamente; ningún documento de
ningún Release lo había mencionado ni como construido ni como diferido —
la única omisión real y silenciosa que la auditoría encontró (a diferencia
de los dos hallazgos anteriores, que sí habían sido documentados, solo que
incorrectamente).

Investigado: todos los listados del frontend (Empresas, Clientes,
Proyectos, Tareas, Catálogos, Workflows) usan `VDataTable` de Vuetify, que
pagina — nunca carga miles de filas al DOM a la vez, que es el problema que
Virtual Scrolling resuelve. No hay ningún listado tipo *feed* sin paginar
en todo el frontend. Documentado en `epics.md` (sección de trazabilidad)
como "no necesario, no como olvido" — mismo criterio YAGNI que ADR-0001
punto 4 ya aplicó al resto de infraestructura diferida; se reconsidera si
algún listado futuro necesita cargar sin paginar.

### 1.4 README, párrafo de apertura — reclamaba el servidor MCP como construido

Encontrado actualizando el propio README en este Sprint, no en la auditoría
inicial: la primera frase del repo ("...con un asistente de IA embebido
(RAG + servidor MCP propio)") describía el MCP como parte del producto
construido, contradiciendo lo que el resto del mismo README y `epics.md`
(E11) ya decían correctamente — diferido sin Release asignado desde
Release 3. La única mención inexacta estaba, con diferencia, en el lugar
más leído de todo el repositorio (la primera línea que ve cualquier
reclutador o arquitecto — `especificcion.md`, OBJETIVO FINAL). Corregida
para reflejar el estado real, con enlace a `epics.md` en vez de una
afirmación tácita.

## 2. `CHANGELOG.md` (F14.4)

`especificcion.md` (DOCUMENTACIÓN) y `epics.md` (E14, "Documentación
Automática... continua, entregable en cada Release") piden un CHANGELOG
desde Release 1. Auditado: nunca existió — ni en la raíz del repo ni en
`docs/`. Gap real, no una decisión de alcance de ningún Release anterior
(a diferencia de los ítems con blockquote de "diferido sin Release
asignado" en `epics.md`, que sí tienen razón documentada; este simplemente
nunca se mencionó).

Creado `CHANGELOG.md` en la raíz, formato inspirado en Keep a Changelog,
reconstruido a partir del historial real ya documentado (README + docs de
cada Sprint) — no inventado, cada entrada enlaza al documento de Sprint
donde se verificó. Organizado por Release (no por versión semántica
todavía — eso es F13.5, Sprint 11).

## 3. Auditoría de diagramas y ADR index

- `docs/adr/README.md`: los 16 ADRs (0001-0016) están presentes, con
  descripciones que siguen coincidiendo con el contenido real de cada uno
  — sin gaps.
- `docs/06-base-de-datos.md`: la sección de Temporal Tables (agregada en
  Sprint 6) ya documentaba correctamente tanto `Projects` como
  `ProjectTasks` desde antes de que Sprint 7 completara el endpoint de
  historial de Tareas — la migración de Sprint 4 cubrió ambas tablas de
  una vez, aunque el endpoint se completara después. Sin gap.
- `docs/03-diseno-arquitectura/`: Sprint 2 ya había razonado explícitamente
  por qué Temporal Tables/OpenTelemetry/BenchmarkDotNet no necesitan nodos
  C4 nuevos (viven dentro de contenedores ya modelados, o corren fuera del
  sistema en ejecución que un diagrama C4 describe) — el mismo
  razonamiento cubre a `benchmarks/EnterpriseFlow.Benchmarks/` y
  `tests/EnterpriseFlow.Infrastructure.SqlServerTests/` (Sprints 7 y 9),
  ninguno de los dos es parte del sistema en ejecución. Sin gap.

## Qué no se hizo en este Sprint (a propósito)

- No se re-auditaron ítems ya verificados a fondo en Sprints anteriores de
  este mismo Release (Temporal Tables, OpenTelemetry, cobertura) — la
  auditoría de este Sprint se enfocó en lo que ningún Sprint anterior
  había mirado todavía: la especificación completa y el propio ADR-0001.
- No se implementó Conventional Commits/Semantic Versioning (F13.5) — es
  DevOps (Sprint 11), no Documentación.
