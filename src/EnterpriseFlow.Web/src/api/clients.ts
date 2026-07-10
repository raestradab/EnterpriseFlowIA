import { apiClient } from './client'
import type { Client } from '../types'

export function getClients() {
  return apiClient.get<Client[]>('/clients')
}

export function createClient(payload: { name: string; companyId: string | null }) {
  return apiClient.post<{ id: string }>('/clients', payload)
}

export function deactivateClient(id: string) {
  return apiClient.post(`/clients/${id}/deactivate`)
}
