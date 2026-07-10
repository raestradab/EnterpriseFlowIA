# Release 2, Sprint 4 — Validación de Arquitectura

Mismo objetivo que Sprint 4 de Release 1: construir **un slice vertical
completo y real**, de punta a punta, y confirmar con evidencia ejecutable
—no solo diagramas— que el esqueleto de Sprint 3 (Redis/Hangfire/SignalR,
`CachingBehavior`/`CacheInvalidationBehavior`) funciona en conjunto. El
slice elegido: **Catálogos (F8.2, HU-082)** — el único mecanismo de Sprint 3
sin ningún consumidor real todavía, y el más simple de los pendientes de
Release 2 (una sola entidad propia, sin depender de Documentos/Workflow que
todavía no existen).

## Qué se implementó

- **Domain**: `CatalogDefinition` (agregado, `ITenantScoped`/
  `IAuditableEntity`/`ISoftDeletable`) dueño de `CatalogItem` (mismo patrón
  que `Role`/`RolePermission`) — `AddItem`/`UpdateItemLabel`/`RemoveItem`,
  con `DuplicateCatalogItemKeyException`/`CatalogItemNotFoundException`
  nuevas.
- **Application**: `Features/Catalogs/{CreateCatalog, AddCatalogItem,
  UpdateCatalogItem, RemoveCatalogItem, GetCatalogItems, GetCatalogs}` — seis
  slices verticales sobre el mismo pipeline ya probado en Release 1.
  `GetCatalogItemsQuery` implementa `ICacheableQuery`;
  `AddCatalogItemCommand`/`UpdateCatalogItemCommand`/`RemoveCatalogItemCommand`
  implementan `IInvalidatesCache` — el primer uso real de ambos marcadores.
  `Permissions.Catalogs.{Read,Manage}` nuevos.
- **Infrastructure**: `CatalogDefinitionConfiguration`/`CatalogItemConfiguration`,
  migración `AddCatalogs` (2 tablas, 2 índices — uno único por
  `(TenantId, Name)` en el catálogo, otro único por `(CatalogDefinitionId, Key)`
  en los ítems, mismo patrón "defensa en profundidad" que
  `ProjectMemberConfiguration` ya establecía).
- **Api**: `CatalogsEndpoints` (`/api/catalogs`, con `/items` anidado).

## Un gap real, encontrado y corregido en este sprint

