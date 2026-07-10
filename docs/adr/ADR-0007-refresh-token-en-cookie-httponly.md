# ADR-0007: El refresh token vive en una cookie HttpOnly, no en el body JSON/localStorage

- Estado: Aceptado
- Fecha: 2026-07-07
- Relacionado: ADR-0006, [docs/08a-seguridad.md](../08a-seguridad.md)

## Contexto

Desde Sprint 7a, `POST /api/auth/login` y `POST /api/auth/refresh` devolvían
el refresh token (30 días de vida) como un campo más del body JSON, y el
frontend lo guardaba en `localStorage` (`tokenStorage.ts`). Una revisión de
seguridad ad-hoc pedida explícitamente por el usuario ("valida los problemas
de seguridad", 2026-07-06) identificó esto como un hallazgo real: `localStorage`
es legible por cualquier JavaScript que corra en la página, incluyendo un
payload de XSS futuro — a diferencia del access token (15 minutos de vida), el
refresh token es exactamente el tipo de credencial de larga duración que las
guías de OWASP marcan como la que **no** debe quedar expuesta a JS.

No se encontró ningún vector de XSS real en el frontend actual (sin
`v-html`/`innerHTML` en toda la base de código, verificado con grep) — el
riesgo era de defensa en profundidad, no una vulnerabilidad explotable hoy. Se
corrige de todas formas porque el costo de hacerlo bien ahora es bajo y crece
con cada sprint que agregue más superficie a la SPA.

## Decisión

El refresh token se transmite como cookie `HttpOnly` + `Secure` (condicionado
a `Request.IsHttps`, ver más abajo) + `SameSite=Strict`, con `Path=/api/auth`
para no enviarse en cada petición a la Api, solo a los endpoints de auth. El
servidor la fija (`AuthEndpoints.SetRefreshTokenCookie`) en `/login` y
`/refresh`; el JSON de respuesta de ambos endpoints ahora solo contiene
`accessToken`/`accessTokenExpiresAtUtc` — el refresh token nunca vuelve a
tocar JavaScript en absoluto.

Cambios que esto arrastra:

- **`POST /api/auth/refresh` ya no recibe body**: lee la cookie directamente
  de `HttpContext.Request.Cookies`. El frontend simplemente hace la petición
  con `withCredentials: true`; el navegador adjunta la cookie solo.
- **Nuevo `POST /api/auth/logout`**: una cookie HttpOnly solo deja de ser
  legible por JS, no deja de ser válida — sin un endpoint que la revoque
  server-side, "cerrar sesión" seguiría siendo válido hasta su expiración de
  30 días para quien tuviera la cookie. `LogoutCommandHandler` revoca el
  `RefreshToken` correspondiente por hash; es best-effort e idempotente (un
  token ya inválido o desconocido no es un error).
- **`Secure = http.Request.IsHttps`**, no un booleano fijo: en local (y en los
  tests de integración) la Api corre sobre HTTP plano, y una cookie `Secure`
  simplemente nunca se guardaría/enviaría — este patrón (`SameAsRequest`) se
  adapta solo el día que haya HTTPS real en frente, sin acoplar el código a
  nombres de entorno.

## Alternativas consideradas

- **Mantener el refresh token en el body JSON, mitigar solo con CSP**:
  rechazada — CSP ayuda a *prevenir* XSS, pero si de todas formas ocurre uno
  (una librería de terceros comprometida, por ejemplo), un token en
  `localStorage` sigue siendo robado trivialmente. La cookie `HttpOnly` es la
  única mitigación que sigue funcionando incluso *después* de que XSS ya
  ocurrió.
- **Mover también el access token a memoria (nunca a `localStorage`)**:
  rechazada para este cambio — habría forzado a rediseñar el arranque de la
  SPA (sin persistencia entre recargas de página, se necesitaría un refresh
  silencioso vía la cookie en cada arranque de la app) por una ganancia
  marginal: el access token ya vive solo 15 minutos, la ventana de exposición
  ante un XSS hipotético es mucho más corta que la del refresh token de 30
  días. Puede revisitarse si en el futuro se justifica con un caso concreto.
- **Cookie de sesión sin rotación, invalidada solo por expiración**: rechazada
  — se perdería la detección de reuso (HU-002) que ya existe y funciona
  (incluyendo la revocación en cadena corregida en el mismo review, ver
  [docs/08a-seguridad.md](../08a-seguridad.md)); cambiar el transporte del token
  no debía degradar una garantía de seguridad que ya estaba validada con
  pruebas.

## Consecuencias

- Positivo: el refresh token queda inalcanzable para JavaScript incluso si
  aparece un XSS futuro en cualquier dependencia o vista nueva — la mitigación
  no depende de que el resto del frontend siga siendo perfecto.
- Positivo: `SameSite=Strict` es suficiente protección CSRF para esta cookie
  sin añadir un mecanismo de token CSRF aparte — no hay ninguna acción
  "navegable" (GET con efectos secundarios) que dependa de ella, y la cookie
  simplemente no se envía en peticiones cross-site.
- Negativo: cualquier cliente no-navegador de esta Api (un futuro cliente
  móvil nativo, un script de automatización) ya no puede leer el refresh
  token del body de la respuesta — tendría que manejar cookies explícitamente
  o la Api necesitaría un flujo de auth alternativo para ese caso. No hay
  ningún cliente así hoy; se revisita si aparece uno real.
- Negativo: probar la rotación/reuso por HTTP directo (tests de integración)
  requiere leer/escribir el header `Set-Cookie`/`Cookie` a mano en vez de
  pasar el token como campo JSON — más verboso, pero es exactamente el
  trade-off esperado al mover un secreto fuera del alcance de JS.
