# Release 2 — Colaboración y Operación: Visión y Alcance

Sprint 1 (Análisis) de Release 2, mismo ciclo `análisis → ... → DevOps` que
Release 1 aplicó (ADR-0001, "se interpreta como el ciclo interno de cada
Release, no como un único paso global"). Este documento juega el mismo rol
que [`01-vision-y-alcance.md`](./01-vision-y-alcance.md) jugó para Release 1.

## 1. Qué agrega Release 2 sobre el MVP

Release 1 demostró el sistema núcleo (identidad multi-tenant, entidades de
negocio, dashboard básico) con arquitectura y calidad completas, pero
deliberadamente sin: manejo de archivos, comunicación con el usuario fuera
de la UI, procesos configurables por el tenant, y datos de referencia
reutilizables. Release 2 cierra esas cuatro brechas — son las que un SaaS de
gestión de proyectos real necesita para dejar de ser "un CRUD con login" y
convertirse en una herramienta de trabajo colaborativa, que es exactamente
el nombre que `02-roadmap.md` ya le da a este Release.

## 2. Alcance de Release 2

Mapea a los Epics E4 (parcial)/E5/E6/E7 (parcial)/E8 de
[`backlog/epics.md`](./backlog/epics.md), detallados con Historias de
Usuario en [`backlog/historias-usuario-release2.md`](./backlog/historias-usuario-release2.md):

- **Documentos (E5)**: subir/descargar/eliminar archivos asociados a
  Proyectos/Clientes/Tareas, con validación de tipo/tamaño, a través de una
  abstracción de storage con cuatro proveedores reales (Local, Azure Blob,
  Amazon S3, Google Cloud Storage) intercambiables por configuración.
- **Notificaciones (E6)**: in-app en tiempo real (SignalR) y por correo
  (background job vía Hangfire, ver ADR-0008), con un centro de
  notificaciones que persiste historial y estado leído/no leído.
- **Workflow (E8.1)**: motor de estados/transiciones genérico y
  tenant-configurable, con un primer consumidor real y concreto — la
  aprobación de Documentos (Borrador → En Revisión → Aprobado/Rechazado) —
  en vez de un motor especulativo sin ningún caso de uso conectado.
- **Catálogos (E8.2)**: listas maestras reutilizables (tenant-configurables),
  cacheadas en Redis (ADR-0008).
- **Configuración (E8.3)**: parámetros de sistema/tenant editables sin
  redeploy.
- **Dashboard avanzado (E4.3/E4.4)**: reportes exportables (CSV/Excel) y
  mapa/alertas.
- **Observabilidad ampliada (E7.4/E7.8)**: vista de Logs en la UI y health
  checks extendidos a las nuevas dependencias externas que este mismo
  Release introduce (Redis, storage de Hangfire, proveedor de Documentos
  activo).
- **Infraestructura activada por caso de uso real** (no especulativa —
  ADR-0008): Redis, Hangfire, Response Compression.

## 3. Decisiones de alcance dentro de Release 2 (para no sobre-construir)

Mismo criterio que Release 1: cada feature se acota a lo que un caso de uso
concreto pide, no a la versión más general imaginable.

- **Los 4 proveedores de Documentos se implementan de verdad** (SDKs reales
  de Azure/AWS/GCP, no stubs) — es el propio requisito explícito de
  `especificcion.md` ("Crear proveedores intercambiables"), y demostrar la
  intercambiabilidad *sin* implementaciones reales sería simular el
  requisito, no cumplirlo. Las pruebas de integración corren contra
  emuladores (Azurite para Blob Storage, un mock S3-compatible, un emulador
  de GCS), no contra cuentas cloud reales — así CI no depende de
  credenciales ni de infraestructura de terceros. Detalle de qué emulador
  exacto se usa cada uno, en el Sprint de Backend de Release 2.
- **El motor de Workflow es genérico pero tiene un solo consumidor real en
  este Release** (aprobación de Documentos). No se construyen workflows para
  Proyectos/Tareas especulativamente — si un caso de uso real lo pide en un
  Release posterior, el motor ya es genérico y solo hace falta una nueva
  definición de estados, no una reescritura.
- **Notificaciones in-app (SignalR) se limitan a: nueva notificación
  recibida, documento aprobado/rechazado, tarea asignada** — no se
  retro-instrumenta cada mutación del sistema para emitir un evento
  SignalR; se conecta a los eventos que Release 2 ya está creando
  (aprobación de documentos, F6) más uno de Release 1 con valor real
  (asignación de tarea, ya existe como Domain Event).
- **Catálogos son genéricos en su modelo de datos** (nombre, tenant,
  colección de ítems clave/valor) **pero Release 2 solo migra un catálogo
  real a este mecanismo**: prioridad/estado de Tarea seguirán siendo enums
  de C# (ya funcionan, cambiarlos no tiene beneficio); el catálogo real que
  Release 2 introduce es uno que Release 1 no tenía — p. ej. "Categorías de
  Documento" (usado por F5, para no inventar un catálogo de ejemplo sin
  consumidor real). Detalle final en el Sprint de Modelo de Dominio.
