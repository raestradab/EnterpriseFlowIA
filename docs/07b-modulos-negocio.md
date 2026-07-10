# Sprint 7b — Backend: Módulos de Negocio

Segunda mitad de Sprint 7, tras Identidad (7a). Alcance: HU-011, HU-012,
HU-020 a HU-024 — Clientes, Contactos, Proyectos (+ Equipos), Tareas y
Calendario, cerrando el Backend completo del MVP.

## Qué se implementó

Mismo patrón que Empresas (Sprint 4) e Identidad (Sprint 7a) para cada
módulo: Command/Query + Handler + Validator (donde aplica) sobre
`AuthorizationBehavior`+`ValidationBehavior`, permisos `<módulo>.read`/
`<módulo>.manage` añadidos a `Permissions`.

- **Clientes** (F2.2): Create, GetById, Deactivate.
- **Contactos** (F2.3): Create, GetById.
- **Proyectos** (F3.1/F3.2): Create, GetById, Close (HU-021), AddMember/
  RemoveMember (HU-022).
- **Tareas** (F3.3): Create, GetById, Assign (HU-023), Complete, Cancel.
- **Calendario** (F3.4): `GetMyCalendarQuery` — proyección de solo lectura de
  las tareas del usuario actual por rango de fechas, sin entidad de dominio
  propia (ya decidido en `05-modelo-dominio.md`).

## Despacho de domain events, por fin cableado

Los eventos `ClientDeactivatedDomainEvent` y `ProjectClosedDomainEvent`
existían desde Sprint 5 pero nadie los publicaba. Este sprint necesitaba que
HU-012 funcionara de verdad (cascada real a Contactos), así que se cableó:
`AppDbContext.SaveChangesAsync` ahora recolecta los eventos de las entidades
modificadas, guarda, y **después** publica cada uno envuelto en
`DomainEventNotification<T>` vía `IPublisher` — solo tras confirmar que el
guardado tuvo éxito. `CascadeContactsOnClientDeactivatedHandler` (Application)
reacciona a ese evento y desactiva los Contactos del Cliente.

## Desviación documentada del diseño original (Sprint 2)

`c4-03-componentes-proyectos.md` proponía una `ProjectHasOpenTasksSpecification`
en Infrastructure. Al implementarlo, `CloseProjectCommandHandler` resuelve el
hecho directamente con `IAppDbContext` (ya disponible en Application) en vez
de introducir un tipo en Infrastructure que Application necesitaría una
interfaz para alcanzar — mismo resultado, una capa menos. Documentado inline
en el propio handler, no oculto.

## El bug más sutil hasta ahora — y la corrección real, no un parche

Al añadir un miembro a un Proyecto (`POST /api/projects/{id}/members`), EF
Core lanzaba `DbUpdateConcurrencyException: expected to affect 1 row(s), but
actually affected 0` — generando un **UPDATE** para una fila que nunca se
había insertado.

**Causa raíz**: todas las entidades generan su `Id` en el cliente
(`Guid.NewGuid()` en `BaseEntity`). Cuando una entidad se agrega vía
`db.Set<T>().Add(...)` explícito, EF Core la marca `Added` sin ambigüedad. Pero
`ProjectMember` se agrega **indirectamente**: `AddProjectMemberCommandHandler`
carga un `Project` ya existente (`Include(p => p.Members)`) y llama a
`project.AddMember(...)`, que solo muta la lista en memoria. EF Core descubre
esa entidad nueva por "fixup" de la colección rastreada, no por un `Add()`
explícito — y su convención por defecto para claves con generación "on add"
asume: *valor no-default en la clave = la entidad probablemente ya existe*.
Resultado: la marca `Modified` en vez de `Added`, y el UPDATE no afecta
ninguna fila porque la fila nunca existió.

Esto **no es un caso aislado de `ProjectMember`** — es estructural a todo
`BaseEntity`, y `AssignRoleToUserCommandHandler` (Sprint 7a) tenía la misma
clase de fragilidad latente sin una prueba que la ejercitara. La corrección
es, por tanto, en `AppDbContext.OnModelCreating`, no en cada configuración:
todas las entidades que heredan de `BaseEntity` se marcan
`ValueGenerated.Never` para su `Id` en un único lugar, cerrando la clase
completa de bug para cualquier entidad futura, no solo `ProjectMember`.

De paso se corrigió `AssignRoleToUserCommandHandler`, que tampoco cargaba
`RoleAssignments` antes de comprobar duplicados — la tercera vez que aparece
exactamente el mismo patrón de "colección no cargada antes de mutarla o
comprobarla" (Login, AddProjectMember, AssignRoleToUser). Ya no es casualidad:
toda esta clase de handlers necesita este cuidado, y ahora hay una prueba de
integración (`Task_Assignment_Requires_Project_Membership`) que ejercita el
camino de `AddProjectMember` específicamente para que no se repita en
silencio.

## Pruebas

68 pruebas en total (antes 65; +14 en integración, con 3 nuevas específicas de
este sprint cubriendo cascada de desactivación, cierre con tareas abiertas, y
restricción de asignación a miembros del equipo — las tres a través de HTTP
con JWT real, no mocks).

## Cierre de Sprint 7 (Backend)

Con 7a + 7b, el Backend del MVP está completo: Identidad, Empresas, Clientes,
Contactos, Proyectos, Equipos, Tareas y Calendario, todos con el mismo
pipeline (CQRS + autorización por permiso + validación), multi-tenancy real,
auditoría y soft-delete automáticos. Sigue Sprint 8 (Frontend).
