import { describe, expect, it } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import {
  createExerciseHref, istGefiltert, newUnitDefaults, SeriesEigentumsHinweis, seriesFilterQuery, UnitForm,
  type SeriesFilterValues,
} from "./VaterLehrwerke";
import type { SeriesUnitResponse, TextbookSeriesResponse } from "../lib/types";

/*
 * Der Regressionstest zu B-129. `UnitForm` löst keinen Netzaufruf aus, solange nicht abgesendet wird –
 * hier wird nur das Themen-Feld bedient, also ein Baustein-Test und kein nachgebauter Bildschirm
 * (siehe frontend/CLAUDE.md).
 *
 * Der Zugriff läuft über den Entfernen-Knopf des Chips (`aria-label`), nicht über seinen Text: der Chip
 * enthält Text UND Knopf, sein `textContent` ist also „Thema×" und trifft keinen exakten Textvergleich.
 */
describe("UnitForm – Themen-Eingabe", () => {
  const feld = () => screen.getByLabelText("Themen der Unit");
  const chip = (thema: string) => screen.queryByRole("button", { name: `Thema ${thema} entfernen` });

  it("verwirft eine angefangene Eingabe bei Escape, statt sie beim Wegklicken anzulegen", () => {
    render(<UnitForm onMeldung={() => {}} seriesId={1} onDone={() => {}} />);

    fireEvent.change(feld(), { target: { value: "Halbfertig" } });
    fireEvent.keyDown(feld(), { key: "Escape" });
    fireEvent.blur(feld());

    expect(chip("Halbfertig")).toBeNull();
    expect((feld() as HTMLInputElement).value).toBe("");
  });

  it("rettet weiterhin eine Eingabe, die ohne Enter verlassen wird", () => {
    render(<UnitForm onMeldung={() => {}} seriesId={1} onDone={() => {}} />);

    fireEvent.change(feld(), { target: { value: "Gerettet" } });
    fireEvent.blur(feld());

    expect(chip("Gerettet")).not.toBeNull();
  });

  it("fügt bei Enter hinzu, ohne das Formular abzuschicken", () => {
    render(<UnitForm onMeldung={() => {}} seriesId={1} onDone={() => {}} />);

    fireEvent.change(feld(), { target: { value: "Mit Enter" } });
    fireEvent.keyDown(feld(), { key: "Enter" });

    expect(chip("Mit Enter")).not.toBeNull();
    expect((feld() as HTMLInputElement).value).toBe("");
  });
});

/*
 * Die Vorbelegung einer neuen Unit (remark #2 vom 2026-08-14). Der zweite Fall ist der, der leicht falsch
 * wird: die Vorbelegung darf nur beim ANLEGEN greifen. Hängt sie am Wert statt am Vorhandensein von `unit`,
 * erbt eine bestehende Unit ohne Band beim blossen Öffnen den Band der Nachbarin – ein stiller Datenwechsel,
 * den erst das Speichern sichtbar machen würde.
 */
