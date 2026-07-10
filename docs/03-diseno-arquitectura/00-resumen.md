# Sprint 2 — Diseño de Arquitectura

Este conjunto de documentos cierra el Sprint 2 del Release 1 (ver
`../02-roadmap.md`). Contiene las vistas C4 (Contexto, Contenedores,
Componentes) y los diagramas de secuencia que describen el comportamiento de
los flujos críticos (autenticación, resolución de tenant, autorización).

Las decisiones estructurales que estas vistas asumen están justificadas en:
- [ADR-0002](../adr/ADR-0002-clean-architecture-vertical-slices.md) — por qué
  Clean Architecture y Vertical Slice Architecture se combinan en vez de
  competir.
- [ADR-0003](../adr/ADR-0003-estrategia-multi-tenant.md) — por qué el
  aislamiento de tenant es por columna discriminadora + query filters, no por
  base de datos o esquema separado.
- [ADR-0004](../adr/ADR-0004-autorizacion-basada-en-policies.md) — por qué la
  autorización es un requirement genérico parametrizado por permiso, no
  Policies declaradas una a una ni `[Authorize(Roles=...)]`.

## Sprint 2 de Release 2 (2026-07-08)

Las vistas C4 (Contexto y Contenedores) se **actualizaron in-place**, no se
duplicaron por Release — reflejan el estado actual del sistema, con
anotaciones de qué sigue diferido (mismo criterio que ya usaban desde que se
escribieron en Sprint 2 de Release 1, anticipando Releases futuros). Cambios
de este Sprint: Redis y Hangfire pasan de "futuro" a "activo"; se agregan
SignalR (como capacidad del contenedor `api`, no un contenedor propio — ver
justificación en `c4-02-contenedores.md`) y las dos primeras dependencias
externas reales del sistema (Storage de Documentos, Proveedor de Correo).

Cuatro ADRs nuevos, uno por decisión de diseño con alternativas reales:
- [ADR-0009](../adr/ADR-0009-abstraccion-almacenamiento-documentos.md) —
  interfaz de storage de Documentos y asociación polimórfica al propietario
  (mismo patrón sin FK física que ADR-0005).
- [ADR-0010](../adr/ADR-0010-motor-workflow-generico.md) — motor de Workflow
  como máquina de estados orientada a datos, no un motor de terceros ni un
  enum hardcodeado.
- [ADR-0011](../adr/ADR-0011-arquitectura-entrega-notificaciones.md) —
  notificaciones reusan el pipeline de Domain Events de Release 1 en vez de
  introducir un mecanismo de eventos nuevo.
- [ADR-0012](../adr/ADR-0012-cache-aside-como-pipeline-behavior.md) —
  cache-aside de Catálogos como MediatR Pipeline Behavior, mismo mecanismo
  que `AuthorizationBehavior`/`ValidationBehavior`.

Diagramas de secuencia nuevos para los flujos críticos de Release 2 (subida
+ aprobación de Documento, notificación in-app + correo) en
[`04-secuencias.md`](./04-secuencias.md).

## Sprint 2 de Release 3 (2026-07-08)

Igual que Sprint 2 de Release 2, las vistas C4 se **actualizaron in-place**.
Este Sprint, a diferencia de los anteriores, incluyó **remover** elementos
del diagrama, no solo agregar: el contenedor `mcp` (Servidor MCP) y el
contenedor `rag` (Motor RAG, como proceso propio) que `c4-02-contenedores.md`
ya tenía dibujados desde Sprint 2 de Release 1 (anticipando el plan
original de Release 3) se quitaron por completo — el Sprint 1 de Análisis
de Release 3 redefinió el alcance hacia un asistente de cara al usuario
final (`r3-01-vision-y-alcance.md`), y el servidor MCP queda diferido sin
Release asignado (`backlog/epics.md`, E11). RAG, redirigido a indexar
Documentos del tenant, se pliega dentro del contenedor `api` existente
como una Vertical Slice más — no le hacía falta un proceso propio, mismo
argumento que ya justificaba mantener SignalR/Documentos/Workflow/
Notificaciones dentro de `api` en vez de servicios separados.

Un ADR nuevo:
- [ADR-0013](../adr/ADR-0013-abstraccion-ia-y-limite-tool-use.md) —
  abstracción `IAiChatClient` sobre OpenAI/Anthropic (mismo patrón que
  ADR-0009) y el límite de seguridad central del asistente: cada
  "herramienta" que el modelo puede invocar es una Query de Application ya
  existente, con su propio `AuthorizationBehavior` y filtro de tenant — el
  modelo nunca tiene una ruta directa a `IAppDbContext` ni genera SQL.

Dos diagramas de secuencia nuevos para los flujos críticos de Release 3
(pregunta al asistente con tool-use, indexación de un Documento para RAG)
en [`04-secuencias.md`](./04-secuencias.md).

## Sprint 2 de Release 4 (2026-07-09)

Alcance redirigido en el Sprint 1 de Análisis
(`../r4-01-vision-y-alcance.md`, sección 0): RabbitMQ/MassTransit,
Elastic/Application Insights y SignalR a escala quedan diferidos sin
Release asignado — ninguno de los tres tenía un caso de uso real probado
en el producto, y este entorno no tiene la infraestructura para
construirlos y verificarlos de verdad. Este Sprint confirma, no
introduce: **ningún contenedor nuevo hace falta** en
`c4-02-contenedores.md` — Temporal Tables (HU-102) vive dentro del mismo
`db` ya modelado, OpenTelemetry corre in-process dentro de `api` con un
exportador local, BenchmarkDotNet es un proyecto que se corre on-demand
(no un servicio en ejecución), y CodeQL/Dependabot/Semantic Versioning
corren en CI, fuera del sistema que un diagrama C4 describe. Mismo
criterio en `c4-01-contexto.md`: sin dependencia externa nueva al
*runtime* del producto.

Un diagrama de secuencia nuevo (consultar el historial de cambios de un
Proyecto vía `FOR SYSTEM_TIME AS OF`, HU-102) en
[`04-secuencias.md`](./04-secuencias.md).

## Índice de vistas

1. [C4 Nivel 1 — Contexto](./c4-01-contexto.md)
2. [C4 Nivel 2 — Contenedores](./c4-02-contenedores.md)
3. [C4 Nivel 3 — Componentes (módulo Proyectos, representativo)](./c4-03-componentes-proyectos.md)
4. [Diagramas de secuencia — Auth, Tenant Resolution, Request Pipeline](./04-secuencias.md)

## Por qué "Proyectos" como módulo representativo
En vez de dibujar el componente de cada uno de los ~10 módulos del MVP (alto
costo de mantenimiento, bajo valor — todos siguen la misma plantilla de
Vertical Slice), se documenta en detalle **un módulo representativo**
(Proyectos, por ser el que tiene más reglas de negocio del MVP: HU-021, HU-023)
y se declara que el resto de módulos del MVP (Empresas, Clientes, Contactos,
Equipos, Tareas, Calendario) replican la misma estructura de carpetas y capas.
Esto evita documentación duplicada — si un módulo se desvía de la plantilla,
esa desviación se documenta puntualmente en su propio ADR corto.
