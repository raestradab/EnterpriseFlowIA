# Release 4, Sprint 3 — Arquitectura

Mismo alcance que Sprint 3 de Releases anteriores: cerrar las decisiones
que Sprint 1 (Análisis) dejó pendientes para esta fase, y dejar el
esqueleto de lo que Sprint 2 (Diseño) confirmó que no necesita
contenedores nuevos. A diferencia de Release 2/3, este Sprint **no**
define una interfaz nueva en `Application.Abstractions` — ni Temporal
Tables ni OpenTelemetry necesitan una abstracción propia intercambiable
por configuración (no hay "proveedores" alternativos de historial de
cambios o de tracing que este proyecto vaya a intercambiar en runtime, a
diferencia de `IAiChatClient`/`IDocumentStorageProvider`).

## Qué se agregó

- **[ADR-0015](./adr/ADR-0015-temporal-tables-historial-de-cambios.md)** —
  SQL Server System-Versioned Temporal Tables en `Projects`/`ProjectTasks`
  únicamente, comparado contra tabla de auditoría manual (rechazada: es
  código de aplicación que puede quedar incompleto) y Event Sourcing
  (rechazado: desproporcionado para dos entidades).
- **[ADR-0016](./adr/ADR-0016-opentelemetry-exportador-local.md)** —
  OpenTelemetry (vendor-neutral) con exportador `Console` por defecto,
  `Otlp` configurable, en vez de acoplarse directo al SDK de Application
  Insights o al agente de Elastic (ninguno con cuenta disponible en este
  entorno).
- **Wiring real de OpenTelemetry** (`Infrastructure/DependencyInjection.cs`):
  instrumentación de ASP.NET Core, `HttpClient` (cubre las llamadas
  salientes a OpenAI/Anthropic de Release 3) y EF Core, exportador
  seleccionado por `Observability:Exporter` (`console` por defecto,
  `otlp` si se configura `Observability:OtlpEndpoint`). Seis paquetes
  nuevos (`OpenTelemetry.Extensions.Hosting` y los cuatro paquetes de
  instrumentación/exportadores) — el de EF Core sigue en beta upstream
  (`1.16.0-beta.1`, sin versión estable publicada todavía), usado igual
  porque es la única forma de instrumentar EF Core sin escribir un
  interceptor a mano; su estado se deja explícito en el propio código.

## Por qué no hay Null fallback aquí

A diferencia de `IAiChatClient`/`IEmbeddingClient` (que caen a una
implementación Null cuando no hay proveedor configurado), OpenTelemetry
**siempre está activo** — no hay un estado "sin telemetría configurada"
que degradar con gracia: `Console` **es** el *fallback* razonable en sí
mismo (spans reales, verificables, sin ninguna cuenta externa), no un
mensaje de "no configurado". Mismo espíritu que llevó a que
`LocalStorageProvider` (Release 2) fuera un proveedor real y útil por
defecto, no un caso especial.

## Verificación

**Ejecución real, no solo revisión de código:**

- Se corrió la Api real (`dotnet run`) y se confirmó en la consola spans
  reales emitidos para requests reales:
  - `GET /health` — un span `Microsoft.AspNetCore` completo (`TraceId`,
    `SpanId`, `http.route`, `http.response.status_code`, el
    `service.name: EnterpriseFlow.Api` correcto).
  - `POST /api/auth/login` (credenciales inválidas a propósito) — el span
    HTTP padre **y** un span hijo de
    `OpenTelemetry.Instrumentation.EntityFrameworkCore` con
    `db.statement` mostrando el SQL real ejecutado
    (`SELECT ... FROM [RefreshTokens] WHERE [TokenHash] = @__incomingHash_0`)
    y `ParentSpanId` correlacionándolo con el span HTTP — trazabilidad
    distribuida genuina, no solo instrumentación configurada sin efecto.
  - Tráfico orgánico del propio frontend (`GET /api/clients`,
    `GET /api/companies`, servidos al navegador que ya estaba usando la
    Api) generó spans reales también, sin ninguna intervención especial —
    confirma que la instrumentación captura tráfico real de uso, no solo
    llamadas sintéticas de verificación.
- `dotnet build`/`dotnet test EnterpriseFlow.slnx` — **281/281** — y
  `dotnet format --verify-no-changes` limpios.
- **Sin backend de observabilidad real detrás** (Elastic/Application
  Insights, diferidos) — dicho explícitamente, no asumido; `Console` es
  lo que se verificó, `Otlp` queda con su *wiring* real pero sin un
  colector corriendo en este entorno contra el cual probarlo.

## Qué no se hizo en este sprint (a propósito)

- Ninguna entidad de Domain nueva ni configuración de EF Core para
  Temporal Tables — Sprint de Modelo de Dominio/Base de Datos de este
  Release, mismo criterio que Release 2/3 ya aplicaron (Sprint 3 es
  esqueleto de mecanismo cross-cutting, no el detalle de una feature
  específica).
- Spans manuales adicionales sobre el loop de tool-use del asistente
  (Release 3) más allá de lo que la instrumentación automática de
  `HttpClient` ya captura — Sprint de Backend de este Release, si se
  justifica.
- BenchmarkDotNet, CodeQL, Dependabot, Semantic Versioning — no son
  arquitectura cross-cutting de runtime, quedan para Sprint 9 (Pruebas)
  y Sprint 11 (DevOps) respectivamente, según a qué fase corresponde cada
  uno.
