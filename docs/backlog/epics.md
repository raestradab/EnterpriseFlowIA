# Backlog — Epics y Features

Cada Epic mapea íntegramente a los módulos listados en `especificcion.md`, para
garantizar que ningún requerimiento original se pierde — solo se reordena en el
tiempo (ver `02-roadmap.md`).

---

## E1 — Identidad y Multi-Tenancy `Release 1`
Autenticación, autorización y aislamiento por tenant.

- F1.1 Registro y aprovisionamiento de Tenant
- F1.2 Login con JWT + Refresh Token
- F1.3 Gestión de Usuarios (alta, edición, desactivación)
- F1.4 Gestión de Roles y Permisos
- F1.5 Autorización basada en Policies
- F1.6 Menú dinámico según permisos del usuario
- F1.7 Perfil de usuario (datos propios, cambio de password, preferencias)

## E2 — Organizaciones y Personas `Release 1`
- F2.1 Gestión de Empresas
- F2.2 Gestión de Clientes
- F2.3 Gestión de Contactos (asociados a Cliente)

## E3 — Proyectos y Trabajo `Release 1`
- F3.1 Gestión de Proyectos
- F3.2 Gestión de Equipos (asignación de usuarios a proyectos)
- F3.3 Gestión de Tareas (con estado, prioridad, asignación)
- F3.4 Calendario (vista de tareas/eventos por fecha)

## E4 — Dashboard Ejecutivo y Reportes `Release 1 (base) / Release 2 (avanzado)`
- F4.1 Indicadores clave (KPIs) de proyectos, clientes, usuarios
- F4.2 Gráficas de actividad y uso
- F4.3 Reportes exportables (Release 2) — caso de uso real que activa Response
  Compression (ADR-0008): el primer endpoint del producto con payloads
  potencialmente grandes (datasets completos para exportar, no listados paginados)
- F4.4 Mapa y alertas (Release 2)

## E5 — Documentos `Release 2`
- F5.1 Abstracción de almacenamiento (interfaz de storage provider)
- F5.2 Proveedor Local Storage (implementación inicial)
- F5.3 Proveedor Azure Blob Storage
- F5.4 Proveedor Amazon S3
- F5.5 Proveedor Google Cloud Storage
- F5.6 Selección de proveedor por configuración (sin cambios de código)
- F5.7 Validación de archivos subidos (tipo, tamaño, escaneo básico)

## E6 — Notificaciones `Release 2`
- F6.1 Notificaciones in-app en tiempo real (SignalR)
- F6.2 Notificaciones por correo — caso de uso real que activa Hangfire
  (ADR-0008): enviar un correo no debe bloquear el hilo de la request HTTP
- F6.3 Centro de notificaciones (historial, marcar leído)

## E7 — Auditoría, Logs y Observabilidad `Release 1 (base) / Release 2-4 (ampliación)`
- F7.1 Auditoría de cambios (quién/cuándo/qué) — Release 1
- F7.2 Logging estructurado con Serilog — Release 1
- F7.3 Health Checks — Release 1
- F7.4 Vista de Logs en UI — Release 2
- F7.8 Health Checks avanzados (Redis, storage de Hangfire, proveedor de
  Documentos) — Release 2, agregado al iniciar el análisis de Release 2: F7.3
  solo cubría SQL Server porque era la única dependencia externa de Release 1;
  Release 2 introduce tres más y el check existente no las ve
- F7.5 Tracing distribuido (OpenTelemetry) — Release 4
- F7.9 Historial completo de cambios vía Temporal Tables (`Project`/
  `ProjectTask`) — Release 4, agregado al iniciar el análisis de ese
  Release: F7.1 solo guarda el último valor conocido
  (`ModifiedAtUtc`/`ModifiedBy`), no cada estado intermedio — ver
  `r4-01-vision-y-alcance.md`
- ~~F7.7 Performance counters / BenchmarkDotNet~~ — duplicado de F12.4
  (mismo Epic de Calidad/Rendimiento donde ya vivía); consolidado ahí al
  auditar el backlog al iniciar Release 4, sin trabajo perdido

> **F7.6 (Integración Elastic Search / Application Insights) diferida sin
> Release asignado** al iniciar el análisis de Release 4 — necesita una
> cuenta cloud real que este entorno no tiene, mismo tipo de límite que
> ya llevó a diferir el servidor MCP propio (E11) en Release 3. No
> descartada — ver `r4-01-vision-y-alcance.md`, sección 0.

