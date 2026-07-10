# Sprint 11 — DevOps

Alcance según `especificcion.md`/`docs/02-roadmap.md`: Dockerfiles,
docker-compose, GitHub Actions (build/test/lint), EditorConfig. Cierra
Release 1 (MVP) — con este Sprint, los 11 pasos del ciclo
`análisis → ... → DevOps` quedan completos para el MVP.

## `.editorconfig` — ya existía, se verificó suficiente

El `.editorconfig` del repo (creado en Sprint 3, arquitectura) ya cubre
indentación, `charset`, convenciones de nombres (`_camelCase` para campos
privados, `PascalCase` para tipos), y las excepciones de StyleCop necesarias
para esas convenciones. No requería trabajo nuevo — salvo un ajuste real
encontrado al conectar `dotnet format` a CI (ver más abajo).

## Dockerfiles

**`src/EnterpriseFlow.Api/Dockerfile`** — build multi-stage (`sdk:8.0` →
`aspnet:8.0`). El contexto de build es la raíz del repo, no la carpeta de la
Api: `EnterpriseFlow.Api` referencia `Domain`/`Application`/`Infrastructure`
por `ProjectReference`, así que `dotnet restore`/`publish` necesitan esos
`.csproj` alcanzables en las mismas rutas relativas que tienen en la
solución. Los `.csproj` se copian antes que el resto del código fuente
(capa de Docker separada) para que `dotnet restore` quede cacheado y no se
repita en cada cambio de código que no toque una versión de paquete.

**`src/EnterpriseFlow.Web/Dockerfile`** — build multi-stage (`node:22-alpine`
→ `nginx:alpine`). Contexto autocontenido (la carpeta del frontend). El
`nginx.conf` que sirve el resultado replica el proxy de Vite: `/api/*` se
reenvía internamente al contenedor `api` sobre la red de compose, así que el
navegador solo ve un origen — la misma razón por la que el proxy de Vite
existe en desarrollo (`vite.config.ts`), y por la que **no hace falta
configurar CORS** para este stack tampoco. Incluye el fallback SPA
(`try_files ... /index.html`) que exige `createWebHistory()` (modo history
de Vue Router) — sin él, refrescar en cualquier ruta que no sea `/` daría
404 directo contra nginx.

Ninguna de las dos imágenes se pudo construir de punta a punta en este
entorno (sin daemon de Docker disponible aquí) — revisadas por lectura
cuidadosa, no por build real. Ver "Verificación" más abajo para lo que sí se
pudo confirmar sin Docker.

## `docker-compose.yml`: sqlserver + api + web

- **`sqlserver`**: `mcr.microsoft.com/mssql/server:2022-latest`, con
  healthcheck que intenta `mssql-tools18` primero (imágenes recientes) y cae
  a `mssql-tools` si no existe — no verificado contra una imagen real
  descargada en este entorno, así que se cubre ambas rutas posibles en vez de
  apostar a una.
- **`api`**: variables de entorno sobrescriben `ConnectionStrings__Default`
  (`Server=sqlserver,1433`, el nombre del servicio de compose — no
  `localhost`, que solo es válido desde fuera de la red de Docker) y
  `Jwt__SigningKey` (el secreto deliberadamente no vive en `appsettings.json`
  desde la revisión de seguridad, [docs/08a-seguridad.md](./08a-seguridad.md);
  aquí se suministra vía `.env`, nunca committeado). Corre en
  `ASPNETCORE_ENVIRONMENT=Development`, no `Production` — a propósito: este
  stack no tiene terminación TLS delante, así que HSTS (que solo se activa
  fuera de Development, ver `Program.cs`) sería activamente incorrecto aquí,
  y mantener Swagger habilitado tiene valor real para explorar un proyecto de
  portafolio, que es el propósito de este stack (un entorno de demo/desarrollo
  local, no un despliegue endurecido).
- **`web`**: depende de `api`; expone `8081:80`.

## Secretos: `.env` + `.env.example`

`SA_PASSWORD` y `JWT_SIGNING_KEY` se leen de un `.env` (gitignored — ya
cubierto por el patrón `.env*` existente, con una excepción explícita
`!.env.example` para que la plantilla sí se versione). Mismo principio que
la revisión de seguridad: ningún secreto real en un archivo que se commitea,
ni siquiera en `docker-compose.yml`.

## Migraciones automáticas al arrancar

`docker-compose up` contra un SQL Server recién creado no tenía, hasta este
Sprint, ninguna forma de que la base de datos terminara con el esquema
aplicado — el README solo documentaba el flujo manual (`dotnet ef database
update` desde el host). Se agregó a `Program.cs`:

```csharp
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}
```

Idempotente (no-op si ya está al día) y excluido explícitamente del
entorno `"Testing"` — `CustomWebApplicationFactory` cambia a SQLite y crea
su esquema con `EnsureCreated()` (sin tabla de historial de migraciones), así
que `Migrate()` ahí competiría con ese mecanismo en vez de complementarlo.
Verificado que los 118 tests siguen pasando con el cambio.

## GitHub Actions: `lint` no eran solo palabras

`.github/workflows/ci.yml`, dos jobs (`backend`, `frontend`), sin necesitar
un repo Git real para escribirse — el workflow es un archivo, se activa el
día que exista un remoto en GitHub.

Job `backend`: restore → **lint** (`dotnet format --verify-no-changes`) →
build → test. Job `frontend`: install → **typecheck+build**
(`npm run build`, que ya corre `vue-tsc -b` como su propio "lint").

### El hallazgo real de este Sprint: `dotnet format` y `dotnet build` no estaban de acuerdo

Antes de escribir el workflow, se corrió `dotnet format --verify-no-changes`
localmente para confirmar que pasaría en CI — y **no pasaba**, con ~50
errores `SA1600`/`SA1601`/`SA1611`/`SA1615`/`SA1618` ("elemento sin
documentar") en `EnterpriseFlow.Infrastructure`, pese a que `dotnet build`
(con `TreatWarningsAsErrors=true`) venía compilando limpio en cada sprint
desde que existe el proyecto. La razón: el comentario del propio
`.editorconfig` ya decía *"sin headers de archivo/XML docs obligatorios"*,
pero la regla que efectivamente exige XML docs (`SA1600` y familia) nunca se
había agregado a la lista de reglas desactivadas — la intención estaba
escrita, la configuración no. `dotnet build` nunca lo marcó como error (por
razones de wiring de analyzers que no se investigaron a fondo, dado que el
fix correcto es el mismo sin importar la causa exacta); `dotnet format` sí,
en cuanto se conectó por primera vez.

Un segundo hallazgo relacionado: `dotnet format style` marcaba
`IDE1006` (violación de convención de nombres) en dos campos `private
const`/`private static readonly` (`AuthEndpoints.RefreshTokenCookieName`,
`AppDbContext.SetGlobalQueryFilterMethodInfo`) — ambos en `PascalCase`, sin
el prefijo `_` que la regla `private_fields_underscore` exige para "campo
privado" en general. `PascalCase` sin prefijo para constantes/estáticos
inmutables ya era la convención de facto (el segundo caso es preexistente,
de antes de este Sprint), solo nunca se había codificado como una regla de
naming distinta — la regla genérica de "campo privado → `_camelCase`" nunca
distinguió const/static-readonly de campos de instancia mutables.

**Corrección, en ambos casos: alinear la configuración con la intención y la
convención ya practicada**, no forzar el código a una regla que nunca reflejó
la intención real del proyecto:

- `.editorconfig`: `SA1600`/`SA1601`/`SA1602`/`SA1611`/`SA1615`/`SA1618` →
  `severity = none`, explícitamente, con el comentario explicando por qué.
- `.editorconfig`: nueva regla de naming `private_constants_pascal_case`
  (para `private const`) y `private_static_readonly_pascal_case` (para
  `private static readonly`), ambas con estilo `PascalCase` sin prefijo —
  más específicas que la regla general de campos privados, así que Roslyn
  las prioriza para esos casos sin afectar el resto de los campos privados
  mutables (que siguen requiriendo `_camelCase`).

`dotnet format EnterpriseFlow.slnx --verify-no-changes` pasa limpio después
de esto — sin renombrar ni un solo símbolo existente, solo corrigiendo la
configuración para que dijera lo que el proyecto ya hacía.

## Verificación

- `dotnet build` + `dotnet test` (118/118) limpios tras el cambio de
  migración automática y los ajustes de `.editorconfig`.
- `dotnet format EnterpriseFlow.slnx --verify-no-changes` limpio — el mismo
  comando que corre en CI, confirmado localmente antes de escribir el
  workflow, no después.
- `docker-compose.yml` y `.github/workflows/ci.yml` validados como YAML
  sintácticamente correcto (parseados con PyYAML) — no se pudieron ejecutar
  de punta a punta (build de imágenes, `docker compose up`, corrida real del
  workflow en GitHub) en este entorno, que no tiene un daemon de Docker ni
  push a un remoto de GitHub disponible. Dicho explícitamente en vez de
  reportarlo como probado cuando no lo fue.
