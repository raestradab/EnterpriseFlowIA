# ADR-0004: Autorización basada en Policies dinámicas por permiso

- Estado: Aceptado
- Fecha: 2026-07-06
- Relacionado: ADR-0002, ADR-0003

## Contexto

La especificación exige explícitamente "Policy Based Authorization" (no solo
roles) y un modelo de Roles + Permisos + Menú dinámico. La forma más común y
más pobre de "usar Policies" en ASP.NET Core es declarar una Policy por rol
(`options.AddPolicy("IsAdmin", p => p.RequireRole("Admin"))`), lo que en la
práctica sigue siendo autorización por rol con una capa de indirección — no
resuelve el requisito de permisos configurables (HU-003).

## Decisión

Se implementa un **requirement genérico parametrizado por permiso**, en vez de
una Policy declarada por cada permiso existente:

- `PermissionRequirement(string permission)` — un `IAuthorizationRequirement`
  que recibe el nombre del permiso necesario (p. ej. `"projects.write"`).
- `PermissionAuthorizationHandler` — un único `AuthorizationHandler` que
  verifica si el claim `permissions` del JWT del usuario actual contiene el
  permiso solicitado.
- Un atributo/extension method `RequirePermission("projects.write")` usado en
  la definición de cada Minimal API endpoint, que internamente registra (o
  reutiliza) una Policy dinámica asociada a ese permiso vía
  `IAuthorizationPolicyProvider` custom — **no se declara una a una en
  `Program.cs`**, se resuelven en tiempo de ejecución a partir del catálogo de
  permisos del sistema.
- Los permisos en sí (`projects.read`, `projects.write`, `projects.close`,
  etc.) son un catálogo versionado en código (Domain/Application), pero **qué
  permisos tiene cada Rol es dato configurable por tenant** (tabla
  `RolePermissions`), cumpliendo HU-003/HU-004.

El `AuthorizationBehavior` de MediatR (ver
`03-diseno-arquitectura/c4-03-componentes-proyectos.md`) reutiliza el mismo
mecanismo a nivel de Command/Query, de modo que la autorización no depende
solo del atributo en el endpoint HTTP — un Handler invocado desde otro
contexto (por ejemplo, un futuro job de Hangfire) queda igualmente protegido.

## Alternativas consideradas

- **`[Authorize(Roles = "Admin,Manager")]` por endpoint**: rechazada. Acopla
  código a nombres de rol específicos; añadir un permiso a un rol nuevo
  requeriría redeploy. Incumple el requisito explícito de "Policy Based
  Authorization" y de "Permisos" configurables.
- **Una Policy declarada explícitamente por permiso en `Program.cs`**
  (`AddPolicy("projects.write", p => p.RequireClaim("permissions",
  "projects.write"))` repetido N veces): rechazada por duplicación — con
  decenas de permisos a través de 10+ módulos, esto es exactamente el "código
  duplicado" que la regla del documento original prohíbe. El
  `IAuthorizationPolicyProvider` dinámico resuelve cualquier permiso sin
  registro manual previo.
- **Autorización solo en el frontend (ocultar botones/menús)**: rechazada como
  único control — el menú dinámico (HU-005) es UX, no seguridad. Toda
  autorización real se aplica en el backend (Minimal API + MediatR pipeline);
  el frontend solo refleja lo mismo para no mostrar acciones que fallarían.

## Consecuencias

- Positivo: añadir un permiso nuevo no requiere tocar `Program.cs` ni el
  pipeline de autorización — solo añadir la constante del permiso y asignarlo
  a roles vía datos.
- Positivo: doble capa de enforcement (endpoint HTTP + pipeline MediatR)
  reduce el riesgo de que un nuevo endpoint quede sin proteger por descuido.
- Negativo: la resolución dinámica de Policies es menos "descubrible" leyendo
  `Program.cs` que una lista explícita — se mitiga con el catálogo de permisos
  documentado (Release 1, junto con Swagger) y pruebas de integración que
  verifican 403 para cada endpoint sin el permiso correspondiente.
- Seguimiento: el catálogo de permisos y su mapeo a roles por defecto se
  documenta y versiona junto con el seed data (Sprint 6 — Base de Datos).
