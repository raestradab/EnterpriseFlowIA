import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    // Same-origin from the browser's point of view, so the Api needs no CORS configuration
    // for local development — Vite forwards /api/* server-side to the running EnterpriseFlow.Api.
    proxy: {
      '/api': {
        target: 'http://localhost:5050',
        changeOrigin: true,
      },
      // F6.1 (ADR-0011): SignalR's WebSocket connection needs its own proxy entry — Vite
      // doesn't upgrade a plain HTTP proxy to WebSocket automatically (ws: true is required).
      '/hubs': {
        target: 'http://localhost:5050',
        changeOrigin: true,
        ws: true,
      },
    },
  },
})
