import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'

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
  },
  preview: {
    port: 4273,
    strictPort: true,
  },
})
