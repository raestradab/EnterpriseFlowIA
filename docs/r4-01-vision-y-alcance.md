# Release 4 — Hardening Empresarial: Visión y Alcance

Sprint 1 (Análisis) de Release 4, mismo ciclo `análisis → ... → DevOps`
que Releases 1-3 aplicaron (ADR-0001). Este documento juega el mismo rol
que [`r3-01-vision-y-alcance.md`](./r3-01-vision-y-alcance.md) jugó para
Release 3.

## 0. Redirección de alcance, decidida por el usuario al iniciar este Sprint

El plan original de Release 4 (`docs/02-roadmap.md`) mezclaba dos tipos de
trabajo muy distintos: piezas que se pueden construir y verificar de
verdad en este entorno (Temporal Tables contra LocalDB real,
BenchmarkDotNet, tracing con OpenTelemetry, configuración real de
Dependabot/CodeQL, Conventional Commits + SemVer), y piezas que necesitan
infraestructura externa real que este entorno no tiene y que, además,
**nunca tuvieron un caso de uso concreto identificado en el producto**:
un broker de mensajería (RabbitMQ/MassTransit), un backend de
observabilidad en la nube (Elastic Search/Application Insights), y
escalado horizontal de SignalR con backplane de Redis (que solo tiene
sentido con más de una instancia de la Api corriendo a la vez — este
stack nunca desplegó más de una).

Antes de escribir código se le preguntó al usuario cómo priorizar esa
segunda categoría, dado que construirla igual habría significado código
0% verificable en este entorno, sin ningún caso de uso real detrás —
exactamente lo que ADR-0001 ya advierte no hacer. Decisión: **diferir las
tres piezas sin caso de uso probado (RabbitMQ/MassTransit, Elastic/
Application Insights, SignalR a escala), foco total en lo que sí se puede
construir y verificar de verdad.** No se descartan — quedan documentadas
en `backlog/epics.md` con la misma trazabilidad que ya recibió el
servidor MCP propio (E11) al diferirse en Release 3.

## 1. Qué agrega Release 4 sobre Release 3

Los Releases 1-3 construyeron producto (gestión de proyectos, colaboración
y operación, asistente de IA). Release 4 no agrega ninguna feature de
producto nueva — endurece lo que ya existe: trazabilidad completa de
cambios (no solo el último valor conocido), medición de rendimiento real
en vez de intuición, observabilidad de qué está pasando en producción, y
automatización de la higiene del propio repositorio (dependencias,
seguridad estática, versionado).

## 2. Alcance de Release 4

Mapea a los Epics E7 (ampliado), E12 (ampliado) y E13 (ampliado) de
[`backlog/epics.md`](./backlog/epics.md):

- **Historial completo de cambios vía Temporal Tables (F7.9, nuevo)**:
  `Project` y `ProjectTask` — las dos entidades de trabajo donde "¿quién
  cambió esto y cuándo?" importa más para un PM real, no solo el último
  valor (`ModifiedAtUtc`/`ModifiedBy` ya existentes desde Release 1) sino
  cada estado intermedio. SQL Server System-Versioned Temporal Tables,
  verificable de punta a punta contra LocalDB real sin ninguna
  infraestructura adicional.
- **Tracing distribuido con OpenTelemetry (F7.5)**: instrumentación real
  (ASP.NET Core, HttpClient, EF Core) con un exportador local verificable
  en este entorno — sin backend real detrás (Elastic/Application
  Insights, F7.6, diferido — ver sección 0).
- **Benchmarks de rendimiento (F12.4)**: BenchmarkDotNet sobre los
  caminos calientes reales del sistema — el filtro global de tenant
  (ADR-0003), la similitud de coseno de RAG (ADR-0014), el pipeline de
  `AuthorizationBehavior`/`ValidationBehavior`. Corre sin infraestructura
  externa, resultados reales, no estimados.
- **Análisis estático de seguridad automatizado (parte de la revisión
  OWASP pendiente, `docs/02-roadmap.md`)**: GitHub CodeQL — nativo de
  GitHub Actions, sin cuenta externa que configurar (a diferencia de
  SonarCloud/SonarQube, que sí la necesitan — ver sección 3).
- **Dependabot (F13.4)**: actualizaciones de dependencias automatizadas —
  configuración real, nativa de GitHub, sin servicio externo.
- **Conventional Commits + Semantic Versioning + Release Notes
  automáticas (F13.5)**: convención de mensajes de commit documentada +
  automatización real de versionado/changelog en CI.
