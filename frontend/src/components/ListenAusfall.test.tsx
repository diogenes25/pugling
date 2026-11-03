import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ListenAusfall } from "./ListenAusfall";

/*
 * B-188. Die drei Optionslisten des Assistenten (Kinder, Fächer, Arten) zeigten ihren Fehler nirgends.
 * Der Baustein trägt jetzt die Regel; DASS eine Seite ihn benutzt, kann ein Bausteintest nicht sagen —
 * das prüfen `e2e/assistent-listen-fallen-aus.spec.ts` und `e2e/typ-manifest-faellt-aus.spec.ts`.
 *
 * Mit dem Baustein aus `VaterWizard.test.tsx` hierher gezogen (B-215); die vier Fälle sind unverändert.
 */
describe("ListenAusfall", () => {
  it("schweigt, solange nichts schiefgegangen ist", () => {
    const { container } = render(
      <ListenAusfall was="Deine Kinder" fehler={null} laedt={false} onRetry={() => {}} />);

    // Nicht „kein Banner", sondern **gar nichts**: Eine dauerhaft leere Fehlerfläche über jedem Schritt
    // wäre ein Kasten, der immer da ist und nie etwas sagt.
    expect(container.innerHTML).toBe("");
  });

  it("nennt die Liste beim Namen und bietet den Weg zurück an", () => {
    const onRetry = vi.fn();
    render(<ListenAusfall was="Deine Kinder" fehler="Netz weg" laedt={false} onRetry={onRetry} />);

    // Der Name der Liste trägt die Aussage: „konnte nicht geladen werden" allein sagt nicht, WAS fehlt,
    // und der Assistent lädt drei Listen.
    expect(screen.getByRole("alert").textContent).toMatch(/Deine Kinder konnten nicht geladen werden: Netz weg/);

    fireEvent.click(screen.getByRole("button", { name: "Erneut laden" }));
    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it("bleibt während der Wiederholung stehen, statt unter dem Finger zu verschwinden", () => {
    const { rerender } = render(
      <ListenAusfall was="Die Fächer" fehler="Netz weg" laedt={false} onRetry={() => {}} />);

    /*
     * Der Zustand, den `useAsync.reload()` erzeugt: `error` wird `null` und `loading` `true` – im selben
     * Schritt. An `fehler` allein hinge das Banner samt Knopf genau im Moment des Klicks aus: Der gedrückte
     * Knopf verschwände unter dem Finger, der Fokus fiele auf `<body>`, und während der Wiederholung stünde
     * gar nichts da. Der Fall bildet diesen Übergang darum als Rerender nach, nicht als Anfangszustand.
     */
    rerender(<ListenAusfall was="Die Fächer" fehler={null} laedt onRetry={() => {}} />);

    expect(screen.getByRole("alert").textContent).toMatch(/Die Fächer konnten nicht geladen werden: Netz weg/);
    const knopf = screen.getByRole("button", { name: "Lädt…" }) as HTMLButtonElement;
    // `disabled={busy}` an jedem Knopf, dessen Klick etwas auslöst (frontend/CLAUDE.md). Erst durch das
    // Festhalten oben ist dieser Zustand überhaupt erreichbar – vorher war die Sperre toter Code.
    expect(knopf.disabled).toBe(true);
  });

  it("räumt sich ab, wenn die Wiederholung geklappt hat", () => {
    const { rerender, container } = render(
      <ListenAusfall was="Die Fächer" fehler="Netz weg" laedt={false} onRetry={() => {}} />);
    rerender(<ListenAusfall was="Die Fächer" fehler={null} laedt onRetry={() => {}} />);

    // Die Gegenprobe zum Festhalten: Ein Gedächtnis, das nie vergisst, ließe das Banner für immer stehen.
    rerender(<ListenAusfall was="Die Fächer" fehler={null} laedt={false} onRetry={() => {}} />);

    expect(container.innerHTML).toBe("");
  });

  // B-183: an einer Abfrage mit wechselnden Abhängigkeiten (Suche, anderes Kind) ist ein Wechsel keine Wiederholung – der
  // alte Fehler gehört zu einer Abfrage, die es nicht mehr gibt (B-227). Rot, wenn `schluessel` nicht bis zum Gedächtnis
  // durchgereicht wird: dann stünde „Lädt…" mit dem alten Fehler über der neuen Suche.
  it("vergisst den Fehler, wenn die Abfrage eine andere geworden ist", () => {
    const { rerender, container } = render(
      <ListenAusfall was="Die Verlage" fehler="Netz weg" laedt={false} onRetry={() => {}} schluessel="a" />);
    rerender(<ListenAusfall was="Die Verlage" fehler={null} laedt onRetry={() => {}} schluessel="b" />);

    expect(container.innerHTML).toBe("");
  });

  // Gegenprobe zum Fall darüber: unter demselben Schlüssel ist es eine Wiederholung, und das Banner bleibt stehen. Rot,
  // wenn der Baustein unter einem Schlüssel nie mehr festhält (etwa jeden Schlüssel als Wechsel liest).
  it("hält den Fehler unter demselben Schlüssel während der Wiederholung", () => {
    const { rerender } = render(
      <ListenAusfall was="Die Verlage" fehler="Netz weg" laedt={false} onRetry={() => {}} schluessel="a" />);
    rerender(<ListenAusfall was="Die Verlage" fehler={null} laedt onRetry={() => {}} schluessel="a" />);

    expect(screen.getByRole("alert").textContent).toMatch(/Die Verlage konnten nicht geladen werden: Netz weg/);
  });
});
