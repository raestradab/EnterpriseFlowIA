# Release 3, Sprint 6 — Base de Datos

Mismo alcance que Sprint 6 de Release 2: configuración de EF Core
(`IEntityTypeConfiguration<T>`), migración, índices — para `DocumentChunk`,
la única entidad que Sprint 5 (Modelo de Dominio) dejó lista y que todavía
no tenía persistencia. `AssistantMessage` ya tenía su configuración desde
Sprint 4 (Validación) — este Sprint aprovecha, igual que Release 2 lo hizo
con Catálogos, para incorporarla también al diagrama ER consolidado
(`06-base-de-datos.md`), que hasta ahora no la tenía documentada ahí.

## Qué se agregó

`DocumentChunkConfiguration` — una `IEntityTypeConfiguration<T>` más, y una
migración (`AddDocumentChunks`, 1 tabla). `Embedding` se mapea a
`varbinary(max)` (confirmado en la migración generada) — el tipo de columna
que corresponde a un `byte[]` serializado, consistente con la decisión de
ADR-0014.

## Decisión de índice

`DocumentChunks (TenantId, DocumentId)` — cubre los dos patrones de
consulta reales: recuperación de RAG (escanear los chunks de *un* tenant
para calcular similitud, HU-101) y limpieza/re-indexación de los chunks de
*un* Documento específico. Mismo criterio que ya justificó el índice
compuesto de `Documents (TenantId, OwnerType, OwnerId)` en Release 2 — un
solo índice cubriendo ambos patrones en vez de dos separados, porque
`TenantId` siempre está presente en ambas consultas.

## Sin FK física, otra vez la misma razón

`AssistantMessages.UserId` y `DocumentChunks.DocumentId` no tienen FK real
— exactamente el mismo trade-off que `Notifications.UserId`/
`Documents.OwnerId` ya aceptaron en Release 2 (ADR-0005/ADR-0009):
agregados independientes, integridad referencial validada en Application.
Ninguna razón nueva que documentar — la misma decisión, aplicada de forma
consistente a las dos entidades nuevas de este Release.

## Verificación

Migración generada y **aplicada contra LocalDB real** en dos pasos: contra
la base de desarrollo existente (incremental, un solo `AddDocumentChunks`)
y contra una base nueva (cadena completa: `InitialCreate` →
`AddCatalogs` → `AddDocumentsWorkflowsNotifications` →
`AddAssistantMessages` → `AddDocumentChunks`), las dos limpias sin
intervención manual. Suite completa sin cambios de comportamiento:
**236/236 tests** (el esquema de pruebas usa SQLite vía `EnsureCreated()`,
que no ejercita las migraciones en sí — la verificación contra LocalDB real
es la que prueba que la migración aplica, no solo que el modelo final es
consistente, mismo razonamiento ya establecido en Sprint 6 de Release 2).
`dotnet format --verify-no-changes` limpio.
