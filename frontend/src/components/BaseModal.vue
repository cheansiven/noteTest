<script setup lang="ts">
import { nextTick, onBeforeUnmount, ref, watch } from 'vue'

const props = withDefaults(
  defineProps<{
    open: boolean
    title: string
    subtitle?: string
    size?: 'md' | 'lg'
  }>(),
  { size: 'md' },
)

const emit = defineEmits<{ close: [] }>()

const panel = ref<HTMLElement | null>(null)

function onKeydown(event: KeyboardEvent): void {
  if (event.key === 'Escape') {
    emit('close')
  }
}

watch(
  () => props.open,
  async (open) => {
    if (open) {
      document.addEventListener('keydown', onKeydown)
      // Stop the page behind the dialog from scrolling.
      document.body.style.overflow = 'hidden'
      await nextTick()
      // Move focus into the dialog so keyboard and screen-reader users land here.
      panel.value?.querySelector<HTMLElement>('[data-autofocus]')?.focus()
    } else {
      document.removeEventListener('keydown', onKeydown)
      document.body.style.overflow = ''
    }
  },
  { immediate: true },
)

onBeforeUnmount(() => {
  document.removeEventListener('keydown', onKeydown)
  document.body.style.overflow = ''
})
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="transition duration-150 ease-out"
      enter-from-class="opacity-0"
      leave-active-class="transition duration-100 ease-in"
      leave-to-class="opacity-0"
    >
      <div v-if="open" class="fixed inset-0 z-50 overflow-y-auto">
        <div class="fixed inset-0 bg-slate-900/40 backdrop-blur-sm" @click="emit('close')" />

        <div class="flex min-h-full items-end justify-center p-0 sm:items-center sm:p-4">
          <div
            ref="panel"
            role="dialog"
            aria-modal="true"
            :aria-label="title"
            class="relative w-full rounded-t-2xl bg-white shadow-xl sm:rounded-2xl"
            :class="size === 'lg' ? 'sm:max-w-2xl' : 'sm:max-w-lg'"
          >
            <header class="flex items-start justify-between gap-4 border-b border-slate-200 px-5 py-4">
              <div class="min-w-0">
                <h2 class="truncate text-base font-semibold text-slate-900">{{ title }}</h2>
                <p v-if="subtitle" class="mt-0.5 truncate text-xs text-slate-500">{{ subtitle }}</p>
              </div>
              <button
                type="button"
                class="btn btn-ghost -mr-2 -mt-1 shrink-0 p-2"
                aria-label="Close dialog"
                @click="emit('close')"
              >
                <svg class="h-5 w-5" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.8">
                  <path d="M5 5l10 10M15 5L5 15" stroke-linecap="round" />
                </svg>
              </button>
            </header>

            <div class="px-5 py-4">
              <slot />
            </div>

            <footer
              v-if="$slots.footer"
              class="flex flex-col-reverse gap-2 border-t border-slate-200 px-5 py-4 sm:flex-row sm:justify-end"
            >
              <slot name="footer" />
            </footer>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>
