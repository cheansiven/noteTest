<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import AppHeader from '@/components/AppHeader.vue'
import ConfirmDialog from '@/components/ConfirmDialog.vue'
import EmptyState from '@/components/EmptyState.vue'
import NoteCard from '@/components/NoteCard.vue'
import NoteCardSkeleton from '@/components/NoteCardSkeleton.vue'
import NoteDetailDialog from '@/components/NoteDetailDialog.vue'
import NoteEditorDialog from '@/components/NoteEditorDialog.vue'
import NotesToolbar from '@/components/NotesToolbar.vue'
import PaginationBar from '@/components/PaginationBar.vue'
import { useAsyncAction } from '@/composables/useAsyncAction'
import { useNotesUrlSync } from '@/composables/useNotesUrlSync'
import { useAuthStore } from '@/stores/auth'
import { useNotesStore } from '@/stores/notes'
import { useToastStore } from '@/stores/toast'
import type { Note, NoteListItem, NotePayload, NoteQuery } from '@/types'

const auth = useAuthStore()
const notes = useNotesStore()
const toasts = useToastStore()
const router = useRouter()

// Search, filters, sort and page live in the address bar, so the view survives a
// reload and can be shared.
const { hydrate } = useNotesUrlSync()

const editorOpen = ref(false)
const editingNote = ref<Note | null>(null)

const detailOpen = ref(false)
const detailNote = ref<Note | null>(null)

const confirmOpen = ref(false)
const pendingDelete = ref<{ id: string; title: string } | null>(null)

const deleteMessage = computed(
  () => `"${pendingDelete.value?.title ?? ''}" will be permanently deleted. This cannot be undone.`,
)

onMounted(() => {
  hydrate()
  void notes.fetchNotes()
  // Confirms the restored token is still valid and picks up profile changes.
  void auth.refreshProfile()
})

/* -------------------------------------------------------------------- read -- */

// Opening a note needs the full record: the list only carries a short preview.
const loadDetail = useAsyncAction(
  async (id: string) => {
    detailNote.value = await notes.getNote(id)
  },
  {
    errorMessage: 'Could not open that note.',
    onError: async () => {
      detailOpen.value = false
      await notes.fetchNotes()
    },
  },
)

function openDetail(item: NoteListItem): void {
  detailNote.value = null
  detailOpen.value = true
  void loadDetail.run(item.id)
}

/* ------------------------------------------------------------ create/update -- */

function openCreate(): void {
  editingNote.value = null
  editorOpen.value = true
}

const loadForEdit = useAsyncAction(
  async (id: string) => {
    editingNote.value = await notes.getNote(id)
    detailOpen.value = false
    editorOpen.value = true
  },
  {
    errorMessage: 'Could not open that note for editing.',
    onError: () => notes.fetchNotes(),
  },
)

function openEdit(item: NoteListItem): void {
  void loadForEdit.run(item.id)
}

function editFromDetail(): void {
  if (!detailNote.value) return
  editingNote.value = detailNote.value
  detailOpen.value = false
  editorOpen.value = true
}

const saveNote = useAsyncAction(
  (payload: NotePayload) =>
    editingNote.value
      ? notes.updateNote(editingNote.value.id, payload)
      : notes.createNote(payload),
  {
    successMessage: () => (editingNote.value ? 'Note updated.' : 'Note created.'),
    errorMessage: 'Could not save the note.',
    onSuccess: () => {
      editorOpen.value = false
      editingNote.value = null
    },
  },
)

/* ------------------------------------------------------------------ delete -- */

function askDelete(item: NoteListItem | Note): void {
  pendingDelete.value = { id: item.id, title: item.title }
  confirmOpen.value = true
}

const deleteNote = useAsyncAction(
  (id: string) => notes.deleteNote(id),
  {
    successMessage: () => `Deleted "${pendingDelete.value?.title ?? 'note'}".`,
    errorMessage: 'Could not delete the note.',
    onSuccess: () => {
      confirmOpen.value = false
      pendingDelete.value = null
      detailOpen.value = false
    },
  },
)

