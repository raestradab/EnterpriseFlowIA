import { apiClient } from './client'
import type { LoginResult, MyPermissions, RegisterTenantResult } from '../types'

export function login(email: string, password: string) {
  return apiClient.post<LoginResult>('/auth/login', { email, password })
}

export function registerTenant(payload: {
  tenantName: string
  tenantSlug: string
  adminEmail: string
  adminPassword: string
}) {
  return apiClient.post<RegisterTenantResult>('/auth/register-tenant', payload)
}

export function getMyPermissions() {
  return apiClient.get<MyPermissions>('/auth/me')
}

export function logout() {
  return apiClient.post('/auth/logout')
}