describe("Vorbelegung einer neuen Unit", () => {
  const unit = (over: Partial<SeriesUnitResponse> = {}): SeriesUnitResponse => ({
    id: 1, seriesId: 7, label: "Unit 1", orderIndex: 0, grade: 8, bookType: "Workbook",
    topics: [], createdAt: "2026-08-12T00:00:00Z", ...over,
  });

  it("übernimmt Band und Buchtyp der letzten Unit der Reihe", () => {
    // Rot, sobald nicht mehr die LETZTE Unit die Vorlage ist – der Server sortiert nach Band, wer anlegt
    // arbeitet nach hinten.
    expect(newUnitDefaults([unit({ id: 1, grade: 7 }), unit({ id: 2, grade: 8, bookType: "TeacherGuide" })]))
      .toEqual({ grade: "8", bookType: "TeacherGuide" });
  });

  it("startet ohne Units leer und beim Regelfall Lehrbuch", () => {
    expect(newUnitDefaults([])).toEqual({ grade: "", bookType: "Textbook" });
  });

  // Der Zugriff per Regex, weil das Band-Feld seine Einheit im Label trägt („Band (als Klasse)") und
  // `getByLabelText` einen String exakt vergleicht.
  const band = () => screen.getByLabelText(/^Band/) as HTMLInputElement;
  const buchtyp = () => screen.getByLabelText("Buchtyp") as HTMLSelectElement;

  it("lässt eine bestehende Unit bei ihren eigenen Werten, statt sie erben zu lassen", () => {
    // Rot, sobald die Fallunterscheidung am Wert statt am Vorhandensein von `unit` hängt: mit
    // `unit?.grade?.toString() ?? defaults.grade` stünde hier 8 statt "" – und ein Speichern schriebe einen
    // Band, den niemand eingetragen hat.
    render(
      <UnitForm onMeldung={() => {}}
        seriesId={7} unit={unit({ grade: null, bookType: "Textbook" })}
        defaults={{ grade: "8", bookType: "TeacherGuide" }} onDone={() => {}}
      />,
    );

    expect(band().value).toBe("");
    expect(buchtyp().value).toBe("Textbook");
  });

  it("belegt ein Anlege-Formular mit Band und Buchtyp vor", () => {
    render(<UnitForm onMeldung={() => {}} seriesId={7} defaults={{ grade: "8", bookType: "Workbook" }} onDone={() => {}} />);

    expect(band().value).toBe("8");
    expect(buchtyp().value).toBe("Workbook");
  });
});

/*
 * Der Weg Unit → Übung. Geprüft wird die **Adresse**, weil genau sie die Arbeit spart: kommt die Auswahl
 * nicht als Query mit, steht im Anlege-Formular wieder die erste Reihe (frontend/CLAUDE.md).
 */
describe("createExerciseHref", () => {
  const series = (over: Partial<TextbookSeriesResponse> = {}): TextbookSeriesResponse => ({
    id: 7, name: "Access", slug: "access", schoolTypes: "None", isOwn: true, unitCount: 2,
    createdAt: "2026-08-12T00:00:00Z", subjectId: 3, ...over,
  });

  it("reicht Fach, Reihe und Unit ans Anlege-Formular durch", () => {
    expect(createExerciseHref(series(), 42))
      .toBe("/vater/exercises/neu?subjectId=3&seriesId=7&seriesUnitId=42");
  });

  it("lässt ein fehlendes Fach weg, statt subjectId=null zu schicken", () => {
    // Kann nur über einen Aufrufer ohne die `canHostExercises`-Schranke passieren – dann trägt die
    // Adresse lieber kein Fach als eines, das `Number(null) || ""` im Formular zu „irgendwas" macht.
    expect(createExerciseHref(series({ subjectId: null }), 42))
      .toBe("/vater/exercises/neu?seriesId=7&seriesUnitId=42");
  });
});

/*
 * Die Filterzeile über der Reihenliste (remark #1: „die Suche soll auch nach Fach selektiert werden").
 * Geprüft wird die Abbildung Formular → Abfrage, weil dort der stille Fehler sitzt: `Number("")` ist `0`,
 * und ein `subjectId=0` filterte auf ein Fach, das es nicht gibt – die Liste bliebe grundlos leer.
 */
describe("seriesFilterQuery", () => {
  const leer: SeriesFilterValues = { search: "", subjectId: "", publisherId: "", schoolTypes: "", grade: "" };

  it("reicht das gewählte Fach als Zahl an die Abfrage durch", () => {
    // Rot, sobald `subjectId` nicht mehr in die Abfrage wandert – der Fall, den remark #1 gemeldet hat.
    expect(seriesFilterQuery({ ...leer, subjectId: "3" }).subjectId).toBe(3);
  });

  it("lässt jede leere Auswahl ganz weg, statt 0 oder einen leeren String zu filtern", () => {
    // Rot bei `Number(v.subjectId)` ohne die Leer-Prüfung: dann stünde hier 0 statt undefined.
    expect(seriesFilterQuery(leer)).toEqual({
      search: undefined, subjectId: undefined, publisherId: undefined, schoolTypes: undefined, grade: undefined,
    });
  });

  it("trägt die übrigen Filter unverändert mit", () => {
    expect(seriesFilterQuery({ search: "Access", subjectId: "3", publisherId: "7", schoolTypes: "Gymnasium", grade: "6" }))
      .toEqual({ search: "Access", subjectId: 3, publisherId: 7, schoolTypes: "Gymnasium", grade: 6 });
  });
});

