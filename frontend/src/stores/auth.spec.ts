import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useAuthStore } from './auth'
import { session } from '@/lib/session'
import { authService } from '@/services/authService'
import type { AuthResponse } from '@/types'

vi.mock('@/services/authService', () => ({
  authService: { register: vi.fn(), login: vi.fn(), me: vi.fn() },
}))

const auth: AuthResponse = {
  token: 'a.b.c',
  expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
  user: {
    id: '11111111-1111-1111-1111-111111111111',
    email: 'user@example.com',
    displayName: 'Ada Lovelace',
    createdAt: '2026-09-18T10:00:00.000Z',
  },
}

describe('auth store', () => {
  beforeEach(() => {
    localStorage.clear()
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('starts signed out when nothing is stored', () => {
    expect(useAuthStore().isAuthenticated).toBe(false)
  })

  it('restores a stored session so a reload stays signed in', () => {
    session.save(auth)

    const store = useAuthStore()

    expect(store.isAuthenticated).toBe(true)
    expect(store.user?.email).toBe('user@example.com')
  })

  it('persists the session on login', async () => {
    vi.mocked(authService.login).mockResolvedValue(auth)
    const store = useAuthStore()

    await store.login({ email: 'user@example.com', password: 'Passw0rd!23' })

    expect(store.isAuthenticated).toBe(true)
    expect(session.load()?.token).toBe('a.b.c')
  })

  it('leaves the user signed out when login fails', async () => {
    vi.mocked(authService.login).mockRejectedValue(new Error('bad credentials'))
    const store = useAuthStore()

    await expect(
      store.login({ email: 'user@example.com', password: 'wrong' }),
    ).rejects.toThrow()

    expect(store.isAuthenticated).toBe(false)
    expect(session.load()).toBeNull()
    expect(store.loading).toBe(false)
  })

  it('logout clears both the store and storage', async () => {
    vi.mocked(authService.login).mockResolvedValue(auth)
    const store = useAuthStore()
    await store.login({ email: 'user@example.com', password: 'Passw0rd!23' })

    store.logout()

    expect(store.isAuthenticated).toBe(false)
    expect(store.user).toBeNull()
    expect(session.load()).toBeNull()
  })

  it('derives initials for the avatar', async () => {
    vi.mocked(authService.login).mockResolvedValue(auth)
    const store = useAuthStore()

    expect(store.initials).toBe('?')

    await store.login({ email: 'user@example.com', password: 'Passw0rd!23' })
    expect(store.initials).toBe('AL')
  })

  it('uses a single initial for a one-word name', async () => {
    vi.mocked(authService.login).mockResolvedValue({
      ...auth,
      user: { ...auth.user, displayName: 'Ada' },
    })
    const store = useAuthStore()

    await store.login({ email: 'user@example.com', password: 'Passw0rd!23' })

    expect(store.initials).toBe('A')
  })

  it('refreshProfile does nothing when signed out', async () => {
    await useAuthStore().refreshProfile()

    expect(authService.me).not.toHaveBeenCalled()
  })

  it('refreshProfile keeps the session when the API is briefly unreachable', async () => {
    session.save(auth)
    vi.mocked(authService.me).mockRejectedValue(new Error('network'))
    const store = useAuthStore()

    await store.refreshProfile()

    // A 401 signs the user out via the axios interceptor; anything else must not.
    expect(store.isAuthenticated).toBe(true)
  })
})
