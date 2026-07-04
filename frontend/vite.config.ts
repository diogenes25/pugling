import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { VitePWA } from "vite-plugin-pwa";

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: "autoUpdate",
      manifest: {
        name: "Pugling – Lernen & Punkte",
        short_name: "Pugling",
        description: "Vokabeln lernen, Punkte sammeln, Belohnungen eintauschen",
        theme_color: "#4f46e5",
        background_color: "#ffffff",
        display: "standalone",
        orientation: "portrait",
        icons: [
          { src: "icon.svg", sizes: "any", type: "image/svg+xml", purpose: "any" }
        ]
      }
    })
  ],
  server: {
    proxy: {
      "/api": "http://localhost:5200"
    }
  }
});
