<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import * as companiesApi from '../../api/companies'
import { useNotificationsStore } from '../../stores/notifications'
import { useAuthStore } from '../../stores/auth'
import { extractErrorMessage } from '../../api/errors'
import type { Company } from '../../types'

const { t } = useI18n()
const notifications = useNotificationsStore()
const auth = useAuthStore()

const companies = ref<Company[]>([])
const loading = ref(true)
const dialog = ref(false)
const saving = ref(false)
const name = ref('')
const taxId = ref('')

// computed, not a plain array: v-data-table headers are only read once at mount otherwise,
// so switching locale (toggleLocale in DefaultLayout) wouldn't re-translate them — caught by
// manually testing the language switcher, not by typecheck/build.
const headers = computed(() => [
  { title: t('companies.name'), key: 'name' },
  { title: t('companies.taxId'), key: 'taxId' },
])

async function load() {
  loading.value = true
  try {
    const response = await companiesApi.getCompanies()
    companies.value = response.data
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  try {
    await companiesApi.createCompany({ name: name.value, taxId: taxId.value || null })
    dialog.value = false
    name.value = ''
    taxId.value = ''
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
    <h1 class="text-h4">{{ t('companies.title') }}</h1>
    <v-btn v-if="auth.hasPermission('companies.manage')" color="primary" prepend-icon="mdi-plus" @click="dialog = true">
      {{ t('companies.new') }}
    </v-btn>
  </div>

  <v-skeleton-loader v-if="loading" type="table" />
  <v-data-table v-else :headers="headers" :items="companies" :no-data-text="t('common.noData')" />

  <v-dialog v-model="dialog" max-width="480">
    <v-card>
      <v-card-title>{{ t('companies.new') }}</v-card-title>
      <v-card-text>
        <v-form @submit.prevent="save">
          <v-text-field v-model="name" :label="t('companies.name')" required autofocus />
          <v-text-field v-model="taxId" :label="t('companies.taxId')" />
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
