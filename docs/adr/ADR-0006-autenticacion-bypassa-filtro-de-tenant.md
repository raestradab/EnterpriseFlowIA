# ADR-0006: Registro y Login operan fuera del filtro de tenant

- Estado: Aceptado
- Fecha: 2026-07-07
- Relacionado: ADR-0003, ADR-0004

## Contexto

El Global Query Filter (ADR-0003) exige un `ICurrentTenantService.TenantId`
para poder filtrar. Pero **Login** y **Registro de Tenant** (HU-001/HU-002)
ocurren, por definición, antes de que exista una sesión autenticada — no hay
todavía un tenant "actual" que usar para filtrar. Aplicado ciegamente, el
filtro dejaría estas dos operaciones incapaces de encontrar ningún dato.

Además, para que un usuario pueda iniciar sesión con solo su email (sin pedir
también el slug del tenant), el email debe ser **único en toda la
plataforma**, no solo dentro de un tenant — otro caso donde una consulta
necesita ver a través de todos los tenants a propósito.

## Decisión

`LoginCommandHandler`, `RefreshAccessTokenCommandHandler` y
`RegisterTenantCommandHandler` consultan `Users`/`Roles`/`RefreshTokens` con
`.IgnoreQueryFilters()` explícito, documentado inline en cada uso. Esto es
**intencional y acotado a estos tres handlers** — el resto de la aplicación
sigue sin poder saltarse el filtro por accidente, porque `IgnoreQueryFilters`
debe invocarse explícitamente por nombre en cada sitio; no hay una forma de
"olvidarlo" silenciosamente como sí la había con un filtro manual por handler.

Complementariamente, `AuditableEntitySaveChangesInterceptor.ApplyTenant` solo
auto-asigna `TenantId` cuando la entidad **todavía no tiene uno** (`TenantId ==
Guid.Empty`). Esto permite que `RegisterTenantCommandHandler` asigne
explícitamente el tenant recién creado a `Role`/`User` antes de guardar, sin
que el interceptor lo sobrescriba con el tenant "ambiente" (que en este flujo
ni siquiera existe).

## Alternativas consideradas

- **Login requiere el slug del tenant además del email** (evitando la
  necesidad de unicidad global de email): rechazada para el MVP — añade
  fricción a la UX de login (un campo más) para un problema que la unicidad
  global de email resuelve de forma más simple. Si en el futuro se requiere
  que el mismo email exista en distintos tenants (p. ej. un consultor externo
  con cuentas en varios clientes), esta decisión se revisita.
- **Un `ICurrentTenantService` con un valor "wildcard"/nulo especial que el
  filtro interprete como "sin filtrar"**: rechazada — mezclar ese caso especial
  dentro del mecanismo de filtro genérico lo haría más difícil de razonar para
  *todas* las entidades, a cambio de simplificar solo tres handlers.
  `IgnoreQueryFilters()` explícito es la primitiva que EF Core ya ofrece para
  exactamente este propósito.

## Consecuencias

- Positivo: el resto del código base sigue teniendo la garantía fuerte de
  ADR-0003 (aislamiento por defecto); el bypass es una excepción visible y
  buscable (`grep IgnoreQueryFilters`), no un modo oculto del mecanismo
  general.
- Negativo: cualquier nuevo flujo "pre-autenticación" futuro (p. ej.
  recuperación de contraseña) tendrá que recordar aplicar el mismo patrón
  explícitamente — no hay una abstracción que lo generalice. Aceptable porque
  son pocos casos y todos de alto riesgo, donde la explicitud es preferible a
  la "magia".
