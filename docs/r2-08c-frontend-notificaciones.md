# Release 2, Sprint 8c — Frontend: Notificaciones

Tercera y última sub-parte de Sprint 8 (Frontend) — cierra Sprint 8
completo.

## Qué se implementó

- `@microsoft/signalr` (paquete nuevo — instalado con `--legacy-peer-deps`
  por un conflicto de peer dependencies preexistente entre `vue-router@5.1.0`
  y `vite@6.x`, sin relación con este cambio; no se tocó ninguna versión ya
  fijada en `package.json`).
- `vite.config.ts`: proxy nuevo para `/hubs` con `ws: true` — el proxy de
  `/api` ya existente no hace upgrade a WebSocket automáticamente.
- `src/realtime/notificationHub.ts`: conexión a `/hubs/notifications`
  (mismo hub de Sprint 7c) vía `accessTokenFactory`, con reconexión
  automática. Escucha el evento `document.transitioned` — el único que
  `SignalRNotifier` envía hoy; un futuro evento nuevo necesitaría su propio
  `.on(...)` aquí, SignalR no tiene listener "wildcard".
- `src/stores/notificationCenter.ts`: store Pinia nuevo, **nombrado
  deliberadamente distinto** del `stores/notifications.ts` ya existente
  (que es la cola de toasts de Release 1, no el centro de notificaciones de
  F6) — la colisión de nombres ya se había anotado como pendiente en
  `r2-08a-frontend-catalogos-workflow.md`.
- `src/api/notifications.ts`: `getMyNotifications`, `markNotificationRead`.
- `DefaultLayout.vue`: campana en la app-bar con `v-badge` mostrando el
  conteo de no leídas, menú desplegable con la lista, clic en un ítem no
  leído lo marca como leído. Conexión SignalR y carga inicial del centro de
  notificaciones arrancan en el mismo `onMounted` donde ya vivía
  `auth.loadMe()`.

## Verificación

**En un navegador real, con el push en vivo observado sin recargar la
página** — la prueba más exigente de todo Sprint 8:

1. Se agregó al propio usuario admin como miembro del Proyecto de prueba
   (usando el campo de texto simple de "Agregar miembro" — sin el problema
   de automatización de `v-select` que afectó a otras pruebas).
2. Con la campana en `0`, se subió y transicionó un Documento por API real.
3. **Sin recargar la página**, el badge de la campana pasó de sin badge a
   `1` — prueba directa de que el push de SignalR llegó al cliente y
   `notificationCenter.load()` se disparó desde el handler `.on(...)`.
4. Abrir la campana mostró el mensaje exacto generado por el backend
   (`NotifyOnDocumentWorkflowTransitionedHandler`, Sprint 7c): *"El
   documento cambió al estado 'Aprobado'."*
5. Clic en la notificación la marcó como leída — badge desapareció de
   inmediato, y `GET /api/notifications` confirmó `isRead: true` persistido
   en la base de datos real.
6. Sin errores de consola durante toda la sesión (incluida la negociación
   SignalR, confirmada como `200` en el log de red).

## Cierre de Sprint 8 (Frontend) de Release 2

Con 8a (Catálogos/Workflow), 8b (Documentos) y 8c (Notificaciones)
completos, Sprint 8 queda cerrado — frontend real, verificado en navegador
contra la Api real, para las cuatro features que Sprint 7 (Backend)
entregó. Dos gaps reales de Backend se encontraron y corrigieron al
construir sus primeros consumidores reales (Sprint 8b: DTOs de Document sin
nombre de estado). Reportes, Configuración y Mapa (`r2-01-vision-y-alcance.md`)
siguen sin backend ni frontend — quedan para un sprint futuro que empiece
por su propio ciclo completo.

Sigue Sprint 9 (Pruebas) de Release 2 — la Api ya tiene 207 pruebas de
integración/unitarias acumuladas a lo largo de los Sprints 4-7; Sprint 9
es el punto donde, siguiendo el mismo criterio que Release 1, se revisa la
cobertura como conjunto (no sprint por sprint) y se decide qué falta.
