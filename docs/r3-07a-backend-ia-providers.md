# Release 3, Sprint 7a — Backend: proveedores reales de IA

Primera parte de Sprint 7 (Backend) de Release 3 — dividido en 7a/7b/7c
por la misma razón que Release 2 dividió su Sprint 7: alcance demasiado
grande para un solo corte de análisis→código→verificación. 7a es la
capa fundacional (los proveedores reales que Sprint 3 dejó como
interfaces) — 7b (RAG) y 7c (más herramientas del asistente) se apoyan en
ella.

## Qué se agregó

- **`OpenAiChatClient`** (`IAiChatClient`) — SDK oficial `OpenAI` (paquete
  `OpenAI`, no un envoltorio de terceros). Traduce hacia/desde el formato
  de function-calling de OpenAI vía `OpenAiChatMessageMapper`.
- **`AnthropicChatClient`** (`IAiChatClient`) — **sin SDK**: Anthropic no
  publica un SDK oficial de .NET, así que se implementó directo contra su
  REST API (`POST /v1/messages`) con `HttpClient` (`AddHttpClient<T>()`,
  no `new HttpClient()` — evita agotamiento de sockets). Traduce
  vía `AnthropicMessageMapper`.
- **`OpenAiEmbeddingClient`** (`IEmbeddingClient`) — SDK oficial, en lote.
  Es la única implementación real de `IEmbeddingClient` en el sistema —
  Anthropic no tiene API de embeddings (corrección de ADR-0013, Sprint 3).
- `OpenAiOptions`/`AnthropicOptions` — `Ai:OpenAi`/`Ai:Anthropic`, con
  `ApiKey` requerido y `ValidateOnStart()`.
- DI (`DependencyInjection.cs`): `Ai:ChatProvider`/`Ai:EmbeddingProvider`
  ahora resuelven a las implementaciones reales cuando valen `"openai"` u
  `"anthropic"` — completa lo que Sprint 3 dejó como solo el *fallback*
  Null.
- **`EnterpriseFlow.Infrastructure.UnitTests`** (proyecto nuevo) — la
  lógica de traducción de protocolo es pura (sin red, sin base de datos),
  a diferencia del resto de Infrastructure (que se prueba vía
  `Api.IntegrationTests` contra una app completa). Forzar estas 13 pruebas
  a pasar por un `WebApplicationFactory` habría sido lento y no habría
  probado nada que un test unitario aislado no probara mejor — primera vez
  que este proyecto necesita ese tipo de prueba.

## Por qué Anthropic no tiene SDK, y por qué eso no fue un problema

No existe un paquete oficial de Anthropic para .NET. Las alternativas
consideradas:
- **Paquete comunitario no oficial** (ej. `Anthropic.SDK`): rechazado —
  depender de un paquete no mantenido por Anthropic mismo para algo tan
  central (el proveedor de IA por defecto que la documentación del
  proyecto usa como ejemplo) es un riesgo de mantenimiento que implementar
  contra la REST API documentada evita por completo.
- **REST API directa con `HttpClient`** (elegida): la API de Anthropic
  (`Messages`) es un único endpoint POST con JSON — no hay tanta
  superficie que un SDK ahorre. Escribirla a mano además obligó a entender
  de verdad las diferencias de protocolo frente a OpenAI (ver abajo), en
  vez de que un SDK las escondiera.

## El hallazgo real de este Sprint: dos protocolos de tool-use genuinamente distintos

Construir ambos clientes contra el mismo `IAiChatClient` expuso una
diferencia de protocolo entre OpenAI y Anthropic que Sprint 3 no había
anticipado en detalle:

- **OpenAI**: `system` es un mensaje más dentro de `messages`
  (`SystemChatMessage`). Los resultados de herramientas van como mensajes
  `role: "tool"` independientes, uno por cada `tool_call_id`.
- **Anthropic**: `system` es un campo de nivel superior del request, *no*
  un mensaje. Solo existen los roles `user`/`assistant` — una petición de
  herramienta es un bloque `tool_use` dentro de un mensaje `assistant`, y
  **todos** los resultados que responden a esa misma petición deben ir
  como bloques `tool_result` dentro de un **único** mensaje `user` — no
  como mensajes `user` separados y consecutivos.

Esta segunda diferencia expuso un bug real en el propio diseño de
`SendAssistantMessageCommandHandler` (Sprint 4): el handler nunca volvía a
insertar el turno del asistente que pidió la herramienta, solo el
resultado — lo cual OpenAI también rechaza (un mensaje `tool` sin un
mensaje `assistant` con `tool_calls` inmediatamente antes es una petición
malformada). Corregido extendiendo `AiChatMessage` con un campo
`ToolCalls` opcional (además de `ToolCallId`/`ToolName`, ya existentes) y
haciendo que el handler reproduzca el turno del asistente antes de
agregar cada resultado — un hallazgo real de compatibilidad entre
proveedores, encontrado al construir el segundo cliente, no al diseñar el
primero.

## Verificación

**No hay claves de API reales en este entorno** (dicho desde Sprint 1) —
ninguno de los dos clientes se pudo ejercitar contra la API real. Lo que
sí se verificó:

- **13 pruebas unitarias nuevas** de la lógica de traducción pura, sin red:
  `AnthropicMessageMapperTests` cubre las dos direcciones completas
  (`BuildRequestBody`/`ParseResponse`, incluida la fusión de resultados de
  herramientas en un solo turno `user`) porque ahí no hay ningún tipo
  opaco del SDK en el medio. `OpenAiChatMessageMapperTests` cubre solo el
  lado de *request* (`ToOpenAi`) — `ChatCompletion` (la respuesta) no tiene
  constructor público en el SDK oficial, así que ese lado no se pudo
  probar de forma aislada sin una llamada real; dicho explícitamente en el
  comentario de la clase de prueba, no simulado con un doble falso que
  daría una falsa sensación de cobertura.
- **Arranque real de la Api con cada proveedor configurado** (clave
  *dummy*, sin llamadas salientes): tanto `Ai:ChatProvider=openai` como
  `Ai:ChatProvider=anthropic` resuelven su cliente vía DI sin error,
  `/health` responde `Healthy` — prueba que el *wiring* en sí es correcto
  (construcción del cliente, opciones, tipado del `HttpClient`), aunque no
  la llamada HTTP real.
  - Nota: el registro de `AnthropicChatClient` con `AddHttpClient<T>()`
    apunta a `https://api.anthropic.com/` desde el propio constructor —
    quedó fuera del alcance de esta verificación confirmar que ese host
    responde (no hay clave real para completar el *round-trip*).
- **Falla rápida cuando falta la clave**: `Ai:ChatProvider=openai` sin
  `Ai:OpenAi:ApiKey` produce un `OptionsValidationException` claro al
  arrancar (`ValidateOnStart()`), no un error diferido a la primera
  petición — mismo patrón que Documentos (Release 2) ya estableció para
  sus proveedores en la nube.
- Suite completa: **249/249 tests** (140+20+13+6+70).
  `dotnet format --verify-no-changes` limpio.

## Qué no se hizo en este sprint (a propósito)

- Ninguna herramienta nueva en `AssistantToolCatalog` más allá de
  `get_my_projects` (ya existente desde Sprint 4) — Sprint 7c.
- Ninguna indexación de RAG (extracción de texto, chunking, embeddings de
  Documentos reales) — Sprint 7b, que ya puede apoyarse en
  `IEmbeddingClient` real construido aquí.
- Streaming de respuestas — diferido explícitamente desde Sprint 1.
