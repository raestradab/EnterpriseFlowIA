# ADR-0008: Activación de Redis, Hangfire y Response Compression en Release 2

- Estado: Aceptado
- Fecha: 2026-07-07
- Relacionado: ADR-0001 (punto 4, "se difieren hasta que un caso de uso
  concreto los requiera" — este ADR es el seguimiento que ese punto ya
  anticipaba al cierre de Release 1)

## Contexto

ADR-0001 difirió Redis, Hangfire, MassTransit/RabbitMQ, OpenTelemetry,
Response Compression, etc. fuera del MVP explícitamente porque ninguno tenía
todavía un caso de uso real que los justificara — añadirlos "porque la lista
los pide" habría violado YAGNI y agregado superficie de fallo sin beneficio
demostrable. Ese mismo ADR también dejó dicho que se revisaría "al cierre de
cada Release para confirmar que la priorización sigue vigente".

Al analizar el alcance de Release 2 (Documentos, Notificaciones, Workflow,
Catálogos — ver `docs/backlog/epics.md`), aparecen por primera vez tres casos
de uso reales, no anticipados especulativamente:

1. **F6.2 (Notificaciones por correo)**: enviar un email es una operación de
   I/O externa lenta (proveedor SMTP/API de terceros) que no debe bloquear el
   hilo de la request HTTP que la origina — a diferencia de todo lo construido
   en Release 1, donde ninguna operación tenía esta característica.
2. **F8.2 (Catálogos genéricos)**: listas maestras reutilizables (dropdowns
   de opciones, listas de referencia) — leídas con altísima frecuencia
   (cada formulario que las usa) y escritas rarísima vez (un administrador
   las edita ocasionalmente). El patrón cache-aside de manual.
3. **F4.3 (Reportes exportables)**: el primer endpoint del producto que
   devuelve un dataset completo para exportar, no un listado paginado — el
   primer caso real donde el tamaño de la respuesta importa.

## Decisión

Se activan las tres piezas de infraestructura que Release 1 difirió, cada
una atada a un caso de uso concreto de Release 2, no de forma general:

1. **Hangfire**, para F6.2 — encolar el envío de correo como background job
   en vez de ejecutarlo síncronamente en el handler de la Command. SQL Server
   como storage de Hangfire (no Redis/otro): ya es una dependencia existente
   del proyecto, evita introducir una pieza de infraestructura nueva solo
   para persistir la cola de jobs.
2. **Redis**, para F8.2 — cache-aside sobre las lecturas de Catálogos
   (`IDistributedCache`, no un cliente de Redis atado directamente al código
   de Application — mismo principio de abstracción que ADR-0003 aplicó al
   tenant). Invalidación explícita en cada escritura del catálogo (create/
   update/delete), no time-based únicamente — los datos de referencia deben
   reflejar cambios de inmediato para un administrador que acaba de editar.
3. **Response Compression** (Brotli/Gzip), activado globalmente en el
   pipeline de la Api — a diferencia de Hangfire/Redis, esto no requiere un
   caso de uso "propio": una vez que existe al menos un endpoint con
   payloads grandes (F4.3), el costo de activarlo para toda la Api es
   prácticamente cero (un middleware) y el beneficio se extiende a
   cualquier endpoint futuro con el mismo patrón, sin trabajo adicional.

Explícitamente **no** se activan en Release 2 (siguen diferidos, sin caso de
uso real todavía): MassTransit/RabbitMQ, OpenTelemetry, Elastic Search,
Application Insights — Release 2 no introduce comunicación entre servicios
ni necesita tracing distribuido (sigue siendo un monolito Api+Web), así que
activarlos ahora repetiría exactamente el error que ADR-0001 evitó en
Release 1: infraestructura sin un caso de uso concreto detrás.

## Alternativas consideradas

- **MassTransit + RabbitMQ para F6.2 en vez de Hangfire**: rechazada para
  este caso — un message broker completo se justifica cuando hay múltiples
  servicios/consumidores independientes o necesidad de garantías de entrega
  entre procesos distintos; enviar un correo desde el mismo proceso que lo
  encola es exactamente el caso de uso que Hangfire (background jobs
  in-process, con dashboard y reintentos) resuelve sin la complejidad
  operativa de un broker separado. Se revisita si Release 2/3 introduce un
  segundo consumidor real de esos eventos.
- **Memory Cache en vez de Redis para F8.2**: rechazada — `IMemoryCache` no
  se comparte entre instancias del proceso Api; en un despliegue con más de
  una réplica (el propio `docker-compose.yml` ya corre la Api como un único
  contenedor, pero el objetivo declarado del proyecto es demostrar patrones
  de nivel Staff Engineer, y un cache que se invalida de forma inconsistente
  entre réplicas es precisamente el tipo de bug de producción que ese nivel
  debe anticipar) no sería consistente. `IDistributedCache` respaldado por
  Redis es la abstracción correcta desde el día uno, con Redis como
  implementación real, no como demostración vacía.
- **Activar todo lo diferido en ADR-0001 de una vez, ya que "en algún
  momento habrá que hacerlo"**: rechazada — es exactamente el razonamiento
  que ADR-0001 ya rechazó para Release 1. Cada pieza se activa solo cuando
  su caso de uso aparece, no antes.

## Consecuencias

- Positivo: cada pieza de infraestructura nueva tiene un caso de uso real
  que se puede señalar en el código (`grep` a la Feature que la usa), no
  queda como "porque la especificación la pedía".
- Positivo: Response Compression, al ser un middleware transversal de costo
  marginal, beneficia también a los endpoints de Release 1 (listados de
  Empresas/Clientes/Proyectos/Tareas) sin trabajo adicional — un caso donde
  activar algo para un caso de uso puntual termina beneficiando al resto del
  sistema sin más costo.
  > **Corrección, Release 4 Sprint 10**: esta decisión nunca se implementó de
  > verdad — `Program.cs` nunca llamó `AddResponseCompression`/
  > `UseResponseCompression`, sin que ningún Sprint posterior lo notara.
  > Encontrado auditando `especificcion.md` contra el `Program.cs` real, y
  > corregido en el mismo Sprint. Al agregarlo se encontró además un segundo
  > problema: registrarlo después de `UseSwagger()` lo dejaba sin efecto para
  > ese endpoint (Swagger es middleware terminal, corta la cadena antes de
  > llegar a la compresión) — solo se detectó probando la respuesta real con
  > `curl -H "Accept-Encoding: gzip, br"`, no leyendo el código. Reordenado al
  > principio del pipeline; verificado de nuevo: `/swagger/v1/swagger.json`
  > pasó de 35.167 a 6.159 bytes con Brotli. Ver `r4-10-documentacion.md`.
- Negativo: `docker-compose.yml` (Sprint 11, Release 1) necesita un servicio
  `redis` nuevo — se actualiza en el Sprint de DevOps de Release 2, no antes,
  siguiendo la misma secuencia análisis→...→DevOps aplicada por Release.
- Seguimiento: este ADR se revisa igual que ADR-0001 al cierre de Release 2 —
  si alguno de los casos de uso que lo justificó cambia de forma sustancial
  (p. ej. F6.2 se descarta), la pieza de infraestructura asociada se
  reconsidera, no se mantiene por inercia.
