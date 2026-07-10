# ADR-0005: Invariantes cross-aggregate resueltas con "hechos inyectados"

- Estado: Aceptado
- Fecha: 2026-07-06
- Relacionado: ADR-0002

## Contexto

Dos invariantes del MVP cruzan fronteras de agregado:

- HU-021: `Project` no puede cerrarse si tiene `ProjectTask` abiertas. Pero
  `ProjectTask` es un agregado separado (tiene su propio ciclo de vida,
  tenant/auditoría/soft-delete propios) — `Project` no tiene ni debe tener una
  colección cargada de sus Tareas.
- HU-023: una `ProjectTask` solo puede asignarse a un usuario miembro del
  equipo del `Project`. La membresía vive dentro del agregado `Project`
  (`ProjectMember`), no en `ProjectTask`.

En DDD clásico, un agregado no debe consultar directamente a otro (eso
requeriría inyectar un repositorio en el dominio, rompiendo la regla de
dependencias de ADR-0002 y mezclando orquestación con lógica de negocio).

## Decisión

El agregado expone un método que **recibe el hecho ya resuelto como
parámetro**, no que lo calcula:

```csharp
// Project.cs
public void Close(bool hasOpenTasks)
{
    if (hasOpenTasks) throw new ProjectHasOpenTasksException(Id);
    Status = ProjectStatus.Closed;
}

// ProjectTask.cs
public void AssignTo(Guid userId, bool isProjectMember)
{
    if (!isProjectMember) throw new TaskAssigneeMustBeProjectMemberException(Id, userId);
    AssignedToUserId = userId;
}
```

La **consulta cross-aggregate** (¿tiene tareas abiertas?, ¿es miembro del
proyecto?) vive en Application — el `CloseProjectCommandHandler` consulta
`ProjectTask` (vía `IAppDbContext`, o la Specification ya prevista en
`c4-03-componentes-proyectos.md`) y el `AssignTaskCommandHandler` carga el
`Project` y llama a `project.IsMember(userId)` — y pasa el resultado al
método de dominio. **La decisión de negocio (¿se permite la transición?) sigue
viviendo en el agregado**, que puede rechazarla; lo único que se mueve fuera
es el acceso a datos que el agregado no puede alcanzar por sí mismo.

## Alternativas consideradas

- **El Handler decide todo y el agregado solo expone setters** (p. ej.
  `project.Status = ProjectStatus.Closed`): rechazada — mueve la invariante
  fuera del dominio, exactamente el "servicio anémico" que ADR-0002 evita. Un
  desarrollador podría cerrar un proyecto sin pasar por la validación si
  escribe código nuevo que no la reproduce.
- **El agregado recibe un repositorio/query-service inyectado en el método**
  (p. ej. `Close(IProjectTaskRepository repo)`): rechazada — el dominio no
  debe depender de abstracciones de acceso a datos ni de nada que requiera un
  `DbContext`/`IServiceProvider` detrás; complica las pruebas unitarias del
  dominio (dejarían de ser puras) y viola la regla de dependencias.
- **Fusionar `ProjectTask` dentro del agregado `Project`** (una sola raíz con
  colección de tareas cargada): rechazada — con muchas tareas por proyecto,
  cargar el agregado completo en cada operación (incluso una edición menor de
  una tarea) es costoso e innecesario; también haría que Tareas heredara
  auditoría/soft-delete/tenant de forma indirecta en vez de tener su propio
  ciclo de vida real (una Tarea se crea, edita y cierra independientemente).

## Consecuencias

- Positivo: los agregados siguen siendo unidades pequeñas y con pruebas
  unitarias puras (ver `ProjectTests`/`ProjectTaskTests` — ninguna usa mocks
  de infraestructura).
- Positivo: la regla de negocio no puede omitirse accidentalmente desde un
  Handler nuevo, porque el método del agregado la re-verifica siempre que se
  invoque, sin importar quién calcule el booleano.
- Negativo: el booleano/hecho debe calcularse *correctamente* en Application
  antes de llamar al método — un bug ahí (p. ej. una consulta mal filtrada)
  podría dejar pasar un hecho falso. Mitigación: estas consultas se
  encapsulan en Specifications reutilizables (Sprint 7) y se cubren con
  integration tests reales, no solo con mocks del booleano.
- Seguimiento: si en un Release futuro esta cross-check se vuelve más
  compleja (varias condiciones combinadas), considerar un Domain Service
  explícito en vez de seguir agregando parámetros booleanos al método.
