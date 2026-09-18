import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { defineComponent, nextTick, ref } from 'vue'
import { useFocusTrap } from './useFocusTrap'

const Dialog = defineComponent({
  setup() {
    const panel = ref<HTMLElement | null>(null)
    const isOpen = ref(false)
    useFocusTrap(panel, isOpen)
    return { panel, isOpen }
  },
  template: `
    <div v-if="isOpen" ref="panel">
      <button id="first">First</button>
      <button id="middle" data-autofocus>Middle</button>
      <button id="last">Last</button>
    </div>
  `,
})

function pressTab(shiftKey = false): KeyboardEvent {
  const event = new KeyboardEvent('keydown', { key: 'Tab', shiftKey, bubbles: true, cancelable: true })
  document.dispatchEvent(event)
  return event
}

describe('useFocusTrap', () => {
  it('moves focus to the element marked data-autofocus', async () => {
    const wrapper = mount(Dialog, { attachTo: document.body })
    wrapper.vm.isOpen = true
    await nextTick()
    await nextTick()

    expect(document.activeElement?.id).toBe('middle')
    wrapper.unmount()
  })

  it('wraps focus from the last element back to the first', async () => {
    const wrapper = mount(Dialog, { attachTo: document.body })
    wrapper.vm.isOpen = true
    await nextTick()
    await nextTick()

    document.querySelector<HTMLElement>('#last')!.focus()
    const event = pressTab()

    // Without the trap this Tab would land on the page behind the dialog.
    expect(event.defaultPrevented).toBe(true)
    expect(document.activeElement?.id).toBe('first')
    wrapper.unmount()
  })

  it('wraps backwards from the first element to the last', async () => {
    const wrapper = mount(Dialog, { attachTo: document.body })
    wrapper.vm.isOpen = true
    await nextTick()
    await nextTick()

    document.querySelector<HTMLElement>('#first')!.focus()
    const event = pressTab(true)

    expect(event.defaultPrevented).toBe(true)
    expect(document.activeElement?.id).toBe('last')
    wrapper.unmount()
  })

  it('pulls focus back in if it somehow escaped the dialog', async () => {
    const outside = document.createElement('button')
    document.body.appendChild(outside)

    const wrapper = mount(Dialog, { attachTo: document.body })
    wrapper.vm.isOpen = true
    await nextTick()
    await nextTick()

    outside.focus()
    pressTab()

    expect(document.activeElement?.id).toBe('first')
    wrapper.unmount()
    outside.remove()
  })

  it('returns focus to whatever opened the dialog', async () => {
    const trigger = document.createElement('button')
    document.body.appendChild(trigger)
    trigger.focus()

    const wrapper = mount(Dialog, { attachTo: document.body })
    wrapper.vm.isOpen = true
    await nextTick()
    await nextTick()
    expect(document.activeElement?.id).toBe('middle')

    wrapper.vm.isOpen = false
    await nextTick()

    // Otherwise the user is dumped back at the top of the document.
    expect(document.activeElement).toBe(trigger)
    wrapper.unmount()
    trigger.remove()
  })
})
