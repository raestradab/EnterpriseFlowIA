import { apiClient } from './client'
import type { Project, ProjectListItem, ProjectRole } from '../types'

export function getProjects() {
  return apiClient.get<ProjectListItem[]>('/projects')
}

export function getProject(id: string) {
  return apiClient.get<Project>(`/projects/${id}`)
}

export function createProject(payload: {
  name: string
  clientId: string
  startDate: string | null
  estimatedEndDate: string | null
}) {
  return apiClient.post<{ id: string }>('/projects', payload)
}

export function closeProject(id: string) {
  return apiClient.post(`/projects/${id}/close`)
}

export function addProjectMember(id: string, payload: { userId: string; role: ProjectRole }) {
  return apiClient.post(`/projects/${id}/members`, payload)
}

export function removeProjectMember(id: string, userId: string) {
  return apiClient.delete(`/projects/${id}/members/${userId}`)
}
