# Revisión de seguridad ad-hoc (2026-07-06/07)

No es un Sprint numerado del roadmap — el usuario pidió explícitamente
("valida los problemas de seguridad") una revisión de seguridad sobre el
código ya construido (Sprints 1-8), frente a la sección SEGURIDAD de
`especificcion.md` (OWASP Top 10, XSS, CSRF, CORS, Rate Limiting, CSP,
Headers, Encriptación, Secrets, Validación de archivos). Adelanta parte de la
"revisión de seguridad OWASP Top 10 dedicada" planeada para Release 4
([docs/02-roadmap.md](./02-roadmap.md)) — no la reemplaza: esta fue una
revisión manual de código (sin repo git, no aplicaba `/security-review` por
diff), no una auditoría con herramientas automatizadas ni pentesting.

## Método

Lectura sistemática del código real contra cada categoría de la especificación
— `Program.cs` (composición/middleware), `GlobalExceptionHandler`,
`appsettings*.json` (secretos), los handlers de `Login`/`RegisterTenant`/
`RefreshAccessToken`, `tokenStorage.ts`/`client.ts` del frontend — más greps
dirigidos (`v-html|innerHTML`, `FromSqlRaw|ExecuteSqlRaw`, `Cors|RateLimit`).

## Hallazgos y corrección

### Altos

| # | Hallazgo | Corrección |
|---|----------|------------|
| 1 | `Jwt:SigningKey` en `appsettings.json` era un placeholder de 62+ caracteres que **pasaba** la validación de longitud tal cual — un despliegue real sin reemplazarlo habría corrido con un secreto ya conocido públicamente. | Se quitó del archivo por completo; el arranque falla explícitamente (`InvalidOperationException` con mensaje accionable) si falta. Desarrollo local usa `dotnet user-secrets` (`EnterpriseFlow.Api/EnterpriseFlow.Api.csproj` ahora tiene `UserSecretsId`). |
| 2 | Sin rate limiting en `/api/auth/*` — brute-force y enumeración sin límite, pese a que la especificación lo exige explícitamente. | `Microsoft.AspNetCore.RateLimiting` (nativo de .NET 8, sin paquete nuevo), política `"auth"` particionada por IP, 5 peticiones/minuto, aplicada a `login`/`register-tenant`/`refresh`. |
| 3 | `RegisterTenantCommandHandler` devolvía "This email is already registered" a un llamador anónimo — email es único *globalmente* (ADR-0006), así que esto permitía enumerar cuentas registradas en cualquier tenant sin credenciales. | `RegistrationFailedException` genérica (mapeada a 400) en vez del mensaje específico de campo. El slug **sí** sigue siendo un error específico — es un identificador semi-público, no un dato personal, y el usuario necesita saber que debe elegir otro. |

### Medios

| # | Hallazgo | Corrección |
|---|----------|------------|
| 4 | Canal lateral de tiempo en `LoginCommandHandler`: el cortocircuito de `\|\|` evitaba llamar a `passwordHasher.Verify` (PBKDF2, deliberadamente lento) cuando el usuario no existía — un email inexistente respondía medible más rápido que uno real con contraseña incorrecta, permitiendo enumerar emails por tiempo de respuesta pese al mensaje de error genérico. | Se ejecuta `passwordHasher.Hash(...)` (costo equivalente) también cuando `user is null`, antes de lanzar `InvalidCredentialsException`. |
| 5 | La detección de reuso de refresh token (`RefreshAccessTokenCommandHandler`) revocaba **solo** el token reusado, no su cadena de descendientes. Secuencia real: atacante roba R1, lo usa → obtiene R2; el usuario legítimo intenta usar su copia (ya vieja) de R1 → el sistema detecta el reuso y revoca R1, pero R2 (en manos del atacante) seguía siendo válido — la detección bloqueaba a la víctima y dejaba viva la sesión del atacante. | `RevokeDescendantChainAsync` recorre `ReplacedByTokenId` hacia adelante y revoca **todos** los descendientes, no solo el token presentado. Probado con dos tests nuevos: reuso de un solo salto y reuso tras varios saltos (`IdentityEndpointsTests.Refresh_Reuse_Revokes_The_Entire_Chain_Even_Several_Hops_Later`). |
| 6 | Sin headers de seguridad (`CSP`, `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`) en ninguna respuesta. | `SecurityHeadersMiddleware` nuevo (`Api/Middleware/`), agregado al inicio del pipeline. `CSP: default-src 'self'` — es una API JSON, la única superficie HTML real es Swagger (solo en Development). |
| 7 | Refresh token de 30 días en `localStorage`, legible por cualquier JavaScript de la página (no solo el propio) — sin XSS real encontrado, pero es exactamente el tipo de credencial de larga duración que no debería depender de "que el resto del frontend siga siendo perfecto". | Movido a cookie `HttpOnly`+`Secure`(condicional)+`SameSite=Strict` — ver [ADR-0007](./adr/ADR-0007-refresh-token-en-cookie-httponly.md) para el detalle completo, incluyendo el nuevo endpoint `POST /api/auth/logout` (una cookie inaccesible a JS no deja de ser válida server-side sin esto). |

