<script setup lang="ts">
import BaseModal from '@/components/BaseModal.vue'

withDefaults(
  defineProps<{
    open: boolean
    title: string
    message: string
    confirmLabel?: string
    busy?: boolean
  }>(),
  { confirmLabel: 'Confirm', busy: false },
)

const emit = defineEmits<{ confirm: []; cancel: [] }>()
</script>

<template>
  <BaseModal :open="open" :title="title" @close="emit('cancel')">
    <p class="text-sm text-slate-600">{{ message }}</p>

    <template #footer>
      <button type="button" class="btn btn-secondary" :disabled="busy" @click="emit('cancel')">Cancel</button>
      <button type="button" class="btn btn-danger" data-autofocus :disabled="busy" @click="emit('confirm')">
        {{ busy ? 'Working...' : confirmLabel }}
      </button>
    </template>
  </BaseModal>
</template>
