<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import * as projectsApi from '../../api/projects'
import * as tasksApi from '../../api/tasks'
import * as documentsApi from '../../api/documents'
import * as workflowsApi from '../../api/workflows'
import { useNotificationsStore } from '../../stores/notifications'
import { useAuthStore } from '../../stores/auth'
import { extractErrorMessage } from '../../api/errors'
import type { DocumentListItem, Project, TaskItem, Workflow, WorkflowListItem } from '../../types'
import { DocumentOwnerType, ProjectRole, ProjectStatus, ProjectTaskStatus, TaskPriority } from '../../types'

const { t } = useI18n()
const route = useRoute()
const notifications = useNotificationsStore()
const auth = useAuthStore()

const projectId = route.params.id as string
const project = ref<Project | null>(null)
const tasks = ref<TaskItem[]>([])
const documents = ref<DocumentListItem[]>([])
const workflows = ref<WorkflowListItem[]>([])
const loading = ref(true)
const closing = ref(false)

const memberDialog = ref(false)
const memberUserId = ref('')
const memberRole = ref<ProjectRole>(ProjectRole.Developer)

const taskDialog = ref(false)
const taskTitle = ref('')
const taskDueDate = ref('')
const taskPriority = ref<TaskPriority>(TaskPriority.Medium)

const uploadDialog = ref(false)
const uploadFile = ref<File | null>(null)
const uploadWorkflowId = ref<string | null>(null)
const uploading = ref(false)

const transitionDialog = ref(false)
const transitioningDocument = ref<DocumentListItem | null>(null)
const transitionWorkflow = ref<Workflow | null>(null)
const transitionTargetStateId = ref<string | null>(null)

// computed so the language switcher (DefaultLayout) re-translates these — a plain array
// only reads t() once at setup and goes stale on locale switch (found via manual browser
// testing: table headers elsewhere in the app had the same bug).
const roleOptions = computed(() => [
  { title: t('projects.role') + ': Developer', value: ProjectRole.Developer },
  { title: 'QA Engineer', value: ProjectRole.QaEngineer },
  { title: 'Lead', value: ProjectRole.Lead },
  { title: 'Project Manager', value: ProjectRole.ProjectManager },
])

const priorityOptions = computed(() =>
  [0, 1, 2, 3].map((p) => ({ title: t(`tasks.priority${p}`), value: p as TaskPriority })),
)

const workflowOptions = computed(() => workflows.value.map((w) => ({ title: w.name, value: w.id })))

const transitionTargetOptions = computed(() => {
  if (!transitionWorkflow.value || !transitioningDocument.value) return []
  const fromStateId = transitioningDocument.value.currentWorkflowStateId
  return transitionWorkflow.value.transitions
    .filter((tr) => tr.fromStateId === fromStateId)
    .map((tr) => {
      const targetState = transitionWorkflow.value!.states.find((s) => s.id === tr.toStateId)
      return { title: targetState?.name ?? tr.toStateId, value: tr.toStateId }
    })
})

const canClose = computed(() => project.value?.status === ProjectStatus.Planned || project.value?.status === ProjectStatus.Active)

function formatSize(bytes: number) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

async function load() {
  loading.value = true
  try {
    const [projectResponse, tasksResponse, documentsResponse, workflowsResponse] = await Promise.all([
      projectsApi.getProject(projectId),
      tasksApi.getTasks(projectId),
      documentsApi.getDocuments(DocumentOwnerType.Project, projectId),
      workflowsApi.getWorkflows(),
    ])
    project.value = projectResponse.data
    tasks.value = tasksResponse.data
    documents.value = documentsResponse.data
    workflows.value = workflowsResponse.data
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  } finally {
    loading.value = false
  }
}

async function close() {
  closing.value = true
  try {
    await projectsApi.closeProject(projectId)
    notifications.show(t('actions.close'), 'success')
    await load()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  } finally {
    closing.value = false
  }
}

async function addMember() {
  try {
    await projectsApi.addProjectMember(projectId, { userId: memberUserId.value, role: memberRole.value })
    memberDialog.value = false
    memberUserId.value = ''
    await load()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  }
}

