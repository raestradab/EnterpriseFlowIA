# ADR-0011: Notificaciones se entregan reusando el pipeline de Domain Events existente

- Estado: Aceptado
- Fecha: 2026-07-08
- Relacionado: ADR-0008 (por qué no se introduce un message bus), HU-060/
  HU-061/HU-062

## Contexto

Release 1 ya estableció un mecanismo de Domain Events: una entidad llama
`Raise(new AlgoOcurrióEvent(...))`, y tras un `SaveChangesAsync` exitoso,
`AuditableEntitySaveChangesInterceptor` los despacha vía `IPublisher` de
MediatR como `DomainEventNotification<T>` a cualquier
`INotificationHandler<DomainEventNotification<T>>` registrado (usado hoy
por `CascadeContactsOnClientDeactivatedHandler`, HU-012). Release 2 necesita
que ciertos eventos (Documento aprobado/rechazado, Tarea asignada) además
de sus efectos actuales, disparen una notificación in-app (F6.1) y,
opcionalmente, un correo (F6.2).

## Decisión

**No se introduce un mecanismo de eventos nuevo.** Los eventos de dominio
relevantes (`DocumentApprovedDomainEvent`, `DocumentRejectedDomainEvent`,
más el ya existente `TaskAssignedDomainEvent` si aplica) ganan handlers
adicionales que implementan `INotificationHandler<DomainEventNotification<T>>`
— el mismo contrato que ya usa `CascadeContactsOnClientDeactivatedHandler` —
en vez de que la Api tenga una ruta de código separada "para notificaciones".

Dentro de esos handlers, dos acciones con tratamiento distinto según su
costo de I/O:

1. **In-app (F6.1)**: `IHubContext<NotificationHub>.Clients.User(userId).SendAsync(...)`
   invocado directamente dentro del handler — un `SendAsync` de SignalR
   sobre una conexión ya abierta es rápido y no bloquea significativamente
   el pipeline de `SaveChangesAsync`.
2. **Correo (F6.2)**: `BackgroundJob.Enqueue<IEmailSender>(x => x.SendAsync(...))`
   (Hangfire) — nunca invocado directamente, exactamente la regla que
   ADR-0008 estableció (I/O externa lenta no debe bloquear la request).

Ambos casos también persisten una fila en `Notifications` (F6.3) dentro de
la misma unidad de trabajo del handler, para que el centro de notificaciones
tenga historial incluso si el usuario no estaba conectado por SignalR en el
momento del evento.

## Alternativas consideradas

- **Introducir MassTransit + RabbitMQ para publicar/consumir estos eventos**:
  rechazada — es exactamente el caso que ADR-0008 ya descartó: todo el
  fan-out ocurre dentro del mismo proceso de la Api (SignalR Hub y Hangfire
  client corren in-process), no hay un segundo servicio consumidor. Un
  message bus resolvería un problema (comunicación entre procesos/servicios
  independientes) que Release 2 no tiene.
- **Un `NotificationDispatcher` central que los `CommandHandler`s llaman
  explícitamente** (p. ej. `await notificationDispatcher.NotifyDocumentApproved(...)`
  dentro de `ApproveDocumentCommandHandler`): rechazada — acopla cada
  handler de negocio a saber qué notificaciones disparar, duplicando esa
  decisión en cada lugar que la necesite. El mecanismo de Domain Events ya
  resuelve exactamente este problema (desacoplar "qué pasó" de "quién
  reacciona") desde Release 1; no reusarlo sería mantener dos mecanismos
  para el mismo propósito.
- **Enviar el correo síncronamente dentro del mismo handler que envía la
  notificación in-app**: rechazada — mezclaría una operación rápida
  (SignalR) con una lenta y falible (SMTP/API de terceros) en el mismo
  `INotificationHandler`; un fallo del proveedor de correo no debe impedir
  que la notificación in-app se entregue, y con Hangfire además se obtienen
  reintentos automáticos sobre el envío de correo sin código adicional.

## Consecuencias

- Positivo: cero infraestructura de mensajería nueva — Redis y Hangfire
  (ya activados por ADR-0008 para otros casos de uso) son la única
  infraestructura que este ADR necesita, ninguna adicional.
- Positivo: un futuro canal de notificación (p. ej. push notifications
  móviles, si el producto lo necesitara) se agrega como un handler más del
  mismo evento, sin tocar los handlers de correo/in-app existentes — abierto
  a extensión, cerrado a modificación (mismo principio SOLID que el resto
  del pipeline de Domain Events ya demuestra).
- Negativo: el fan-out de notificaciones ocurre acoplado al ciclo de vida de
  la request HTTP que originó el evento (aunque el correo se encola async,
  encolarlo sigue siendo parte del mismo `SaveChangesAsync`) — aceptable
  porque encolar un job es una operación local rápida (escribir una fila en
  la tabla de Hangfire), no una llamada de red al proveedor de correo en sí.
