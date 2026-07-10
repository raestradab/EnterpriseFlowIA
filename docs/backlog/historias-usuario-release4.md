# Historias de Usuario y Casos de Uso — Release 4

Mismo formato que
[`historias-usuario-release3.md`](./historias-usuario-release3.md):
`Como <rol>, quiero <acción>, para <beneficio>`, con criterios de
aceptación en Gherkin donde hay una regla de negocio real que lo
justifica. Numeración continua desde donde Release 3 terminó (HU-101) —
a diferencia de Release 3, Release 4 no introduce Epics nuevos con su
propio rango numérico (E9/E10 en su momento); F7.9 es una ampliación de
un Epic que ya existía desde Release 1 (E7).

**Solo una Historia de Usuario en este documento.** El resto del alcance
de Release 4 (OpenTelemetry, BenchmarkDotNet, CodeQL, Dependabot,
Semantic Versioning) es trabajo de ingeniería/operación sin un usuario
final que lo pida en esos términos — se justifica vía ADR y se documenta
en el Sprint correspondiente, no forzado a un formato de HU que no le
queda (mismo criterio que Sprint 11, DevOps, de Releases 1-3 nunca generó
HUs propias tampoco). Contexto de alcance completo, incluyendo qué se
difirió y por qué, en
[`../r4-01-vision-y-alcance.md`](../r4-01-vision-y-alcance.md).

---

## E7 — Auditoría, Logs y Observabilidad (ampliado en Release 4)

### HU-102 — Historial completo de cambios de Proyectos y Tareas (F7.9)
Como administrador de tenant, quiero poder consultar el estado completo
de un Proyecto o una Tarea en cualquier momento pasado, no solo su último
valor conocido, para poder auditar disputas o reconstruir qué pasó
cuando algo salió mal.

```gherkin
Dado un Proyecto que cambió de estado "Planeado" a "Activo" el lunes, y
  de "Activo" a "En pausa" el miércoles
Cuando consulto su estado "tal como estaba" el martes (FOR SYSTEM_TIME AS
  OF)
Entonces veo "Activo" — el valor real de ese momento, no el actual
  ("En pausa") ni solo el último modificado (`ModifiedAtUtc`/`ModifiedBy`,
  que ya existían desde Release 1 pero solo guardan el estado más
  reciente, nunca los intermedios)
```

Regla: la retención de historial es responsabilidad de SQL Server
(System-Versioned Temporal Tables), no de código de aplicación nuevo por
escribir a mano — evita el riesgo de que un cambio directo a la base
(fuera de la aplicación) deje el historial incompleto, algo que un log de
auditoría a nivel de aplicación no podría garantizar.
