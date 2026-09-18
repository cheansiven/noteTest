import { afterEach, describe, expect, it, vi } from 'vitest'
import { formatDate, formatDateTime, formatRelative, toDateInputValue } from './format'

// The suite runs with TZ=UTC (vitest.setup.ts) so these assertions are stable anywhere.
describe('formatDate', () => {
  it('renders the calendar day of a UTC instant', () => {
    expect(formatDate('2026-09-18T10:30:00.000Z')).toBe('Sep 18, 2026')
  })

  it('uses the instant, not the raw string, near midnight', () => {
    expect(formatDate('2026-09-18T23:59:59.999Z')).toBe('Sep 18, 2026')
    expect(formatDate('2026-09-19T00:00:00.000Z')).toBe('Sep 19, 2026')
  })
})

describe('formatDateTime', () => {
  it('includes both the date and the time', () => {
    const formatted = formatDateTime('2026-09-18T14:05:00.000Z')

    expect(formatted).toContain('Sep 18, 2026')
    expect(formatted).toMatch(/02:05|14:05/)
  })
})

describe('formatRelative', () => {
  const now = new Date('2026-09-18T12:00:00.000Z')

  afterEach(() => vi.useRealTimers())

  function at(iso: string): string {
    vi.useFakeTimers()
    vi.setSystemTime(now)
    return formatRelative(iso)
  }

  it('calls anything under a minute "Just now"', () => {
    expect(at('2026-09-18T11:59:30.000Z')).toBe('Just now')
    expect(at('2026-09-18T12:00:00.000Z')).toBe('Just now')
  })

  it('describes the recent past', () => {
    expect(at('2026-09-18T11:30:00.000Z')).toBe('30 minutes ago')
    expect(at('2026-09-18T09:00:00.000Z')).toBe('3 hours ago')
    expect(at('2026-09-16T12:00:00.000Z')).toBe('2 days ago')
  })

  it('describes the future too, which matters if a client clock runs slow', () => {
    expect(at('2026-09-18T13:00:00.000Z')).toBe('in 1 hour')
  })

  it('picks the largest unit that fits', () => {
    expect(at('2025-09-18T12:00:00.000Z')).toBe('last year')
  })
})

describe('toDateInputValue', () => {
  it('produces the YYYY-MM-DD an <input type="date"> expects', () => {
    expect(toDateInputValue(new Date('2026-09-18T10:00:00.000Z'))).toBe('2026-09-18')
  })

  it('uses the local calendar day rather than the UTC one', () => {
    // Built from local parts, so the output must match those parts back.
    const local = new Date(2026, 0, 5, 23, 30)

    expect(toDateInputValue(local)).toBe('2026-01-05')
  })
})
