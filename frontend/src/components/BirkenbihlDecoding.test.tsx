import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { BirkenbihlDecoding } from "./BirkenbihlDecoding";
import type { WordPair } from "../lib/types";

const paar = (wordId: number, learningWord: string, gloss: string | null): WordPair =>
  ({ wordId, learningWord, gloss, vocabularyId: null, _self: null });

/** Die Spalten als Paare (Lernwort, Bedeutung) – so, wie sie sichtbar untereinander stehen. */
const spalten = (container: HTMLElement) =>
  [...container.querySelectorAll(".decoding-pair")].map((p) => [
    p.querySelector(".decoding-word")?.textContent,
    p.querySelector(".decoding-gloss")?.textContent,
  ]);

/**
 * B-78: Der Vater pflegte die Dekodierung Wort für Wort, beim Kind kam nur „Satz → Übersetzung" an. Geprüft
 * wird, dass jedes Paar in Satzreihenfolge ankommt – und dass ein Wort ohne Eintrag im Vokabelspeicher keine
 * Bedeutung erfindet.
 */
describe("BirkenbihlDecoding", () => {
  it("zeigt jedes Wort mit seiner wörtlichen Bedeutung in Satzreihenfolge", () => {
    const { container } = render(
      <BirkenbihlDecoding decoding={[paar(1, "How", "Wie"), paar(2, "are", "bist"), paar(3, "you", "du")]} />);

    expect(screen.getByRole("group", { name: "Wort-für-Wort-Dekodierung" })).toBeTruthy();
    expect(spalten(container)).toEqual([["How", "Wie"], ["are", "bist"], ["you", "du"]]);
  });

  /**
   * Die Zuordnung Wort→Bedeutung ist visuell eine Spalte; wer zuhört, braucht sie als Satz. Ohne die
   * vorgelesenen Trenner wäre „How Wie are bist" ein Durchlauf, in dem nichts Wort von Bedeutung trennt.
   */
  it("liest sich als Paare vor, nicht als Wortkette", () => {
    render(<BirkenbihlDecoding decoding={[paar(1, "How", "Wie"), paar(2, "are", "bist")]} />);

    expect(screen.getByRole("group").textContent).toBe("How – Wie, are – bist, ");
  });

  /**
   * Das Wort steht nicht im Speicher: die Spalte bleibt (die Ausrichtung zum Satz hängt daran), aber sie
   * behauptet keine Bedeutung – und verspricht auch dem Ohr keine („How –" ohne Fortsetzung).
   */
  it("lässt ein Wort ohne Eintrag im Speicher ohne Bedeutung", () => {
    const { container } = render(
      <BirkenbihlDecoding decoding={[paar(1, "How", "Wie"), paar(2, "y'all", null)]} />);

    expect(spalten(container)).toEqual([["How", "Wie"], ["y'all", ""]]);
    expect(screen.getByRole("group").textContent).toBe("How – Wie, y'all, ");
  });

  /**
   * Dasselbe Wort darf im Satz zweimal stehen – der Schlüssel ist darum die übungsweit eindeutige `wordId`,
   * nicht das Wort. Mit dem Wort als Schlüssel verschluckte React die zweite Spalte.
   */
  it("zeigt ein doppeltes Wort zweimal", () => {
    const { container } = render(
      <BirkenbihlDecoding decoding={[paar(1, "very", "sehr"), paar(2, "very", "sehr")]} />);

    expect(spalten(container)).toEqual([["very", "sehr"], ["very", "sehr"]]);
  });

  it("schweigt ohne Dekodierung – die anderen Übungstypen tragen keine", () => {
    for (const leer of [undefined, null, []] as const) {
      const { container } = render(<BirkenbihlDecoding decoding={leer} />);
      expect(container.textContent).toBe("");
    }
  });
});
