import path from "path"
import tailwindcss from "@tailwindcss/vite"
import react from "@vitejs/plugin-react"
import { defineConfig } from "vite"

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  // 根据构建模式决定输出目录
  // 生产构建时输出到wwwroot，CI/CD构建时输出到dist
  const outDir = process.env.VITE_OUT_DIR || (mode === 'production' ? 'dist' : 'dist')
  
  return {
    plugins: [react(), tailwindcss()],
    resolve: {
      alias: {
        "@": path.resolve(__dirname, "./src"),
      },
    },
    server: {
      proxy: {
        '/api': {
          target: 'http://localhost:6500'
        },
      },
    },
    build: {
      outDir,
      emptyOutDir: true,
      rollupOptions: {
        output: {
          manualChunks: undefined,
        },
      },
    },
  }
})

