import { ref, type Ref } from 'vue'
import { toErrorMessage } from '@/lib/http'
import { useToastStore } from '@/stores/toast'

interface AsyncActionOptions {
  /** Toast shown on success. Omit for actions that speak for themselves. */
  successMessage?: string | (() => string)
  /** Fallback used when the server sends no usable problem detail. */
  errorMessage: string
  /** Runs only when the action succeeded. */
  onSuccess?: () => void | Promise<void>
  /** Runs only when it failed, after the toast. */
  onError?: (error: unknown) => void | Promise<void>
}

interface AsyncAction<TArgs extends unknown[]> {
  run: (...args: TArgs) => Promise<boolean>
  pending: Ref<boolean>
}

/**
 * Wraps an async operation with the handling every user-triggered action needs:
 * a pending flag, a success toast, and a failure toast built from the API's
 * ProblemDetails. Returns whether it succeeded so callers can branch without
 * writing their own try/catch.
 *
 * Without this, the same five-line try/catch is repeated for every button, and it
 * only takes one missed catch for a failed save to look like a successful one.
 */
export function useAsyncAction<TArgs extends unknown[]>(
  action: (...args: TArgs) => Promise<unknown>,
  options: AsyncActionOptions,
): AsyncAction<TArgs> {
  const toasts = useToastStore()
  const pending = ref(false)

  async function run(...args: TArgs): Promise<boolean> {
    pending.value = true
    try {
      await action(...args)

      if (options.successMessage) {
        toasts.success(
          typeof options.successMessage === 'function'
            ? options.successMessage()
            : options.successMessage,
        )
      }

      await options.onSuccess?.()
      return true
    } catch (error) {
      toasts.error(toErrorMessage(error, options.errorMessage))
      await options.onError?.(error)
      return false
    } finally {
      pending.value = false
    }
  }

  return { run, pending }
}
