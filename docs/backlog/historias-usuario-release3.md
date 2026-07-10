# Historias de Usuario y Casos de Uso — Release 3

Mismo formato que
[`historias-usuario-release2.md`](./historias-usuario-release2.md):
`Como <rol>, quiero <acción>, para <beneficio>`, con criterios de
aceptación en Gherkin donde hay una regla de negocio real que lo
justifica. Numeración con huecos deliberados por Epic (E9 → 90s, E10 →
100s), igual que en los documentos de Release 1/2. Contexto de alcance
completo, incluyendo la redirección de E9/E10 respecto al plan original,
en [`../r3-01-vision-y-alcance.md`](../r3-01-vision-y-alcance.md).

---

## E9 — AI Assistant (de cara al usuario final, ver nota de redirección en `epics.md`)

### HU-090 — Integración intercambiable con proveedores de IA (F9.1)
Como sistema, quiero poder usar OpenAI o Anthropic (Claude) como motor del
asistente según configuración, sin que ninguna capa por encima de la
abstracción de IA sepa cuál está activo, para poder cambiar de proveedor
o agregar uno nuevo sin tocar Application/Domain.

```gherkin
Dado el proveedor de IA configurado como "OpenAI"
Cuando el asistente responde una pregunta
Entonces el comportamiento observable (forma de la respuesta, reglas de
  anclaje a datos del tenant) es idéntico a si estuviera configurado "Anthropic"
```
Regla de negocio (arquitectónica): `Application` depende de una interfaz
`IAiChatClient`, nunca de un SDK de proveedor específico — mismo patrón
que `IDocumentStorageProvider` ya estableció (ADR-0009, Release 2).

### HU-091 — Chat conversacional con historial (F9.2)
Como usuario autenticado, quiero conversar con un asistente dentro de
EnterpriseFlow y que recuerde el contexto de mi conversación actual, para
no tener que repetir contexto en cada pregunta.

Regla: el historial de una conversación pertenece a un usuario dentro de
su tenant — nadie más, ni siquiera un administrador del mismo tenant, ve
las conversaciones de otro usuario sin un permiso explícito para ello
(ninguna HU de este Release pide ese permiso; por defecto, una
conversación es privada de quien la inició).

### HU-092 — Respuestas ancladas en datos reales del tenant (F9.3)
Como usuario, quiero que el asistente responda preguntas sobre mis
Proyectos/Tareas/Clientes/Documentos con datos reales y actuales, no con
una respuesta genérica o inventada, para poder confiar en lo que responde.

```gherkin
Dado que tengo 3 Tareas con estado "Vencida" asignadas a mí
Cuando le pregunto al asistente "¿cuántas tareas tengo atrasadas?"
Entonces la respuesta refleja el número real (3), resuelto consultando
  GetMyCalendar/GetTasks — la misma Query que ya usa el Dashboard, no un
  cálculo distinto hecho por el modelo
Y si intento (via el mismo chat) pedirle datos de otro tenant
Entonces el asistente no tiene forma de acceder a ellos — las Queries que
  invoca ya filtran por tenant (ADR-0003) antes de que el modelo vea el resultado
```
Regla de seguridad (la central de este Epic): el modelo de IA nunca recibe
un acceso más amplio que el que el propio usuario ya tiene — cada
"herramienta" que el asistente puede invocar es una Query de Application
existente, con el mismo `AuthorizationBehavior`/filtro de tenant que
cualquier otro caller. El asistente no es una puerta trasera.

### HU-093 — Resúmenes y reportes ejecutivos generados por IA (F9.4)
Como usuario con acceso al Dashboard, quiero pedirle al asistente un
resumen en lenguaje natural del estado de mis Proyectos (p. ej. "resume el
estado de mis proyectos activos"), para obtener una síntesis sin tener que
armar el reporte manualmente.
Regla: el resumen se genera a partir de datos resueltos por Queries reales
(mismas que HU-092), nunca de texto libre sin verificar contra el sistema.

---

## E10 — RAG sobre Documentos del Tenant (redirigido, ver nota en `epics.md`)

### HU-100 — Indexación de Documentos del tenant (F10.1, F10.2)
Como sistema, quiero indexar automáticamente el contenido de los
Documentos que un tenant sube (F5, Release 2) — texto plano, PDF, Word —
para que el asistente pueda responder preguntas ancladas en su contenido.

```gherkin
Dado que subo un contrato en PDF a un Proyecto
Cuando la subida se completa
Entonces el contenido de texto del documento queda indexado, asociado al
  mismo TenantId del Documento

Dado un PDF que es solo una imagen escaneada sin capa de texto
Cuando se sube
Entonces el documento se guarda igual (F5 no cambia) pero no queda indexado
  para RAG — sin OCR en este Release, ver r3-01-vision-y-alcance.md sección 4
```
Regla: la indexación reutiliza el mismo storage/validación de F5 — no es
un segundo sistema de subida de archivos en paralelo.

### HU-101 — Respuestas ancladas en el corpus indexado del tenant (F10.3)
Como usuario, quiero preguntarle al asistente sobre el contenido de
documentos que ya subí (p. ej. "¿qué dice el contrato que subí la semana
pasada sobre el plazo de entrega?"), para no tener que abrirlo y buscar
manualmente.
Regla de aislamiento (la misma de HU-092, aplicada al corpus de RAG en vez
de a las Queries de negocio): la búsqueda de similitud solo considera
documentos indexados del tenant del usuario que pregunta — nunca de otro
tenant, aunque el modelo de embeddings sea compartido entre todos.

---

## Fuera de alcance de Release 3 (recordatorio)

Generación de historias de usuario/SQL/DTOs/Entidades/tests, detección de
code smells, servidor MCP propio — no tienen historias de usuario en este
documento, diferidas sin Release asignado (ver `epics.md`, notas de
redirección en E9/E11, y `r3-01-vision-y-alcance.md` sección 4).
