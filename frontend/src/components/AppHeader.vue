<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const emit = defineEmits<{ signOut: [] }>()
</script>

<template>
  <header class="sticky top-0 z-40 border-b border-slate-200 bg-white/80 backdrop-blur">
    <div class="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-3 sm:px-6">
      <div class="flex items-center gap-2.5">
        <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-indigo-600 text-white">
          <svg class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
            <path d="M7 4h7l5 5v11a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1Z" stroke-linejoin="round" />
            <path d="M14 4v5h5M9 13h6M9 17h4" stroke-linecap="round" />
          </svg>
        </span>
        <div class="leading-tight">
          <p class="text-sm font-semibold text-slate-900">Notes</p>
          <p class="hidden text-xs text-slate-500 sm:block">Capture what matters</p>
        </div>
      </div>

      <div class="flex items-center gap-2 sm:gap-3">
        <div class="flex items-center gap-2">
          <span
            class="flex h-8 w-8 items-center justify-center rounded-full bg-slate-200 text-xs font-semibold text-slate-700"
            :title="auth.user?.email"
          >
            {{ auth.initials }}
          </span>
          <div class="hidden leading-tight sm:block">
            <p class="text-sm font-medium text-slate-900">{{ auth.user?.displayName }}</p>
            <p class="text-xs text-slate-500">{{ auth.user?.email }}</p>
          </div>
        </div>

        <button type="button" class="btn btn-secondary px-3 py-1.5" @click="emit('signOut')">
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.8">
            <path d="M12 14l3-4-3-4M15 10H7M9 4H5v12h4" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
          <span class="hidden sm:inline">Sign out</span>
        </button>
      </div>
    </div>
  </header>
</template>
