# Release 2, Sprint 3 — Arquitectura (esqueleto)

Mismo alcance que Sprint 3 de Release 1: **esqueleto, no una feature real
todavía** — las interfaces cross-cutting, el wiring de infraestructura y las
convenciones que Sprint 4 (Validación) va a ejercitar con el primer slice
vertical real de Release 2. Ninguna entidad de Domain se agrega en este
sprint (eso es Sprint 5, Modelo de Dominio) — mismo criterio que Release 1
aplicó.

## Qué se agregó

**`Application.Abstractions`** (interfaces nuevas, cero implementación):
`ICacheableQuery`/`IInvalidatesCache` (ADR-0012), `IDocumentStorageProvider`
(ADR-0009), `IRealtimeNotifier` (ADR-0011) — mismo patrón que
`ITokenService`/`IPasswordHasher` ya establecieron: Application define el
contrato, Infrastructure lo implementa, Application nunca ve el framework
concreto detrás.

**`CachingBehavior`/`CacheInvalidationBehavior`** (`Application.Common.Behaviors`):
implementación completa del diseño de ADR-0012, registrados en
`Application/DependencyInjection.cs` en el orden Authorization → Validation
→ Caching → CacheInvalidation. Ninguna Query implementa `ICacheableQuery`
todavía (eso llega con Catálogos, F8.2) — igual que `AuthorizationBehavior`
existió antes de que `Company` existiera en Release 1.

**Infrastructure**:
- `Realtime/NotificationHub.cs` — Hub de SignalR, `[Authorize]`, sin métodos
  invocables por el cliente (solo push servidor→cliente).
- `Realtime/JwtSubUserIdProvider.cs` — `IUserIdProvider` que lee el claim
  `sub` directamente, igual que `JwtCurrentUserService`. Sin esto,
  `Clients.User(userId)` nunca habría encontrado ninguna conexión: el
  `IUserIdProvider` por defecto de SignalR busca `ClaimTypes.NameIdentifier`,
  que `MapInboundClaims = false` (Sprint 7a) deja de existir en los claims.
  Encontrado por inspección del comportamiento por defecto de SignalR contra
  la configuración JWT ya existente, no por un fallo en ejecución — el mismo
  tipo de bug que ya mordió al proyecto una vez (ver `08-frontend.md`) se
  evitó aquí revisando la interacción antes de escribir el resto del código.
- `Realtime/SignalRNotifier.cs` — implementa `IRealtimeNotifier` sobre
  `IHubContext<NotificationHub>`.
- `DependencyInjection.cs`: Redis (`AddStackExchangeRedisCache` si
  `Redis:ConnectionString` está configurado, si no `AddDistributedMemoryCache()`
  como *fallback* — nunca deja `IDistributedCache` sin implementación),
  Hangfire (`AddHangfire`+`AddHangfireServer` solo si
  `ConnectionStrings:Hangfire` está configurado — **se omite por completo**,
  no se apunta a un valor falso, porque `UseSqlServerStorage` intenta crear
  su esquema de forma inmediata al arrancar y fallaría duro contra una base
  inexistente en vez de degradar con gracia), `AddSignalR()`.

**Api** (`Program.cs`): `OnMessageReceived` en `JwtBearerOptions` — lee el
JWT desde el query string `access_token` solo para peticiones bajo
`/hubs/*` (los navegadores no pueden fijar el header `Authorization` en una
conexión WebSocket nativa, patrón estándar de SignalR+JWT). Mapeo de
`/hubs/notifications` (`RequireAuthorization()`) y `/hangfire` (Dashboard,
gateado a `Development` + Hangfire configurado — misma razón que Swagger:
valor real para explorar el proyecto, sin exponerlo fuera de desarrollo).

**Paquetes nuevos** (`Directory.Packages.props`, central): 
`Microsoft.Extensions.Caching.Abstractions` (Application — liviano, sin
arrastrar todo ASP.NET Core solo por `IDistributedCache`),
`Microsoft.Extensions.Caching.StackExchangeRedis`, `Hangfire.AspNetCore`,
`Hangfire.SqlServer` (Infrastructure). SignalR no necesita paquete —
`Microsoft.AspNetCore.SignalR` ya llega vía el `FrameworkReference` a
`Microsoft.AspNetCore.App` que Infrastructure ya tenía desde Release 1.

## Verificación

No solo revisión de código — donde fue posible, ejecución real:

- **Hangfire contra LocalDB (real, no simulado)**: se creó una base
  `EnterpriseFlowHangfireTest` en `(localdb)\MSSQLLocalDB` y se arrancó la
  Api apuntando `ConnectionStrings:Hangfire` ahí. Log confirmado: *"Start
  installing Hangfire SQL objects..." → "Hangfire SQL objects installed."* →
  el servidor de Hangfire arrancó sus 8 dispatchers
  (`ServerWatchdog`, `Worker`, `DelayedJobScheduler`, etc.) sin error. Se
  intentó primero sin crear la base a propósito: falló con un error claro de
  login (`Cannot open database`) sin tumbar el resto de la aplicación —
  confirma que un Hangfire mal configurado degrada, no crashea el proceso
  entero (`/health` siguió respondiendo `Healthy` durante el fallo).
- **Smoke test end-to-end con todo el wiring nuevo activo**: registrar
  tenant → login sigue devolviendo un JWT válido con todos los permisos —
  nada de lo agregado rompió el flujo de Identidad de Release 1.
- **`/hangfire` (Dashboard)**: `200 OK` en Development con Hangfire
  configurado.
- **`/hubs/notifications/negotiate`**: `401 Unauthorized` sin token —
  confirma que el Hub está mapeado y protegido, no abierto por descuido.
- **`/swagger`, `/health`**: sin regresión.
- Suite completa: **118/118 tests**, `dotnet format --verify-no-changes`
  limpio.
- **Redis: no verificado en ejecución real** — no hay una instancia de Redis
  disponible en este entorno (sin Docker, sin Redis local instalado). El
  camino de *fallback* (`AddDistributedMemoryCache()`) sí se verificó
  indirectamente: es exactamente lo que corrió durante toda la suite de
  tests y el smoke test manual (ninguno configuró `Redis:ConnectionString`),
  y no hubo ningún error relacionado con caché. El camino de Redis real
  queda pendiente de verificación cuando exista una instancia alcanzable
  (Sprint de DevOps de Release 2, cuando `docker-compose.yml` sume el
  servicio `redis`) — dicho explícitamente en vez de darlo por probado.

## Qué no se hizo en este sprint (a propósito)

- Ninguna entidad de Domain (`Document`, `WorkflowDefinition`, `Notification`,
  `CatalogDefinition`) — Sprint 5.
- Ninguna carpeta `Application/Features/{Documents,Notifications,Workflow,Catalogs}/`
  todavía — Sprint 4 crea la primera con un slice real y probado, mismo
  criterio que Release 1 (Companies apareció en Sprint 4, no en Sprint 3).
- `docker-compose.yml` no gana el servicio `redis` ni la variable
  `ConnectionStrings__Hangfire` todavía — eso es Sprint 11 de Release 2
  (DevOps), consecuencia ya anticipada en ADR-0008.
- Filtro de autorización propio para el Dashboard de Hangfire (hoy usa el
  comportamiento por defecto de Hangfire.AspNetCore, gateado solo por
  `IsDevelopment()`) — aceptable para el alcance actual (mismo tratamiento
  que Swagger), revisitable si el Dashboard necesitara exponerse fuera de
  Development en algún momento.
