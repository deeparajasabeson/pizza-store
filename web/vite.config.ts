/// <reference types="vitest/config" />
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      // Same-origin /api calls keep the Identity auth cookie same-site (CLAUDE.md "Authentication").
      // Proxying also streams SSE (/api/chat/stream) through without buffering.
      '/api': {
        target: 'https://localhost:7001',
        changeOrigin: true,
        secure: false, // ASP.NET Core dev certificate
      },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    coverage: {
      provider: 'v8',
      include: ['src/**/*.{ts,tsx}'],
      exclude: ['src/api/**', 'src/test/**', 'src/main.tsx', '**/*.d.ts'],
    },
  },
})
