import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useNotesStore } from './notes'
import { noteService } from '@/services/noteService'
import type { NoteListItem, NoteQuery, PagedResult } from '@/types'

vi.mock('@/services/noteService', () => ({
  noteService: {
    list: vi.fn(),
    get: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    remove: vi.fn(),
  },
}))

const listed = vi.mocked(noteService.list)

function item(id: string, title = 'Note'): NoteListItem {
  return {
    id,
    title,
    preview: null,
    hasContent: false,
    createdAt: '2026-09-18T10:00:00.000Z',
    updatedAt: '2026-09-18T10:00:00.000Z',
  }
}

function page(items: NoteListItem[], overrides: Partial<PagedResult<NoteListItem>> = {}) {
  const totalCount = overrides.totalCount ?? items.length
  const pageSize = overrides.pageSize ?? 12
  const totalPages = Math.ceil(totalCount / pageSize)
  return {
    items,
    page: overrides.page ?? 1,
    pageSize,
    totalCount,
    totalPages,
    hasPrevious: (overrides.page ?? 1) > 1,
    hasNext: (overrides.page ?? 1) < totalPages,
    ...overrides,
  } as PagedResult<NoteListItem>
}

describe('notes store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('loads notes and exposes the paging metadata', async () => {
    listed.mockResolvedValue(page([item('1'), item('2')], { totalCount: 30 }))
    const notes = useNotesStore()

    await notes.fetchNotes()

    expect(notes.items).toHaveLength(2)
    expect(notes.totalCount).toBe(30)
    expect(notes.loading).toBe(false)
    expect(notes.error).toBeNull()
  })

  it('surfaces a load failure as an error message and an empty list', async () => {
    listed.mockRejectedValue(new Error('offline'))
    const notes = useNotesStore()

    await notes.fetchNotes()

    expect(notes.error).toBeTruthy()
    expect(notes.items).toEqual([])
    expect(notes.loading).toBe(false)
  })

  it('resets to page 1 for any change other than paging', async () => {
    listed.mockResolvedValue(page([]))
    const notes = useNotesStore()
    await notes.applyQuery({ page: 3 })

    await notes.applyQuery({ search: 'groceries' })

    expect(notes.query.page).toBe(1)
    expect(notes.query.search).toBe('groceries')
  })

  it('keeps the page when only the page changes', async () => {
    listed.mockResolvedValue(page([], { page: 2 }))
    const notes = useNotesStore()

    await notes.applyQuery({ page: 2 })

    expect(notes.query.page).toBe(2)
  })

  it('steps back when the last note on a page is deleted', async () => {
    const notes = useNotesStore()
    notes.query.page = 3
    // Page 3 is now empty but rows still exist, which is what a delete leaves behind.
    listed
      .mockResolvedValueOnce(page([], { page: 3, totalCount: 13, pageSize: 12 }))
      .mockResolvedValueOnce(page([item('1')], { page: 2, totalCount: 13, pageSize: 12 }))

    await notes.fetchNotes()

    expect(notes.query.page).toBe(2)
    expect(notes.items).toHaveLength(1)
    expect(listed).toHaveBeenCalledTimes(2)
  })

  it('does not step back when the list is genuinely empty', async () => {
    listed.mockResolvedValue(page([], { totalCount: 0 }))
    const notes = useNotesStore()

    await notes.fetchNotes()

    expect(listed).toHaveBeenCalledTimes(1)
    expect(notes.isEmpty).toBe(true)
  })

  it('ignores a superseded request so fast typing cannot show stale results', async () => {
    const notes = useNotesStore()
    listed.mockImplementation((_query: NoteQuery, signal?: AbortSignal) =>
      new Promise((resolve, reject) => {
        signal?.addEventListener('abort', () =>
          reject(Object.assign(new Error('canceled'), { __CANCEL__: true })),
        )
        setTimeout(() => resolve(page([item('slow', 'Slow')])), 20)
      }),
    )

    const first = notes.fetchNotes()
    listed.mockResolvedValueOnce(page([item('fast', 'Fast')]))
    const second = notes.fetchNotes()
    await Promise.all([first, second])

    expect(notes.items.map((i) => i.title)).toEqual(['Fast'])
    expect(notes.error).toBeNull()
  })

  it('reports whether any filter is active', async () => {
    listed.mockResolvedValue(page([]))
    const notes = useNotesStore()

    expect(notes.hasActiveFilters).toBe(false)

    await notes.applyQuery({ search: 'x' })
    expect(notes.hasActiveFilters).toBe(true)

    await notes.resetQuery()
    expect(notes.hasActiveFilters).toBe(false)
  })

  it('sorting alone does not count as a filter', async () => {
    listed.mockResolvedValue(page([]))
    const notes = useNotesStore()

    await notes.applyQuery({ sortBy: 'Title', sortDirection: 'Asc' })

    expect(notes.hasActiveFilters).toBe(false)
  })

  it('hydrateQuery applies state without loading', () => {
    const notes = useNotesStore()

    notes.hydrateQuery({ search: 'from url', page: 4 })

    expect(notes.query.search).toBe('from url')
    expect(notes.query.page).toBe(4)
    expect(listed).not.toHaveBeenCalled()
  })

  it('hydrateQuery clears anything absent from the URL', () => {
    const notes = useNotesStore()
    notes.hydrateQuery({ search: 'first', hasContent: true })

    notes.hydrateQuery({ page: 2 })

    expect(notes.query.search).toBe('')
    expect(notes.query.hasContent).toBeUndefined()
    expect(notes.query.page).toBe(2)
  })

  it('creating a note returns to the first page', async () => {
    listed.mockResolvedValue(page([]))
    vi.mocked(noteService.create).mockResolvedValue({
      id: 'new', title: 'New', content: null,
      createdAt: '2026-09-18T10:00:00.000Z', updatedAt: '2026-09-18T10:00:00.000Z',
    })
    const notes = useNotesStore()
    notes.query.page = 5

    await notes.createNote({ title: 'New', content: null })

    expect(notes.query.page).toBe(1)
  })

  it('reset clears the list and the filters', async () => {
    listed.mockResolvedValue(page([item('1')]))
    const notes = useNotesStore()
    await notes.applyQuery({ search: 'x' })

    notes.reset()

    expect(notes.items).toEqual([])
    expect(notes.totalCount).toBe(0)
    expect(notes.query.search).toBe('')
  })
})
