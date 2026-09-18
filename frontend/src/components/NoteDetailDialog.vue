<script setup lang="ts">
import BaseModal from '@/components/BaseModal.vue'
import type { Note } from '@/types'
import { formatDateTime, formatRelative } from '@/utils/format'

defineProps<{
  open: boolean
  note: Note | null
  loading: boolean
}>()

const emit = defineEmits<{ close: []; edit: []; remove: [] }>()
</script>

<template>
  <BaseModal
    :open="open"
    :title="note?.title ?? 'Loading note...'"
    size="lg"
    @close="emit('close')"
  >
    <div v-if="loading" class="space-y-2 py-2">
      <div class="h-3 w-full animate-pulse rounded bg-slate-100" />
      <div class="h-3 w-5/6 animate-pulse rounded bg-slate-100" />
      <div class="h-3 w-2/3 animate-pulse rounded bg-slate-100" />
    </div>

    <div v-else-if="note">
      <dl class="mb-4 grid grid-cols-1 gap-3 rounded-lg bg-slate-50 p-3 text-xs sm:grid-cols-2">
        <div>
          <dt class="font-medium text-slate-500">Created</dt>
          <dd class="mt-0.5 text-slate-800">{{ formatDateTime(note.createdAt) }}</dd>
        </div>
        <div>
          <dt class="font-medium text-slate-500">Last updated</dt>
          <dd class="mt-0.5 text-slate-800">
            {{ formatDateTime(note.updatedAt) }}
            <span class="text-slate-400">({{ formatRelative(note.updatedAt) }})</span>
          </dd>
        </div>
      </dl>

      <!-- whitespace-pre-wrap preserves the line breaks the user typed. -->
      <p v-if="note.content" class="max-h-[50vh] overflow-y-auto whitespace-pre-wrap text-sm leading-relaxed text-slate-700">
        {{ note.content }}
      </p>
      <p v-else class="text-sm italic text-slate-400">This note has no content yet.</p>
    </div>

    <template #footer>
      <button type="button" class="btn btn-ghost sm:mr-auto" @click="emit('remove')">Delete</button>
      <button type="button" class="btn btn-secondary" @click="emit('close')">Close</button>
      <button type="button" class="btn btn-primary" data-autofocus @click="emit('edit')">Edit note</button>
    </template>
  </BaseModal>
</template>
