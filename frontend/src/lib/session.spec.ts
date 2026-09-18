import { beforeEach, describe, expect, it, vi } from 'vitest'
import { session } from './session'
import type { AuthResponse } from '@/types'

const KEY = 'notes.session'

function authResponse(expiresAt: string): AuthResponse {
  return {
    token: 'a.b.c',
    expiresAt,
    user: {
      id: '11111111-1111-1111-1111-111111111111',
      email: 'user@example.com',
      displayName: 'Test User',
      createdAt: '2026-09-18T10:00:00.000Z',
    },
  }
}

const future = new Date(Date.now() + 60 * 60 * 1000).toISOString()
const past = new Date(Date.now() - 1000).toISOString()

describe('session', () => {
  beforeEach(() => localStorage.clear())

  it('round-trips a saved session', () => {
    session.save(authResponse(future))

    const loaded = session.load()

    expect(loaded?.token).toBe('a.b.c')
    expect(loaded?.user.email).toBe('user@example.com')
  })

  it('returns null when nothing is stored', () => {
    expect(session.load()).toBeNull()
  })

  it('discards an expired token instead of sending a doomed request', () => {
    session.save(authResponse(past))

    expect(session.load()).toBeNull()
    // Also cleared from storage, so it is not re-examined on every request.
    expect(localStorage.getItem(KEY)).toBeNull()
  })

  it('survives corrupt stored data rather than throwing on boot', () => {
    localStorage.setItem(KEY, 'not json at all')

    expect(session.load()).toBeNull()
  })

  it('treats a structurally wrong payload as no session', () => {
    localStorage.setItem(KEY, JSON.stringify({ token: '', user: null }))

    expect(session.load()).toBeNull()
  })

  it('clear removes the stored session', () => {
    session.save(authResponse(future))
    session.clear()

    expect(session.load()).toBeNull()
  })

  it('stays usable when storage is unavailable, as in a private window', () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('QuotaExceededError')
    })
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new DOMException('SecurityError')
    })

    // Saving must not throw, and loading reports "signed out" rather than crashing.
    expect(() => session.save(authResponse(future))).not.toThrow()
    expect(session.load()).toBeNull()
  })
})