function confirmDelete(): void {
  if (pendingDelete.value) {
    void deleteNote.run(pendingDelete.value.id)
  }
}

/* ---------------------------------------------------------------- toolbar --- */

function applyQuery(patch: Partial<NoteQuery>): void {
  void notes.applyQuery(patch)
}

function signOut(): void {
  auth.logout()
  notes.reset()
  toasts.info('Signed out.')
  void router.push({ name: 'login' })
}
</script>

<template>
  <div class="min-h-screen bg-slate-50">
    <AppHeader @sign-out="signOut" />

    <main class="mx-auto max-w-6xl px-4 py-6 sm:px-6 sm:py-8">
      <div class="mb-5 flex items-center justify-between gap-4">
        <div>
          <h1 class="text-lg font-semibold text-slate-900 sm:text-xl">My notes</h1>
          <p class="mt-0.5 text-sm text-slate-500">
            {{ notes.totalCount }} {{ notes.totalCount === 1 ? 'note' : 'notes' }}
            <span v-if="notes.hasActiveFilters">matching your filters</span>
          </p>
        </div>

        <button type="button" class="btn btn-primary shrink-0" @click="openCreate">
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M10 4v12M4 10h12" stroke-linecap="round" />
          </svg>
          <span class="hidden sm:inline">New note</span>
          <span class="sm:hidden">New</span>
        </button>
      </div>

      <NotesToolbar
        :query="notes.query"
        :loading="notes.loading"
        :has-active-filters="notes.hasActiveFilters"
        class="mb-5"
        @change="applyQuery"
        @reset="notes.resetQuery()"
      />

      <!-- Error -->
      <div
        v-if="notes.error"
        class="card border-rose-200 bg-rose-50 p-4 text-sm text-rose-700"
        role="alert"
      >
        <p class="font-medium">{{ notes.error }}</p>
        <button type="button" class="btn btn-secondary mt-3" @click="notes.fetchNotes()">Try again</button>
      </div>

      <!-- Loading -->
      <div
        v-else-if="notes.loading && notes.items.length === 0"
        class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
      >
        <NoteCardSkeleton v-for="n in 6" :key="n" />
      </div>

      <!-- Empty -->
      <EmptyState
        v-else-if="notes.isEmpty && notes.hasActiveFilters"
        title="No notes match your filters"
        message="Try a different search term, or clear the filters to see everything."
        action-label="Clear filters"
        @action="notes.resetQuery()"
      />
      <EmptyState
        v-else-if="notes.isEmpty"
        title="No notes yet"
        message="Create your first note and it will show up here."
        action-label="Create a note"
        @action="openCreate"
      />

      <!-- List -->
      <div v-else class="space-y-6">
        <div
          class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
          :class="{ 'opacity-60 transition': notes.loading }"
        >
          <NoteCard
            v-for="note in notes.items"
            :key="note.id"
            :note="note"
            @open="openDetail(note)"
            @edit="openEdit(note)"
            @remove="askDelete(note)"
          />
        </div>

        <PaginationBar
          :page="notes.query.page"
          :page-size="notes.query.pageSize"
          :total-count="notes.totalCount"
          :total-pages="notes.totalPages"
          :has-next="notes.hasNext"
          :has-previous="notes.hasPrevious"
          @change="(page) => applyQuery({ page })"
        />
      </div>
    </main>

    <NoteEditorDialog
      :open="editorOpen"
      :note="editingNote"
      :saving="saveNote.pending.value"
      @save="saveNote.run"
      @close="editorOpen = false"
    />

    <NoteDetailDialog
      :open="detailOpen"
      :note="detailNote"
      :loading="loadDetail.pending.value"
      @close="detailOpen = false"
      @edit="editFromDetail"
      @remove="detailNote && askDelete(detailNote)"
    />

    <ConfirmDialog
      :open="confirmOpen"
      title="Delete note"
      :message="deleteMessage"
      confirm-label="Delete"
      :busy="deleteNote.pending.value"
      @confirm="confirmDelete"
      @cancel="confirmOpen = false"
    />
  </div>
</template>
