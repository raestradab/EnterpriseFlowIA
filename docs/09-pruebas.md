# Sprint 9 — Pruebas

Alcance de este Sprint según `especificcion.md`: medir la cobertura real de
pruebas y empujarla al ≥90% global que exige la fase "pruebas" del ciclo
(análisis → ... → pruebas → documentación → DevOps). Hasta este punto la
cobertura era implícita — 70 tests habían ido apareciendo sprint a sprint sin
que nadie midiera qué fracción del código real ejercitaban.

## Medición

`dotnet test --collect:"XPlat Code Coverage"` (coverlet.collector, ya
referenciado en los 4 `.csproj` de test desde el scaffold inicial — sin
paquete nuevo) + `dotnet-reportgenerator-globaltool` para el resumen legible.

**Antes de escribir un solo test nuevo**: 44.4% de líneas. Engañosamente bajo
— la mayor parte de ese déficit no era código sin probar, sino código que no
*debería* contarse:

- `EnterpriseFlow.Infrastructure.Persistence.Migrations.*`: código generado
  por `dotnet ef migrations add` (cada columna/índice de cada tabla),
  regenerado, nunca editado a mano. Afirmar cobertura ahí no aporta señal.
- `DesignTimeDbContextFactory`: solo lo ejecuta la CLI de `dotnet ef` en
  tiempo de diseño, nunca en runtime.

Ambos se excluyeron explícitamente — el primero vía `coverlet.runsettings`
(filtro `[EnterpriseFlow.Infrastructure]EnterpriseFlow.Infrastructure.Persistence.Migrations.*`),
el segundo vía `[ExcludeFromCodeCoverage]` directo en la clase (justificación
inline, no un blanket exclude por carpeta ya que es una sola clase). Esto es
higiene de medición, no manipulación del número: código que nunca se ejecuta
en producción no debería influir en si el proyecto "necesita más pruebas".

## Gaps reales encontrados (una vez descontado lo anterior)

El patrón fue consistente: **el lado de escritura de cada módulo de negocio
tenía pruebas, el lado de lectura no**. `CreateCompany`/`CreateClient`/
`CreateProject`/`CreateTask` y sus invariantes cruzados (HU-012, HU-021,
HU-023) estaban probados end-to-end desde Sprint 4/7b — pero `GetCompanies`,
`GetClientById`/`GetClients`, `GetProjectById`/`GetProjects`, `GetTaskById`/
`GetTasks`/`GetMyCalendar` (HU-024) estaban en 0%: construidos, mapeados en
sus `*Endpoints.cs`, nunca invocados por ningún test. Lo mismo con
`RemoveProjectMember`, `CancelTask`, y en Identidad `CreateRole`/
`AssignRoleToUser` (incluidos sus caminos de `NotFoundException`) y el caso
de email duplicado en `RegisterTenant` (`RegistrationFailedException`,
introducida en la revisión de seguridad del 2026-07-07 y nunca probada hasta
ahora).

Un segundo patrón, en Domain: las entidades ya tenían tests, pero solo para
las invariantes de negocio "interesantes" (`GrantPermission` duplicado,
`AddMember` duplicado, etc.) — los *guard clauses* de cada `Create()` (nombre
vacío, IDs `Guid.Empty`) y los métodos de ciclo de vida triviales
(`AssignTenant`, `MarkDeleted`) casi nunca estaban cubiertos, porque las
validaciones de FluentValidation en Application los interceptan primero en
cualquier flujo HTTP real — el guard clause del dominio nunca se alcanza
salvo que un test lo invoque directamente. `Tenant` no tenía ni un solo test
dedicado (`TenantTests.cs` no existía).

## Qué se agregó

- **`tests/EnterpriseFlow.Api.IntegrationTests`**: `ClientsEndpointsTests.cs`
  y `ProjectsEndpointsTests.cs` y `ProjectTasksEndpointsTests.cs` (nuevos —
  Get-by-id encontrado/no-encontrado, listados con aislamiento por tenant,
  `RemoveProjectMember`, `CancelTask`, calendario HU-024 con filtrado por
  rango de fechas y por usuario asignado). `CompaniesEndpointsTests.cs`
  ganó el listado. `IdentityEndpointsTests.cs` ganó `CreateRole`+
  `AssignRoleToUser` (éxito y ambos caminos de `NotFound`) y el registro con
  email duplicado — este último con una aserción explícita de que el body de
  error *no* confirma qué campo existía (guarda contra que la corrección de
  enumeración de usuarios de la revisión de seguridad regrese sin que ningún
  test lo note).
- **`tests/EnterpriseFlow.Domain.UnitTests`**: `TenantTests.cs` nuevo;
  `RoleTests.cs`/`UserTests.cs`/`ProjectTests.cs`/`ProjectTaskTests.cs`/
  `RefreshTokenTests.cs` ganaron los guard clauses de `Create()` y los
  métodos de ciclo de vida (`AssignTenant`, `MarkDeleted`, `Start`) que
  faltaban.

## Resultado

| | Antes | Después |
|---|---|---|
| Tests totales | 70 | 118 |
| Cobertura de líneas (global, migraciones excluidas) | 44.4%* | **95.8%** |
| Cobertura de métodos | 71.8% | 94.3% |
| Cobertura de branches | 74.3% | 85.5% |

\* El 44.4% inicial mezclaba migraciones sin excluir; no es comparable
directamente con el 95.8% final — el número relevante es que, tras excluir
lo no-testeable, todos los gaps reales encontrados quedaron cerrados.

Por ensamblado: `EnterpriseFlow.Application` 98.5%, `EnterpriseFlow.Infrastructure`
97.2%, `EnterpriseFlow.Api` 92.2%, `EnterpriseFlow.Domain` sobre 90% tras
cerrar los guard clauses (antes 83.3%). Todos por encima del ≥90% global que
exige la especificación.

## Explícitamente no perseguido hasta el 100%

- `Program.cs` (75%): las ramas no cubiertas son ajustes de arranque
  (`UseSwagger`/`UseHsts` condicionados a `IsDevelopment()`) que un test de
  integración con `WebApplicationFactory` no puede ejercitar sin arrancar la
  Api dos veces bajo dos environments distintos en el mismo proceso —
  costo/beneficio no lo justifica para un `Program.cs` de composición, no de
  lógica de negocio.
- Métodos EF Core generados por convención dentro de `Migrations/` fuera del
  alcance de `coverlet.runsettings` que igual aparecieran sueltos: no se
  persiguieron caso por caso, mismo razonamiento que la exclusión global.
- No se introdujo un framework de mutation testing (Stryker.NET) — cobertura
  de línea/branch es la métrica que pide la especificación; mutation testing
  es una mejora de calidad de test más profunda, candidata a Release 4 si se
  justifica.

## Verificación

`dotnet test EnterpriseFlow.slnx` — 118/118 passing. Reporte HTML navegable
generado con `reportgenerator` en `coverage-report/index.html` (no versionado
— es un artefacto de build, regenerable con el comando de abajo).

```bash
dotnet test EnterpriseFlow.slnx --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory ./coverage-results
reportgenerator -reports:"coverage-results/**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:"Html;TextSummary"
```
