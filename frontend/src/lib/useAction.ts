import { useCallback, useState } from "react";
import { errorMessage } from "./api";

/** Rückmeldung einer Mutation; `ok` entscheidet über die Einfärbung des Banners. */
export interface ActionMessage {
  ok: boolean;
  text: string;
}

/** Zustand einer schreibenden Aktion: läuft sie, und was kam dabei heraus. */
export interface ActionState {
  /** `true`, solange eine Mutation läuft – damit Knöpfe sperren und Doppel-POSTs ausbleiben. */
  busy: boolean;
  /** Letzte Rückmeldung oder `null` (noch nichts getan bzw. bewusst geräumt). */
  message: ActionMessage | null;
  /**
   * Führt die Mutation aus und hält Erfolg/Fehler fest. Der Rückgabewert sagt, ob es geklappt hat – daran
   * hängt der Aufrufer sein Aufräumen (Eingabefeld leeren, Dialog schließen). Ohne `okText` bleibt das
   * Banner bei Erfolg leer; das ist für Panels gedacht, die sich schlicht neu laden.
   */
  run: (fn: () => Promise<unknown>, okText?: string) => Promise<boolean>;
  /**
   * Wie `run`, liefert aber das **Ergebnis** der Mutation (bei Fehler `null`). Für die Fälle, in denen der
   * Aufrufer die Antwort des Servers braucht – etwa den vergebenen Namen für die Erfolgsmeldung.
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

  const runFor = useCallback(async <T,>(fn: () => Promise<T>, okText?: string): Promise<T | null> => {
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
      setBusy(false);
    }
  }, []);

  // `run` ist der Normalfall: die meisten Mutationen liefern `void`, und dort wäre ein `T | null` als
  // Erfolgssignal irreführend (ein erfolgreiches DELETE gäbe `undefined` zurück).
  const run = useCallback(async (fn: () => Promise<unknown>, okText?: string): Promise<boolean> => {
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
      setBusy(false);
    }
  }, []);

  const fail = useCallback((text: string) => setMessage({ ok: false, text }), []);
  const succeed = useCallback((text: string) => setMessage({ ok: true, text }), []);
  const clear = useCallback(() => setMessage(null), []);

  return { busy, message, run, runFor, fail, succeed, clear };
}
