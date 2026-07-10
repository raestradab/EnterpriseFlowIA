# Release 2, Sprint 10 — Documentación

Mismo alcance que Sprint 10 de Release 1: no escribir documentación nueva
desde cero — el proyecto viene documentando cada sprint desde el Sprint 1
de Release 2 (`docs/r2-01-*` a `docs/r2-09-*`, más `07a/7b/7c` y `08a/8b/8c`
para las sub-partes fuera de la secuencia numerada). El trabajo real de
este sprint fue **auditar** esa documentación acumulada contra el estado
actual del código y corregir lo que había quedado desactualizado.

## Auditoría: qué estaba desactualizado y por qué

**`docs/06-base-de-datos.md` — el índice de `Companies` faltaba en la
tabla de índices.** El índice `(TenantId, Name)` existe en código desde
Sprint 4 de *Release 1* (`CompanyConfiguration.cs`) — nunca se agregó una
fila para él en la tabla de índices, ni siquiera cuando esa tabla se
completó en Sprint 10 de Release 1 para Identidad. Un gap que llevaba
arrastrándose desde antes de que Release 2 empezara, encontrado recién
ahora por la misma disciplina de auditoría. El diagrama ER en sí (ambos,
el de Release 1 y el de Release 2 agregado en Sprint 6) ya estaba
completo — las 19 entidades actuales de Domain están todas representadas;
solo la tabla de índices tenía el hueco.

**`docs/02-roadmap.md` — la sección de Release 2 solo mencionaba el
Sprint 1.** Cada sprint posterior (2 a 9) se documentó en su propio archivo
en tiempo real, pero nadie había vuelto a tocar el resumen del roadmap
después de Sprint 1 — alguien leyendo solo ese archivo habría concluido,
incorrectamente, que Release 2 se detuvo ahí. Completado con los enlaces a
los 9 sprints.

**Swagger sin resumen en el endpoint de subida de Documentos.** Mismo
criterio que Release 1 aplicó a `AuthEndpoints.cs`: agregar
`.WithSummary()`/`.WithDescription()` solo donde el contrato no es
evidente desde la ruta y el DTO. De los ~10 endpoints nuevos de Release 2,
uno solo califica: `POST /api/documents` (`DocumentsEndpoints.UploadAsync`)
parsea el `multipart/form-data` a mano, sin un DTO tipado — Swagger no
puede mostrar qué campos de formulario espera (`file`, `ownerType`,
`ownerId`, `workflowDefinitionId`) sin esa anotación explícita. El resto
(Catálogos, Workflow, Notificaciones) son CRUD estándar con DTOs tipados
ya autoexplicativos — agregarles resúmenes habría sido documentación por
documentación, lo mismo que Release 1 decidió no hacer para Companies/
Clients/Contacts/Projects/Tasks.

## Qué NO estaba desactualizado (verificado, no asumido)

- **`docs/adr/README.md`**: las 12 ADRs (0001-0012) están todas listadas,
  con resúmenes que siguen coincidiendo con el contenido real de cada
  archivo.
- **`README.md`**: los 12 archivos `docs/r2-*.md` que existen están los 12
  enlazados en la sección "Documentación" — sin huecos.
- **Las notas "todavía no existe"/"pendiente" en `r2-03`, `r2-04`, `r2-08a`,
  `r2-08c`**: todas siguen siendo ciertas hoy (Redis real sin verificar en
  ejecución, Reportes/Configuración/Mapa sin backend ni frontend) o están
  correctamente delimitadas como una descripción histórica del momento en
  que se escribieron, no una promesa de estado actual — mismo criterio que
  Release 1 aplicó al no reescribir sus propias retrospectivas de sprint.

## Qué NO se hizo, deliberadamente

Mismas tres decisiones que Release 1 ya tomó y siguen vigentes sin
necesidad de repetir el análisis: sin cliente OpenAPI/TypeScript generado,
sin documentación XML de C# en cada DTO para Swagger, sin sitio de
documentación estático — el repo mismo, con Markdown navegable, sigue
siendo la unidad de portafolio.

## Verificación

`dotnet build EnterpriseFlow.slnx` y `dotnet test EnterpriseFlow.slnx`
limpios tras el cambio en `DocumentsEndpoints.cs` (218/218 tests, sin
relación funcional con el resumen de Swagger agregado, pero confirma que no
rompió nada). El resto de los cambios de este sprint son estrictamente
documentación (`.md`), sin superficie de código para probar.
