import { useEffect, useState } from "react";
import { api, ApiError } from "../lib/api";
import { unitAmount } from "../lib/shop";
import type { ActivationRequest } from "../lib/types";
import { confirmAction } from "../lib/ui";

/*
 * Freigeben und Ablehnen einer Einlöse-Anfrage – die eine Stelle, durch die jede Entscheidung des Vaters geht, ob aus
 * dem Shop eines Kindes oder aus der Sammelliste der Startseite (B-749). Zwei Kopien des Fehlersatzes liefen sonst
 * auseinander, und der Zähler an der Navigation erführe von der einen Liste nichts.
 */

const GEAENDERT = "pugling:einloesungen-geaendert";

/** Sagt dem Zähler an der Navigation (und jeder anderen offenen Liste), dass sich offene Anfragen geändert haben. */
export function meldeEinloesungenGeaendert(): void {
  window.dispatchEvent(new Event(GEAENDERT));
}

/**
 * Die Rückfrage vor dem Freigeben. Steht beim Aufrufer **vor** `action.run`: läge sie darin, endete ein „Abbrechen"
 * als gelungener Lauf, und die Seite meldete „freigegeben", obwohl nichts geschah.
 */
export function bestaetigeFreigabe(r: ActivationRequest): boolean {
  return confirmAction(`${unitAmount(r.requestedQuantity, r.unitType)} „${r.articleTitle}" freigeben? Die Einheiten werden aus dem Inventar entnommen.`);
}

/**
 * Entscheidet die Anfrage; wirft mit einem eigenen Satz, wenn das Inventar nicht reicht. Meldet danach die Änderung –
 * auch nach einem Fehler, denn hat ein anderer Erwachsener schon entschieden, stimmt die Zahl nicht mehr.
 */
export async function entscheideEinloesung(r: ActivationRequest, approve: boolean): Promise<void> {
  try {
    await (approve ? api.approveActivation(r.childId, r.id) : api.rejectActivation(r.childId, r.id));
  } catch (e) {
    // Nur hier sind Artikel und Menge bekannt, und nur hier bleibt eine Anfrage offen (beim Kind entsteht keine) –
    // darum ein eigener Satz statt des geteilten aus `GERMAN_PROBLEM_TEXT` (B-504).
    if (approve && e instanceof ApiError && e.code === "insufficient_inventory")
      // „weniger als N" ist genau die Bedingung des Servers (keine Zeile oder `Quantity < RequestedQuantity`).
      throw new Error(`„${r.articleTitle}" lässt sich nicht freigeben: im Inventar sind weniger als `
        + `${unitAmount(r.requestedQuantity, r.unitType)} übrig. Die Anfrage bleibt offen – du kannst sie ablehnen.`);
    throw e;
  } finally {
    meldeEinloesungenGeaendert();
  }
}

/**
 * Die Zahl offener Einlöse-Anfragen aller Kinder (B-747: `X-Total-Count` einer Seite mit einer Zeile). Lädt beim
 * Einbinden, nach jeder gemeldeten Änderung und wenn `neuLaden` wechselt (Seitenwechsel). `null`, solange nichts da
 * ist, der Abruf scheitert oder `aktiv` fehlt (ein Lehrer-Konto hat keinen Shop) – ein Zähler ist ein Hinweis, kein Ort
 * für eine Fehlermeldung.
 */
export function useOffeneEinloesungenZahl(aktiv: boolean, neuLaden: unknown): number | null {
  const [zahl, setZahl] = useState<number | null>(null);
  const [tick, setTick] = useState(0);
  useEffect(() => {
    const onChange = () => setTick((n) => n + 1);
    window.addEventListener(GEAENDERT, onChange);
    return () => window.removeEventListener(GEAENDERT, onChange);
  }, []);
  useEffect(() => {
    if (!aktiv) { setZahl(null); return; }
    let alive = true;
    api.allActivations({ status: "Pending", take: 1 }).then(
      (p) => { if (alive) setZahl(p.total); },
      () => { if (alive) setZahl(null); });
    return () => { alive = false; };
  }, [aktiv, tick, neuLaden]);
  return zahl;
}
