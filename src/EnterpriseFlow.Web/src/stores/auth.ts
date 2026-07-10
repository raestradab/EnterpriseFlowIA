import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import * as authApi from '../api/auth'
import * as tokenStorage from '../api/tokenStorage'

export const useAuthStore = defineStore('auth', () => {
  const identity = tokenStorage.getIdentity()

  const isAuthenticated = ref(!!tokenStorage.getAccessToken())
  const userId = ref(identity.userId)
  const tenantId = ref(identity.tenantId)
  const permissions = ref<string[]>(identity.permissions)

  const hasPermission = computed(() => (permission: string) => permissions.value.includes(permission))

  async function loadMe() {
    const response = await authApi.getMyPermissions()
    userId.value = response.data.userId
    tenantId.value = response.data.tenantId
    permissions.value = response.data.permissions
    tokenStorage.setIdentity(response.data.userId, response.data.tenantId, response.data.permissions)
  }

  async function login(email: string, password: string) {
    const response = await authApi.login(email, password)
    tokenStorage.setAccessToken(response.data.accessToken)
    isAuthenticated.value = true
    await loadMe()
  }

  async function registerTenant(payload: {
    tenantName: string
    tenantSlug: string
    adminEmail: string
    adminPassword: string
  }) {
    await authApi.registerTenant(payload)
  }

  async function logout() {
    // Security review finding companion: an HttpOnly cookie is inaccessible to JS, but that
    // alone doesn't end the session server-side — without this call the refresh token would
    // stay valid for anyone holding the cookie until its 30-day expiry, "logout" or not.
    // Best-effort: local state must still clear even if the network call fails.
    try {
      await authApi.logout()
    } catch {
      // Ignored — clearing local state below is what actually logs the user out of this device.
    }

    tokenStorage.clearAll()
    isAuthenticated.value = false
    userId.value = null
    tenantId.value = null
    permissions.value = []
  }

  return { isAuthenticated, userId, tenantId, permissions, hasPermission, login, registerTenant, loadMe, logout }
})
