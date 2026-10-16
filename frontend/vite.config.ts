/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vite'

// Backend (profil `http` u launchSettings.json). Proxy drži frontend i API na
// istom originu, pa kolačić za prijavu radi bez CORS-a.
const apiTarget = process.env.TERMINUS_API_URL ?? 'http://localhost:5183'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  build: {
    // Glavni paket (React, Mantine, router) ima ~200 kB gzip; stranice su
    // zasebni lazy dijelovi.
    chunkSizeWarningLimit: 800,
  },
  server: {
    port: 5173,
    proxy: {
      '/api': apiTarget,
      '/health': apiTarget,
      '/openapi': apiTarget,
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: true,
  },
})
