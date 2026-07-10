# Release 4, Sprint 9 — Pruebas

`r4-01-vision-y-alcance.md` (sección 2, F12.3) da por alcanzada la
cobertura ≥90% desde Release 3 Sprint 9 (94.9%) y le asigna a este
Sprint dos cosas distintas: auditar cómo se mantiene ese número hacia
adelante (un gate real en CI, no solo una foto puntual), y decidir qué
hacer con el único gap de cobertura ya conocido — Temporal Tables
(HU-102) sin ninguna prueba automatizada, porque SQLite (el motor de
`Api.IntegrationTests`) no las soporta.

## 1. Corrida completa + auditoría de cobertura

`dotnet test EnterpriseFlow.slnx --settings coverlet.runsettings
--collect:"XPlat Code Coverage"` + `reportgenerator`, sobre el estado
del repo tal cual llegó a este Sprint (281 tests, Sprints 1-8 de
Release 4 ya cerrados): **94.1% de líneas** (94.9% en Release 3 Sprint
9) — la diferencia es exactamente las 6 clases de HU-102
(`GetProjectHistoryQuery`/`Handler`/`Dto`,
`GetTaskHistoryQuery`/`Handler`/`Dto`) en 0%, el gap que la sección 2
de este documento cierra. Nada más retrocedió: por ensamblado,
`Application` 98%, `Domain` 94.3%, `Api` 92.3%, `Infrastructure` 89.2%
— todos por encima del ≥90% agregado exigido, igual que en Release 3.

