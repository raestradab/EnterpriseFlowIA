# Sprint 8 — Frontend

Vue 3 + TypeScript + Vite consumiendo la API real (Sprint 7). Alcance:
Composition API, Vuetify, Pinia, Vue Router (lazy loading), Axios, i18n
(es/en), dark mode, menú dinámico (HU-005), toast notifications, skeleton
loading, validación de formularios.

## Estructura

```
src/EnterpriseFlow.Web/
  src/
    api/            # axios client + interceptor de refresh, un módulo por recurso
    stores/          # Pinia: auth, notifications
    router/          # rutas con lazy loading + guard de autenticación/permisos
    layouts/         # DefaultLayout: nav drawer dinámico, dark mode, i18n
    views/           # auth, dashboard, companies, clients, projects, tasks
    locales/         # es.ts, en.ts
    types/           # DTOs espejados a mano desde Application/Features/**
```

## Decisiones

- **Sin generador de cliente OpenAPI** (NSwag/openapi-typescript): los tipos en
  `types/index.ts` están escritos a mano. Con la API todavía cambiando de
  forma (nuevos módulos en cada sprint), generar el cliente ahora significaría
  regenerarlo constantemente por poco beneficio — se reconsidera en Release 2
  cuando la superficie de la API se estabilice.
- **Refresh token con "single-flight"**: si varias peticiones reciben 401 al
  mismo tiempo (token expirado), todas esperan una única llamada a
  `/api/auth/refresh`, no una por petición — el refresh token es de un solo
  uso y rota (HU-002), así que una segunda llamada concurrente siempre
  fallaría por detección de reuso.
- **`tokenStorage.ts` separado del store de Pinia**: el cliente Axios necesita
  leer/escribir el token, y el store también — desacoplarlos en un módulo
  plano evita una dependencia circular entre "el cliente HTTP necesita el
  store" y "el store necesita el cliente HTTP". Desde la revisión de
  seguridad del 2026-07-07 solo guarda el *access token* (15 min de vida); el
  refresh token ya no pasa por aquí en absoluto — ver más abajo y
  [ADR-0007](./adr/ADR-0007-refresh-token-en-cookie-httponly.md).
- **Sin generador de tipos para los enums del backend**: se usa el patrón
  `as const` + tipo derivado en vez de `enum` de TypeScript, porque el
  `tsconfig` scaffolded por Vite trae `erasableSyntaxOnly` activado (no
  permite `enum`, que no es sintaxis puramente borrable). Los valores
  numéricos deben coincidir exactamente con los enums de C# (Sytem.Text.Json
  los serializa como número, sin `JsonStringEnumConverter`).

## Bug encontrado en smoke test manual — el más importante hasta ahora

Con la Api real corriendo, `GET /api/auth/me` devolvía `userId` en
`00000000-0000-0000-0000-000000000000` a pesar de un login exitoso con un
JWT correctamente emitido (verificado decodificando el token: el claim `sub`
sí traía el id correcto).

**Causa**: el `JwtBearerHandler` de ASP.NET Core remapea por defecto el claim
`sub` al URI legado `ClaimTypes.NameIdentifier`, a menos que se desactive
explícitamente (`options.MapInboundClaims = false`). `JwtCurrentUserService`
buscaba literalmente `JwtRegisteredClaimNames.Sub` ("sub"), que ya no existía
en `HttpContext.User.Claims` tras el remapeo — encontraba `null` y devolvía
`Guid.Empty`.

**Por qué no lo atrapó ningún test previo**: `RegisterTenant_Then_Login_Grants_A_Working_Token`
(Sprint 7a) llama a `/api/auth/me` y sí revisaba la respuesta — pero solo
afirmaba `Permissions.Should().Contain(...)`. Los permisos usan un claim
personalizado (`"permission"`), no remapeado por el mecanismo por defecto, así
que la prueba pasaba igual. Nunca se afirmó el valor de `UserId`. Esto no es
un detalle menor: **todo el estampado de auditoría** (`CreatedBy`/`ModifiedBy`
en `AuditableEntitySaveChangesInterceptor`) depende de
`ICurrentUserService.UserId`, así que cada registro creado por un usuario real
autenticado por JWT quedaba con auditoría en ceros — silenciosamente, sin que
ningún test lo notara.

Corregido con `options.MapInboundClaims = false` en `Program.cs`, y reforzada
la prueba original para afirmar `me.UserId` contra el id real devuelto por el
registro — no solo el código de estado ni una lista de permisos que por
casualidad no se veía afectada.

Este es ya el **cuarto** bug de esta clase en el proyecto (Sprint 4: header
dropeado en el factory de test; Sprint 7a: `Include` faltante en
Login/Refresh; Sprint 7b: `Include` faltante en AddProjectMember/AssignRole +
`ValueGenerated` mal inferido; ahora: remapeo de claims) — en los cuatro casos
un test que solo miraba el código de estado HTTP habría pasado igual. La
lección se repite lo suficiente como para tratarla como regla: **cualquier
prueba de un flujo de extremo a extremo debe afirmar el contenido de la
respuesta, no solo que la petición "tuvo éxito"**.

