import { useEffect, useRef, type ReactNode } from "react";

/**
 * Ein modaler Dialog mit der Tastatur-Etikette, die ein Dialog braucht: Fokus beim Öffnen hinein, Tab
 * darin gefangen (Fokus-Falle), Escape schließt, beim Schließen wandert der Fokus zurück, wo er war.
 * Das ist der Teil, den man beim zweiten Dialog sonst vergisst – deshalb liegt er hier und nicht in den
 * einzelnen Dialogen.
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
  // Neueste onClose-Referenz im Ref halten: Der Aufrufer übergibt onClose inline (neue Funktion pro Render);
  // ohne das Ref würde die Fokus-Falle bei jedem Eltern-Re-Render neu aufgesetzt und der Fokus zurückgerissen.
  const onCloseRef = useRef(onClose);
  onCloseRef.current = onClose;

  useEffect(() => {
    const dialog = dialogRef.current;
    const previouslyFocused = document.activeElement as HTMLElement | null;
    const focusables = () =>
      dialog
        ? Array.from(
            dialog.querySelectorAll<HTMLElement>(
              'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])',
            ),
          )
        : [];

    // Fokus initial in den Dialog holen (erstes fokussierbares Element, sonst der Container selbst).
    (focusables()[0] ?? dialog)?.focus();

    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") { onCloseRef.current(); return; }
      if (e.key !== "Tab" || !dialog) return;
      const items = focusables();
      if (items.length === 0) { e.preventDefault(); dialog.focus(); return; }
      const first = items[0];
      const last = items[items.length - 1];
      const active = document.activeElement;
      if (e.shiftKey && (active === first || active === dialog)) {
        e.preventDefault(); last.focus();
      } else if (!e.shiftKey && active === last) {
        e.preventDefault(); first.focus();
      }
    };

    window.addEventListener("keydown", onKey);
    return () => {
      window.removeEventListener("keydown", onKey);
      previouslyFocused?.focus?.();
    };
    // Nur beim Öffnen/Schließen – nicht bei jedem Eltern-Re-Render (siehe onCloseRef).
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

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
