import { apiClient } from './client'
import type { Company } from '../types'

export function getCompanies() {
  return apiClient.get<Company[]>('/companies')
}

export function createCompany(payload: { name: string; taxId: string | null }) {
  return apiClient.post<{ id: string }>('/companies', payload)
}
