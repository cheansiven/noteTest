import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useAsyncAction } from './useAsyncAction'
import { useToastStore } from '@/stores/toast'

describe('useAsyncAction', () => {
  beforeEach(() => setActivePinia(createPinia()))

  it('reports success and shows the success toast', async () => {
    const toasts = useToastStore()
    const action = useAsyncAction(() => Promise.resolve('ok'), {
      successMessage: 'Saved.',
      errorMessage: 'Could not save.',
    })

    await expect(action.run()).resolves.toBe(true)
    expect(toasts.toasts.map((t) => t.message)).toContain('Saved.')
  })

  it('turns a rejection into a toast instead of an unhandled error', async () => {
    const toasts = useToastStore()
    const action = useAsyncAction(() => Promise.reject(new Error('network down')), {
      errorMessage: 'Could not save.',
    })

    await expect(action.run()).resolves.toBe(false)
    expect(toasts.toasts.map((t) => t.kind)).toContain('error')
  })

  it('tracks pending state across the call', async () => {
    let release: (() => void) | undefined
    const action = useAsyncAction(
      () => new Promise<void>((resolve) => { release = resolve }),
      { errorMessage: 'Failed.' },
    )

    expect(action.pending.value).toBe(false)
    const running = action.run()
    expect(action.pending.value).toBe(true)

    release?.()
    await running
    expect(action.pending.value).toBe(false)
  })

  it('clears pending even when the action throws', async () => {
    const action = useAsyncAction(() => Promise.reject(new Error('boom')), {
      errorMessage: 'Failed.',
    })

    await action.run()

    expect(action.pending.value).toBe(false)
  })

  it('runs onSuccess only on success and onError only on failure', async () => {
    const onSuccess = vi.fn<() => void>()
    const onError = vi.fn<(error: unknown) => void>()

    await useAsyncAction(() => Promise.resolve(), {
      errorMessage: 'x',
      onSuccess,
      onError,
    }).run()

    expect(onSuccess).toHaveBeenCalledOnce()
    expect(onError).not.toHaveBeenCalled()

    await useAsyncAction(() => Promise.reject(new Error('no')), {
      errorMessage: 'x',
      onSuccess,
      onError,
    }).run()

    expect(onSuccess).toHaveBeenCalledOnce()
    expect(onError).toHaveBeenCalledOnce()
  })

  it('passes arguments through to the wrapped action', async () => {
    const spy = vi.fn<(id: string, flag: boolean) => Promise<void>>().mockResolvedValue()
    const action = useAsyncAction(spy, { errorMessage: 'x' })

    await action.run('note-1', true)

    expect(spy).toHaveBeenCalledWith('note-1', true)
  })

  it('builds the success message lazily, so it can read current state', async () => {
    const toasts = useToastStore()
    let title = 'first'
    const action = useAsyncAction(() => Promise.resolve(), {
      successMessage: () => `Deleted "${title}".`,
      errorMessage: 'x',
    })

    title = 'second'
    await action.run()

    expect(toasts.toasts.map((t) => t.message)).toContain('Deleted "second".')
  })
})
