import { apiClient } from './client'
import type { NotificationItem } from '../types'

export function getMyNotifications() {
  return apiClient.get<NotificationItem[]>('/notifications')
}

export function markNotificationRead(id: string) {
  return apiClient.post(`/notifications/${id}/read`)
}
