import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// https://vite.dev/config/

export default ({ mode }: { mode: string }) => {
  const isNetFramework = mode === "netframework";
  return defineConfig({
    plugins: [react()],
    css: {
      preprocessorOptions: {
        scss: {
          additionalData: isNetFramework
            ? `$image-base-path: "/wwwroot/images/";`
            : "",
        },
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
    },
  });
};
