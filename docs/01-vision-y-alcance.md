# EnterpriseFlow AI — Visión y Alcance

## 1. Propósito

EnterpriseFlow AI es un producto SaaS de gestión empresarial (proyectos, clientes,
equipos, documentos, workflow) con un Asistente de IA embebido (RAG + MCP) capaz de
responder preguntas sobre el propio sistema y asistir en tareas de ingeniería
(historias de usuario, tests, SQL, documentación).

El propósito declarado es servir como **portafolio técnico**: debe demostrar
criterio de Staff Engineer / Solution Architect, no solo cobertura de funcionalidades.
Esto cambia las prioridades frente a un SaaS comercial real: **la calidad,
justificación y consistencia de las decisiones técnicas pesan tanto como el
alcance funcional**.

## 2. Problema de alcance (léase antes de continuar)

La especificación original enumera ~14 áreas funcionales/técnicas, cada una
comparable en tamaño a un producto propio (identidad multi-tenant, gestión de
proyectos, documentos, workflow, IA, RAG, MCP, observabilidad completa, DevOps,
90%+ cobertura de pruebas). Construir todo simultáneamente con calidad "Staff
Engineer" no es viable en un único tramo de trabajo continuo.

**Decisión de alcance**: en vez de construir 14 áreas a medio terminar, se define
un **MVP acotado (Release 1)** que se lleva a un estándar de calidad completo
(arquitectura, pruebas, documentación, DevOps básico), y el resto se organiza en
releases posteriores. Esto es consistente con la regla del documento: *"Cada
Sprint debe finalizar completamente antes de iniciar el siguiente"* — aplicada
aquí a nivel de Release, no solo de capa técnica.

Ver el desglose completo en [`02-roadmap.md`](./02-roadmap.md) y
[`backlog/epics.md`](./backlog/epics.md).

## 3. Alcance del MVP (Release 1)

**Incluido:**
- Identidad multi-tenant: registro de tenant, login JWT + refresh token, roles,
  permisos, autorización basada en políticas, menú dinámico según permisos.
- Entidades núcleo de negocio: Empresas, Clientes, Contactos, Proyectos, Equipos,
  Tareas, Calendario — con auditoría (created/modified by-when) y soft delete.
- Dashboard ejecutivo con indicadores clave y gráficas sobre las entidades núcleo.
- Perfil de usuario y configuración básica de cuenta.
- Observabilidad base: logging estructurado (Serilog), health checks, manejo de
  errores consistente (ProblemDetails).
- Calidad: pruebas unitarias y de integración sobre el dominio y los endpoints
  del MVP, pipeline de CI (build + test) en GitHub Actions.

**Explícitamente fuera del MVP** (quedan en el backlog priorizado, no se
implementan "a medias" dentro de Release 1):
- Documentos (almacenamiento intercambiable local/Azure/S3/GCS).
- Notificaciones (SignalR, correo).
- Workflow configurable, Catálogos dinámicos, módulo de Configuración avanzada.
- Reportes avanzados más allá del dashboard.
- AI Assistant, RAG, servidor MCP propio.
- Redis, Hangfire, MassTransit/RabbitMQ, OpenTelemetry/Elastic/App Insights,
  Rate limiting, Response compression — se introducen cuando exista una
  necesidad real del MVP que los justifique (ver ADR-0001, sección "YAGNI
  táctico").

Esta lista **no es un recorte de calidad** — es orden de entrega. Cada elemento
excluido está documentado como Epic en el backlog con su release objetivo.

## 4. Criterios de éxito del MVP

1. La solución compila y se ejecuta con `docker-compose up` sin pasos manuales
   adicionales.
2. Un usuario puede: registrar tenant → iniciar sesión → crear una Empresa, un
   Cliente y un Proyecto → asignar Tareas a un Equipo → verlo reflejado en el
   Dashboard.
3. Cobertura de pruebas del dominio y casos de uso (Application layer) medible y
   reportada en CI (el 90% del documento original se trata como meta agregada
   del producto completo, no como bloqueo de cada PR del MVP — ver ADR-0001).
4. Cada decisión de patrón (Repository, Specification, CQRS, etc.) tiene un ADR
   o al menos una justificación en el PR que la introduce.
5. Un tercero (reclutador/arquitecto) puede clonar el repo, leer el README y
   entender en <10 minutos qué hace el sistema y por qué está construido así.

## 5. No-objetivos

- No es un CRUD genérico: las entidades núcleo tienen reglas de negocio propias
  (p. ej. un Proyecto no puede cerrarse con Tareas abiertas, un Contacto
  pertenece a un Cliente dentro del mismo Tenant, etc. — detalladas en las
  Historias de Usuario).
- No se optimiza prematuramente: Redis, MassTransit, Hangfire, OpenTelemetry no
  se añaden "porque la lista los pide", sino cuando un caso de uso del MVP o de
  un release posterior los necesite, con su ADR correspondiente.
