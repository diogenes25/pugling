import { describe, expect, it } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { ProfileForm } from "./VaterFachlehrer";
import type { SubjectResponse, TextbookSeriesResponse } from "../lib/types";

/*
 * Der Regressionstest zu B-67. `ProfileForm` löst keinen Netzaufruf aus, solange nicht abgesendet wird –
 * darum ist das hier ein Baustein-Test und kein nachgebauter Bildschirm (siehe frontend/CLAUDE.md).
 */

const ENGLISH: SubjectResponse = { id: 1, name: "Englisch", createdAt: "2026-01-01T00:00:00Z", categoriesCount: 0 };

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

    expect((screen.getByLabelText("Fach") as HTMLSelectElement).value).toBe("1");
    expect((screen.getByLabelText("Lernsprache") as HTMLInputElement).value).toBe("en");
    expect((screen.getByLabelText("Muttersprache") as HTMLInputElement).value).toBe("de");
    expect(screen.getAllByText("aus dem Lehrwerk übernommen")).toHaveLength(3);
  });

  it("lässt ein vom Nutzer geändertes Feld beim Wählen der Reihe unverändert", () => {
    render(<ProfileForm subjects={[ENGLISH]} series={[seriesWith({ sourceLanguage: "en" })]} onDone={() => {}} />);

    fireEvent.change(screen.getByLabelText("Lernsprache"), { target: { value: "fr" } });
    fireEvent.change(screen.getByLabelText("Lehrwerk"), { target: { value: "1" } });

    expect((screen.getByLabelText("Lernsprache") as HTMLInputElement).value).toBe("fr");
    expect((screen.getByLabelText("Fach") as HTMLSelectElement).value).toBe("1");
  });

  it("lässt das Fach-Pulldown in Ruhe, wenn die Reihe kein Katalog-Fach trägt", () => {
    render(<ProfileForm
      subjects={[ENGLISH]} series={[seriesWith({ subjectId: null, subjectName: "Freitextfach" })]}
      onDone={() => {}}
    />);

    fireEvent.change(screen.getByLabelText("Lehrwerk"), { target: { value: "1" } });

    expect((screen.getByLabelText("Fach") as HTMLSelectElement).value).toBe("");
    expect((screen.getByLabelText("Lernsprache") as HTMLInputElement).value).toBe("en");
  });
});
