# Release 3, Sprint 11 — DevOps

Cierra Release 3. Mismo alcance que Sprint 11 de Release 2: no construir el
stack de DevOps desde cero — ya existe — sino extenderlo para lo que
Release 3 activó (proveedores de IA, RAG) que el stack de Release 2 nunca
necesitó.

## `docker-compose.yml`: sin servicios nuevos, un comentario nuevo

A diferencia de Release 2 (que sumó el servicio `redis` y el volumen
`documents-data`), Release 3 **no agrega ningún servicio ni volumen** —
`AssistantMessages`/`DocumentChunks` son tablas más en el mismo SQL Server
que ya existe (ADR-0014: sin almacén de vectores dedicado), y no hay
ningún contenedor local que pueda sustituir a OpenAI/Anthropic (a
diferencia de Redis, que sí corre como contenedor local). El único cambio
es un comentario en el bloque `environment` del servicio `api`, explicando
por qué `Ai:ChatProvider`/`Ai:EmbeddingProvider` se dejan sin configurar
— mismo criterio ya aplicado a `Documents:Provider` (Azure/S3/Gcs) y SMTP
en Release 2: sin una clave de API real, el stack local/demo no la asume,
y `IAiChatClient`/`IEmbeddingClient` resuelven a sus *fallbacks* Null en
vez de fallar. `.env.example` gana un bloque comentado (`OPENAI_API_KEY`/
`ANTHROPIC_API_KEY`) documentando cómo activar un proveedor real si
alguien corre este stack con sus propias credenciales.

## `nginx.conf`: sin cambios

El chat del asistente es HTTP normal (`POST`/`GET` a `/api/assistant/*`)
— el bloque `location /api/` que ya existe desde Release 1 lo cubre sin
necesitar nada específico, a diferencia de SignalR (Release 2, Sprint 11),
que sí necesitó su propio bloque para el *upgrade* a WebSocket.

## `Dockerfile` (Api): sin cambios

Los paquetes nuevos de este Release (`OpenAI`, `PdfPig`,
`DocumentFormat.OpenXml`) se resuelven automáticamente por el `dotnet
restore` que ya existía — `Directory.Packages.props` ya se copia antes de
ese paso, y el Dockerfile copia las 4 carpetas de `src/` completas antes
de compilar. Ningún archivo `.csproj` nuevo bajo `src/` (el único proyecto
de test nuevo, `EnterpriseFlow.Infrastructure.UnitTests`, vive bajo
`tests/`, fuera de lo que la imagen de la Api construye).

## Verificación

- **Migración automática contra una base completamente nueva, en
  ejecución real** (no solo revisado): se corrió la Api apuntando a una
  base LocalDB que no existía todavía
  (`ConnectionStrings__Default` con un nombre de base nuevo) — el mismo
  mecanismo que `docker compose up` ejercitaría contra un contenedor de
  SQL Server recién creado. Log confirmado: `CREATE DATABASE` →
  `CREATE TABLE [__EFMigrationsHistory]` → las 5 migraciones aplicadas en
  orden (`InitialCreate` → `AddCatalogs` →
  `AddDocumentsWorkflowsNotifications` → `AddAssistantMessages` →
  `AddDocumentChunks`) → `Application started` → `/health` respondiendo
  `Healthy`. Confirma que `Program.cs`'s `Database.Migrate()`
  (implementado en Sprint 11 de Release 1, sin cambios desde entonces)
  sigue aplicando toda la cadena de migraciones de los tres Releases
  automáticamente al arrancar, sin intervención manual.
- **`docker-compose.yml` no se pudo levantar de punta a punta en este
  entorno** (sin daemon de Docker disponible aquí) — mismo límite ya
  declarado en Sprint 11 de Release 1 y Release 2. Se validó como YAML
  sintácticamente correcto (`yaml.safe_load`, mismo criterio que las
  auditorías anteriores).
- `.github/workflows/ci.yml` no necesitó cambios: el job `backend` ya
  corre `dotnet test EnterpriseFlow.slnx` sin enumerar proyectos por
  nombre, así que cubre `EnterpriseFlow.Infrastructure.UnitTests` (el
  proyecto de test nuevo de Sprint 7a) automáticamente, sin haberlo
  anticipado a propósito.
- `dotnet build`/`dotnet test EnterpriseFlow.slnx` — **281/281** — y
  `dotnet format --verify-no-changes` limpios.

## Cierre de Release 3

Con Sprint 11 completo, **Release 3 (Inteligencia Artificial) queda
cerrada** — los 11 pasos del ciclo completo (análisis → diseño →
arquitectura → validación → modelo de dominio → base de datos → backend →
frontend → pruebas → documentación → DevOps) aplicados al asistente de IA
conversacional (F9) y RAG sobre Documentos del tenant (F10), con la misma
disciplina de verificación real que Release 1 y Release 2 establecieron.

Sin claves de API reales de OpenAI/Anthropic disponibles en ningún momento
de este Release — cada pieza que las necesita (`OpenAiChatClient`,
`AnthropicChatClient`, `OpenAiEmbeddingClient`) se construyó igual, con su
lógica de traducción de protocolo probada de forma aislada y real, y con
el límite de verificación declarado explícitamente en el momento en que
se construyó cada una, no descubierto después. El camino de degradación
elegante (`NullAiChatClient`/`NullEmbeddingClient`) — el que efectivamente
corre en este entorno — se verificó de punta a punta, por HTTP real, en
Sprint 9.

Doce Historias de Usuario (HU-090 a HU-101), tres herramientas reales del
asistente (`get_my_projects`, `search_my_documents`,
`get_my_overdue_tasks`), un chat funcional con historial persistido, y un
pipeline completo de indexación de Documentos para RAG — cada gap
encontrado durante la construcción (el bug de `ORDER BY DateTimeOffset`
repetido dos veces, el olvido de agregar al usuario como miembro del
Proyecto antes de asignarle una tarea, la diferencia de protocolo entre
OpenAI y Anthropic para el tool-use) corregido y documentado en el momento
en que se encontró.
