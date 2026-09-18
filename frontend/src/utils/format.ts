/**
 * The API sends ISO-8601 UTC ("...Z"); the Date constructor converts to the
 * viewer's local zone, which is what we want to display.
 */
export function formatDate(value: string): string {
  return new Date(value).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

export function formatDateTime(value: string): string {
  return new Date(value).toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

const UNITS: Array<[Intl.RelativeTimeFormatUnit, number]> = [
  ['year', 1000 * 60 * 60 * 24 * 365],
  ['month', 1000 * 60 * 60 * 24 * 30],
  ['week', 1000 * 60 * 60 * 24 * 7],
  ['day', 1000 * 60 * 60 * 24],
  ['hour', 1000 * 60 * 60],
  ['minute', 1000 * 60],
]

/** "3 minutes ago" / "in 2 days", falling back to "Just now" under a minute. */
export function formatRelative(value: string): string {
  const diff = new Date(value).getTime() - Date.now()
  const absolute = Math.abs(diff)

  if (absolute < 60_000) return 'Just now'

  const formatter = new Intl.RelativeTimeFormat(undefined, { numeric: 'auto' })
  for (const [unit, ms] of UNITS) {
    if (absolute >= ms) {
      return formatter.format(Math.round(diff / ms), unit)
    }
  }

  return 'Just now'
}

/** Local YYYY-MM-DD, the format <input type="date"> expects. */
export function toDateInputValue(date: Date): string {
  const offset = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offset).toISOString().slice(0, 10)
}