## E8 — Workflow y Catálogos `Release 2`
- F8.1 Motor de Workflow configurable (estados y transiciones)
- F8.2 Catálogos genéricos (listas maestras reutilizables) — caso de uso real
  que activa Redis (ADR-0008): datos de referencia leídos con alta frecuencia
  y escritos rara vez, el patrón cache-aside de manual del libro
- F8.3 Módulo de Configuración (parámetros de sistema/tenant)

## E9 — AI Assistant `Release 3`
> **Redefinido al iniciar el análisis de Release 3** (decisión explícita del
> usuario, ver `r3-01-vision-y-alcance.md`): el plan original de E9 era
> tooling de *desarrollo* (generar historias/tests/SQL/DTOs/entidades para
> construir el propio EnterpriseFlow — meta-tooling, no una feature del
> producto). Redirigido a un asistente de cara al usuario *final* del SaaS,
> coherente con el resto del portafolio (features de producto real, no
> herramientas internas del equipo de desarrollo). F9.2-F9.9 originales
> (historias/SQL/DTOs/tests/code smells) no tienen equivalente de usuario
> final — quedan diferidas sin Release asignado, ver nota al final de este
> Epic.
- F9.1 Integración con proveedores OpenAI y Claude (abstracción
  intercambiable) — sin cambios respecto al plan original.
- F9.2 Chat conversacional dentro de EnterpriseFlow, con historial
  persistido por usuario/tenant.
- F9.3 Respuestas ancladas (*grounded*) en los datos reales del tenant vía
  herramientas internas (tool-use/function-calling contra las Queries de
  Proyectos/Tareas/Clientes/Documentos ya existentes) — nunca acceso
  directo a SQL desde el modelo.
- F9.4 Resúmenes y reportes ejecutivos generados a partir de datos reales
  (conserva la intención de F9.10 del plan original, que ya apuntaba a
  usuario final).

> **Diferido, sin Release asignado**: generación de historias de usuario,
> SQL, DTOs/Entidades, Unit/Integration Tests, y detección de code
> smells (F9.2/F9.5/F9.6/F9.7/F9.8 del plan original) — tooling de
> desarrollo válido en sí mismo, pero no lo que Release 3 prioriza. Sigue
> representado aquí para trazabilidad con `especificcion.md` (sección
> MÓDULO IA), no descartado — candidato a una Release futura si surge un
> caso de uso real.

## E10 — RAG (Conocimiento del Tenant) `Release 3`
> **Redefinido junto con E9**: el plan original indexaba la documentación
> *del propio proyecto* EnterpriseFlow (README, Swagger, ADRs — de nuevo,
> tooling de desarrollo). Redirigido a indexar los **Documentos que cada
> tenant sube** (F5, ya existente desde Release 2) — así el RAG responde
> preguntas sobre los datos reales de negocio del usuario, no sobre el
> código fuente del SaaS que ese usuario ni siquiera puede ver.
- F10.1 Indexación automática de Documentos subidos por el tenant (F5) —
  no README/arquitectura/Swagger del propio proyecto.
- F10.2 Indexación de texto plano, PDF y Word — mismo subconjunto de tipos
  que `FileSignatureValidator` (F5.7, Release 2) ya valida en la subida.
- F10.3 Respuesta a preguntas restringida al corpus indexado *de ese
  tenant* (grounding) — aislamiento multi-tenant aplica también a qué
  puede "ver" el asistente, no solo a las tablas de negocio.

## E11 — Servidor MCP Propio `Sin Release asignado`
> **Diferido por completo al redefinir Release 3** — exponer este propio
> repo (DB/docs/Swagger/archivos/logs/git/incidencias/arquitectura) vía MCP
> es tooling de desarrollo para quien construye EnterpriseFlow, no una
> feature para quien lo *usa*, que es el foco que Release 3 terminó
> priorizando. Se mantiene en el backlog, sin Release asignado, por la
> misma razón de trazabilidad que el resto de F9 diferido — no se descarta,
> se reordena.
- F11.1 Herramienta MCP: consulta a Base de Datos
- F11.2 Herramienta MCP: consulta a documentación
- F11.3 Herramienta MCP: consulta a Swagger
- F11.4 Herramienta MCP: consulta a archivos del proyecto
- F11.5 Herramienta MCP: consulta a Logs
- F11.6 Herramienta MCP: consulta a Git
- F11.7 Herramienta MCP: consulta a incidencias
- F11.8 Herramienta MCP: consulta a arquitectura

