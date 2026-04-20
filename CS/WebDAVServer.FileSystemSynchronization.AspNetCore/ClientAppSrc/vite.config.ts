import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import { fileURLToPath, URL } from "node:url";




export default defineConfig(({ mode }) => ({
    base: mode === 'netframework' ? '/wwwroot/' : '/',
    plugins: [plugin()],
    resolve: {
        alias: {
            "@": fileURLToPath(new URL("./src", import.meta.url)),
        },
    },
    build: {
        outDir: "../wwwroot",
        rollupOptions: {
            output: {
                entryFileNames: "app.js",
                chunkFileNames: "app.js",
                assetFileNames: ({ name }) => {
                    if (name && name.endsWith(".css")) {
                        return "app.css";
                    }
                    return "[name].[ext]";
                },
            },
        },
        assetsDir: "",
    }
}));