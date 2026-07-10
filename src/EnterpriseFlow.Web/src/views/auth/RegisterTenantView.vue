<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '../../stores/auth'
import { useNotificationsStore } from '../../stores/notifications'
import { extractErrorMessage } from '../../api/errors'

const { t } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const notifications = useNotificationsStore()

const tenantName = ref('')
const tenantSlug = ref('')
const adminEmail = ref('')
const adminPassword = ref('')
const loading = ref(false)

async function submit() {
  loading.value = true
  try {
    await auth.registerTenant({
      tenantName: tenantName.value,
      tenantSlug: tenantSlug.value,
      adminEmail: adminEmail.value,
      adminPassword: adminPassword.value,
    })
    notifications.show(t('auth.registerSuccess'), 'success')
    await router.push({ name: 'login' })
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('common.unexpectedError')), 'error')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <v-container class="fill-height" fluid>
    <v-row justify="center" align="center" class="fill-height">
      <v-col cols="12" sm="8" md="5">
        <v-card class="pa-4" elevation="4">
          <v-card-title class="text-h5 mb-2">{{ t('auth.registerTenant') }}</v-card-title>
          <v-form @submit.prevent="submit">
            <v-text-field v-model="tenantName" :label="t('auth.tenantName')" required />
            <v-text-field v-model="tenantSlug" :label="t('auth.tenantSlug')" required />
            <v-text-field
              v-model="adminEmail"
              :label="t('auth.adminEmail')"
              type="email"
              autocomplete="username"
              required
            />
            <v-text-field
              v-model="adminPassword"
              :label="t('auth.adminPassword')"
              type="password"
              autocomplete="new-password"
              required
            />
            <v-btn type="submit" color="primary" block size="large" :loading="loading">
              {{ t('actions.create') }}
            </v-btn>
          </v-form>
          <div class="text-center mt-4">
            <span class="text-medium-emphasis">{{ t('auth.haveAccount') }}</span>
            <router-link to="/login" class="ml-1">{{ t('auth.login') }}</router-link>
          </div>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>
