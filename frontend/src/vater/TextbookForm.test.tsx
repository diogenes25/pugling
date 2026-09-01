import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { TextbookForm } from "./ChildMaterialSection";
import { AuthProvider } from "../lib/auth";
import type { SubjectResponse, TextbookResponse } from "../lib/types";

/*
 * B-148. `textbookPatch.test.ts` prüft die REGEL (was in den PATCH-Rumpf gehört) – aber die Lehre aus
 * B-143 ist, dass der Defekt dort gar nicht sitzen muss: Kaputt war, dass das Formular einen Zustand des
 * Modells nicht anzeigen konnte. Für das Lehrbuch am Kind gab es diese Prüfebene bis zu dieser Story
 * überhaupt nicht.
 *
 * Ohne diese Datei liefe ein späteres Entfernen der deaktivierten `<option>` grün durch — und mit ihr
 * fiele der Schutz vorm versehentlichen Löschen weg, denn er hängt daran, dass `form` gleich `loaded`
 * bleiben KANN.
 */

const SUBJECTS: SubjectResponse[] = [
  { id: 1, name: "Englisch", createdAt: "2026-01-01T00:00:00Z", categoriesCount: 0, isMine: false },
  { id: 2, name: "Mathe", createdAt: "2026-01-01T00:00:00Z", categoriesCount: 0, isMine: false },
];

function buch(overrides: Partial<TextbookResponse> = {}): TextbookResponse {
  return {
    id: 4, title: "Access 8", subjectId: 1, subjectName: "Englisch", grade: 8, publisher: "Cornelsen",
    isbn: null, currentChapter: "Unit 4",
    // Ohne Reihe lädt die Unit-Liste nicht – die Komponente kommt so ohne gefälschtes `fetch` aus.
    seriesId: null, currentUnitId: null, ...overrides,
  } as TextbookResponse;
}

// `AuthProvider` ist Pflicht, obwohl nichts geladen wird: `useAsync` hängt für den 401-Fall an `useAuth`,
// und die Unit-Liste benutzt es auch dann, wenn sie ohne Reihe gar nicht abfragt.
const zeige = (o: Partial<TextbookResponse> = {}) =>
  render(
    <AuthProvider>
      <TextbookForm childId={1} book={buch(o)} series={[]} subjects={SUBJECTS} onDone={() => {}} />
    </AuthProvider>,
  );

describe("TextbookForm – Zustände, die das Modell erlaubt", () => {
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

  it("zeigt keine Freitext-Option, wenn gar kein Fach hinterlegt ist", () => {
    // Die Gegenprobe zum Fall darüber: Ohne Namen gibt es nichts zu zeigen, und eine leere Option
    // „ (Freitext)" wäre schlimmer als keine.
    zeige({ subjectId: null, subjectName: null });

    expect((screen.getByLabelText("Fach", { exact: true }) as HTMLSelectElement).value).toBe("");
    expect(screen.queryByRole("option", { name: /Freitext/ })).toBeNull();
  });

  it("beschriftet das Fach-Feld mit einer Erklärung", () => {
    // Das Etikett „(Freitext)" sagt WAS, nicht WARUM – das trägt die Feldhilfe (B-148, Entscheidung 4).
    zeige({ subjectId: null, subjectName: "Erdkunde" });

    expect(screen.getByLabelText("Erklärung zu „Fach\"", { exact: true })).toBeTruthy();
  });
});

/*
 * B-172, nachgetragen nach dem Review: die drei Referenzfelder dieses Formulars waren vom neuen
 * Verhalten völlig unabgedeckt. Gemessen hat der `frontend-reviewer` das, indem er die Unit-Bedingung
 * entfernte – 299/299 blieben grün.
 *
 * Ein gefälschtes `fetch` braucht es dafür nicht: mit gesetzter `seriesId` scheitert der echte
 * `seriesUnits`-Abruf in happy-dom von selbst. Das IST der zu prüfende Fehlerfall.
 *
 * **Der `ECONNREFUSED`-Block in der Ausgabe gehört dazu** – er ist dieser scheiternde Abruf, nicht ein
 * Mangel am Test. Wer ihn wegräumt, räumt den Fehlerfall weg; die Fälle oben halten deshalb weiter an
 * `seriesId: null` fest, wo sie ihn nicht brauchen.
 *
 * Rot macht diese Fälle das Entfernen der Rückfall-Option bzw. der Bedingung `eigeneUnit`.
 */
describe("TextbookForm – wenn eine Optionsliste den geladenen Wert nicht kennt", () => {
  const feld = (name: string) => screen.getByLabelText(name, { exact: true }) as HTMLSelectElement;

  it("zeigt das geladene Fach, wenn die Fächerliste es nicht kennt", () => {
    render(
      <AuthProvider>
        <TextbookForm childId={1} book={buch()} series={[]} subjects={[]} onDone={() => {}} />
      </AuthProvider>,
    );

    expect(feld("Fach").value).toBe("1");
    expect((screen.getByRole("option", { name: "Englisch (nicht geladen)" }) as HTMLOptionElement)
      .disabled).toBe(false);
  });

  it("zeigt die geladene Reihe und die geladene Unit, wenn ihre Listen fehlen", () => {
    zeige({ seriesId: 9, seriesName: "Access", currentUnitId: 7, currentUnitLabel: "Unit 7" });

    expect(feld("Reihe im Katalog").value).toBe("9");
    expect(screen.getByRole("option", { name: "Access (nicht geladen)" })).toBeTruthy();

    expect(feld("Aktuelle Unit").value).toBe("7");
    expect(screen.getByRole("option", { name: "Unit 7 (nicht geladen)" })).toBeTruthy();
  });

  it("nimmt die Unit-Option zurück, sobald die Reihe gewechselt ist", () => {
    // Sonst könnte der Nutzer eine Unit der ALTEN Reihe abschicken. `chooseSeries` räumt den Wert,
    // die Option muss mitgehen – Id UND Name hängen darum an derselben Bedingung.
    zeige({ seriesId: 9, seriesName: "Access", currentUnitId: 7, currentUnitLabel: "Unit 7" });

    fireEvent.change(feld("Reihe im Katalog"), { target: { value: "" } });

    expect(feld("Aktuelle Unit").value).toBe("");
    expect(screen.queryByRole("option", { name: /Unit 7/ })).toBeNull();
  });
});
