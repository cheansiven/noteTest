<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasNext: boolean
  hasPrevious: boolean
}>()

const emit = defineEmits<{ change: [page: number] }>()

const rangeStart = computed(() => (props.totalCount === 0 ? 0 : (props.page - 1) * props.pageSize + 1))
const rangeEnd = computed(() => Math.min(props.page * props.pageSize, props.totalCount))
</script>

<template>
  <nav
    v-if="totalPages > 1"
    class="flex flex-col items-center justify-between gap-3 sm:flex-row"
    aria-label="Pagination"
  >
    <p class="text-xs text-slate-500">
      Showing <span class="font-medium text-slate-700">{{ rangeStart }}-{{ rangeEnd }}</span>
      of <span class="font-medium text-slate-700">{{ totalCount }}</span> notes
    </p>

    <div class="flex items-center gap-2">
      <button
        type="button"
        class="btn btn-secondary px-3 py-1.5"
        :disabled="!hasPrevious"
        @click="emit('change', page - 1)"
      >
        Previous
      </button>
      <span class="px-1 text-xs text-slate-500">Page {{ page }} of {{ totalPages }}</span>
      <button
        type="button"
        class="btn btn-secondary px-3 py-1.5"
        :disabled="!hasNext"
        @click="emit('change', page + 1)"
      >
        Next
      </button>
    </div>
  </nav>
</template>
