import { useSyncExternalStore } from "react";

/**
 * Unterhalb dieser Breite gilt ein Fenster als Telefon (B-663, Entscheidung 4). Ab 768 px gilt alles – dieselbe Grenze
 * wie die bestehende Probe `e2e/responsive-vater.spec.ts`, ein Tablet im Hochformat sieht also noch die Werkstatt.
 */
export const NARROW_MAX_PX = 767;

const QUERY = `(max-width: ${NARROW_MAX_PX}px)`;

function subscribe(onChange: () => void): () => void {
  if (typeof window === "undefined" || typeof window.matchMedia !== "function") return () => {};
  const mql = window.matchMedia(QUERY);
  mql.addEventListener("change", onChange);
  return () => mql.removeEventListener("change", onChange);
}

function snapshot(): boolean {
  if (typeof window === "undefined" || typeof window.matchMedia !== "function") return false;
  return window.matchMedia(QUERY).matches;
}

/**
 * Ist das Fenster gerade schmal (Telefonbreite)? **Abonniert** die Medienabfrage statt sie einmal zu lesen: nach dem
 * Verbreitern muss die Werkstatt zurückkommen (B-663, AK 4) – ein einmal gelesener Wert bliebe als Zustand stehen.
 * Nach Fensterbreite, nicht nach Gerät: ein schmal gezogenes Rechner-Fenster verhält sich wie ein Telefon.
 */
export function useNarrowViewport(): boolean {
  return useSyncExternalStore(subscribe, snapshot, () => false);
}
