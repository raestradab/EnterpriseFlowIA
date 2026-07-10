# ADR-0015: Historial de cambios de Project/ProjectTask vía SQL Server Temporal Tables

- Estado: Aceptado
- Fecha: 2026-07-09
- Relacionado: ADR-0001 (activar infraestructura por caso de uso real, no
  especulativamente), ADR-0003 (aislamiento multi-tenant, que el
  historial también debe respetar)

## Contexto

HU-102 (F7.9, Release 4): un administrador de tenant necesita poder
consultar el estado completo de un `Project`/`ProjectTask` en cualquier
momento pasado, no solo su último valor conocido. `CreatedAtUtc`/
`CreatedBy`/`ModifiedAtUtc`/`ModifiedBy` (`IAuditableEntity`, desde
Release 1) ya registran *quién y cuándo fue la última vez* que algo
cambió, pero no *qué valores tenía antes* — un Proyecto que pasó por tres
estados distintos en una semana solo deja ver el tercero.

## Decisión

**SQL Server System-Versioned Temporal Tables**, activadas únicamente en
`Projects` y `ProjectTasks` — no en las 21 tablas del sistema. EF Core 8
tiene soporte nativo (`.ToTable(tb => tb.IsTemporal())`, operadores LINQ
`TemporalAsOf`/`TemporalBetween`/`TemporalAll`); SQL Server mantiene la
tabla de historial (`ProjectsHistory`/`ProjectTasksHistory`)
automáticamente en cada `UPDATE`/`DELETE` — **sin código de aplicación
que lo pueble ni lo pueda saltear**, ni siquiera un cambio directo por
`sqlcmd` fuera de la Api.

## Alternativas consideradas

- **Tabla de auditoría manual** (`ProjectAuditLog`, poblada vía un
  interceptor de `SaveChanges` o un manejador de evento de dominio,
  guardando valores viejos/nuevos): rechazada — es código de aplicación
  real que hay que escribir, probar y mantener, con dos riesgos que
  Temporal Tables no tiene: (1) cualquier ruta de escritura que no pase
  por ese interceptor específico (una migración de datos a mano, un
  script de corrección) deja el historial incompleto sin que nada lo
  detecte; (2) reconstruir "el estado tal como estaba en el momento X" a
  partir de una serie de diffs es lógica no trivial que también habría
  que escribir y probar — Temporal Tables ya la resuelve con una sola
  cláusula SQL (`FOR SYSTEM_TIME AS OF`) que EF Core traduce
  directamente.
- **Event Sourcing** (el estado de cada entidad se deriva reproduciendo
  su historial de eventos, no se persiste directamente): rechazada por
  desproporción — exigiría rediseñar el modelo de persistencia de *todo*
  el sistema (los otros 19 tipos de entidad siguen con CRUD+EF Core
  directo desde Release 1) para resolver una necesidad de solo dos
  entidades. Válida si el producto entero se rediseñara alrededor de
  eventos como fuente de verdad, no como respuesta puntual a HU-102.
- **Activar Temporal Tables en las 21 entidades del sistema**: rechazada
  por el mismo motivo que ya activó Redis solo para Catálogos (ADR-0008)
  — ninguna otra entidad tiene todavía un caso de uso real de "necesito
  ver su estado en un momento pasado"; extenderlo después, si aparece esa
  necesidad, es una migración más por entidad, no una reescritura.

## Consecuencias

- Positivo: cero código de aplicación para mantener el historial — la
  garantía de completitud viene de la base de datos, no de disciplina de
  desarrollo.
- Positivo: consultar el historial es una Query de Application más
  (`db.Projects.TemporalAsOf(fecha)...`), con el mismo filtro global de
  tenant (ADR-0003) aplicando igual que a cualquier otra consulta — no es
  una ruta de acceso paralela sin aislamiento.
- Negativo: duplica el almacenamiento de esas dos tablas (una fila nueva
  en la tabla de historial por cada `UPDATE`, nunca se borra sola). Sin
  política de retención/purga en este Release — ninguna HU la pide
  todavía; revisitable si el volumen real lo justifica.
- Seguimiento: el diseño exacto (qué columnas quedan ocultas como
  `PeriodStart`/`PeriodEnd`, la migración en sí) se define en el Sprint
  de Base de Datos de este Release — este ADR fija la decisión de
  mecanismo y alcance, no el DDL exacto.
