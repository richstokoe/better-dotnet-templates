import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  build: {
    // Build directly into wwwroot so the .NET app serves the SPA as static files.
    // emptyOutDir is false to preserve MVC-owned static files (lib/, css/, etc.)
    outDir: '../wwwroot',
    emptyOutDir: false,
  },
})
