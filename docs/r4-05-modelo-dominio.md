# Release 4, Sprint 5 — Modelo de Dominio

Mismo alcance que Sprint 5 de Releases anteriores: modelar lo que Sprint 4
(Validación) no haya cubierto todavía. A diferencia de Release 2/3, este
Sprint es de **confirmación, no de introducción** — Release 4 no agrega
ninguna entidad de Domain nueva.

## Por qué no hace falta ninguna entidad nueva

- **Temporal Tables (HU-102, ADR-0015)** es una capacidad de persistencia
  pura — SQL Server mantiene el historial, EF Core lo traduce vía
  `TemporalAsOf`. `Project`/`ProjectTask` (`src/EnterpriseFlow.Domain/Entities/`)
  **no cambiaron ni una línea** en Sprint 3 ni Sprint 4: la entidad de
  Domain no sabe ni necesita saber que su tabla es temporal, exactamente
  igual que no sabe si su tabla tiene un índice o no. Confirmado
  revisando ambos archivos — cero diferencias desde antes de Release 4.
- **OpenTelemetry, BenchmarkDotNet, CodeQL, Dependabot, Semantic
  Versioning** — ninguno modela un concepto de negocio del dominio de
  EnterpriseFlow; son infraestructura cross-cutting y herramientas de
  ingeniería, sin invariante que Domain deba proteger.

## Verificación

`EnterpriseFlow.Domain.UnitTests`: **141/141**, el mismo número exacto que
al cierre de Release 3 (Sprint 9-11) — ninguna prueba nueva, ninguna
rota, porque no hubo ningún cambio de código en `EnterpriseFlow.Domain`
que verificar. `EnterpriseFlow.Architecture.Tests` (que ya incluye la
regla "Domain no depende de ninguna otra capa") sigue en 6/6, confirmando
que la nueva pieza de Application (`IAppDbContext.GetProjectsAsOf`,
Sprint 4) no introdujo ninguna dependencia indebida hacia Domain tampoco
— el método sigue trabajando sobre el mismo `Project` de siempre, sin
necesitar tocarlo.

## Qué no se hizo en este sprint (a propósito)

Nada — no hay trabajo de modelado pendiente para Release 4. Sprint 6
(Base de Datos) tiene el mismo carácter de confirmación: la migración de
Temporal Tables ya se generó y verificó contra LocalDB real en Sprint 4,
así que tampoco queda pendiente ningún DDL nuevo.
