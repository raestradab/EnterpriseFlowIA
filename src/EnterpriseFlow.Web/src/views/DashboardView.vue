<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '../stores/auth'
import * as companiesApi from '../api/companies'
import * as clientsApi from '../api/clients'
import * as projectsApi from '../api/projects'
import * as tasksApi from '../api/tasks'
import { ProjectStatus, ProjectTaskStatus } from '../types'

const { t } = useI18n()
const auth = useAuthStore()

const loading = ref(true)
const counts = ref({ companies: 0, clients: 0, activeProjects: 0, openTasks: 0 })

// Dashboard cards reflect only the entities the current user can actually read (HU-005's
// same "don't render what you can't use" principle applied to KPI tiles, not just menu items).
async function load() {
  loading.value = true
  try {
    const requests: Promise<void>[] = []

    if (auth.hasPermission('companies.read')) {
      requests.push(
        companiesApi.getCompanies().then((r) => {
          counts.value.companies = r.data.length
        }),
      )
    }
    if (auth.hasPermission('clients.read')) {
      requests.push(
        clientsApi.getClients().then((r) => {
          counts.value.clients = r.data.length
        }),
      )
    }
    if (auth.hasPermission('projects.read')) {
      requests.push(
        projectsApi.getProjects().then((r) => {
          counts.value.activeProjects = r.data.filter((p) => p.status === ProjectStatus.Active).length
        }),
      )
    }
    if (auth.hasPermission('tasks.read')) {
      requests.push(
        tasksApi.getTasks().then((r) => {
          counts.value.openTasks = r.data.filter(
            (task) => task.status === ProjectTaskStatus.Todo || task.status === ProjectTaskStatus.InProgress,
          ).length
        }),
      )
    }

    await Promise.all(requests)
  } finally {
    loading.value = false
  }
}

onMounted(load)

const cards = [
  { key: 'companies' as const, label: 'dashboard.companies', icon: 'mdi-domain', permission: 'companies.read' },
  { key: 'clients' as const, label: 'dashboard.clients', icon: 'mdi-account-group', permission: 'clients.read' },
  { key: 'activeProjects' as const, label: 'dashboard.projects', icon: 'mdi-briefcase-outline', permission: 'projects.read' },
  { key: 'openTasks' as const, label: 'dashboard.openTasks', icon: 'mdi-checkbox-marked-outline', permission: 'tasks.read' },
]
</script>

<template>
  <h1 class="text-h4 mb-6">{{ t('dashboard.title') }}</h1>

  <v-row>
    <template v-for="card in cards" :key="card.key">
      <v-col v-if="auth.hasPermission(card.permission)" cols="12" sm="6" md="3">
        <v-card class="pa-4">
          <v-skeleton-loader v-if="loading" type="heading" />
          <template v-else>
            <div class="d-flex align-center justify-space-between">
              <div>
                <div class="text-medium-emphasis text-body-2">{{ t(card.label) }}</div>
                <div class="text-h4 font-weight-bold">{{ counts[card.key] }}</div>
              </div>
              <v-icon :icon="card.icon" size="40" color="primary" />
            </div>
          </template>
        </v-card>
      </v-col>
    </template>
  </v-row>
</template>
