# Release 3, Sprint 8 — Frontend

Alcance de un solo Sprint, sin sub-partes (a diferencia de Release 2): F9
(asistente) es la única feature con UI nueva de este Release — F10 (RAG)
no necesita interfaz propia, participa de forma invisible a través del
chat (`search_my_documents` ya es una herramienta más del asistente,
construida en Sprint 7b).

## Qué se agregó

- **`types/index.ts`**: `AssistantMessageRole` (`as const`, no `enum` —
  mismo criterio que el resto del archivo, `erasableSyntaxOnly`) +
  `AssistantMessageItem`, reflejando `AssistantMessageDto` del backend
  (Sprint 4) campo por campo.
- **`api/assistant.ts`**: `getAssistantMessages()`/`sendAssistantMessage(message)`
  — dos funciones, mismo patrón exacto que `api/catalogs.ts`, sobre el
  `apiClient` compartido (JWT inyectado automáticamente, sin código nuevo
  de autenticación).
- **`views/assistant/AssistantView.vue`** — la única vista sin precedente
  directo en el proyecto (ninguna feature anterior es un chat). Reutiliza
  el esqueleto ya establecido (`script setup`, `useI18n`, `useNotificationsStore`,
  `extractErrorMessage`, `onMounted(load)`) pero compone la UI con
  primitivas de Vuetify sin una plantilla previa que copiar:
  - Historial en burbujas (`v-card`), alineadas a la derecha (usuario,
    color `primary`) o izquierda (asistente, `variant="tonal"`).
  - Envío **optimista**: sin streaming (ADR-0013), una respuesta real
    puede tardar varios segundos — el mensaje del usuario aparece de
    inmediato, antes de que la red responda, para no dejar la pantalla en
    blanco. Al confirmar el envío, se vuelve a pedir el historial completo
    (`load()`) para reemplazar el mensaje optimista por el estado real
    persistido — mismo principio que ya usa `CatalogsListView` al crear un
    Catálogo (recargar en vez de mantener estado local divergente).
  - `v-textarea` con Enter para enviar (Shift+Enter para salto de línea),
    deshabilitado mientras se espera respuesta, con un indicador "Pensando…".
- **Ruta** `/assistant` (`router/index.ts`) — sin `meta.permission`: el
  backend (`AssistantEndpoints.cs`) solo exige autenticación, ninguna
  política de permiso (ADR-0013: el límite de seguridad vive en cada
  herramienta, no en el endpoint de chat).
- **Navegación**: entrada nueva en `DefaultLayout.vue` (`mdi-robot-outline`,
  `permission: null` — mismo criterio que el Dashboard).
- **i18n**: claves `assistant.*` en `locales/es.ts`/`en.ts`.

## Verificación

**Probado en un navegador real, no solo `npm run build`:**

- `npm run build` (`vue-tsc -b && vite build`) — el único gate de
  tipo/lint de este proyecto (no hay ESLint separado) — limpio, sin
  errores. El nuevo chunk `AssistantView` se generó correctamente
  (code-splitting por ruta, mismo patrón que el resto de vistas).
- **Flujo completo en Chrome real**: con la Api y el servidor de Vite
  corriendo de verdad, se navegó a `/assistant`, se escribió un mensaje,
  se envió con el botón, y se confirmó — con una captura de pantalla real,
  no solo el árbol de accesibilidad — que: el mensaje del usuario aparece
  alineado a la derecha en color primario, la respuesta del asistente
  aparece alineada a la izquierda, el campo de texto se limpia después de
  enviar, y recargar la página conserva el historial completo en el orden
  correcto (confirma que `GetAssistantMessagesQuery`, construida en Sprint
  4, sigue funcionando end-to-end con un cliente HTTP real, no solo con la
  suite de integración).
- **Sin proveedor de IA real configurado en este entorno** (dicho desde
  Sprint 1): la respuesta real observada fue *"El asistente de IA no está
  configurado en este entorno."* — exactamente el mensaje de
  `NullAiChatClient` (Sprint 3). Confirma que el camino de *fallback*
  gracioso llega intacto hasta la UI, sin ningún error 500 ni pantalla
  rota — la misma verificación honesta que el resto del proyecto ya
  aplicó a Redis/Hangfire/SMTP/proveedores de nube.

## Una limitación real de la herramienta de automatización, no del código

El primer intento de simular la escritura+envío en una pestaña que ya
llevaba varias interacciones previas (`navigate`, clicks, un intento
fallido con `form_input` estableciendo el valor del `<textarea>`
directamente sin pasar por los listeners de Vue) dejó esa pestaña
específica con capturas de pantalla que agotaban el tiempo de espera
("the renderer may be frozen or unresponsive") y el botón de enviar
aparentemente inerte. **No se asumió que era un bug del componente**: se
verificó primero el round-trip real llamando al endpoint directamente
vía `fetch` con el token de sesión real del navegador (200 OK, respuesta
correcta), después se recargó la página y se confirmó que el historial
se renderizaba bien — y finalmente, en una **pestaña nueva**, el flujo
completo de clic+escritura+envío funcionó correctamente al primer
intento, con captura de pantalla real confirmando el resultado. La causa
más probable es el mismo tipo de limitación ya documentada en Release 2
(Sprints 8a/8b) con `v-select` de Vuetify y eventos sintéticos — esta vez
aparentemente agravada por el estado acumulado de una pestaña con varias
interacciones previas fallidas, no una falla del propio componente.

## Qué no se hizo en este sprint (a propósito)

- Streaming de la respuesta — diferido desde Sprint 1.
- Indicador visual de qué Documentos participan en RAG (F10.1/F10.2) — el
  backlog no pide una vista de administración de la indexación, solo que
  el asistente pueda usarla como herramienta.
- Historial de conversaciones múltiples con nombre — ninguna HU lo pide
  (HU-091 habla de "mi conversación actual", singular — ver
  `r3-05-modelo-dominio.md`).
- Pruebas automatizadas de frontend — el proyecto no tiene Vitest/Cypress/
  Playwright configurado todavía (confirmado al explorar la estructura
  existente); introducir tooling de testing de cero es una decisión más
  grande que agregar una página, fuera del alcance de "igualar
  convenciones existentes" de este Sprint.
