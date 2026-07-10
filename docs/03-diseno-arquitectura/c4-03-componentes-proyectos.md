# C4 — Nivel 3: Componentes del contenedor API (módulo Proyectos)

Vista de componentes dentro del contenedor **API**, usando el módulo
**Proyectos** como representativo de la plantilla de Vertical Slice que
siguen también Empresas, Clientes, Contactos, Equipos, Tareas y Calendario
(ver justificación en `00-resumen.md`).

```mermaid
C4Component
    title API — Componentes del módulo Proyectos (Vertical Slice)

    Container_Boundary(api, "API") {

        Component(endpoints, "ProjectsEndpoints", "Minimal API Group", "Mapea rutas HTTP a comandos/queries MediatR. Sin lógica de negocio.")

        Boundary(pipeline, "Pipeline Behaviors (MediatR)") {
            Component(authBehavior, "AuthorizationBehavior", "IPipelineBehavior", "Verifica permiso requerido antes de ejecutar el handler")
            Component(validationBehavior, "ValidationBehavior", "IPipelineBehavior", "Ejecuta FluentValidation antes del handler")
            Component(auditBehavior, "AuditBehavior", "IPipelineBehavior", "Registra auditoría para comandos que mutan estado")
        }

        Boundary(appLayer, "Application — Feature: Projects") {
            Component(createCmd, "CreateProjectCommand + Handler", "MediatR Command", "Crea un Proyecto validando Cliente del mismo tenant")
            Component(closeCmd, "CloseProjectCommand + Handler", "MediatR Command", "Cierra un Proyecto solo si no hay tareas abiertas (HU-021)")
            Component(getQuery, "GetProjectByIdQuery + Handler", "MediatR Query", "Lectura de proyección, sin pasar por el agregado de dominio")
            Component(validator, "CreateProjectValidator", "FluentValidation", "Reglas de formato/obligatoriedad de entrada")
        }

        Boundary(domainLayer, "Domain") {
            Component(projectAggregate, "Project (Aggregate Root)", "Entidad de Dominio", "Invariantes: no se cierra con tareas abiertas; pertenece a un Cliente del mismo tenant")
            Component(domainEvents, "ProjectClosedDomainEvent", "Domain Event", "Disparado al cerrar; consumido por Auditoría/futuras Notificaciones")
        }

        Boundary(infraLayer, "Infrastructure") {
            Component(dbContext, "AppDbContext", "EF Core DbContext", "Query filter global por TenantId; aplica soft delete")
            Component(projectSpec, "ProjectHasOpenTasksSpecification", "Specification Pattern", "Encapsula la regla de consulta 'tiene tareas abiertas', reutilizable en validación y en listados")
        }
    }

    Rel(endpoints, authBehavior, "Pasa por")
    Rel(authBehavior, validationBehavior, "Pasa por")
    Rel(validationBehavior, auditBehavior, "Pasa por")
    Rel(auditBehavior, createCmd, "Invoca")
    Rel(auditBehavior, closeCmd, "Invoca")
    Rel(endpoints, getQuery, "Invoca directo (query no muta, sin audit)")

    Rel(createCmd, validator, "Valida con")
    Rel(closeCmd, projectAggregate, "Invoca método de dominio Close()")
    Rel(projectAggregate, domainEvents, "Emite")
    Rel(closeCmd, projectSpec, "Consulta antes de invocar Close()")
    Rel(createCmd, dbContext, "Persiste vía")
    Rel(getQuery, dbContext, "Proyecta vía")
```

## Decisiones clave de este componente

**¿Por qué `GetProjectByIdQuery` no pasa por `AuditBehavior` ni por el
agregado de dominio?**
Las lecturas no mutan estado ni tienen invariantes que proteger; forzarlas a
pasar por el Aggregate Root y por un behavior de auditoría sería trabajo sin
propósito (CQRS: las queries proyectan directamente contra `DbContext`,
devolviendo DTOs de solo lectura). Esto es consistente con la sección
`REGLAS` del documento original ("no generar código duplicado"): un mismo
pipeline no debe forzarse sobre operaciones con necesidades distintas.

**¿Por qué existe `ProjectHasOpenTasksSpecification` (Specification Pattern)
pero no un `IProjectRepository` genérico?**
La regla de negocio "¿tiene tareas abiertas?" se necesita en dos lugares
(validar el cierre en `CloseProjectCommand`, y potencialmente en un badge de UI
"no se puede cerrar"). Encapsularla como Specification evita duplicar la
query LINQ en dos handlers. Un repositorio genérico `IProjectRepository` con
métodos `GetById/Add/Update` por encima de EF Core no añadiría nada que
`DbContext` no dé ya — sería la abstracción redundante que ADR-0001 (sección 5)
descarta explícitamente para agregados sin necesidad real de desacoplo de EF
Core.

**¿Por qué un Domain Event (`ProjectClosedDomainEvent`) en el MVP si
Notificaciones es Release 2?**
El evento se define y se dispara ahora (es parte del comportamiento correcto
del agregado `Project`), pero en Release 1 su único subscriber es
`AuditBehavior`/registro interno. Cuando Notificaciones (Release 2) exista,
se añade un nuevo handler suscrito al mismo evento — sin modificar el
agregado de dominio ni el `CloseProjectCommand`. Este es el punto donde Clean
Architecture paga: el dominio ya está listo para nuevos consumidores del
evento sin cambios.
