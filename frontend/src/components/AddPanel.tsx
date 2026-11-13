import { useId, useState, type ReactNode } from "react";

/**
 * Ein Hinzufügen-Formular, das erst auf Wunsch aufgeht (B-534). Eine Seite zeigt zuerst, was **da** ist; das
 * Formular für Neues steht sonst dauerhaft mit eigenem Absende-Knopf neben dem Bestand und konkurriert mit den
 * Speichern-Knöpfen der Seite um die Aufmerksamkeit.
 *
 * Der Knopf behält seinen Namen (das „+"/„−" ist `aria-hidden`), sein Zustand steht in `aria-expanded`. Er darf
 * **nicht** so heißen wie der Absende-Knopf im Formular – sonst trifft ein Name zwei Knöpfe.
 *
 * `defaultOpen` für den Leerfall: Gibt es noch nichts, ist das Formular der Inhalt der Fläche. Der Wert gilt nur
 * beim Einhängen – darum erst rendern, wenn der Bestand geladen ist.
 */
export function AddPanel({ label, defaultOpen = false, children }: {
  label: string;
  defaultOpen?: boolean;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(defaultOpen);
  const bodyId = useId();
  return (
    <div className="add-panel">
      <button type="button" className={`btn ghost small add-panel-toggle${open ? " open" : ""}`}
        style={{ width: "auto" }} aria-expanded={open} aria-controls={bodyId}
        onClick={() => setOpen((o) => !o)}>
        <span aria-hidden="true">{open ? "− " : "+ "}</span>{label}
      </button>
      {open && <div id={bodyId} className="add-panel-body">{children}</div>}
    </div>
  );
}
