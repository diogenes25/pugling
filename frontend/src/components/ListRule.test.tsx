import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ListRule } from "./ListRule";

/**
 * B-77: Eine ungeordnete Liste ergab sechzehn zeichengleiche Karten, jede verlangte ein bestimmtes
 * Bundesland, und nichts sagte welches. Der Server sagt jetzt je Karte, welche Regel gilt – hier wird
 * geprüft, dass die beiden Fälle beim Kind auch **unterschiedlich** ankommen.
 */
describe("ListRule", () => {
  it("nennt bei einer Menge die Regel statt einer Position", () => {
    render(<ListRule type="List" anyOrder itemIndex={7} />);

    expect(screen.getByText(/noch nicht genannt hast/)).toBeTruthy();
    // Eine Position wäre hier eine Lüge: jede offene Antwort zählt, nicht die achte.
    expect(screen.queryByText(/Eintrag \d/)).toBeNull();
  });

  it("adressiert bei einer Folge den Eintrag – 1-basiert für das Kind", () => {
    render(<ListRule type="List" anyOrder={false} itemIndex={7} />);

    expect(screen.getByText("Eintrag 8")).toBeTruthy();
  });

  /**
   * Die Mengen-Zeile hängt am Feld, nicht am Typ: sie ist für jeden mengenweise bewerteten Typ richtig,
   * und ein künftiger bekäme sie ohne Änderung hier.
   */
  it("zeigt die Mengen-Regel auch ohne bekannten Typ", () => {
    render(<ListRule anyOrder itemIndex={0} />);

    expect(screen.getByText(/noch nicht genannt hast/)).toBeTruthy();
  });

  /**
   * Die beiden Formen, die der **Vertrag** zulässt: `anyOrder` ist optional (`boolean | undefined`) und
   * `TestItem.type` nullable. Ein fehlendes Feld – etwa gegen ein älteres Backend – darf eine ungeordnete
   * Liste nicht still als Folge ausgeben, und ein fehlender Typ darf nichts erfinden.
   */
  it("behandelt ein fehlendes `anyOrder` wie eine Folge und einen fehlenden Typ als unbekannt", () => {
    const { container: ohneFlag } = render(<ListRule type="List" itemIndex={7} />);
    expect(ohneFlag.textContent).toBe("Eintrag 8");

    const { container: ohneTyp } = render(<ListRule type={null} anyOrder={false} itemIndex={7} />);
    expect(ohneTyp.textContent).toBe("");
  });

  it("schweigt bei jedem anderen Übungstyp", () => {
    const { container } = render(<ListRule type="Vocabulary" anyOrder={false} itemIndex={3} />);

    // Sonst stünde unter jeder Vokabelkarte ein „Eintrag 4", das nichts adressiert.
    expect(container.textContent).toBe("");
  });
});
