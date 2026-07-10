# Release 4, Sprint 8 — Frontend

`r4-01-vision-y-alcance.md` (sección 3) ya había decidido, en Sprint 1,
que Release 4 no agrega ninguna UI nueva: el historial de Temporal Tables
(HU-102) vive en la base de datos, consultable por API, sin ninguna
Historia de Usuario que pida una pantalla dedicada — mismo criterio que
RAG (Release 3) no prometió una UI de búsqueda de texto libre. Ese
alcance se confirma en este Sprint: no se agregó ninguna vista, ruta ni
componente nuevo.

Igual que Sprint 6 (Base de Datos, esperado como "solo confirmación" y
terminó encontrando un gap real de documentación), este Sprint auditó el
frontend buscando un gap real de *hardening* — coherente con el nombre
del Release — en vez de cerrarlo en la primera línea.

## Hallazgo real: la SPA servida en producción no tenía cabeceras de seguridad

`SecurityHeadersMiddleware`
(`src/EnterpriseFlow.Api/Middleware/SecurityHeadersMiddleware.cs`) ya
existe desde antes de Release 4, pero su propio comentario dice
explícitamente que cubre la API JSON, no un sitio HTML — y no corre para
nada servido por `nginx.conf`
(`src/EnterpriseFlow.Web/nginx.conf`), que es exactamente donde vive la
superficie real de ataque del navegador (XSS, clickjacking) en
producción. Auditado: `nginx.conf` no tenía ninguna cabecera de
seguridad — ni `X-Frame-Options`, ni `Content-Security-Policy`, nada.

**Corregido** — agregado un bloque `add_header ... always;` al `server`
de `nginx.conf`, con una política adaptada a lo que la SPA realmente
hace (no copiada de la de la API):

- `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`,
  `Referrer-Policy: no-referrer`,
  `Permissions-Policy: geolocation=(), camera=(), microphone=()` —
  iguales a la API.
- `Content-Security-Policy: default-src 'self'; script-src 'self';
  style-src 'self' 'unsafe-inline'; font-src 'self'; img-src 'self'
  data:; connect-src 'self'; frame-ancestors 'none'` — con una diferencia
  deliberada frente al `default-src 'self'` estricto de la API:
  `style-src` necesita `'unsafe-inline'` porque Vuetify
  (`src/plugins/vuetify.ts`) inyecta un `<style
  id="vuetify-theme-stylesheet">` en tiempo de ejecución para aplicar
  los colores del tema, y este stack no tiene wiring de nonce de CSP
  (Vuetify sí soporta un `cspNonce` de configuración, pero requiere
  generar y propagar un nonce por request desde el servidor — nginx solo
  sirviendo archivos estáticos no puede hacerlo sin un módulo adicional
  como `njs`; se evaluó y se descartó por ser una complejidad real sin
  un caso de uso que la justifique todavía, mismo criterio de ADR-0001).
  El resto de orígenes se mantiene en `'self'` porque una búsqueda real
  (`grep` de literales `http://`/`https://` en todo `src/`) confirmó
  cero dependencias de terceros: sin fuentes externas (MDI se empaqueta
  vía npm), sin CDN, sin imágenes remotas.

## Verificación

**Real, no solo revisión de código:**

- `npm run build` (Vite + `vue-tsc -b`) generó el bundle de producción
  real: `dist/index.html` confirma que no hay scripts inline (`<script
  type="module" src="/assets/index-*.js">`) y que el CSS es un archivo
  externo (`<link rel="stylesheet" href="/assets/index-*.css">`) — el
  build de Vite nunca necesitó `script-src 'unsafe-inline'`.
- Se sirvió ese `dist/` real con un servidor HTTP local que replica
  exactamente las cabeceras nuevas de `nginx.conf` (mismo texto de CSP,
  carácter por carácter), y se abrió en un navegador real
  (`http://localhost:4173/login`):
  - La página de login renderizó con el tema de Vuetify aplicado
    correctamente (botón primario azul, tipografía, `VCard` redondeada)
    — prueba directa de que `style-src 'unsafe-inline'` no rompe el
    theming en tiempo de ejecución.
  - Cero mensajes en la consola del navegador (ni errores de CSP del
    tipo "Refused to apply/load/execute", ni ningún otro).
  - `curl -D -` confirmó las 5 cabeceras presentes carácter por carácter
    en la respuesta real.
- `npm audit` (con y sin `--production`) sobre
  `src/EnterpriseFlow.Web/package.json` — **0 vulnerabilidades**,
  confirmado de nuevo en este Sprint como parte de la auditoría de
  *hardening* (dependencias del frontend, no solo del backend).
- Servidor de verificación temporal detenido y `dist/` de prueba
  eliminado al terminar — no queda ningún artefacto de esta verificación
  en el repositorio (`dist/` ya estaba en `.gitignore` desde antes).

## Qué no se hizo en este Sprint (a propósito)

- Ningún nonce de CSP real — ver razonamiento arriba.
- Ninguna vista/ruta/componente nuevo para navegar el historial de
  Temporal Tables — confirmado el alcance de `r4-01-vision-y-alcance.md`.
