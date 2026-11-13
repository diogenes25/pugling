import { useEffect, useRef, type RefObject } from "react";

/**
 * Die Tastatur-Etikette eines modalen Dialogs: Fokus beim Öffnen hinein, Tab darin gefangen (Fokus-Falle), Escape
 * schließt, beim Schließen wandert der Fokus zurück, wo er war. Das ist der Teil, den man beim zweiten Dialog sonst
 * vergisst – und `aria-modal="true"` ist ohne die Falle eine Lüge: ein Screenreader blendet den Hintergrund aus,
 * während Tab dorthin wandert. Der Hook liegt hier, weil ihn zwei Dialoge brauchen (`Modal`, das Blatt des
 * Lernpfads), die sonst nichts teilen.
 *
 * `initialFocus` nennt das Element, das beim Öffnen den Fokus bekommt; ohne Angabe das erste fokussierbare.
 */
export function useDialogFocus(
  dialogRef: RefObject<HTMLElement | null>,
  onClose: () => void,
  initialFocus?: RefObject<HTMLElement | null>,
) {
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

    // Fokus initial in den Dialog holen (gewünschtes Element, sonst das erste fokussierbare, sonst der Container).
    (initialFocus?.current ?? focusables()[0] ?? dialog)?.focus();

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
      } else if (!e.shiftKey && (active === last || !dialog.contains(active))) {
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
}
