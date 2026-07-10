# Release 3, Sprint 9 — Pruebas

Mismo alcance que Sprint 9 de Release 2: no features nuevas — correr
cobertura real, cerrar huecos genuinos que las 8 sprints anteriores
dejaron pasar, y revisar que la clase de bug ya encontrada dos veces
(`ORDER BY` sobre `DateTimeOffset` en SQLite) no se haya colado de nuevo
en código más reciente.

## Cobertura antes de este Sprint

`dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"`
+ `reportgenerator`: **94.7%** de líneas, **83.9%** de ramas — por encima
de la meta del 90% ya establecida, pero con huecos reales identificables
clase por clase, no solo el número agregado.

## Huecos reales encontrados y cerrados

- **`NullAiChatClient`/`NullEmbeddingClient` en 0%** — ninguna prueba
  existente los ejercitaba: **todos** los tests de asistente/RAG
  reemplazan `IAiChatClient`/`IEmbeddingClient` con los *fakes* de Sprint
  4/7b (`FakeAiChatClient`/`FakeEmbeddingClient`) para tener una señal real
  con la que trabajar. Eso dejó el camino de degradación elegante real —
  el que corre de verdad en este entorno, sin claves de API — sin ninguna
  prueba automatizada. Se agregaron:
  - 2 pruebas unitarias triviales (`NullAiChatClientTests`/
    `NullEmbeddingClientTests`, `EnterpriseFlow.Infrastructure.UnitTests`)
    documentando el contrato exacto de cada clase.
  - **`NullAiWebApplicationFactory`** (nueva) — subclase de
    `CustomWebApplicationFactory` (se le quitó `sealed` para permitirlo)
    que reutiliza toda su configuración vía `override` y solo revierte
    `IAiChatClient`/`IEmbeddingClient` a las implementaciones Null reales,
    en vez de duplicar toda la configuración de SQLite/entorno de pruebas.
    Con ella, `AssistantNullAiTests` prueba **de punta a punta, por HTTP
    real**: preguntarle algo al asistente sin proveedor configurado
    devuelve el mensaje real de `NullAiChatClient` (no un 500), y subir un
    Documento sin proveedor de embeddings configurado indexa cero
    `DocumentChunks` sin que la subida falle — exactamente el escenario de
    "recién desplegado, todavía sin claves de API" que el proyecto lleva
    documentando desde Sprint 1 de este Release, ahora con una prueba real
    detrás en vez de solo la palabra.
- **Extensión de archivo no soportada nunca probada** —
  `IndexDocumentOnUploadHandler`'s rama `text is null` (HU-100: un archivo
  sin extractor de texto se guarda igual, simplemente no participa en RAG)
  no tenía ninguna prueba ejercitándola — todas las pruebas de Sprint 7b
  subían `.txt`. Se agregó `Uploading_An_Unsupported_File_Type_Uploads_Successfully_But_Creates_No_Chunks`
  (`AssistantRagTests`), subiendo un `.png` real (con los bytes mágicos
  correctos para pasar `FileSignatureValidator`, Release 2) y confirmando
  que la subida responde `200` pero no crea ningún `DocumentChunk`.

## Un hueco identificado y aceptado sin forzar una prueba

`SearchDocumentChunksQueryHandler`'s rama `queryEmbeddings.Count == 0`
(sin proveedor de embeddings configurado) sigue sin una prueba de
integración directa: esa Query solo es alcanzable a través de la
herramienta `search_my_documents` del asistente, y `NullAiChatClient`
nunca pide ninguna herramienta (responde texto final de inmediato) — no
hay forma de llegar a esa rama por HTTP sin una tercera combinación de
*fakes* (cliente de chat que sí pide la herramienta + cliente de
embeddings Null). Se evaluó construir esa combinación y se decidió que el
costo (una fábrica más, un *fake* más) no se justifica para dos líneas de
una rama que ya comparte la misma lógica (`if (count == 0) return`) que
`IndexDocumentOnUploadHandler` — que sí quedó cubierta. Documentado en vez
de forzado.

## Revisión: ¿el bug de `ORDER BY DateTimeOffset` volvió a aparecer?

Encontrado dos veces antes en este proyecto (Release 2 Sprint 9,
`GetMyNotificationsQueryHandler`; Release 3 Sprint 4,
`SendAssistantMessageCommandHandler` — el mismo Sprint que ya lo había
corregido en `GetAssistantMessagesQueryHandler` sin notar que se repetía
en el propio Command handler). Se revisaron todos los `OrderBy`/
`OrderByDescending` sobre columnas de fecha en `Application`:
`GetAssistantMessagesQueryHandler`/`SendAssistantMessageCommandHandler`/
`GetMyNotificationsQueryHandler` — los tres ya materializan antes de
ordenar (corregidos). `GetTasksQueryHandler` ordena por `DueDate` dentro
de la propia consulta LINQ-a-SQL — pero `DueDate` es `DateOnly?`, no
`DateTimeOffset`, un tipo que SQLite sí sabe traducir en `ORDER BY` (a
diferencia de `DateTimeOffset`, que su proveedor de EF Core rechaza
explícitamente) — confirmado porque las pruebas existentes de
`ProjectTasksEndpointsTests` ya ejercitan ese camino contra SQLite y
pasan. Sin ningún caso nuevo que corregir.

## Verificación

- Suite completa: **281/281 tests** (141 Domain + 32 Application + 22
  Infrastructure + 6 Architecture + 80 Api.IntegrationTests) — 5 pruebas
  nuevas de este Sprint.
- Cobertura después: **94.9%** de líneas (arriba de 94.7%), **84.3%** de
  ramas (arriba de 83.9%).
- `dotnet format --verify-no-changes` limpio.
- `coverage-results/`/`coverage-report/` ya estaban en `.gitignore` desde
  Release 1 — no se agregó nada nuevo a ignorar.

## Qué no se hizo en este sprint (a propósito)

- No se persiguió el 100% de cobertura como objetivo en sí mismo — los
  huecos restantes (`Program.cs` al 73.7%, ramas de arranque de Hangfire/
  Redis condicionadas a configuración, `AuthEndpoints` al 98%) son en su
  mayoría caminos ya evaluados y aceptados en Releases anteriores, sin
  relación con Release 3.
- No se agregó una tercera combinación de *fakes* para cerrar la única
  rama de `SearchDocumentChunksQueryHandler` que queda sin probar
  directamente (ver arriba) — costo desproporcionado al valor real.
