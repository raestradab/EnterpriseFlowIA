# Roadmap por Releases y Sprints

La especificación original define la secuencia de trabajo (`FORMA DE TRABAJAR`)
como una serie de fases que deben completarse en orden:

> análisis → diseño → arquitectura → validación de arquitectura → modelo de
> dominio → base de datos → backend → frontend → IA → pruebas → documentación →
> DevOps

Esta secuencia se interpreta como el **ciclo interno de cada Release**, no como
un único paso global — de lo contrario "IA" y "pruebas" solo se abordarían una
vez al final de todo el proyecto, lo cual contradice la exigencia de que "cada
Sprint finalice completamente antes de iniciar el siguiente".

## Release 1 — Foundation (MVP)
Objetivo: producto funcional end-to-end, delgado en alcance, completo en calidad.

| Sprint | Fase | Entregable |
|---|---|---|
| 1 | Análisis | Vision, Epics, Features, Historias de Usuario, Casos de Uso (este conjunto de documentos) |
| 2 | Diseño | Diagramas de contexto/contenedor (C4), decisiones clave (ADRs 0001-000N) |
| 3 | Arquitectura | Esqueleto de solución: Clean Architecture + Vertical Slices, capas, convenciones |
| 4 | Validación de arquitectura | Prueba de concepto de un slice vertical completo (1 caso de uso end-to-end) + revisión contra checklist SOLID/Clean Architecture |
| 5 | Modelo de dominio | Entidades, Value Objects, agregados, invariantes de negocio del MVP |
| 6 | Base de datos | Esquema SQL Server, migraciones EF Core, seed data, soft delete, auditoría |
| 7 | Backend | Endpoints CQRS/MediatR de los módulos del MVP, FluentValidation, autenticación/autorización |
| 8 | Frontend | Vue 3 + Vuetify: login, dashboard, CRUD de entidades núcleo, dark mode, i18n base |
| 9 | Pruebas | xUnit + FluentAssertions + Moq, integration tests, medición de cobertura |
| 10 | Documentación | README, diagramas Mermaid, modelo ER, Swagger, ADRs consolidados |
| 11 | DevOps | Dockerfiles, docker-compose, GitHub Actions (build/test/lint), EditorConfig |

## Release 2 — Colaboración y Operación
Documentos (storage intercambiable), Notificaciones, Workflow, Catálogos,
Configuración avanzada, Reportes ampliados, Auditoría/Logs como módulo visible
(no solo cross-cutting), Redis (cache), Hangfire (jobs), ~~Rate limiting~~,
Response compression, Health checks avanzados.

**Sprints 1-9 completos** (auditado en Sprint 10, ver
[`r2-10-documentacion.md`](./r2-10-documentacion.md)). Sprint 1 (Análisis):
[`r2-01-vision-y-alcance.md`](./r2-01-vision-y-alcance.md) y
[`backlog/historias-usuario-release2.md`](./backlog/historias-usuario-release2.md).
Activación de Redis/Hangfire/Response Compression justificada caso por caso
en [ADR-0008](./adr/ADR-0008-activacion-redis-hangfire-response-compression.md)
(seguimiento explícito de ADR-0001, no una decisión nueva sin relación).
Sprints 2-3 (Diseño/Arquitectura):
[`03-diseno-arquitectura/00-resumen.md`](./03-diseno-arquitectura/00-resumen.md)
(actualizado en el lugar, no un archivo `r2-02`) y
[`r2-03-arquitectura.md`](./r2-03-arquitectura.md). Sprint 4 (Validación,
Catálogos): [`r2-04-validacion.md`](./r2-04-validacion.md). Sprint 5 (Modelo
de dominio): [`r2-05-modelo-dominio.md`](./r2-05-modelo-dominio.md). Sprint 6
(Base de datos): [`r2-06-base-de-datos.md`](./r2-06-base-de-datos.md).
Sprint 7 (Backend, en 3 sub-partes — Workflow/Documentos/Notificaciones):
[`r2-07a`](./r2-07a-backend-workflow.md) ·
[`r2-07b`](./r2-07b-backend-documentos.md) ·
[`r2-07c`](./r2-07c-backend-notificaciones.md). Sprint 8 (Frontend, misma
división en 3): [`r2-08a`](./r2-08a-frontend-catalogos-workflow.md) ·
[`r2-08b`](./r2-08b-frontend-documentos.md) ·
[`r2-08c`](./r2-08c-frontend-notificaciones.md). Sprint 9 (Pruebas):
[`r2-09-pruebas.md`](./r2-09-pruebas.md) — 218/218 tests, 95.5% de
cobertura de líneas.

