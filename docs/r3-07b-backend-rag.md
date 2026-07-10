# Release 3, Sprint 7b — Backend: RAG (indexación + recuperación)

Segunda de tres sub-partes de Sprint 7 (Backend) de Release 3. Se apoya
directamente en 7a: `IEmbeddingClient` real (`OpenAiEmbeddingClient`) ya
existía, `DocumentChunk` (Sprint 5/6) ya tenía Domain y persistencia — este
Sprint construye el pipeline completo que los conecta: extraer texto de un
Documento subido, generarle embeddings, guardarlos, y dejar que el
asistente los busque para anclar sus respuestas (F10, HU-100/HU-101).

## Qué se agregó

- **`IDocumentTextExtractor`** (Application.Abstractions) + `DocumentTextExtractor`
  (Infrastructure, real — sin *fallback* Null: extraer texto no necesita
  clave de API ni servicio externo). Soporta `.txt` (directo),
  `.pdf` (`PdfPig`, Apache-2.0), `.docx` (`DocumentFormat.OpenXml`, SDK de
  Microsoft para OOXML). Cualquier otra extensión (`.png`/`.jpg`/`.xlsx`) o
  contenido sin texto extraíble devuelve `null` — no una excepción.
- **`TextChunker`/`EmbeddingSerializer`/`CosineSimilarity`**
  (Application.Common) — utilidades puras, sin dependencias, 100%
  testeables sin mocks: partición de texto de tamaño fijo con solapamiento,
  conversión `float[]` ↔ `byte[]` (el límite exacto que ADR-0014 fija para
  `DocumentChunk.Embedding`), y similitud de coseno.
- **`DocumentUploadedDomainEvent`** — nuevo evento de dominio, agregado a
  `Document.Create` (mismo patrón que `DocumentWorkflowTransitionedDomainEvent`
  ya estableció). Solo lleva `DocumentId`: `TenantId` todavía no está
  asignado en el momento en que `Create` corre (se asigna después vía
  `AssignTenant`, antes de `SaveChangesAsync`) — el handler vuelve a leer
  el Document completo cuando el evento se despacha, ya con todos los
  campos poblados.
- **`IndexDocumentOnUploadHandler`** — reacciona al evento: descarga el
  archivo (`IDocumentStorageProvider`), extrae texto, lo parte en chunks,
  genera embeddings en lote, borra los chunks previos de ese Documento (una
  re-indexación es idempotente) y persiste los nuevos. **Corre de forma
  síncrona, dentro del mismo request de subida** — así lo definió el
  diagrama de secuencia de Sprint 2, y no hay ningún caso de uso real en
  este Release que justifique agregar un nuevo tipo de job de Hangfire
  solo para esto (ADR-0001). Un fallo de indexación (proveedor caído,
  archivo corrupto) se atrapa y se registra vía `ILogger` — **nunca** tumba
  la subida, que ya se guardó exitosamente antes.
- **`SearchDocumentChunksQuery`/Handler** — HU-101, gateado por
  `documents.read` (el mismo permiso que leer un Documento directamente).
  El filtro de tenant de ADR-0003 actúa primero (los `DocumentChunks` que
  llegan a memoria ya son solo los del tenant de quien pregunta); la
  similitud de coseno se calcula después, en memoria, sobre ese conjunto ya
  acotado — nunca sobre todos los tenants.
- **Herramienta nueva en `AssistantToolCatalog`**: `search_my_documents` —
  recibe un argumento `query` (string), devuelve los 5 fragmentos más
  relevantes. `SendAssistantMessageCommandHandler.InvokeToolAsync` gana un
  caso más, parseando el argumento del modelo con `JsonDocument`.

## Verificación

**El pipeline completo se probó de punta a punta con datos reales, no solo
revisión de código:**