- **Cobertura ≥90% agregada (F12.3)**: **ya alcanzada** — 94.9% desde
  Sprint 9 de Release 3 (`r3-09-pruebas.md`). Este Release la audita y
  establece cómo se mantiene hacia adelante (gate en CI), no la persigue
  desde cero.

## 3. Decisiones de alcance dentro de Release 4 (para no sobre-construir)

- **Temporal Tables solo en `Project`/`ProjectTask`, no en las 21
  entidades del sistema**: mismo criterio que ya activó Redis solo para
  Catálogos (ADR-0008) — un caso de uso real y acotado primero, no una
  política blanket sin necesidad probada en el resto. Si otra entidad
  demuestra necesitarlo después, extenderlo es una migración más, no una
  reescritura.
- **CodeQL, no SonarCloud/SonarQube, para análisis estático de
  seguridad**: `backlog/epics.md` (F13.3) daba a entender que SonarLint ya
  estaba cubierto desde Release 1-2 — auditado al iniciar este Sprint, se
  confirmó que **nunca se configuró** (solo StyleCop.Analyzers, un linter
  de estilo, no de seguridad). SonarCloud/SonarQube necesitan una cuenta
  externa y un token que este entorno no tiene — mismo tipo de límite que
  ya aplicó a Elastic/Application Insights (sección 0), así que se
  aplica el mismo criterio: se difiere la variante que exige cuenta
  externa, y se construye la que no la exige (CodeQL) para cubrir la
  necesidad real (SAST automatizado) sin quedar 0% verificable.
- **OpenTelemetry con exportador local, no un backend real** — la
  instrumentación y los spans generados sí son código real y verificable
  (se pueden ver emitidos), pero ningún backend de observabilidad
  (Elastic/App Insights/Jaeger real) está detrás en este entorno.
- **Sin UI nueva para navegar el historial de Temporal Tables** — la
  capacidad vive en la base de datos, consultable por SQL/una Query de
  Application si se necesita; una pantalla dedicada para explorarlo no
  la pide ninguna Historia de Usuario todavía (mismo criterio que RAG,
  Release 3, no prometió una UI de búsqueda de texto libre).

## 4. Explícitamente fuera de Release 4

- **RabbitMQ/MassTransit** — diferido sin Release asignado
  (`backlog/epics.md`, E15 nuevo): ningún flujo del sistema necesita hoy
  mensajería asíncrona más allá de lo que Hangfire (Release 2, ADR-0008)
  ya cubre para el único caso real (envío de correo).
- **Elastic Search / Application Insights (F7.6)** — diferido sin Release
  asignado: necesita una cuenta cloud real que este entorno no tiene: ver
  sección 0.
- **SignalR a escala con backplane de Redis** — diferido sin Release
  asignado (`backlog/epics.md`, E15 nuevo): solo tiene sentido con más de
  una instancia de la Api corriendo — este stack nunca desplegó más de
  una, y no hay caso de uso real de escalado horizontal probado todavía.
- **SonarCloud/SonarQube real** — ver sección 3; CodeQL cubre la
  necesidad real sin la dependencia de cuenta externa.
- **Pentesting real / herramientas DAST** — necesitan un despliegue real
  expuesto que este entorno no tiene; sigue en `docs/02-roadmap.md` como
  parte de la revisión OWASP pendiente, sin Release asignado dentro de
  este ciclo.

## 5. Criterios de éxito de Release 4

1. `Project`/`ProjectTask` retienen su historial completo de cambios vía
   Temporal Tables, verificado con una consulta real `FOR SYSTEM_TIME
   AS OF` contra LocalDB, no solo revisado por lectura de la migración.
2. Al menos un benchmark real corrido con BenchmarkDotNet sobre un camino
   caliente del sistema, con resultados reales documentados (no
   estimados).
3. OpenTelemetry emite spans reales y verificables (aunque sin backend de
   observabilidad real detrás) para al menos un flujo end-to-end.
4. CodeQL, Dependabot y el pipeline de Semantic Versioning corren de
   verdad en CI (verificado contra una ejecución real de GitHub Actions,
   no solo YAML sintácticamente válido — mismo estándar que
   `dotnet format`/`dotnet test` ya tienen en `ci.yml`).
5. Todo lo diferido por falta de infraestructura real queda dicho
   explícitamente en `backlog/epics.md`, con la misma trazabilidad que
   ya recibió el servidor MCP propio — nunca descartado en silencio.
