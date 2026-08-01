import { fireEvent, render, screen } from "@testing-library/react";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import { Pager, SortableTh, TruncationHint } from "./ListControls";
import type { SortDir } from "../lib/types";

/**
 * Die Blätter-/Sortier-Bausteine der Vater-Listen. Geprüft werden die **Ränder** und `aria-sort`: ein
 * „Weiter" auf der letzten Seite lädt eine leere Seite (der Server hat nichts mehr), und eine
 * Sortier-Spalte ohne `aria-sort` sieht richtig aus, sagt aber nichts an.
 */
describe("Pager", () => {
  it("bleibt weg, wenn alles auf eine Seite passt", () => {
    const { container } = render(<Pager skip={0} take={25} total={25} onSkip={() => {}} />);
    // Ein Blätterwerk über einer vollständigen Liste ist eine Lüge über weitere Treffer.
    expect(container.querySelector(".pager")).toBeNull();
  });

  it("sperrt Zurück auf der ersten Seite und zählt von 1 an", () => {
    render(<Pager skip={0} take={25} total={60} onSkip={() => {}} />);

    expect(screen.getByRole("button", { name: "‹ Zurück" }).hasAttribute("disabled")).toBe(true);
    expect(screen.getByRole("button", { name: "Weiter ›" }).hasAttribute("disabled")).toBe(false);
    // Die Anzeige ist 1-basiert, `skip` 0-basiert – die Verwechslung wäre unsichtbar und dauerhaft falsch.
    expect(screen.getByText("1–25 von 60")).toBeDefined();
  });

  it("sperrt Weiter auf der letzten Seite, auch wenn sie nicht voll ist", () => {
    render(<Pager skip={50} take={25} total={60} onSkip={() => {}} />);

    expect(screen.getByRole("button", { name: "Weiter ›" }).hasAttribute("disabled")).toBe(true);
    expect(screen.getByRole("button", { name: "‹ Zurück" }).hasAttribute("disabled")).toBe(false);
    // Nicht „51–75": die Obergrenze ist der Gesamtbestand, nicht das Seitenende.
    expect(screen.getByText("51–60 von 60")).toBeDefined();
  });
});

describe("TruncationHint", () => {
  it("schweigt bei vollständiger Liste und meldet die Kappung sonst", () => {
    const { container, unmount } = render(<TruncationHint shown={12} total={12} />);
    expect(container.textContent).toBe("");
    unmount();

    render(<TruncationHint shown={12} total={40} />);
    // Eine still gekappte Auswahlliste liest sich wie „mehr gibt es nicht".
    expect(screen.getByRole("status").textContent).toContain("Zeigt 12 von 40 Treffern");
  });
});

/** Ein Kopf, der seine Sortierung selbst hält – so wie die Bildschirme ihn benutzen. */
function SortierbarerKopf() {
  const [sort, setSort] = useState<{ key: string; dir: SortDir }>({ key: "title", dir: "asc" });
  return (
    <table><thead><tr>
      <SortableTh label="Titel" sortKey="title" active={sort.key === "title"} dir={sort.dir}
        onSort={(key, dir) => setSort({ key, dir })} />
      <SortableTh label="Punkte" sortKey="points" active={sort.key === "points"} dir={sort.dir} numeric
        onSort={(key, dir) => setSort({ key, dir })} />
    </tr></thead></table>
  );
}

describe("SortableTh", () => {
  it("sagt die aktive Spalte und ihre Richtung über `aria-sort` an", () => {
    render(<SortierbarerKopf />);
    const [titel, punkte] = screen.getAllByRole("columnheader");

    expect(titel.getAttribute("aria-sort")).toBe("ascending");
    // Die inaktive Spalte trägt ausdrücklich „none" – ein fehlendes Attribut wäre nicht dasselbe.
    expect(punkte.getAttribute("aria-sort")).toBe("none");
  });

  it("dreht die Richtung auf der aktiven Spalte und beginnt bei einer neuen aufsteigend", () => {
    render(<SortierbarerKopf />);
    const [titel, punkte] = screen.getAllByRole("columnheader");

    // `fireEvent`, nicht `node.click()`: nur der Weg über Testing Library läuft in `act` und spült das
    // State-Update ein – sonst prüft die Zusicherung den Stand **vor** dem Klick und ist grün wie rot.
    fireEvent.click(screen.getByRole("button", { name: /Titel/ }));
    expect(titel.getAttribute("aria-sort")).toBe("descending");

    fireEvent.click(screen.getByRole("button", { name: /Punkte/ }));
    // Der Wechsel auf eine andere Spalte fängt aufsteigend an, statt die Richtung mitzunehmen – sonst
    // sortiert der erste Klick auf „Punkte" absteigend, ohne dass jemand das verlangt hat.
    expect(punkte.getAttribute("aria-sort")).toBe("ascending");
    expect(titel.getAttribute("aria-sort")).toBe("none");
  });
});
