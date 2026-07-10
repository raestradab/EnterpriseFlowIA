# Release 4, Sprint 11 — DevOps

Último Sprint de Release 4: las tres piezas de madurez de DevOps que
`r4-01-vision-y-alcance.md` (sección 2) asignó a este Release —
CodeQL (F13.3), Dependabot (F13.4), Conventional Commits + SemVer +
Release Notes (F13.5) — más cerrar la última pieza explícitamente diferida
de Sprint 9: SQL Server como servicio real de CI.

## 1. SQL Server como servicio de CI (cierra el diferido de Sprint 9)

`ci.yml` ganó un servicio `sqlserver` (`mcr.microsoft.com/mssql/server:2022-latest`,
Linux — LocalDB no existe fuera de Windows) con *health check* propio antes
de que el job `Test` arranque. El filtro `Category!=RequiresSqlServer`
(Sprint 9) se quitó del paso `Test` — `EnterpriseFlow.Infrastructure.SqlServerTests`
corre en el mismo barrido que el resto de la suite.

`SqlServerFixture.ConnectionString` (antes una constante fija a LocalDB)
pasó a leer `SQLSERVERTESTS_CONNECTION_STRING` si está presente, con la
cadena de LocalDB como *fallback* — así una máquina de desarrollo Windows
sigue sin necesitar ninguna variable de entorno nueva, y `ci.yml` apunta al
servicio Linux vía esa misma variable.

**Verificado de verdad, no solo revisado**: el mecanismo de *override* se
probó con dos casos reales, no solo leyendo el código —
`SQLSERVERTESTS_CONNECTION_STRING` apuntando a la misma LocalDB (4/4 pasan,
confirma que la variable se lee) y apuntando a un `Server=localhost,1433`
con contraseña incorrecta (4/4 fallan con un error de conexión real,
confirma que no hay un *fallback* silencioso enmascarando la variable). La
contraseña `CiOnly_Test_Passw0rd!` en `ci.yml` es una credencial
desechable para una base de datos efímera que no sale de ese job — no un
secreto real, documentado inline como tal.

**Límite de verificación real de este entorno**: `E:\CodigoFuente\demo` no
es un repositorio Git (`git status` → *"not a git repository"*) — no hay
remoto, no hay Actions corriendo, y este entorno tampoco tiene un daemon
de Docker (`docker --version` falla). El contenedor de servicio SQL Server
en sí, y todo el resto de `ci.yml`, quedan validados como YAML
sintácticamente correcto (`python -c "import yaml; ..."`) pero nunca
ejecutados de punta a punta contra GitHub Actions real — el mismo límite
que `docs/11-devops.md` ya declaró explícitamente desde Release 1 para
este mismo archivo, no una limitación nueva de este Sprint.

## 2. CodeQL (F13.3)

`.github/workflows/codeql.yml` — análisis estático de seguridad (SAST)
nativo de GitHub Actions, sin cuenta externa (razón ya fijada en
`r4-01-vision-y-alcance.md`, sección 3, al descartar SonarCloud/SonarQube).
Matriz de dos lenguajes, los dos que el repo realmente tiene: `csharp`
(necesita una build real — CodeQL no puede extraer de un lenguaje
compilado solo leyendo texto, a diferencia de TypeScript) y
`javascript-typescript` (el frontend, sin paso de build). Corre en push,
PR, y semanalmente (una CVE nueva sobre una dependencia ya presente en el
repo necesita una corrida sin que nada haya cambiado en el código).

## 3. Dependabot (F13.4)

`.github/dependabot.yml` — tres ecosistemas reales del repo:
`nuget` (raíz, resuelve versiones desde `Directory.Packages.props` pese a
que los `.csproj` individuales no fijan versión), `npm`
(`src/EnterpriseFlow.Web`), y `github-actions` (los propios workflows —
`actions/checkout`, `github/codeql-action`, etc. fijan versión mayor y
necesitan quien los actualice). Renovate quedó descartado desde
`r4-01-vision-y-alcance.md` (necesita una GitHub App instalada; Dependabot
es nativo).

## 4. Conventional Commits + SemVer + Release Notes automáticas (F13.5)

`CONTRIBUTING.md` (nuevo) documenta la convención de Conventional Commits
usada en este repo y por qué importa más allá de legibilidad: es el input
real que consume la automatización, no solo una guía de estilo.

