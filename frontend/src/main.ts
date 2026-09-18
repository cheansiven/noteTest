import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { setUnauthorizedHandler } from './lib/http'
import { useAuthStore } from './stores/auth'
import './style.css'

const app = createApp(App)

app.use(createPinia())
app.use(router)

// An expired or rejected token signs the user out and returns them to /login,
// preserving where they were so they land back there after signing in.
const auth = useAuthStore()
setUnauthorizedHandler(() => {
  auth.logout()
  if (router.currentRoute.value.name !== 'login') {
    void router.push({ name: 'login', query: { redirect: router.currentRoute.value.fullPath } })
  }
})

app.mount('#app')
