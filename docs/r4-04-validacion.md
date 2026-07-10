# Release 4, Sprint 4 — Validación de arquitectura

Mismo criterio que Sprint 4 de Releases anteriores: la primera prueba
real, de punta a punta, del mecanismo que Sprint 3 dejó como decisión de
arquitectura (ADR-0015) — no revisión de código, ejecución real contra
LocalDB.

## Un hallazgo real de arquitectura, encontrado al escribir el primer consumidor

El primer intento de usar `TemporalAsOf` directamente desde el Handler
(`GetProjectHistoryQueryHandler`, sobre `db.Projects` vía `IAppDbContext`)
no compiló: `TemporalAsOf` es una extensión de
`Microsoft.EntityFrameworkCore.SqlServer`, un paquete que **Application
deliberadamente no referencia** (ADR-0002 — Application solo depende del
paquete `Microsoft.EntityFrameworkCore` agnóstico de proveedor;
`EnterpriseFlow.Architecture.Tests` lo refuerza como regla, no como
convención). Exactamente el mismo tipo de límite que ya llevó a
`IDocumentStorageProvider`/`IAiChatClient` a existir como interfaces.

**Corrección**: `IAppDbContext` gana un método nuevo,
`IQueryable<Project> GetProjectsAsOf(DateTimeOffset asOf)` — Application
declara *qué* necesita (Proyectos como estaban en un momento dado);
`AppDbContext` (Infrastructure) decide *cómo* (`Projects.TemporalAsOf(...)`),
que es el único lugar donde el paquete específico de SQL Server puede
usarse. El Handler arma su propio DTO con `.Select()` sobre ese
`IQueryable`, exactamente igual que ya hace con `db.Projects` en
cualquier otra Query — mismo patrón, sin una abstracción nueva que
inventar.

## Qué se agregó

- `IAppDbContext.GetProjectsAsOf(DateTimeOffset asOf)` + su implementación
  en `AppDbContext`.
- `GetProjectHistoryQuery`/Handler (`Features/Projects/GetProjectHistory`)
  — mismo permiso que leer el Proyecto actual (`projects.read`): ver su
  historial no es un privilegio menor que verlo hoy.
- `GET /api/projects/{id}/history?asOf=<ISO-8601 UTC>` — `404` si el
  Proyecto no existía (o no es visible para el tenant que pregunta) en
  ese momento, `200` con el estado real en caso contrario.

## Verificación

**Contra LocalDB real, con datos reales — no simulado, y no cubierto por
la suite automatizada (ver la sección siguiente sobre por qué):**

1. Se registró un tenant real, se creó un Cliente y un Proyecto real vía
   la Api (estado inicial: `status=0`, Planeado).
2. Se capturó el timestamp UTC exacto de ese momento.
3. Se cerró el Proyecto vía `POST /api/projects/{id}/close` (`status=3`,
   Cerrado, confirmado con una lectura normal después).
4. `GET .../history?asOf=<timestamp del paso 2>` → **`status=0`** — el
   valor real que tenía en ese momento, no el actual.
5. `GET .../history?asOf=<ahora>` → **`status=3`** — el valor actual.
6. **Aislamiento de tenant sobre consultas temporales**: un usuario de un
   tenant *distinto* pidiendo el historial del mismo `Id` de Proyecto
   recibe `404` — confirma que el filtro global de tenant (ADR-0003)
   sigue aplicando sobre `TemporalAsOf`, no es una ruta paralela sin
   aislamiento (la suposición que el propio comentario del Handler
   señalaba como "verificar, no asumir").
7. `asOf` anterior a la creación del Proyecto → `404` con gracia, no un
   error.

## Por qué esto no tiene cobertura en la suite automatizada de xUnit

`Api.IntegrationTests` corre contra SQLite (ADR: real traducción de SQL,
más rápido que un contenedor real) — pero **SQLite no tiene Temporal
Tables**; es una capacidad exclusiva de SQL Server sin equivalente. Esto
significa que, a diferencia de cada feature anterior de este proyecto,
esta específicamente **no puede** probarse con el mismo mecanismo que
todas las demás. Se evaluaron las alternativas:

- **Agregar SQL Server como contenedor de servicio en GitHub Actions
  CI** (`ci.yml`, corre en `ubuntu-latest`, sin SQL Server disponible por
  defecto): técnicamente posible, pero es una decisión de infraestructura
  de CI, no de esta Query puntual — se evalúa en Sprint 11 (DevOps) de
  este Release, donde corresponde, no aquí.
- **Un proyecto de test nuevo que corra contra LocalDB real, solo en este
  entorno de desarrollo**: rechazado por ahora — sin la pieza de CI de
  arriba, esas pruebas solo se ejecutarían localmente, dando una falsa
  sensación de cobertura continua que en realidad no corre en cada push.

**Decisión para este Sprint**: la verificación manual de arriba, real y
exhaustiva (siete escenarios reales, incluido el de aislamiento de
tenant), documentada explícitamente en vez de una prueba automatizada que
no existe todavía — mismo principio de transparencia que el proyecto
aplicó a Redis/SMTP/proveedores de IA: decir la verdad sobre qué se
verificó y cómo, no simular cobertura que no hay.

Suite automatizada sin cambios de resultado: **281/281 tests**
(`GetProjectHistoryQueryHandler` en sí no tiene una prueba de integración
directa por la razón de arriba, pero el resto del sistema se confirmó
intacto). `dotnet build`/`dotnet format --verify-no-changes` limpios.

## Qué no se hizo en este sprint (a propósito)

- Ningún endpoint de historial para `ProjectTask` todavía — HU-102 (y esta
  validación) se acotó a `Project`, la entidad que efectivamente se probó
  de punta a punta; `ProjectTask` ya tiene la misma capacidad activada a
  nivel de base de datos (Sprint 4 configuró ambas tablas), exponerla es
  una Query más del mismo patrón, no una decisión nueva.
- Ninguna UI para navegar el historial — ya descartada explícitamente en
  `r4-01-vision-y-alcance.md`, sección 3.
- La decisión de si SQL Server entra a CI (`ci.yml`) — Sprint 11.
