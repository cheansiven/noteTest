<script setup lang="ts">
import { computed, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import { toErrorMessage } from '@/lib/http'

const PASSWORD_MIN = 8

const auth = useAuthStore()
const toasts = useToastStore()
const router = useRouter()

const displayName = ref('')
const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const error = ref<string | null>(null)

// Mirrors the server-side rules so the user gets feedback before a round-trip.
const passwordError = computed(() => {
  if (!password.value) return null
  return password.value.length < PASSWORD_MIN ? `Use at least ${PASSWORD_MIN} characters.` : null
})

const confirmError = computed(() => {
  if (!confirmPassword.value) return null
  return confirmPassword.value !== password.value ? 'Passwords do not match.' : null
})

const canSubmit = computed(
  () =>
    displayName.value.trim().length >= 2 &&
    email.value.trim().length > 0 &&
    password.value.length >= PASSWORD_MIN &&
    confirmPassword.value === password.value &&
    !auth.loading,
)

async function submit(): Promise<void> {
  error.value = null
  if (!canSubmit.value) return

  try {
    await auth.register({
      displayName: displayName.value.trim(),
      email: email.value.trim(),
      password: password.value,
    })
    toasts.success('Account created. Welcome!')
    await router.push({ name: 'notes' })
  } catch (err) {
    error.value = toErrorMessage(err, 'Could not create your account.')
  }
}
</script>

<template>
  <main class="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-10">
    <div class="w-full max-w-sm">
      <div class="mb-6 text-center">
        <h1 class="text-xl font-semibold text-slate-900">Create your account</h1>
        <p class="mt-1 text-sm text-slate-500">It takes a few seconds.</p>
      </div>

      <form class="card space-y-4 p-6" novalidate @submit.prevent="submit">
        <p v-if="error" class="rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700" role="alert">
          {{ error }}
        </p>

        <div>
          <label for="name" class="label">Display name</label>
          <input id="name" v-model="displayName" type="text" class="input" autocomplete="name" placeholder="Alex Kim" required />
        </div>

        <div>
          <label for="email" class="label">Email</label>
          <input id="email" v-model="email" type="email" class="input" autocomplete="email" placeholder="you@example.com" required />
        </div>

        <div>
          <label for="password" class="label">Password</label>
          <input
            id="password"
            v-model="password"
            type="password"
            class="input"
            :class="{ 'input-error': passwordError }"
            autocomplete="new-password"
            :minlength="PASSWORD_MIN"
            required
          />
          <p class="mt-1 text-xs" :class="passwordError ? 'text-rose-600' : 'text-slate-400'">
            {{ passwordError ?? `At least ${PASSWORD_MIN} characters.` }}
          </p>
        </div>

        <div>
          <label for="confirm" class="label">Confirm password</label>
          <input
            id="confirm"
            v-model="confirmPassword"
            type="password"
            class="input"
            :class="{ 'input-error': confirmError }"
            autocomplete="new-password"
            required
          />
          <p v-if="confirmError" class="mt-1 text-xs text-rose-600">{{ confirmError }}</p>
        </div>

        <button type="submit" class="btn btn-primary w-full" :disabled="!canSubmit">
          {{ auth.loading ? 'Creating account...' : 'Create account' }}
        </button>

        <p class="text-center text-sm text-slate-500">
          Already have an account?
          <RouterLink :to="{ name: 'login' }" class="font-medium text-indigo-600 hover:underline">Sign in</RouterLink>
        </p>
      </form>
    </div>
  </main>
</template>
