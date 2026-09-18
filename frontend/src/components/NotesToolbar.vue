<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import type { NoteQuery, NoteSortField, SortDirection } from '@/types'
import { toDateInputValue } from '@/utils/format'

const props = defineProps<{ query: NoteQuery; loading: boolean; hasActiveFilters: boolean }>()

const emit = defineEmits<{
  change: [patch: Partial<NoteQuery>]
  reset: []
}>()

/* ------------------------------------------------------------------ search -- */

const searchTerm = ref(props.query.search ?? '')
let debounceHandle: number | undefined

// Keep typing responsive but avoid a request per keystroke.
watch(searchTerm, (value) => {
  window.clearTimeout(debounceHandle)
  debounceHandle = window.setTimeout(() => {
    if ((props.query.search ?? '') !== value.trim()) {
      emit('change', { search: value.trim() })
    }
  }, 300)
})

// Reflect external changes (e.g. "Clear filters") back into the input.
watch(
  () => props.query.search,
  (value) => {
    if ((value ?? '') !== searchTerm.value.trim()) {
      searchTerm.value = value ?? ''
    }
  },
)

onBeforeUnmount(() => window.clearTimeout(debounceHandle))

function clearSearch(): void {
  searchTerm.value = ''
  emit('change', { search: '' })
}

/* -------------------------------------------------------------------- sort -- */

const SORT_OPTIONS: Array<{ value: string; label: string }> = [
  { value: 'UpdatedAt:Desc', label: 'Recently updated' },
  { value: 'UpdatedAt:Asc', label: 'Least recently updated' },
  { value: 'CreatedAt:Desc', label: 'Newest first' },
  { value: 'CreatedAt:Asc', label: 'Oldest first' },
  { value: 'Title:Asc', label: 'Title A-Z' },
  { value: 'Title:Desc', label: 'Title Z-A' },
]

const sortValue = computed({
  get: () => `${props.query.sortBy}:${props.query.sortDirection}`,
  set: (value: string) => {
    const [sortBy, sortDirection] = value.split(':') as [NoteSortField, SortDirection]
    emit('change', { sortBy, sortDirection })
  },
})

/* ----------------------------------------------------------------- filters -- */

function startOfDaysAgo(days: number): string {
  const date = new Date()
  date.setHours(0, 0, 0, 0)
  date.setDate(date.getDate() - days)
  return toDateInputValue(date)
}

const DATE_PRESETS = [
  { label: 'Any time', days: null },
  { label: 'Today', days: 0 },
  { label: 'Last 7 days', days: 6 },
  { label: 'Last 30 days', days: 29 },
] as const

function isPresetActive(days: number | null): boolean {
  if (days === null) return !props.query.createdFrom && !props.query.createdTo
  return props.query.createdFrom === startOfDaysAgo(days) && !props.query.createdTo
}

function applyPreset(days: number | null): void {
  emit('change', {
    createdFrom: days === null ? undefined : startOfDaysAgo(days),
    createdTo: undefined,
  })
}

const CONTENT_FILTERS = [
  { label: 'All notes', value: undefined },
  { label: 'With content', value: true },
  { label: 'Empty', value: false },
] as const

const showAdvanced = ref(false)

function onDateInput(key: 'createdFrom' | 'createdTo', event: Event): void {
  const value = (event.target as HTMLInputElement).value
  emit('change', { [key]: value || undefined })
}
</script>

<template>
  <section class="card p-4" aria-label="Search and filter notes">
    <div class="flex flex-col gap-3 sm:flex-row sm:items-center">
      <!-- Search -->
      <div class="relative flex-1">
        <span class="pointer-events-none absolute inset-y-0 left-3 flex items-center text-slate-400">
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.8">
            <circle cx="9" cy="9" r="5.5" />
            <path d="M13.5 13.5L17 17" stroke-linecap="round" />
          </svg>
        </span>
        <input
          v-model="searchTerm"
          type="search"
          class="input pl-9"
          placeholder="Search title and content..."
          aria-label="Search notes"
        />
        <button
          v-if="searchTerm"
          type="button"
          class="absolute inset-y-0 right-2 flex items-center px-1 text-slate-400 hover:text-slate-600"
          aria-label="Clear search"
          @click="clearSearch"
        >
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M5 5l10 10M15 5L5 15" stroke-linecap="round" />
          </svg>
        </button>
      </div>

      <!-- Sort -->
      <div class="flex items-center gap-2">
        <label for="sort" class="shrink-0 text-xs font-medium text-slate-500">Sort by</label>
        <select id="sort" v-model="sortValue" class="input sm:w-52">
          <option v-for="option in SORT_OPTIONS" :key="option.value" :value="option.value">
            {{ option.label }}
          </option>
        </select>
      </div>
    </div>

    <!-- Filters -->
    <div class="mt-3 flex flex-wrap items-center gap-2">
      <button
        v-for="preset in DATE_PRESETS"
        :key="preset.label"
        type="button"
        class="chip"
        :class="{ 'chip-active': isPresetActive(preset.days) }"
        @click="applyPreset(preset.days)"
      >
        {{ preset.label }}
      </button>

      <span class="mx-1 hidden h-4 w-px bg-slate-200 sm:block" />

      <button
        v-for="filter in CONTENT_FILTERS"
        :key="filter.label"
        type="button"
        class="chip"
        :class="{ 'chip-active': query.hasContent === filter.value }"
        @click="emit('change', { hasContent: filter.value })"
      >
        {{ filter.label }}
      </button>

      <button type="button" class="chip" @click="showAdvanced = !showAdvanced">
        {{ showAdvanced ? 'Hide' : 'Custom' }} date range
      </button>

      <button
        v-if="hasActiveFilters"
        type="button"
        class="ml-auto text-xs font-medium text-indigo-600 underline-offset-2 hover:underline"
        @click="emit('reset')"
      >
        Clear all filters
      </button>
    </div>

    <div v-if="showAdvanced" class="mt-3 grid gap-3 border-t border-slate-100 pt-3 sm:grid-cols-2">
      <div>
        <label for="created-from" class="label text-xs">Created from</label>
        <input
          id="created-from"
          type="date"
          class="input"
          :value="query.createdFrom ?? ''"
          @change="onDateInput('createdFrom', $event)"
        />
      </div>
      <div>
        <label for="created-to" class="label text-xs">Created to</label>
        <input
          id="created-to"
          type="date"
          class="input"
          :value="query.createdTo ?? ''"
          @change="onDateInput('createdTo', $event)"
        />
      </div>
    </div>

    <p v-if="loading" class="mt-3 text-xs text-slate-400">Updating results...</p>
  </section>
</template>
