import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Allow local development by default. Enable secure WSS HMR only when explicitly requested.
const useWss = process.env.VITE_USE_WSS === 'true'

export default defineConfig({
  plugins: [react()],
  server: {
    // host true binds to all interfaces and works locally
    host: true,
    port: 5173,
    strictPort: true,
    // Allow localhost and optional custom domain
    allowedHosts: ['localhost', '127.0.0.1', '.manusvm.computer'],
    ...(useWss
      ? {
          hmr: {
            clientPort: 443,
            protocol: 'wss'
          }
        }
      : {})
  }
})

