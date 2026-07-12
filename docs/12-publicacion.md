# Publicación en GitHub — CI, validación de PR, releases y CD

Este documento no es un Sprint más del ciclo `análisis → ... → DevOps` (ese
ciclo ya cerró con Release 4) — es el *runbook* real de cómo este
repositorio, que hasta ahora vivió solo en disco local sin control de
versiones, pasa a GitHub con el pipeline completo activo.

## 0. Punto de partida

Hasta este punto, `E:\CodigoFuente\demo` nunca fue un repositorio Git
(`git status` fallaba con *"not a git repository"*) — dato ya declarado
explícitamente desde `docs/11-devops.md` (Release 1) y repetido en cada
Sprint de DevOps posterior, porque limitaba qué se podía verificar de
`ci.yml`/`docker-compose.yml` (sintaxis sí, ejecución real no). Esa
limitación se resuelve recién ahora.

## 1. Repositorio

- `git init` corrido sobre el directorio existente — no un clon, no un
  repo nuevo vacío al que se le copian archivos.
- `.gitignore` auditado antes del primer commit: sin secretos reales
  (`appsettings.json`/`appsettings.Development.json` solo tienen
  contraseñas placeholder o auth integrada de Windows — el `Jwt:SigningKey`
  real nunca vive ahí, ver ADR-0007/`08a-seguridad.md`), sin `bin/`/`obj/`/
  `node_modules/`/artefactos de cobertura o benchmarks. Se agregó `.claude/`
  (estado local de sesión de Claude Code — no es código del proyecto).
- **Un solo commit inicial**, no un historial retroactivo fabricado por
  Sprint — decisión explícita del usuario: un historial con 40+ commits,
  todos con el timestamp de hoy, describiendo un desarrollo "de varios
  meses" sería una señal falsa para cualquier revisor técnico que lo mire
  de cerca. La narrativa real de cómo se construyó (Sprint por Sprint, con
  hallazgos y decisiones) ya vive en `docs/`, que sí es honesto sobre
  cuándo y cómo pasó cada cosa.

## 2. Integración Continua (ya existía, ahora se activa de verdad)

`.github/workflows/ci.yml` (Release 1, madurado en Release 4) — jobs
`backend` (lint, build, test con cobertura, gate ≥90%) y `frontend`
(typecheck + build). Sin cambios de contenido en este paso; lo que cambia
es que por primera vez corre contra GitHub Actions real, no solo se valida
como YAML sintácticamente correcto.

## 3. Validación de Pull Request

- `.github/pull_request_template.md` (nuevo) — checklist mínimo:
  formato, tests, build de frontend si aplica, cobertura, Conventional
  Commits, backlog al día si el alcance cambió.
- **Branch protection sobre `main`** — esto no se puede hacer desde este
  entorno (necesita la API de GitHub autenticada; no hay `gh` ni token
  disponibles acá). Pasos manuales, una sola vez, en
  `github.com/<owner>/<repo>/settings/branches`:
  1. *Add branch protection rule* → pattern `main`.
  2. *Require a pull request before merging* — activado.
  3. *Require status checks to pass before merging* — activado; buscar y
     marcar como requeridos los checks `Backend (lint · build · test)` y
     `Frontend (typecheck · build)` (aparecen en la lista recién después
     de que `ci.yml` corrió al menos una vez sobre algún PR/push).
  4. *Require branches to be up to date before merging* — activado (evita
     mergear un PR verde contra una versión vieja de `main`).
  5. Aprobación de revisor: **no activada** — proyecto de un solo
     desarrollador; exigirla bloquearía todos los PRs propios sin un
     revisor externo real. Reconsiderar si se suma un colaborador.

## 4. Versionado y Release Notes automáticas

Ya construido en Release 4 Sprint 11
([`r4-11-devops.md`](./r4-11-devops.md)): `release-please`
(`.github/workflows/release-please.yml`) lee Conventional Commits desde
`main`, mantiene un PR con la próxima versión + `CHANGELOG.md`, y al
mergearlo crea el tag + GitHub Release. Esa era la pieza que no se podía
verificar de punta a punta sin un repo real — ahora sí puede, una vez que
haya al menos un commit con formato Conventional Commits después del
commit inicial.

## 5. Despliegue Continuo (CD) — nuevo en este documento

`.github/workflows/publish.yml` (nuevo): se dispara con `release:
types: [published]` — es decir, exactamente el evento que `release-please`
genera al mergear su PR de versión. Build + push de las dos imágenes
Docker que `docker-compose.yml` ya construye localmente
(`src/EnterpriseFlow.Api/Dockerfile`, `src/EnterpriseFlow.Web/Dockerfile`)
a **GitHub Container Registry** (`ghcr.io/<owner>/enterpriseflow-api`,
`ghcr.io/<owner>/enterpriseflow-web`), taggeadas con la versión semántica
y `latest`.

