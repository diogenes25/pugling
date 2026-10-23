import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { ConfigEditor, buildTypeConfig, emptyExtra, emptyRow } from "./exerciseConfig";
import { LANGUAGES } from "../lib/languages";
import type { Row } from "./exerciseConfig";

/**
 * B-17: Lern- und Muttersprache einer Birkenbihl-Übung kommen aus derselben geschlossenen Liste wie überall
 * sonst. Die Dekodierung vergleicht die Codes exakt mit dem Vokabel-Store – ein frei getipptes „Englisch"
 * fände dort nie ein Wort und legte bei einer eigenen Glosse sogar einen neuen Store-Eintrag unter
 * „Englisch" an. Je Fall steht, welche Änderung ihn rot macht.
 */
function view(extra: Row) {
  const setExtra = vi.fn();
  render(<ConfigEditor type="Birkenbihl" rows={[emptyRow("Birkenbihl")]} extra={extra} setExtra={setExtra}
    patchRow={() => {}} addRow={() => {}} removeRow={() => {}} />);
  return { setExtra };
}

const codes = (el: HTMLElement) => [...(el as HTMLSelectElement).options].filter((o) => !o.disabled).map((o) => o.value);

describe("Birkenbihl-Sprachen", () => {
  // Rot im Ausgangszustand (zwei freie <input>, keine Auswahl) und bei jeder Liste außer `LANGUAGES`.
  it("bietet beide Sprachen als Auswahl aus der geschlossenen Liste an", () => {
    view(emptyExtra("Birkenbihl"));
    const learn = screen.getByRole("combobox", { name: "Lernsprache" });
    const native = screen.getByRole("combobox", { name: "Muttersprache" });
    expect(codes(learn)).toEqual(LANGUAGES.map((l) => l.code));
    expect(codes(native)).toEqual(LANGUAGES.map((l) => l.code));
    expect((learn as HTMLSelectElement).value).toBe("en");
    expect((native as HTMLSelectElement).value).toBe("de");
  });

  // Rot, wenn die Auswahl in das falsche Feld schreibt (z. B. beide auf `learningLang`).
  it("schreibt die gewählte Sprache in das passende Feld", () => {
    const { setExtra } = view(emptyExtra("Birkenbihl"));
    fireEvent.change(screen.getByRole("combobox", { name: "Muttersprache" }), { target: { value: "fr" } });
    const updater = setExtra.mock.calls[0][0] as (e: Row) => Row;
    expect(updater({ learningLang: "en", nativeLang: "de" })).toEqual({ learningLang: "en", nativeLang: "fr" });
  });

  // Rot ohne den Platzhalter für unbekannte Codes: dann zeigte die Auswahl „Deutsch", gespeichert bliebe „Englisch".
  it("zeigt einen unbekannten Alt-Code als „bitte wählen“, nicht als erste Sprache", () => {
    view({ learningLang: "Englisch", nativeLang: "de" });
    const learn = screen.getByRole("combobox", { name: "Lernsprache" }) as HTMLSelectElement;
    expect(learn.value).toBe("");
    expect(learn.selectedOptions[0].textContent).toBe("– bitte wählen –");
  });

  // Rot ohne die Vorbelegung in `emptyExtra`: eine neue Übung, an der niemand die Auswahl anfasst, speicherte "".
  it("speichert eine unberührte neue Übung mit zwei gültigen Codes", () => {
    const config = buildTypeConfig("Birkenbihl", [], emptyExtra("Birkenbihl")) as { learningLang: string; nativeLang: string };
    expect([config.learningLang, config.nativeLang]).toEqual(["en", "de"]);
  });
});
