import { describe, expect, it } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { ProfileForm } from "./VaterFachlehrer";
import type { CreatorProfileResponse, ExerciseTypeManifest, SubjectResponse, TextbookSeriesResponse } from "../lib/types";

/*
 * Der Regressionstest zu B-67. `ProfileForm` löst keinen Netzaufruf aus, solange nicht abgesendet wird –
 * darum ist das hier ein Baustein-Test und kein nachgebauter Bildschirm (siehe frontend/CLAUDE.md).
 */

const ENGLISH: SubjectResponse = {
  id: 1, name: "Englisch", createdAt: "2026-01-01T00:00:00Z", categoriesCount: 0, isOwn: false,
};

function seriesWith(overrides: Partial<TextbookSeriesResponse>): TextbookSeriesResponse {
  return {
    id: 1, name: "Access", slug: "access", publisherId: null, publisherName: null, subjectName: "Englisch", subjectId: 1,
    schoolTypes: "Gymnasium", sourceLanguage: "en", targetLanguage: "de", notes: null,
    ownerAdultId: null, isOwn: true, unitCount: 0, gradeMin: null, gradeMax: null,
    createdAt: "2026-01-01T00:00:00Z", ...overrides,
  };
}

describe("ProfileForm – Ableitung aus dem Lehrwerk", () => {
  it("füllt Fach, Lern- und Muttersprache beim Wählen der Reihe", () => {
    render(<ProfileForm onMeldung={() => {}} subjects={[ENGLISH]} series={[seriesWith({})]} onDone={() => {}} />);

    fireEvent.change(screen.getByLabelText("Lehrwerk"), { target: { value: "1" } });

    expect((screen.getByLabelText("Fach", { exact: true }) as HTMLSelectElement).value).toBe("1");
    expect((screen.getByLabelText("Lernsprache") as HTMLInputElement).value).toBe("en");
    expect((screen.getByLabelText("Muttersprache") as HTMLInputElement).value).toBe("de");
    expect(screen.getAllByText("aus dem Lehrwerk übernommen")).toHaveLength(3);
  });

  it("lässt ein vom Nutzer geändertes Feld beim Wählen der Reihe unverändert", () => {
    render(<ProfileForm onMeldung={() => {}} subjects={[ENGLISH]} series={[seriesWith({ sourceLanguage: "en" })]} onDone={() => {}} />);

    fireEvent.change(screen.getByLabelText("Lernsprache"), { target: { value: "fr" } });
    fireEvent.change(screen.getByLabelText("Lehrwerk"), { target: { value: "1" } });

    expect((screen.getByLabelText("Lernsprache") as HTMLInputElement).value).toBe("fr");
    expect((screen.getByLabelText("Fach", { exact: true }) as HTMLSelectElement).value).toBe("1");
  });

  /*
   * B-185. Ist die Fächerliste nicht geladen, hat der abgeleitete Wert keine Option: der `<select>` fiel auf
   * „– fachneutral –", der Hinweis daneben behauptete „aus dem Lehrwerk übernommen", und der POST trug die
   * Id trotzdem. Anzeige und Zustand drifteten. Rot macht das ein Feld, das nur den GELADENEN Wert als
   * Sonderoption kennt – beim Anlegen gibt es keinen, also stand `""` statt der Id.
   */
  it("zeigt das abgeleitete Fach auch dann, wenn die Fächerliste es nicht kennt", () => {
    render(<ProfileForm onMeldung={() => {}} subjects={[]} series={[seriesWith({})]} onDone={() => {}} />);

    fireEvent.change(screen.getByLabelText("Lehrwerk"), { target: { value: "1" } });

    const fach = screen.getByLabelText("Fach", { exact: true }) as HTMLSelectElement;
    expect(fach.value).toBe("1");
    expect(fach.options[fach.selectedIndex].textContent).toBe("Englisch (nicht geladen)");
    // Der Hinweis war nie falsch über den Zustand – jetzt widerspricht ihm das Feld nicht mehr.
    expect(screen.getAllByText("aus dem Lehrwerk übernommen")).toHaveLength(3);
  });

  it("lässt das Fach-Pulldown in Ruhe, wenn die REIHE kein Katalog-Fach trägt", () => {
    // Nicht zu verwechseln mit dem Freitext-Fach des Profils selbst (B-148, weiter unten): Hier trägt
    // die gewählte Reihe eines, steuert also zum Fach-Feld nichts bei.
    render(<ProfileForm onMeldung={() => {}}
      subjects={[ENGLISH]} series={[seriesWith({ subjectId: null, subjectName: "Freitextfach" })]}
      onDone={() => {}}
    />);

    fireEvent.change(screen.getByLabelText("Lehrwerk"), { target: { value: "1" } });

    expect((screen.getByLabelText("Fach", { exact: true }) as HTMLSelectElement).value).toBe("");
    expect((screen.getByLabelText("Lernsprache") as HTMLInputElement).value).toBe("en");
  });

  /*
   * B-132. Die Zusicherung ist nicht „der Hinweis erscheint" (das prüft der erste Fall), sondern „die
   * Region war vorher schon da". Viele Screenreader sagen nur an, was in eine BEREITS VORHANDENE
   * Live-Region hineinwächst; entsteht sie mit ihrem Text zusammen, bleibt die Ansage aus
   * (WCAG 2.2 SC 4.1.3, dieselbe Begründung wie in StatusBanner.tsx).
   */
  it("hält die Hinweis-Regionen dauerhaft im DOM, statt sie mit ihrem Text entstehen zu lassen", () => {
    const { container } = render(<ProfileForm onMeldung={() => {}} subjects={[ENGLISH]} series={[seriesWith({})]} onDone={() => {}} />);

    // Vier Regionen: die drei Feld-Hinweise plus der StatusBanner des Formulars – alle noch ohne Text.
    const vorher = [...container.querySelectorAll('[role="status"]')];
    expect(vorher).toHaveLength(4);
    expect(vorher.filter((n) => n.textContent !== "")).toHaveLength(0);

    fireEvent.change(screen.getByLabelText("Lehrwerk"), { target: { value: "1" } });

    // Dieselben Knoten, nur mit Inhalt – nicht drei neue. Identität statt Anzahl ist hier der Kern.
    const nachher = [...container.querySelectorAll('[role="status"]')];
    expect(nachher).toHaveLength(4);
    expect(nachher.filter((n) => n.textContent === "aus dem Lehrwerk übernommen")).toHaveLength(3);
    expect(vorher.every((n) => nachher.includes(n))).toBe(true);
  });
});

