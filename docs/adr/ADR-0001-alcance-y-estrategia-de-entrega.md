# ADR-0001: Estrategia de entrega por Releases y decisiones de alcance iniciales

- Estado: Aceptado
- Fecha: 2026-07-06

## Contexto

La especificación (`especificcion.md`) solicita simultáneamente: ~20 módulos de
negocio, un stack backend/frontend completo, un módulo de IA con RAG y un
servidor MCP propio, observabilidad end-to-end, DevOps maduro y cobertura de
pruebas ≥90%. Construir todo a la vez, con la profundidad de justificación que
el propio documento exige ("explicar cada decisión técnica", "comparar
alternativas antes de implementar"), no es compatible con producir un
resultado de calidad "Staff Engineer" en un único tramo continuo de trabajo.

La regla del documento — *"Cada Sprint debe finalizar completamente antes de
iniciar el siguiente"* — es la que se usa para resolver esta tensión.

## Decisión

1. **Se define un Release 1 (MVP) acotado** a: identidad multi-tenant,
   entidades núcleo de negocio (Empresas/Clientes/Contactos/Proyectos/Equipos/
   Tareas/Calendario), dashboard básico, auditoría, y una base de calidad/DevOps
   (tests + CI + Docker). Detalle en `01-vision-y-alcance.md`.
2. **IA, RAG y MCP se tratan como Release 3**, no como una capa transversal
   presente desde el día uno. Razón: requieren que exista ya un sistema (código,
   documentación, base de datos) sobre el cual operar; construirlos antes sería
   simular contra un sistema vacío.
3. **La meta de cobertura ≥90% se trata como meta agregada de producto
   completo (Release 4)**, no como gate bloqueante de cada PR del MVP. Cada PR
   del MVP sí requiere pruebas para su código nuevo (regla de no-regresión),
   pero no se bloquea por un umbral global prematuro.
4. **Redis, Hangfire, MassTransit/RabbitMQ, OpenTelemetry, Elastic Search,
   Application Insights, Rate Limiting, Response Compression se difieren** hasta
   que un caso de uso concreto del MVP o de un Release posterior los requiera.
   Añadirlos sin un caso de uso real violaría YAGNI y añadiría superficie de
   fallo no justificada — contradictorio con "explicar por qué mejora el
   rendimiento/mantenibilidad", que exige una razón concreta, no anticipada.
5. **Repository Pattern se usa únicamente sobre agregados donde EF Core
   `DbContext` expuesto directamente generaría acoplamiento indebido en la capa
   de Application** (p. ej. cuando se necesita Specification Pattern para
   componer filtros complejos reutilizables). Para lecturas simples de
   proyección (dashboards, listados), se usa el `DbContext`/queries directas en
   la capa de Application vía MediatR queries, evitando una capa de repositorio
   que no añadiría valor (repositorio "porque sí" sobre CRUD trivial es
   duplicación de abstracción, prohibida explícitamente por la regla "No
   generar código duplicado").
6. **Mapster vs AutoMapper**: no se elige a priori. Se implementa un benchmark
   con BenchmarkDotNet en el Sprint de Backend del MVP sobre los mapeos reales
   del dominio, y el resultado (con datos) decide cuál se usa en el código de
   producción. Solo uno queda en el código final — mantener ambos sería
   duplicación, no comparación.
   > **Resuelto de otra forma, auditado en Release 4 Sprint 10**: el benchmark
   > nunca se hizo porque nunca hizo falta — los Query Handlers (CQRS,
   > Vertical Slice) proyectan directo de `IQueryable<Entity>` a un DTO
   > record vía `.Select(e => new Dto(...))`, que EF Core traduce a un
   > `SELECT` de columnas específicas (ver p. ej.
   > `GetProjectByIdQueryHandler`). Nunca existe un paso intermedio de
   > "entidad completa materializada → objeto mapeado campo a campo", que es
   > el problema que AutoMapper/Mapster resuelven — comparar rendimiento
   > entre ambos habría sido optimizar un paso que el propio diseño CQRS ya
   > elimina. En el lado de escritura, los Command Handlers construyen
   > agregados vía sus factory methods (`Project.Create(...)`, etc.), nunca
   > por reflexión de un mapper genérico — eso es intencional (ADR de DDD:
   > un mapper no puede saber qué invariantes de dominio aplicar). Ninguna
   > librería quedó sin usar por descuido; la necesidad que motivó este
   > punto simplemente no se materializó.

## Alternativas consideradas

- **Big-bang (construir las 14 Epics en paralelo)**: descartada. Maximiza
  alcance nominal pero garantiza superficialidad en cada área — contradice el
  objetivo declarado de "parecer desarrollado por un equipo senior durante
  varios meses", que se demuestra con profundidad, no con amplitud vacía.
- **MVP mínimo sin reglas de negocio (CRUD puro)**: descartado. El propio
  documento prohíbe "construir un CRUD"; por eso el MVP incluye invariantes de
  dominio explícitas (ver HU-021, HU-012, HU-023 en el backlog).
- **Cobertura 90% desde el primer commit**: descartada como gate temprano;
  ralentiza la fase de exploración arquitectónica (Sprints 2-4) sin beneficio
  proporcional, y se retoma con fuerza en Release 4 cuando el diseño ya es
  estable.

## Consecuencias

- Positivo: cada Release entregado es demostrable y completo, no un conjunto de
  features a medio hacer. Mejor para portafolio (un reclutador ve un sistema
  terminado, aunque acotado, en vez de 14 sistemas a medias).
- Positivo: las decisiones diferidas (Redis, MCP, RAG, etc.) tienen su propio
  ADR cuando se implementen, con contexto real del sistema ya construido en
  vez de suposiciones.
- Negativo: el README inicial no podrá listar "todas las tecnologías
  implementadas" desde el Release 1 — se mitiga documentando el roadmap
  completo y el estado de cada Epic (ver `02-roadmap.md`), de forma que el
  alcance total siga siendo visible aunque no esté todo construido aún.
- Seguimiento: este ADR se revisa al cierre de cada Release para confirmar que
  la priorización sigue vigente o requiere ajuste. Revisado explícitamente en
  Release 4 Sprint 10 (`r4-10-documentacion.md`): punto 6 resuelto (ver nota
  arriba), punto 5 confirmado sin cambios (Repository/Specification Pattern
  nunca se necesitaron), puntos 1-4 vigentes.