- **Reportes exportables (F4.3) exportan a CSV y Excel**, no PDF — PDF
  requiere una librería de generación de documentos adicional
  (QuestPDF/similar) que ningún otro caso de uso de Release 2 necesita;
  se difiere hasta que exista una razón real (p. ej. reportes con
  formato/branding específico), mismo criterio que ADR-0001 aplicó a Redis
  en su momento.
- **Mapa (F4.4)** muestra la ubicación de Clientes/Empresas si tienen
  dirección — requiere agregar un campo de dirección/geocoding a esas
  entidades, una migración de base de datos real, no una feature aislada.

## 4. Explícitamente fuera de Release 2

Quedan en el backlog con su Release ya asignado en `epics.md` — no se
adelantan ni se construyen parcialmente aquí:

- AI Assistant, RAG, servidor MCP propio (Release 3).
- MassTransit/RabbitMQ, OpenTelemetry, Elastic Search, Application Insights,
  Temporal Tables, BenchmarkDotNet, cobertura ≥90% agregada (Release 4) —
  ninguno tiene un caso de uso real en Release 2 (ver ADR-0008, sección
  "Explícitamente no se activan").
- Escaneo antivirus real de archivos subidos (F5.7 incluye validación de
  tipo/tamaño/extensión; escaneo de contenido malicioso real requeriría un
  servicio externo — p. ej. ClamAV o un servicio cloud — que ningún otro
  caso de uso de Release 2 necesita todavía).

## 5. Criterios de éxito de Release 2

1. Un usuario puede subir un Documento a un Proyecto, verlo pasar por el
   flujo de aprobación (Workflow), y recibir una notificación in-app y por
   correo cuando se aprueba/rechaza.
2. El proveedor de almacenamiento activo se cambia editando configuración
   (`appsettings`/variables de entorno), sin recompilar ni tocar código de
   Application/Domain — verificado con un test de integración que corre la
   misma suite de casos contra los 4 proveedores.
3. `docker-compose up` levanta Redis y el worker de Hangfire junto con el
   resto del stack, con health checks que reportan el estado de las 4
   dependencias externas del sistema (SQL Server, Redis, storage de
   Hangfire, proveedor de Documentos activo).
4. Cada pieza de infraestructura nueva (Redis, Hangfire, Response
   Compression) tiene su ADR (0008) y un caso de uso real localizable en el
   código — no aparece "porque la lista la pedía".
5. Mismo estándar de Release 1: cobertura de pruebas medida y reportada en
   CI, documentación (README/ER/ADRs/Mermaid) actualizada al cierre del
   Release, `docker-compose up` sin pasos manuales adicionales.
