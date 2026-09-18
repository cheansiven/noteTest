<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import BaseModal from '@/components/BaseModal.vue'
import type { Note, NotePayload } from '@/types'
import { formatDateTime } from '@/utils/format'

const TITLE_MAX = 200

const props = defineProps<{
  open: boolean
  note: Note | null
  saving: boolean
}>()

const emit = defineEmits<{
  save: [payload: NotePayload]
  close: []
}>()

const title = ref('')
const content = ref('')
const touched = ref(false)

const isEditing = computed(() => props.note !== null)

const titleError = computed(() => {
  if (!touched.value) return null
  const value = title.value.trim()
  if (!value) return 'Title is required.'
  if (value.length > TITLE_MAX) return `Title must be ${TITLE_MAX} characters or fewer.`
  return null
})

const canSave = computed(() => {
  const value = title.value.trim()
  return value.length > 0 && value.length <= TITLE_MAX && !props.saving
})

// Reload the form whenever the dialog opens, so a cancelled edit leaves no trace.
watch(
  () => props.open,
  (open) => {
    if (!open) return
    title.value = props.note?.title ?? ''
    content.value = props.note?.content ?? ''
    touched.value = false
  },
  { immediate: true },
)

function submit(): void {
  touched.value = true
  if (!canSave.value) return

  emit('save', {
    title: title.value.trim(),
    content: content.value.trim() ? content.value.trim() : null,
  })
}
</script>

<template>
  <BaseModal
    :open="open"
    :title="isEditing ? 'Edit note' : 'New note'"
    :subtitle="note ? `Last updated ${formatDateTime(note.updatedAt)}` : undefined"
    size="lg"
    @close="emit('close')"
  >
    <form id="note-form" class="space-y-4" novalidate @submit.prevent="submit">
      <div>
        <label for="note-title" class="label">
          Title <span class="text-rose-500" aria-hidden="true">*</span>
        </label>
        <input
          id="note-title"
          v-model="title"
          data-autofocus
          type="text"
          class="input"
          :class="{ 'input-error': titleError }"
          :maxlength="TITLE_MAX"
          placeholder="Give your note a name"
          required
          :aria-invalid="Boolean(titleError)"
          aria-describedby="title-help"
          @blur="touched = true"
        />
        <div id="title-help" class="mt-1 flex justify-between gap-2 text-xs">
          <span :class="titleError ? 'text-rose-600' : 'text-slate-400'">
            {{ titleError ?? 'Required.' }}
          </span>
          <span class="shrink-0 text-slate-400">{{ title.length }}/{{ TITLE_MAX }}</span>
        </div>
      </div>

      <div>
        <label for="note-content" class="label">Content <span class="font-normal text-slate-400">(optional)</span></label>
        <textarea
          id="note-content"
          v-model="content"
          class="input min-h-[12rem] resize-y leading-relaxed"
          placeholder="Write something worth remembering..."
        />
      </div>
    </form>

    <template #footer>
      <button type="button" class="btn btn-secondary" :disabled="saving" @click="emit('close')">Cancel</button>
      <button type="submit" form="note-form" class="btn btn-primary" :disabled="!canSave">
        {{ saving ? 'Saving...' : isEditing ? 'Save changes' : 'Create note' }}
      </button>
    </template>
  </BaseModal>
</template>
