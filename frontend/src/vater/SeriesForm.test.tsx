import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { SeriesForm } from "./VaterLehrwerke";
import type { PublisherResponse, SubjectResponse, TextbookSeriesResponse } from "../lib/types";

/*
 * B-143. `seriesPatch.test.ts` prüft die REGEL (was in den PATCH-Rumpf gehört) – aber der Defekt der
 * Story saß nicht dort: an der Schulart war `seriesPatch` nie kaputt. Kaputt war, dass das Formular
 * einen Zustand des Modells nicht anzeigen konnte, und das ist genau hier zu sehen.
 *
 * Ohne diese Datei liefe ein späteres Entfernen der beiden deaktivierten `<option>`s grün durch: die
 * Vitests zur Regel blieben grün, und der E2E deckt nur den Fach-Fall ab (die Schulart-Kombination ist
 * über die Oberfläche gar nicht herstellbar, Entscheidungen 5 und 6).
 */

const SUBJECTS: SubjectResponse[] = [
  { id: 1, name: "Englisch", createdAt: "2026-01-01T00:00:00Z", categoriesCount: 0, isMine: false },
  { id: 2, name: "Mathe", createdAt: "2026-01-01T00:00:00Z", categoriesCount: 0, isMine: false },
];
const PUBLISHERS: PublisherResponse[] = [
  { id: 3, name: "Klett", slug: "klett", seriesCount: 1, foreignSeriesCount: 0, createdAt: "2026-01-01T00:00:00Z" },
];

function reihe(overrides: Partial<TextbookSeriesResponse> = {}): TextbookSeriesResponse {
  return {
    id: 7, name: "Access", slug: "access", publisherId: 3, publisherName: "Klett",
    subjectName: "Englisch", subjectId: 1, schoolTypes: "Gymnasium", sourceLanguage: "en",
    targetLanguage: "de", notes: "Aufbau", ownerAdultId: 1, isOwn: true, unitCount: 2,
    gradeMin: 8, gradeMax: 8, createdAt: "2026-01-01T00:00:00Z", ...overrides,
  };
}

const zeige = (o: Partial<TextbookSeriesResponse> = {}) =>
  render(<SeriesForm series={reihe(o)} subjects={SUBJECTS} publishers={PUBLISHERS} onSaved={() => {}} />);

describe("SeriesForm – Zustände, die das Modell erlaubt", () => {
  it("zeigt ein Freitext-Fach als vorausgewählte, gesperrte Option", () => {
    zeige({ subjectId: null, subjectName: "Erdkunde" });

    const feld = screen.getByLabelText("Fach", { exact: true }) as HTMLSelectElement;
    expect(feld.value).toBe("__freetext__");
    const option = screen.getByRole("option", { name: "Erdkunde (Freitext)" }) as HTMLOptionElement;
    // Gesperrt ist die halbe Aussage: so kann der Nutzer den Zustand sehen, aber nicht herstellen.
    expect(option.disabled).toBe(true);
  });

  it("zeigt keine Freitext-Option, wenn das Fach im Katalog steht", () => {
    zeige();

    expect((screen.getByLabelText("Fach", { exact: true }) as HTMLSelectElement).value).toBe("1");
    expect(screen.queryByRole("option", { name: /Freitext/ })).toBeNull();
  });

  it("zeigt eine Schulart-Kombination als vorausgewählte, gesperrte Option", () => {
    // Der Fall, den die Oberfläche nicht erzeugen kann und darum auch kein E2E herstellt: `SchoolTypes`
    // ist ein [Flags]-Enum, eine Kombination reist als freier String (B-60).
    zeige({ schoolTypes: "Realschule, Gymnasium" });

    const feld = screen.getByLabelText("Schulart", { exact: true }) as HTMLSelectElement;
    expect(feld.value).toBe("Realschule, Gymnasium");
    expect((screen.getByRole("option", { name: "Realschule, Gymnasium" }) as HTMLOptionElement).disabled)
      .toBe(true);
  });

  it("zeigt für einen gewöhnlichen Einzelwert keine Zusatz-Option", () => {
    zeige({ schoolTypes: "Gymnasium" });

    const feld = screen.getByLabelText("Schulart", { exact: true }) as HTMLSelectElement;
    expect(feld.value).toBe("Gymnasium");
    // „Gymnasium" steht genau einmal zur Wahl – nicht zusätzlich als gesperrte Kopie.
    expect(screen.getAllByRole("option", { name: "Gymnasium" })).toHaveLength(1);
  });

  it("zeigt für „für alle“ keine Zusatz-Option", () => {
    // `None` ist ein echter Wert und steht bereits als „– für alle –" im Feld; eine zweite Option
    // daneben behauptete eine Kombination, wo keine ist.
    zeige({ schoolTypes: "None" });

    expect((screen.getByLabelText("Schulart", { exact: true }) as HTMLSelectElement).value).toBe("None");
    expect(screen.queryByRole("option", { name: "None" })).toBeNull();
  });
});
