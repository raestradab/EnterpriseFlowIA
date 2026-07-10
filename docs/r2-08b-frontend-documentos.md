# Release 2, Sprint 8b — Frontend: Documentos

Segunda sub-parte de Sprint 8 (Frontend). Sobre lo que 8a ya construyó
(Workflow) — un Documento no puede subirse sin elegir un Workflow real.

## Un gap real de backend, encontrado al construir el primer consumidor real

`GetDocumentByIdQuery`/`GetDocumentsQuery` (Sprint 7b) exponían
`CurrentWorkflowStateId` — un GUID crudo, sin nombre. Al construir la
pantalla real (Sprint 8b) quedó claro que una UI no puede mostrar un estado
legible, ni saber a qué estados puede transicionar un Documento, con solo
ese id: `Document` no guarda `WorkflowDefinitionId` (por diseño, ADR-0010 —
solo referencia su estado actual). Corregido extendiendo ambos DTOs
(`DocumentDto`, `DocumentListItemDto`) con `CurrentWorkflowStateName` y
`WorkflowDefinitionId`, resueltos en el propio backend con un `join` contra
`WorkflowDefinitions`/`States` — el mismo patrón de resolución que
`TransitionDocumentCommandHandler` ya usaba internamente, solo que ahora
también expuesto en las Queries de lectura. Verificado con las 9 pruebas de
integración de `DocumentsEndpointsTests` (sin cambios necesarios en ellas —
adición aditiva) y confirmado en vivo: el estado "Borrador"/"Aprobado" se
ve como texto legible en la UI, no como un GUID.

Mismo tipo de hallazgo que el gap de `CachingBehavior`/tenant-key en Sprint 4
o el `ORDER BY DateTimeOffset` de Sprint 7c — un DTO diseñado en el sprint de
Backend, sin un consumidor real todavía, que resulta incompleto en cuanto
alguien intenta construir la UI real sobre él.

## Qué se implementó

- `src/types/index.ts`: `DocumentOwnerType`, `DocumentListItem`,
  `DocumentItem` — nombrado `*Item`, no `Document`, porque ese identificador
  ya es la interfaz global del DOM en TypeScript.
- `src/api/documents.ts`: `getDocuments`, `getDocument`, `uploadDocument`
  (multipart real vía `FormData`), `downloadDocumentContent` (responseType
  `blob`, la conversión a descarga del navegador vive en la vista, no aquí —
  este módulo se mantiene como solo llamadas HTTP), `transitionDocument`,
  `deleteDocument`.
- `ProjectDetailView.vue`: nueva tarjeta "Documentos" — mismo patrón visual
  que las tarjetas de Equipo/Tareas ya existentes. Subida (`v-file-input` +
  selector de Workflow), descarga (blob → `<a download>` sintético),
  transición (diálogo que resuelve las transiciones válidas desde el estado
  actual consultando el Workflow completo — `GET /api/workflows/{id}` de
  8a), eliminación.
- Sin pantalla de detalle de Documento propia: dado el alcance (un único
  consumidor real, Documentos de Proyecto), la tarjeta embebida en
  `ProjectDetailView` cubre HU-050/HU-081 sin la complejidad de una ruta
  adicional que ningún caso de uso pide todavía.

## Verificación

**En un navegador real contra la Api real, no simulado.** Se creó un
Cliente y un Proyecto reales, y contra ese Proyecto:

- **Subida real de un archivo**: un `File` de verdad, vía el input nativo
  de la tarjeta (`onFileSelected` recibiendo el `File` real del picker),
  subido con `multipart/form-data` real — la Api respondió 201 y la fila
  quedó en LocalDB.
- **Descarga real**: los bytes devueltos por `GET /api/documents/{id}/content`
  son **idénticos** a los subidos — confirmado comparando el contenido
  exacto, no solo el código de estado.
- **Transición real**: el diálogo calculó correctamente que, desde
  "Borrador", la única transición válida es a "Aprobado" (filtrando
  `workflow.transitions` por `fromStateId` en el cliente) — tras confirmar,
  el estado se actualizó a "Aprobado" tanto en la base de datos como en la
  pantalla.
- **Eliminación real**: el documento desaparece de la lista tras confirmar.
- Sin errores de consola durante toda la sesión.

### Misma limitación de la herramienta de automatización que 8a

El selector de Workflow en el diálogo de subida, y el de estado destino en
el diálogo de transición, son `v-select` de Vuetify — el mismo componente
que en 8a no respondió a eventos sintéticos de clic/`change` disparados por
script. La **subida de archivo en sí** se verificó con una interacción real
(un `File` real asignado al `<input type="file">` nativo vía
`DataTransfer`, con Vue detectándolo correctamente — confirmado viendo el
nombre del archivo aparecer en el diálogo). Las acciones que dependían del
`v-select` (elegir el Workflow al subir, elegir el estado destino al
transicionar) se verificaron llamando al endpoint real con el mismo token
de sesión del navegador, y confirmando el resultado en la UI tras recargar
— mismo criterio ya usado y documentado en 8a.

## Qué sigue

8c (Notificaciones): cliente de SignalR, campana con contador de no
leídas, centro de notificaciones con marcar-como-leída — cierra Sprint 8.
