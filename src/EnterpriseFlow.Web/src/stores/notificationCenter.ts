import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import * as notificationsApi from '../api/notifications'
import type { NotificationItem } from '../types'

// Named *Center*, not `notifications` — that store name is already taken by the toast queue
// (stores/notifications.ts), a Release 1 concept unrelated to F6's persisted notification
// center (HU-062). Found and flagged during Sprint 8a; kept in mind here to not collide.
export const useNotificationCenterStore = defineStore('notificationCenter', () => {
  const items = ref<NotificationItem[]>([])

  const unreadCount = computed(() => items.value.filter((n) => !n.isRead).length)

  async function load() {
    const response = await notificationsApi.getMyNotifications()
    items.value = response.data
  }

  async function markRead(id: string) {
    await notificationsApi.markNotificationRead(id)
    const item = items.value.find((n) => n.id === id)
    if (item) {
      item.isRead = true
    }
  }

  return { items, unreadCount, load, markRead }
})
