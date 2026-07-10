<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useTheme } from 'vuetify'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useNotificationCenterStore } from '../stores/notificationCenter'
import { startNotificationHubConnection } from '../realtime/notificationHub'

const theme = useTheme()
const { t, locale } = useI18n()
const router = useRouter()
const auth = useAuthStore()
const notificationCenter = useNotificationCenterStore()

const drawer = ref(true)

onMounted(() => {
  const savedTheme = localStorage.getItem('theme')
  if (savedTheme) {
    theme.change(savedTheme)
  }
  auth.loadMe().catch(() => {
    // /api/auth/me failing here just means a stale/invalid token slipped past the guard —
    // the axios interceptor already redirects to /login on 401, nothing else to do.
  })
  notificationCenter.load().catch(() => {
    // Same reasoning — a stale token is already handled by the axios interceptor.
  })
  startNotificationHubConnection()
})

const isDark = computed(() => theme.current.value.dark)

function toggleTheme() {
  const next = isDark.value ? 'light' : 'dark'
  theme.change(next)
  localStorage.setItem('theme', next)
}

function toggleLocale() {
  locale.value = locale.value === 'es' ? 'en' : 'es'
  localStorage.setItem('locale', locale.value)
}

// HU-005 (Dynamic Menu): each item only renders if the current user's permissions include it.
// `permission: null` means "always visible to an authenticated user" (Dashboard).
const menuItems = computed(() => [
  { title: t('nav.dashboard'), to: '/', icon: 'mdi-view-dashboard', permission: null },
  { title: t('nav.companies'), to: '/companies', icon: 'mdi-domain', permission: 'companies.read' },
  { title: t('nav.clients'), to: '/clients', icon: 'mdi-account-group', permission: 'clients.read' },
  { title: t('nav.projects'), to: '/projects', icon: 'mdi-briefcase-outline', permission: 'projects.read' },
  { title: t('nav.tasks'), to: '/tasks', icon: 'mdi-checkbox-marked-outline', permission: 'tasks.read' },
  { title: t('nav.catalogs'), to: '/catalogs', icon: 'mdi-format-list-bulleted', permission: 'catalogs.read' },
  { title: t('nav.workflows'), to: '/workflows', icon: 'mdi-sitemap-outline', permission: 'workflows.read' },
  { title: t('nav.assistant'), to: '/assistant', icon: 'mdi-robot-outline', permission: null },
])

const visibleMenuItems = computed(() =>
  menuItems.value.filter((item) => item.permission === null || auth.hasPermission(item.permission)),
)

async function logout() {
  await auth.logout()
  router.push({ name: 'login' })
}
</script>

<template>
  <v-navigation-drawer v-model="drawer" app>
    <v-list-item :title="t('app.name')" class="py-4" />
    <v-divider />
    <v-list nav>
      <v-list-item
        v-for="item in visibleMenuItems"
        :key="item.to"
        :to="item.to"
        :prepend-icon="item.icon"
        :title="item.title"
        exact
      />
    </v-list>
  </v-navigation-drawer>

  <v-app-bar app flat border>
    <v-app-bar-nav-icon @click="drawer = !drawer" />
    <v-app-bar-title>{{ t('app.name') }}</v-app-bar-title>
    <v-spacer />
    <v-btn variant="text" @click="toggleLocale">{{ locale.toUpperCase() }}</v-btn>

    <v-menu location="bottom end">
      <template #activator="{ props }">
        <v-btn icon variant="text" v-bind="props">
          <v-badge v-if="notificationCenter.unreadCount > 0" :content="notificationCenter.unreadCount" color="error">
            <v-icon>mdi-bell-outline</v-icon>
          </v-badge>
          <v-icon v-else>mdi-bell-outline</v-icon>
        </v-btn>
      </template>
      <v-card min-width="320" max-width="420">
        <v-list v-if="notificationCenter.items.length" density="compact">
          <v-list-item
            v-for="n in notificationCenter.items"
            :key="n.id"
            :title="n.message"
            :subtitle="n.eventName"
            :class="{ 'font-weight-bold': !n.isRead }"
            @click="!n.isRead && notificationCenter.markRead(n.id)"
          />
        </v-list>
        <v-card-text v-else class="text-medium-emphasis">{{ t('common.noData') }}</v-card-text>
      </v-card>
    </v-menu>

    <v-btn :icon="isDark ? 'mdi-weather-sunny' : 'mdi-weather-night'" variant="text" @click="toggleTheme" />
    <v-btn :icon="'mdi-logout'" variant="text" @click="logout" />
  </v-app-bar>

  <v-main>
    <v-container fluid>
      <router-view />
    </v-container>
  </v-main>
</template>
