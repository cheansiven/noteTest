<script setup lang="ts">
import type { NoteListItem } from '@/types'
import { formatDate, formatRelative } from '@/utils/format'

defineProps<{ note: NoteListItem }>()

const emit = defineEmits<{ open: []; edit: []; remove: [] }>()
</script>

<template>
  <article
    class="card group flex h-full flex-col p-4 transition hover:-translate-y-0.5 hover:border-indigo-200 hover:shadow-md"
  >
    <!-- The whole body is the "open" target; action buttons sit outside it. -->
    <button type="button" class="flex-1 text-left focus:outline-none" @click="emit('open')">
      <h3 class="line-clamp-2 text-sm font-semibold text-slate-900 group-hover:text-indigo-700">
        {{ note.title }}
      </h3>
      <p v-if="note.hasContent" class="mt-2 line-clamp-4 text-sm leading-relaxed text-slate-600">
        {{ note.preview }}
      </p>
      <p v-else class="mt-2 text-sm italic text-slate-400">No content</p>
    </button>

    <footer class="mt-4 flex items-end justify-between gap-2 border-t border-slate-100 pt-3">
      <div class="min-w-0 text-xs leading-tight text-slate-500">
        <p :title="note.createdAt">Created {{ formatDate(note.createdAt) }}</p>
        <p v-if="note.updatedAt !== note.createdAt" class="text-slate-400">
          Edited {{ formatRelative(note.updatedAt) }}
        </p>
      </div>

      <!-- Always visible on touch devices; revealed on hover/focus on pointer devices. -->
      <div
        class="flex shrink-0 gap-1 opacity-100 transition sm:opacity-0 sm:group-hover:opacity-100 sm:group-focus-within:opacity-100"
      >
        <button
          type="button"
          class="btn btn-ghost p-1.5"
          :aria-label="`Edit ${note.title}`"
          title="Edit"
          @click="emit('edit')"
        >
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.7">
            <path d="M13.5 3.5l3 3L8 15l-3.5.5L5 12l8.5-8.5Z" stroke-linejoin="round" />
          </svg>
        </button>
        <button
          type="button"
          class="btn btn-ghost p-1.5 hover:bg-rose-50 hover:text-rose-600"
          :aria-label="`Delete ${note.title}`"
          title="Delete"
          @click="emit('remove')"
        >
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.7">
            <path d="M4 6h12M8 6V4h4v2M6 6l.7 9.1a1 1 0 0 0 1 .9h4.6a1 1 0 0 0 1-.9L14 6" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
        </button>
      </div>
    </footer>
  </article>
</template>
