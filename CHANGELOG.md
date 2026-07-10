# Changelog

Formato inspirado en [Keep a Changelog](https://keepachangelog.com/), adaptado
a la forma de entrega de este proyecto: por **Release** (no por versión
semántica todavía — F13.5, Release 4, introduce Conventional Commits +
Semantic Versioning; hasta entonces las entradas se agrupan por Release/Sprint,
que es la unidad de entrega real documentada en `docs/`).

Cada entrada resume el resultado, no el proceso — el detalle completo
(alternativas comparadas, verificación real, hallazgos) vive en el documento
de Sprint enlazado. Reconstruido en Release 4 Sprint 10 a partir del historial
real ya documentado en `docs/` y `README.md` — F14.4 lo pedía desde Release 1
("continua, entregable en cada Release") pero nunca se había creado.

> **Nota (Release 4 Sprint 11, F13.5)**: a partir de aquí,
> [release-please](https://github.com/googleapis/release-please)
> (`.github/workflows/release-please.yml`) genera y antepone sus propias
> entradas por versión semántica automáticamente a partir de Conventional
> Commits (ver `CONTRIBUTING.md`) — las secciones por Release de abajo son
> el historial manual previo a esa automatización, no se reescriben.

## Release 4 — Hardening Empresarial

Sprints 1-11 completos. Ver
[`docs/r4-01-vision-y-alcance.md`](./docs/r4-01-vision-y-alcance.md) para el
alcance completo y la redirección de scope decidida al iniciar el Release.

### Added
- Historial completo de cambios para `Project`/`ProjectTask` vía SQL Server
  Temporal Tables (HU-102, [ADR-0015](./docs/adr/ADR-0015-temporal-tables-historial-de-cambios.md)) —
  `GET /api/projects/{id}/history` y `GET /api/tasks/{id}/history`.
- Tracing distribuido con OpenTelemetry, exportador local
  ([ADR-0016](./docs/adr/ADR-0016-opentelemetry-exportador-local.md)).
- `benchmarks/EnterpriseFlow.Benchmarks/` (BenchmarkDotNet) — mediciones
  reales de `CosineSimilarity.Compute` (RAG) y `TextChunker.Split`
  (indexación de Documentos).
- `tests/EnterpriseFlow.Infrastructure.SqlServerTests/` — cierra el gap de
  cobertura de Temporal Tables (SQLite no las soporta) con pruebas reales
  contra LocalDB.
- Gate de cobertura ≥90% en CI (`ci.yml`), con verificación real de que
  falla ante una regresión.
- Cabeceras de seguridad (CSP, `X-Frame-Options`, etc.) para la SPA servida
  por `nginx.conf` en producción — antes solo cubrían la API JSON.
- `CHANGELOG.md` (este archivo, F14.4) y `CONTRIBUTING.md` (Conventional
  Commits, F13.5).
- `.github/workflows/codeql.yml` (SAST, F13.3), `.github/dependabot.yml`
  (F13.4), `.github/workflows/release-please.yml` (SemVer + Release Notes
  automáticas, F13.5) — servicio `mssql/server` (Linux) agregado a
  `ci.yml` para que `EnterpriseFlow.Infrastructure.SqlServerTests` corra
  en CI sin el filtro `RequiresSqlServer` que tenía desde su creación.

### Fixed
- Response Compression (Brotli/Gzip): documentado como activo desde
  Release 2 ([ADR-0008](./docs/adr/ADR-0008-activacion-redis-hangfire-response-compression.md))
  pero nunca registrado en `Program.cs`. Al agregarlo se encontró además que
  el orden de middleware lo dejaba sin efecto para `/swagger/v1/swagger.json`
  (Swagger es terminal, corta la cadena antes de llegar a la compresión) —
  corregido y verificado con una petición real (35 KB → 6 KB con Brotli).
- El índice de documentación del README, que nunca se había actualizado
  más allá del Sprint 1 de Release 4 (Sprint 10).
- `docs/02-roadmap.md`: la sección de Release 4 seguía marcada "Sprint 1
  completo" pese a que el Release ya estaba cerrado (Sprint 11) — al
  corregirlo se encontró el mismo problema en la sección de Release 3
  ("Sprints 1-9 completos" pese a que ese Release cerró en el Sprint 11),
  corregida también.
- La primera frase del README describía un servidor MCP propio como
  construido — sigue diferido (E11); corregida (Sprint 10).

### Changed
- [ADR-0001](./docs/adr/ADR-0001-alcance-y-estrategia-de-entrega.md), punto 6
  (Mapster vs. AutoMapper): resuelto sin benchmark — el patrón CQRS de
  proyección directa (`IQueryable.Select(e => new Dto(...))`) nunca necesitó
  un mapper de objetos genérico.

Detalle Sprint por Sprint:
[Arquitectura](./docs/r4-03-arquitectura.md) ·
[Validación](./docs/r4-04-validacion.md) ·
[Modelo de dominio](./docs/r4-05-modelo-dominio.md) ·
[Base de datos](./docs/r4-06-base-de-datos.md) ·
[Backend](./docs/r4-07-backend.md) ·
[Frontend](./docs/r4-08-frontend.md) ·
[Pruebas](./docs/r4-09-pruebas.md) ·
[Documentación](./docs/r4-10-documentacion.md) ·
[DevOps](./docs/r4-11-devops.md)

## Release 3 — Inteligencia Artificial

Asistente de IA conversacional embebido, con RAG sobre los Documentos de
cada tenant. Ver [`docs/r3-01-vision-y-alcance.md`](./docs/r3-01-vision-y-alcance.md).

### Added
- Asistente de IA con proveedores intercambiables OpenAI/Claude (F9.1),
  chat con historial persistido por usuario/tenant (F9.2).
- RAG: indexación automática de Documentos subidos por el tenant (texto
  plano, PDF, Word) y respuestas ancladas al corpus de ese tenant (F10).
- 3 herramientas reales del asistente vía tool-use/function-calling,
  siempre reutilizando Queries de Application existentes, nunca SQL directo:
  `get_my_projects`, `search_my_documents`, `get_my_overdue_tasks`.
- `NullAiChatClient`/`NullEmbeddingClient` — degradación visible sin claves
  de API reales configuradas en este entorno, en vez de un crash.

### Deferred (con trazabilidad, sin Release asignado)
- Servidor MCP propio (E11) y el resto de tooling de *desarrollo* del plan
  original de E9/E10 (generar historias/SQL/DTOs/tests) — redirigido a un
  asistente de cara al usuario final del SaaS en su lugar.

Detalle: [`docs/r3-03-arquitectura.md`](./docs/r3-03-arquitectura.md) ·
[`docs/r3-07a-backend-ia-providers.md`](./docs/r3-07a-backend-ia-providers.md) ·
[`docs/r3-07b-backend-rag.md`](./docs/r3-07b-backend-rag.md) ·
[`docs/r3-09-pruebas.md`](./docs/r3-09-pruebas.md) (94.9% de cobertura).

## Release 2 — Colaboración y Operación

Ver [`docs/r2-01-vision-y-alcance.md`](./docs/r2-01-vision-y-alcance.md).

### Added
- Documentos: almacenamiento desacoplado con 4 proveedores intercambiables
  por configuración (Local, Azure Blob, Amazon S3, Google Cloud Storage —
  F5).
- Notificaciones in-app en tiempo real (SignalR) y por correo (Hangfire,
  asíncrono respecto al hilo de la request — F6).
- Motor de Workflow configurable (estados/transiciones, F8.1) y Catálogos
  genéricos con cache-aside en Redis (F8.2).
- Reportes exportables (F4.3) y Response Compression global, activados
  ambos por un caso de uso real, no por la lista de la especificación
  ([ADR-0008](./docs/adr/ADR-0008-activacion-redis-hangfire-response-compression.md)).
- Health Checks avanzados (Redis, storage de Hangfire, proveedor de
  Documentos — F7.8).

Detalle: [`docs/r2-04-validacion.md`](./docs/r2-04-validacion.md) ·
[`docs/r2-09-pruebas.md`](./docs/r2-09-pruebas.md).

## Release 1 — MVP

Ver [`docs/01-vision-y-alcance.md`](./docs/01-vision-y-alcance.md).

### Added
- Identidad multi-tenant: JWT + Refresh Token, Roles/Permisos, autorización
  por Policies, menú dinámico según permisos (E1).
- Entidades núcleo de negocio: Empresas, Clientes, Contactos, Proyectos,
  Equipos, Tareas, Calendario (E2-E3).
- Dashboard ejecutivo básico, auditoría (quién/cuándo/qué), logging
  estructurado con Serilog, Health Checks (E4, E7).
- Docker + Docker Compose, GitHub Actions (build/test), `dotnet format` +
  `.editorconfig` (E13).
- Cobertura de pruebas empujada de 44.4% a 95.8% en el Sprint de Pruebas
  (`docs/09-pruebas.md`).

### Fixed
- Revisión de seguridad ad-hoc (2026-07-07): 10 hallazgos corregidos
  (secretos, rate limiting, enumeración de usuarios, canal de tiempo en
  login, revocación en cadena de refresh tokens, headers de seguridad,
  refresh token a cookie HttpOnly, CORS explícito, HSTS). Ver
  [`docs/08a-seguridad.md`](./docs/08a-seguridad.md).
- Diagrama ER desactualizado (solo 6 de 12 tablas) y dos documentos con
  secciones "qué falta" obsoletas, encontrados en el Sprint de
  Documentación (`docs/10-documentacion.md`).
