import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { federation } from '@module-federation/vite';

export default defineConfig({
  plugins: [
    react(),
    federation({
      name: 'system-info',
      filename: 'remoteEntry.js',
      exposes: { './Page': './src/Page.tsx' },
      shared: {
        react: { strategy: 'loaded-first' },
        'react-dom': { strategy: 'loaded-first' }
      }
    })
  ],
  build: { target: 'esnext' }
});
