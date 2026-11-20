import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// In dev, /api/* is proxied to the .NET API so the browser sees one origin (no CORS).
// In production the API lives on its own host: set VITE_API_URL at build time and add the
// site's origin to AzureOpenAI:AllowedOrigins on the API.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: 'http://localhost:5000', changeOrigin: true },
    },
  },
});