## E12 — Calidad, Pruebas y Rendimiento `Release 1 (base) / Release 4 (meta 90%+)`
- F12.1 Pruebas unitarias (xUnit, FluentAssertions, Moq) — Release 1+
- F12.2 Pruebas de integración — Release 1+
- F12.3 Cobertura ≥90% agregada — **ya alcanzada** (94.9% desde Sprint 9
  de Release 3, `r3-09-pruebas.md`); Release 4 audita y establece cómo se
  mantiene hacia adelante (gate en CI), no la persigue desde cero
- F12.4 Benchmarks de rendimiento (BenchmarkDotNet) — Release 4 (también
  cubre lo que F7.7, ahora removido de E7, describía — mismo Epic)

## E13 — DevOps y Entrega Continua `Release 1 (base) / Release 4 (madurez)`
- F13.1 Docker + Docker Compose — Release 1
- F13.2 GitHub Actions (build/test) — Release 1
- F13.3 EditorConfig, análisis estático — Release 1-2 (StyleCop.Analyzers,
  un linter de estilo) **/ Release 4** (SAST de seguridad real, vía
  CodeQL — auditado al iniciar Release 4: "SonarLint" nunca se configuró
  pese a lo que esta línea decía desde Release 1; SonarCloud/SonarQube
  necesitan una cuenta externa que este entorno no tiene, mismo límite
  que Elastic/Application Insights — ver `r4-01-vision-y-alcance.md`,
  sección 3)
- F13.4 Dependabot (Renovate evaluado y no elegido — necesita una app de
  GitHub instalada, Dependabot es nativo sin esa dependencia adicional)
  — Release 4
- F13.5 Conventional Commits + Semantic Versioning + Release Notes automáticas — Release 4

## E14 — Documentación Automática `Continua, entregable en cada Release`
- F14.1 README y diagramas Mermaid
- F14.2 Modelo ER
- F14.3 ADRs
- F14.4 CHANGELOG
- F14.5 Wiki / Casos de uso / Historias de usuario / Backlog / Roadmap

## E15 — Mensajería Asíncrona y Escalado Horizontal `Sin Release asignado`
> **Nuevo al iniciar el análisis de Release 4** — el roadmap
> (`02-roadmap.md`) ya mencionaba RabbitMQ/MassTransit y SignalR a escala
> en su descripción de Release 4, pero ninguno de los dos tenía una
> entrada propia aquí, así que no había dónde marcarlos diferidos con
> trazabilidad real. Se agregan ahora, ya diferidos, por la misma razón
> que llevó a diferir el servidor MCP propio (E11) en Release 3: sin
> caso de uso real identificado en el producto todavía, y sin
> infraestructura disponible en este entorno para construirlos y
> verificarlos de verdad (ver `r4-01-vision-y-alcance.md`, sección 0). No
> descartados — candidatos a una Release futura si un caso de uso real lo
> justifica.
- F15.1 Broker de mensajería asíncrona (RabbitMQ/MassTransit) — ningún
  flujo del sistema necesita hoy más que lo que Hangfire (Release 2,
  ADR-0008) ya cubre para el único caso real (envío de correo asíncrono)
- F15.2 SignalR a escala con backplane de Redis — solo tiene sentido con
  más de una instancia de la Api corriendo a la vez; este stack nunca
  desplegó más de una

---

## Trazabilidad con la especificación original
Todas las secciones de `especificcion.md` (TECNOLOGÍAS, BASE DE DATOS,
FRONTEND, AUTENTICACIÓN, MÓDULOS, MÓDULO IA, MCP, RAG, DOCUMENTOS, DASHBOARD,
OBSERVABILIDAD, DEVOPS, CALIDAD, SEGURIDAD, DOCUMENTACIÓN) quedan representadas
en E1-E14. Ninguna se descarta; se reordena por Release según el criterio de
"MVP primero, hardening después" definido en `01-vision-y-alcance.md`.

Las capacidades transversales de FRONTEND (Lazy Loading, Skeleton Loading,
Toast Notifications, Dark Mode, i18n, Responsive) no tienen Epic propio —
se aplican por vista, dentro del Sprint de Frontend de cada módulo. Auditadas
en Release 4 Sprint 10 (`r4-10-documentacion.md`): todas presentes salvo
**Virtual Scrolling**, ausente y nunca antes disclosed. Resuelto como "no
necesario, no como olvido": todos los listados del frontend usan Vuetify
`VDataTable` (paginado — Empresas/Clientes/Proyectos/Tareas/Catálogos/
Workflows), no un feed de scroll infinito; Virtual Scrolling resuelve un
problema distinto (miles de filas ya cargadas en el DOM a la vez) que la
paginación por diseño no genera. Si algún listado futuro necesita cargar
miles de filas sin paginar, se reconsidera entonces — mismo criterio YAGNI
de ADR-0001, punto 4.
