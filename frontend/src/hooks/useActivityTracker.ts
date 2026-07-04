import { useEffect, useRef } from "react";
import { api } from "../api";

const HEARTBEAT_INTERVAL_S = 15;
/** Ohne Interaktion in diesem Fenster gilt die Zeit als "idle" (nur auf den Bildschirm gucken). */
const IDLE_THRESHOLD_S = 25;

/**
 * Misst echte Lernaktivität:
 * - Zeit zählt nur, wenn die App sichtbar ist (Page Visibility API)
 * - Sekunden seit der letzten Interaktion > Schwelle => idle statt aktiv
 * - Sendet alle 15s einen Heartbeat ans Backend
 *
 * `noteInteraction` zusätzlich bei fachlichen Aktionen aufrufen (Karte umdrehen, Antwort geben).
 */
export function useActivityTracker(sessionId: number | null) {
  const lastInteraction = useRef(Date.now());
  const interactions = useRef(0);

  useEffect(() => {
    if (sessionId === null) return;

    const onInteract = () => {
      lastInteraction.current = Date.now();
      interactions.current++;
    };
    window.addEventListener("pointerdown", onInteract);
    window.addEventListener("keydown", onInteract);

    const timer = setInterval(() => {
      if (document.hidden) return; // App nicht sichtbar => zählt gar nicht

      const sinceInteraction = (Date.now() - lastInteraction.current) / 1000;
      const isActive = sinceInteraction < IDLE_THRESHOLD_S;

      api.heartbeat(
        sessionId,
        isActive ? HEARTBEAT_INTERVAL_S : 0,
        isActive ? 0 : HEARTBEAT_INTERVAL_S,
        interactions.current,
      ).catch(() => { /* offline: Heartbeat verwerfen */ });
      interactions.current = 0;
    }, HEARTBEAT_INTERVAL_S * 1000);

    return () => {
      clearInterval(timer);
      window.removeEventListener("pointerdown", onInteract);
      window.removeEventListener("keydown", onInteract);
    };
  }, [sessionId]);

  return {
    noteInteraction: () => {
      lastInteraction.current = Date.now();
      interactions.current++;
    },
  };
}
