import path from "path"
import { defineConfig } from "vite"
import react from "@vitejs/plugin-react"
import tailwindcss from "@tailwindcss/vite"

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  build: {
    // Build directly into wwwroot so the .NET app serves the SPA as static files.
    // emptyOutDir is false to preserve MVC-owned static files (lib/, css/, etc.)
    outDir: "../wwwroot",
    emptyOutDir: false,
  },
  server: {
    // During `vite dev`, proxy SignalR connections through to the .NET backend
    // so the browser sees a single origin.
    proxy: {
      "/hubs": {
        target: "http://localhost:5264",
        ws: true,
        changeOrigin: true,
      },
    },
  },
})
