# Release 3, Sprint 4 — Validación de arquitectura

Mismo criterio que Sprint 4 de Release 2: el primer slice vertical real,
completo (Domain → Base de Datos → Application → Api → pruebas), elegido
para probar que el esqueleto de Sprint 3 (`IAiChatClient`/`IEmbeddingClient`,
ADR-0013) funciona de verdad — no otra ronda de diseño en abstracto.

## Por qué el chat con tool-use (HU-091/HU-092), no RAG

Entre F9 (asistente) y F10 (RAG), F9 es el prerrequisito: el flujo de
recuperación de RAG (F10.3) se expone naturalmente como *otra herramienta*
más que el asistente puede invocar ("buscar en mis Documentos") — sin el
mecanismo de tool-use ya funcionando, RAG no tiene dónde engancharse.
Mismo razonamiento que ya priorizó Catálogos en Release 2: la pieza más
simple que prueba el mecanismo central, no la más completa.

## Qué se construyó

- **Domain**: `AssistantMessage` — sin agregado `Conversation` separado
  (ninguna HU pide administrar múltiples hilos con nombre, solo "recordar
  el contexto de mi conversación actual"); mismo criterio que `Notification`
  (Release 2): una entidad simple, sin padre, con `TenantId`/`UserId`.
- **Base de datos**: migración `AddAssistantMessages`, índice
  `(TenantId, UserId, CreatedAtUtc)` — el único patrón de lectura real
  (cargar el historial propio en orden). Verificada aplicando la cadena
  completa de migraciones contra una base LocalDB nueva.
- **Application** (`Features/Assistant/`):
  - `AssistantToolCatalog` — el catálogo completo de lo que el modelo puede
    pedir, hoy con una sola herramienta real (`get_my_projects`, envuelve
    `GetProjectsQuery`).
  - `SendAssistantMessageCommand`/Handler — el loop de tool-use: persiste
    el mensaje del usuario, arma el historial + prompt de sistema, llama a
    `IAiChatClient.SendAsync`, resuelve cada `AiToolCallRequest` invocando
    la Query real vía `ISender` (mismo pipeline de `AuthorizationBehavior`
    que cualquier otro caller), vuelve a llamar al modelo con el resultado,
    hasta obtener texto final (tope de 5 iteraciones). Un
    `ForbiddenAccessException` de una herramienta se atrapa y se convierte
    en un mensaje de error legible para el modelo, no en un 403 que tumbe
    toda la conversación.
  - `GetAssistantMessagesQuery`/Handler — historial del usuario actual.
- **Api**: `POST /api/assistant/messages`, `GET /api/assistant/messages`
  (`RequireAuthorization()` a nivel de grupo, sin permiso propio — el
  límite de seguridad vive en cada herramienta individual, no en el
  endpoint de chat, tal como fija ADR-0013).

## Un bug real, encontrado por la propia suite (otra vez el mismo patrón de Sprint 9)

`SendAssistantMessageCommandHandler` ordenaba el historial con
`.OrderByDescending(m => m.CreatedAtUtc)` **dentro** de la consulta LINQ —
exactamente el mismo error que `GetMyNotificationsQueryHandler` cometió en
Release 2/Sprint 9, y que **ya había corregido** en
`GetAssistantMessagesQueryHandler` de este mismo Sprint... pero no en el
propio Command handler, que tiene su propia carga de historial
independiente. SQLite (motor de la suite de integración) no traduce ese
`ORDER BY`; SQL Server sí, así que habría llegado a producción sin que
ningún test lo notara de no ser por esta misma suite. Corregido
materializando primero y ordenando en memoria — misma solución, aplicada
esta vez en dos lugares, no uno.

## Verificación

**El mecanismo central — no solo que compila, sino que el modelo
efectivamente no puede ver más de lo que el usuario ya podía ver — se
probó con un doble real, no una respuesta enlatada.** `FakeAiChatClient`
(reemplaza a `NullAiChatClient` en `CustomWebApplicationFactory`, sin
claves de API reales disponibles — `r3-01-vision-y-alcance.md`, sección 0)
ejecuta un **loop real de dos idas y vueltas**: en la primera llamada pide
la herramienta disponible; en la segunda, ve el resultado real que esa
herramienta devolvió y arma su respuesta a partir de ese texto — no puede
"inventar" una respuesta sin pasar por una herramienta real, porque el
propio *fake* no tiene otra fuente de la que tomar los datos.

- **Respuesta anclada en datos reales**: la pregunta "¿qué proyectos
  tengo?" devuelve una respuesta que contiene el nombre real de un
  Proyecto sembrado directamente en la base — no un texto genérico.
- **Historial persistido correctamente**: tras una pregunta, el historial
  tiene exactamente 2 mensajes (Usuario + Asistente), en el orden correcto.
- **Denegación de permiso, sin romper la conversación**: un usuario sin
  `projects.read` recibe una respuesta de texto explicando que no tiene
  permiso — `200 OK`, no un `403` ni un `500` — y esa respuesta **no**
  contiene ningún dato del Proyecto que sí existía.
- **Aislamiento de tenant real, no solo descrito**: un Proyecto sembrado
  para el tenant A nunca aparece en la respuesta que recibe un usuario del
  tenant B, aunque ambos hagan exactamente la misma pregunta — la Query que
  la herramienta invoca ya filtra por tenant (ADR-0003) antes de que el
  modelo vea el resultado, exactamente lo que ADR-0013 prometía.
- Suite completa: **228/228 tests** (132+20+6+70 — 4 nuevas de integración,
  5 nuevas de Domain), `dotnet format --verify-no-changes` limpio.
- **Sin verificación real de OpenAI/Anthropic** — mismo límite ya declarado
  en Sprint 3: sin implementaciones concretas todavía (llegan en el Sprint
  de Backend) y sin claves de API disponibles en este entorno de todos
  modos.

## Qué no se hizo en este sprint (a propósito)

- RAG (F10) — ninguna entidad ni handler nuevo; depende del mecanismo de
  tool-use que este Sprint recién valida, se construye en un Sprint
  posterior con su propia herramienta ("buscar en mis Documentos").
- Más de una herramienta real — `get_my_projects` es suficiente para
  probar el mecanismo; agregar `get_my_tasks`/`get_my_documents`/etc. es
  trabajo del Sprint de Backend, no de Validación.
- Streaming de la respuesta — ya diferido explícitamente en Sprint 1.
- Cualquier endpoint o componente de frontend — Sprint de Frontend.