**Por qué GHCR y no un despliegue real a un servidor público**: decisión
explícita del usuario al planear esta publicación. Un despliegue real
necesita un host (VPS/PaaS/cloud) con credenciales propias que este
entorno no tiene — mismo criterio que ya aplicó ADR-0001 a Elastic/App
Insights/RabbitMQ: no se construye contra infraestructura que no se puede
verificar de verdad. GHCR sí es 100% verificable con lo que GitHub ya da
gratis (`GITHUB_TOKEN`, sin cuenta ni secreto nuevo) — el pipeline queda
completo y ejecutable de punta a punta; publicar las imágenes a un host
real es un paso adicional que se agrega el día que exista ese host, sin
tocar este workflow (`docker pull ghcr.io/...` funciona desde cualquier
destino con acceso al registro).

## 6. Secuencia real para activar todo, en orden

1. Crear un repositorio vacío en GitHub (sin README/licencia/`.gitignore`
   — ya existen acá) y agregarlo como remoto.
2. Push del commit inicial a `main`.
3. Configurar branch protection (sección 3) — recién ahora aparecen los
   checks de `ci.yml` en la lista, porque necesitan haber corrido una vez.
4. Cualquier cambio nuevo, vía Pull Request (no push directo a `main`) —
   confirma que la protección de rama y el gate de CI funcionan de verdad.
5. Una vez que `release-please` acumule commits Conventional Commits en
   `main`, abre su PR de versión automáticamente; al mergearlo, crea el
   Release, que dispara `publish.yml`.

## 7. Verificación real, post-push

`git remote add` + `git push` no salieron limpios al primer intento: el
repo que se creó en GitHub no estaba vacío — traía un commit *"Initial
commit"* generado por GitHub agregando un `.gitignore` genérico (429
líneas, sin relación con este proyecto). `git push` lo rechazó
(`! [rejected] main -> main (fetch first)`). Confirmado con `git fetch` +
`git show --stat origin/main` que ese commit no tenía trabajo real —
decisión explícita del usuario: `git push --force` para reemplazarlo con
el commit inicial real de este repo, en vez de mergear y terminar con dos
commits (contradiciendo la preferencia de "un solo commit limpio").

Con el push ya real, se pudo consultar la API pública de GitHub
(`api.github.com/repos/.../actions/runs`, sin autenticación — funciona
para repos públicos) para confirmar ejecución real, no solo sintaxis:

- **`CI` y `CodeQL` sobre `main` mismo, en ambos commits pusheados**
  (`91534aa` inicial y `2dd4b12` del fix de Dependabot): **completed /
  success**, los dos. `Release Please` también: **completed / success**
  (sin abrir PR de versión todavía — el primer commit es `chore:`, que no
  dispara un bump de versión bajo Conventional Commits, comportamiento
  esperado, no un fallo).
- **Hallazgo real, no anticipado**: el primer escaneo de Dependabot abrió
  **21 PRs individuales** (un paquete desactualizado por PR — las
  versiones fijadas en `Directory.Packages.props`/`package.json` nunca se
  habían tocado desde que se fijaron). Cada PR disparó `ci.yml` y
  `codeql.yml` (ambos con `pull_request:` sin filtro de rama, correcto en
  sí — cualquier PR real debe validarse), generando 30+ corridas en cola
  de golpe. Corregido agregando `groups:` a `dependabot.yml` (una PR por
  ecosistema, no por paquete) — verificado que funcionó de verdad:
  Dependabot cerró los 21 PRs individuales por su cuenta y los reemplazó
  con exactamente 3 PRs agrupados (`nuget`, `npm`, `github-actions`) en
  su siguiente ciclo, sin intervención manual.
- **De esos 3 PRs agrupados, 2 fallan `CI` de verdad** (el paso
  `Restore` de NuGet) — el bump agrupado de 25 paquetes NuGet y el de 6
  paquetes npm introducen conflictos de versión reales, no un problema de
  configuración (el PR de `github-actions`, sin tocar NuGet/npm, pasa
  limpio). No se investigó el conflicto exacto línea por línea en este
  documento — lectura de logs completos requiere permisos de administrador
  sobre el repo que este entorno no tiene (`403 Must have admin rights`
  al pedir `actions/jobs/{id}/logs` sin autenticación); el usuario sí
  puede verlos desde la UI de GitHub. Es, en sí, la prueba de que el gate
  de PR funciona: agarra bumps automáticos rotos antes de que lleguen a
  `main`, que es exactamente para lo que existe.
- **Sin verificar todavía**: `publish.yml` (CD) — necesita un primer
  Release real, que a su vez necesita un commit `feat:`/`fix:` después
  del commit inicial para que `release-please` tenga algo que versionar.
  Branch protection sobre `main` (sección 3) — pendiente de que el
  usuario la configure manualmente, mismo límite de acceso que el resto
  de este documento.
