<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '../../stores/auth'
import { useNotificationsStore } from '../../stores/notifications'
import { extractErrorMessage } from '../../api/errors'

const { t } = useI18n()
const router = useRouter()
const route = useRoute()
const auth = useAuthStore()
const notifications = useNotificationsStore()

const email = ref('')
const password = ref('')
const loading = ref(false)

async function submit() {
  loading.value = true
  try {
    await auth.login(email.value, password.value)
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    await router.push(redirect)
  } catch (error) {
    notifications.show(extractErrorMessage(error, t('auth.invalidCredentials')), 'error')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <v-container class="fill-height" fluid>
    <v-row justify="center" align="center" class="fill-height">
      <v-col cols="12" sm="8" md="4">
        <v-card class="pa-4" elevation="4">
          <v-card-title class="text-h5 mb-2">{{ t('app.name') }}</v-card-title>
          <v-form @submit.prevent="submit">
            <v-text-field
              v-model="email"
              :label="t('auth.email')"
              type="email"
              autocomplete="username"
              required
            />
            <v-text-field
              v-model="password"
              :label="t('auth.password')"
              type="password"
              autocomplete="current-password"
              required
            />
            <v-btn type="submit" color="primary" block size="large" :loading="loading">
              {{ t('auth.login') }}
            </v-btn>
          </v-form>
          <div class="text-center mt-4">
            <span class="text-medium-emphasis">{{ t('auth.noAccount') }}</span>
            <router-link to="/register" class="ml-1">{{ t('auth.registerTenant') }}</router-link>
          </div>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>
