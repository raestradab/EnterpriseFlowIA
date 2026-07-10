import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useNotificationsStore = defineStore('notifications', () => {
  const message = ref('')
  const color = ref<'success' | 'error'>('success')
  const visible = ref(false)

  function show(text: string, type: 'success' | 'error' = 'success') {
    message.value = text
    color.value = type
    visible.value = true
  }

  return { message, color, visible, show }
})
