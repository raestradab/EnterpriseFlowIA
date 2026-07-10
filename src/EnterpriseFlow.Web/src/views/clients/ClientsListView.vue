<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import * as clientsApi from '../../api/clients'
import * as companiesApi from '../../api/companies'
import { useNotificationsStore } from '../../stores/notifications'
import { useAuthStore } from '../../stores/auth'
import { extractErrorMessage } from '../../api/errors'
import type { Client, Company } from '../../types'

const { t } = useI18n()
const notifications = useNotificationsStore()
const auth = useAuthStore()

const clients = ref<Client[]>([])
const companies = ref<Company[]>([])
const loading = ref(true)
const dialog = ref(false)
const saving = ref(false)
const name = ref('')
const companyId = ref<string | null>(null)

const companyOptions = computed(() => [
  { title: t('clients.none'), value: null },
  ...companies.value.map((c) => ({ title: c.name, value: c.id })),
])

function companyName(id: string | null) {
  if (!id) return t('clients.none')
  return companies.value.find((c) => c.id === id)?.name ?? id
}

// computed so the language switcher (DefaultLayout) re-translates it — see CompaniesListView.
const headers = computed(() => [
  { title: t('clients.name'), key: 'name' },
  { title: t('clients.company'), key: 'companyName' },
])

const rows = computed(() => clients.value.map((c) => ({ ...c, companyName: companyName(c.companyId) })))

async function load() {
  loading.value = true
  try {
    const [clientsResponse, companiesResponse] = await Promise.all([
      clientsApi.getClients(),
      auth.hasPermission('companies.read') ? companiesApi.getCompanies() : Promise.resolve({ data: [] }),
    ])
    clients.value = clientsResponse.data
    companies.value = companiesResponse.data
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  try {
    await clientsApi.createClient({ name: name.value, companyId: companyId.value })
    dialog.value = false
    name.value = ''
    companyId.value = null
    await load()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="d-flex align-center justify-space-between mb-4">
    <h1 class="text-h4">{{ t('clients.title') }}</h1>
    <v-btn v-if="auth.hasPermission('clients.manage')" color="primary" prepend-icon="mdi-plus" @click="dialog = true">
      {{ t('clients.new') }}
    </v-btn>
  </div>

  <v-skeleton-loader v-if="loading" type="table" />
  <v-data-table v-else :headers="headers" :items="rows" :no-data-text="t('common.noData')" />

  <v-dialog v-model="dialog" max-width="480">
    <v-card>
      <v-card-title>{{ t('clients.new') }}</v-card-title>
      <v-card-text>
        <v-form @submit.prevent="save">
          <v-text-field v-model="name" :label="t('clients.name')" required autofocus />
          <v-select v-model="companyId" :items="companyOptions" :label="t('clients.company')" />
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="dialog = false">{{ t('actions.cancel') }}</v-btn>
        <v-btn color="primary" :loading="saving" :disabled="!name" @click="save">{{ t('actions.save') }}</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