## Verificación

- `npm run build` (typecheck con `vue-tsc` + build de producción con Vite):
  pasa limpio.
- Smoke test manual vía `curl` contra la Api real (LocalDB): registrar tenant
  → login → `GET /api/auth/me` con el token → crear Empresa → listar Empresas.
- **Verificación visual completa en navegador real** (Chrome, vía
  claude-in-chrome), flujo de punta a punta contra la Api real (LocalDB):
  1. Registrar organización → toast de éxito → redirección a login.
  2. Iniciar sesión → dashboard con menú dinámico completo (admin tiene todos
     los permisos) y KPIs en 0 (tenant nuevo).
  3. Dark mode: toggle inmediato, sin recarga.
  4. Crear Empresa → aparece en la tabla.
  5. Crear Cliente asociado a esa Empresa (el select carga la Empresa real
     desde la Api).
  6. Crear Proyecto asociado a ese Cliente.
  7. Dentro del detalle del Proyecto: crear una Tarea → **intentar cerrar el
     proyecto → rechazado con el mensaje real del dominio** ("Project '...'
     cannot be closed because it still has open tasks.", HU-021) → completar
     la tarea → cerrar el proyecto → éxito.
  8. Listado global de Tareas con el nombre de proyecto resuelto y chip de
     estado.
  9. Responsive: ventana a 414×896 (tamaño móvil) — el nav drawer colapsa a
     ícono de hamburguesa y se abre como overlay.
  10. Cambio de idioma ES/EN en vivo, sin recargar.
  11. Logout → redirección a login, sesión limpiada.

  Los 8 archivos de datos usados en la prueba (Empresa, Cliente, Proyecto,
  Tarea) fueron creados y leídos exclusivamente a través de la UI contra la
  Api real — ninguno fue insertado manualmente.

### Dos bugs reales encontrados durante la verificación visual (no por curl ni por tests)

1. **Encabezados de tabla no reactivos al idioma.** Al cambiar de ES a EN, el
   menú y los botones se tradujeron correctamente, pero los encabezados de
   `v-data-table` en las 4 vistas de lista (Empresas, Clientes, Proyectos,
   Tareas) y las opciones de rol/prioridad en `ProjectDetailView`/`TasksListView`
   seguían en español. Causa: `const headers = [...]` llama a `t()` una sola
   vez al montar el componente, sin ser reactivo a cambios posteriores de
   `locale`. Corregido envolviendo cada uno en `computed(() => [...])` — el
   patrón correcto quedó comentado inline para que no se repita al agregar
   nuevas vistas.
2. **API de tema de Vuetify obsoleta.** `theme.global.name.value = 'dark'`
   generaba un warning de deprecación en consola en cada toggle; la API nueva
   es `theme.change('dark')` y `theme.current` (sin `.global`). Corregido en
   `DefaultLayout.vue`.

Ninguno de los dos bugs habría aparecido en `npm run build` (typecheck) ni en
pruebas de backend — solo se detectan interactuando de verdad con la UI, que
es exactamente la razón por la que esta verificación se hizo antes de cerrar
el sprint.

## Corrección de seguridad post-Sprint (2026-07-07)

La revisión de seguridad ad-hoc pedida por el usuario (ver
[docs/08a-seguridad.md](./08a-seguridad.md)) encontró que el refresh token
(30 días de vida) se guardaba en `localStorage`, legible por cualquier
JavaScript de la página. Se movió a una cookie `HttpOnly` que el navegador
adjunta solo — `client.ts` ahora usa `axios.create({ withCredentials: true })`
y `POST /api/auth/refresh` ya no envía el token en el body; `tokenStorage.ts`
perdió `getRefreshToken`/`setTokens` (ahora solo `setAccessToken`).
`stores/auth.ts#logout` pasó a ser async: además de limpiar el estado local,
llama a `POST /api/auth/logout` para revocar el token server-side (best-effort
— si la llamada de red falla, el estado local igual se limpia). Detalle de la
decisión completo en [ADR-0007](./adr/ADR-0007-refresh-token-en-cookie-httponly.md).

Verificado con `fetch()` directo en un navegador real a través del proxy de
Vite (login → `document.cookie` no expone el token → refresh sin body
funciona → logout → refresh posterior falla con 401). No se pudo verificar el
mismo flujo con clics reales sobre el formulario de registro en esta sesión
— referencias de DOM obsoletas en la herramienta de automatización del
navegador usada, no un problema del código.

## Deuda técnica reconocida

- Sin code-splitting fino de Vuetify: el bundle de producción supera 500 KB
  en un solo chunk (advertencia de Vite, no error). Aceptable para portafolio;
  se revisita si el rendimiento real llega a importar.
- El toolchain de Node en esta máquina (`v20.18.0`) no soporta Vite 8/Rolldown
  (requiere Node ≥20.19); se fijó `vite: ^6.3.5` con `--legacy-peer-deps` para
  sortear un conflicto de peer dependency opcional de `vue-router@5`. No
  afecta el código de la app, pero documentado por si se reproduce en otra
  máquina.
