import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Ayri build. Prod'da Caddy statik dosyalari servis eder ve /api -> Api'ye proxy'ler.
// Dev'de asagidaki proxy ayni isi yapar.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: process.env.VITE_API_URL ?? 'http://localhost:5094',
        changeOrigin: true,
      },
    },
  },
  build: {
    outDir: 'dist',
  },
})
