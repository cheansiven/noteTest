import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { authService } from '@/services/authService'
import { session } from '@/lib/session'
import type { Credentials, RegisterPayload, User } from '@/types'

export const useAuthStore = defineStore('auth', () => {
  // Seed from localStorage so a reload does not bounce the user to /login.
  const restored = session.load()

  const user = ref<User | null>(restored?.user ?? null)
  const token = ref<string | null>(restored?.token ?? null)
  const loading = ref(false)

  const isAuthenticated = computed(() => token.value !== null)
  const initials = computed(() => {
    const name = user.value?.displayName?.trim()
    if (!name) return '?'
    return name
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase() ?? '')
      .join('')
  })

  async function login(credentials: Credentials): Promise<void> {
    loading.value = true
    try {
      const auth = await authService.login(credentials)
      session.save(auth)
      user.value = auth.user
      token.value = auth.token
    } finally {
      loading.value = false
    }
  }

  async function register(payload: RegisterPayload): Promise<void> {
    loading.value = true
    try {
      const auth = await authService.register(payload)
      session.save(auth)
      user.value = auth.user
      token.value = auth.token
    } finally {
      loading.value = false
    }
  }

  function logout(): void {
    session.clear()
    user.value = null
    token.value = null
  }

  /** Re-validates a restored token against the API and refreshes the profile. */
  async function refreshProfile(): Promise<void> {
    if (!token.value) return
    try {
      user.value = await authService.me()
    } catch {
      // A 401 already triggered logout via the axios interceptor; anything else
      // (e.g. API briefly down) should not destroy a usable session.
    }
  }

  return { user, token, loading, isAuthenticated, initials, login, register, logout, refreshProfile }
})
