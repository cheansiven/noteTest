import axios from 'axios'
import { defineStore } from 'pinia'
import { computed, reactive, ref } from 'vue'
import { noteService } from '@/services/noteService'
import { toErrorMessage } from '@/lib/http'
import type { Note, NoteListItem, NotePayload, NoteQuery } from '@/types'

const DEFAULT_QUERY: NoteQuery = {
  search: '',
  sortBy: 'UpdatedAt',
  sortDirection: 'Desc',
  createdFrom: undefined,
  createdTo: undefined,
  hasContent: undefined,
  page: 1,
  pageSize: 12,
}

export const useNotesStore = defineStore('notes', () => {
  const items = ref<NoteListItem[]>([])
  const query = reactive<NoteQuery>({ ...DEFAULT_QUERY })

  const totalCount = ref(0)
  const totalPages = ref(0)
  const hasNext = ref(false)
  const hasPrevious = ref(false)

  const loading = ref(false)
  const saving = ref(false)
  const error = ref<string | null>(null)

  /** Cancels a list request that a newer one has superseded. */
  let inFlight: AbortController | null = null

  const isEmpty = computed(() => !loading.value && items.value.length === 0)

  const hasActiveFilters = computed(() =>
    Boolean(query.search) ||
    Boolean(query.createdFrom) ||
    Boolean(query.createdTo) ||
    typeof query.hasContent === 'boolean',
  )

  async function fetchNotes(): Promise<void> {
    inFlight?.abort()
    const controller = new AbortController()
    inFlight = controller

    loading.value = true
    error.value = null

    try {
      const result = await noteService.list(query, controller.signal)

      items.value = result.items
      totalCount.value = result.totalCount
      totalPages.value = result.totalPages
      hasNext.value = result.hasNext
      hasPrevious.value = result.hasPrevious

      // Deleting the last note on a page leaves us past the end; step back.
      if (result.items.length === 0 && result.totalCount > 0 && query.page > 1) {
        query.page = Math.max(1, result.totalPages)
        await fetchNotes()
      }
    } catch (err) {
      if (axios.isCancel(err)) return
      error.value = toErrorMessage(err, 'Could not load your notes.')
      items.value = []
      totalCount.value = 0
      totalPages.value = 0
    } finally {
      if (inFlight === controller) {
        loading.value = false
        inFlight = null
      }
    }
  }

  /** Applies a partial query change; anything but paging resets to page 1. */
  async function applyQuery(patch: Partial<NoteQuery>): Promise<void> {
    Object.assign(query, patch)
    if (!('page' in patch)) {
      query.page = 1
    }
    await fetchNotes()
  }

  async function resetQuery(): Promise<void> {
    Object.assign(query, DEFAULT_QUERY)
    await fetchNotes()
  }

  async function goToPage(page: number): Promise<void> {
    if (page < 1 || (totalPages.value > 0 && page > totalPages.value)) return
    await applyQuery({ page })
  }

  async function createNote(payload: NotePayload): Promise<Note> {
    saving.value = true
    try {
      const note = await noteService.create(payload)
      // A new note is the most recently updated, so show it from the top.
      query.page = 1
      await fetchNotes()
      return note
    } finally {
      saving.value = false
    }
  }

  async function updateNote(id: string, payload: NotePayload): Promise<Note> {
    saving.value = true
    try {
      const note = await noteService.update(id, payload)
      await fetchNotes()
      return note
    } finally {
      saving.value = false
    }
  }

  async function deleteNote(id: string): Promise<void> {
    saving.value = true
    try {
      await noteService.remove(id)
      await fetchNotes()
    } finally {
      saving.value = false
    }
  }

  function getNote(id: string): Promise<Note> {
    return noteService.get(id)
  }

  function $resetAll(): void {
    items.value = []
    totalCount.value = 0
    totalPages.value = 0
    hasNext.value = false
    hasPrevious.value = false
    error.value = null
    Object.assign(query, DEFAULT_QUERY)
  }

  return {
    items,
    query,
    totalCount,
    totalPages,
    hasNext,
    hasPrevious,
    loading,
    saving,
    error,
    isEmpty,
    hasActiveFilters,
    fetchNotes,
    applyQuery,
    resetQuery,
    goToPage,
    createNote,
    updateNote,
    deleteNote,
    getNote,
    $resetAll,
  }
})