- **13 tests unitarios nuevos** en `EnterpriseFlow.Infrastructure.UnitTests`
  para `DocumentTextExtractor` — incluye un PDF mínimo válido construido a
  mano (con offsets de `xref` calculados en código, no tipeados a mano) y
  un `.docx` construido con el propio *writer* de `DocumentFormat.OpenXml`
  — extracción real verificada contra archivos reales, no solo contra
  `.txt`.
- **12 tests unitarios nuevos** en `EnterpriseFlow.Application.UnitTests`
  para `TextChunker`/`EmbeddingSerializer`/`CosineSimilarity` — lógica pura,
  cubierta con casos borde reales (solapamiento igual al tamaño del chunk,
  vectores ortogonales/opuestos/cero).
- **4 tests de integración nuevos** (`AssistantRagTests`), con
  `FakeEmbeddingClient` reemplazando a `NullEmbeddingClient` — un vector de
  vocabulario fijo pequeño (8 palabras), no un modelo semántico real, pero
  suficiente para que la similitud de coseno separe de verdad "esto es
  relevante" de "esto no lo es":
  - Subir un `.txt` real indexa un `DocumentChunk` real de forma síncrona
    (verificado leyendo la base directamente después del `POST`, sin
    esperar nada — la indexación ya terminó cuando la respuesta HTTP
    vuelve).
  - Preguntarle al asistente por el contenido de un Documento responde con
    texto que viene genuinamente del chunk indexado (no un texto
    genérico).
  - El contenido de un Documento de un tenant nunca aparece en la
    respuesta a otro tenant que hace la misma pregunta.
  - Sin permiso `documents.read`, la pregunta recibe una respuesta de
    denegación legible (`200`, no `500`), sin fuga de contenido.
- Suite completa: **273/273 tests** (141+32+20+6+74).
  `dotnet format --verify-no-changes` limpio.
- `coverlet.runsettings`: `OpenAiChatClient`/`AnthropicChatClient`/
  `OpenAiEmbeddingClient` y sus Options se excluyen del cálculo de
  cobertura — mismo criterio ya aplicado a los proveedores de storage en
  la nube (Release 2, Sprint 9): envuelven un SDK/API real sin clave
  disponible en este entorno. Sus *mappers* (`OpenAiChatMessageMapper`,
  `AnthropicMessageMapper`) quedan deliberadamente **dentro** del cálculo
  — son lógica pura, ya cubierta por pruebas reales.

## Un detalle real encontrado al escribir el test de integración

El primer intento de verificar `DocumentChunks` directamente contra la
base de datos después de la subida devolvió una colección vacía — no
porque la indexación hubiera fallado, sino porque el `scope` manual del
test no tiene ningún `HttpContext` detrás, así que `ICurrentTenantService`
no tiene tenant que usar para el filtro global (ADR-0003), y la consulta
termina sin filtrar nada visible. Corregido con `IgnoreQueryFilters()` —
una lectura intencional del estado crudo persistido, la misma técnica que
cualquier inspección de base de datos fuera de una request autenticada
necesita. Ninguna otra prueba de la suite había necesitado antes leer un
tipo con `TenantId` directamente desde un `scope` manual (las anteriores
siempre verificaban vía la Api autenticada) — primera vez que el patrón
hizo falta.

## Qué no se hizo en este sprint (a propósito)

- Streaming de la respuesta del asistente — diferido desde Sprint 1.
- Endpoint dedicado de búsqueda para el frontend (`GET /api/documents/search`
  o similar) — HU-101 solo pide que el asistente pueda usarlo como
  herramienta, ninguna HU pide una búsqueda de texto libre expuesta
  directamente en la UI todavía.
- Reindexación automática cuando un Documento se reemplaza — F5 (Release 2)
  no tiene un flujo de "reemplazar contenido", solo subir uno nuevo; nada
  que reindexar todavía.
- Más herramientas del asistente (`get_my_tasks`, resúmenes ejecutivos
  HU-093) — Sprint 7c.
