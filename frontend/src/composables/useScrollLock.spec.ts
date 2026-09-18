import { mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it } from 'vitest'
import { defineComponent, ref, type Ref } from 'vue'
import { __resetScrollLock, useScrollLock } from './useScrollLock'

function mountWithLock(active: Ref<boolean>) {
  return mount(
    defineComponent({
      setup() {
        useScrollLock(active)
        return () => null
      },
    }),
  )
}

describe('useScrollLock', () => {
  beforeEach(() => __resetScrollLock())

  it('locks the body while open and releases it on close', async () => {
    const active = ref(false)
    mountWithLock(active)

    expect(document.body.style.overflow).toBe('')

    active.value = true
    await Promise.resolve()
    expect(document.body.style.overflow).toBe('hidden')

    active.value = false
    await Promise.resolve()
    expect(document.body.style.overflow).toBe('')
  })

  it('keeps the page locked while a second dialog is still open', async () => {
    // Regression: deleting from the note detail view stacks two dialogs. With a plain
    // boolean flag, closing the inner one restored scrolling while the outer one was
    // still covering the page.
    const outer = ref(true)
    const inner = ref(true)
    mountWithLock(outer)
    mountWithLock(inner)
    await Promise.resolve()

    expect(document.body.style.overflow).toBe('hidden')

    inner.value = false
    await Promise.resolve()
    expect(document.body.style.overflow).toBe('hidden')

    outer.value = false
    await Promise.resolve()
    expect(document.body.style.overflow).toBe('')
  })

  it('releases the lock if the component unmounts while still open', async () => {
    const active = ref(true)
    const wrapper = mountWithLock(active)
    await Promise.resolve()
    expect(document.body.style.overflow).toBe('hidden')

    wrapper.unmount()
    expect(document.body.style.overflow).toBe('')
  })
})
