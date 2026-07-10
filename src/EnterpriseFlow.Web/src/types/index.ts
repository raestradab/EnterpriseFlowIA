// Mirrors the Application-layer DTOs (EnterpriseFlow.Application/Features/**). Kept as plain
// interfaces, hand-written rather than generated — the backend is stable enough at this point
// that generating a client (NSwag/OpenAPI) is worth doing once the API surface stops growing
// as fast (Release 2), not before.

// Plain `as const` objects, not TS `enum` — the scaffolded tsconfig enables
// `erasableSyntaxOnly` (Vite/TS's newer default steering away from non-erasable TS-only
// runtime constructs like `enum`), and this pattern gives the same "namespace + type" usage
// (ProjectStatus.Active, and ProjectStatus as a type) while emitting to plain JS. Numeric
// values matter: they must match the backend enums' underlying int values exactly, since
// System.Text.Json serializes them as numbers by default (no string converter configured).
export const ProjectStatus = {
  Planned: 0,
  Active: 1,
  OnHold: 2,
  Closed: 3,
  Cancelled: 4,
} as const
export type ProjectStatus = (typeof ProjectStatus)[keyof typeof ProjectStatus]

export const ProjectTaskStatus = {
  Todo: 0,
  InProgress: 1,
  Completed: 2,
  Cancelled: 3,
} as const
export type ProjectTaskStatus = (typeof ProjectTaskStatus)[keyof typeof ProjectTaskStatus]

export const TaskPriority = {
  Low: 0,
  Medium: 1,
  High: 2,
  Critical: 3,
} as const
export type TaskPriority = (typeof TaskPriority)[keyof typeof TaskPriority]

export const ProjectRole = {
  Developer: 0,
  QaEngineer: 1,
  Lead: 2,
  ProjectManager: 3,
} as const
export type ProjectRole = (typeof ProjectRole)[keyof typeof ProjectRole]

export interface LoginResult {
  accessToken: string
  accessTokenExpiresAtUtc: string
}

export interface RegisterTenantResult {
  tenantId: string
  adminUserId: string
}

export interface MyPermissions {
  userId: string
  tenantId: string
  permissions: string[]
}

export interface Company {
  id: string
  name: string
  taxId: string | null
}

export interface Client {
  id: string
  name: string
  companyId: string | null
}

export interface Contact {
  id: string
  name: string
  email: string | null
  phone: string | null
  clientId: string
}

export interface ProjectMember {
  userId: string
  role: ProjectRole
}

export interface Project {
  id: string
  name: string
  clientId: string
  startDate: string | null
  estimatedEndDate: string | null
  status: ProjectStatus
  members: ProjectMember[]
}

export interface ProjectListItem {
  id: string
  name: string
  clientId: string
  status: ProjectStatus
}

export interface TaskItem {
  id: string
  title: string
  description: string | null
  priority: TaskPriority
  status: ProjectTaskStatus
  projectId: string
  assignedToUserId: string | null
  dueDate: string | null
}

export interface ProblemDetails {
  title?: string
  status?: number
  errors?: Record<string, string[]>
}

// Release 2 — F8.2 (Catálogos)
export interface CatalogListItem {
  id: string
  name: string
  itemCount: number
}

export interface CatalogItem {
  id: string
  key: string
  label: string
}

// Release 2 — F8.1 (Workflow genérico, ADR-0010)
export interface WorkflowListItem {
  id: string
  name: string
  stateCount: number
  transitionCount: number
}

export interface WorkflowState {
  id: string
  name: string
  isInitial: boolean
  isFinal: boolean
}

export interface WorkflowTransition {
  id: string
  name: string
  fromStateId: string
  toStateId: string
}

export interface Workflow {
  id: string
  name: string
  states: WorkflowState[]
  transitions: WorkflowTransition[]
}

// Release 2 — F5 (Documentos, ADR-0009). Named *Item, not `Document` — that identifier is
// already the global DOM interface, and TypeScript would silently shadow it.
export const DocumentOwnerType = {
  Project: 0,
  Client: 1,
  Task: 2,
} as const
export type DocumentOwnerType = (typeof DocumentOwnerType)[keyof typeof DocumentOwnerType]

export interface DocumentListItem {
  id: string
  fileName: string
  sizeBytes: number
  currentWorkflowStateId: string
  currentWorkflowStateName: string
  workflowDefinitionId: string
}

export interface DocumentItem {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
  ownerType: DocumentOwnerType
  ownerId: string
  currentWorkflowStateId: string
  currentWorkflowStateName: string
  workflowDefinitionId: string
}

// Release 2 — F6 (Notificaciones, ADR-0011). Named *Item, not `Notification` — that identifier
// is already the browser's global Notifications API interface.
export interface NotificationItem {
  id: string
  eventName: string
  message: string
  isRead: boolean
  createdAtUtc: string
}

// Release 3 — F9 (Asistente de IA, ADR-0013)
export const AssistantMessageRole = {
  User: 0,
  Assistant: 1,
} as const
export type AssistantMessageRole = (typeof AssistantMessageRole)[keyof typeof AssistantMessageRole]

export interface AssistantMessageItem {
  id: string
  role: AssistantMessageRole
  content: string
  createdAtUtc: string
}
