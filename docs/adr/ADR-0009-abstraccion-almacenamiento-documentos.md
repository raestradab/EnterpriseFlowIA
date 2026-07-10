# ADR-0009: Abstracción de almacenamiento de Documentos y asociación polimórfica al propietario

- Estado: Aceptado
- Fecha: 2026-07-08
- Relacionado: ADR-0005 (mismo patrón de referencia cross-aggregate sin FK
  física), ADR-0008 (activación de infraestructura por caso de uso real)

## Contexto

F5 (Documentos, HU-050) exige que un archivo pueda subirse contra cuatro
proveedores de storage intercambiables (Local, Azure Blob, S3, GCS) sin que
`Application`/`Domain` sepan cuál está activo, y que un Documento pueda
asociarse a Proyectos, Clientes o Tareas — tres tipos de entidad distintos,
en tres agregados distintos (`Project`, `Client`, `ProjectTask`).

## Decisión

**Interfaz de storage**: `IDocumentStorageProvider` en `Application.Abstractions`
(mismo namespace/patrón que `ITokenService`/`IPasswordHasher` de Identidad),
con tres operaciones: `UploadAsync`, `DownloadAsync`, `DeleteAsync`, todas
recibiendo/devolviendo un `Stream` y una clave de storage opaca (`string`)
que `Application` trata como un identificador sin estructura interpretable
— **nunca** una ruta de archivo local ni una URL de blob específica de un
proveedor, para no filtrar detalles de un proveedor a través del contrato.

**Selección de proveedor**: una sola implementación activa por instancia en
ejecución, resuelta en `Infrastructure.DependencyInjection` según
`Documents:Provider` (`appsettings`/variable de entorno) — no un patrón de
fábrica ni servicios *keyed* que permitan cambiar de proveedor en caliente
sin reiniciar. "Selección por configuración" (F5.6) significa "sin
recompilar", no "sin reiniciar el proceso" — ninguna HU pide *hot-swap* de
proveedor, y resolverlo así habría sido complejidad sin requisito real.

**Asociación Documento→propietario**: referencia polimórfica
(`OwnerType` enum + `OwnerId Guid` en la entidad `Document`), **sin FK física**
— exactamente el mismo patrón que ADR-0005 ya estableció para
`ProjectMember.UserId`/`ProjectTask.AssignedToUserId`: agregados
independientes, la integridad referencial se valida en el `CommandHandler`
correspondiente (el propietario debe existir y pertenecer al tenant actual
antes de crear el Documento), no a nivel de base de datos.

## Alternativas consideradas

- **Tabla de unión tipada por entidad** (`ProjectDocument`, `ClientDocument`,
  `TaskDocument`, cada una con FK real a su propietario): rechazada — tres
  tablas casi idénticas para la misma operación conceptual ("adjuntar un
  documento a X") es la duplicación que la regla "no generar código
  duplicado" prohíbe explícitamente; además, cada nuevo tipo de entidad que
  en el futuro necesite Documentos (p. ej. Contactos) requeriría una tabla y
  un `CommandHandler` nuevos en vez de reusar los existentes.
- **FK física con una tabla `Owners` genérica que unifique Project/Client/Task
  bajo un solo padre común**: rechazada — introduciría una jerarquía de
  herencia de tabla (`Table-Per-Hierarchy`/`Table-Per-Type`) solo para
  satisfacer una FK, cuando `Project`/`Client`/`ProjectTask` son y deben
  seguir siendo agregados independientes sin relación de herencia real
  entre sí (violaría ADR-0002/ADR-0005 sin necesidad).
- **Servicios *keyed* de .NET 8 (`AddKeyedSingleton`) para permitir elegir el
  proveedor en tiempo de ejecución por request/tenant**: rechazada para
  Release 2 — ninguna HU pide que distintos tenants usen distintos
  proveedores simultáneamente; toda la plataforma corre sobre un único
  proveedor activo a la vez. Si esa necesidad aparece en un Release futuro
  (multi-tenant con storage por tenant), se revisita con ese caso de uso
  real, no antes.
- **Devolver la clave de storage como la URL/ruta real del proveedor activo**:
  rechazada — filtraría el proveedor concreto al resto del sistema
  (`Application`, el frontend) rompiendo la intercambiabilidad que es el
  propio requisito de F5; la Api sirve la descarga a través de su propio
  endpoint (`GET /api/documents/{id}/content`), nunca redirigiendo al cliente
  a una URL de Azure/S3 directamente.

## Consecuencias

- Positivo: agregar un quinto tipo de propietario (p. ej. Contactos, en un
  Release futuro) no requiere una migración ni un `CommandHandler` nuevo —
  solo un nuevo valor de `OwnerType` y la validación de existencia
  correspondiente.
- Positivo: los 4 proveedores se prueban con la misma suite de tests de
  integración parametrizada por proveedor (HU-050, criterio de éxito #2 de
  `r2-01-vision-y-alcance.md`) — la interfaz común lo hace posible sin
  duplicar casos de prueba.
- Negativo: sin FK física, un propietario borrado físicamente (no debería
  ocurrir — todo el sistema usa soft delete, ver `06-base-de-datos.md`)
  podría dejar Documentos huérfanos sin que la base de datos lo impida.
  Aceptado — es el mismo trade-off que ADR-0005 ya aceptó para
  `ProjectMember`/`ProjectTask`, y el soft delete generalizado del sistema
  hace que un borrado físico real sea un caso que no ocurre en operación
  normal.
- Seguimiento: la elección final entre las 4 SDKs concretas (cliente de
  Azure.Storage.Blobs, AWSSDK.S3, Google.Cloud.Storage) y los emuladores
  usados para probarlas se documenta en el Sprint de Backend de Release 2,
  no aquí — este ADR fija el contrato, no la implementación de cada
  proveedor.
