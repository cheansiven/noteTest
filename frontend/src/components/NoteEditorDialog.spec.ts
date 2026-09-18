import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { nextTick } from 'vue'
import NoteEditorDialog from './NoteEditorDialog.vue'
import type { Note } from '@/types'

const note: Note = {
  id: '11111111-1111-1111-1111-111111111111',
  title: 'Shopping list',
  content: 'Milk and eggs',
  createdAt: '2026-09-18T10:00:00.000Z',
  updatedAt: '2026-09-18T11:00:00.000Z',
}

function mountDialog(props: Partial<InstanceType<typeof NoteEditorDialog>['$props']> = {}) {
  return mount(NoteEditorDialog, {
    props: { open: true, note: null, saving: false, ...props },
    attachTo: document.body,
  })
}

const title = () => document.querySelector<HTMLInputElement>('#note-title')!
const content = () => document.querySelector<HTMLTextAreaElement>('#note-content')!
const submit = () =>
  Array.from(document.querySelectorAll<HTMLButtonElement>('button')).find(
    (b) => b.type === 'submit',
  )!

describe('NoteEditorDialog', () => {
  it('opens empty when creating', async () => {
    const wrapper = mountDialog()
    await nextTick()

    expect(title().value).toBe('')
    expect(content().value).toBe('')
    expect(submit().textContent).toContain('Create note')
    wrapper.unmount()
  })

  it('pre-fills the form when editing', async () => {
    const wrapper = mountDialog({ note })
    await nextTick()

    expect(title().value).toBe('Shopping list')
    expect(content().value).toBe('Milk and eggs')
    expect(submit().textContent).toContain('Save changes')
    wrapper.unmount()
  })

  it('will not save without a title', async () => {
    const wrapper = mountDialog()
    await nextTick()

    expect(submit().disabled).toBe(true)

    title().value = 'Something'
    title().dispatchEvent(new Event('input'))
    await nextTick()
    expect(submit().disabled).toBe(false)
    wrapper.unmount()
  })

  it('treats a whitespace-only title as missing', async () => {
    const wrapper = mountDialog()
    await nextTick()

    title().value = '    '
    title().dispatchEvent(new Event('input'))
    await nextTick()

    expect(submit().disabled).toBe(true)
    wrapper.unmount()
  })

  it('emits trimmed values, with empty content as null', async () => {
    const wrapper = mountDialog()
    await nextTick()

    title().value = '  Groceries  '
    title().dispatchEvent(new Event('input'))
    content().value = '   '
    content().dispatchEvent(new Event('input'))
    await nextTick()

    document.querySelector('form')!.dispatchEvent(new Event('submit'))
    await nextTick()

    expect(wrapper.emitted('save')?.[0]).toEqual([{ title: 'Groceries', content: null }])
    wrapper.unmount()
  })

  it('discards edits when reopened, so a cancelled edit leaves no trace', async () => {
    const wrapper = mountDialog({ note })
    await nextTick()

    title().value = 'Half-typed change'
    title().dispatchEvent(new Event('input'))
    await nextTick()

    await wrapper.setProps({ open: false })
    await wrapper.setProps({ open: true })
    await nextTick()

    expect(title().value).toBe('Shopping list')
    wrapper.unmount()
  })

  it('shows progress and blocks double submission while saving', async () => {
    const wrapper = mountDialog({ note, saving: true })
    await nextTick()

    expect(submit().textContent).toContain('Saving')
    expect(submit().disabled).toBe(true)
    wrapper.unmount()
  })
})
