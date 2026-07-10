# ADR-0002: Clean Architecture combinada con Vertical Slice Architecture

- Estado: Aceptado
- Fecha: 2026-07-06
- Relacionado: ADR-0001

## Contexto

La especificación pide simultáneamente "Clean Architecture", "DDD", "CQRS" y
"Vertical Slice Architecture". Estos estilos no son mutuamente excluyentes,
pero sí requiere definirse **en qué eje aplica cada uno**, porque Clean
Architecture tradicionalmente organiza el código por capa técnica (Domain /
Application / Infrastructure / Presentation) mientras que Vertical Slice
organiza por caso de uso (todo lo de "crear proyecto" junto). Aplicarlas sin
definir la frontera produce, en la práctica, o bien un monolito de capas con
servicios "God class" (Clean Architecture mal aplicada), o bien slices que
reimplementan reglas de dominio en cada handler sin un núcleo compartido
(Vertical Slice sin Domain layer).

## Decisión

Se combinan en dos ejes distintos:

- **Eje de dependencias (Clean Architecture / Dependency Rule)**: se mantienen
  4 proyectos — `Domain`, `Application`, `Infrastructure`, `Api` — con la regla
  de dependencia estándar (Domain no depende de nada; Application depende solo
  de Domain; Infrastructure implementa interfaces definidas en Application;
  Api depende de Application e Infrastructure solo para composición/DI).
- **Eje de organización interna (Vertical Slice)**: dentro de `Application`,
  el código no se organiza por tipo técnico (`Commands/`, `Queries/`,
  `Validators/` como carpetas de primer nivel), sino **por feature**:

```
Application/
  Features/
    Projects/
      CreateProject/
        CreateProjectCommand.cs
        CreateProjectCommandHandler.cs
        CreateProjectValidator.cs
      CloseProject/
        CloseProjectCommand.cs
        CloseProjectCommandHandler.cs
      GetProjectById/
        GetProjectByIdQuery.cs
        GetProjectByIdQueryHandler.cs
        ProjectDto.cs
    Clients/
      ...
```

Cada carpeta de caso de uso es autocontenida y solo se comunica con el resto
del sistema a través de `Domain` (entidades/reglas) e interfaces de
`Application` (`IAppDbContext`, `ICurrentTenantService`, etc.), nunca
directamente con otra feature.

`Domain` contiene los agregados con sus invariantes (p. ej. `Project.Close()`
lanza excepción de dominio si hay tareas abiertas — no es el `Handler` quien
decide esto, es el propio agregado). Esto es lo que hace que MediatR/CQRS no
degenere en "servicios anémicos": el Handler orquesta (carga, invoca método de
dominio, persiste), pero la regla de negocio vive en `Domain`, reusable y
testeable sin infraestructura.

## Alternativas consideradas

1. **Clean Architecture clásica en capas técnicas** (`Controllers/`,
   `Services/`, `Repositories/` con una clase por entidad). Rechazada: con ~10
   módulos, los `Services` tienden a crecer con métodos de múltiples casos de
   uso no relacionados entre sí, dificultando ubicar y aislar el código de una
   sola historia de usuario — justo lo que Vertical Slice resuelve.
2. **Vertical Slice puro, sin capa de Domain separada** (cada slice con su
   propia lógica y acceso directo a EF Core). Rechazada: las invariantes de
   negocio (HU-021, HU-023) quedarían duplicadas o inconsistentes entre el
   `CreateProjectHandler` y el `CloseProjectHandler` si no existe un agregado
   `Project` único que las centralice.
3. **DDD con Repository genérico para todos los agregados**
   (`IRepository<T>: Add, Update, Delete, GetById`). Rechazada como *default*:
   sobre EF Core, un repositorio genérico solo reintroduce lo que `DbContext`
   ya ofrece, sin aportar desacoplo real (EF Core ya es la abstracción sobre
   el motor de base de datos). Se usa Repository/Specification puntualmente
   solo donde una consulta compleja se reutiliza entre casos de uso (ver
   ADR-0001 punto 5 y `c4-03-componentes-proyectos.md`).

## Consecuencias

- Positivo: alta cohesión por caso de uso, bajo acoplamiento entre features —
  añadir un módulo nuevo (p. ej. Documentos en Release 2) no toca código de
  módulos existentes.
- Positivo: las reglas de negocio son testeables unitariamente contra `Domain`
  puro, sin levantar MediatR ni EF Core (pruebas rápidas, sin mocks pesados).
- Negativo: mayor cantidad de archivos pequeños por caso de uso (mitigado con
  convenciones de nombres y templates/snippets, no es un costo de diseño sino
  de tecleo).
- Seguimiento: si un módulo futuro no encaja en esta plantilla (p. ej. RAG,
  que no es CRUD de un agregado), se documenta su propia variante en un ADR
  corto al iniciar su Release, en vez de forzar la plantilla.
