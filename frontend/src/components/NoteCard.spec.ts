import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import NoteCard from './NoteCard.vue'
import type { NoteListItem } from '@/types'

function note(overrides: Partial<NoteListItem> = {}): NoteListItem {
  return {
    id: '11111111-1111-1111-1111-111111111111',
    title: 'Shopping list',
    preview: 'Milk and eggs',
    hasContent: true,
    createdAt: '2026-09-18T10:00:00.000Z',
    updatedAt: '2026-09-18T10:00:00.000Z',
    ...overrides,
  }
}

describe('NoteCard', () => {
  it('shows the title, preview and creation date', () => {
    const wrapper = mount(NoteCard, { props: { note: note() } })

    expect(wrapper.text()).toContain('Shopping list')
    expect(wrapper.text()).toContain('Milk and eggs')
    expect(wrapper.text()).toContain('Created Sep 18, 2026')
  })

  it('marks a note with no body rather than showing a blank area', () => {
    const wrapper = mount(NoteCard, { props: { note: note({ hasContent: false, preview: null }) } })

    expect(wrapper.text()).toContain('No content')
  })

  it('hides the edited line for a note that has never changed', () => {
    const wrapper = mount(NoteCard, { props: { note: note() } })

    expect(wrapper.text()).not.toContain('Edited')
  })

  it('shows when an edited note was last changed', () => {
    const wrapper = mount(NoteCard, {
      props: { note: note({ updatedAt: '2026-09-19T10:00:00.000Z' }) },
    })

    expect(wrapper.text()).toContain('Edited')
  })

  it('emits open when the body is activated', async () => {
    const wrapper = mount(NoteCard, { props: { note: note() } })

    await wrapper.findAll('button')[0].trigger('click')

    expect(wrapper.emitted('open')).toHaveLength(1)
  })

  it('emits edit and remove from the action buttons', async () => {
    const wrapper = mount(NoteCard, { props: { note: note() } })

    await wrapper.get('[aria-label="Edit Shopping list"]').trigger('click')
    await wrapper.get('[aria-label="Delete Shopping list"]').trigger('click')

    expect(wrapper.emitted('edit')).toHaveLength(1)
    expect(wrapper.emitted('remove')).toHaveLength(1)
  })

  it('gives the action buttons labels that name the note', () => {
    // Otherwise a screen reader announces a page full of identical "Edit" buttons.
    const wrapper = mount(NoteCard, { props: { note: note({ title: 'Standup notes' }) } })

    const labels = wrapper
      .findAll('footer button')
      .map((button) => button.attributes('aria-label'))

    expect(labels).toEqual(['Edit Standup notes', 'Delete Standup notes'])
  })
})
