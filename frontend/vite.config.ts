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
        // The installed app is the child's: it opens the arcade, not the product page.
        start_url: "/sohn",
        lang: "de",
        theme_color: "#0f1030",
        background_color: "#0f1030",
        display: "standalone",
        orientation: "portrait",
        // Android only installs a real app (WebAPK) – the thing Family Link can mark "always allowed" – when PNG
        // icons in 192 and 512 are present; the maskable one keeps the letter inside the launcher's crop.
        icons: [
          { src: "icon-192.png", sizes: "192x192", type: "image/png", purpose: "any" },
          { src: "icon-512.png", sizes: "512x512", type: "image/png", purpose: "any" },
          { src: "icon-maskable-512.png", sizes: "512x512", type: "image/png", purpose: "maskable" }
        ]
      }
    })
  ],
  server: {
    proxy: {
      // Überschreibbar, damit E2E neben dem Heimnetz-Server (:5200, echte DB der Söhne) laufen kann (B-485).
      "/api": process.env.PUGLING_API_URL ?? "http://localhost:5200",
      // Hochgeladene Bilder liefert die API unter Media:PublicPath aus. Ohne diese Zeile antwortet Vite mit der
      // App-Seite, und auf der Karte steht im Entwicklungsbetrieb nur ein kaputtes Bild (gefunden im Rollengang B-485).
      "/media": process.env.PUGLING_API_URL ?? "http://localhost:5200"
    }
  }
});
