import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import * as tokenStorage from './tokenStorage'
import type { LoginResult } from '../types'

// withCredentials: the refresh token now travels as an HttpOnly cookie (security review
// finding — it used to be a 30-day credential sitting in localStorage), so every request needs
// to include cookies for the server to read it back on /auth/refresh.
export const apiClient = axios.create({ baseURL: '/api', withCredentials: true })

apiClient.interceptors.request.use((config) => {
  const token = tokenStorage.getAccessToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Single-flight refresh: if several requests 401 at once (e.g. a page firing multiple calls
// right as the access token expires), they must all wait for ONE refresh call, not each
// trigger their own — the refresh token is single-use and rotates (HU-002), so a second
// concurrent refresh attempt would always fail reuse detection.
let refreshPromise: Promise<string> | null = null

async function refreshAccessToken(): Promise<string> {
  refreshPromise ??= axios
    .post<LoginResult>('/api/auth/refresh', null, { withCredentials: true })
    .then((response) => {
      tokenStorage.setAccessToken(response.data.accessToken)
      return response.data.accessToken
    })
    .finally(() => {
      refreshPromise = null
    })

  return refreshPromise
}

interface RetryableConfig extends InternalAxiosRequestConfig {
  _retried?: boolean
}

const AUTH_ENDPOINTS_WITHOUT_RETRY = ['/auth/login', '/auth/register-tenant', '/auth/refresh']

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const config = error.config as RetryableConfig | undefined
    const isAuthEndpoint = AUTH_ENDPOINTS_WITHOUT_RETRY.some((path) => config?.url?.startsWith(path))

    if (error.response?.status !== 401 || !config || isAuthEndpoint) {
      return Promise.reject(error)
    }

    // The refresh token cookie is HttpOnly — there's no client-side way to check whether one
    // exists before trying, unlike before when tokenStorage could report it directly. The
    // server rejects with 401 if there's no valid cookie, which the catch below handles.
    if (!config._retried) {
      config._retried = true
      try {
        const newAccessToken = await refreshAccessToken()
        config.headers.Authorization = `Bearer ${newAccessToken}`
        return apiClient(config)
      } catch {
        // Refresh itself failed (expired/reused/revoked/absent) — fall through to logout below.
      }
    }

    tokenStorage.clearAll()
    window.location.href = '/login'
    return Promise.reject(error)
  },
)
