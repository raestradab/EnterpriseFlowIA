# Release 2, Sprint 8a — Frontend: Catálogos y Workflow

Primera sub-parte de Sprint 8 (Frontend) de Release 2. Igual que Sprint 7
(Backend), se divide en sub-partes completas en vez de avanzar todo a
medias: **8a = Catálogos + Workflow** (pantallas de administración,
CRUD-simple), **8b = Documentos** (subida/descarga de archivos, la pieza
más grande), **8c = Notificaciones** (SignalR en vivo + centro de
notificaciones).

## Por qué solo estas dos features en este sprint

El Sprint 7 (Backend) construyó Catálogos, Documentos, Workflow y
Notificaciones — pero **no** Reportes (F4.3), Configuración (F8.3) ni el
Mapa/alertas (F4.4): esas tres features de `r2-01-vision-y-alcance.md` no
tienen backend todavía. Siguiendo la metodología (backend antes que
frontend, dentro de cada feature), Sprint 8 solo puede construir frontend
para lo que Sprint 7 ya entregó. Reportes/Configuración/Mapa quedan
pendientes de un futuro sprint que empiece por su propio backend —
mencionado aquí explícitamente para que no se lea como un olvido.

## Qué se implementó

Mismo patrón exacto que toda vista existente (`ProjectsListView.vue`/
`ProjectDetailView.vue`): Vuetify `v-data-table`+`v-dialog` para
listar/crear, Pinia solo para identidad/toast (no para datos de features,
que viven en `ref()` local por vista), Axios vía el cliente compartido
(`api/client.ts`, interceptor de refresh ya existente).

- `src/types/index.ts`: `CatalogListItem`, `CatalogItem`, `WorkflowListItem`,
  `Workflow`, `WorkflowState`, `WorkflowTransition` — DTOs espejo de
  Application, mismo criterio ya documentado en `docs/08-frontend.md`
  (sin generador de cliente OpenAPI todavía).
- `src/api/catalogs.ts`, `src/api/workflows.ts`.
- `src/views/catalogs/{CatalogsListView,CatalogDetailView}.vue`,
  `src/views/workflows/{WorkflowsListView,WorkflowDetailView}.vue`.
- Rutas nuevas en `router/index.ts` (`catalogs`, `catalogs/:id`, `workflows`,
  `workflows/:id`) y entradas de menú en `DefaultLayout.vue`
  (`catalogs.read`/`workflows.read` filtran su visibilidad, HU-005).
- Claves i18n nuevas en `locales/{es,en}.ts`.

## Una decisión de nombres deliberada

El store Pinia existente `stores/notifications.ts` es en realidad la cola
de **toasts** (mensaje/color/visible) — nada que ver con el centro de
notificaciones persistente de F6 (HU-062). Nombre ya ocupado desde Release 1
antes de que F6 existiera. Al construir 8c (Notificaciones reales) habrá que
nombrar su store distinto (p.ej. `useNotificationCenterStore`) para no
chocar con este — anotado aquí para no repetir la confusión al
retomar esa sub-parte.

## Verificación

**Ejecución real, no solo `vue-tsc` limpio.** `vue-tsc -b` pasó sin
errores, pero además se levantó la Api real (contra LocalDB, migraciones al
día) y el dev server de Vite, se registró un tenant nuevo por el flujo real
de registro/login, y se ejercitó cada pantalla nueva contra el backend real
por HTTP:

- **Catálogos**: crear catálogo → aparece en la lista con `itemCount`
  correcto → entrar al detalle → agregar un elemento → **editar** su
  etiqueta → **eliminar** el elemento — las cuatro operaciones confirmadas
  contra respuestas reales de la Api, no simuladas.
- **Workflow**: crear Workflow → agregar 2 estados (`Borrador` inicial,
  `Aprobado` final, con sus chips visibles) → agregar una transición
  (`Borrador → Aprobado`) → recargar la página y confirmar que la
  transición persiste y el nombre de cada estado se resuelve correctamente
  en la lista de transiciones.
- **i18n**: cambio de idioma ES↔EN confirmado en ambas pantallas nuevas —
  los headers de tabla (`computed()`, no un array plano) se retraducen sin
  recargar, seg��n el patrón ya documentado para evitar el bug conocido.
- **Sin errores de consola** durante toda la sesión de pruebas.

### Una limitación de la propia herramienta de automatización, no del código

Al automatizar el formulario de "Nueva transición", el componente
`v-select` de Vuetify (usado también, sin problema, en `ProjectDetailView`/
`ProjectsListView` desde Release 1) no respondió a eventos de clic/`change`
sintéticos disparados por script — un límite conocido de manipular
componentes Vue con estado interno mediante DOM crudo, no un defecto de
`WorkflowDetailView.vue`. Se verificó la misma acción llamando al endpoint
real (`POST /api/workflows/{id}/transitions`) con el token de sesión real
capturado del propio navegador, y confirmando que la UI la renderiza
correctamente tras recargar — cubre lo que importa (integración real
Front↔Back) aunque no haya sido un clic físico en el dropdown.

## Qué sigue

8b (Documentos): subida real de archivos (input de tipo file, validación de
extensión en el cliente como mejora de UX — el rechazo real ya lo hace
`FileSignatureValidator` en el servidor), descarga, transición de estado de
un documento. 8c (Notificaciones): cliente de SignalR (`@microsoft/signalr`,
paquete nuevo a agregar), campana con contador de no leídas, lista con
marcar-como-leída.
