import { useCallback, useRef, useState } from "react";
import { errorMessage } from "./api";

/** Rückmeldung einer Mutation; `ok` entscheidet über die Einfärbung des Banners. */
export interface ActionMessage {
  ok: boolean;
  text: string;
}

/** Zustand einer schreibenden Aktion: läuft sie, und was kam dabei heraus. */
export interface ActionState {
  /**
   * `true`, solange eine Mutation läuft – damit Knöpfe sperren. Ein Knopf, der eine Mutation auslöst,
   * trägt `disabled={busy}`: das ist der **sichtbare** Teil und der Punkt, auf den Playwrights
   * Actionability wartet. Gegen den Doppel-POST schützt er nicht (dazu die Sperre in `run`/`runFor`) –
   * aber er ist der Grund, warum eine verworfene zweite Aktion nicht wie „nichts passiert" aussieht.
   */
  busy: boolean;
  /** Letzte Rückmeldung oder `null` (noch nichts getan bzw. bewusst geräumt). */
  message: ActionMessage | null;
  /**
   * Führt die Mutation aus und hält Erfolg/Fehler fest. Der Rückgabewert sagt, ob es geklappt hat – daran
   * hängt der Aufrufer sein Aufräumen (Eingabefeld leeren, Dialog schließen). Ohne `okText` bleibt das
   * Banner bei Erfolg leer; das ist für Panels gedacht, die sich schlicht neu laden.
   *
   * **Läuft schon eine Aktion dieser Instanz, wird der Aufruf verworfen** und `false` geliefert – ohne
   * Meldung, ohne Server-Aufruf (siehe Sperre unten).
   */
  run: (fn: () => Promise<unknown>, okText?: string) => Promise<boolean>;
  /**
   * Wie `run`, liefert aber das **Ergebnis** der Mutation (bei Fehler `null`). Für die Fälle, in denen der
   * Aufrufer die Antwort des Servers braucht – etwa den vergebenen Namen für die Erfolgsmeldung.
   *
   * Ein verworfener Aufruf (Sperre) liefert ebenfalls `null` – für den Aufrufer nicht unterscheidbar von
   * einem Fehler, und das ist richtig: in beiden Fällen darf er nicht aufräumen.
   */
  runFor: <T>(fn: () => Promise<T>, okText?: string) => Promise<T | null>;
  /** Eine Fehlermeldung ohne Server-Aufruf setzen (Eingabeprüfung vor dem Absenden). */
  fail: (text: string) => void;
  /** Eine Erfolgsmeldung ohne Server-Aufruf setzen – etwa „Nichts zu speichern.". */
  succeed: (text: string) => void;
  /** Meldung räumen. */
  clear: () => void;
}

/**
 * Der schreibende Gegenpart zu <c>useAsync</c>: eine Mutation ausführen und dabei „läuft gerade" plus
 * Rückmeldung halten.
 *
 * Vorher trug jedes Panel dieselben zwei <c>useState</c>s und dieselbe try/catch/finally-Kaskade – über ein
 * Dutzend Kopien, die auf Fehler jeweils etwas anders reagierten (mal blieb das Formular gefüllt, mal nicht).
 * <c>401</c> muss hier nicht behandelt werden: den fängt der API-Client global ab und beendet die Sitzung.
 */
export function useAction(): ActionState {
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<ActionMessage | null>(null);
  /*
   * Die Wiedereintritts-Sperre. Ein Ref, kein State: `busy` steht erst nach dem Re-Render am Knopf, und
   * zwei Klicks im selben Tick kamen darum beide durch – zwei POSTs, beim Umschalten sogar ein Flip-Flop
   * zurück in den Ausgangszustand mit Erfolgsmeldung. Der Ref greift synchron.
   *
   * Er gilt je **Hook-Instanz**, nicht je Knopf. Wo ein Panel eine Instanz für eine ganze Liste hält
   * (`PlanPositions`, `VaterShop`, `VaterZiele`), sperrt eine laufende Aktion darum auch die übrigen
   * Zeilen. Ein Schlüssel-Parameter wurde erwogen und verworfen (die Warteschlange, die er nach sich
   * zöge, stellt Fragen, die niemand gestellt hat) – sichtbar bleibt das über `disabled={busy}`.
   */
  const running = useRef(false);

  const runFor = useCallback(async <T,>(fn: () => Promise<T>, okText?: string): Promise<T | null> => {
    if (running.current) return null;
    running.current = true;
    setBusy(true);
    setMessage(null);
    try {
      const result = await fn();
      if (okText) setMessage({ ok: true, text: okText });
      return result;
    } catch (err) {
      setMessage({ ok: false, text: errorMessage(err) });
      return null;
    } finally {
      // Ohne das `finally` bliebe die Maske nach dem ersten Fehler tot.
      running.current = false;
      setBusy(false);
    }
  }, []);

  // `run` ist der Normalfall: die meisten Mutationen liefern `void`, und dort wäre ein `T | null` als
  // Erfolgssignal irreführend (ein erfolgreiches DELETE gäbe `undefined` zurück).
  const run = useCallback(async (fn: () => Promise<unknown>, okText?: string): Promise<boolean> => {
    if (running.current) return false;
    running.current = true;
    setBusy(true);
    setMessage(null);
    try {
      await fn();
      if (okText) setMessage({ ok: true, text: okText });
      return true;
    } catch (err) {
      setMessage({ ok: false, text: errorMessage(err) });
      return false;
    } finally {
      running.current = false;
      setBusy(false);
    }
  }, []);

  const fail = useCallback((text: string) => setMessage({ ok: false, text }), []);
  const succeed = useCallback((text: string) => setMessage({ ok: true, text }), []);
  const clear = useCallback(() => setMessage(null), []);

  return { busy, message, run, runFor, fail, succeed, clear };
}
