# Release 3 — Inteligencia Artificial: Visión y Alcance

Sprint 1 (Análisis) de Release 3, mismo ciclo `análisis → ... → DevOps` que
Release 1 y 2 aplicaron (ADR-0001). Este documento juega el mismo rol que
[`r2-01-vision-y-alcance.md`](./r2-01-vision-y-alcance.md) jugó para
Release 2.

## 0. Redirección de alcance, decidida por el usuario al iniciar este Sprint

El plan original de Release 3 (`backlog/epics.md`, E9/E10/E11 antes de este
sprint) era **tooling de desarrollo**: generar historias de usuario/tests/
SQL/DTOs/entidades para construir el propio EnterpriseFlow más rápido, un
RAG sobre la documentación de este mismo repo, y un servidor MCP exponiendo
la base de datos/logs/git/Swagger de este proyecto. Es una interpretación
válida de "MÓDULO IA"/"MCP"/"RAG" en `especificcion.md`, pero es meta —
herramientas para quien *construye* EnterpriseFlow, no una feature para
quien lo *usa*.

Antes de escribir código se le preguntó al usuario cuál de las dos lecturas
priorizar, dado que son suficientemente distintas como para requerir
decisiones de arquitectura incompatibles entre sí. Decisión: **un asistente
de IA integrado en el propio EnterpriseFlow, de cara al usuario final del
SaaS** — coherente con el resto del portafolio (Releases 1-2 son features
de producto real, no herramientas internas del equipo). El servidor MCP
propio y la generación de artefactos de desarrollo quedan diferidos sin
Release asignado (`backlog/epics.md`, E9/E11) — no descartados, solo fuera
de este Release.

**Sin claves de API reales de OpenAI/Anthropic disponibles en este
entorno** (confirmado con el usuario) — mismo tipo de restricción ya
enfrentada con Redis (Release 2, Sprint 3) y los SDKs cloud de Documentos
(Release 2, Sprint 7b): el código se construye contra una abstracción
propia (`IAiChatClient`, ver más abajo), probado con fakes/mocks reales, y
cualquier llamada real a OpenAI/Anthropic queda explícitamente documentada
como no verificada en ejecución — nunca reportada como probada cuando no
lo fue.

## 1. Qué agrega Release 3 sobre Release 2

Release 2 dejó a EnterpriseFlow con datos de negocio reales (Proyectos,
Tareas, Documentos, Catálogos) pero ninguna forma de que un usuario
pregunte algo sobre esos datos en lenguaje natural — para eso hay que abrir
el Dashboard, navegar a la pantalla correcta, y leer. Release 3 agrega esa
capa conversacional: un asistente que responde preguntas ancladas en los
datos reales del tenant (grounding, no alucinación), sin exponer ni una
fila de otro tenant.

## 2. Alcance de Release 3

Mapea a los Epics E9 (redefinido)/E10 (redefinido) de
[`backlog/epics.md`](./backlog/epics.md), detallados con Historias de
Usuario en
[`backlog/historias-usuario-release3.md`](./backlog/historias-usuario-release3.md):

- **Integración con proveedores de IA (F9.1)**: abstracción propia
  (`IAiChatClient` en Application, mismo patrón exacto que
  `IDocumentStorageProvider`/ADR-0009 — una interfaz, N implementaciones,
  una activa por configuración) sobre OpenAI y Anthropic (Claude).
- **Chat conversacional (F9.2)**: panel de chat en el frontend, historial
  de conversación persistido por usuario dentro de su tenant.
- **Respuestas ancladas en datos reales, no alucinadas (F9.3)**: el modelo
  no tiene acceso directo a SQL — responde vía *tool-use* contra las
  Queries de Application que ya existen (`GetProjects`, `GetTasks`,
  `GetDocuments`, etc.), el mismo aislamiento de permisos y de tenant que
  ya aplica a cualquier otro caller de esas Queries (`AuthorizationBehavior`,
  filtro global por tenant, ADR-0003/ADR-0004) — el asistente no es una
  puerta trasera que evita esas capas.
- **Resúmenes y reportes ejecutivos generados por IA (F9.4)**: a partir de
  datos reales resueltos por las mismas Queries, no de texto libre.
- **RAG sobre Documentos del tenant (E10 redefinido)**: los archivos que un
  tenant ya sube vía F5 (Release 2) se indexan (extracción de texto +
  embeddings) para que el asistente pueda responder preguntas ancladas en
  su contenido — "¿qué dice el contrato que subí la semana pasada?" — con
  el mismo aislamiento por tenant que el resto del sistema.

