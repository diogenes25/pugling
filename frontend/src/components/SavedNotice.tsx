import { useEffect, useRef, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { StatusBanner } from "./StatusBanner";
import type { ActionMessage } from "../lib/useAction";

/**
 * Die Meldungsstelle der Sohn-Shell: Sie zeigt die Bestätigung einer Aktion, die den Bildschirm
 * **verlässt**, an dem sie stattfand.
 *
 * Der Grund ist die Fehlerklasse aus B-151 – eine Bestätigung darf nicht in dem Element leben, dessen
 * Verschwinden sie bestätigt. In der Arcade lag der Reflex nahe, dafür die Feier (`CelebrationLayer`) zu
 * nehmen; die trägt es aber nicht: sie ist `aria-hidden`, verschwindet bei „Bewegung reduzieren" ganz
 * und räumt sich nach 1,5 s selbst ab. Als *Schmuck* über einer ohnehin sichtbaren Änderung ist das
 * richtig, als **einziger** Beleg einer Mutation nicht.
 *
 * Aufrufer geben die Meldung dem Sprung mit: `navigate("/sohn", { state: { saved: "…" } })`.
 *
 * **Sie gehört zu einem Bildschirm, nicht zur Sitzung** (B-209): Der Baustein bleibt in der Shell die
 * ganze Zeit montiert, also muss er selbst abräumen. Vorher tat er das nie — die Bestätigung des
 * Interessen-Speicherns stand danach über Shop, Weg und Konto, als sei dort gerade etwas passiert.
 */
export function SavedNotice() {
  const location = useLocation();
  const navigate = useNavigate();
  const [message, setMessage] = useState<ActionMessage | null>(null);
  /*
   * Zu welchem Bildschirm die stehende Meldung gehört. Der Pfad ist der Bezug und **nicht** das
   * Vorhandensein von `state.saved`: Der Effekt leert den Verlaufseintrag gleich nach dem Setzen selbst
   * (siehe unten) und läuft dadurch sofort ein zweites Mal ohne `saved`. Ein Räumen an „kein `saved`"
   * löschte die Meldung also im selben Atemzug, in dem sie entstanden ist.
   */
  const gezeigtAuf = useRef<string | null>(null);

  useEffect(() => {
    const saved = (location.state as { saved?: unknown } | null)?.saved;
    if (typeof saved === "string" && saved.length > 0) {
      // Erst im Effekt setzen, nicht als Anfangswert: Viele Screenreader sagen nur an, was in eine
      // *bereits vorhandene* Live-Region hineinwächst. Region (erster Anstrich) und Text (zweiter)
      // müssen darum nacheinander entstehen – dieselbe Begründung wie in `StatusBanner`.
      setMessage({ ok: true, text: saved });
      gezeigtAuf.current = location.pathname;
      // Den Verlaufseintrag räumen, sonst wiederholt ein Neuladen oder ein „Zurück" die Meldung.
      navigate(location.pathname, { replace: true, state: null });
      return;
    }

    // Der Bildschirm hat gewechselt: Was hier stand, gehörte zum vorigen und ist ab jetzt eine
    // Behauptung über etwas, das auf diesem Bildschirm nicht stattgefunden hat.
    if (gezeigtAuf.current !== null && gezeigtAuf.current !== location.pathname) {
      setMessage(null);
      gezeigtAuf.current = null;
    }
  }, [location.state, location.pathname, navigate]);

  return <StatusBanner message={message} />;
}
