# Sprint 4 — Validación de Arquitectura

Objetivo del sprint (según `02-roadmap.md`): construir un slice vertical completo
end-to-end y confirmar, con evidencia ejecutable (no solo diagramas), que la
arquitectura diseñada en el Sprint 2 (ADR-0002, ADR-0003, ADR-0004) realmente
funciona en conjunto.

## Qué se implementó

El módulo **Empresas** (F2.1), ya anticipado como slice representativo en
`03-diseno-arquitectura/c4-03-componentes-proyectos.md`:

- **Domain**: `Company` (agregado), implementando `ITenantScoped`,
  `IAuditableEntity`, `ISoftDeletable`.
- **Application**: `CreateCompanyCommand`/`Handler`/`Validator`,
  `GetCompanyByIdQuery`/`Handler`, pipeline `AuthorizationBehavior` +
  `ValidationBehavior`, catálogo de permisos (`Permissions.Companies.*`).
- **Infrastructure**: `AppDbContext` (con el query filter de tenant + soft
  delete), `AuditableEntitySaveChangesInterceptor` (asigna `TenantId` y estampa
  auditoría automáticamente), `CompanyConfiguration`, migración inicial de EF
  Core, implementaciones **temporales** basadas en headers de
  `ICurrentTenantService`/`ICurrentUserService` (reemplazadas por JWT en el
  Sprint 7 / E1, sin tocar Application ni Api).
- **Api**: `CompaniesEndpoints` (Minimal API), `GlobalExceptionHandler`
  (`IExceptionHandler` de .NET 8, traduce excepciones a `ProblemDetails`).

## Evidencia ejecutable

- `EnterpriseFlow.Architecture.Tests` (NetArchTest): 6 pruebas verifican que
  Domain no depende de Application/Infrastructure/Api ni de MediatR/EF
  Core/AspNetCore; que Application no depende de Infrastructure/Api; que
  Infrastructure no depende de Api; y que los Handlers son `sealed`.
- `EnterpriseFlow.Domain.UnitTests` / `EnterpriseFlow.Application.UnitTests`:
  invariantes de `Company`, comportamiento de `AuthorizationBehavior`
  (permite/rechaza según permiso), validación de `CreateCompanyCommand`.
- `EnterpriseFlow.Api.IntegrationTests`: **prueba real end-to-end** contra
  SQLite (no el proveedor "InMemory" de EF Core — ver nota en
  `CustomWebApplicationFactory`), a través de HTTP real (`WebApplicationFactory`):
  - Crear y luego leer una Empresa dentro del mismo tenant funciona.
  - Leer una Empresa creada por **otro tenant** devuelve 404 (aislamiento real,
    no solo teórico).
  - Crear sin el permiso `companies.manage` devuelve 403.
  - Crear con nombre vacío devuelve 400 con el detalle de validación.

Total: 23 pruebas, 0 fallos, en los 4 proyectos de test.

## Un bug real, encontrado y corregido en este sprint

La primera versión de `CustomWebApplicationFactory` reemplazaba el registro de
`AppDbContext` para usar SQLite, pero al hacerlo **omitía volver a registrar
`AuditableEntitySaveChangesInterceptor`** vía `.AddInterceptors(...)`. Resultado:
la Empresa se creaba (201 Created) pero con `TenantId = Guid.Empty` (el
interceptor que lo asigna nunca se ejecutaba), y la lectura posterior siempre
devolvía 404 — no por un fallo del filtro de tenant, sino porque el dato nunca
tuvo el tenant correcto.

Se diagnosticó con una prueba temporal que inspeccionaba la fila directamente
vía `IgnoreQueryFilters()`, confirmando `TenantId` vacío. Corregido registrando
el interceptor también en la configuración de test. Se documenta aquí en vez de
ocultarlo: es exactamente el tipo de error que las pruebas de integración de
este sprint existen para atrapar, y confirma que la prueba end-to-end no es
superflua — sin ella este bug habría llegado a Sprint 5 sin detectar.

Una hipótesis inicial (incorrecta) fue que el patrón "instance re-binding" de
EF Core para query filters basados en campos de instancia no funcionaba a
través de despacho por reflexión. Se simplificó `AppDbContext` a un
`HasQueryFilter` directo por entidad (sin reflexión) tanto para eliminar esa
variable de la investigación como por ser el patrón más simple y ampliamente
documentado. Con un solo entity (`Company`) el costo de "un `HasQueryFilter`
por entidad" es mínimo; se reevaluará generalizarlo con reflexión en Sprint 5,
cuando haya 2-3 entidades más para probarlo con la misma cobertura de pruebas
de integración que atrapó este bug.

## Checklist SOLID / Clean Architecture

| Principio | Evidencia |
|---|---|
| **S**RP | `CreateCompanyCommandHandler` solo orquesta (crea agregado + persiste); la invariante de nombre vive en `Company.Create`; la autorización vive en `AuthorizationBehavior`, no en el Handler. |
| **O**CP | Nuevos casos de uso (`CloseCompany`, futuro) se añaden como nuevas carpetas en `Features/Companies/`, sin modificar `CreateCompany`/`GetCompanyById`. |
| **L**SP | `ITenantScoped`/`IAuditableEntity`/`ISoftDeletable` son contratos mínimos; cualquier entidad que los implemente funciona con el interceptor y los query filters sin casos especiales. |
| **I**SP | Interfaces pequeñas y específicas (`ICurrentTenantService` solo expone `TenantId`; `ICurrentUserService` solo lo relativo a permisos/identidad) — ningún consumidor depende de miembros que no usa. |
| **D**IP | `CreateCompanyCommandHandler` depende de `IAppDbContext` (Application), no de `AppDbContext` (Infrastructure); confirmado por `Application_Should_Not_Depend_On_Infrastructure_Or_Api`. |
| Regla de dependencias (Clean Architecture) | Confirmada por 4 de las 6 pruebas de `EnterpriseFlow.Architecture.Tests`, no solo por inspección visual del diagrama C4. |
| CQRS | `CreateCompanyCommand` (muta, pasa por `ValidationBehavior`) vs. `GetCompanyByIdQuery` (proyección de solo lectura directa a DTO, sin pasar por el agregado). |

## Simplificaciones deliberadas de este sprint (no de alcance final)

- `ICurrentTenantService`/`ICurrentUserService` leen headers HTTP, no JWT — se
  reemplazan en Sprint 7 (E1: Identidad) sin tocar Application/Api.
- No hay un `AuditBehavior` de MediatR separado para el trail de cambios
  (HU-040 completo, con diff de campos); el interceptor solo estampa
  created/modified by/when. El trail de auditoría completo se construye en
  Sprint 7 cuando el módulo de Auditoría se implemente, reutilizando este mismo
  punto de extensión.
- El query filter multi-tenant está escrito a mano para `Company` (sin
  reflexión sobre las interfaces marcador); se generalizará en Sprint 5 con más
  entidades y la misma cobertura de integration tests como red de seguridad.
