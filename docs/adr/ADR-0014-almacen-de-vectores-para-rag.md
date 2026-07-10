# ADR-0014: Almacén de vectores para RAG — tabla en SQL Server, no un vector DB dedicado

- Estado: Aceptado
- Fecha: 2026-07-08
- Relacionado: ADR-0001 (activar infraestructura por caso de uso real, no
  especulativamente), ADR-0008 (mismo criterio ya aplicado a Redis/
  Hangfire/RabbitMQ), ADR-0013 (abstracción de IA), ADR-0003 (aislamiento
  de tenant, que la búsqueda de similitud debe respetar)

## Contexto

F10.1/F10.2 (RAG sobre Documentos del tenant) necesitan persistir
embeddings — vectores de punto flotante generados a partir del texto
extraído de cada Documento — y poder buscar los más similares a la
pregunta de un usuario (F10.3), filtrado por `TenantId`. `r3-01-vision-y-alcance.md`
(sección 3) dejó esta decisión explícitamente pendiente para este Sprint.

## Decisión

**Los vectores se guardan en una tabla más de SQL Server** (diseño de
entidad concreto en el Sprint de Modelo de Dominio/Base de Datos de este
Release, no aquí), serializados como un arreglo de `float` — no un
servicio de vectores dedicado (Pinecone/Qdrant/Azure AI Search/pgvector).
La búsqueda de similitud (coseno) se calcula **en código de aplicación**,
sobre el subconjunto de filas ya filtrado por `TenantId` — nunca una
búsqueda aproximada (ANN) sobre el corpus completo de todos los tenants.

## Alternativas consideradas

- **Servicio de vectores dedicado** (Qdrant, Pinecone, Azure AI Search):
  rechazado para este Release — es infraestructura nueva sin un caso de uso
  que hoy demuestre que hace falta *indexación aproximada a escala*. El
  corpus relevante para una búsqueda de similitud siempre está acotado a
  documentos de **un** tenant (ADR-0003) — no es una búsqueda global sobre
  millones de vectores, es una búsqueda sobre los documentos que un tenant
  individual subió, un volumen que un escaneo lineal en memoria resuelve en
  milisegundos sin necesitar un índice aproximado. Mismo criterio que
  ADR-0008 ya aplicó a RabbitMQ/OpenTelemetry: sin un caso de uso real que
  lo justifique hoy, no se activa. Si el volumen real de un tenant
  individual demuestra lo contrario más adelante, se reconsidera con ese
  dato en mano, no antes.
- **Extensión `pgvector`**: requeriría migrar de SQL Server a PostgreSQL (o
  correr ambos motores en paralelo) solo para esta feature — cambiar el
  motor de base de datos de todo el sistema por una sola Vertical Slice
  contradice directamente ADR-0002 (un monolito modular con una base de
  datos compartida, no una base de datos por feature).
- **Búsqueda vectorial nativa de SQL Server/Azure SQL**: no disponible en la
  versión de SQL Server que este proyecto ya despliega
  (`mcr.microsoft.com/mssql/server:2022-latest`, fijada desde
  `docker-compose.yml` en Sprint 11 de Release 2) — es una capacidad más
  reciente de Azure SQL Database, no de SQL Server 2022 on-premises/en
  contenedor. Se reconsidera si el proyecto migra de motor o de versión por
  otra razón independiente.
- **Índice en memoria del proceso, sin persistir en base de datos** (p. ej.
  reconstruir el índice al arrancar): rechazado — perdería todos los
  embeddings generados en cada reinicio del contenedor `api`, obligando a
  re-indexar (y re-pagar el costo de generar embeddings) cada vez; los
  Documentos y su contenido ya persisten en SQL Server, sus vectores deben
  persistir con la misma durabilidad.

## Consecuencias

- Positivo: cero infraestructura nueva — mismo argumento que ya usó
  Hangfire reutilizando SQL Server como storage en vez de una pieza
  adicional (ADR-0008).
- Positivo: la búsqueda de similitud filtrada por tenant es tan simple de
  razonar sobre su corrección como cualquier otra Query de Application
  (`WHERE TenantId = @actual` antes de calcular distancias) — no hay una
  capa de indexación externa donde ese filtro pudiera olvidarse.
- Negativo: un escaneo lineal por tenant no escala a un volumen de
  documentos por tenant muy grande (miles de chunks). Aceptado
  explícitamente para este Release — ninguna HU ni caso de uso real de
  Release 3 alcanza ese volumen; es la misma clase de trade-off que
  ADR-0009 ya aceptó para Documentos huérfanos sin FK física: conocido,
  documentado, revisitable si la realidad de uso lo exige.
- Seguimiento: el diseño concreto de la tabla (columnas, índices, tamaño
  del chunk de texto) se define en el Sprint de Modelo de Dominio/Base de
  Datos de este Release — este ADR fija *dónde* viven los vectores y *cómo*
  se buscan a alto nivel, no el esquema exacto.