Los demás huecos en 0% del reporte (`HangfireEmailQueue`,
`NullEmailQueue`, `SignalRNotifier`, `JwtSubUserIdProvider`,
`DocumentsOptions`) no son nuevos — ya estaban explícitamente
disclosed y justificados en `r2-09-pruebas.md` ("Explícitamente no
perseguido hasta el 100%") desde Release 2; auditados de nuevo aquí,
siguen justificados de la misma forma, así que no se reabren.

## 2. Cerrando el gap real de HU-102 (no se difiere de nuevo)

Los Sprints 4 y 7 dejaron esto anotado como una decisión explícita
("SQL Server como servicio de CI, evaluado para Sprint 11"), no como
un descuido — pero un Sprint específicamente de Pruebas es el lugar
correcto para resolverlo de verdad en vez de volver a diferirlo una
tercera vez.

Proyecto nuevo: `tests/EnterpriseFlow.Infrastructure.SqlServerTests/`.
No podía ser `Infrastructure.UnitTests` (esa suite es explícitamente
para lógica pura sin base de datos, CLAUDE.md) ni `Api.IntegrationTests`
(SQLite, sin Temporal Tables) — necesita un motor SQL Server real.
Corre contra una LocalDB real
(`Server=(localdb)\MSSQLLocalDB;Database=EnterpriseFlow_SqlServerTests`,
una base propia, nunca la que un desarrollador pueda estar usando a
mano vía la Api en ejecución), migrada de verdad
(`Database.MigrateAsync()`, mismo criterio que la verificación de
`r3-11-devops.md`) en un fixture de colección xUnit que migra una sola
vez para toda la suite.

Cuatro pruebas — dos por entidad (`Project`/`ProjectTask`), replicando
exactamente los tres escenarios que Sprints 4 y 7 ya habían verificado
a mano contra LocalDB:

- El historial refleja el estado correcto en el punto exacto en el
  tiempo pedido (estado antes de un cambio, estado después, y `null`
  para un punto anterior a que la fila existiera siquiera) —
  ejercitando `GetProjectHistoryQueryHandler`/`GetTaskHistoryQueryHandler`
  reales, no solo `AppDbContext.GetProjectsAsOf` en aislamiento.
- El filtro global de tenant (ADR-0003) sigue aplicando incluso bajo
  `TemporalAsOf` — un tenant distinto pidiendo el mismo Id no ve nada.

Para evitar el error de metodología ya documentado en Sprint 7 (un
timestamp capturado con precisión insuficiente cayendo justo en el
borde del período), cada paso usa `DateTimeOffset.UtcNow` capturado
directamente en C# (precisión completa, no una cadena truncada) con
`Task.Delay(50)` de margen entre pasos.

**Corridas de verdad contra LocalDB real, no solo compiladas**:
`dotnet test tests/EnterpriseFlow.Infrastructure.SqlServerTests/...` →
**4/4**. Verificado además con una consulta SQL directa después de la
corrida (no solo confiando en los asserts de C#):

```sql
SELECT name, temporal_type_desc FROM sys.tables WHERE name IN ('Projects','ProjectTasks');
-- Projects     SYSTEM_VERSIONED_TEMPORAL_TABLE
-- ProjectTasks SYSTEM_VERSIONED_TEMPORAL_TABLE
SELECT COUNT(*) FROM ProjectsHistory;     -- 1 (la fila que Close() generó)
SELECT COUNT(*) FROM ProjectTasksHistory; -- 1 (la fila que Cancel() generó)
```

Cobertura completa local (`dotnet test EnterpriseFlow.slnx`,
**285/285**, las 4 pruebas nuevas incluidas): **94.6%** — sube desde el
94.1% de la sección 1, cerrando la mayor parte del gap real (quedan
fracciones sin cubrir en los DTOs, sin señal adicional que perseguir).

## 3. Por qué esta suite no entra al barrido por defecto de CI (todavía)

`EnterpriseFlow.Infrastructure.SqlServerTests` necesita
`(localdb)\MSSQLLocalDB`, que es específico de Windows — el job
`backend` de `ci.yml` corre en `ubuntu-latest`. Incluirla sin más
habría roto tanto CI como la propiedad que el proyecto siempre
mantuvo a propósito: `dotnet test EnterpriseFlow.slnx` corre sin
ninguna dependencia externa (la razón original de elegir SQLite para
`Api.IntegrationTests`, no InMemory).

Corregido con un `[Trait("Category", "RequiresSqlServer")]` en ambas
clases de prueba, y `--filter "Category!=RequiresSqlServer"` agregado
al paso `Test` de `ci.yml` — verificado localmente que el filtro deja
exactamente los 281 tests originales (`No test matches the given
testcase filter` para el proyecto nuevo, código de salida 0, no un
fallo) y que sin el filtro los 285 corren y pasan. Sprint 11 (DevOps)
es donde corresponde agregar un servicio `mssql/server` (contenedor
Linux) a `ci.yml` y quitar el filtro — mismo criterio de límite de
Sprint que ya aplicó Sprint 4/7 al diferir esto la primera vez, ahora
acotado a una sola pieza real y pequeña en vez de todo el gap.

## 4. Gate de cobertura en CI (F12.3, "cómo se mantiene hacia adelante")

Auditado `ci.yml`: el paso `Test` no recolectaba cobertura ni exigía
ningún mínimo — un PR futuro podía bajar por debajo del 90% sin que
nada lo notara. Corregido:

- El paso `Test` ahora recolecta cobertura (`--settings
  coverlet.runsettings --collect:"XPlat Code Coverage"`) además de
  correr los tests.
- Un paso nuevo instala `dotnet-reportgenerator-globaltool` (no hay
  `.config/dotnet-tools.json` en este repo — instalarlo en el propio
  job es más simple que agregar un manifiesto solo para este uso) y
  genera el resumen de texto.
- Un paso final extrae el porcentaje de líneas cubiertas y falla el
  job si cae debajo de 90.

**Verificado de verdad, no solo revisado como YAML**: se simuló
localmente la extracción (`grep -oP` + `awk`) exacta que el workflow
usa, una vez contra el 94.1% real (pasa) y una vez contra un 85% de
prueba fabricado a propósito (falla) — confirmando que el gate
realmente puede fallar un build, no solo que el comando no tira error
de sintaxis.

## Qué no se hizo en este Sprint (a propósito)

- Ningún servicio SQL Server en CI — ver sección 3, Sprint 11.
- No se persiguió el 100% en `Infrastructure.DependencyInjection`
  (ramas condicionales de proveedores nunca configurados en este
  entorno) ni en los huecos ya disclosed desde Release 2 — mismo
  criterio de "sin señal incremental" que ya se aplicó entonces.
