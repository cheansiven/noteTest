<script setup lang="ts">
defineProps<{
  title: string
  message: string
  actionLabel?: string
}>()

const emit = defineEmits<{ action: [] }>()
</script>

<template>
  <div class="card flex flex-col items-center px-6 py-14 text-center">
    <span class="flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
      <svg class="h-6 w-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
        <path d="M7 4h7l5 5v11a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1Z" stroke-linejoin="round" />
        <path d="M14 4v5h5" stroke-linecap="round" />
      </svg>
    </span>
    <h3 class="mt-4 text-sm font-semibold text-slate-900">{{ title }}</h3>
    <p class="mt-1 max-w-sm text-sm text-slate-500">{{ message }}</p>
    <button v-if="actionLabel" type="button" class="btn btn-primary mt-5" @click="emit('action')">
      {{ actionLabel }}
    </button>
  </div>
</template>
