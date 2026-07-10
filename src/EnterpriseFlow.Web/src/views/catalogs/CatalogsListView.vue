<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import * as catalogsApi from '../../api/catalogs'
import { useNotificationsStore } from '../../stores/notifications'
import { useAuthStore } from '../../stores/auth'
import { extractErrorMessage } from '../../api/errors'
import type { CatalogListItem } from '../../types'

const { t } = useI18n()
const router = useRouter()
const notifications = useNotificationsStore()
const auth = useAuthStore()

const catalogs = ref<CatalogListItem[]>([])
const loading = ref(true)
const dialog = ref(false)
const saving = ref(false)
const name = ref('')

// computed so the language switcher re-translates it — see ProjectsListView.
const headers = computed(() => [
  { title: t('catalogs.name'), key: 'name' },
  { title: t('catalogs.itemCount'), key: 'itemCount' },
])

async function load() {
  loading.value = true
  try {
    const response = await catalogsApi.getCatalogs()
    catalogs.value = response.data
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  try {
    await catalogsApi.createCatalog({ name: name.value })
    dialog.value = false
    name.value = ''
    await load()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  } finally {
    saving.value = false
  }
}

function openCatalog(id: string) {
  router.push({ name: 'catalog-detail', params: { id } })
}

onMounted(load)
</script>

<template>
  <div class="d-flex align-center justify-space-between mb-4">
    <h1 class="text-h4">{{ t('catalogs.title') }}</h1>
    <v-btn v-if="auth.hasPermission('catalogs.manage')" color="primary" prepend-icon="mdi-plus" @click="dialog = true">
      {{ t('catalogs.new') }}
    </v-btn>
  </div>

  <v-skeleton-loader v-if="loading" type="table" />
  <v-data-table
    v-else
    :headers="headers"
    :items="catalogs"
    :no-data-text="t('common.noData')"
    @click:row="(_e: unknown, row: { item: CatalogListItem }) => openCatalog(row.item.id)"
  />

  <v-dialog v-model="dialog" max-width="420">
    <v-card>
      <v-card-title>{{ t('catalogs.new') }}</v-card-title>
      <v-card-text>
        <v-form @submit.prevent="save">
          <v-text-field v-model="name" :label="t('catalogs.name')" required autofocus />
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
