import { describe, expect, it } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { ProfileForm } from "./VaterFachlehrer";
import type { CreatorProfileResponse, SubjectResponse, TextbookSeriesResponse } from "../lib/types";

/*
 * Der Regressionstest zu B-67. `ProfileForm` löst keinen Netzaufruf aus, solange nicht abgesendet wird –
 * darum ist das hier ein Baustein-Test und kein nachgebauter Bildschirm (siehe frontend/CLAUDE.md).
 */

const ENGLISH: SubjectResponse = {
  id: 1, name: "Englisch", createdAt: "2026-01-01T00:00:00Z", categoriesCount: 0, isMine: false,
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
    render(<ProfileForm subjects={[ENGLISH]} series={[seriesWith({})]} onDone={() => {}} />);

    fireEvent.change(screen.getByLabelText("Lehrwerk"), { target: { value: "1" } });

    expect((screen.getByLabelText("Fach", { exact: true }) as HTMLSelectElement).value).toBe("1");
    expect((screen.getByLabelText("Lernsprache") as HTMLInputElement).value).toBe("en");
    expect((screen.getByLabelText("Muttersprache") as HTMLInputElement).value).toBe("de");
    expect(screen.getAllByText("aus dem Lehrwerk übernommen")).toHaveLength(3);
  });

  it("lässt ein vom Nutzer geändertes Feld beim Wählen der Reihe unverändert", () => {
    render(<ProfileForm subjects={[ENGLISH]} series={[seriesWith({ sourceLanguage: "en" })]} onDone={() => {}} />);

    fireEvent.change(screen.getByLabelText("Lernsprache"), { target: { value: "fr" } });
    fireEvent.change(screen.getByLabelText("Lehrwerk"), { target: { value: "1" } });

    expect((screen.getByLabelText("Lernsprache") as HTMLInputElement).value).toBe("fr");
    expect((screen.getByLabelText("Fach", { exact: true }) as HTMLSelectElement).value).toBe("1");
  });

  it("lässt das Fach-Pulldown in Ruhe, wenn die REIHE kein Katalog-Fach trägt", () => {
    // Nicht zu verwechseln mit dem Freitext-Fach des Profils selbst (B-148, weiter unten): Hier trägt
    // die gewählte Reihe eines, steuert also zum Fach-Feld nichts bei.
    render(<ProfileForm
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
    const { container } = render(<ProfileForm subjects={[ENGLISH]} series={[seriesWith({})]} onDone={() => {}} />);

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
    render(<ProfileForm profile={profil()} subjects={[ENGLISH]} series={[]} onDone={() => {}} />);

    const feld = screen.getByLabelText("Fach", { exact: true }) as HTMLSelectElement;
    expect(feld.value).toBe("__freetext__");
    expect((screen.getByRole("option", { name: "Erdkunde (Freitext)" }) as HTMLOptionElement).disabled)
      .toBe(true);
  });

  it("zeigt keine Freitext-Option, wenn das Fach im Katalog steht", () => {
    render(<ProfileForm profile={profil({ subjectId: 1, subjectName: "Englisch" })} subjects={[ENGLISH]}
      series={[]} onDone={() => {}} />);

    expect((screen.getByLabelText("Fach", { exact: true }) as HTMLSelectElement).value).toBe("1");
    expect(screen.queryByRole("option", { name: /Freitext/ })).toBeNull();
  });

  it("zeigt eine Schulart-Kombination als vorausgewählte, gesperrte Option", () => {
    // Der Fall, den die Oberfläche nicht erzeugen kann: `SchoolTypes` ist ein [Flags]-Enum, eine
    // Kombination reist als freier String (B-60). Ohne die Option stünde das Feld leer — und mit einem
    // auf `None` normalisierten Ladezustand wäre „– für alle –" nicht mehr auszulösen.
    render(<ProfileForm profile={profil({ schoolTypes: "Realschule, Gymnasium" })} subjects={[ENGLISH]}
      series={[]} onDone={() => {}} />);

    const feld = screen.getByLabelText("Schulart", { exact: true }) as HTMLSelectElement;
    expect(feld.value).toBe("Realschule, Gymnasium");
    expect((screen.getByRole("option", { name: "Realschule, Gymnasium" }) as HTMLOptionElement).disabled)
      .toBe(true);
  });

  it("zeigt für einen gewöhnlichen Einzelwert keine Zusatz-Option", () => {
    render(<ProfileForm profile={profil({ schoolTypes: "Gymnasium" })} subjects={[ENGLISH]}
      series={[]} onDone={() => {}} />);

    expect(screen.getAllByRole("option", { name: "Gymnasium" })).toHaveLength(1);
  });

  it("beschriftet das Fach-Feld mit einer Erklärung", () => {
    render(<ProfileForm profile={profil()} subjects={[ENGLISH]} series={[]} onDone={() => {}} />);

    expect(screen.getByLabelText("Erklärung zu „Fach\"", { exact: true })).toBeTruthy();
  });
});
