# Release 2, Sprint 11 — DevOps

Cierra Release 2. Mismo alcance que Sprint 11 de Release 1: no construir el
stack de DevOps desde cero — ya existe (`Dockerfile`s, `docker-compose.yml`,
CI, `.editorconfig`) — sino extenderlo para lo que Release 2 activó
(Redis, Hangfire, SignalR, almacenamiento de Documentos) que el stack de
Release 1 nunca necesitó.

## `docker-compose.yml`: 3 cambios reales

- **Servicio `redis` nuevo** (`redis:7-alpine`, sin contraseña — stack
  local/demo). Aditivo, no bloqueante: `Infrastructure.DependencyInjection`
  ya cae a caché en memoria si `Redis__ConnectionString` está ausente
  (Sprint 3), así que el stack sigue arrancando aunque este servicio no
  esté disponible — el mismo comportamiento de degradación elegante ya
  verificado, ahora también aplicable a "el contenedor de Redis no
  levantó", no solo a "Redis no está configurado".
- **`ConnectionStrings__Hangfire` apunta a la *misma* base que
  `ConnectionStrings__Default`, no a una separada.** Decisión encontrada al
  verificar esto de verdad (ver "Verificación" abajo): `UseSqlServerStorage`
  crea las ~10 tablas propias de Hangfire dentro de la base a la que se
  conecta, pero — a diferencia de las migraciones de EF Core — **nunca
  crea la base en sí**. Una base "EnterpriseFlowHangfire" separada habría
  necesitado su propio paso de creación que este stack no tiene (el mismo
  problema que obligó a crear la base a mano con `sqlcmd` en Sprint 3).
  Reutilizar la base de la aplicación lo evita por completo, porque
  `Database.Migrate()` (`Program.cs`, ya wireado desde Sprint 11 de
  Release 1) ya la crea en el primer arranque.
- **Volumen `documents-data` nuevo**, montado en `/app/documents` del
  contenedor `api`, con `Documents__Local__BasePath` apuntando ahí — sin
  esto, cada `docker-compose up` con un contenedor `api` reconstruido
  perdería todos los archivos subidos (F5), igual que perdería la base de
  datos sin el volumen `sqlserver-data` que ya existía.

## Lo que se dejó deliberadamente sin configurar

`Documents:Provider` sigue en su default (`Local`) — los otros tres
proveedores (Azure/S3/Gcs, F5/ADR-0009) necesitan una cuenta cloud real que
este stack local/demo no asume, misma decisión ya explícita al construirlos
(`r2-07b-backend-documentos.md`). SMTP (F6.2) tampoco se configura — sin un
servidor de correo real disponible, `IEmailQueue` resuelve a
`NullEmailQueue` (no-op) en vez de fallar. Ninguna de las dos ausencias
rompe el stack; ambas son casos ya cubiertos por el mismo patrón de
degradación elegante que Redis/Hangfire ya tenían.

## nginx (`src/EnterpriseFlow.Web/nginx.conf`): bloque nuevo para SignalR

El proxy de `/api/` que ya existía no basta para `/hubs/notifications`
(F6.1, ADR-0011) — una conexión SignalR necesita upgrade a WebSocket
(`Connection: upgrade`, `proxy_http_version 1.1`), que un `proxy_pass`
simple no hace por sí solo. Mismo problema, mismo tipo de fix, que
`vite.config.ts` ya necesitó en Sprint 8c para el proxy de desarrollo
(`ws: true` ahí; el bloque `location /hubs/` equivalente aquí, para
producción/demo servida por nginx).

## Verificación

- **Hangfire contra la base compartida, verificado en ejecución real**
  (no solo revisado por lectura): se corrió la Api contra LocalDB real con
  `ConnectionStrings__Hangfire` apuntando a la *misma* base "EnterpriseFlow"
  que EF Core ya migró — log confirmado: `"Hangfire SQL objects installed"`
  seguido de `"Starting Hangfire Server using job storage: 'SQL Server:
  ...@EnterpriseFlow'"`, arranque limpio, sin conflicto entre las tablas de
  EF Core y las de Hangfire en la misma base. Esto es lo que llevó a la
  decisión de "misma base, no una separada" documentada arriba — no fue el
  plan original, fue lo que la verificación real encontró que evitaba un
  problema ya conocido desde Sprint 3.
- `docker-compose.yml` y `nginx.conf` no se pudieron construir/levantar de
  punta a punta en este entorno (sin daemon de Docker disponible aquí) —
  mismo límite ya declarado en Sprint 11 de Release 1. `docker-compose.yml`
  sí se validó como YAML sintácticamente correcto (parseado con `js-yaml`,
  equivalente a la validación con PyYAML que usó Release 1).
- `.github/workflows/ci.yml` no necesitó cambios: el job `backend` ya corre
  `dotnet test EnterpriseFlow.slnx` sin enumerar proyectos por nombre, así
  que cubre los 4 proyectos de test de Release 2 automáticamente; el job
  `frontend` ya usaba `npm install --legacy-peer-deps` desde Release 1 (el
  mismo flag que Sprint 8c volvió a necesitar para instalar
  `@microsoft/signalr`, ya cubierto sin saberlo de antemano).
- `dotnet build`/`dotnet test EnterpriseFlow.slnx` — 218/218 — y
  `dotnet format --verify-no-changes` limpios (sin cambios de código C# en
  este sprint, pero confirmado de todos modos: es el mismo gate que corre
  en CI).

## Cierre de Release 2

Con Sprint 11 completo, **Release 2 (Colaboración y Operación) queda
cerrada** — los 11 pasos del ciclo completo
(análisis → diseño → arquitectura → validación → modelo de dominio → base
de datos → backend → frontend → pruebas → documentación → DevOps)
aplicados a Catálogos, Documentos, Workflow y Notificaciones, con la misma
disciplina de verificación real (no solo revisión de código) que Release 1
estableció, y con cada gap encontrado durante la construcción — desde el
prefijo de tenant en caché (Sprint 4) hasta la base compartida de Hangfire
(este sprint) — corregido y documentado en el momento en que se encontró,
no descubierto después.
