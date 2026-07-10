// Plain localStorage-backed token storage, deliberately independent of Pinia and the axios
// client — both need it (client.ts for attaching/refreshing the Bearer token, stores/auth.ts
// for reactive state), and having neither depend on the other avoids a circular import between
// "the HTTP client needs the store" and "the store needs the HTTP client".
//
// Security review finding: the refresh token used to live here too (REFRESH_TOKEN_KEY), a
// 30-day credential readable by any JavaScript running on the page — including an XSS payload.
// It now lives only in an HttpOnly cookie the server sets directly (see AuthEndpoints.cs),
// which this module has no access to and never needs to.

const ACCESS_TOKEN_KEY = 'ef_access_token'
const USER_ID_KEY = 'ef_user_id'
const TENANT_ID_KEY = 'ef_tenant_id'
const PERMISSIONS_KEY = 'ef_permissions'

export function getAccessToken(): string | null {
  return localStorage.getItem(ACCESS_TOKEN_KEY)
}

export function setAccessToken(accessToken: string): void {
  localStorage.setItem(ACCESS_TOKEN_KEY, accessToken)
}

export function getIdentity(): { userId: string | null; tenantId: string | null; permissions: string[] } {
  const raw = localStorage.getItem(PERMISSIONS_KEY)
  return {
    userId: localStorage.getItem(USER_ID_KEY),
    tenantId: localStorage.getItem(TENANT_ID_KEY),
    permissions: raw ? (JSON.parse(raw) as string[]) : [],
  }
}

export function setIdentity(userId: string, tenantId: string, permissions: string[]): void {
  localStorage.setItem(USER_ID_KEY, userId)
  localStorage.setItem(TENANT_ID_KEY, tenantId)
  localStorage.setItem(PERMISSIONS_KEY, JSON.stringify(permissions))
}

export function clearAll(): void {
  localStorage.removeItem(ACCESS_TOKEN_KEY)
  localStorage.removeItem(USER_ID_KEY)
  localStorage.removeItem(TENANT_ID_KEY)
  localStorage.removeItem(PERMISSIONS_KEY)
}