/*
 * B-173, zweite Hälfte. „Noch kein Lehrwerk. **Lege unten eines an.**" stand auch dann da, wenn die Reihe
 * längst existierte und nur ausgefiltert war – eine falsche Handlungsanweisung, deren Befolgung eine
 * Dublette im GETEILTEN Katalog erzeugt.
 *
 * Der Ausdruck steht als exportierte Funktion da und nicht im JSX, damit jeder der fünf Filter einzeln
 * belegbar ist: Ein `||`-Ausdruck mit fünf Gliedern, von dem der Test nur eines fährt, sieht grün aus,
 * während vier Glieder fehlen könnten.
 */
describe("istGefiltert – was eine leere Liste bedeutet", () => {
  const KEIN_FILTER = { search: "", subjectId: "", publisherId: "", schoolTypes: "" as const, grade: "" };

  it("sagt Nein, wenn die ganze Zeile leer ist", () => {
    expect(istGefiltert(KEIN_FILTER)).toBe(false);
  });

  it.each([
    ["Suchtext", { search: "Access" }],
    ["Fach", { subjectId: "3" }],
    ["Verlag", { publisherId: "7" }],
    ["Schulart", { schoolTypes: "Gymnasium" as const }],
    ["Klassenstufe", { grade: "8" }],
  ])("sagt Ja bei gesetztem Filter: %s", (_name, teil) => {
    expect(istGefiltert({ ...KEIN_FILTER, ...teil })).toBe(true);
  });
});

/*
 * B-159 – der Zwilling von B-154 (`CatalogAdmin.test.tsx`) eine Katalogebene höher. Bei JEDER nicht eigenen
 * Reihe stand „hat jemand anderes angelegt" – auch bei einer aus dem Grundbestand, die niemandem gehört (im
 * Seed sind das alle). Die zwei Fälle werden getrennt geprüft, weil der falsche Satz nicht bloß fehlen,
 * sondern gar nicht entstehen darf.
 *
 * Rot macht die ersten beiden Fälle das Zusammenlegen der Zweige; den dritten ein strenges `=== null`.
 */
describe("SeriesEigentumsHinweis – wem eine nicht eigene Reihe gehört", () => {
  const reihe = (over: Partial<TextbookSeriesResponse> = {}): TextbookSeriesResponse => ({
    id: 7, name: "Access", slug: "access", schoolTypes: "None", isOwn: false, unitCount: 2,
    createdAt: "2026-08-12T00:00:00Z", subjectId: 3, ...over,
  });

  it("nennt bei einer fremden Reihe den anderen Creator", () => {
    render(<SeriesEigentumsHinweis series={reihe({ ownerAdultId: 42 })} />);

    expect(screen.getByText(/hat jemand anderes angelegt/)).toBeTruthy();
    expect(screen.queryByText(/gehört zum Grundbestand/)).toBeNull();
  });

  it("nennt bei einer Reihe aus dem Grundbestand niemanden als Eigentümer", () => {
    render(<SeriesEigentumsHinweis series={reihe({ ownerAdultId: null })} />);

    expect(screen.getByText(/gehört zum Grundbestand/)).toBeTruthy();
    expect(screen.queryByText(/hat jemand anderes angelegt/)).toBeNull();
  });

  it("behandelt ein fehlendes ownerAdultId wie ownerlos, nicht wie fremd", () => {
    // Der Vertrag gibt `ownerAdultId` optional heraus – ohne das Feld darf der fremde Creator nicht erfunden
    // werden (dieselbe Falle wie in `CatalogAdmin.test.tsx`).
    const ohneFeld = reihe();
    delete (ohneFeld as { ownerAdultId?: number | null }).ownerAdultId;

    render(<SeriesEigentumsHinweis series={ohneFeld} />);

    expect(screen.getByText(/gehört zum Grundbestand/)).toBeTruthy();
  });
});
