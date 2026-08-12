import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { NameRow, SubjectRow } from "./CatalogAdmin";
import type { SubjectResponse } from "../lib/types";

/*
 * Der Regressionstest zu B-154. Geprüft wird `SubjectRow` einzeln, nicht die ganze `CatalogAdmin`: die
 * lädt beim Fachwechsel die Arten nach und hinge damit am Netz – Bausteine hier, Wege durch die App bei
 * Playwright (frontend/CLAUDE.md). Kein Test dieses Frontends mockt `../lib/api`, und das bleibt so.
 *
 * Der Fehler, den er festhält: Seit B-13 verweigert der Server `PATCH`/`DELETE` an einem fremden **und**
 * an einem ownerlosen Fach mit `403 not_owner`. Die Zeile bot „OK" und „Löschen" trotzdem an jedem Fach
 * an, und der Löschdialog zählte vorher noch auf, was alles seine Zuordnung verliert. Die Antwort trug
 * `isMine` von Anfang an – gelesen wurde es nicht.
 *
 * Warum die zwei Nicht-Eigentümer-Fälle getrennt geprüft werden: „hat jemand anderes angelegt" ist bei
 * einem Fach aus dem Grundbestand nicht bloß unschön, sondern falsch – es gehört niemandem.
 */

function subject(over: Partial<SubjectResponse> = {}): SubjectResponse {
  return {
    id: 3, name: "Englisch", createdAt: "2026-08-12T00:00:00Z", categoriesCount: 2,
    ownerAdultId: 1, isMine: true, ...over,
  };
}

const okKnopf = () => screen.queryByRole("button", { name: /speichern$/ });
const loeschKnopf = () => screen.queryByRole("button", { name: /löschen$/ });

describe("SubjectRow – Eigentum entscheidet, was angeboten wird", () => {
  it("bietet am eigenen Fach Umbenennen und Löschen an – und speichert wirklich", () => {
    const onSave = vi.fn();
    render(<SubjectRow subject={subject()} busy={false} onSave={onSave} onDelete={() => {}} />);

    const feld = screen.getByLabelText("Fach umbenennen");
    expect(loeschKnopf()).not.toBeNull();
    // „OK" erscheint erst bei echter Änderung (`dirty`), darum wird hier getippt statt nur geschaut:
    // ohne diesen Schritt wäre jede Zusicherung auf den OK-Knopf leer und könnte nie fehlschlagen.
    expect(okKnopf()).toBeNull();

    fireEvent.change(feld, { target: { value: "Englisch neu" } });

    expect(okKnopf()).not.toBeNull();
    fireEvent.click(okKnopf()!);
    expect(onSave).toHaveBeenCalledWith("Englisch neu");
  });

  it("zeigt am fremden Fach keine Knöpfe, sondern den Grund", () => {
    render(<SubjectRow subject={subject({ isMine: false, ownerAdultId: 7 })}
      busy={false} onSave={() => {}} onDelete={() => {}} />);

    expect(screen.queryByLabelText("Fach umbenennen")).toBeNull();
    expect(okKnopf()).toBeNull();
    expect(loeschKnopf()).toBeNull();
    expect(screen.getByText(/hat jemand anderes angelegt/)).toBeTruthy();
  });

  it("nennt beim Fach aus dem Grundbestand niemanden als Eigentümer", () => {
    render(<SubjectRow subject={subject({ isMine: false, ownerAdultId: null })}
      busy={false} onSave={() => {}} onDelete={() => {}} />);

    expect(loeschKnopf()).toBeNull();
    expect(screen.getByText(/gehört zum Grundbestand/)).toBeTruthy();
    // Der falsche Satz darf nicht bloß fehlen, er darf hier gar nicht entstehen.
    expect(screen.queryByText(/hat jemand anderes angelegt/)).toBeNull();
  });

  it("behandelt ein fehlendes ownerAdultId wie ownerlos, nicht wie fremd", () => {
    // Der Vertrag gibt `ownerAdultId` optional heraus (`ownerAdultId?: number | null`) – ein strenges
    // `=== null` hätte hier den Satz über den fremden Creator erzeugt, und der wäre erfunden.
    const ohneFeld = subject({ isMine: false });
    delete (ohneFeld as { ownerAdultId?: number | null }).ownerAdultId;

    render(<SubjectRow subject={ohneFeld} busy={false} onSave={() => {}} onDelete={() => {}} />);

    expect(screen.getByText(/gehört zum Grundbestand/)).toBeTruthy();
  });
});

/*
 * Die Gegenprobe zur Verengung: Die „Art" (Kategorie) trägt serverseitig **kein** Eigentum
 * (`ExerciseCategory` hat kein Owner-Feld, `ExerciseCategoriesController` prüft nur die Creator-Rolle),
 * also darf die Oberfläche dort auch keines erfinden. Belegt wird hier, dass `NameRow` selbst
 * bedingungslos beide Knöpfe zeigt – *dass* die Art-Zeilen sie unverändert benutzen, steht im Diff
 * dieser Story (nur die Fach-Zeile wurde angefasst) und ist nicht Sache einer Zusicherung.
 */
describe("NameRow – kennt kein Eigentum", () => {
  it("zeigt Löschen unabhängig von jedem Recht", () => {
    render(<NameRow fieldId="ca-category-9" label="Art" srName={'Art „Grammatik"'} value="Grammatik"
      busy={false} onSave={() => {}} onDelete={() => {}} />);

    expect(screen.getByLabelText("Art")).toBeTruthy();
    expect(screen.getByRole("button", { name: 'Art „Grammatik" löschen' })).toBeTruthy();
  });
});
