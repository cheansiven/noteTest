import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'

// Set by docker-compose.dev.yml to the API service; unset for a plain `npm run dev`,
// which talks to the API directly on its published port instead.
const apiProxyTarget = process.env.VITE_API_PROXY_TARGET

export default defineConfig({
  plugins: [vue(), tailwindcss()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  // Dedicated ports so this stack never collides with anything else running locally.
  server: {
    port: 5273,
    strictPort: true,
    // Inside Docker the source arrives over a bind mount, which does not deliver
    // inotify events on macOS or Windows, so the watcher has to poll instead.
    watch: process.env.VITE_DOCKER === 'true' ? { usePolling: true, interval: 300 } : undefined,
    // When a target is supplied, the dev server proxies /api just as nginx does in
    // production, so the browser talks to a single origin in both setups.
    proxy: apiProxyTarget
      ? { '/api': { target: apiProxyTarget, changeOrigin: true } }
      : undefined,
  },
  preview: {
    port: 4273,
    strictPort: true,
  },
})
