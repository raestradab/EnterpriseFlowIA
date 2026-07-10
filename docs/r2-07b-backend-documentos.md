# Release 2, Sprint 7b — Backend: Documentos (F5)

Segunda sub-parte de Sprint 7 (Backend) de Release 2, sobre el Workflow que
7a ya dejó construido y probado.

## Qué se implementó

**Infrastructure** (`Storage/`): los 4 proveedores comprometidos en
`r2-01-vision-y-alcance.md` — `LocalStorageProvider` (sistema de archivos
real), `AzureBlobStorageProvider` (Azure.Storage.Blobs), `AmazonS3StorageProvider`
(AWSSDK.S3), `GoogleCloudStorageProvider` (Google.Cloud.Storage.V1) — los 4
con SDKs reales, no stubs. Selección por configuración
(`Documents:Provider`) en `Infrastructure.DependencyInjection`: exactamente
uno se registra por instancia (ADR-0009); el resto del sistema nunca sabe
cuál está activo.

**Application** (`Common/FileSignatureValidator.cs`, `Features/Documents/`):
`UploadDocument`, `GetDocumentById`, `GetDocuments`, `DownloadDocument`,
`TransitionDocument`, `DeleteDocument`. `Permissions.Documents.{Read,Manage,Approve}`
— `Approve` es un permiso propio, distinto de `Manage` (HU-081: quien
aprueba un documento no necesariamente administra el ciclo de vida de
Documentos en general, y viceversa).

**Api**: `DocumentsEndpoints` (`/api/documents`, subida multipart real vía
`HttpRequest.ReadFormAsync`, descarga vía `Results.Stream`).

## Decisiones de este sprint

- **`DocumentValidationOptions` duplica dos campos de `DocumentsOptions`
  (Infrastructure) en Application.** No es descuido: Application no puede
  referenciar tipos de Infrastructure (ADR-0002), pero `UploadDocumentValidator`
  necesita `MaxSizeBytes`/`AllowedExtensions` para HU-051. Ambas clases se
  bindean desde la misma sección `Documents` de configuración —
  `Infrastructure.Storage.DocumentsOptions` retiene solo `Provider` (lo único
  que de verdad es una decisión de infraestructura).
- **`WorkflowDefinitionId` es explícito en `UploadDocumentCommand`**, no
  implícito. F8.1 es genérico — un tenant podría tener más de un Workflow —
  así que quién sube el documento decide cuál rige, en vez de que el sistema
  asuma "el único que existe".
- **HU-051 se verifica por firma binaria real, no por extensión ni
  `Content-Type` declarado** (`FileSignatureValidator`, ambos controlados
  por quien sube el archivo). La prueba central
  (`Upload_With_Extension_Mismatched_Content_Returns_BadRequest`) sube un
  header real de ejecutable de Windows (`4D 5A`, "MZ") renombrado
  `invoice.pdf` — exactamente el escenario que la validación por extensión
  sola no detecta.
- **Borrar un Documento borra también el archivo físico**, no solo la fila
  (soft-deleted, conserva auditoría). Ningún caso de uso pide conservar el
  blob huérfano indefinidamente, y la fila ya retiene lo que vale la pena
  auditar (nombre, propietario, fechas).

## Qué se verificó de verdad — y qué no

- **Subida → descarga real contra `LocalStorageProvider`**: la prueba
  `Upload_Then_Download_Returns_The_Exact_Bytes_Uploaded` escribe a disco de
  verdad (bajo el directorio temporal del SO, no `App_Data` del repo —
  ver `CustomWebApplicationFactory`) y confirma que los bytes descargados
  son *idénticos, byte a byte*, a los subidos — no solo que la fila
  `Documents` se creó.
- **Borrado real de archivo**: `Delete_Removes_The_Document_And_Its_Underlying_File`
  confirma que, tras el DELETE, tanto la metadata como el contenido dejan de
  servirse (404) — ejercita `LocalStorageProvider.DeleteAsync` contra el
  archivo real que la prueba anterior había escrito.
- **`AzureBlobStorageProvider`/`AmazonS3StorageProvider`/`GoogleCloudStorageProvider`
  no se verificaron en ejecución** — mismo tipo de limitación ya declarada
  para Redis en Sprint 3: este entorno no tiene Docker (para LocalStack/
  emulador de GCS) ni credenciales de una cuenta real de Azure/AWS/GCP.
  Se evaluó levantar Azurite (el emulador de Azure Blob Storage, distribuible
  vía `npm` sin Docker) pero se priorizó terminar Documentos completo dentro
  del alcance de este sprint; queda como candidato concreto para una
  verificación posterior si se decide invertir el tiempo. El código de los
  3 proveedores compila, seguiría exactamente el contrato de
  `IDocumentStorageProvider` que `LocalStorageProvider` ya prueba
  cumplir en ejecución, y usa las credenciales estándar de cada SDK
  (cadena de conexión / ADC / cadena de proveedores de AWS) — nunca
  hardcodeadas.
- **No se hizo una corrida manual contra Kestrel real** (solo
  `WebApplicationFactory`/`TestServer`): a diferencia del caso de Hangfire
  en Sprint 3 (donde el riesgo real era una conexión eager a una base
  inexistente, algo que un test in-process no dispara), `TestServer` ejecuta
  el pipeline HTTP real de ASP.NET Core — incluida la negociación
  multipart/form-data — así que no hay una clase de bug plausible que solo
  aparecería contra un Kestrel real aquí.

## Verificación

**Suite completa: 203/203 tests** (122+20+6+55 — 9 pruebas de integración
nuevas para `DocumentsEndpoints`, 8 unitarias nuevas para
`FileSignatureValidator`). `dotnet format --verify-no-changes` limpio.
