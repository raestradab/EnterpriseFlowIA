import { apiClient } from './client'
import type { DocumentItem, DocumentListItem, DocumentOwnerType } from '../types'

export function getDocuments(ownerType: DocumentOwnerType, ownerId: string) {
  return apiClient.get<DocumentListItem[]>('/documents', { params: { ownerType, ownerId } })
}

export function getDocument(id: string) {
  return apiClient.get<DocumentItem>(`/documents/${id}`)
}

export function uploadDocument(payload: {
  file: File
  ownerType: DocumentOwnerType
  ownerId: string
  workflowDefinitionId: string
}) {
  const form = new FormData()
  form.append('file', payload.file)
  form.append('ownerType', String(payload.ownerType))
  form.append('ownerId', payload.ownerId)
  form.append('workflowDefinitionId', payload.workflowDefinitionId)
  return apiClient.post<{ id: string }>('/documents', form)
}

// responseType 'blob': the caller (a view) turns this into a browser download — kept out of
// this module so api/*.ts stays plain HTTP calls, no DOM manipulation.
export function downloadDocumentContent(id: string) {
  return apiClient.get(`/documents/${id}/content`, { responseType: 'blob' })
}

export function transitionDocument(id: string, targetStateId: string) {
  return apiClient.post(`/documents/${id}/transition`, { targetStateId })
}

export function deleteDocument(id: string) {
  return apiClient.delete(`/documents/${id}`)
}
