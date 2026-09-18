import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent } from 'vue'
import { createRouter, createWebHistory, type Router } from 'vue-router'
import { useNotesUrlSync } from './useNotesUrlSync'
import { noteService } from '@/services/noteService'
import { useNotesStore } from '@/stores/notes'

vi.mock('@/services/noteService', () => ({
  noteService: { list: vi.fn(), get: vi.fn(), create: vi.fn(), update: vi.fn(), remove: vi.fn() },
}))

const emptyPage = {
  items: [], page: 1, pageSize: 12, totalCount: 0, totalPages: 0, hasPrevious: false, hasNext: false,
}

const Host = defineComponent({
  setup() {
    const { hydrate } = useNotesUrlSync()
    hydrate()
    return () => null
  },
})

async function mountAt(router: Router, path: string) {
  await router.push(path)
  await router.isReady()
  return mount(Host, { global: { plugins: [router] } })
}

function makeRouter(): Router {
  return createRouter({
    history: createWebHistory(),
    routes: [{ path: '/notes', name: 'notes', component: { template: '<div />' } }],
  })
}

describe('useNotesUrlSync', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    vi.mocked(noteService.list).mockResolvedValue(emptyPage)
  })

  it('restores search, filters, sort and page from the address bar', async () => {
    const router = makeRouter()
    await mountAt(
      router,
      '/notes?search=milk&sortBy=Title&sortDirection=Asc&hasContent=true&page=3&createdFrom=2026-09-01',
    )
    const notes = useNotesStore()

    expect(notes.query.search).toBe('milk')
    expect(notes.query.sortBy).toBe('Title')
    expect(notes.query.sortDirection).toBe('Asc')
    expect(notes.query.hasContent).toBe(true)
    expect(notes.query.page).toBe(3)
    expect(notes.query.createdFrom).toBe('2026-09-01')
  })

  it('ignores hand-edited values that are not valid', async () => {
    const router = makeRouter()
    await mountAt(router, '/notes?sortBy=DROP%20TABLE&sortDirection=sideways&page=-4&createdFrom=nonsense')
    const notes = useNotesStore()

    // Falls back to defaults rather than forwarding junk to the API.
    expect(notes.query.sortBy).toBe('UpdatedAt')
    expect(notes.query.sortDirection).toBe('Desc')
    expect(notes.query.page).toBe(1)
    expect(notes.query.createdFrom).toBeUndefined()
  })

  it('writes state back to the URL, omitting defaults', async () => {
    const router = makeRouter()
    await mountAt(router, '/notes')
    const notes = useNotesStore()

    await notes.applyQuery({ search: 'coffee' })
    await vi.waitFor(() => expect(router.currentRoute.value.query.search).toBe('coffee'))

    // A default sort should not clutter the address bar.
    expect(router.currentRoute.value.query.sortBy).toBeUndefined()
    expect(router.currentRoute.value.query.page).toBeUndefined()
  })

  it('does not stack history entries while the user types', async () => {
    const router = makeRouter()
    await mountAt(router, '/notes')
    const notes = useNotesStore()
    const replace = vi.spyOn(router, 'replace')
    const push = vi.spyOn(router, 'push')

    await notes.applyQuery({ search: 'a' })
    await vi.waitFor(() => expect(replace).toHaveBeenCalled())

    expect(push).not.toHaveBeenCalled()
  })

  it('reloads when the user navigates back to an earlier filter', async () => {
    const router = makeRouter()
    await mountAt(router, '/notes')
    const notes = useNotesStore()

    await router.push('/notes?search=tea')
    await vi.waitFor(() => expect(notes.query.search).toBe('tea'))

    expect(noteService.list).toHaveBeenCalled()
  })

  it('does not refetch when the URL already matches the store', async () => {
    const router = makeRouter()
    await mountAt(router, '/notes?search=tea')
    const notes = useNotesStore()
    vi.mocked(noteService.list).mockClear()

    // Re-applying the same state must not trigger a second round trip.
    await notes.applyQuery({ search: 'tea' })
    await vi.waitFor(() => expect(router.currentRoute.value.query.search).toBe('tea'))

    expect(noteService.list).toHaveBeenCalledTimes(1)
  })
})
