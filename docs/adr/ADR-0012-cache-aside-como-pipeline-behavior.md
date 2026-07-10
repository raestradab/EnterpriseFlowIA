# ADR-0012: Cache-aside de Catálogos como MediatR Pipeline Behavior, no llamadas manuales

- Estado: Aceptado
- Fecha: 2026-07-08
- Relacionado: ADR-0008 (activación de Redis), HU-082

## Contexto

F8.2 (Catálogos) necesita cache-aside sobre sus lecturas, con invalidación
explícita en cada escritura (ADR-0008). Release 1 ya estableció el patrón de
Pipeline Behaviors de MediatR para preocupaciones transversales —
`AuthorizationBehavior` y `ValidationBehavior` envuelven cada
Command/Query sin que ningún handler individual sepa que existen (ver
`07a-identidad.md`). La pregunta de este ADR es si el cacheo de Catálogos
sigue ese mismo patrón o se implementa como llamadas manuales a
`IDistributedCache` dentro de los handlers de Catálogos.

## Decisión

Dos Pipeline Behaviors nuevos, mismo mecanismo que los ya existentes:

- **`CachingBehavior<TRequest, TResponse>`**: si `TRequest` implementa el
  marcador `ICacheableQuery` (expone `CacheKey`), intenta resolver la
  respuesta desde `IDistributedCache` antes de invocar el handler real; si
  hay miss, invoca el handler y guarda el resultado.
- **`CacheInvalidationBehavior<TRequest, TResponse>`**: si `TRequest`
  implementa `IInvalidatesCache` (expone las `CacheKey`s a remover), ejecuta
  el handler real primero y **luego** invalida — nunca al revés, para no
  invalidar una entrada que un handler fallido nunca llegó a cambiar.

`GetCatalogItemsQuery` (ítems de un Catálogo) implementa `ICacheableQuery`;
`AddCatalogItemCommand`/`UpdateCatalogItemCommand`/`RemoveCatalogItemCommand`
implementan `IInvalidatesCache`. Ningún `CommandHandler`/`QueryHandler` de
Catálogos conoce `IDistributedCache` ni Redis directamente.

**Corrección encontrada al construir el primer consumidor real (Sprint 4)**:
el diseño original de este ADR dejaba que cada `ICacheableQuery` incluyera el
`TenantId` en su propio `CacheKey` — delegado al autor de cada Query, el
mismo tipo de convención-no-forzada que ADR-0003 evita deliberadamente para
el filtro de tenant en la base de datos. Corregido: `CachingBehavior`/
`CacheInvalidationBehavior` inyectan `ICurrentTenantService` y anteponen
`tenant:{tenantId}:` a toda clave (`CacheKeys.ForTenant`, compartido entre
ambos behaviors para que una escritura y su invalidación calculen siempre la
misma clave) — un autor de Query no puede filtrar datos entre tenants a
través del caché ni queriendo, en vez de simplemente no debiendo.

## Alternativas consideradas

- **Llamadas manuales a `IDistributedCache` dentro de
  `GetCatalogsQueryHandler`/los handlers de escritura**: rechazada — mezcla
  una preocupación transversal (caching) con la lógica de negocio del
  handler, y no se reutiliza si un Release futuro necesita cachear otra
  lectura (p. ej. `GetCompaniesQuery`, hoy sin cachear porque no tenía el
  volumen de lectura que justificara Redis — ver ADR-0008). El Behavior
  genérico deja esa puerta abierta sin costo adicional: cualquier Query
  futura solo necesita implementar `ICacheableQuery`.
- **Decorator pattern sobre el handler específico vía Scrutor
  (`services.Decorate<IRequestHandler<...>>(...)`)**: rechazada — añadiría
  una dependencia nueva (Scrutor) para resolver algo que el pipeline de
  Behaviors de MediatR, ya presente y ya entendido en el proyecto, resuelve
  igual de bien. Dos mecanismos distintos para la misma clase de problema
  (interceptar el flujo de una Request) sería la inconsistencia que
  `especificcion.md` pide evitar ("no generar código duplicado" se lee aquí
  como "no dupliques el *mecanismo*", no solo el código literal).
- **TTL únicamente, sin invalidación explícita en escritura**: rechazada
  en ADR-0008 y no reabierta aquí — un catálogo que tarda hasta que expira
  un TTL en reflejar una edición reciente confundiría al administrador que
  acaba de hacer el cambio.

## Consecuencias

- Positivo: agregar caching a una Query nueva en el futuro es una línea
  (`: ICacheableQuery`), no un cambio en su `Handler`.
- Positivo: consistente con el resto del pipeline — un desarrollador que ya
  entiende cómo `AuthorizationBehavior`/`ValidationBehavior` funcionan
  entiende `CachingBehavior` sin aprender un mecanismo nuevo.
- Negativo: el orden de registro de los Behaviors en el pipeline importa
  (`CachingBehavior` antes de `ValidationBehavior` significaría servir una
  respuesta cacheada sin validar un request potencialmente inválido — el
  orden correcto es Authorization → Validation → Caching → Handler real,
  documentado explícitamente en `DependencyInjection.cs` para que no se
  reordene por accidente en un cambio futuro).
