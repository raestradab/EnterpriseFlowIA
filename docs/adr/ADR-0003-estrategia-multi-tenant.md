# ADR-0003: Estrategia de Multi-Tenancy

- Estado: Aceptado
- Fecha: 2026-07-06
- Relacionado: ADR-0002

## Contexto

El sistema debe aislar datos entre organizaciones (tenants) que comparten la
misma instancia de la aplicación. Existen tres estrategias estándar y hay que
elegir una para el MVP, dejando la puerta abierta a migrar si un cliente
enterprise real lo exige más adelante.

## Opciones evaluadas

| Estrategia | Aislamiento | Complejidad operativa | Costo de migraciones EF Core | Adecuado para portafolio/MVP |
|---|---|---|---|---|
| Base de datos por tenant | Máximo | Alta (N bases de datos, N connection strings, backups por tenant) | Se ejecutan N veces | No — sobreingeniería para demostrar el patrón |
| Esquema por tenant (misma BD) | Medio-alto | Media (N esquemas, EF Core requiere `DbContext` dinámico por esquema) | Complejo de automatizar | No — complejidad de tooling no justificada aún |
| Base de datos y esquema compartidos + columna `TenantId` + Global Query Filter | Medio (lógico, no físico) | Baja (una sola BD, un solo pipeline de migraciones) | Trivial | **Sí** |

## Decisión

Se adopta **base de datos compartida, esquema compartido, con columna
`TenantId` en cada tabla de negocio** y **EF Core Global Query Filters** que
aplican automáticamente `WHERE TenantId = @currentTenantId` a toda consulta,
sin que cada Handler tenga que recordarlo.

Componentes de la solución:
1. `ICurrentTenantService` (Application) — expone el `TenantId` resuelto para
   la request actual. Implementado en `Infrastructure`/`Api` a partir del claim
   `tenant_id` del JWT (ver secuencia en `03-diseno-arquitectura/04-secuencias.md`).
2. `AppDbContext.OnModelCreating` aplica `HasQueryFilter(e => e.TenantId ==
   _currentTenant.TenantId)` a todas las entidades que implementen la interfaz
   marcador `ITenantScoped`.
3. Un `SaveChangesInterceptor` asigna automáticamente `TenantId` al insertar
   una entidad nueva, para que ningún Handler tenga que asignarlo manualmente
   (elimina una clase entera de bugs: "olvidé setear el tenant").
4. Las tablas de catálogo verdaderamente globales (si las hubiera) no
   implementan `ITenantScoped` y quedan fuera del filtro explícitamente.

## Alternativas consideradas

- **Base de datos por tenant**: rechazada para el MVP por el costo operativo
  (N bases, N migraciones, N backups) frente al beneficio — el objetivo es
  demostrar el patrón multi-tenant con datos reales, no operar SaaS real con
  decenas de clientes. Se documenta como opción de escalamiento futura si un
  ADR posterior justifica aislamiento físico (p. ej. requisito de compliance).
- **Esquema por tenant**: rechazada — EF Core no soporta nativamente `DbContext`
  con esquema dinámico sin trabajo adicional considerable (fábricas de
  `DbContext` por esquema, migraciones por esquema), complejidad no
  justificada por el alcance del MVP.
- **Confiar en que cada Handler filtre por tenant manualmente**: rechazada de
  forma explícita — es la opción más simple de programar pero la más frágil:
  un solo Handler que olvide el filtro es una fuga de datos entre tenants. El
  Global Query Filter mueve la garantía de "aislamiento correcto" del nivel
  "disciplina del desarrollador" al nivel "infraestructura", que es más
  confiable y auditable.

## Consecuencias

- Positivo: aislamiento correcto por defecto, sin depender de que cada nuevo
  caso de uso lo implemente bien.
- Positivo: migraciones EF Core, backups y operación siguen siendo las de una
  única base de datos — bajo costo operativo, apto para demo/portafolio.
- Negativo: aislamiento es lógico, no físico — un bug en la capa de
  infraestructura (no en el Global Query Filter en sí, sino p. ej. una query
  SQL cruda mal escrita con `FromSqlRaw`) podría saltarse el filtro. Mitigación:
  prohibir `FromSqlRaw`/SQL crudo salvo excepción documentada y revisada.
- Seguimiento: si en Release 4 o posterior surge un requisito real de
  aislamiento físico (p. ej. un cliente que exige su propia base de datos por
  contrato), se abre un ADR de migración a "esquema o base de datos por
  tenant", reutilizando `ICurrentTenantService` como punto de extensión (el
  resto del código de Application no debería cambiar).