> `Rate limiting` en `/api/auth/*` se adelantó a la revisión de seguridad
> ad-hoc del 2026-07-07 (pedida por el usuario, fuera de la secuencia de
> Sprints) — ver [docs/08a-seguridad.md](./08a-seguridad.md). Queda pendiente
> para Release 2 extenderlo a otros endpoints si el caso de uso lo justifica.

> "SignalR" aparece dos veces en la especificación original con alcance
> distinto: notificaciones in-app básicas (F6.1, un solo proceso Api) son
> Release 2 — ya estaban tageadas así en `backlog/epics.md` desde el Sprint 1
> de Release 1. "SignalR real-time a escala" (backplane de Redis para
> múltiples instancias de la Api) sigue en Release 4: es una necesidad de
> *escalado horizontal*, no de la funcionalidad en sí, y Release 2 no
> despliega más de una instancia de la Api (ver `docker-compose.yml`).

## Release 3 — Inteligencia Artificial
**Redefinido al iniciar el Sprint 1 de este Release** (decisión explícita
del usuario — ver [`r3-01-vision-y-alcance.md`](./r3-01-vision-y-alcance.md),
sección 0): el plan original de este renglón (generación de historias/
tests/SQL/DTOs/entidades, análisis de code smells, RAG sobre la
documentación de este propio repo, servidor MCP propio) era tooling de
*desarrollo* — herramientas para quien construye EnterpriseFlow, no una
feature para quien lo usa. Redirigido a un **asistente de IA de cara al
usuario final**, coherente con el resto del portafolio: integración
intercambiable con OpenAI/Anthropic (F9.1), chat con respuestas ancladas en
los datos reales del tenant vía las Queries de Application ya existentes —
nunca acceso directo a SQL (F9.2-F9.4), y RAG sobre los Documentos que el
propio tenant sube (F5, Release 2) en vez de sobre la documentación del
proyecto (E10 redefinido). La generación de artefactos de desarrollo y el
servidor MCP propio quedan diferidos sin Release asignado
(`backlog/epics.md`, E9/E11) — no descartados, solo fuera de este Release.

Sin claves de API reales de OpenAI/Anthropic disponibles en este entorno —
mismo tipo de límite ya enfrentado con Redis y los SDKs cloud de
Documentos en Release 2: la abstracción y las pruebas con fakes se
construyen igual, y toda llamada real al proveedor queda explícitamente
marcada como no verificada en ejecución hasta que existan credenciales.

**Sprints 1-11 completos** (Release 3 cerrada). Sprint 1 (Análisis):
[`r3-01-vision-y-alcance.md`](./r3-01-vision-y-alcance.md) y
[`backlog/historias-usuario-release3.md`](./backlog/historias-usuario-release3.md)
(HU-090 a HU-101). Sprint 2 (Diseño): actualizado en el lugar, no un
archivo `r3-02` (mismo criterio que Release 2) —
[`03-diseno-arquitectura/00-resumen.md`](./03-diseno-arquitectura/00-resumen.md),
`c4-01-contexto.md`/`c4-02-contenedores.md` (se retiraron los contenedores
`mcp`/`rag` del plan original) y dos diagramas de secuencia nuevos en
`04-secuencias.md`. Sprint 3 (Arquitectura):
[`r3-03-arquitectura.md`](./r3-03-arquitectura.md) —
[ADR-0013](./adr/ADR-0013-abstraccion-ia-y-limite-tool-use.md) (`IAiChatClient`/
`IEmbeddingClient`, límite de seguridad del tool-use) y
[ADR-0014](./adr/ADR-0014-almacen-de-vectores-para-rag.md) (vectores en
una tabla de SQL Server, sin servicio dedicado). Sprint 4 (Validación,
chat con tool-use): [`r3-04-validacion.md`](./r3-04-validacion.md).
Sprint 5 (Modelo de dominio, `DocumentChunk`):
[`r3-05-modelo-dominio.md`](./r3-05-modelo-dominio.md). Sprint 6 (Base de
datos): [`r3-06-base-de-datos.md`](./r3-06-base-de-datos.md). Sprint 7
(Backend, en 3 sub-partes — proveedores de IA/RAG/más herramientas):
[`r3-07a`](./r3-07a-backend-ia-providers.md) ·
[`r3-07b`](./r3-07b-backend-rag.md) ·
[`r3-07c`](./r3-07c-backend-mas-herramientas.md). Sprint 8 (Frontend, sin
sub-partes — solo el chat tiene UI nueva):
[`r3-08-frontend.md`](./r3-08-frontend.md). Sprint 9 (Pruebas):
[`r3-09-pruebas.md`](./r3-09-pruebas.md) — 281/281 tests, 94.9% de
cobertura de líneas. Sprint 10 (Documentación):
[`r3-10-documentacion.md`](./r3-10-documentacion.md). Sprint 11 (DevOps,
cierra el Release): [`r3-11-devops.md`](./r3-11-devops.md) — migración
completa verificada contra una base de datos nueva.

