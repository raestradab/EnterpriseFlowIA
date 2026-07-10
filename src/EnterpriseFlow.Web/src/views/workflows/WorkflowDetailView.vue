<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import * as workflowsApi from '../../api/workflows'
import { useNotificationsStore } from '../../stores/notifications'
import { useAuthStore } from '../../stores/auth'
import { extractErrorMessage } from '../../api/errors'
import type { Workflow } from '../../types'

const { t } = useI18n()
const route = useRoute()
const notifications = useNotificationsStore()
const auth = useAuthStore()

const workflowId = route.params.id as string
const workflow = ref<Workflow | null>(null)
const loading = ref(true)

const stateDialog = ref(false)
const stateName = ref('')
const stateIsInitial = ref(false)
const stateIsFinal = ref(false)

const transitionDialog = ref(false)
const transitionName = ref('')
const transitionFromStateId = ref<string | null>(null)
const transitionToStateId = ref<string | null>(null)

const stateOptions = computed(() => (workflow.value?.states ?? []).map((s) => ({ title: s.name, value: s.id })))

function stateName_(id: string) {
  return workflow.value?.states.find((s) => s.id === id)?.name ?? id
}

async function load() {
  loading.value = true
  try {
    const response = await workflowsApi.getWorkflow(workflowId)
    workflow.value = response.data
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  } finally {
    loading.value = false
  }
}

async function addState() {
  try {
    await workflowsApi.addWorkflowState(workflowId, {
      name: stateName.value,
      isInitial: stateIsInitial.value,
      isFinal: stateIsFinal.value,
    })
    stateDialog.value = false
    stateName.value = ''
    stateIsInitial.value = false
    stateIsFinal.value = false
    await load()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  }
}

async function addTransition() {
  if (!transitionFromStateId.value || !transitionToStateId.value) return
  try {
    await workflowsApi.addWorkflowTransition(workflowId, {
      name: transitionName.value,
      fromStateId: transitionFromStateId.value,
      toStateId: transitionToStateId.value,
    })
    transitionDialog.value = false
    transitionName.value = ''
    transitionFromStateId.value = null
    transitionToStateId.value = null
    await load()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  }
}

onMounted(load)
</script>

<template>
  <v-skeleton-loader v-if="loading" type="article" />

  <template v-else-if="workflow">
    <h1 class="text-h4 mb-4">{{ workflow.name }}</h1>

    <v-row>
      <v-col cols="12" md="6">
        <v-card class="pa-4 mb-4">
          <div class="d-flex align-center justify-space-between mb-2">
            <span class="text-h6">{{ t('workflows.states') }}</span>
            <v-btn
              v-if="auth.hasPermission('workflows.manage')"
              size="small"
              variant="tonal"
              prepend-icon="mdi-plus"
              @click="stateDialog = true"
            >
              {{ t('workflows.newState') }}
            </v-btn>
          </div>
          <v-list v-if="workflow.states.length" density="compact">
            <v-list-item v-for="s in workflow.states" :key="s.id" :title="s.name">
              <template #append>
                <v-chip v-if="s.isInitial" color="primary" size="small" class="mr-1">{{ t('workflows.initial') }}</v-chip>
                <v-chip v-if="s.isFinal" color="success" size="small">{{ t('workflows.final') }}</v-chip>
              </template>
            </v-list-item>
          </v-list>
          <p v-else class="text-medium-emphasis">{{ t('common.noData') }}</p>
        </v-card>
      </v-col>

      <v-col cols="12" md="6">
        <v-card class="pa-4 mb-4">
          <div class="d-flex align-center justify-space-between mb-2">
            <span class="text-h6">{{ t('workflows.transitions') }}</span>
            <v-btn
              v-if="auth.hasPermission('workflows.manage') && workflow.states.length >= 2"
              size="small"
              variant="tonal"
              prepend-icon="mdi-plus"
              @click="transitionDialog = true"
            >
              {{ t('workflows.newTransition') }}
            </v-btn>
          </div>
          <v-list v-if="workflow.transitions.length" density="compact">
            <v-list-item
              v-for="tr in workflow.transitions"
              :key="tr.id"
              :title="tr.name"
              :subtitle="`${stateName_(tr.fromStateId)} → ${stateName_(tr.toStateId)}`"
            />
          </v-list>
          <p v-else class="text-medium-emphasis">{{ t('common.noData') }}</p>
        </v-card>
      </v-col>
    </v-row>
  </template>

  <v-dialog v-model="stateDialog" max-width="420">
    <v-card>
      <v-card-title>{{ t('workflows.newState') }}</v-card-title>
      <v-card-text>
        <v-text-field v-model="stateName" :label="t('workflows.stateName')" required autofocus />
        <v-checkbox v-model="stateIsInitial" :label="t('workflows.initial')" />
        <v-checkbox v-model="stateIsFinal" :label="t('workflows.final')" />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="stateDialog = false">{{ t('actions.cancel') }}</v-btn>
        <v-btn color="primary" :disabled="!stateName" @click="addState">{{ t('actions.save') }}</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <v-dialog v-model="transitionDialog" max-width="420">
    <v-card>
      <v-card-title>{{ t('workflows.newTransition') }}</v-card-title>
      <v-card-text>
        <v-text-field v-model="transitionName" :label="t('workflows.transitionName')" required autofocus />
        <v-select v-model="transitionFromStateId" :items="stateOptions" :label="t('workflows.fromState')" />
        <v-select v-model="transitionToStateId" :items="stateOptions" :label="t('workflows.toState')" />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="transitionDialog = false">{{ t('actions.cancel') }}</v-btn>
        <v-btn
          color="primary"
          :disabled="!transitionName || !transitionFromStateId || !transitionToStateId"
          @click="addTransition"
        >
          {{ t('actions.save') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
