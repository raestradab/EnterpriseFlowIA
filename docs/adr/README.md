# Architecture Decision Records — índice consolidado

Sprint 10 (Documentación). Cada ADR completo tiene Contexto, Decisión,
Alternativas consideradas y Consecuencias — esta tabla es solo un punto de
entrada rápido, no un sustituto de leerlas.

| # | Título | Decide qué | Relacionado con |
|---|---|---|---|
| [0001](./ADR-0001-alcance-y-estrategia-de-entrega.md) | Estrategia de entrega por Releases | Por qué el MVP (Release 1) excluye IA/Documentos/Notificaciones/Workflow pese a estar en la especificación original, y por qué el ciclo análisis→...→DevOps se aplica por Release, no una sola vez para todo el proyecto | — |
| [0002](./ADR-0002-clean-architecture-vertical-slices.md) | Clean Architecture + Vertical Slice Architecture | Por qué las capas (Domain/Application/Infrastructure/Api) conviven con organización por Feature dentro de Application, en vez de las carpetas técnicas tradicionales (`Controllers/`, `Services/`, `Repositories/`) | ADR-0001 |
| [0003](./ADR-0003-estrategia-multi-tenant.md) | Estrategia de Multi-Tenancy | `TenantId` compartido + Global Query Filter de EF Core generalizado por reflexión sobre `ITenantScoped`, en vez de bases de datos o esquemas separados por tenant | ADR-0002 |
| [0004](./ADR-0004-autorizacion-basada-en-policies.md) | Autorización basada en Policies dinámicas | `IAuthorizationPolicyProvider` que resuelve una Policy por nombre de permiso bajo demanda, en vez de un `AddPolicy(...)` explícito por cada permiso del catálogo | ADR-0002, ADR-0003 |
| [0005](./ADR-0005-invariantes-cross-aggregate.md) | Invariantes cross-aggregate con "hechos inyectados" | Cómo `Project.Close(hasOpenTasks)` o `ProjectTask.AssignTo(userId, isProjectMember)` deciden sobre un hecho que Application consultó cruzando agregados, sin que Domain dependa de EF Core ni de otros agregados directamente | ADR-0002 |
| [0006](./ADR-0006-autenticacion-bypassa-filtro-de-tenant.md) | Login/Registro operan fuera del filtro de tenant | Por qué `LoginCommandHandler`/`RegisterTenantCommandHandler` usan `IgnoreQueryFilters()` explícito — no existe un tenant "actual" antes de autenticar, y el email debe ser único en toda la plataforma | ADR-0003, ADR-0004 |
| [0007](./ADR-0007-refresh-token-en-cookie-httponly.md) | Refresh token en cookie HttpOnly | Por qué el refresh token se transmite como cookie `HttpOnly`+`Secure`+`SameSite=Strict` en vez de en el body JSON/`localStorage` — hallazgo de la revisión de seguridad ad-hoc del 2026-07-07 | ADR-0006, [docs/08a-seguridad.md](../08a-seguridad.md) |
| [0008](./ADR-0008-activacion-redis-hangfire-response-compression.md) | Activación de Redis, Hangfire y Response Compression en Release 2 | Por qué estas tres piezas, diferidas explícitamente en ADR-0001, se activan ahora — cada una atada a un caso de uso concreto de Release 2 (F6.2 correo async, F8.2 cache de catálogos, F4.3 payloads de reportes), no "porque la especificación las pedía" | ADR-0001 |
| [0009](./ADR-0009-abstraccion-almacenamiento-documentos.md) | Abstracción de almacenamiento de Documentos | `IDocumentStorageProvider` + asociación polimórfica Documento→propietario sin FK física, mismo patrón que ADR-0005; una sola implementación activa por instancia, no *hot-swap* en caliente | ADR-0005, ADR-0008 |
| [0010](./ADR-0010-motor-workflow-generico.md) | Motor de Workflow genérico | Máquina de estados orientada a datos (`WorkflowDefinition`/`WorkflowState`/`WorkflowTransition`) en vez de un enum hardcodeado o una librería de terceros — el mismo patrón de "hecho inyectado" de ADR-0005 aplicado a transiciones de estado | ADR-0005 |
| [0011](./ADR-0011-arquitectura-entrega-notificaciones.md) | Entrega de notificaciones reusa Domain Events | Por qué las notificaciones in-app (SignalR) y por correo (Hangfire) se enganchan como handlers adicionales del mismo pipeline de Domain Events de Release 1, en vez de introducir un mecanismo de eventos o un message bus nuevo | ADR-0008 |
| [0012](./ADR-0012-cache-aside-como-pipeline-behavior.md) | Cache-aside como Pipeline Behavior | `CachingBehavior`/`CacheInvalidationBehavior` de MediatR para Catálogos, reusando el mismo mecanismo que `AuthorizationBehavior`/`ValidationBehavior` en vez de llamadas manuales a `IDistributedCache` o un Decorator con una librería nueva | ADR-0008 |
| [0013](./ADR-0013-abstraccion-ia-y-limite-tool-use.md) | Abstracción de proveedores de IA y límite de seguridad del tool-use | `IAiChatClient`/`IEmbeddingClient` (interfaces separadas, corrección de Sprint 3 — Anthropic no ofrece embeddings) sobre OpenAI/Anthropic; el asistente solo puede invocar Queries de Application ya existentes como "herramientas" — nunca SQL directo — para que el aislamiento de tenant (ADR-0003) siga siendo estructural, no una convención de prompt | ADR-0003, ADR-0004, ADR-0009 |
| [0014](./ADR-0014-almacen-de-vectores-para-rag.md) | Almacén de vectores para RAG | Una tabla más en SQL Server con similitud coseno calculada en código de aplicación, filtrada por tenant antes de comparar — no un servicio de vectores dedicado, sin caso de uso real que hoy demuestre falta de escala | ADR-0001, ADR-0008, ADR-0013 |
| [0015](./ADR-0015-temporal-tables-historial-de-cambios.md) | Historial de cambios vía Temporal Tables | SQL Server System-Versioned Temporal Tables en `Projects`/`ProjectTasks` únicamente — sin código de aplicación que mantenga el historial ni pueda saltearlo, en vez de una tabla de auditoría manual o Event Sourcing (desproporcionado para dos entidades) | ADR-0001, ADR-0003 |
| [0016](./ADR-0016-opentelemetry-exportador-local.md) | Tracing distribuido con exportador local | OpenTelemetry (vendor-neutral) sobre ASP.NET Core/HttpClient/EF Core, con exportador `Console` por defecto y `Otlp` configurable — en vez de acoplarse directo al SDK de Application Insights o al agente de Elastic, ninguno con cuenta disponible en este entorno | ADR-0001 |

## Convención

Un ADR nuevo se agrega cuando una decisión técnica tiene alternativas reales
que se descartaron por una razón concreta — no para documentar el único
camino obvio. `especificcion.md` (sección REGLAS) pide justificar
comparando alternativas; estos documentos son donde esa comparación queda
registrada, no solo mencionada de pasada en un commit o un comentario de
código.
