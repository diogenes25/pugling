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
  // Ein Code, den `errorMessage` deutsch kennt, die Arcade aber nicht: nur an ihm unterscheiden sich die beiden Sprachen
  // außerhalb von `problem.*` (für `unknown` liefert `errorMessage` byte-gleich die englische Server-Zeile).
  const known = new ApiError(409, "Duplicate series.", "trace-3", "duplicate_textbook_series");

  it("sind auf Deutsch genau `errorMessage` – auch die eigenen Schlüssel sind wortgleich", async () => {
    // Rot, wenn ein deutscher `problem.*`-Satz von `errorMessage` abweicht: die Arcade liest ihn, und dieselbe
    // Sperre hieße in Arcade und Vater-Web verschieden.
    for (const code of Object.keys(de.sohn.problem)) {
      expect(de.sohn.problem[code as keyof typeof de.sohn.problem]).toBe(errorMessage(new ApiError(400, "x", undefined, code)));
    }
    const { result } = renderHook(() => useErrorMessage());
    expect(result.current(exhausted)).toBe(errorMessage(exhausted));
    expect(result.current(unknown)).toBe(errorMessage(unknown));
    // Rot, wenn die Sprachweiche immer Englisch wählt (dann stünde die englische Server-Zeile da, wo ein deutsches Kind
    // bisher den deutschen Satz las).
    expect(result.current(known)).toBe(errorMessage(known));
  });

  it("sind auf Englisch der eigene Satz oder der englische Server-Text – nie der deutsche", async () => {
    await setLanguage("en");
    const { result } = renderHook(() => useErrorMessage());
    // Rot, wenn der Code-Schlüssel nicht greift (erste Zeile) oder die Sprachweiche immer `errorMessage` wählt (letzte
    // Zeile: dann der deutsche Satz – `unknown` allein sähe das nicht, B-319).
    expect(result.current(exhausted)).toBe("You've used up your tries for today. You can have another go tomorrow.");
    expect(result.current(unknown)).toBe("Something specific went wrong. (Ref: trace-2)");
    expect(result.current(known)).toBe("Duplicate series. (Ref: trace-3)");
  });

  // B-330: den Code bekommt nur das Kind (Plan pausiert, abgelaufen oder nicht der aktive) – auch nach einem Reload auf
  // Üben/Klausur. Die deutsche Zeile fällt nur, wenn beide Einträge fehlen (`GERMAN_PROBLEM_TEXT` und Arcade-Schlüssel –
  // dann die englische Server-Zeile samt Ref); fehlt nur einer, hält die Wortgleichheits-Schleife oben. Die englische
  // Zeile trägt den Arcade-Schlüssel: ohne ihn stünde der technische `detail`-Text da.
  it("sagen dem Kind in beiden Sprachen, dass sein Plan gerade nicht dran ist", async () => {
    const inactive = new ApiError(403, "This study plan is not currently active. Ask your parent.", "trace-4", "plan_inactive");
    const { result } = renderHook(() => useErrorMessage());
    expect(result.current(inactive)).toBe("Dieser Lernplan ist gerade nicht dran. Frag deine Eltern, welcher Plan jetzt gilt.");
    await setLanguage("en");
    expect(result.current(inactive)).toBe("This study plan isn't active right now. Ask your parents which plan to do now.");
  });

  // B-415: die Sperren, die nur das Kind auslöst (Skin, Shop, Bild, Wunsch-Interessen, Überschneidung beim Speichern).
  // Rot, wenn ein Code weder in `GERMAN_PROBLEM_TEXT` noch unter `problem.*` steht: dann stünde auf Deutsch die
  // englische Server-Zeile samt Ref da, auf Englisch die technische Server-Zeile.
  it.each(["skin_already_unlocked", "shop_insufficient_stock", "media_no_alternative", "media_not_on_card",
    "interest_wish_limit_reached", "concurrency_conflict",
    // B-499: vier 400er aus Kauf und Einlösen (zu wenig Gems/Münzen, ein abgelaufenes Angebot, zu wenig Bestand) – das
    // 409-Tor aus B-411 sieht sie nicht.
    "insufficient_gems", "insufficient_coins", "shop_listing_inactive", "insufficient_inventory"])(
    "sagen dem Kind %s in beiden Sprachen mit einem eigenen Satz", async (code) => {
      const blocked = new ApiError(409, "Technical server line.", "trace-5", code);
      const { result } = renderHook(() => useErrorMessage());
      expect(result.current(blocked)).not.toContain("Technical server line");
      expect(result.current(blocked)).not.toContain("trace-5");
      await setLanguage("en");
      expect(result.current(blocked)).not.toContain("Technical server line");
      expect(result.current(blocked)).not.toContain("trace-5");
    });

  // B-499 (Review): `insufficient_inventory` trifft auch den Vater (er genehmigt zwei Anfragen, die zusammen den Bestand
  // übersteigen) und heißt „nicht genug", nicht „nichts". Rot, wenn der Satz das Kind anspricht oder „nichts mehr" sagt.
  it("sagen „zu wenig im Inventar“ so, dass es auch für den Vater stimmt", () => {
    const text = errorMessage(new ApiError(400, "Not enough units.", "trace-6", "insufficient_inventory"));
    expect(text).not.toMatch(/\b(du|dein|deine|dir)\b/i);
    expect(text).not.toMatch(/nichts/);
  });

  it("folgen der Sprache zum Zeitpunkt des Fehlers, nicht der beim Render", async () => {
    // Rot, wenn die Funktion die Sprache beim Render festhält (ein früh gebundener Fang spräche dann weiter Deutsch) –
    // für den Schlüssel (`exhausted`) wie für die Weiche davor (`known`, B-319).
    const { result } = renderHook(() => useErrorMessage());
    const format = result.current;
    await setLanguage("en");
    expect(format(exhausted)).toBe("You've used up your tries for today. You can have another go tomorrow.");
    expect(format(known)).toBe("Duplicate series. (Ref: trace-3)");
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