/*
 * B-148. Der zweite Zustand, den das Formular nicht darstellen konnte — und anders als bei B-143 kostete
 * er hier etwas: Der Schalter entstand aus dem Momentanwert, also schickte JEDES Speichern eines
 * beliebigen anderen Feldes `clearSubject` und zerstörte den Fachnamen.
 */
describe("ProfileForm – ein Freitext-Fach am Profil selbst", () => {
  const profil = (o: Partial<CreatorProfileResponse> = {}): CreatorProfileResponse => ({
    id: 4, name: "Englisch 8", subjectId: null, subjectName: "Erdkunde", schoolTypes: "Gymnasium",
    gradeMin: null, gradeMax: null, seriesId: null, sourceLang: "en", targetLang: "de",
    persona: null, didactics: null, defaultTypes: [], active: true, ...o,
  } as CreatorProfileResponse);

  it("zeigt es als vorausgewählte, gesperrte Option", () => {
    render(<ProfileForm onMeldung={() => {}} profile={profil()} subjects={[ENGLISH]} series={[]} onDone={() => {}} />);

    const feld = screen.getByLabelText("Fach", { exact: true }) as HTMLSelectElement;
    expect(feld.value).toBe("__freetext__");
    expect((screen.getByRole("option", { name: "Erdkunde (Freitext)" }) as HTMLOptionElement).disabled)
      .toBe(true);
  });

  /*
   * B-172, nachgetragen nach dem Review: Fach und Lehrwerk dieses Formulars waren vom neuen Verhalten
   * unabgedeckt – der `frontend-reviewer` hat den Rückfall hier abgeschaltet und 299/299 blieben grün.
   * Rot macht diese Fälle das Entfernen der Rückfall-Option.
   */
  it("zeigt das geladene Fach, wenn die Fächerliste es nicht kennt", () => {
    render(<ProfileForm onMeldung={() => {}} profile={profil({ subjectId: 1, subjectName: "Englisch" })} subjects={[]}
      series={[]} onDone={() => {}} />);

    expect((screen.getByLabelText("Fach", { exact: true }) as HTMLSelectElement).value).toBe("1");
    expect((screen.getByRole("option", { name: "Englisch (nicht geladen)" }) as HTMLOptionElement)
      .disabled).toBe(false);
  });

  it("zeigt das geladene Lehrwerk, wenn die Reihenliste es nicht kennt", () => {
    render(<ProfileForm onMeldung={() => {}} profile={profil({ seriesId: 9, seriesName: "Access" })} subjects={[ENGLISH]}
      series={[]} onDone={() => {}} />);

    expect((screen.getByLabelText("Lehrwerk", { exact: true }) as HTMLSelectElement).value).toBe("9");
    expect(screen.getByRole("option", { name: "Access (nicht geladen)" })).toBeTruthy();
  });

  it("zeigt keine Freitext-Option, wenn das Fach im Katalog steht", () => {
    render(<ProfileForm onMeldung={() => {}} profile={profil({ subjectId: 1, subjectName: "Englisch" })} subjects={[ENGLISH]}
      series={[]} onDone={() => {}} />);

    expect((screen.getByLabelText("Fach", { exact: true }) as HTMLSelectElement).value).toBe("1");
    expect(screen.queryByRole("option", { name: /Freitext/ })).toBeNull();
  });

  it("zeigt eine Schulart-Kombination als vorausgewählte, gesperrte Option", () => {
    // Der Fall, den die Oberfläche nicht erzeugen kann: `SchoolTypes` ist ein [Flags]-Enum, eine
    // Kombination reist als freier String (B-60). Ohne die Option stünde das Feld leer — und mit einem
    // auf `None` normalisierten Ladezustand wäre „– für alle –" nicht mehr auszulösen.
    render(<ProfileForm onMeldung={() => {}} profile={profil({ schoolTypes: "Realschule, Gymnasium" })} subjects={[ENGLISH]}
      series={[]} onDone={() => {}} />);

    const feld = screen.getByLabelText("Schulart", { exact: true }) as HTMLSelectElement;
    expect(feld.value).toBe("Realschule, Gymnasium");
    expect((screen.getByRole("option", { name: "Realschule, Gymnasium" }) as HTMLOptionElement).disabled)
      .toBe(true);
  });

  it("zeigt für einen gewöhnlichen Einzelwert keine Zusatz-Option", () => {
    render(<ProfileForm onMeldung={() => {}} profile={profil({ schoolTypes: "Gymnasium" })} subjects={[ENGLISH]}
      series={[]} onDone={() => {}} />);

    expect(screen.getAllByRole("option", { name: "Gymnasium" })).toHaveLength(1);
  });

  it("beschriftet das Fach-Feld mit einer Erklärung", () => {
    render(<ProfileForm onMeldung={() => {}} profile={profil()} subjects={[ENGLISH]} series={[]} onDone={() => {}} />);

    expect(screen.getByLabelText("Erklärung zu „Fach\"", { exact: true })).toBeTruthy();
  });
});