`release-please` ([googleapis/release-please-action](https://github.com/googleapis/release-please-action),
Apache-2.0) elegido sobre `semantic-release`: hace lo mismo (versión
semántica + CHANGELOG + GitHub Release, todo derivado de Conventional
Commits) sin una cadena de plugins Node que configurar — un único Action
de Marketplace, `release-type: simple` (no hay un paquete npm/NuGet que
publicar, solo una aplicación versionada). `release-please-config.json` +
`.release-please-manifest.json` (semilla `0.1.0` — este repo nunca tuvo un
tag real, así que no hay una versión SemVer previa real de la cual partir;
0.x refleja honestamente que la superficie pública aún no se declaró
estable bajo ese esquema).

`CHANGELOG.md` (Sprint 10) queda con una nota explícita al inicio: de aquí
en adelante, `release-please` antepone sus propias secciones por versión
generadas de los commits reales; las secciones por Release existentes
(historial manual, Releases 1-4) no se reescriben — coexisten como el
registro previo a la automatización.

**Mismo límite de verificación que la sección 1**: sin repositorio Git
real, no hay commits desde los cuales `release-please` pueda calcular una
versión — el workflow y su configuración quedan validados sintácticamente
(YAML + JSON parseados con éxito), no ejecutados. Documentado
explícitamente en `CONTRIBUTING.md`, no solo aquí.

## 5. Hallazgo tardío: `docs/02-roadmap.md` desactualizado (Releases 3 y 4)

Al escribir el `CHANGELOG.md` de Sprint 10 se afirmó que
`docs/02-roadmap.md` ya se había corregido ese mismo Sprint — una
afirmación falsa, detectada recién al cerrar este Sprint 11 y releer el
archivo real: la sección de Release 4 seguía diciendo *"Sprint 1 (Análisis)
completo"*, sin mencionar los Sprints 2-11. Revisando el mismo archivo se
encontró el problema gemelo en la sección de Release 3: *"Sprints 1-9
completos"*, pese a que ese Release cerró formalmente en el Sprint 11
(`r3-11-devops.md`). Ambas corregidas ahora, con el mismo nivel de detalle
por Sprint que el resto del archivo ya tenía para Release 1/2. La entrada
incorrecta del `CHANGELOG.md` también se corrigió — documentar un hallazgo
que no ocurrió es peor que no documentarlo.

## Verificación

- `dotnet build`/`dotnet test EnterpriseFlow.slnx` (sin filtro) —
  **285/285**, incluyendo `EnterpriseFlow.Infrastructure.SqlServerTests`
  corriendo contra LocalDB real vía el mismo mecanismo de *fallback* que
  usará `ci.yml`.
- `dotnet format EnterpriseFlow.slnx --verify-no-changes` limpio.
- Los 5 archivos YAML/JSON nuevos o modificados
  (`ci.yml`, `codeql.yml`, `dependabot.yml`, `release-please.yml`,
  `release-please-config.json`, `.release-please-manifest.json`)
  parseados con éxito (`PyYAML`/`json`) — sintaxis confirmada, ejecución
  de punta a punta fuera del alcance verificable de este entorno (sección 1).

## Cierre de Release 4 (Hardening Empresarial)

Sprints 1-11 completos. Alcance real entregado: historial de cambios vía
Temporal Tables (HU-102, `Project`/`ProjectTask`), tracing distribuido con
OpenTelemetry (exportador local), BenchmarkDotNet sobre caminos calientes
reales, auditoría de cobertura con gate real en CI, corrección de dos
gaps reales encontrados auditando contra `especificcion.md` (Response
Compression nunca activado pese al ADR que lo daba por hecho; ADR-0001
punto 6 nunca resuelto), `CHANGELOG.md` (F14.4, pedido desde Release 1),
y la madurez de DevOps de este Sprint (CodeQL, Dependabot, Conventional
Commits/SemVer/Release Notes). Redirigido explícitamente al iniciar el
Release (RabbitMQ/MassTransit, Elastic/Application Insights, SignalR a
escala — sin caso de uso real ni infraestructura verificable en este
entorno) con la misma trazabilidad que el servidor MCP propio recibió en
Release 3 — nada se descartó en silencio.