### Bajos

| # | Hallazgo | Corrección |
|---|----------|------------|
| 8 | `RegisterTenantValidator.AdminPassword` sin longitud máxima — entrada arbitrariamente larga llegaba al hasher (costo de PBKDF2 escala con el tamaño de entrada). | `MaximumLength(128)`. |
| 9 | Sin política CORS configurada en absoluto — hoy enmascarado por el proxy de Vite (mismo origen), pero la ausencia total invita a resolverlo con `AllowAnyOrigin()` bajo presión de deadline el día que frontend y Api se desplieguen a orígenes distintos. | Política `"frontend"` explícita, dirigida por configuración (`Cors:AllowedOrigins`) — sin orígenes configurados, no permite ninguno (cierra en falso), en vez de no tener mecanismo en absoluto. `AllowCredentials()` incluido para que la cookie de refresh (hallazgo #7) funcione cross-origin el día que se necesite. |
| 10 | Sin `UseHttpsRedirection()`/`UseHsts()`. | Agregados; `UseHsts()` condicionado a `!IsDevelopment()` (HSTS en local es activamente molesto — hace que el navegador recuerde exigir HTTPS para ese host). |

## Lo que ya estaba bien (no todo era hallazgo)

- **Sin SQL injection**: grep confirmó cero usos de `FromSqlRaw`/`ExecuteSqlRaw`/`ExecuteSql` en toda la base de código — coherente con la regla de ADR-0003 (LINQ parametrizado, sin excepción documentada necesaria porque no hay excepción).
- **Sin XSS por HTML crudo**: grep confirmó cero usos de `v-html`/`innerHTML`/`dangerouslySetInnerHTML` en todo `EnterpriseFlow.Web/src` — toda interpolación pasa por el auto-escape de Vue.
- **`GlobalExceptionHandler`** no filtra stack traces ni mensajes de excepción internos en el caso 500 genérico (`"An unexpected error occurred."`) — no hay fuga de información por errores no controlados.
- **Aislamiento multi-tenant a nivel de ORM** (ADR-0003): verificado que `AssignRoleToUser`, `AddProjectMember`, etc. resuelven siempre a través de `DbSet`s filtrados por tenant — un id de otro tenant simplemente no se encuentra (404), no hay IDOR posible por adivinar GUIDs.
- **Contraseñas** hasheadas con `Microsoft.AspNetCore.Identity.PasswordHasher<T>` (PBKDF2-HMACSHA256 de fábrica), no criptografía casera.
- **Rotación de refresh token con detección de reuso** (HU-002) ya existía y funcionaba — este review la *corrigió* (hallazgo #5), no la introdujo.
- **Sin CSRF clásico**: la autenticación usa `Authorization: Bearer <token>`, no cookies de sesión ambiental — un formulario malicioso en otro sitio no puede forzar una petición autenticada. (La nueva cookie de refresh, hallazgo #7, se mitiga aparte con `SameSite=Strict`, ver ADR-0007.)

## Explícitamente fuera de alcance de esta revisión

- **Validación de archivos**: el módulo de Documentos (F5) es Release 2, todavía no existe código de subida de archivos que revisar.
- **Encriptación en reposo** de la base de datos: decisión de infraestructura/despliegue, no de código de aplicación — se revisita en el Sprint de DevOps (Release 1 Sprint 11) o Release 4.
- **`TrustServerCertificate=True`** en las connection strings: aceptable contra un certificado autofirmado local; debe quitarse antes de cualquier entorno real, ya señalado junto al resto de la deuda de connection strings en [docs/07a-identidad.md](./07a-identidad.md).
- **MFA, recuperación de contraseña, verificación de email**: ninguna HU del MVP las pide (ya documentado como deliberadamente fuera de alcance en Sprint 7a); no se construyen especulativamente solo porque esta revisión tocó el módulo de Identidad.
- Auditoría con herramientas automatizadas (SAST/DAST) y pentesting real: cubiertos por Dependabot/Renovate/SonarLint en Release 4, no por esta revisión manual.

## Verificación

- Los 70 tests automatizados pasan (`dotnet test EnterpriseFlow.slnx`), incluyendo 3 nuevos/reescritos: revocación en cadena de un salto y de varios saltos, y logout revoca el refresh token.
- `npx vue-tsc --noEmit` limpio tras los cambios de frontend.
- Verificado en un navegador real (Chrome, vía claude-in-chrome) contra la Api real, a través del proxy de Vite: `document.cookie` no expone `refreshToken` tras login (confirma `HttpOnly` real, no solo la intención); `POST /api/auth/refresh` sin body funciona (la cookie viaja sola); tras `POST /api/auth/logout`, un `refresh` posterior con la misma cookie devuelve 401 (confirma revocación server-side real, no solo borrado de cookie en el cliente).
- No se pudo verificar el envío del formulario de registro por clic real en la UI en esta sesión — problema de referencias de DOM obsoletas en la herramienta de automatización del navegador, no relacionado con el código (ningún componente `.vue` fue tocado por este review salvo el `logout()` de `DefaultLayout.vue`, que pasó a ser `async`).
