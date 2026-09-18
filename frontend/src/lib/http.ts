import axios, { type AxiosError } from 'axios'
import { session } from '@/lib/session'

export const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5215/api',
  headers: { 'Content-Type': 'application/json' },
  timeout: 15_000,
})

let onUnauthorized: (() => void) | null = null

/** Lets the auth store react to an expired/invalid token without a circular import. */
export function setUnauthorizedHandler(handler: () => void): void {
  onUnauthorized = handler
}

http.interceptors.request.use((config) => {
  const current = session.load()
  if (current) {
    config.headers.Authorization = `Bearer ${current.token}`
  }
  return config
})

http.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    // 401 on any endpoint other than the login/register calls themselves means
    // the stored token is no longer good - sign the user out.
    const url = error.config?.url ?? ''
    const isAuthAttempt = url.includes('/auth/login') || url.includes('/auth/register')

    if (error.response?.status === 401 && !isAuthAttempt) {
      session.clear()
      onUnauthorized?.()
    }

    return Promise.reject(error)
  },
)

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

/** Turns an axios failure into a single sentence suitable for a toast. */
export function toErrorMessage(error: unknown, fallback = 'Something went wrong.'): string {
  if (!axios.isAxiosError(error)) {
    return error instanceof Error ? error.message : fallback
  }

  if (error.code === 'ECONNABORTED') return 'The request timed out. Please try again.'
  if (!error.response) return 'Cannot reach the server. Is the API running?'

  const problem = error.response.data as ProblemDetails | undefined

  // Model-validation failures arrive as { errors: { Field: ["msg"] } }.
  if (problem?.errors) {
    const first = Object.values(problem.errors).flat().find(Boolean)
    if (first) return first
  }

  return problem?.detail || problem?.title || fallback
}