## 3. Decisiones de alcance dentro de Release 3 (para no sobre-construir)

Mismo criterio que Releases 1 y 2: cada feature se acota a lo que un caso
de uso concreto pide.

- **Dos proveedores de IA, no una lista abierta**: OpenAI y Anthropic — los
  dos que `especificcion.md` nombra explícitamente. La abstracción
  (`IAiChatClient`) hace que agregar un tercero después sea una
  implementación nueva, no una reescritura — mismo argumento que
  ADR-0009 ya usó para Documentos.
- **RAG apunta a Documentos del tenant, no a la documentación del propio
  proyecto** — ver sección 0. Indexar README/Swagger de EnterpriseFlow
  tendría audiencia (el propio equipo de desarrollo), pero no es la
  audiencia que este Release prioriza.
- **Tool-use se limita a consultas de lectura ya existentes** (Proyectos,
  Tareas, Clientes, Documentos, Catálogos) — el asistente no obtiene un
  comando nuevo de "crear/editar/eliminar vía lenguaje natural" en este
  Release: eso multiplicaría la superficie de permisos a auditar (¿qué
  puede mutar el modelo, con qué confirmación?) sin que ninguna Historia de
  Usuario lo pida todavía. Si se justifica después, la abstracción de
  tool-use ya generaliza a comandos, no solo Queries.
- **Sin streaming de respuesta en este Release**: request/response
  síncrono. Streaming (Server-Sent Events o WebSocket incremental) mejora
  la UX percibida pero es una capa de complejidad adicional de transporte
  que no cambia qué puede hacer el asistente — candidato a agregar después
  si la latencia real (una vez con claves de API reales) lo justifica.
- **Sin historial de conversación compartido entre usuarios**: cada
  conversación pertenece a un usuario dentro de su tenant — ninguna HU
  pide un chat de equipo compartido.
- **La elección de almacén de vectores para RAG se decide en el Sprint de
  Arquitectura de este Release, no aquí** — depende de una comparación real
  de alternativas (¿una tabla propia con similitud coseno calculada en
  memoria, suficiente para el volumen de un tenant individual, vs. un
  servicio de vectores dedicado?) que corresponde a esa fase del ciclo, no
  a Análisis.

## 4. Explícitamente fuera de Release 3

- Generación de historias de usuario/SQL/DTOs/Entidades/tests, detección de
  code smells — diferido sin Release asignado (`backlog/epics.md`, E9).
- Servidor MCP propio — diferido sin Release asignado (`backlog/epics.md`,
  E11).
- Mutaciones vía lenguaje natural (crear/editar/eliminar por el asistente)
  — ver sección 3.
- Fine-tuning de modelos propios — ninguna HU lo pide; los proveedores
  elegidos (OpenAI/Anthropic) ya cubren el caso de uso con sus modelos
  base.
- OCR de documentos escaneados/imágenes — F10.2 indexa texto extraíble
  (PDF/Word/texto plano, el mismo subconjunto que F5.7 ya valida al subir);
  un PDF que es solo una imagen escaneada sin capa de texto queda fuera,
  mismo criterio que Release 2 aplicó a "sin escaneo antivirus real de
  contenido" — requeriría un servicio adicional (OCR) sin otro caso de uso
  que lo justifique todavía.

## 5. Criterios de éxito de Release 3

1. Un usuario puede preguntarle al asistente algo sobre sus propios datos
   ("¿cuántas tareas tengo atrasadas?", "resume el documento X que subí") y
   recibir una respuesta anclada en datos reales de su tenant — nunca en
   datos de otro tenant, nunca inventada sin base en una Query o un
   Documento real.
2. El proveedor de IA activo se cambia editando configuración, sin
   recompilar ni tocar código de Application/Domain — mismo criterio de
   éxito que Release 2 ya aplicó a Documentos, verificado de la misma
   forma (una suite de casos corrida contra cada proveedor disponible).
3. Todo lo que no se pudo verificar en ejecución real por falta de claves
   de API queda dicho explícitamente en la documentación del Sprint
   correspondiente — nunca reportado como probado sin haberlo estado.
4. Mismo estándar de Releases 1-2: cobertura de pruebas medida y
   reportada, documentación actualizada al cierre del Release,
   `docker-compose up` sin pasos manuales adicionales (más allá de proveer
   las claves de API reales en `.env`, que por su naturaleza no pueden
   tener un default funcional).
