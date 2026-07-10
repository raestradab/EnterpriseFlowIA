import { apiClient } from './client'
import type { TaskItem, TaskPriority } from '../types'

export function getTasks(projectId?: string) {
  return apiClient.get<TaskItem[]>('/tasks', { params: projectId ? { projectId } : undefined })
}

export function createTask(payload: {
  title: string
  description: string | null
  priority: TaskPriority
  projectId: string
  dueDate: string | null
}) {
  return apiClient.post<{ id: string }>('/tasks', payload)
}

export function assignTask(id: string, userId: string) {
  return apiClient.post(`/tasks/${id}/assign`, { userId })
}

export function completeTask(id: string) {
  return apiClient.post(`/tasks/${id}/complete`)
}

export function cancelTask(id: string) {
  return apiClient.post(`/tasks/${id}/cancel`)
}
