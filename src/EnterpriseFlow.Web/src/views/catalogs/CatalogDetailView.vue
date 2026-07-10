<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import * as catalogsApi from '../../api/catalogs'
import { useNotificationsStore } from '../../stores/notifications'
import { useAuthStore } from '../../stores/auth'
import { extractErrorMessage } from '../../api/errors'
import type { CatalogItem } from '../../types'

const { t } = useI18n()
const route = useRoute()
const notifications = useNotificationsStore()
const auth = useAuthStore()

const catalogId = route.params.id as string
const items = ref<CatalogItem[]>([])
const loading = ref(true)

const addDialog = ref(false)
const itemKey = ref('')
const itemLabel = ref('')

const editDialog = ref(false)
const editingItem = ref<CatalogItem | null>(null)
const editLabel = ref('')

async function load() {
  loading.value = true
  try {
    const response = await catalogsApi.getCatalogItems(catalogId)
    items.value = response.data
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  } finally {
    loading.value = false
  }
}

async function addItem() {
  try {
    await catalogsApi.addCatalogItem(catalogId, { key: itemKey.value, label: itemLabel.value })
    addDialog.value = false
    itemKey.value = ''
    itemLabel.value = ''
    await load()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  }
}

function openEdit(item: CatalogItem) {
  editingItem.value = item
  editLabel.value = item.label
  editDialog.value = true
}

async function saveEdit() {
  if (!editingItem.value) return
  try {
    await catalogsApi.updateCatalogItem(catalogId, editingItem.value.id, { label: editLabel.value })
    editDialog.value = false
    await load()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  }
}

async function removeItem(item: CatalogItem) {
  try {
    await catalogsApi.removeCatalogItem(catalogId, item.id)
    await load()
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  }
}

onMounted(load)
</script>

<template>
  <div class="d-flex align-center justify-space-between mb-4">
    <h1 class="text-h4">{{ t('catalogs.items') }}</h1>
    <v-btn v-if="auth.hasPermission('catalogs.manage')" color="primary" prepend-icon="mdi-plus" @click="addDialog = true">
      {{ t('catalogs.newItem') }}
    </v-btn>
  </div>

  <v-skeleton-loader v-if="loading" type="table" />
  <v-list v-else-if="items.length" density="compact">
    <v-list-item v-for="item in items" :key="item.id" :title="item.label" :subtitle="item.key">
      <template v-if="auth.hasPermission('catalogs.manage')" #append>
        <v-btn size="small" variant="text" icon="mdi-pencil" @click="openEdit(item)" />
        <v-btn size="small" variant="text" icon="mdi-delete" @click="removeItem(item)" />
      </template>
    </v-list-item>
  </v-list>
  <p v-else class="text-medium-emphasis">{{ t('common.noData') }}</p>

  <v-dialog v-model="addDialog" max-width="420">
    <v-card>
      <v-card-title>{{ t('catalogs.newItem') }}</v-card-title>
      <v-card-text>
        <v-text-field v-model="itemKey" :label="t('catalogs.key')" required autofocus />
        <v-text-field v-model="itemLabel" :label="t('catalogs.label')" required />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="addDialog = false">{{ t('actions.cancel') }}</v-btn>
        <v-btn color="primary" :disabled="!itemKey || !itemLabel" @click="addItem">{{ t('actions.save') }}</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <v-dialog v-model="editDialog" max-width="420">
    <v-card>
      <v-card-title>{{ t('catalogs.label') }}</v-card-title>
      <v-card-text>
        <v-text-field v-model="editLabel" :label="t('catalogs.label')" required autofocus />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn variant="text" @click="editDialog = false">{{ t('actions.cancel') }}</v-btn>
        <v-btn color="primary" :disabled="!editLabel" @click="saveEdit">{{ t('actions.save') }}</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