## Release 4 — Hardening Empresarial
**Redirigido al iniciar el Sprint 1 de este Release** (decisión explícita
del usuario — ver [`r4-01-vision-y-alcance.md`](./r4-01-vision-y-alcance.md),
sección 0): del plan original, tres piezas (MassTransit/RabbitMQ, Elastic/
Application Insights, SignalR a escala) necesitaban infraestructura real
que este entorno no tiene **y** nunca tuvieron un caso de uso concreto
identificado en el producto — quedan diferidas sin Release asignado
(`backlog/epics.md`, E15 nuevo + F7.6), mismo criterio que ya difirió el
servidor MCP propio en Release 3. Foco de Release 4: Temporal Tables
(`Project`/`ProjectTask`, F7.9), OpenTelemetry con exportador local
(F7.5), BenchmarkDotNet (F12.4), CodeQL (F13.3 — SonarCloud/SonarQube
también diferido, misma razón que Elastic: cuenta externa que este
entorno no tiene), Dependabot (F13.4), Conventional Commits + Semantic
Versioning + Release Notes automáticas (F13.5). La cobertura de pruebas
≥90% (F12.3) **ya está alcanzada** (94.9% desde Sprint 9 de Release 3) —
este Release la audita y establece cómo mantenerla, no la persigue desde
cero.

> La revisión de seguridad OWASP Top 10 se adelantó parcialmente el
> 2026-07-07 como revisión manual de código (sin herramientas SAST/DAST ni
> pentesting) — ver [docs/08a-seguridad.md](./08a-seguridad.md). Release 4
> cubre la parte de SAST automatizado con CodeQL (nativo de GitHub, sin
> cuenta externa); pentesting real/DAST siguen sin Release asignado —
> necesitan un despliegue real expuesto que este entorno no tiene.

**Sprints 1-11 completos** (Release 4 cerrada). Sprint 1 (Análisis):
visión y alcance en [`r4-01-vision-y-alcance.md`](./r4-01-vision-y-alcance.md);
Historias de Usuario en
[`backlog/historias-usuario-release4.md`](./backlog/historias-usuario-release4.md)
(HU-102 — la única de este Release; el resto del alcance es ingeniería/
operación justificada vía ADR, no HUs). Sprint 2 (Diseño): confirmación,
sin contenedores C4 nuevos. Sprint 3 (Arquitectura):
[`r4-03-arquitectura.md`](./r4-03-arquitectura.md) —
[ADR-0015](./adr/ADR-0015-temporal-tables-historial-de-cambios.md) y
[ADR-0016](./adr/ADR-0016-opentelemetry-exportador-local.md), spans reales
verificados en ejecución. Sprint 4 (Validación, historial de `Project`):
[`r4-04-validacion.md`](./r4-04-validacion.md) — encontró el *seam*
`IAppDbContext.GetProjectsAsOf` (`TemporalAsOf` es específico de
`Microsoft.EntityFrameworkCore.SqlServer`, que Application no puede
referenciar). Sprint 5 (Modelo de dominio): confirmación, sin cambios de
Domain — [`r4-05-modelo-dominio.md`](./r4-05-modelo-dominio.md). Sprint 6
(Base de datos): [`r4-06-base-de-datos.md`](./r4-06-base-de-datos.md).
Sprint 7 (Backend, completa HU-102 para `ProjectTask` + BenchmarkDotNet):
[`r4-07-backend.md`](./r4-07-backend.md). Sprint 8 (Frontend, sin UI
nueva — encontró y corrigió cabeceras de seguridad ausentes en la SPA):
[`r4-08-frontend.md`](./r4-08-frontend.md). Sprint 9 (Pruebas, cierra el
gap de cobertura de Temporal Tables con un proyecto de pruebas nuevo
contra LocalDB real, agrega gate de cobertura en CI):
[`r4-09-pruebas.md`](./r4-09-pruebas.md). Sprint 10 (Documentación,
auditoría completa contra `especificcion.md` — Response Compression nunca
activado pese al ADR, ADR-0001 punto 6 sin resolver, `CHANGELOG.md`
inexistente desde Release 1):
[`r4-10-documentacion.md`](./r4-10-documentacion.md). Sprint 11 (DevOps,
cierra el Release): CodeQL, Dependabot, Conventional Commits + SemVer +
Release Notes automáticas, y el servicio SQL Server de CI diferido desde
Sprint 9 — [`r4-11-devops.md`](./r4-11-devops.md).

## Regla de cierre de Release
Un Release no se considera cerrado hasta que sus Sprints 1-11 (tabla de arriba,
adaptada a su alcance) estén completos, incluyendo documentación y DevOps —
igual que exige el documento original para "Sprint".
