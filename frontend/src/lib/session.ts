import type { AuthResponse, User } from '@/types'

const STORAGE_KEY = 'notes.session'

export interface StoredSession {
  token: string
  expiresAt: string
  user: User
}

/**
 * The session lives in localStorage so a page reload keeps the user signed in.
 * It is kept in its own module (rather than in the Pinia store) so the axios
 * interceptor can read the token without importing the store - that would create
 * a circular dependency between the store and the HTTP client.
 */
export const session = {
  load(): StoredSession | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY)
      if (!raw) return null

      const parsed = JSON.parse(raw) as StoredSession
      if (!parsed?.token || !parsed?.user) return null

      // Drop an expired token instead of firing a doomed request with it.
      if (new Date(parsed.expiresAt).getTime() <= Date.now()) {
        localStorage.removeItem(STORAGE_KEY)
        return null
      }

      return parsed
    } catch {
      // Corrupt payload or storage unavailable (private mode) - start signed out.
      return null
    }
  },

  save(auth: AuthResponse): StoredSession {
    const value: StoredSession = { token: auth.token, expiresAt: auth.expiresAt, user: auth.user }
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(value))
    } catch {
      /* non-fatal: the session simply will not survive a reload */
    }
    return value
  },

  clear(): void {
    try {
      localStorage.removeItem(STORAGE_KEY)
    } catch {
      /* ignore */
    }
  },
}
