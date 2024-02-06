/// <reference types="vitest" />
import { defineConfig, splitVendorChunkPlugin } from "vite";
import { comlink } from "vite-plugin-comlink";
import Inspect from "vite-plugin-inspect";
import { nodePolyfills } from "vite-plugin-node-polyfills";
import { viteStaticCopy } from "vite-plugin-static-copy";
import { resolve } from "path";
import monacoEditorPlugin from "vite-plugin-monaco-editor";
import { createHtmlPlugin } from "vite-plugin-html";
import { terser } from "rollup-plugin-terser";

// https://vitejs.dev/config/
export default defineConfig({
  test: {
    include: ["src/**/*.{test,spec}.{js,ts}"],
  },
  base: "./",
  plugins: [
    monacoEditorPlugin.default({
      languageWorkers: ["json", "typescript"],
    }),
    Inspect(),
    comlink(),
    splitVendorChunkPlugin(),
    nodePolyfills(),
    createHtmlPlugin({
      minify: true,
    }),
    viteStaticCopy({
      targets: [
        {
          src: "../vscode-bebop/schemas/bebop-schema.json",
          dest: "./assets/",
        },
        /* {
          src: "node_modules/coi-serviceworker/coi-serviceworker.min.js",
          dest: ".",
        },*/
        /*{
          src: "./bin/bebopc.wasm",
          dest: ".",
        },
        */
      ],
    }),
  ],
  worker: {
    plugins: () => [comlink()],
  },
  build: {
    minify: "terser",
    sourcemap: false,
    chunkSizeWarningLimit: Infinity,
    target: "ES2022",
    rollupOptions: {
      plugins: [
        terser({
          format: {
            comments: false,
          },

          mangle: {
            keep_classnames: false,
            reserved: [],
          },
        }),
      ],
    },
  },
});
