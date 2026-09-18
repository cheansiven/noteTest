<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'
import { toErrorMessage } from '@/lib/http'

const auth = useAuthStore()
const toasts = useToastStore()
const router = useRouter()
const route = useRoute()

const email = ref('')
const password = ref('')
const error = ref<string | null>(null)

async function submit(): Promise<void> {
  error.value = null
  try {
    await auth.login({ email: email.value.trim(), password: password.value })
    toasts.success(`Welcome back, ${auth.user?.displayName}.`)

    // Return the user to whatever they were trying to reach.
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : null
    await router.push(redirect ?? { name: 'notes' })
  } catch (err) {
    error.value = toErrorMessage(err, 'Could not sign you in.')
  }
}
</script>

<template>
  <main class="flex min-h-screen items-center justify-center bg-slate-50 px-4 py-10">
    <div class="w-full max-w-sm">
      <div class="mb-6 text-center">
        <span class="mx-auto flex h-11 w-11 items-center justify-center rounded-xl bg-indigo-600 text-white">
          <svg class="h-6 w-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
            <path d="M7 4h7l5 5v11a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1Z" stroke-linejoin="round" />
            <path d="M14 4v5h5M9 13h6M9 17h4" stroke-linecap="round" />
          </svg>
        </span>
        <h1 class="mt-4 text-xl font-semibold text-slate-900">Sign in to Notes</h1>
        <p class="mt-1 text-sm text-slate-500">Your notes, private to your account.</p>
      </div>

      <form class="card space-y-4 p-6" novalidate @submit.prevent="submit">
        <p v-if="error" class="rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700" role="alert">
          {{ error }}
        </p>

        <div>
          <label for="email" class="label">Email</label>
          <input
            id="email"
            v-model="email"
            type="email"
            class="input"
            autocomplete="email"
            placeholder="you@example.com"
            required
          />
        </div>

        <div>
          <label for="password" class="label">Password</label>
          <input
            id="password"
            v-model="password"
            type="password"
            class="input"
            autocomplete="current-password"
            placeholder="********"
            required
          />
        </div>

        <button type="submit" class="btn btn-primary w-full" :disabled="auth.loading">
          {{ auth.loading ? 'Signing in...' : 'Sign in' }}
        </button>

        <p class="text-center text-sm text-slate-500">
          No account yet?
          <RouterLink :to="{ name: 'register' }" class="font-medium text-indigo-600 hover:underline">
            Create one
          </RouterLink>
        </p>
      </form>
    </div>
  </main>
</template>