async function createTask() {
  try {
    await tasksApi.createTask({
      title: taskTitle.value,
      description: null,
      priority: taskPriority.value,
      projectId,
      dueDate: taskDueDate.value || null,
    })
    taskDialog.value = false
    taskTitle.value = ''
    taskDueDate.value = ''
    await load()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  }
}

async function completeTask(id: string) {
  try {
    await tasksApi.completeTask(id)
    await load()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  }
}

function taskStatusLabel(status: ProjectTaskStatus) {
  return t(`tasks.status${status}`)
}

function onFileSelected(files: File[] | File | null) {
  uploadFile.value = Array.isArray(files) ? (files[0] ?? null) : files
}

async function uploadDocument() {
  if (!uploadFile.value || !uploadWorkflowId.value) return
  uploading.value = true
  try {
    await documentsApi.uploadDocument({
      file: uploadFile.value,
      ownerType: DocumentOwnerType.Project,
      ownerId: projectId,
      workflowDefinitionId: uploadWorkflowId.value,
    })
    uploadDialog.value = false
    uploadFile.value = null
    uploadWorkflowId.value = null
    await load()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  } finally {
    uploading.value = false
  }
}

async function downloadDocument(doc: DocumentListItem) {
  try {
    const response = await documentsApi.downloadDocumentContent(doc.id)
    const url = window.URL.createObjectURL(response.data as Blob)
    const link = document.createElement('a')
    link.href = url
    link.download = doc.fileName
    link.click()
    window.URL.revokeObjectURL(url)
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  }
}

async function openTransitionDialog(doc: DocumentListItem) {
  transitioningDocument.value = doc
  transitionTargetStateId.value = null
  transitionDialog.value = true
  try {
    const response = await workflowsApi.getWorkflow(doc.workflowDefinitionId)
    transitionWorkflow.value = response.data
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  }
}

async function confirmTransition() {
  if (!transitioningDocument.value || !transitionTargetStateId.value) return
  try {
    await documentsApi.transitionDocument(transitioningDocument.value.id, transitionTargetStateId.value)
    transitionDialog.value = false
    await load()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  }
}

async function removeDocument(doc: DocumentListItem) {
  try {
    await documentsApi.deleteDocument(doc.id)
    await load()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  }
}

onMounted(load)
</script>

