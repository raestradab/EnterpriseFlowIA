import { apiClient } from './client'
import type { AssistantMessageItem } from '../types'

export function getAssistantMessages() {
  return apiClient.get<AssistantMessageItem[]>('/assistant/messages')
}

export function sendAssistantMessage(message: string) {
  return apiClient.post<{ reply: string }>('/assistant/messages', { message })
}
