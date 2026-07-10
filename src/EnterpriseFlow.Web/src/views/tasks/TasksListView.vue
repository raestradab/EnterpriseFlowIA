<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import * as tasksApi from '../../api/tasks'
import * as projectsApi from '../../api/projects'
import { useNotificationsStore } from '../../stores/notifications'
import { useAuthStore } from '../../stores/auth'
import { extractErrorMessage } from '../../api/errors'
import type { ProjectListItem, TaskItem } from '../../types'
import { ProjectTaskStatus, TaskPriority } from '../../types'

const { t } = useI18n()
const notifications = useNotificationsStore()
const auth = useAuthStore()

const tasks = ref<TaskItem[]>([])
const projects = ref<ProjectListItem[]>([])
const loading = ref(true)
const dialog = ref(false)
const saving = ref(false)

const title = ref('')
const projectId = ref<string | null>(null)
const priority = ref<TaskPriority>(TaskPriority.Medium)
const dueDate = ref('')

const projectOptions = computed(() => projects.value.map((p) => ({ title: p.name, value: p.id })))
// computed (not a plain mapped array) so it re-translates on locale switch — see below.
const priorityOptions = computed(() =>
  [0, 1, 2, 3].map((p) => ({ title: t(`tasks.priority${p}`), value: p as TaskPriority })),
)

function projectName(id: string) {
  return projects.value.find((p) => p.id === id)?.name ?? id
}

function statusLabel(status: ProjectTaskStatus) {
  return t(`tasks.status${status}`)
}

const statusColors: Record<ProjectTaskStatus, string> = {
  [ProjectTaskStatus.Todo]: 'grey',
  [ProjectTaskStatus.InProgress]: 'primary',
  [ProjectTaskStatus.Completed]: 'success',
  [ProjectTaskStatus.Cancelled]: 'error',
}

// computed so the language switcher (DefaultLayout) re-translates it — see CompaniesListView.
const headers = computed(() => [
  { title: t('tasks.titleField'), key: 'title' },
  { title: t('tasks.project'), key: 'projectName' },
  { title: t('projects.status'), key: 'status' },
  { title: t('tasks.dueDate'), key: 'dueDate' },
  { title: '', key: 'actions', sortable: false },
])

const rows = computed(() => tasks.value.map((task) => ({ ...task, projectName: projectName(task.projectId) })))

async function load() {
  loading.value = true
  try {
    const [tasksResponse, projectsResponse] = await Promise.all([tasksApi.getTasks(), projectsApi.getProjects()])
    tasks.value = tasksResponse.data
    projects.value = projectsResponse.data
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  } finally {
    loading.value = false
  }
}

async function save() {
  if (!projectId.value) return
  saving.value = true
  try {
    await tasksApi.createTask({
      title: title.value,
      description: null,
      priority: priority.value,
      projectId: projectId.value,
      dueDate: dueDate.value || null,
    })
    dialog.value = false
    title.value = ''
    projectId.value = null
    dueDate.value = ''
    await load()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  } finally {
    saving.value = false
  }
}

async function complete(id: string) {
  try {
    await tasksApi.completeTask(id)
    await load()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  }
}

async function cancel(id: string) {
  try {
    await tasksApi.cancelTask(id)
    await load()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  }
}

onMounted(load)
</script>

<template>
  <div class="d-flex align-center justify-space-between mb-4">
    <h1 class="text-h4">{{ t('tasks.title') }}</h1>
    <v-btn v-if="auth.hasPermission('tasks.manage')" color="primary" prepend-icon="mdi-plus" @click="dialog = true">
      {{ t('tasks.new') }}
    </v-btn>
  </div>

  <v-skeleton-loader v-if="loading" type="table" />
  <v-data-table v-else :headers="headers" :items="rows" :no-data-text="t('common.noData')">
    <template #item.status="{ item }">
      <v-chip :color="statusColors[item.status as ProjectTaskStatus]" size="small">{{ statusLabel(item.status) }}</v-chip>
    </template>
    <template #item.actions="{ item }">
      <template v-if="auth.hasPermission('tasks.manage') && item.status !== ProjectTaskStatus.Completed && item.status !== ProjectTaskStatus.Cancelled">
        <v-btn size="small" variant="text" @click="complete(item.id)">{{ t('actions.complete') }}</v-btn>
        <v-btn size="small" variant="text" @click="cancel(item.id)">{{ t('actions.cancel') }}</v-btn>
      </template>
    </template>
  </v-data-table>

  <v-dialog v-model="dialog" max-width="480">
    <v-card>
      <v-card-title>{{ t('tasks.new') }}</v-card-title>
      <v-card-text>
        <v-form @submit.prevent="save">
          <v-text-field v-model="title" :label="t('tasks.titleField')" required autofocus />
          <v-select v-model="projectId" :items="projectOptions" :label="t('tasks.project')" required />
          <v-select v-model="priority" :items="priorityOptions" :label="t('tasks.priority')" />
          <v-text-field v-model="dueDate" :label="t('tasks.dueDate')" type="date" />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="dialog = false">{{ t('actions.cancel') }}</v-btn>
        <v-btn color="primary" :loading="saving" :disabled="!title || !projectId" @click="save">
          {{ t('actions.save') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
