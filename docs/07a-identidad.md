# Sprint 7a — Backend: Identidad (E1)

Primera mitad de Sprint 7 (Backend), según lo acordado: Identidad completa
antes que los módulos de negocio (Sprint 7b). Alcance: HU-001 a HU-006.

## Qué se implementó

**Dominio**: `Tenant` (raíz, sin `ITenantScoped` — define la frontera, no está
dentro de ella), `User` (dueño de `UserRoleAssignment`, mismo patrón que
`Project`/`ProjectMember`, ADR-0005), `Role` (dueño de `RolePermission`),
`RefreshToken` (con `IsActive`/`MarkUsed`/`Revoke` para rotación).

**Application**: `RegisterTenant`, `Login`, `RefreshAccessToken`,
`CreateRole`, `AssignRoleToUser`, `GetMyPermissions` — seis slices verticales
sobre el mismo pipeline (`AuthorizationBehavior`+`ValidationBehavior`) ya
probado en Sprint 4. `Permissions.All()` (reflexión sobre el catálogo) siembra
el rol "Administrator" con todos los permisos existentes al registrar un
tenant, sin mantener una lista duplicada.

**Infrastructure**: `JwtTokenService` (emisión, `System.IdentityModel.Tokens.Jwt`),
`PasswordHasher` (envuelve `Microsoft.AspNetCore.Identity.PasswordHasher<T>` —
PBKDF2-HMACSHA256 de fábrica, no cifrado casero), `JwtCurrentTenantService`/
`JwtCurrentUserService` (reemplazan los stubs de headers de Sprint 4), EF
configs + migración para las 4 tablas nuevas.

**Api**: `AddAuthentication().AddJwtBearer(...)`, `IAuthorizationPolicyProvider`
dinámico (`DynamicPermissionPolicyProvider` + `PermissionAuthorizationHandler`
— exactamente el diseño de ADR-0004: el nombre del permiso ES el nombre de la
Policy, resuelta bajo demanda, cero `AddPolicy` por permiso), endpoints
`/api/auth/{register-tenant,login,refresh,me,roles,users/{id}/roles/{id}}`.
`CompaniesEndpoints` migrado de headers a `RequirePermission(...)` real.

## ADRs nuevos

- [ADR-0006](./adr/ADR-0006-autenticacion-bypassa-filtro-de-tenant.md):
  Login/Registro operan con `IgnoreQueryFilters()` explícito porque no existe
  tenant "actual" antes de autenticar. El interceptor de auditoría se ajustó
  para no sobrescribir un `TenantId` ya asignado explícitamente (necesario
  para que `RegisterTenant` funcione).

## Bug encontrado y corregido (otra vez, documentado sin adornos)

`LoginCommandHandler` y `RefreshAccessTokenCommandHandler` cargaban el `User`
sin `.Include(u => u.RoleAssignments)`. Sin carga diferida configurada, la
colección quedaba vacía, `PermissionResolver` no encontraba roles, y el JWT
se emitía **sin ningún permiso** — login "funcionaba" (200 OK, token válido),
pero cualquier endpoint protegido devolvía 403 siempre. El test end-to-end
(`RegisterTenant_Then_Login_Grants_A_Working_Token`, que valida el contenido
real de `/api/auth/me`, no solo el código de estado del login) lo atrapó
inmediatamente. Segunda vez en este proyecto que una prueba end-to-end real
encuentra un bug que un test más superficial (solo verificar 200 OK) habría
dejado pasar — el patrón se está repitiendo lo suficiente como para tomarlo en
serio: **verificar contenido de la respuesta, no solo el código de estado**,
en cualquier test que exista para probar un flujo completo.

## Nota de seguridad honesta (resuelta)

El `Jwt:SigningKey` en `appsettings.json` era un placeholder de texto plano.
Señalado aquí originalmente como deuda a resolver en el Sprint de
DevOps/Seguridad — se adelantó y se corrigió en la revisión de seguridad
ad-hoc del 2026-07-07 (pedida explícitamente por el usuario, no parte de un
Sprint numerado): el secreto ya no vive en el archivo versionado, y el
arranque falla explícitamente si no se configura vía `dotnet user-secrets`
(desarrollo) o variable de entorno/Key Vault (producción). Detalle completo en
[docs/08a-seguridad.md](./08a-seguridad.md).

## Qué falta (a propósito) para más adelante

- Recuperación de contraseña, verificación de email, MFA — ninguna HU del MVP
  las pide; no se construyen especulativamente.
- UI del menú dinámico (HU-005) — el backend ya expone `/api/auth/me` con los
  permisos; el frontend que lo consume es Sprint 8.

> Nota (2026-07-07): el ítem que originalmente estaba aquí — "revocación de
> *todos* los refresh tokens al detectar reuso, hoy solo se revoca el token
> reusado" — se corrigió en la revisión de seguridad ad-hoc. Era un hallazgo
> real, no una mejora opcional: dejaba viva la sesión de un atacante mientras
> bloqueaba al usuario legítimo. Ver [docs/08a-seguridad.md](./08a-seguridad.md).
