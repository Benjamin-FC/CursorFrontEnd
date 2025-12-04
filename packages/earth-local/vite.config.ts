import { defineConfig } from 'vite';

export default defineConfig({
  build: {
    cssCodeSplit: true,
    cssMinify: false,
    emptyOutDir: false,
    lib: {
      formats: ['es'],
      entry: {
        ['index']: 'src/index.css',
      },
    },
  },
});
