import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import "../lib/i18n";
import type { PracticeCard } from "../lib/types";
import { abgeschrieben, Lernkarte, nurAkzenteFalsch } from "./Lernkarte";
import { aufgabenform } from "./aufgabenform";

/*
 * B-598 – die Lernkarte in vier Schritten. Je Fall steht dabei, welche Änderung ihn rot macht.
 */
const KARTE = {
  itemIndex: 0, stage: 8, type: "Vocabulary", prompt: "gehen", reveal: "walk", learnCard: true, introduction: true,
  revealSpeechLang: "en", revealAudioUrl: null,
} as unknown as PracticeCard;

class Aeusserung { lang = ""; voice: unknown = null; constructor(public text: string) {} }
function stimme() {
  vi.stubGlobal("SpeechSynthesisUtterance", Aeusserung);
  vi.stubGlobal("speechSynthesis", {
    getVoices: () => [{ lang: "en-GB", name: "Daniel" }], speak: vi.fn(), cancel: vi.fn(),
    addEventListener: vi.fn(), removeEventListener: vi.fn(),
  });
}
afterEach(() => vi.unstubAllGlobals());

describe("Lernkarte", () => {
  // AK 2–4 im Ablauf. Rot, wenn ein Schritt fehlt, das Wort in Schritt 4 noch zu sehen oder zu hören ist, oder wenn
  // die Karte mehr als einen Aufruf mit beiden Ergebnissen schickte.
  it("führt vom Aufdecken übers Abschreiben zum Erinnern und schickt beide Ergebnisse auf einmal", () => {
    stimme();
    const fertig = vi.fn();
    render(<Lernkarte karte={KARTE} gesperrt={false} onFertig={fertig} />);
    expect(screen.queryByText(/walk/)).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: "Aufdecken" }));
    expect(screen.getByText("→ walk")).toBeTruthy();
    expect(screen.getByRole("button", { name: "🔊 Anhören" })).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Nein, neu" }));

    // Schritt 3: das Wort bleibt sichtbar und hörbar, „OK“ erst mit der richtigen Abschrift.
    expect(screen.getByText("→ walk")).toBeTruthy();
    const kopie = screen.getByLabelText("Schreib es ab");
    const ok = () => screen.getByRole("button", { name: "OK" }) as HTMLButtonElement;
    fireEvent.change(kopie, { target: { value: "wakl" } });
    expect(ok().disabled).toBe(true);
    fireEvent.change(kopie, { target: { value: "Walk" } });
    expect(ok().disabled).toBe(false);
    fireEvent.click(ok());

    // Schritt 4: das Wort ist weg, kein Vorlesen.
    expect(screen.queryByText("→ walk")).toBeNull();
    expect(screen.queryByRole("button", { name: "🔊 Anhören" })).toBeNull();
    fireEvent.change(screen.getByLabelText("Und jetzt aus dem Kopf"), { target: { value: "walk" } });
    fireEvent.click(ok());
    expect(fertig).toHaveBeenCalledTimes(1);
    expect(fertig).toHaveBeenCalledWith({ wasKnown: false, givenAnswer: "walk" });
  });

  // Entscheidung 3 + 8. Rot, wenn „Ja“ das Abschreiben überspränge (Entscheidung 4) oder Aufgeben eine leere Zeichenkette
  // statt „keine Antwort“ schickte – die Historie unterscheidet „aufgegeben“ an genau diesem `null`.
  it("schreibt auch nach „Ja“ ab, und Aufgeben schickt keine Antwort", () => {
    const fertig = vi.fn();
    render(<Lernkarte karte={KARTE} gesperrt={false} onFertig={fertig} />);
    fireEvent.click(screen.getByRole("button", { name: "Aufdecken" }));
    fireEvent.click(screen.getByRole("button", { name: "Ja, kannte ich" }));
    expect(screen.getByLabelText("Schreib es ab")).toBeTruthy();
    fireEvent.change(screen.getByLabelText("Schreib es ab"), { target: { value: "walk" } });
    fireEvent.click(screen.getByRole("button", { name: "OK" }));

    fireEvent.click(screen.getByRole("button", { name: "Später nochmal" }));
    expect(fertig).toHaveBeenCalledWith({ wasKnown: true, givenAnswer: null });
  });

  // Entscheidung 5. Rot, wenn die Abschrift strenger prüfte als der Server (Großbuchstabe, typografischer Apostroph,
  // Schlusspunkt) oder lascher (ein anderes Wort).
  it("prüft die Abschrift nach den Regeln des Servers", () => {
    expect(abgeschrieben("Walk", "walk")).toBe(true);
    expect(abgeschrieben("don’t", "don't")).toBe(true);
    expect(abgeschrieben("la gare.", "la gare")).toBe(true);
    expect(abgeschrieben("talk", "walk")).toBe(false);
    expect(abgeschrieben("  ", "walk")).toBe(false);
    // Klammerteile einzeln weglassbar wie am Server (B-462). Rot, wenn die Klammern wieder exakt verlangt werden.
    expect(abgeschrieben("folk", "folk (pl) (infml)")).toBe(true);
    expect(abgeschrieben("folk pl", "folk (pl) (infml)")).toBe(true);
    expect(abgeschrieben("folk (pl) (infml)", "folk (pl) (infml)")).toBe(true);
    expect(abgeschrieben("reply to", "(to) reply (to)")).toBe(true);
    // Der Klammertext allein und ein Tippfehler im Kürzel bleiben falsch. Rot, wenn Klammern beliebig abgeschnitten werden.
    expect(abgeschrieben("pl", "folk (pl) (infml)")).toBe(false);
    expect(abgeschrieben("folk (pl) (inform)", "folk (pl) (infml)")).toBe(false);
  });

  // Rot, wenn die Lernkarte als Selbsteinschätzung erschiene (sie bringt `reveal` mit) – dann gäbe es „Gewusst?“
  // statt der vier Schritte, und die Anweisung sagte „Weißt du es?“.
  it("hat ihre eigene Form und Anweisung", () => {
    const form = aufgabenform(KARTE, false);
    expect([form.antwort, form.anweisung]).toEqual(["lernkarte", "lektion.learn"]);
  });
});