El diseño original de `ICacheableQuery` (ADR-0012, Sprint 2) dejaba que cada
Query incluyera el `TenantId` en su propio `CacheKey` — una convención, no
una garantía. Al construir el primer consumidor real, se corrigió: la
prefijación de tenant se movió a `CachingBehavior`/`CacheInvalidationBehavior`
mismos (`CacheKeys.ForTenant`), la misma razón por la que ADR-0003 aplica el
filtro de tenant como Global Query Filter en vez de confiar en que cada
handler recuerde el `WHERE TenantId = ...`. Para `GetCatalogItemsQuery`
específicamente esto no era explotable todavía (la clave ya incluye el
`CatalogId`, un GUID único entre tenants, así que dos tenants nunca
colisionarían) — pero es exactamente el tipo de garantía estructural que
debe existir *antes* de que una Query futura la necesite genuinamente, no
después de un incidente. Detalle completo en ADR-0012 (sección "Corrección
encontrada").

Encaja con el patrón ya establecido en el proyecto: Sprint 4 de Release 1
encontró que el interceptor de auditoría no se registraba en el factory de
tests; Sprint 3 de Release 2 encontró que el `IUserIdProvider` por defecto de
SignalR no funcionaría con la configuración JWT existente; este sprint
encuentra el gap de la clave de caché. Cada validación real destapa al menos
un supuesto que la fase de diseño no pudo ver hasta que hubo código
ejecutándose.

## Evidencia ejecutable

- **`EnterpriseFlow.Domain.UnitTests`**: 13 pruebas nuevas de
  `CatalogDefinition` (guard clauses, duplicados, ítem no encontrado, ciclo
  de vida) — mismo patrón que `RoleTests`.
- **`EnterpriseFlow.Application.UnitTests`**: 6 pruebas nuevas,
  `CachingBehaviorTests`/`CacheInvalidationBehaviorTests` — behaviors
  probados en aislamiento con `IDistributedCache`/`ICurrentTenantService`
  mockeados (Moq), verificando explícitamente que la clave calculada lleva
  el prefijo `tenant:{tenantId}:` y que una invalidación **nunca** ocurre si
  el handler real lanza una excepción.
- **`EnterpriseFlow.Api.IntegrationTests`**: 5 pruebas nuevas
  (`CatalogsEndpointsTests`), la más importante:
  `GetCatalogItems_Serves_A_Cached_Read_Until_A_Write_Invalidates_It` —
  deliberadamente *black-box* (HTTP puro, sin tocar `IDistributedCache`
  directamente): puebla el caché con una lectura, muta la fila **directamente
  en la base de datos, sin pasar por la Api**, confirma que la siguiente
  lectura **todavía devuelve el valor viejo** (prueba positiva de que sí hay
  caché, no una simple lectura a base de datos en cada request), y luego
  confirma que una escritura real a través de la Api invalida y la
  lectura siguiente refleja el cambio de inmediato — exactamente el
  escenario Gherkin de HU-082.
- **Migración `AddCatalogs` verificada contra LocalDB real** (no solo
  generada): aplicada a una base nueva junto con `InitialCreate`, ambas
  migraciones limpias, sin intervención manual.
- Suite completa: **142/142 tests** (84+12+6+40), `dotnet format
  --verify-no-changes` limpio.

## Checklist SOLID / Clean Architecture (mismo formato que Sprint 4 de Release 1)

| Principio | Evidencia |
|---|---|
| **S**RP | `CachingBehavior`/`CacheInvalidationBehavior` son la única pieza del sistema que sabe de `IDistributedCache`; ningún `CommandHandler`/`QueryHandler` de Catálogos lo conoce. |
| **O**CP | Una Query futura que necesite caché solo implementa `ICacheableQuery` — cero cambios en `CachingBehavior` ni en su propio `Handler`. |
| **L**SP | `CatalogDefinition` implementa `ITenantScoped`/`IAuditableEntity`/`ISoftDeletable` sin casos especiales — el filtro global y el interceptor de auditoría lo cubren automáticamente, mismo mecanismo que las 11 entidades anteriores. |
| **I**SP | `ICacheableQuery`/`IInvalidatesCache` son marcadores mínimos (una propiedad/colección cada uno) — un Command que no necesita invalidar nada simplemente no los implementa. |
| **D**IP | `CachingBehavior` depende de `IDistributedCache` (abstracción de `Microsoft.Extensions.Caching.Abstractions`, no de `StackExchange.Redis` directamente) — Application nunca referencia Redis. |
| CQRS | `AddCatalogItemCommand`/`UpdateCatalogItemCommand`/`RemoveCatalogItemCommand` (mutan, invalidan) vs. `GetCatalogItemsQuery` (lectura cacheada) — misma separación que el resto del sistema. |

## Qué no se hizo en este sprint (a propósito)

- Ningún consumidor real de Catálogos todavía (Documentos, que según
  `r2-01-vision-y-alcance.md` sería el primer consumidor real de
  "Categorías de Documento") — eso llega cuando F5 se construya. El catálogo
  en sí es genérico y funcional, sin un caso de uso especulativo inventado
  para "usarlo ya".
- Redis real: la suite corre contra `AddDistributedMemoryCache()` (mismo
  gap ya señalado en Sprint 3, todavía sin una instancia de Redis alcanzable
  en este entorno). El mecanismo de `CachingBehavior`/`CacheInvalidationBehavior`
  ya quedó probado exhaustivamente contra la abstracción `IDistributedCache`
  — lo único no verificado es la implementación concreta de Redis, que es
  código de Microsoft (`Microsoft.Extensions.Caching.StackExchangeRedis`),
  no código de este proyecto.