<template>
  <v-skeleton-loader v-if="loading" type="article" />

  <template v-else-if="project">
    <div class="d-flex align-center justify-space-between mb-4">
      <h1 class="text-h4">{{ project.name }}</h1>
      <v-btn
        v-if="auth.hasPermission('projects.manage') && canClose"
        color="warning"
        :loading="closing"
        @click="close"
      >
        {{ t('actions.close') }}
      </v-btn>
    </div>

    <v-row>
      <v-col cols="12" md="6">
        <v-card class="pa-4 mb-4">
          <div class="d-flex align-center justify-space-between mb-2">
            <span class="text-h6">{{ t('projects.team') }}</span>
            <v-btn
              v-if="auth.hasPermission('projects.manage')"
              size="small"
              variant="tonal"
              prepend-icon="mdi-plus"
              @click="memberDialog = true"
            >
              {{ t('actions.addMember') }}
            </v-btn>
          </div>
          <v-list v-if="project.members.length" density="compact">
            <v-list-item v-for="m in project.members" :key="m.userId" :title="m.userId" :subtitle="String(m.role)" />
          </v-list>
          <p v-else class="text-medium-emphasis">{{ t('common.noData') }}</p>
        </v-card>
      </v-col>

      <v-col cols="12" md="6">
        <v-card class="pa-4 mb-4">
          <div class="d-flex align-center justify-space-between mb-2">
            <span class="text-h6">{{ t('tasks.title') }}</span>
            <v-btn
              v-if="auth.hasPermission('tasks.manage')"
              size="small"
              variant="tonal"
              prepend-icon="mdi-plus"
              @click="taskDialog = true"
            >
              {{ t('tasks.new') }}
            </v-btn>
          </div>
          <v-list v-if="tasks.length" density="compact">
            <v-list-item v-for="task in tasks" :key="task.id" :title="task.title" :subtitle="taskStatusLabel(task.status)">
              <template #append>
                <v-btn
                  v-if="auth.hasPermission('tasks.manage') && task.status !== ProjectTaskStatus.Completed"
                  size="small"
                  variant="text"
                  @click="completeTask(task.id)"
                >
                  {{ t('actions.complete') }}
                </v-btn>
              </template>
            </v-list-item>
          </v-list>
          <p v-else class="text-medium-emphasis">{{ t('common.noData') }}</p>
        </v-card>
      </v-col>

      <v-col cols="12">
        <v-card class="pa-4 mb-4">
          <div class="d-flex align-center justify-space-between mb-2">
            <span class="text-h6">{{ t('documents.title') }}</span>
            <v-btn
              v-if="auth.hasPermission('documents.manage')"
              size="small"
              variant="tonal"
              prepend-icon="mdi-upload"
              @click="uploadDialog = true"
            >
              {{ t('documents.upload') }}
            </v-btn>
          </div>
          <v-list v-if="documents.length" density="compact">
            <v-list-item v-for="doc in documents" :key="doc.id" :title="doc.fileName" :subtitle="formatSize(doc.sizeBytes)">
              <template #append>
                <v-chip size="small" class="mr-2">{{ doc.currentWorkflowStateName }}</v-chip>
                <v-btn size="small" variant="text" icon="mdi-download" @click="downloadDocument(doc)" />
                <v-btn
                  v-if="auth.hasPermission('documents.approve')"
                  size="small"
                  variant="text"
                  icon="mdi-arrow-right-circle-outline"
                  @click="openTransitionDialog(doc)"
                />
                <v-btn
                  v-if="auth.hasPermission('documents.manage')"
                  size="small"
                  variant="text"
                  icon="mdi-delete"
                  @click="removeDocument(doc)"
                />
              </template>
            </v-list-item>
          </v-list>
          <p v-else class="text-medium-emphasis">{{ t('common.noData') }}</p>
        </v-card>
      </v-col>
    </v-row>
  </template>

  <v-dialog v-model="memberDialog" max-width="420">
    <v-card>
      <v-card-title>{{ t('actions.addMember') }}</v-card-title>
      <v-card-text>
        <v-text-field v-model="memberUserId" :label="t('projects.member')" required autofocus />
        <v-select v-model="memberRole" :items="roleOptions" :label="t('projects.role')" />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="memberDialog = false">{{ t('actions.cancel') }}</v-btn>
        <v-btn color="primary" :disabled="!memberUserId" @click="addMember">{{ t('actions.save') }}</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <v-dialog v-model="taskDialog" max-width="420">
    <v-card>
      <v-card-title>{{ t('tasks.new') }}</v-card-title>
      <v-card-text>
        <v-text-field v-model="taskTitle" :label="t('tasks.titleField')" required autofocus />
        <v-select v-model="taskPriority" :items="priorityOptions" :label="t('tasks.priority')" />
        <v-text-field v-model="taskDueDate" :label="t('tasks.dueDate')" type="date" />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="taskDialog = false">{{ t('actions.cancel') }}</v-btn>
        <v-btn color="primary" :disabled="!taskTitle" @click="createTask">{{ t('actions.save') }}</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <v-dialog v-model="uploadDialog" max-width="420">
    <v-card>
      <v-card-title>{{ t('documents.upload') }}</v-card-title>
      <v-card-text>
        <v-file-input :label="t('documents.file')" required @update:model-value="onFileSelected" />
        <v-select v-model="uploadWorkflowId" :items="workflowOptions" :label="t('documents.workflow')" />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="uploadDialog = false">{{ t('actions.cancel') }}</v-btn>
        <v-btn
          color="primary"
          :loading="uploading"
          :disabled="!uploadFile || !uploadWorkflowId"
          @click="uploadDocument"
        >
          {{ t('actions.save') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <v-dialog v-model="transitionDialog" max-width="420">
    <v-card>
      <v-card-title>{{ t('documents.transition') }}</v-card-title>
      <v-card-text>
        <v-select
          v-model="transitionTargetStateId"
          :items="transitionTargetOptions"
          :label="t('documents.targetState')"
          :no-data-text="t('documents.noTransitionsAvailable')"
        />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="transitionDialog = false">{{ t('actions.cancel') }}</v-btn>
        <v-btn color="primary" :disabled="!transitionTargetStateId" @click="confirmTransition">
          {{ t('actions.save') }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
