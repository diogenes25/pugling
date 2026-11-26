import { describe, expect, it } from "vitest";
import { KEINE_LOESUNG, mengeAntwort } from "./mengeAntwort";

// B-712: die Bindung der Felder an den Antworttext. Je Fall die Änderung, die ihn rot macht.
describe("mengeAntwort", () => {
  // AK 2: rot ohne Sortierung („4;2“).
  it("sortiert aufsteigend", () => {
    expect(mengeAntwort(["4", "2"], false)).toBe("2;4");
    expect(mengeAntwort(["-1", "-3"], false)).toBe("-3;-1");
  });

  // AK 3: rot, wenn die Bindung das Komma in einen Punkt umschreibt oder wie Text sortiert („3.5“ vor „2,5“ nicht).
  it("lässt Komma und Punkt unverändert und sortiert nach Zahlwert", () => {
    expect(mengeAntwort(["3.5", "2,5"], false)).toBe("2,5;3.5");
    expect(mengeAntwort(["10", "9"], false)).toBe("9;10");
  });

  // AK 5: rot, wenn leere Felder als leere Elemente („;4“) mitgehen oder alles leer etwas sendet.
  it("lässt leere Felder weg; alles leer bleibt leer", () => {
    expect(mengeAntwort(["", "4", " "], false)).toBe("4");
    expect(mengeAntwort(["", ""], false)).toBe("");
  });

  // AK 4: rot, wenn der Umschalter die Felder nicht überstimmt oder „0“ sendet.
  it("keine Lösung schlägt die Felder", () => {
    expect(mengeAntwort(["2", "4"], true)).toBe(KEINE_LOESUNG);
    expect(KEINE_LOESUNG).toBe("keine Lösung");
  });

  // Rot, wenn eine unlesbare Eingabe verschwindet – der Server muss sie sehen und als falsch werten.
  it("behält Unlesbares hinten", () => {
    expect(mengeAntwort(["x", "2"], false)).toBe("2;x");
  });
});