describe("Lernkarte – leichter schalten (B-643)", () => {
  // Bis Schritt 4 durchspielen, dann steht das Erinnern-Feld.
  function bisZumErinnern() {
    fireEvent.click(screen.getByRole("button", { name: "Aufdecken" }));
    fireEvent.click(screen.getByRole("button", { name: "Nein, neu" }));
    fireEvent.change(screen.getByLabelText("Schreib es ab"), { target: { value: "walk" } });
    fireEvent.click(screen.getByRole("button", { name: "OK" }));
  }

  // AK 1: der Knopf steht nur, wo der Server ihn anbietet, und erst in Schritt 4. Rot, wenn das Frontend ihn selbst
  // zeigte (ohne `canEase`) oder schon beim Aufdecken – dort hieße leichter, das Wort zu verstecken, das es gerade lernt.
  it("zeigt „Mach's mir leichter“ nur in Schritt 4 und nur mit canEase", () => {
    const leichter = vi.fn();
    const { rerender } = render(<Lernkarte karte={{ ...KARTE, canEase: true }} gesperrt={false} onFertig={vi.fn()} onLeichter={leichter} />);
    const knopf = () => screen.queryByRole("button", { name: "Mach's mir leichter" });
    expect(knopf()).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Aufdecken" }));
    expect(knopf()).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Nein, neu" }));
    expect(knopf()).toBeNull();
    fireEvent.change(screen.getByLabelText("Schreib es ab"), { target: { value: "walk" } });
    fireEvent.click(screen.getByRole("button", { name: "OK" }));
    expect(knopf()).toBeTruthy();

    rerender(<Lernkarte karte={KARTE} gesperrt={false} onFertig={vi.fn()} onLeichter={leichter} />);
    expect(knopf()).toBeNull();
    rerender(<Lernkarte karte={{ ...KARTE, canEase: true }} gesperrt={false} onFertig={vi.fn()} onLeichter={leichter} />);
    fireEvent.click(screen.getByRole("button", { name: "Mach's mir leichter" }));
    expect(leichter).toHaveBeenCalledTimes(1);
  });

  // AK 1 + 2: die leichtere Form kommt vom Server (`eased`, hier Kästchen) und ersetzt das freie Feld; die Karte bleibt in
  // Schritt 4, die Antwort geht wie jede andere an den Server. Rot, wenn die Karte nach dem Schalten von vorn begänne oder
  // das freie Feld stehen bliebe.
  it("spielt die leichtere Form des Servers in Schritt 4", () => {
    const fertig = vi.fn();
    const { rerender } = render(<Lernkarte karte={{ ...KARTE, canEase: true }} gesperrt={false} onFertig={fertig} onLeichter={vi.fn()} />);
    bisZumErinnern();
    const kaesten = { ...KARTE, stage: 3, reveal: null, eased: true, canEase: true, answerLength: 4, answerPattern: "____" } as PracticeCard;
    rerender(<Lernkarte karte={kaesten} gesperrt={false} onFertig={fertig} onLeichter={vi.fn()} />);

    expect(screen.queryByRole("button", { name: "Aufdecken" })).toBeNull();
    expect(document.querySelectorAll(".letterboxes input")).toHaveLength(4);
    expect(document.getElementById("lernkarte-erinnern")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Später nochmal" }));
    expect(fertig).toHaveBeenCalledWith({ wasKnown: false, givenAnswer: null });
  });
});

describe("Lernkarte – Sonderzeichen (B-661)", () => {
  const FR = { ...KARTE, reveal: "dîner", revealSpeechLang: "fr", keyboardLang: "fr" } as unknown as PracticeCard;
  const zuSchritt3 = (karte: PracticeCard) => {
    render(<Lernkarte karte={karte} gesperrt={false} onFertig={vi.fn()} />);
    fireEvent.click(screen.getByRole("button", { name: "Aufdecken" }));
    fireEvent.click(screen.getByRole("button", { name: "Nein, neu" }));
    return screen.getByLabelText("Schreib es ab") as HTMLInputElement;
  };
  const ok = () => screen.getByRole("button", { name: "OK" }) as HTMLButtonElement;

  // Entscheidung 2. Rot, wenn „i“ statt „î“ doch „OK“ freigäbe (Toleranz) oder der Hinweis fehlte bzw. auch bei einem
  // falschen Buchstaben käme.
  it("lässt „diner“ nicht durch, sagt aber „Fast – achte auf die Akzente“", () => {
    const kopie = zuSchritt3(FR);
    fireEvent.change(kopie, { target: { value: "diner" } });
    expect(ok().disabled).toBe(true);
    expect(screen.getByRole("status").textContent).toBe("Fast – achte auf die Akzente");

    fireEvent.change(kopie, { target: { value: "dinar" } });
    expect(screen.getByRole("status").textContent).toBe("");
  });

  // Entscheidungen 1 und 3, der eigentliche Blocker. Rot, wenn die Leiste in Schritt 3 fehlt oder ihr Zeichen nicht an
  // der Schreibmarke im Feld landet.
  it("schreibt „dîner“ über die Leiste ab, ohne dass die Tastatur ein „î“ haben muss", () => {
    const kopie = zuSchritt3(FR);
    fireEvent.change(kopie, { target: { value: "dner" } });
    kopie.setSelectionRange(1, 1);
    fireEvent.click(screen.getByRole("button", { name: "î einsetzen" }));
    expect(kopie.value).toBe("dîner");
    expect(ok().disabled).toBe(false);
  });

  // Entscheidung 3. Rot, wenn die Wort-Ergänzung in Schritt 4 durchsickert – dann zeigte die Leiste dort, welche
  // Sonderzeichen die Lösung trägt – oder wenn sie in Schritt 3 fehlt.
  it("bietet die Zeichen des Wortes nur beim Abschreiben an, nicht aus dem Kopf", () => {
    const kopie = zuSchritt3({ ...FR, reveal: "le cañon" } as unknown as PracticeCard);
    expect(screen.getByRole("button", { name: "ñ einsetzen" })).toBeTruthy();
    fireEvent.change(kopie, { target: { value: "le cañon" } });
    fireEvent.click(ok());

    expect(screen.getByLabelText("Und jetzt aus dem Kopf")).toBeTruthy();
    expect(screen.queryByRole("button", { name: "ñ einsetzen" })).toBeNull();
    expect(screen.getByRole("button", { name: "î einsetzen" })).toBeTruthy();
  });

  it("erkennt „nur Akzente falsch“ auch mit Klammerteilen und nicht bei richtiger Abschrift", () => {
    expect(nurAkzenteFalsch("connaitre", "connaître (qn)")).toBe(true);
    expect(nurAkzenteFalsch("connaître", "connaître (qn)")).toBe(false);
    expect(nurAkzenteFalsch("conaitre", "connaître (qn)")).toBe(false);
  });
});
