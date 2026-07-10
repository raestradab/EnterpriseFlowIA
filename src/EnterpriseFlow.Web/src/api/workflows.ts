import { apiClient } from './client'
import type { Workflow, WorkflowListItem } from '../types'

export function getWorkflows() {
  return apiClient.get<WorkflowListItem[]>('/workflows')
}

export function getWorkflow(id: string) {
  return apiClient.get<Workflow>(`/workflows/${id}`)
}

export function createWorkflow(payload: { name: string }) {
  return apiClient.post<{ id: string }>('/workflows', payload)
}

export function addWorkflowState(workflowId: string, payload: { name: string; isInitial: boolean; isFinal: boolean }) {
  return apiClient.post<{ id: string }>(`/workflows/${workflowId}/states`, payload)
}

export function addWorkflowTransition(
  workflowId: string,
  payload: { name: string; fromStateId: string; toStateId: string },
) {
  return apiClient.post<{ id: string }>(`/workflows/${workflowId}/transitions`, payload)
}
