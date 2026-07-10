# Historias de Usuario y Casos de Uso — Release 1 (MVP)

Formato: `Como <rol>, quiero <acción>, para <beneficio>`, con criterios de
aceptación en Gherkin. Cada historia referencia su Feature en
`epics.md`.

---

## E1 — Identidad y Multi-Tenancy

### HU-001 — Registro de Tenant (F1.1)
Como visitante, quiero registrar una nueva organización (tenant), para empezar
a usar EnterpriseFlow AI de forma aislada de otras organizaciones.

```gherkin
Dado que no existe un tenant con el dominio "acme"
Cuando registro el tenant "Acme Corp" con dominio "acme" y un usuario administrador
Entonces se crea el tenant y el usuario administrador queda asociado exclusivamente a "acme"
Y no puede ver datos de ningún otro tenant
```
Regla de negocio: el aislamiento por tenant se aplica a nivel de query filter
global (EF Core), no opcional por endpoint.

### HU-002 — Login con JWT + Refresh Token (F1.2)
Como usuario registrado, quiero iniciar sesión con mis credenciales, para
obtener un token de acceso y un refresh token.

```gherkin
Dado un usuario válido dentro de su tenant
Cuando envío credenciales correctas
Entonces recibo un Access Token (corta duración) y un Refresh Token (larga duración)

Dado un Access Token expirado y un Refresh Token válido
Cuando solicito renovación
Entonces recibo un nuevo Access Token sin volver a autenticar con contraseña

Dado un Refresh Token ya utilizado o revocado
Cuando intento usarlo de nuevo
Entonces la solicitud se rechaza (rotación + detección de reuso)
```

### HU-003 — Gestión de Roles y Permisos (F1.4)
Como administrador de tenant, quiero crear roles con permisos específicos,
para controlar qué puede hacer cada usuario.

```gherkin
Dado un rol "Project Manager" con permiso "projects.write"
Cuando un usuario con ese rol intenta editar un Proyecto
Entonces la operación se permite

Dado un usuario sin el permiso "projects.write"
Cuando intenta editar un Proyecto
Entonces recibe 403 Forbidden
```

### HU-004 — Autorización basada en Policies (F1.5)
Como arquitecto del sistema, quiero que la autorización se exprese como
Policies (no roles hardcodeados en el código), para poder cambiar reglas de
acceso sin recompilar lógica de negocio.
Criterio: ningún controlador/endpoint referencia nombres de rol directamente;
solo referencia nombres de Policy.

### HU-005 — Menú dinámico (F1.6)
Como usuario autenticado, quiero ver solo las opciones de menú para las que
tengo permiso, para no encontrar rutas a las que no tengo acceso.

### HU-006 — Perfil de usuario (F1.7)
Como usuario autenticado, quiero ver y editar mis datos de perfil y cambiar mi
contraseña, para mantener mi cuenta actualizada y segura.

---

## E2 — Organizaciones y Personas

### HU-010 — CRUD de Empresas (F2.1)
Como usuario con permiso `companies.manage`, quiero crear/editar/desactivar
Empresas, para mantener el catálogo de organizaciones con las que trabajamos.
Regla: desactivar es soft delete (no elimina físicamente, preserva auditoría).

### HU-011 — CRUD de Clientes (F2.2)
Como usuario con permiso `clients.manage`, quiero gestionar Clientes asociados
opcionalmente a una Empresa, para llevar el registro comercial.

### HU-012 — CRUD de Contactos (F2.3)
Como usuario, quiero gestionar Contactos que pertenecen siempre a un Cliente
del mismo tenant, para tener a quién dirigirme en cada cuenta.
Regla de negocio (invariante de dominio): un Contacto no puede existir sin un
Cliente válido del mismo tenant; si el Cliente se desactiva, sus Contactos se
marcan como inactivos en cascada lógica (no física).

---

## E3 — Proyectos y Trabajo

### HU-020 — CRUD de Proyectos (F3.1)
Como Project Manager, quiero crear Proyectos asociados a un Cliente, con
fecha de inicio/fin estimada y estado, para planificar el trabajo.

### HU-021 — Cierre de Proyecto con validación (F3.1)
```gherkin
Dado un Proyecto con Tareas en estado distinto de "Completada" o "Cancelada"
Cuando intento marcar el Proyecto como "Cerrado"
Entonces la operación se rechaza indicando cuántas tareas siguen abiertas
```
Esta es la regla de negocio central que distingue al módulo de un CRUD plano.

### HU-022 — Gestión de Equipos (F3.2)
Como Project Manager, quiero asignar usuarios a un Proyecto como miembros del
Equipo con un rol de proyecto (ej. Developer, QA, Lead), para saber quién
trabaja en qué.

### HU-023 — Gestión de Tareas (F3.3)
Como miembro de un Equipo, quiero crear y actualizar Tareas dentro de un
Proyecto (título, descripción, prioridad, estado, asignado, fecha límite),
para hacer seguimiento del trabajo.
Regla: una Tarea solo puede asignarse a un usuario que sea miembro del Equipo
del Proyecto (invariante validada en el dominio, no solo en el frontend).

### HU-024 — Calendario (F3.4)
Como usuario, quiero ver mis Tareas y fechas límite de Proyectos en una vista
de calendario, para planificar mi semana.

---

## E4 — Dashboard Ejecutivo

### HU-030 — Indicadores clave (F4.1)
Como usuario con acceso al Dashboard, quiero ver KPIs de proyectos activos,
tareas vencidas, clientes nuevos y usuarios activos, para tener visibilidad
ejecutiva sin entrar a cada módulo.

### HU-031 — Gráficas de actividad (F4.2)
Como usuario, quiero ver gráficas de tareas completadas por semana y proyectos
por estado, para identificar tendencias.

---

## E7 (base) — Auditoría y Observabilidad

### HU-040 — Registro de auditoría (F7.1)
```gherkin
Dado que un usuario edita una entidad auditable (Proyecto, Cliente, Tarea, ...)
Cuando la operación se confirma
Entonces se registra quién, cuándo, qué campo cambió y el valor anterior/nuevo
```

### HU-041 — Health Checks (F7.3)
Como operador, quiero un endpoint `/health` que reporte el estado de la base
de datos y dependencias críticas, para monitoreo básico.

---

## Fuera de alcance del MVP (recordatorio)
Documentos, Notificaciones, Workflow, Catálogos dinámicos, AI Assistant, RAG y
MCP **no** tienen historias de usuario en este documento — se detallarán al
iniciar su Release correspondiente (ver `02-roadmap.md`), para no diseñar
prematuramente sobre supuestos que puedan cambiar.
