import { apiClient } from './client'
import type { CatalogItem, CatalogListItem } from '../types'

export function getCatalogs() {
  return apiClient.get<CatalogListItem[]>('/catalogs')
}

export function createCatalog(payload: { name: string }) {
  return apiClient.post<{ id: string }>('/catalogs', payload)
}

export function getCatalogItems(catalogId: string) {
  return apiClient.get<CatalogItem[]>(`/catalogs/${catalogId}/items`)
}

export function addCatalogItem(catalogId: string, payload: { key: string; label: string }) {
  return apiClient.post(`/catalogs/${catalogId}/items`, payload)
}

export function updateCatalogItem(catalogId: string, itemId: string, payload: { label: string }) {
  return apiClient.put(`/catalogs/${catalogId}/items/${itemId}`, payload)
}

export function removeCatalogItem(catalogId: string, itemId: string) {
  return apiClient.delete(`/catalogs/${catalogId}/items/${itemId}`)
}
