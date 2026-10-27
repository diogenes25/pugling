import { afterEach, describe, expect, it } from "vitest";
import { act, render, renderHook, screen } from "@testing-library/react";
import { setLanguage } from "../lib/i18n";
import { ApiError, errorMessage } from "../lib/api";
import { ArcadeScope, useErrorMessage } from "../lib/chromeText";
import { useAction } from "../lib/useAction";
import { DEFAULT_SKIN } from "../lib/skins";
import { de } from "../i18n/de";
import { AudioButton } from "../components/AudioButton";
import { BirkenbihlDecoding } from "../components/BirkenbihlDecoding";
import { ClozePrompt } from "../components/ClozePrompt";
import { LetterBoxes } from "../components/LetterBoxes";
import { Passage } from "../components/Passage";
import { LetterTiles } from "../components/LetterTiles";
import { RevealAlternatives } from "../components/RevealAlternatives";
import { ListRule } from "../components/ListRule";
import { SelfAssessAnswer, TestResult } from "./SohnTest";
import type { TestSubmitResponse } from "../lib/types";

/*
 * B-305 – der Spielweg der Arcade auf Englisch. Je Fall steht dabei, welche Änderung ihn rot macht.
 */

// Ein gefallener Fall darf die Sprache nicht auf Englisch stehen lassen – die anderen Dateien erwarten Deutsch.
afterEach(async () => { await setLanguage("de"); });

/**
 * Alle acht geteilten Bausteine, wie Arcade und Vater-Vorschau sie rendern. Die Alternative ist mit Absicht ein
 * vorhandener Schlüssel: liefe sie durch `t`, würde aus „practice.task“ sichtbar „Task“ (AK 2 auf dem Spielweg).
 */
function Bausteine() {
  return (
    <>
      <ClozePrompt text="Ich {{0}} müde, {{1}}." gapIndex={0} />
      <LetterTiles tiles={["a", "b"]} value="" onChange={() => {}} />
      <LetterBoxes length={2} value="" onChange={() => {}} />
      <RevealAlternatives alternatives={["practice.task"]} />
      <ListRule type="List" anyOrder={false} itemIndex={2} />
      <ListRule type="Vocabulary" anyOrder itemIndex={0} />
      <Passage text="Ein Text." />
      <BirkenbihlDecoding decoding={[{ wordId: 1, learningWord: "dog", gloss: "Hund" }]} />
      <AudioButton url="data:audio/wav;base64," autoPlay={false} />
    </>
  );
}

// Je Baustein ein Text, englisch in der Arcade, deutsch außerhalb. Rot je Zeile, wenn der Baustein sein Literal
// zurückbekommt oder `useTranslation` statt `useChromeT` ruft (dann folgte er außerhalb der Arcade dem Englisch).
const TEXTE = [
  ["label", "the gap you're asked for", "gesuchte Lücke"],
  ["label", "gap 1", "Lücke 1"],
  ["button", "Start over", "Neu anfangen"],
  ["label", "Letter boxes", "Buchstaben-Kästchen"],
  ["text", "also correct: practice.task", "auch richtig: practice.task"],
  ["text", "Entry 3", "Eintrag 3"],
  ["text", "Name an entry you haven't named yet.", "Nenne einen Eintrag, den du noch nicht genannt hast."],
  ["label", "Text for the task", "Text zur Aufgabe"],
  ["label", "Word-by-word decoding", "Wort-für-Wort-Dekodierung"],
  ["button", "🔊 Listen", "🔊 Anhören"],
] as const;

function expectText(kind: "label" | "button" | "text", text: string) {
  if (kind === "label") expect(screen.getByLabelText(text)).toBeTruthy();
  else if (kind === "button") expect(screen.getByRole("button", { name: text })).toBeTruthy();
  else expect(screen.getByText(text)).toBeTruthy();
}

describe("Geteilte Spiel-Bausteine", () => {
  it("sprechen in der Arcade deren Sprache", async () => {
    // Rot, wenn `useChromeT` auch im Scope fest Deutsch liefert (dann bliebe „Neu anfangen“ stehen).
    await setLanguage("en");
    render(<ArcadeScope><Bausteine /></ArcadeScope>);
    for (const [kind, en] of TEXTE) expectText(kind, en);
  });

  it("bleiben außerhalb der Arcade deutsch, auch wenn Englisch gespeichert ist (Vater-Vorschau, AK 3)", async () => {
    // Rot, wenn `useChromeT` den Scope nicht beachtet und der globalen Sprache folgt.
    await setLanguage("en");
    render(<Bausteine />);
    for (const [kind, , deText] of TEXTE) expectText(kind, deText);
  });
});

