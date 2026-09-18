import { watch } from 'vue'
import { useRoute, useRouter, type LocationQuery, type LocationQueryRaw } from 'vue-router'
import { useNotesStore } from '@/stores/notes'
import type { NoteQuery, NoteSortField, SortDirection } from '@/types'

const SORT_FIELDS: NoteSortField[] = ['UpdatedAt', 'CreatedAt', 'Title']
const SORT_DIRECTIONS: SortDirection[] = ['Asc', 'Desc']

const DEFAULTS = {
  sortBy: 'UpdatedAt' as NoteSortField,
  sortDirection: 'Desc' as SortDirection,
  page: 1,
}

function first(value: LocationQuery[string]): string | undefined {
  const raw = Array.isArray(value) ? value[0] : value
  return raw ?? undefined
}

/** Query strings are user-editable, so every value is validated, never trusted. */
function parse(query: LocationQuery): Partial<NoteQuery> {
  const parsed: Partial<NoteQuery> = {}

  const search = first(query.search)?.trim()
  if (search) parsed.search = search

  const sortBy = first(query.sortBy)
  if (sortBy && SORT_FIELDS.includes(sortBy as NoteSortField)) {
    parsed.sortBy = sortBy as NoteSortField
  }

  const sortDirection = first(query.sortDirection)
  if (sortDirection && SORT_DIRECTIONS.includes(sortDirection as SortDirection)) {
    parsed.sortDirection = sortDirection as SortDirection
  }

  // Dates stay as the YYYY-MM-DD strings the API and the date inputs both expect.
  const createdFrom = first(query.createdFrom)
  if (createdFrom && /^\d{4}-\d{2}-\d{2}$/.test(createdFrom)) parsed.createdFrom = createdFrom

  const createdTo = first(query.createdTo)
  if (createdTo && /^\d{4}-\d{2}-\d{2}$/.test(createdTo)) parsed.createdTo = createdTo

  const hasContent = first(query.hasContent)
  if (hasContent === 'true') parsed.hasContent = true
  if (hasContent === 'false') parsed.hasContent = false

  const page = Number(first(query.page))
  if (Number.isInteger(page) && page > 0) parsed.page = page

  return parsed
}

/** Only non-default values reach the URL, so a clean view has a clean address. */
function serialize(query: NoteQuery): LocationQueryRaw {
  const serialized: LocationQueryRaw = {}

  if (query.search) serialized.search = query.search
  if (query.sortBy !== DEFAULTS.sortBy) serialized.sortBy = query.sortBy
  if (query.sortDirection !== DEFAULTS.sortDirection) serialized.sortDirection = query.sortDirection
  if (query.createdFrom) serialized.createdFrom = query.createdFrom
  if (query.createdTo) serialized.createdTo = query.createdTo
  if (typeof query.hasContent === 'boolean') serialized.hasContent = String(query.hasContent)
  if (query.page !== DEFAULTS.page) serialized.page = String(query.page)

  return serialized
}

const fingerprint = (value: LocationQueryRaw | LocationQuery): string =>
  JSON.stringify(Object.entries(value).sort(([a], [b]) => a.localeCompare(b)))

/**
 * Keeps the notes list state and the address bar in step, so a filtered view survives a
 * reload and can be shared or bookmarked.
 *
 * Changes use `replace` rather than `push`: search is debounced per keystroke, and
 * pushing each one would bury the previous page under a stack of near-identical entries.
 */
export function useNotesUrlSync(): { hydrate: () => void } {
  const route = useRoute()
  const router = useRouter()
  const notes = useNotesStore()

  function hydrate(): void {
    notes.hydrateQuery(parse(route.query))
  }

  // Store -> URL.
  watch(
    () => serialize(notes.query),
    (next) => {
      if (fingerprint(next) !== fingerprint(route.query)) {
        void router.replace({ query: next })
      }
    },
    { deep: true },
  )

  // URL -> store, for the back/forward buttons and hand-edited addresses. Comparing
  // fingerprints is what stops the two watchers from bouncing off each other.
  watch(
    () => route.query,
    (next) => {
      if (fingerprint(serialize(notes.query)) === fingerprint(next)) {
        return
      }

      notes.hydrateQuery(parse(next))
      void notes.fetchNotes()
    },
  )

  return { hydrate }
}
