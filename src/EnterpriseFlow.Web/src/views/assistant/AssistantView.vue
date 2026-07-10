<script setup lang="ts">
import { nextTick, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import * as assistantApi from '../../api/assistant'
import { useNotificationsStore } from '../../stores/notifications'
import { extractErrorMessage } from '../../api/errors'
import { AssistantMessageRole, type AssistantMessageItem } from '../../types'

const { t } = useI18n()
const notifications = useNotificationsStore()

const messages = ref<AssistantMessageItem[]>([])
const loading = ref(true)
const sending = ref(false)
const draft = ref('')
const messagesContainer = ref<HTMLElement | null>(null)

async function scrollToBottom() {
  await nextTick()
  if (messagesContainer.value) {
    messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
  }
}

async function load() {
  loading.value = true
  try {
    const response = await assistantApi.getAssistantMessages()
    messages.value = response.data
    await scrollToBottom()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  } finally {
    loading.value = false
  }
}

async function send() {
  const content = draft.value.trim()
  if (!content || sending.value) return

  sending.value = true
  draft.value = ''
  // Optimistic: no streaming (ADR-0013), so the round-trip — including the assistant's own
  // reply — can take a few seconds. Showing the user's own message immediately avoids a blank
  // screen while waiting; load() below replaces it with the real persisted history.
  messages.value.push({ id: 'pending', role: AssistantMessageRole.User, content, createdAtUtc: new Date().toISOString() })
  await scrollToBottom()

  try {
    await assistantApi.sendAssistantMessage(content)
    await load()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
    messages.value = messages.value.filter((m) => m.id !== 'pending')
  } finally {
    sending.value = false
  }
}

function handleEnter(event: KeyboardEvent) {
  if (!event.shiftKey) {
    event.preventDefault()
    send()
  }
}

onMounted(load)
</script>

<template>
  <div class="d-flex flex-column" style="height: calc(100vh - 112px)">
    <h1 class="text-h4 mb-4">{{ t('assistant.title') }}</h1>

    <v-skeleton-loader v-if="loading" type="paragraph, paragraph" />

    <template v-else>
      <div ref="messagesContainer" class="flex-grow-1 overflow-y-auto mb-4 pa-2">
        <p v-if="messages.length === 0" class="text-medium-emphasis">{{ t('assistant.empty') }}</p>

        <div
          v-for="message in messages"
          :key="message.id"
          class="d-flex mb-3"
          :class="message.role === AssistantMessageRole.User ? 'justify-end' : 'justify-start'"
        >
          <v-card
            :color="message.role === AssistantMessageRole.User ? 'primary' : undefined"
            :variant="message.role === AssistantMessageRole.User ? 'flat' : 'tonal'"
            max-width="70%"
            class="pa-3"
          >
            <span style="white-space: pre-wrap">{{ message.content }}</span>
          </v-card>
        </div>

        <div v-if="sending" class="d-flex justify-start mb-3">
          <v-card variant="tonal" class="pa-3">
            <v-progress-circular indeterminate size="16" width="2" />
            <span class="ml-2 text-medium-emphasis">{{ t('assistant.thinking') }}</span>
          </v-card>
        </div>
      </div>

      <v-textarea
        v-model="draft"
        :label="t('assistant.placeholder')"
        rows="1"
        auto-grow
        max-rows="4"
        :disabled="sending"
        hide-details
        @keydown.enter="handleEnter"
      >
        <template #append-inner>
          <v-btn icon="mdi-send" variant="text" :disabled="!draft.trim() || sending" :loading="sending" @click="send" />
        </template>
      </v-textarea>
    </template>
  </div>
</template>
