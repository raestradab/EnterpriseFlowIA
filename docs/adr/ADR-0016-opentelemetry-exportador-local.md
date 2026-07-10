# ADR-0016: Tracing distribuido con OpenTelemetry y exportador local

- Estado: Aceptado
- Fecha: 2026-07-09
- Relacionado: ADR-0001 (activar infraestructura por caso de uso real,
  nunca especulativamente), `r4-01-vision-y-alcance.md` (sección 0 y 3 —
  Elastic/Application Insights diferidos por necesitar cuenta externa)

## Contexto

Serilog (Release 1, F7.2) da logs estructurados, pero no una vista de
*una request completa* a través del pipeline de MediatR
(`AuthorizationBehavior` → `ValidationBehavior` → `CachingBehavior` →
handler → EF Core →, desde Release 3, una llamada HTTP saliente a
OpenAI/Anthropic) — reconstruir esa cadena hoy exige correlacionar líneas
de log a mano. F7.5 (Release 4) pide tracing distribuido real.

## Decisión

**OpenTelemetry** (SDK vendor-neutral, estándar CNCF) instrumentando
ASP.NET Core, `HttpClient` (las llamadas a OpenAI/Anthropic, Release 3) y
EF Core — con el exportador seleccionable por configuración
(`Observability:Exporter`), igual patrón que `Ai:ChatProvider`
(ADR-0013): **`Console` por defecto** (spans reales, impresos por
consola, verificables en este entorno sin ninguna cuenta externa) u
**`Otlp`** si se configura un endpoint (para quien corra este stack con
su propio backend compatible con OTLP — Jaeger, Tempo, o el propio
Elastic/Application Insights vía su recolector OTLP — sin que este
proyecto necesite tener credenciales de ninguno).

## Alternativas consideradas

- **SDK de Application Insights directamente**
  (`Microsoft.ApplicationInsights.AspNetCore`): rechazado para este
  Release — acopla la instrumentación a un backend específico de Azure
  que necesita una cuenta real y una `InstrumentationKey`, ninguna
  disponible en este entorno (mismo límite que ya aplicó a Elastic,
  `r4-01-vision-y-alcance.md` sección 0). Cambiar de backend después
  significaría re-instrumentar todo el código, no solo cambiar
  configuración.
- **Agente APM de Elastic** (`Elastic.Apm.AspNetCore`): mismo problema —
  acoplado a una cuenta de Elastic Cloud/despliegue propio que este
  entorno no tiene.
- **OpenTelemetry con exportador OTLP fijo, sin `Console`**: rechazado —
  sin ningún colector OTLP corriendo en este entorno, la instrumentación
  quedaría 0% verificable (spans generados pero nunca vistos por nadie).
  `Console` como *default* es lo que hace la diferencia real: da algo
  genuinamente comprobable en este entorno hoy, no una promesa de que
  funcionará cuando exista un backend.
- **No instrumentar en absoluto, esperar a tener un backend real
  primero**: rechazado — la instrumentación (dónde se abren los spans,
  qué atributos llevan) es trabajo real e independiente del backend que
  los reciba; hacerlo ahora con un exportador verificable evita
  re-visitar cada Handler/Behavior cuando exista una cuenta real de
  Elastic/App Insights, momento en el que cambiar el exportador es
  configuración, no código.

## Consecuencias

- Positivo: la instrumentación (los spans, sus atributos) es la misma
  sin importar qué backend termine consumiéndolos — cambiar de `Console`
  a `Otlp` (y de ahí a cualquier backend compatible) es
  `Observability:Exporter` en configuración, nunca una reescritura.
- Positivo: verificable de verdad en este entorno — a diferencia de
  Elastic/Application Insights, que quedarían enteramente sin probar.
- Negativo: `Console` no es un backend de observabilidad real para
  producción — es el *fallback* honesto para este entorno, mismo rol que
  `NullEmailQueue`/`NullAiChatClient` ya cumplen para sus propias
  dependencias externas ausentes, aunque a diferencia de esos dos,
  `Console` sí produce una salida con valor real (spans genuinos, no un
  mensaje de "no configurado").
- Seguimiento: el detalle de qué operaciones llevan spans manuales
  además de la instrumentación automática (p. ej. el loop de tool-use del
  asistente, Release 3) se define en el Sprint de Backend de este
  Release.
