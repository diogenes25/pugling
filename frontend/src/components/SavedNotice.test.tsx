import { describe, expect, it } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { Link, MemoryRouter, Route, Routes, useNavigate } from "react-router-dom";
import { SavedNotice } from "./SavedNotice";

/*
 * B-209. `SavedNotice` sitzt in der Sohn-Shell **über** den Routen und bleibt die ganze Sitzung montiert.
 * Es zeigt die Bestätigung einer Aktion, die ihren Bildschirm verlässt — und räumte sie nie wieder ab:
 * „Gespeichert – deine Übungen passen sich an." stand danach über Shop, Weg und Konto.
 *
 * Warum hier und nicht (nur) im E2E: Die Regel ist die des Bausteins, und sie hat zwei Hälften, die
 * einander widersprechen können — räumen beim Bildschirmwechsel, aber **nicht** beim eigenen
 * `navigate(state: null)`, mit dem der Baustein den Verlaufseintrag selbst leert. Ein Fix, der nur die
 * erste Hälfte kennt, löscht die Meldung im selben Atemzug, in dem er sie gesetzt hat. Beide Hälften
 * stehen darum als eigener Fall.
 *
 * Der Weg durch die App fährt zusätzlich `e2e/bestaetigung-reist-nicht-mit.spec.ts`.
 */

const MELDUNG = "Gespeichert – deine Übungen passen sich an.";

/**
 * Die Sohn-Shell im Kleinen: ein dauerhaft montiertes `SavedNotice` über wechselnden Routen, betreten mit
 * einer Meldung im Verlaufseintrag — genau so, wie `SohnInterests` nach dem Speichern springt.
 */
function Shell() {
  return (
    <MemoryRouter initialEntries={[{ pathname: "/sohn", state: { saved: MELDUNG } }]}>
      <SavedNotice />
      <Routes>
        <Route path="/sohn" element={<Link to="/sohn/shop">Zum Shop</Link>} />
        <Route path="/sohn/shop" element={<Zurueck />} />
      </Routes>
    </MemoryRouter>
  );
}

/**
 * Echtes Zurück, kein `<Link to="/sohn">`. Der Unterschied ist der ganze dritte Fall: Ein Link legt einen
 * **neuen** Verlaufseintrag ohne `state` an – dort erschiene die Meldung auch dann nicht, wenn nie
 * aufgeräumt worden wäre. Nur das Zurückgehen holt den ursprünglichen Eintrag zurück und kann damit
 * belegen, dass er wirklich geleert ist.
 */
function Zurueck() {
  const navigate = useNavigate();
  return <button type="button" onClick={() => navigate(-1)}>Zurück</button>;
}

describe("SavedNotice", () => {
  it("zeigt die Meldung auf dem Bildschirm, zu dem sie gehört", () => {
    render(<Shell />);

    // Trägt mehr als es aussieht: Der Baustein leert seinen eigenen Verlaufseintrag direkt nach dem
    // Setzen (`navigate(state: null)`) und rendert dadurch sofort ein zweites Mal ohne `saved`. Ein
    // Räumen, das nur „kein saved" prüft, löschte die Meldung genau hier wieder weg.
    expect(screen.getByText(MELDUNG)).toBeTruthy();
  });

  it("räumt sie ab, sobald das Kind auf einen anderen Bildschirm geht", () => {
    render(<Shell />);
    expect(screen.getByText(MELDUNG)).toBeTruthy();

    fireEvent.click(screen.getByText("Zum Shop"));

    expect(screen.queryByText(MELDUNG)).toBeNull();
  });

  it("bringt sie beim Zurückgehen nicht wieder mit", () => {
    render(<Shell />);
    fireEvent.click(screen.getByText("Zum Shop"));
    fireEvent.click(screen.getByText("Zurück"));

    // Der Verlaufseintrag ist beim ersten Zeigen geleert worden (`navigate(state: null, replace)`); das
    // Zurückgehen betritt genau diesen Eintrag wieder. Ohne das Leeren stünde die alte Bestätigung hier
    // erneut – eine Meldung über etwas, das Minuten her ist.
    expect(screen.queryByText(MELDUNG)).toBeNull();
  });
});
