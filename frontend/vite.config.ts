import path from 'node:path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Destino del proxy de `/api` hacia el backend ASP.NET.
// - Dev local (`dotnet run`): http://localhost:5155 (ver launchSettings).
// - Docker Compose: se inyecta `VITE_API_PROXY_TARGET=http://backend:5000`
//   (el nombre del servicio en la red de compose), porque dentro del contenedor
//   `localhost` es el propio contenedor del frontend, no el backend.
const apiProxyTarget = process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5155'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5173,
    host: true,
    allowedHosts: true,
    proxy: {
      '/api': {
        target: apiProxyTarget,
        changeOrigin: true,
      },
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: true,
  },
})
