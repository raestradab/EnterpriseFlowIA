# Sprint 10 — Documentación

Alcance según `especificcion.md`/`docs/02-roadmap.md`: README, diagramas
Mermaid, modelo ER, Swagger, ADRs consolidados. A diferencia de sprints
anteriores, este no fue "escribir documentación nueva desde cero" — el
proyecto viene documentando cada sprint desde el Sprint 1 (`docs/01-*` a
`docs/09-*`, más `07a`/`07b`/`08a` para sub-partes fuera de la secuencia
numerada). El trabajo real de Sprint 10 fue auditar esa documentación
acumulada contra el estado *actual* del código y corregir lo que había
quedado desactualizado — documentación que miente es peor que no tenerla.

## Auditoría: qué estaba desactualizado y por qué

**El diagrama ER (`docs/06-base-de-datos.md`) solo tenía 6 tablas.** Sprint 6
lo escribió antes de que Identidad (Sprint 7a) existiera — correcto en su
momento, pero nunca se volvió a tocar después de que `Tenants`/`Users`/
`Roles`/`RolePermissions`/`UserRoleAssignments`/`RefreshTokens` se agregaran.
Alguien leyendo solo ese diagrama concluiría, incorrectamente, que el
esquema de Identidad no existe. Corregido: las 12 tablas actuales, con una
nota explícita sobre por qué `ProjectMembers.UserId`/`ProjectTasks.AssignedToUserId`/
`UserRoleAssignments.RoleId` siguen sin FK física (ADR-0005, cross-aggregate
— una razón distinta a la original de Sprint 6, que era simplemente "la
tabla `Users` todavía no existe"). También se completó la tabla de índices
con los 6 que Identidad agregó y que nunca se habían listado ahí.

**`docs/05-modelo-dominio.md` y `docs/06-base-de-datos.md` tenían secciones
"Qué falta para Sprint 6/7" desde Sprint 5/6** — Sprint 7 lleva completo desde
hace varios sprints. Actualizadas a "qué se resolvió después", con enlaces a
dónde quedó resuelto, en vez de dejar una lista de pendientes que ya no lo son.

**No existía un índice consolidado de ADRs.** Las 7 ADRs (0001-0007) estaban
completas individualmente, pero para saber qué decisión resolvía qué pregunta
había que abrir cada archivo. `docs/adr/README.md` nuevo: una tabla de una
línea por ADR (qué decide, con qué se relaciona) — el índice que "ADRs
consolidados" pedía explícitamente en el roadmap.

**`README.md` solo enlazaba los primeros 5 documentos.** `07a`, `07b`, `08`,
`08a` y `09` existían pero no estaban linkeados desde la sección
"Documentación" — alguien llegando por primera vez al repo no los habría
encontrado sin explorar la carpeta `docs/` a mano. Completado, y la sección
"Estado actual" actualizada con el resumen de la revisión de seguridad y de
Sprint 9 (que ya estaban documentados en detalle en sus propios archivos,
pero no resumidos en el punto de entrada del repo).

**Swagger no tenía resúmenes en los endpoints de Auth**, los más complejos y
con más semántica no obvia desde la firma (p. ej. que `/refresh` ya no recibe
body y no puede probarse desde la propia UI de Swagger, o que `/login`
devuelve el refresh token como cookie, no en el body). Se agregaron
`.WithSummary()`/`.WithDescription()` a los 7 endpoints de `AuthEndpoints.cs`
— los de Companies/Clients/Contacts/Projects/Tasks no los tienen todavía: sus
nombres de ruta y forma de request/response ya son autoexplicativos, y
agregar resúmenes a los ~21 endpoints restantes por completitud pura, sin que
ninguno tenga la misma complejidad de contrato que Auth, habría sido
documentación por documentación — el mismo criterio que `especificcion.md`
(sección REGLAS) pide aplicar contra trabajo sin propósito real.

## Renumeración: `09-seguridad.md` → `08a-seguridad.md`

Detalle de housekeeping documentado aquí porque afecta la navegación de
`docs/`: la revisión de seguridad ad-hoc del 2026-07-07 se documentó
originalmente como `docs/09-seguridad.md`, antes de que el propio Sprint 9
(Pruebas, un Sprint real del roadmap) reclamara ese número. Se renombró a
`08a-seguridad.md` — mismo patrón que `07a`/`07b` para sub-partes fuera de la
secuencia numerada — y se actualizaron las 6 referencias cruzadas
(`ADR-0007`, `07a-identidad.md`, `08-frontend.md`, `02-roadmap.md`,
`04-secuencias.md`, `README.md`).

## Qué NO se hizo, deliberadamente

- **No se generó un cliente OpenAPI/TypeScript** desde el `swagger.json` —
  decisión ya tomada y justificada en `docs/08-frontend.md` (la superficie de
  la API todavía cambia sprint a sprint; se reconsidera en Release 2).
- **No se agregó documentación XML (`<summary>` de C#) a cada DTO/comando**
  para que Swagger la muestre — Minimal API no la recoge automáticamente sin
  filtros adicionales de Swashbuckle, y el valor marginal sobre los DTOs ya
  autoexplicativos (nombres de propiedad claros, tipos explícitos) no
  justificaba el esfuerzo de correr un procesamiento adicional en cada build.
- **No se creó un sitio de documentación estático** (DocFX, MkDocs, etc.) —
  el repo es la unidad de portafolio; Markdown navegable en GitHub/el propio
  editor cumple el mismo propósito sin una herramienta adicional que
  mantener.

## Verificación

`dotnet build EnterpriseFlow.slnx` y `dotnet test EnterpriseFlow.slnx` limpios
tras los cambios de `AuthEndpoints.cs` (118/118 tests, sin relación funcional
con la documentación pero confirma que agregar los resúmenes de Swagger no
rompió nada). El resto de los cambios de este sprint son estrictamente
documentación (`.md`), sin superficie de código para probar.
