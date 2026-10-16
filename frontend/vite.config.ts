/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { fileURLToPath } from 'node:url'
import { defineConfig, loadEnv, type ProxyOptions } from 'vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'TERMINUS_')

  // Isti origin za frontend i API, pa kolačić za prijavu radi bez CORS-a.
  const proxy: ProxyOptions = {
    target: env.TERMINUS_API_URL || 'http://localhost:5183',
    changeOrigin: true,
    secure: false,
  }

  return {
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
        '/api': proxy,
        '/health': proxy,
        '/openapi': proxy,
      },
    },
    test: {
      environment: 'jsdom',
      setupFiles: ['./src/test/setup.ts'],
      css: true,
    },
  }
})
