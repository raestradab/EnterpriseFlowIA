# Release 3, Sprint 3 — Arquitectura (esqueleto)

Mismo alcance que Sprint 3 de Release 2: **esqueleto, no una feature real
todavía** — las interfaces cross-cutting y el wiring de infraestructura que
el Sprint de Backend de este Release va a ejercitar con las
implementaciones reales. Ninguna entidad de Domain (Sprint 5), ni carpeta
`Application/Features/{Assistant,Rag}/` todavía — mismo criterio que
Release 2 aplicó (Catálogos apareció en Sprint 4, no en Sprint 3).

## Qué se agregó

**Dos ADRs**, cerrando decisiones que Sprint 1 dejó explícitamente
pendientes para este Sprint:
- [ADR-0014](./adr/ADR-0014-almacen-de-vectores-para-rag.md) — los vectores
  de RAG viven en una tabla más de SQL Server, similitud coseno calculada
  en código de aplicación sobre el subconjunto ya filtrado por tenant — no
  un servicio de vectores dedicado (sin caso de uso real que hoy demuestre
  falta de escala; el corpus relevante para cualquier búsqueda es siempre
  el de **un** tenant, nunca global).
- Corrección a [ADR-0013](./adr/ADR-0013-abstraccion-ia-y-limite-tool-use.md) —
  al diseñar el esqueleto concreto quedó claro que `IAiChatClient` no puede
  cubrir chat *y* embeddings en una sola interfaz: Anthropic no ofrece API
  de embeddings, así que una única implementación activa por "proveedor"
  dejaría sin RAG a cualquiera que eligiera Claude para el chat. Dividido
  en `IAiChatClient`/`IEmbeddingClient`, cada uno con su propia
  configuración de proveedor (`Ai:ChatProvider`/`Ai:EmbeddingProvider`).

**`Application.Abstractions`** (interfaces nuevas, cero implementación
real todavía):
- `IAiChatClient` — una operación (`SendAsync`), mensajes + catálogo de
  herramientas disponibles, respuesta de texto final o solicitud de invocar
  una herramienta. Tipos de soporte en el mismo archivo (`AiChatMessage`,
  `AiChatRole`, `AiToolDefinition`, `AiToolCallRequest`, `AiChatResponse`) —
  mismo patrón que `ITokenService`/`AccessToken`.
- `IEmbeddingClient` — `GenerateEmbeddingsAsync` recibe una lista de textos
  y devuelve sus vectores en el mismo orden, en lote (no una llamada por
  chunk — toda API de embeddings real acepta múltiples entradas por
  request, e indexar un Documento produce muchos chunks a la vez).

**Infrastructure** (`Ai/`):
- `NullAiChatClient` — registrado cuando `Ai:ChatProvider` no está
  configurado. A diferencia de un no-op silencioso, devuelve un mensaje
  explicando que el asistente no está configurado — quien le pregunte algo
  recibe una respuesta honesta, no un vacío sin explicación.
- `NullEmbeddingClient` — registrado cuando `Ai:EmbeddingProvider` no está
  configurado. Devuelve una lista vacía, no una excepción: el manejador de
  indexación de Documentos (HU-100, Sprint de Backend) ya necesita tratar
  "sin contenido indexable" como un caso normal (un PDF escaneado sin capa
  de texto toma el mismo camino) — un proveedor sin configurar cae en ese
  mismo caso, en vez de ser un modo de fallo aparte que el handler tendría
  que distinguir.
- `DependencyInjection.cs`: `Ai:ChatProvider`/`Ai:EmbeddingProvider` leídos
  independientemente; solo se registra el *fallback* Null en este Sprint —
  los `case` reales de OpenAI/Anthropic, y las clases que implementan, se
  agregan en el Sprint de Backend de este Release (mismo split que
  Documentos: Sprint 3 de Release 2 solo dejó la interfaz lista,
  `LocalStorageProvider`/`AzureBlobStorageProvider`/etc. llegaron en
  Sprint 7b).

## Verificación

- **Arranque real de la Api, sin ninguna clave de API configurada**: se
  corrió `dotnet run` contra LocalDB real, sin `Ai:ChatProvider` ni
  `Ai:EmbeddingProvider` en el entorno. Log limpio, sin errores; `GET /health`
  respondió `Healthy`. Confirma que el esqueleto de IA no rompe el arranque
  del resto del sistema aunque no haya ninguna clave real disponible — el
  mismo criterio de degradación elegante que Redis/Hangfire/SMTP ya
  probaron en Release 2.
- Los 66 tests de `Api.IntegrationTests` ya construyen el contenedor de DI
  completo vía `CustomWebApplicationFactory` en cada corrida — esto
  significa que, sin buscarlo explícitamente, **ya venían verificando
  implícitamente** que el wiring de `IAiChatClient`/`IEmbeddingClient` no
  rompe nada, desde el momento en que se agregó. Suite completa:
  **218/218 tests** sin cambios, `dotnet format --verify-no-changes` limpio.
- **Sin verificación real de OpenAI/Anthropic** — no hay implementaciones
  concretas todavía que verificar (llegan en el Sprint de Backend), y sin
  claves de API disponibles en este entorno de todos modos
  (`r3-01-vision-y-alcance.md`, sección 0). Dicho explícitamente, no
  asumido.

## Qué no se hizo en este sprint (a propósito)

- Ninguna entidad de Domain (`AssistantConversation`, `AssistantMessage`,
  `DocumentChunk`/vectores) — Sprint 5 (Modelo de Dominio).
- Ninguna carpeta `Application/Features/{Assistant,Rag}/` todavía — Sprint
  de Backend, con el primer slice vertical real y probado.
- Las implementaciones concretas `OpenAiChatClient`/`AnthropicChatClient`/
  el cliente de embeddings real — Sprint de Backend, mismo split que
  Documentos ya estableció en Release 2.
- El diseño exacto de la tabla de vectores (columnas, tamaño de chunk,
  índices) — Sprint de Modelo de Dominio/Base de Datos; ADR-0014 fija
  *dónde* viven y *cómo* se buscan, no el esquema.
- Endpoints de Api para el chat (`POST /api/assistant/messages` del
  diagrama de secuencia de Sprint 2) — Sprint de Backend.
