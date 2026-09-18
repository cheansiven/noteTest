import { defineStore } from 'pinia'
import { ref } from 'vue'

export type ToastKind = 'success' | 'error' | 'info'

export interface Toast {
  id: number
  kind: ToastKind
  message: string
}

let nextId = 1

export const useToastStore = defineStore('toast', () => {
  const toasts = ref<Toast[]>([])

  function push(message: string, kind: ToastKind = 'info', timeout = 4000): void {
    const id = nextId++
    toasts.value.push({ id, kind, message })
    window.setTimeout(() => dismiss(id), timeout)
  }

  function dismiss(id: number): void {
    toasts.value = toasts.value.filter((toast) => toast.id !== id)
  }

  const success = (message: string) => push(message, 'success')
  const error = (message: string) => push(message, 'error', 6000)
  const info = (message: string) => push(message, 'info')

  return { toasts, push, dismiss, success, error, info }
})
