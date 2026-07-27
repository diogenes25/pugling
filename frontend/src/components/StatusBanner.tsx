import type { CSSProperties } from "react";
import type { ActionMessage } from "../lib/useAction";

/**
 * Die Rückmeldung einer schreibenden Aktion. Eigene Komponente wegen der beiden Attribute, die man beim
 * Abtippen zuverlässig vergisst: ohne `role="status"` und `aria-live="polite"` bemerkt ein Screenreader
 * weder „Gespeichert." noch die Fehlermeldung – die Einfärbung allein ist keine Rückmeldung.
 *
 * Die Live-Region steht **immer** im DOM, auch ohne Meldung: viele Screenreader sagen nur an, was in eine
 * *bereits vorhandene* Region hineinwächst. Würde die Komponente im Leerfall `null` liefern, entstünde
 * Region und Text gleichzeitig – und die Ansage bliebe aus. Sichtbar (eingefärbter Kasten) wird nur der
 * innere Teil, damit die leere Region nichts einnimmt.
 */
export function StatusBanner({ message, style }: { message: ActionMessage | null; style?: CSSProperties }) {
  return (
    <div role="status" aria-live="polite">
      {message && (
        <div className={`banner ${message.ok ? "ok" : "err"}`} style={{ marginTop: 8, ...style }}>
          {message.text}
        </div>
      )}
    </div>
  );
}