describe("Klausur auf Englisch", () => {
  const result: TestSubmitResponse = {
    attemptId: 1, stage: 2, totalItems: 1, correctItems: 1, scorePercent: 100,
    passed: true, passPercent: 80, attemptsRemaining: 1,
    // Aufgabe und Lösung sind mit Absicht vorhandene Schlüssel: schickte jemand sie durch `t`, würde sichtbar
    // „Daily test“ bzw. „Task“ daraus. Ein gewöhnliches Wort bliebe gleich – `t` liefert bei fehlendem Schlüssel
    // den Schlüssel.
    items: [{ itemIndex: 0, prompt: "test.title", expected: "practice.task", givenAnswer: null, wasCorrect: true, gapIndex: null }],
    wrongMentions: [],
  };

  it("übersetzt den Auswertungsschirm samt Stufenname, nicht aber Aufgabe und Lösung", async () => {
    await setLanguage("en");
    render(<TestResult result={result} skin={DEFAULT_SKIN} onHome={() => {}} onRetry={() => {}} />);
    // Rot, wenn Sieg-Titel, Bestehensgrenze oder der Stufenname am deutschen Literal hängen.
    expect(screen.getByText("VICTORY!")).toBeTruthy();
    expect(screen.getByText("Pass mark 80%")).toBeTruthy();
    expect(screen.getByText("TEST · SELF-CHECK")).toBeTruthy();
    expect(screen.getByText("Correct")).toBeTruthy();
    // Nutzerdaten (AK 2): rot, wenn Aufgabe oder Lösung durch `t` laufen.
    expect(screen.getByText("test.title")).toBeTruthy();
    expect(screen.getByText("practice.task")).toBeTruthy();
  });

  it("nennt eine unbekannte Stufe mit Nummer statt mit einem Schlüsselnamen", async () => {
    // Rot, wenn die Stufe ungeprüft in den Schlüssel wandert („test.stage9“ im Bild).
    await setLanguage("en");
    render(<TestResult result={{ ...result, stage: 9 }} skin={DEFAULT_SKIN} onHome={() => {}} onRetry={() => {}} />);
    expect(screen.getByText("TEST · STAGE 9")).toBeTruthy();
  });

  it("deckt bei der Selbsteinschätzung auf Englisch auf", async () => {
    await setLanguage("en");
    render(<SelfAssessAnswer reveal={null} revealed busy={false} onReveal={() => {}} onJudge={() => {}} />);
    // Rot, wenn Rückfalltext oder Urteilsknöpfe deutsch bleiben.
    expect(screen.getByText("→ (revealed)")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Knew it" })).toBeTruthy();
  });
});

describe("Fehlermeldungen der Arcade", () => {
  const exhausted = new ApiError(429, "No attempts left today.", "trace-1", "test_attempts_exhausted");
  const unknown = new ApiError(400, "Something specific went wrong.", "trace-2", "some_other_code");

  it("sind auf Deutsch genau `errorMessage` – auch die eigenen Schlüssel sind wortgleich", async () => {
    // Rot, wenn ein deutscher `problem.*`-Satz von `errorMessage` abweicht: die Arcade liest ihn, und dieselbe
    // Sperre hieße in Arcade und Vater-Web verschieden.
    for (const code of Object.keys(de.sohn.problem)) {
      expect(de.sohn.problem[code as keyof typeof de.sohn.problem]).toBe(errorMessage(new ApiError(400, "x", undefined, code)));
    }
    const { result } = renderHook(() => useErrorMessage());
    expect(result.current(exhausted)).toBe(errorMessage(exhausted));
    expect(result.current(unknown)).toBe(errorMessage(unknown));
    // Ein Code, den `errorMessage` deutsch kennt, die Arcade aber nicht: rot, wenn die Sprachweiche fehlt (dann
    // stünde die englische Server-Zeile da, wo ein deutsches Kind bisher den deutschen Satz las).
    const known = new ApiError(409, "Duplicate series.", "trace-3", "duplicate_textbook_series");
    expect(result.current(known)).toBe(errorMessage(known));
  });

  it("sind auf Englisch der eigene Satz oder der englische Server-Text – nie der deutsche", async () => {
    await setLanguage("en");
    const { result } = renderHook(() => useErrorMessage());
    // Rot, wenn die Sprachweiche fehlt (dann der deutsche Satz) oder der Code-Schlüssel nicht greift.
    expect(result.current(exhausted)).toBe("You've used up your tries for today. You can have another go tomorrow.");
    expect(result.current(unknown)).toBe("Something specific went wrong. (Ref: trace-2)");
  });

  it("folgen der Sprache zum Zeitpunkt des Fehlers, nicht der beim Render", async () => {
    // Rot, wenn die Funktion die Sprache beim Render festhält (ein früh gebundener Fang spräche dann weiter Deutsch).
    const { result } = renderHook(() => useErrorMessage());
    const format = result.current;
    await setLanguage("en");
    expect(format(exhausted)).toBe("You've used up your tries for today. You can have another go tomorrow.");
  });

  it("erreichen über `useAction` die Meldung der Arcade", async () => {
    // Rot, wenn `useAction` den übergebenen Formatierer übergeht und wieder fest `errorMessage` ruft.
    const { result } = renderHook(() => useAction(() => "formatiert"));
    await act(async () => { await result.current.run(() => Promise.reject(new Error("roh"))); });
    expect(result.current.message).toEqual({ ok: false, text: "formatiert" });
  });

  it("bleiben in `useAction` ohne Formatierer `errorMessage` (Vater-Web)", async () => {
    // Rot, wenn der Standard des Parameters sich ändert – das Vater-Web übergibt keinen.
    const { result } = renderHook(() => useAction());
    await act(async () => { await result.current.run(() => Promise.reject(exhausted)); });
    expect(result.current.message?.text).toBe(errorMessage(exhausted));
  });
});
