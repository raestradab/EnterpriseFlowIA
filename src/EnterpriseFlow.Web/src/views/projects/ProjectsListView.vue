<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import * as projectsApi from '../../api/projects'
import * as clientsApi from '../../api/clients'
import { useNotificationsStore } from '../../stores/notifications'
import { useAuthStore } from '../../stores/auth'
import { extractErrorMessage } from '../../api/errors'
import type { Client, ProjectListItem } from '../../types'
import { ProjectStatus } from '../../types'

const { t } = useI18n()
const router = useRouter()
const notifications = useNotificationsStore()
const auth = useAuthStore()

const projects = ref<ProjectListItem[]>([])
const clients = ref<Client[]>([])
const loading = ref(true)
const dialog = ref(false)
const saving = ref(false)
const name = ref('')
const clientId = ref<string | null>(null)
const startDate = ref('')
const estimatedEndDate = ref('')

const clientOptions = computed(() => clients.value.map((c) => ({ title: c.name, value: c.id })))

function clientName(id: string) {
  return clients.value.find((c) => c.id === id)?.name ?? id
}

function statusLabel(status: ProjectStatus) {
  return t(`projects.status${status}`)
}

const statusColors: Record<ProjectStatus, string> = {
  [ProjectStatus.Planned]: 'grey',
  [ProjectStatus.Active]: 'primary',
  [ProjectStatus.OnHold]: 'warning',
  [ProjectStatus.Closed]: 'success',
  [ProjectStatus.Cancelled]: 'error',
}

// computed so the language switcher (DefaultLayout) re-translates it — see CompaniesListView.
const headers = computed(() => [
  { title: t('projects.name'), key: 'name' },
  { title: t('projects.client'), key: 'clientName' },
  { title: t('projects.status'), key: 'status' },
])

const rows = computed(() => projects.value.map((p) => ({ ...p, clientName: clientName(p.clientId) })))

async function load() {
  loading.value = true
  try {
    const [projectsResponse, clientsResponse] = await Promise.all([
      projectsApi.getProjects(),
      clientsApi.getClients(),
    ])
    projects.value = projectsResponse.data
    clients.value = clientsResponse.data
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  } finally {
    loading.value = false
  }
}

async function save() {
  if (!clientId.value) return
  saving.value = true
  try {
    await projectsApi.createProject({
      name: name.value,
      clientId: clientId.value,
      startDate: startDate.value || null,
      estimatedEndDate: estimatedEndDate.value || null,
    })
    dialog.value = false
    name.value = ''
    clientId.value = null
    startDate.value = ''
    estimatedEndDate.value = ''
    await load()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  } finally {
    saving.value = false
  }
}

function openProject(id: string) {
  router.push({ name: 'project-detail', params: { id } })
}

onMounted(load)
</script>

<template>
  <div class="d-flex align-center justify-space-between mb-4">
    <h1 class="text-h4">{{ t('projects.title') }}</h1>
    <v-btn v-if="auth.hasPermission('projects.manage')" color="primary" prepend-icon="mdi-plus" @click="dialog = true">
      {{ t('projects.new') }}
    </v-btn>
  </div>

  <v-skeleton-loader v-if="loading" type="table" />
  <v-data-table
    v-else
    :headers="headers"
    :items="rows"
    :no-data-text="t('common.noData')"
    @click:row="(_e: unknown, row: { item: ProjectListItem }) => openProject(row.item.id)"
  >
    <template #item.status="{ item }">
      <v-chip :color="statusColors[item.status as ProjectStatus]" size="small">{{ statusLabel(item.status) }}</v-chip>
    </template>
  </v-data-table>

  <v-dialog v-model="dialog" max-width="480">
    <v-card>
      <v-card-title>{{ t('projects.new') }}</v-card-title>
      <v-card-text>
        <v-form @submit.prevent="save">
          <v-text-field v-model="name" :label="t('projects.name')" required autofocus />
          <v-select v-model="clientId" :items="clientOptions" :label="t('projects.client')" required />
          <v-text-field v-model="startDate" :label="t('projects.startDate')" type="date" />
          <v-text-field v-model="estimatedEndDate" :label="t('projects.estimatedEndDate')" type="date" />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="dialog = false">{{ t('actions.cancel') }}</v-btn>
        <v-btn color="primary" :loading="saving" :disabled="!name || !clientId" @click="save">
          {{ t('actions.save') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
