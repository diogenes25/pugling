import { useRef, type ReactNode } from "react";
import { useDialogFocus } from "../lib/useDialogFocus";

/**
 * Ein modaler Dialog mit der Tastatur-Etikette, die ein Dialog braucht: Fokus beim Öffnen hinein, Tab
 * darin gefangen (Fokus-Falle), Escape schließt, beim Schließen wandert der Fokus zurück, wo er war.
 * Die Etikette selbst liegt in `useDialogFocus` – das Blatt des Lernpfads braucht dieselbe (B-525).
 *
 * Klick auf den Hintergrund schließt; `onMouseDown` (nicht `onClick`), damit eine Textauswahl, die im
 * Dialog beginnt und außerhalb endet, ihn nicht zuklappt.
 */
export function Modal({ label, onClose, maxWidth = 620, children }: {
  /** Vorgelesener Name des Dialogs (`aria-label`). */
  label: string;
  onClose: () => void;
  maxWidth?: number;
  children: ReactNode;
}) {
  const dialogRef = useRef<HTMLDivElement | null>(null);
  useDialogFocus(dialogRef, onClose);

  return (
    <div ref={dialogRef} tabIndex={-1} style={backdrop} role="dialog" aria-modal="true" aria-label={label}
      onMouseDown={onClose}>
      <div className="card" style={{ ...sheet, maxWidth }} onMouseDown={(e) => e.stopPropagation()}>
        {children}
      </div>
    </div>
  );
}

const backdrop: React.CSSProperties = {
  position: "fixed", inset: 0, background: "rgba(0,0,0,.45)", zIndex: 1000,
  display: "flex", alignItems: "flex-start", justifyContent: "center", padding: "5vh 16px", overflowY: "auto",
  overscrollBehavior: "contain",
};
const sheet: React.CSSProperties = {
  width: "100%", display: "flex", flexDirection: "column", gap: 14,
};
