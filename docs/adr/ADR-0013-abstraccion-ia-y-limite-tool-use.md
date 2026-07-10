# ADR-0013: Abstracción de proveedores de IA y límite de seguridad del tool-use

- Estado: Aceptado
- Fecha: 2026-07-08
- Relacionado: ADR-0001 (por qué Release 3 se redefine sin romper el ciclo
  de entrega), ADR-0009 (mismo patrón de abstracción intercambiable por
  configuración), ADR-0003/ADR-0004 (aislamiento de tenant y autorización
  que el tool-use reutiliza, no reemplaza)

## Contexto

F9.1 exige que EnterpriseFlow pueda usar OpenAI o Anthropic como motor del
asistente de IA sin que `Application`/`Domain` sepan cuál está activo.
F9.3 exige que las respuestas del asistente estén ancladas en datos reales
del tenant, sin exponer datos de otro tenant ni requerir que el modelo
tenga acceso directo a la base de datos. El plan original de Release 3
(`backlog/epics.md`, E9/E10/E11 antes del Sprint 1 de este Release) incluía
además un servidor MCP propio y RAG sobre la documentación del proyecto —
redefinido en el Sprint 1 de Análisis hacia un asistente de usuario final
(`r3-01-vision-y-alcance.md`, sección 0).

## Decisión

**Abstracción de proveedor**: `IAiChatClient` en `Application.Abstractions`
(mismo patrón que `IDocumentStorageProvider`/ADR-0009), con una operación
central — enviar mensajes más un catálogo de herramientas disponibles,
recibir una respuesta que es texto final o una solicitud de invocar una
herramienta. Una sola implementación activa por instancia, seleccionada
por configuración (`Ai:Provider`), mismo criterio que `Documents:Provider`.

**Tool-use restringido a Queries de Application ya existentes**: cada
"herramienta" que el catálogo expone al modelo es un adaptador delgado
sobre una Query de MediatR que ya existe (`GetTasksQuery`,
`GetProjectsQuery`, `GetDocumentsQuery`, etc.) — nunca una conexión directa
a `IAppDbContext` ni SQL generado por el modelo. El resultado de una
herramienta pasa por el mismo `AuthorizationBehavior` y el mismo filtro de
tenant que cualquier otro caller de esa Query.

**RAG sobre Documentos del tenant, no documentación del proyecto**: los
vectores de embeddings se asocian al `Document.Id` y heredan su
`TenantId`; la búsqueda de similitud filtra por tenant *antes* de construir
el contexto que se envía al modelo, no después de generar la respuesta.

**Servidor MCP propio: fuera de esta arquitectura por completo** — ningún
componente del sistema expone protocolo MCP; el único cliente del
asistente es la propia SPA de EnterpriseFlow, vía HTTP, igual que cualquier
otro endpoint.

## Alternativas consideradas

- **Herramienta genérica de "ejecutar SQL de solo lectura"**: rechazada —
  aunque sea de solo lectura, rompe el aislamiento de tenant que depende de
  los Global Query Filters de EF Core (ADR-0003): una consulta SQL cruda
  generada por el modelo no pasa por esos filtros salvo que el propio
  prompt se lo pida, y confiar en que el modelo siempre incluya
  `WHERE TenantId = ...` es exactamente el tipo de control "que depende de
  disciplina humana" que ADR-0003 ya rechazó para el resto del sistema —
  aquí, disciplina del modelo, mismo problema.
- **SDK genérico multi-proveedor de terceros** (p. ej. Microsoft.Extensions.AI,
  Semantic Kernel): considerada pero diferida — añadiría una dependencia
  grande con su propia superficie de abstracciones, a menudo redundante con
  el `IAiChatClient` propuesto aquí, antes de tener un caso de uso real
  corriendo contra un proveedor real (sin claves de API en este momento,
  ver `r3-01-vision-y-alcance.md` sección 0). Se reconsidera si un tercer
  proveedor real lo justifica, no antes (YAGNI, mismo criterio que
  ADR-0001 ya aplicó repetidamente).
- **Servidor MCP propio, exponiendo las mismas herramientas también por
  protocolo MCP a un cliente externo**: es literalmente el plan original de
  Release 3 (E11) — rechazado para este Release por decisión explícita del
  usuario (`r3-01-vision-y-alcance.md`, sección 0), no por una razón
  técnica: mantener dos superficies de protocolo (HTTP REST para la SPA,
  MCP para agentes externos) sobre las mismas herramientas no se justifica
  sin un consumidor MCP real.
- **Servicio de vectores dedicado desde el arranque** (Pinecone/Qdrant/
  Azure AI Search): diferida a Sprint 3 (Arquitectura) — no es una decisión
  de este ADR, ver `c4-02-contenedores.md`.

## Consecuencias

- Positivo: agregar un tercer proveedor de IA (p. ej. un modelo
  self-hosted) es una implementación nueva de `IAiChatClient`, no una
  reescritura — mismo argumento que ADR-0009 ya demostró para Documentos.
- Positivo: el límite de seguridad del tool-use es estructural (las
  herramientas *son* Queries existentes con sus propios guards), no una
  convención de prompt ("no muestres datos de otro tenant") que dependería
  de que el modelo la respete siempre.
- Negativo: cada nueva capacidad que el asistente deba tener requiere
  envolver explícitamente una Query como herramienta — no hay una ruta
  genérica de "dale acceso a todo Application de una vez". Aceptado — mismo
  trade-off que ADR-0004 ya aceptó para permisos: explícito por diseño, no
  una superficie abierta por defecto.
- Seguimiento: el SDK concreto para OpenAI/Anthropic, el formato exacto del
  catálogo de herramientas (el function-calling de cada proveedor no es
  idéntico), y el almacén de vectores para RAG se documentan en Sprint 3
  (Arquitectura) y en el Sprint de Backend de Release 3 — este ADR fija el
  contrato y el límite de seguridad, no la implementación.

## Corrección encontrada en Sprint 3 (Arquitectura): `IAiChatClient` se
divide en dos interfaces, no una

Al diseñar el esqueleto concreto de `IAiChatClient` en Sprint 3 quedó claro
que "una operación central de chat + una de embeddings" (como este ADR lo
planteaba originalmente) no puede ser una sola interfaz con una sola
implementación activa: **Anthropic no ofrece una API de embeddings** — solo
completions/chat. Si `Ai:Provider=Anthropic` seleccionara una única
implementación para *ambas* operaciones, RAG (F10) quedaría sin forma de
generar embeddings apenas alguien eligiera Claude como proveedor de chat,
un acoplamiento que ninguna HU pide y que le resta una capacidad real al
sistema sin necesidad.

**Corrección**: `IAiChatClient` (chat/tool-use) e `IEmbeddingClient`
(embeddings) son interfaces separadas, cada una con su propia
configuración de proveedor activo (`Ai:ChatProvider`, `Ai:EmbeddingProvider`)
— una instalación puede usar Claude para el chat y OpenAI para los
embeddings de RAG, o el mismo proveedor para ambos, sin que ninguna capa
de Application lo sepa. Mismo espíritu que ADR-0009 (una interfaz por
capacidad, no una interfaz que mezcla dos capacidades porque hoy da la
casualidad de que un proveedor ofrece las dos).