/** B-426: die Typ-Knöpfe zeigen die deutschen Namen aus dem Manifest und speichern dessen Schlüssel. */
describe("ProfileForm – Übungstypen aus dem Manifest", () => {
  const MANIFEST = [
    { type: "Vocabulary", label: "Vokabelkarten" },
    { type: "Cloze", label: "Lückentext" },
    { type: "Birkenbihl", label: "Birkenbihl" },
  ] as ExerciseTypeManifest[];

  // AK 1. Rot, wenn der Knopf den Schlüssel statt des Namens zeigt oder nur eine feste Teilmenge erscheint.
  it("zeigt jeden Typ des Manifests mit seinem deutschen Namen", () => {
    render(<ProfileForm onMeldung={() => {}} subjects={[ENGLISH]} series={[]} exerciseTypes={MANIFEST} onDone={() => {}} />);
    expect(screen.getByRole("button", { name: "Lückentext" })).toBeTruthy();
    expect(screen.getByRole("button", { name: "Birkenbihl" })).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Cloze" })).toBeNull();
  });

  // AK 2. Rot, wenn der gespeicherte Schlüssel nicht auf den Namen abgebildet wird (dann ist „Lückentext" nicht gedrückt).
  it("zeigt einen gespeicherten Schlüssel als gedrückten Namen", () => {
    const profil = { id: 1, name: "P", defaultTypes: ["Cloze"], schoolTypes: "None", active: true, isOwn: true } as unknown as CreatorProfileResponse;
    render(<ProfileForm onMeldung={() => {}} profile={profil} subjects={[ENGLISH]} series={[]} exerciseTypes={MANIFEST} onDone={() => {}} />);
    expect(screen.getByRole("button", { name: "Lückentext" }).getAttribute("aria-pressed")).toBe("true");
    expect(screen.getByRole("button", { name: "Vokabelkarten" }).getAttribute("aria-pressed")).toBe("false");
  });

  // Rot, wenn ein gespeicherter Schlüssel, den das Manifest nicht kennt, verschwindet – er wäre dann nie mehr abwählbar.
  it("lässt einen unbekannten gespeicherten Schlüssel abwählbar", () => {
    const profil = { id: 1, name: "P", defaultTypes: ["Altlast"], schoolTypes: "None", active: true, isOwn: true } as unknown as CreatorProfileResponse;
    render(<ProfileForm onMeldung={() => {}} profile={profil} subjects={[ENGLISH]} series={[]} exerciseTypes={MANIFEST} onDone={() => {}} />);
    const knopf = screen.getByRole("button", { name: "Altlast" });
    fireEvent.click(knopf);
    expect(screen.queryByRole("button", { name: "Altlast" })).toBeNull();
  });
});
