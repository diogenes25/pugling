import { act, fireEvent, render, screen, within } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { de } from "../i18n/de";
import { ApiError } from "../lib/api";
import { setLanguage } from "../lib/i18n";
import type { PracticeCard, ReviewOutcome } from "../lib/types";

/**
 * B-284: eine Bewertung, die die Sperre verwirft, schaltet die Karte nicht weiter. Vorher lief `next()` nach `action.run`
 * immer – auch für den verworfenen zweiten Aufruf –, und ein doppeltes Enter übersprang eine Karte.
 */
const review = vi.fn();
const startSession = vi.fn();
const cards = vi.fn();
const endSession = vi.fn();
const heartbeat = vi.fn();
const cardShown = vi.fn();
const overview = vi.fn();

vi.mock("./SohnApp", () => ({
  // `skin`: die Aufgabe steht seit B-526 in der Sprechblase am Maskottchen.
  useSohn: () => ({ planId: 3, refreshWallet: () => {}, setStreak: () => {}, celebrate: () => {}, skin: { id: "pug", name: "Pug", emoji: "🐶", gradient: "", cost: 0, blurb: "" } }),
}));
vi.mock("../lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("../lib/api")>();
  return {
    ...original,
    api: {
      startSession: (...a: unknown[]) => startSession(...a),
      cards: (...a: unknown[]) => cards(...a),
      review: (...a: unknown[]) => review(...a),
      heartbeat: (...a: unknown[]) => heartbeat(...a),
      endSession: (...a: unknown[]) => endSession(...a),
      cardShown: (...a: unknown[]) => cardShown(...a),
      overview: (...a: unknown[]) => overview(...a),
    },
  };
});

const { SohnPractice, NACH_BLATT_MS } = await import("./SohnPractice");

/** Eine getippte Karte (`reveal: null`) ohne Auswahl und ohne Kästchen – also mit dem freien Textfeld. */
function card(itemIndex: number, prompt: string): PracticeCard {
  return { itemIndex, prompt, reveal: null } as PracticeCard;
}

beforeEach(() => {
  review.mockReset();
  startSession.mockReset().mockResolvedValue({ id: 11, testable: true });
  endSession.mockReset().mockResolvedValue(undefined);
  heartbeat.mockReset().mockResolvedValue(undefined);
  cardShown.mockReset().mockResolvedValue(undefined);
  overview.mockReset().mockResolvedValue({ currentStreak: 0, today: { positions: [] } });
  cards.mockReset().mockImplementation(() => Promise.resolve(["eins", "zwei", "drei"].map((prompt, i) => card(i, prompt))));
});

describe("SohnPractice, doppeltes Absenden", () => {
  // Rot, wenn `judge` nach einem verworfenen `action.run` wieder `next()` ruft: dann rückt der zweite Aufruf sofort eine
  // Karte weiter, der erste nach seiner Antwort noch eine – das Kind landet bei „drei", „zwei" kam nie.
  it("rückt bei zweimal Enter während der Prüfung genau eine Karte weiter", async () => {
    let answer!: (o: ReviewOutcome | null) => void;
    review.mockReturnValueOnce(new Promise((r) => { answer = r; }));
    render(
      <MemoryRouter initialEntries={["/sohn/practice/5"]}>
        <Routes><Route path="/sohn/practice/:positionId" element={<SohnPractice />} /></Routes>
      </MemoryRouter>,
    );

    const input = await screen.findByRole("textbox");
    expect(screen.getByText("eins")).toBeTruthy();
    fireEvent.change(input, { target: { value: "one" } });
    const form = input.closest("form")!;
    fireEvent.submit(form);
    fireEvent.submit(form);
    // Rot, wenn statt des laufenden der verworfene Aufruf weiterschaltet (`if (!ran)`): das Endbild wäre dasselbe. Das
    // leere `act` lässt den verworfenen Aufruf erst zu Ende laufen – ohne es prüfte die Zeile vor ihm und bliebe grün.
    await act(async () => {});
    expect(screen.getByText("eins")).toBeTruthy();
    await act(async () => { answer(null); });

    expect(await screen.findByText("zwei")).toBeTruthy();
    expect(screen.queryByText("drei")).toBeNull();
    expect(review).toHaveBeenCalledTimes(1);
  });

  // B-532, AK 1 (kehrt B-330/5 „rückt still weiter" um): eine gescheiterte Bewertung hält die Karte mit einer Meldung an.
  // Rot, wenn `next()` wieder nach jedem Fehlschlag läuft (dann steht „zwei" da), wenn die Meldung oder einer der beiden
  // Knöpfe fehlt, wenn das Feld die Antwort verliert, oder wenn der Fokus nicht auf „Noch einmal senden" geht (B-531).
  it("hält nach einer gescheiterten Bewertung mit Meldung an", async () => {
    await setLanguage("de");
    review.mockRejectedValueOnce(new Error("offline"));
    renderPractice();
    await answerTyped("one");

    expect(screen.getByRole("alert")).toBeTruthy();
    const nochmal = screen.getByRole("button", { name: de.sohn.practice.resend });
    expect(screen.getByRole("button", { name: de.sohn.practice.skip })).toBeTruthy();
    expect((screen.getByRole("textbox") as HTMLInputElement).value).toBe("one");
    expect(screen.getByText("eins")).toBeTruthy();
    expect(screen.queryByText("zwei")).toBeNull();
    expect(document.activeElement).toBe(nochmal);
  });

  // B-532, AK 4 (B-333, Nachschau zu B-330): nur `plan_inactive` beendet die Runde, jeder andere Server-Fehler hält an wie
  // ein Netzfehler. Ein 403 mit anderem Code, damit beide naheliegenden Verkürzungen fallen: `e instanceof ApiError` und
  // `e.status === 403` statt des Codes – beide beendeten hier die Runde, dann fehlten Meldung und Knöpfe.
  it("hält nach einem anderen Server-Fehler an, auch bei 403", async () => {
    await setLanguage("de");
    review.mockRejectedValueOnce(new ApiError(403, "Forbidden.", "trace-6", "forbidden"));
    renderPractice();
    await answerTyped("one");

    expect(screen.getByRole("button", { name: de.sohn.practice.resend })).toBeTruthy();
    expect(screen.queryByText(de.sohn.problem.plan_inactive)).toBeNull();
    expect(screen.queryByText("zwei")).toBeNull();
  });

  // B-532, AK 2. Rot, wenn „Noch einmal senden" eine leere oder andere Nutzlast schickt, oder wenn nach dem Gelingen kein
  // Blatt folgt (dann bliebe die Meldung stehen oder die Karte rückte still weiter).
  it("schickt mit Noch einmal senden dieselbe Antwort und zeigt dann das Blatt", async () => {
    await setLanguage("de");
    review.mockRejectedValueOnce(new Error("offline")).mockResolvedValueOnce(outcome(true, null));
    renderPractice();
    await answerTyped("one");

    fireEvent.click(screen.getByRole("button", { name: de.sohn.practice.resend }));
    await act(async () => {});

    expect(review).toHaveBeenCalledTimes(2);
    expect(review.mock.calls[1][3]).toEqual({ itemIndex: 0, givenAnswer: "one" });
    expect(praiseSheet().textContent).toContain("+10 🪙");
    expect(screen.queryByRole("alert")).toBeNull();
  });

  // B-532, AK 3. Rot, wenn „Überspringen" fehlt oder nicht weiterschaltet; und – B-531 – wenn der Fokus danach nicht im
  // Feld der nächsten Karte steht (der Knopf verschwindet mit der Meldung).
  it("schaltet mit Überspringen zur nächsten Karte", async () => {
    await setLanguage("de");
    review.mockRejectedValueOnce(new Error("offline"));
    renderPractice();
    await answerTyped("one");

    fireEvent.click(screen.getByRole("button", { name: de.sohn.practice.skip }));

    expect(await screen.findByText("zwei")).toBeTruthy();
    expect(screen.queryByRole("alert")).toBeNull();
    expect((screen.getByRole("textbox") as HTMLInputElement).value).toBe("");
    expect(document.activeElement).toBe(screen.getByRole("textbox"));
  });

  // B-532, AK 5 (B-284). Rot, wenn „Noch einmal senden" an der Sperre von `useAction` vorbei sendet: dann gingen bei zwei
  // Klicks im selben Tick zwei Bewertungen raus. Beide Klicks in EINEM `act`, sonst fängt `disabled` den zweiten (B-325).
  it("schickt bei zweimal Noch einmal senden nur eine Bewertung", async () => {
    await setLanguage("de");
    let answer!: (o: ReviewOutcome | null) => void;
    review.mockRejectedValueOnce(new Error("offline")).mockReturnValueOnce(new Promise((r) => { answer = r; }));
    renderPractice();
    await answerTyped("one");

    const nochmal = screen.getByRole("button", { name: de.sohn.practice.resend });
    await act(async () => { fireEvent.click(nochmal); fireEvent.click(nochmal); });
    expect(review).toHaveBeenCalledTimes(2);
    await act(async () => { answer(outcome(true, null)); });

    expect(review).toHaveBeenCalledTimes(2);
    expect(praiseSheet()).toBeTruthy();
  });

  // B-532, Entscheidung 2: auch ein „Gewusst!" bewegt den Kasten. Rot, wenn der Halt nur für getippte Antworten gilt oder
  // die Urteils-Knöpfe neben der Meldung stehen bleiben (dann urteilte das Kind ein zweites Mal statt neu zu senden).
  it("hält auch eine Selbsteinschätzung an, deren Bewertung scheitert", async () => {
    await setLanguage("de");
    cards.mockImplementation(() => Promise.resolve([
      { itemIndex: 0, prompt: "eins", reveal: "one" } as PracticeCard, card(1, "zwei"),
    ]));
    review.mockRejectedValueOnce(new Error("offline"));
    renderPractice();
    fireEvent.click(await screen.findByRole("button", { name: de.sohn.practice.flip }));
    fireEvent.click(screen.getByRole("button", { name: de.sohn.practice.knewIt }));
    await act(async () => {});

    expect(screen.getByRole("button", { name: de.sohn.practice.resend })).toBeTruthy();
    expect(screen.queryByRole("button", { name: de.sohn.practice.knewIt })).toBeNull();
    expect(screen.queryByText("zwei")).toBeNull();
  });

  // Review B-532. Rot, wenn die Meldung die Antwortfläche an eine andere Stelle im Baum setzt: React baute sie neu auf,
  // und die gewählte Option verlöre ihre Markierung – das Kind sähe nicht mehr, was es geschickt hat.
  it("behält die gewählte Option markiert, während die Meldung steht", async () => {
    await setLanguage("de");
    cards.mockImplementation(() => Promise.resolve([
      { itemIndex: 0, prompt: "la gare", reveal: null, choices: ["der Bahnhof", "die Brücke"] },
      { itemIndex: 1, prompt: "le pont", reveal: null, choices: ["der Bahnhof", "die Brücke"] },
    ] as PracticeCard[]));
    review.mockRejectedValueOnce(new Error("offline"));
    renderPractice();
    fireEvent.click(await screen.findByRole("button", { name: "der Bahnhof" }));
    fireEvent.click(screen.getByRole("button", { name: de.sohn.practice.check }));
    await act(async () => {});

    expect(screen.getByRole("alert")).toBeTruthy();
    expect(screen.getByRole("button", { name: "der Bahnhof" }).getAttribute("aria-pressed")).toBe("true");
  });

  // Review B-532. Rot, wenn das Feld neben der Meldung beschreibbar bleibt: „Noch einmal senden" schickt die alte Antwort,
  // das Feld zeigte dann eine andere als die, die gesendet wird.
  it("macht das Feld nur lesbar, während die Meldung steht", async () => {
    await setLanguage("de");
    review.mockRejectedValueOnce(new Error("offline"));
    renderPractice();
    await answerTyped("one");

    expect((screen.getByRole("textbox") as HTMLInputElement).readOnly).toBe(true);
  });

  // B-532, Entscheidung 2: jede Bewertung, auch die einer Kennenlernkarte. Rot, wenn der Halt nur für getippte Antworten
  // gilt (dann rückt die Kennenlernkarte still weiter und „zwei" steht da).
  it("hält auch eine Kennenlernkarte an, deren Bewertung scheitert", async () => {
    await setLanguage("de");
    cards.mockImplementation(() => Promise.resolve([
      { itemIndex: 0, prompt: "la ville", reveal: "die Stadt", displayOnly: true } as PracticeCard, card(1, "zwei"),
    ]));
    review.mockRejectedValueOnce(new Error("offline"));
    renderPractice();
    fireEvent.click(await screen.findByRole("button", { name: de.sohn.practice.next }));
    await act(async () => {});

    expect(screen.getByRole("button", { name: de.sohn.practice.resend })).toBeTruthy();
    expect(screen.queryByText("zwei")).toBeNull();
  });

  // B-330: anders als ein Netzfehler endet ein Plan, der mitten in der Runde nicht mehr dran ist, die Runde – sonst blättert
  // das Kind weiter, ohne dass etwas zählt. Rot, wenn der Fang den Code wieder verschluckt oder danach doch weiterschaltet
  // (dann steht „zwei" statt der Meldung da). Welcher Formatierer den Satz liefert, trägt `arcadeSpielweg.test.tsx`.
  it("beendet die Runde mit dem Satz der Arcade, wenn der Plan nicht mehr dran ist", async () => {
    await setLanguage("de");
    review.mockRejectedValueOnce(new ApiError(403, "This study plan is not currently active. Ask your parent.", "trace-5", "plan_inactive"));
    render(
      <MemoryRouter initialEntries={["/sohn/practice/5"]}>
        <Routes><Route path="/sohn/practice/:positionId" element={<SohnPractice />} /></Routes>
      </MemoryRouter>,
    );

    const input = await screen.findByRole("textbox");
    fireEvent.change(input, { target: { value: "one" } });
    fireEvent.submit(input.closest("form")!);

    const meldung = await screen.findByText(de.sohn.problem.plan_inactive);
    expect(screen.queryByText("zwei")).toBeNull();
    // B-541 AK 9: das Feld ist mit der Karte verschwunden – der Fokus steht auf der Meldung. Rot, wenn der
    // `landingRef`-Zweig weiter nur nach dem Laden greift (dann liegt er auf `body`).
    expect(document.activeElement).toBe(meldung);
  });
});

/** B-326: wie in der Klausur – ein Plan, den es nicht mehr gibt, führt zur Basis. */
describe("SohnPractice mit einem Plan, den es nicht mehr gibt", () => {
  function renderWithHome() {
    return render(
      <MemoryRouter initialEntries={["/sohn/practice/5"]}>
        <Routes>
          <Route path="/sohn/practice/:positionId" element={<SohnPractice />} />
          <Route path="/sohn" element={<p>BASIS</p>} />
        </Routes>
      </MemoryRouter>,
    );
  }

  // Rot, wenn der Start-Fehler wieder als Meldung steht.
  it("führt zur Basis statt die Serverzeile zu zeigen", async () => {
    startSession.mockRejectedValue(new ApiError(404, "Study plan not found.", "trace-9", "not_found"));
    renderWithHome();

    expect(await screen.findByText("BASIS")).toBeTruthy();
    // Absichtserklärung, kein eigener Beleg: ist die Basis da, ist der Bildschirm schon weg – rot macht ihn nur, was die
    // Zeile davor schon rot macht.
    expect(screen.queryByText(/Study plan not found/)).toBeNull();
  });

  // Rot, wenn jeder Start-Fehler zur Basis führt.
  it("zeigt einen anderen Start-Fehler weiter an", async () => {
    startSession.mockRejectedValue(new ApiError(500, "Server kaputt.", "trace-8"));
    renderWithHome();

    expect(await screen.findByText("Server kaputt. (Ref: trace-8)")).toBeTruthy();
  });
});

/** B-286: nach dem Kartenwechsel behält das Textfeld den Fokus; die neue Aufgabe kommt über die Live-Region. */
describe("SohnPractice, Ansage der neuen Aufgabe", () => {
  // Ein Screenreader sagt nur an, was in eine SCHON VORHANDENE Region hineinwächst (B-134) – darum zählt, dass es vom Laden
  // bis zur zweiten Karte derselbe Knoten ist, nicht nur, dass der Text irgendwo steht. Rot, wenn die Region in der Karte
  // sitzt oder einen `key` trägt (neuer Knoten), wenn sie fehlt, oder wenn sie dem Wechsel nicht folgt („eins").
  it("sagt jede Aufgabe in derselben Region an, ab der ersten", async () => {
    review.mockResolvedValueOnce(null);
    render(
      <MemoryRouter initialEntries={["/sohn/practice/5"]}>
        <Routes><Route path="/sohn/practice/:positionId" element={<SohnPractice />} /></Routes>
      </MemoryRouter>,
    );
    const region = taskRegion();
    expect(region.textContent).toBe("");

    const input = await screen.findByRole("textbox");
    expect(taskRegion()).toBe(region);
    expect(region.textContent).toBe("Neue Aufgabe: eins");
    fireEvent.change(input, { target: { value: "one" } });
    fireEvent.submit(input.closest("form")!);

    expect(await screen.findByText("Neue Aufgabe: zwei")).toBe(region);
  });
});

/** Die Ansage-Region der Aufgabe (das `sr-only`-Absatz-Element), nicht der Toast. */
function taskRegion(): HTMLElement {
  const region = screen.getAllByRole("status").find((el) => el.tagName === "P" && el.classList.contains("sr-only"));
  if (!region) throw new Error("keine Ansage-Region");
  return region;
}

/** B-439: das rote Blatt (B-420, B-526) – Zusicherungen auf seinen Inhalt, nicht auf irgendeinen Text der Seite. */
function mistakeCard(): HTMLElement {
  const held = document.querySelector<HTMLElement>(".lektion-blatt.falsch");
  if (!held) throw new Error("kein rotes Blatt");
  return held;
}

/** B-526: das grüne Blatt nach einer vom Server geprüften, richtigen Antwort. */
function praiseSheet(): HTMLElement {
  const sheet = document.querySelector<HTMLElement>(".lektion-blatt.richtig");
  if (!sheet) throw new Error("kein grünes Blatt");
  return sheet;
}

/** „Weiter" auf dem Blatt. */
function weiter() {
  fireEvent.click(screen.getByRole("button", { name: de.sohn.practice.next }));
}

/** Die Toast-/Live-Region (das `div`), nicht die Ansage-Region der Aufgabe. */
function toastRegion(): HTMLElement {
  const region = screen.getAllByRole("status").find((el) => el.tagName === "DIV" && el.classList.contains("live-region"));
  if (!region) throw new Error("keine Live-Region");
  return region;
}

function renderPractice() {
  return render(
    <MemoryRouter initialEntries={["/sohn/practice/5"]}>
      <Routes><Route path="/sohn/practice/:positionId" element={<SohnPractice />} /></Routes>
    </MemoryRouter>,
  );
}

function outcome(wasCorrect: boolean, expected: string | null): ReviewOutcome {
  return { wasCorrect, expected, awarded: wasCorrect ? 10 : 0, box: 1, dueOn: null, combo: 0, comboBonus: 0, speedBonus: 0, next: null, done: false } as ReviewOutcome;
}

async function answerTyped(text: string) {
  const input = await screen.findByRole("textbox");
  fireEvent.change(input, { target: { value: text } });
  fireEvent.submit(input.closest("form")!);
  await act(async () => {});
}

/** B-526: die Auswahl wertet erst mit „Prüfen“, und das Eingabefeld überlebt den Kartenwechsel. */
describe("SohnPractice, Antwortfläche im Raster", () => {
  beforeEach(async () => { await setLanguage("de"); });

  // AK 3 im Üben. Rot, wenn ein Tipp auf eine Option wieder sofort wertet, oder wenn „Prüfen“ nicht die markierte
  // Option schickt.
  it("wertet eine Auswahl erst mit Prüfen", async () => {
    cards.mockImplementation(() => Promise.resolve([
      { itemIndex: 0, prompt: "la gare", reveal: null, choices: ["der Bahnhof", "die Brücke"] },
      { itemIndex: 1, prompt: "le pont", reveal: null, choices: ["der Bahnhof", "die Brücke"] },
    ] as PracticeCard[]));
    review.mockResolvedValueOnce(outcome(true, null));
    renderPractice();

    fireEvent.click(await screen.findByRole("button", { name: "der Bahnhof" }));
    await act(async () => {});
    expect(review).not.toHaveBeenCalled();
    expect(screen.getByRole("heading", { name: "Wähle die richtige Antwort" })).toBeTruthy();

    fireEvent.click(screen.getByRole("button", { name: de.sohn.practice.check }));
    await act(async () => {});
    expect(review).toHaveBeenCalledTimes(1);
    expect(review.mock.calls[0][3]).toEqual({ itemIndex: 0, givenAnswer: "der Bahnhof" });
    weiter();

    // Die nächste Karte trägt dieselben Optionen – keine davon darf noch markiert sein, und „Prüfen“ ist wieder gesperrt.
    expect(await screen.findByText("le pont")).toBeTruthy();
    expect(screen.getByRole("button", { name: "der Bahnhof" }).getAttribute("aria-pressed")).toBe("false");
    expect((screen.getByRole("button", { name: de.sohn.practice.check }) as HTMLButtonElement).disabled).toBe(true);
  });

});

/** B-420: eine falsch getippte Antwort hält die Karte an, bis das Kind „Weiter" drückt. */
describe("SohnPractice, falsche Antwort", () => {
  beforeEach(async () => { await setLanguage("de"); });

  // AK 1. Rot, wenn der wrong-Zweig wieder weiterschaltet (dann steht „zwei" da) oder die Lösung/eigene Antwort fehlt.
  it("hält die Karte mit eigener Antwort und Lösung an, bis Weiter gedrückt wird", async () => {
    review.mockResolvedValueOnce(outcome(false, "die Straße"));
    renderPractice();
    await answerTyped("die Strasse");

    expect(screen.getByText("eins")).toBeTruthy();
    expect(screen.queryByText("zwei")).toBeNull();
    // B-439: die Lösung steht IN der Karte neben der eigenen Antwort – nicht nur in der Ansage-Region, die hier sr-only ist.
    const held = mistakeCard();
    expect(within(held).getByText("Lösung: die Straße")).toBeTruthy();
    // Lesbar, nicht durchgestrichen – „falsch“ sagt schon das rote Blatt.
    expect(within(held).getByText("Deine Antwort: die Strasse").tagName).not.toBe("S");
    expect(toastRegion().classList.contains("sr-only")).toBe(true);
    // Der Fokus steht auf „Weiter" – Enter trägt das Kind weiter, ohne erst zu zielen.
    const weiter = screen.getByRole("button", { name: de.sohn.practice.next });
    expect(document.activeElement).toBe(weiter);

    fireEvent.click(weiter);
    expect(await screen.findByText("zwei")).toBeTruthy();
    expect(screen.queryByText("Deine Antwort: die Strasse")).toBeNull();
    // Zurück ins Antwortfeld der nächsten Karte – sonst müsste das Kind erst hineinklicken (Review-Fund).
    expect(document.activeElement).toBe(screen.getByRole("textbox"));
  });

  // B-526, Entscheidung 3 (kehrt AK 2 von B-420 um). Rot, wenn ein Treffer wieder von selbst weiterspringt (dann steht
  // „zwei" da) – die Rückmeldung säße dann wieder an zwei Stellen und die Münzen wären nach einer Sekunde weg.
  it("hält auch nach einer richtigen Antwort mit dem Blatt an, bis Weiter gedrückt wird", async () => {
    review.mockResolvedValueOnce(outcome(true, "die Stadt"));
    renderPractice();
    await answerTyped("die Stadt");

    expect(screen.getByText("eins")).toBeTruthy();
    expect(screen.queryByText("zwei")).toBeNull();
    expect(praiseSheet().textContent).toContain("+10 🪙");
    // Die Ansage läuft über die Region, die schon dastand – das Blatt selbst ist keine (B-134).
    expect(toastRegion().classList.contains("sr-only")).toBe(true);
    expect(toastRegion().textContent).toBe("Richtig. +10 🪙");
    const knopf = screen.getByRole("button", { name: de.sohn.practice.next });
    expect(document.activeElement).toBe(knopf);

    fireEvent.click(knopf);
    expect(await screen.findByText("zwei")).toBeTruthy();
    expect(document.querySelector(".lektion-blatt")).toBeNull();
    // Zurück ins Antwortfeld der nächsten Karte – sonst müsste das Kind nach jeder Karte erst hineintippen.
    expect(document.activeElement).toBe(screen.getByRole("textbox"));
  });

  // Entscheidung 8. Rot, wenn der Tipp-Knopf unter dem Blatt weiter bedienbar ist – ein Tipp gehört vor die Antwort.
  it("nimmt den Tipp-Knopf weg, solange das Blatt steht", async () => {
    cards.mockImplementation(() => Promise.resolve([{ ...card(0, "eins"), hint: "beginnt mit o" }, card(1, "zwei")]));
    review.mockResolvedValueOnce(outcome(true, null));
    renderPractice();
    expect(await screen.findByRole("button", { name: de.sohn.practice.hint })).toBeTruthy();
    await answerTyped("one");

    expect(praiseSheet()).toBeTruthy();
    expect(screen.queryByRole("button", { name: de.sohn.practice.hint })).toBeNull();
  });

  // Review B-526. Rot, wenn ein Doppeltipp auf „Weiter" die nächste Karte trifft: die Kennenlern-Karte wäre gebucht
  // (ein zweites `review`), ohne dass das Kind sie gesehen hat. Nach der Ruhezeit nimmt derselbe Knopf wieder an –
  // rot auch, wenn die Sperre nie endet.
  it("lässt einen Doppeltipp auf Weiter nicht auf die nächste Karte durchschlagen", async () => {
    cards.mockImplementation(() => Promise.resolve([
      card(0, "eins"), { itemIndex: 1, prompt: "la ville", reveal: "die Stadt", displayOnly: true } as PracticeCard, card(2, "drei"),
    ]));
    review.mockResolvedValue(outcome(true, null));
    renderPractice();
    await answerTyped("one");
    expect(review).toHaveBeenCalledTimes(1);

    weiter();
    // Derselbe Platz, derselbe Name: der zweite Tipp landet auf dem „Weiter" der Kennenlern-Karte.
    weiter();
    await act(async () => {});
    expect(review).toHaveBeenCalledTimes(1);
    expect(screen.getByText("la ville")).toBeTruthy();

    await act(async () => { await new Promise((r) => setTimeout(r, NACH_BLATT_MS + 30)); });
    weiter();
    await act(async () => {});
    expect(review).toHaveBeenCalledTimes(2);
    expect(await screen.findByText("drei")).toBeTruthy();
  });

  // Rot, wenn das Blatt Münzen zeigt, die es nicht gab: eine nicht wertbare Karte (nicht fällig, heute schon gewertet)
  // darf keinen Gewinn vortäuschen.
  it("zeigt auf dem Blatt keine Münzen, wenn die Antwort richtig, aber nichts wert war", async () => {
    review.mockResolvedValueOnce({ ...outcome(true, null), awarded: 0 });
    renderPractice();
    await answerTyped("die Stadt");

    expect(praiseSheet().textContent).not.toContain("🪙");
    expect(toastRegion().textContent).toBe("Richtig.");
  });

  // B-422, AK 4. Rot, wenn das Blatt das Kennzeichen `misspelled` ignoriert (dann stünde „Fast!" da) oder die Lösung
  // dabei verliert; die Wertung bleibt falsch (rotes Blatt).
  it("zeigt auf dem Blatt Falsch geschrieben samt Lösung, wenn der Server es meldet", async () => {
    review.mockResolvedValueOnce({ ...outcome(false, "clinic"), misspelled: true });
    renderPractice();
    await answerTyped("clinik");

    const held = mistakeCard();
    expect(within(held).getByText(de.sohn.test.misspelled)).toBeTruthy();
    // Die eigene Antwort fällt weg; die Lösung markiert den verschriebenen Buchstaben.
    expect(held.querySelector(".rev")!.textContent).toBe("Lösung: clinic");
    expect(held.querySelector("mark")!.textContent).toBe("c");
    expect(within(held).queryByText(/Deine Antwort/)).toBeNull();
    expect(within(held).queryByText(de.sohn.lektion.almost)).toBeNull();
    expect(toastRegion().textContent).toBe("Falsch geschrieben. Lösung: clinic");
  });

  // B-422, AK 4, zweiter Teil. Rot, wenn das Etikett auch ohne Kennzeichen erscheint.
  // B-529 AK 5: die Erklärung der Bewertung landet auf dem Blatt. Rot, wenn SohnPractice `explanation` nicht ins Blatt
  // reicht – bei falsch wie bei richtig.
  it("reicht die Erklärung des Servers ins Blatt", async () => {
    cards.mockImplementation(() => Promise.resolve([card(0, "eins"), card(1, "zwei")]));
    review.mockResolvedValueOnce({ ...outcome(false, "au"), explanation: "à + le wird zu au" })
      .mockResolvedValueOnce({ ...outcome(true, "des"), explanation: "de + les wird zu des" });
    renderPractice();
    await answerTyped("à le");
    expect(document.querySelector(".lektion-blatt")!.textContent).toContain("💡 Regel: à + le wird zu au");

    fireEvent.click(screen.getByRole("button", { name: de.sohn.practice.next }));
    await screen.findByText("zwei");
    await answerTyped("des");
    expect(document.querySelector(".lektion-blatt")!.textContent).toContain("💡 Regel: de + les wird zu des");
  });

  // B-530 AK 7: „Weiter" auf dem Blatt meldet die nächste Karte als sichtbar – genau einmal, und nennt sie (Code-Review:
  // der Server stempelt nur seine aktuelle Karte). Die Indizes sind absichtlich nicht 0, 1, 2. Rot, wenn `next()` den
  // Aufruf nicht macht, ihn doppelt schickt, keine Karte nennt oder die Position in der Runde statt des `itemIndex`.
  it("meldet mit Weiter die nächste Karte genau einmal als sichtbar", async () => {
    cards.mockImplementation(() => Promise.resolve([card(7, "eins"), card(4, "zwei"), card(9, "drei")]));
    review.mockResolvedValueOnce(outcome(false, "one"));
    renderPractice();
    await answerTyped("falsch");
    expect(cardShown).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole("button", { name: de.sohn.practice.next }));

    expect(await screen.findByText("zwei")).toBeTruthy();
    expect(cardShown).toHaveBeenCalledTimes(1);
    expect(cardShown).toHaveBeenCalledWith(3, 5, 11, 4);
  });

  // B-530 AK 7: die Meldung ist fire-and-forget. Rot, wenn `next()` auf sie wartet (dann stünde „zwei" erst nach der
  // Antwort da – hier nie) oder ihr Fehlschlag die Runde anhält.
  it("schaltet weiter, ohne auf die Meldung zu warten, auch wenn sie scheitert", async () => {
    let fail!: (e: unknown) => void;
    cardShown.mockReturnValueOnce(new Promise((_, rej) => { fail = rej; }));
    review.mockResolvedValueOnce(outcome(false, "one"));
    renderPractice();
    await answerTyped("falsch");

    fireEvent.click(screen.getByRole("button", { name: de.sohn.practice.next }));
    expect(await screen.findByText("zwei")).toBeTruthy();
    await act(async () => { fail(new Error("offline")); });

    expect(screen.getByText("zwei")).toBeTruthy();
    expect(screen.queryByRole("alert")).toBeNull();
  });

  it("bleibt ohne Kennzeichen bei der gewohnten Absage", async () => {
    review.mockResolvedValueOnce({ ...outcome(false, "clinic"), misspelled: false });
    renderPractice();
    await answerTyped("hospital");

    const held = mistakeCard();
    expect(within(held).getByText(de.sohn.lektion.almost)).toBeTruthy();
    expect(within(held).queryByText(de.sohn.test.misspelled)).toBeNull();
    expect(within(held).getByText("Lösung: clinic")).toBeTruthy();
  });

  // AK 3. Rot, wenn der Halt an einer gelieferten Lösung hängt statt an der falschen Antwort.
  it("hält in einer Menge ohne Lösung mit der Absage an", async () => {
    review.mockResolvedValueOnce(outcome(false, null));
    renderPractice();
    await answerTyped("Hamburg");

    expect(screen.getByText("eins")).toBeTruthy();
    expect(within(mistakeCard()).getByText(de.sohn.practice.wrong)).toBeTruthy();
    expect(toastRegion().classList.contains("sr-only")).toBe(true);
    expect(screen.getByRole("button", { name: de.sohn.practice.next })).toBeTruthy();
  });

  // AK 4. Rot, wenn „Weiter" auf der letzten Karte nicht ins Rundenende führt.
  it("führt nach der letzten Karte über Weiter ins Rundenende", async () => {
    cards.mockImplementation(() => Promise.resolve([card(0, "eins")]));
    review.mockResolvedValueOnce(outcome(false, "die Stadt"));
    renderPractice();
    await answerTyped("falsch");
    expect(screen.queryByText(de.sohn.practice.roundDone)).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: de.sohn.practice.next }));
    const heading = await screen.findByText(de.sohn.practice.roundDone);
    // B-439: der Fokus geht an das Rundenende, nicht auf `body` – „Weiter" ist mit der Karte verschwunden.
    expect(document.activeElement).toBe(heading);
  });

  // B-439. Rot, wenn „Weiter" auf dem Blatt der letzten Karte den Fokus nicht an die Überschrift des Rundenendes gibt –
  // das Blatt verschwindet samt seinem fokussierten Knopf.
  it("fokussiert das Rundenende auch nach einem Treffer auf der letzten Karte", async () => {
    cards.mockImplementation(() => Promise.resolve([card(0, "eins")]));
    review.mockResolvedValueOnce(outcome(true, "one"));
    renderPractice();
    await answerTyped("one");
    weiter();

    const heading = await screen.findByText(de.sohn.practice.roundDone);
    expect(document.activeElement).toBe(heading);
  });

  // Rot, wenn auch „Nochmal" der Selbsteinschätzung anhält – dort stand die Lösung schon auf der Rückseite.
  it("hält bei der Selbsteinschätzung nicht an", async () => {
    cards.mockImplementation(() => Promise.resolve([
      { itemIndex: 0, prompt: "eins", reveal: "one" } as PracticeCard,
      { itemIndex: 1, prompt: "zwei", reveal: "two" } as PracticeCard,
    ]));
    review.mockResolvedValueOnce(outcome(false, "one"));
    renderPractice();
    fireEvent.click(await screen.findByRole("button", { name: de.sohn.practice.flip }));
    fireEvent.click(screen.getByRole("button", { name: de.sohn.practice.again }));

    expect(await screen.findByText("zwei")).toBeTruthy();
  });
});

/**
 * B-531: verschwindet das fokussierte Element durch eine Handlung des Kindes, geht der Fokus an das erste Bedienbare der
 * Antwortzone – sonst fällt er auf `body`, und wer an der Tastatur oder mit Screenreader übt, verliert die Stelle.
 */
describe("SohnPractice, Fokus beim Kartenwechsel", () => {
  beforeEach(async () => { await setLanguage("de"); });

  function selbst(itemIndex: number, prompt: string): PracticeCard {
    return { itemIndex, prompt, reveal: "x" } as PracticeCard;
  }

  // AK 1. Rot, wenn der Effekt den Wechsel `front → back` ignoriert (Aufdecken ersetzt den fokussierten Knopf), oder wenn
  // er auf „Gewusst!" statt auf den ersten Knopf zielt (Entscheidung 1: Enter darf nicht der bequeme Weg zum Lob sein).
  it("setzt den Fokus nach Aufdecken auf Nochmal", async () => {
    cards.mockImplementation(() => Promise.resolve([selbst(0, "eins"), selbst(1, "zwei")]));
    renderPractice();
    fireEvent.click(await screen.findByRole("button", { name: de.sohn.practice.flip }));
    await act(async () => {});

    expect(document.activeElement).toBe(screen.getByRole("button", { name: de.sohn.practice.again }));
  });

  // AK 2. Rot, wenn der Zweig `back → front` fehlt: das Urteil nimmt seinen Knopf mit, die nächste Karte bekäme keinen Fokus.
  it("setzt den Fokus nach Gewusst in die nächste Karte", async () => {
    cards.mockImplementation(() => Promise.resolve([selbst(0, "eins"), card(1, "zwei")]));
    review.mockResolvedValueOnce(outcome(true, null));
    renderPractice();
    fireEvent.click(await screen.findByRole("button", { name: de.sohn.practice.flip }));
    fireEvent.click(screen.getByRole("button", { name: de.sohn.practice.knewIt }));

    expect(await screen.findByText("zwei")).toBeTruthy();
    expect(document.activeElement).toBe(screen.getByRole("textbox"));
  });

  // AK 3. Rot, wenn der Effekt wieder nur an `phase` hängt: „Weiter" der Kennenlernkarte bleibt bei `front`, nur `idx` wechselt.
  it("setzt den Fokus nach Weiter auf einer Kennenlernkarte in das Feld der nächsten", async () => {
    cards.mockImplementation(() => Promise.resolve([
      { itemIndex: 0, prompt: "la ville", reveal: "die Stadt", displayOnly: true } as PracticeCard, card(1, "zwei"),
    ]));
    review.mockResolvedValueOnce(outcome(false, null));
    renderPractice();
    fireEvent.click(await screen.findByRole("button", { name: de.sohn.practice.next }));

    expect(await screen.findByText("zwei")).toBeTruthy();
    expect(document.activeElement).toBe(screen.getByRole("textbox"));
  });

  // AK 4 (B-443). Rot, wenn der Kartenwechsel-Zweig auch den ersten Aufbau trifft: dann spränge beim Öffnen der Seite
  // ungefragt die Tastatur auf.
  it("setzt beim ersten Laden keinen Fokus in die Karte", async () => {
    renderPractice();
    await screen.findByRole("textbox");
    await act(async () => {});

    expect(document.activeElement).toBe(document.body);
  });
});

/** B-419: eine Runde, die neue Wörter gezeigt hat, sagt das am Ende und bietet die Abfrage an. */
describe("SohnPractice, Rundenende nach dem Kennenlernen", () => {
  beforeEach(async () => { await setLanguage("de"); });

  function shown(itemIndex: number, prompt: string, introduction: boolean): PracticeCard {
    return { itemIndex, prompt, reveal: "x", displayOnly: true, introduction } as PracticeCard;
  }

  // AK 5. Rot, wenn das Ende die Einführung nicht zählt oder „Jetzt abfragen" keine neue Runde startet.
  it("nennt die neuen Wörter und startet mit Jetzt abfragen eine neue Runde", async () => {
    cards.mockImplementationOnce(() => Promise.resolve([shown(0, "la ville", true), shown(1, "la rue", true)]));
    review.mockResolvedValue(outcome(false, null));
    const clock = vi.spyOn(Date, "now").mockReturnValue(1_000_000);
    renderPractice();
    for (const _ of [0, 1]) {
      fireEvent.click(await screen.findByRole("button", { name: de.sohn.practice.next }));
      await act(async () => {});
    }

    expect(await screen.findByText("2 neue Wörter kennengelernt")).toBeTruthy();
    expect(startSession).toHaveBeenCalledTimes(1);
    startSession.mockResolvedValueOnce({ id: 12, testable: true });
    clock.mockReturnValue(1_005_000); // fünf Sekunden nach dem Start der Kennenlern-Runde
    fireEvent.click(screen.getByRole("button", { name: de.sohn.practice.quizNow }));

    expect(await screen.findByText("eins")).toBeTruthy(); // die Abfrage-Runde (Standard-Karten des Mocks)
    expect(startSession).toHaveBeenCalledTimes(2);
    // Die Kennenlern-Sitzung ist geschlossen, genau einmal, und nur sie (Review-Fund: ohne `round` in den
    // Abhängigkeiten des Heartbeat-Effekts bliebe sie offen).
    await act(async () => {});
    expect(endSession.mock.calls).toEqual([[3, 5, 11]]);
    // Mit ihren Rest-Sekunden: ein Zurücksetzen der Uhr schon beim Klick schickte 0 (Review-Fund).
    expect(heartbeat).toHaveBeenCalledWith(3, 5, 11, 5, true);
    clock.mockRestore();
  });

  // B-443. Rot, wenn der Fokus nach „Jetzt abfragen" auf `body` fällt: der Knopf verschwindet mit dem Rundenende.
  it("setzt den Fokus nach Jetzt abfragen in die erste Karte", async () => {
    cards.mockImplementationOnce(() => Promise.resolve([shown(0, "la ville", true)]));
    review.mockResolvedValue(outcome(false, null));
    renderPractice();
    fireEvent.click(await screen.findByRole("button", { name: de.sohn.practice.next }));
    await act(async () => {});
    startSession.mockResolvedValueOnce({ id: 12, testable: true });

    fireEvent.click(await screen.findByRole("button", { name: de.sohn.practice.quizNow }));
    await screen.findByText("eins");
    await act(async () => {});

    // Das Antwortfeld der neuen Karte, nicht irgendein Knopf (B-515).
    expect(document.activeElement).toBe(screen.getByRole("textbox"));
  });

  // B-515. Rot, wenn eine leere neue Runde den Fokus auf `body` fallen lässt („Jetzt abfragen" ist weg, keine Karte da).
  it("setzt den Fokus nach Jetzt abfragen auf die Überschrift, wenn nichts fällig ist", async () => {
    cards.mockImplementationOnce(() => Promise.resolve([shown(0, "la ville", true)]))
      .mockImplementationOnce(() => Promise.resolve([]));
    review.mockResolvedValue(outcome(false, null));
    renderPractice();
    fireEvent.click(await screen.findByRole("button", { name: de.sohn.practice.next }));
    await act(async () => {});
    startSession.mockResolvedValueOnce({ id: 12, testable: true });

    fireEvent.click(await screen.findByRole("button", { name: de.sohn.practice.quizNow }));
    const title = await screen.findByText(de.sohn.practice.nothingDue);
    await act(async () => {});

    expect(document.activeElement).toBe(title);
  });

  // B-443 (Review). Rot, wenn nur Karten mit Eingabefeld den Fokus bekommen: welche Stufe die neue Runde bringt, entscheidet
  // der Server, und eine Kennenlern- oder Umdreh-Karte hat keins.
  it("setzt den Fokus nach Jetzt abfragen auch auf eine Karte ohne Eingabefeld", async () => {
    cards.mockImplementationOnce(() => Promise.resolve([shown(0, "la ville", true)]))
      .mockImplementationOnce(() => Promise.resolve([shown(0, "la rue", false)]));
    review.mockResolvedValue(outcome(false, null));
    renderPractice();
    fireEvent.click(await screen.findByRole("button", { name: de.sohn.practice.next }));
    await act(async () => {});
    startSession.mockResolvedValueOnce({ id: 12, testable: true });

    fireEvent.click(await screen.findByRole("button", { name: de.sohn.practice.quizNow }));
    await screen.findByText("la rue");
    await act(async () => {});

    expect(document.activeElement).toBe(screen.getByRole("button", { name: de.sohn.practice.next }));
  });

  // B-518. Rot, wenn eine gescheiterte neue Runde den Fokus auf `body` fallen lässt: die Meldung (role="alert") bekommt ihn.
  it("setzt den Fokus nach Jetzt abfragen auf die Meldung, wenn die Runde scheitert", async () => {
    cards.mockImplementationOnce(() => Promise.resolve([shown(0, "la ville", true)]));
    review.mockResolvedValue(outcome(false, null));
    renderPractice();
    fireEvent.click(await screen.findByRole("button", { name: de.sohn.practice.next }));
    await act(async () => {});
    startSession.mockRejectedValueOnce(new Error("Verbindung weg"));

    fireEvent.click(await screen.findByRole("button", { name: de.sohn.practice.quizNow }));
    const meldung = await screen.findByRole("alert");
    await act(async () => {});

    expect(document.activeElement).toBe(meldung);
  });

  // AK 5, zweiter Teil. Rot, wenn das Ende an `displayOnly` statt an `introduction` hängt.
  it("behält nach einer Kennenlern-Stufe des Tages den heutigen Bildschirm", async () => {
    cards.mockImplementationOnce(() => Promise.resolve([shown(0, "la ville", false)]));
    review.mockResolvedValue(outcome(false, null));
    renderPractice();
    fireEvent.click(await screen.findByRole("button", { name: de.sohn.practice.next }));

    expect(await screen.findByText(de.sohn.practice.roundDone)).toBeTruthy();
    expect(screen.queryByRole("button", { name: de.sohn.practice.quizNow })).toBeNull();
    // Die Münz-Kachel steht (der heutige Bildschirm), aber keine Genauigkeit: nichts wurde vom Server geprüft.
    expect(screen.getByText("+0")).toBeTruthy();
    expect(screen.queryByLabelText(/Genauigkeit/)).toBeNull();
  });

  // Review-Fund: eine Runde mischt neue und schon bekannte Karten – was die abgefragten verdient haben, bleibt stehen.
  it("zeigt in einer gemischten Runde neben den neuen Wörtern auch die verdienten Münzen", async () => {
    cards.mockImplementationOnce(() => Promise.resolve([shown(0, "la ville", true), card(1, "la rue")]));
    review.mockResolvedValueOnce(outcome(false, null)).mockResolvedValueOnce(outcome(true, "die Straße"));
    renderPractice();
    fireEvent.click(await screen.findByRole("button", { name: de.sohn.practice.next }));
    await answerTyped("die Straße");
    weiter();

    expect(await screen.findByText("1 neues Wort kennengelernt")).toBeTruthy();
    expect(screen.getByText("+10")).toBeTruthy();
    // Die Kennenlern-Karte zählt nicht in die Genauigkeit – eine geprüfte Antwort, eine richtige.
    expect(screen.getByLabelText("Genauigkeit 100 %")).toBeTruthy();
  });

  // B-526, Entscheidung 9. Rot, wenn die Genauigkeit falsche Antworten nicht zählt (dann stünde 100 %) oder ein selbst
  // vergebenes „Gewusst!" mitzählt (dann stünden 67 %): geprüft sind hier zwei getippte Antworten, eine davon richtig.
  it("rechnet die Genauigkeit nur aus Antworten, die der Server geprüft hat", async () => {
    cards.mockImplementationOnce(() => Promise.resolve([
      card(0, "la rue"), card(1, "le pont"), { itemIndex: 2, prompt: "la gare", reveal: "der Bahnhof" } as PracticeCard,
    ]));
    review.mockResolvedValueOnce(outcome(true, null)).mockResolvedValueOnce(outcome(false, "die Brücke"))
      .mockResolvedValueOnce(outcome(true, null));
    renderPractice();
    await answerTyped("die Straße");
    weiter();
    await answerTyped("der Fluss");
    weiter();
    // Die Ruhezeit nach dem Blatt abwarten – sonst schluckte sie das „Aufdecken" (siehe den Doppeltipp-Fall).
    await act(async () => { await new Promise((r) => setTimeout(r, NACH_BLATT_MS + 30)); });
    fireEvent.click(await screen.findByRole("button", { name: de.sohn.practice.flip }));
    fireEvent.click(screen.getByRole("button", { name: de.sohn.practice.knewIt }));

    expect(await screen.findByText(de.sohn.practice.roundDone)).toBeTruthy();
    expect(screen.getByLabelText("Genauigkeit 50 %")).toBeTruthy();
  });

  // Rot, wenn eine Runde nur aus Selbsteinschätzung eine Genauigkeit zeigte – sie wäre selbst vergeben.
  it("zeigt nach einer Runde nur aus Selbsteinschätzung keine Genauigkeit", async () => {
    cards.mockImplementationOnce(() => Promise.resolve([{ itemIndex: 0, prompt: "la gare", reveal: "der Bahnhof" } as PracticeCard]));
    review.mockResolvedValueOnce(outcome(true, null));
    renderPractice();
    fireEvent.click(await screen.findByRole("button", { name: de.sohn.practice.flip }));
    fireEvent.click(screen.getByRole("button", { name: de.sohn.practice.knewIt }));

    expect(await screen.findByText(de.sohn.practice.roundDone)).toBeTruthy();
    expect(screen.getByText("+10")).toBeTruthy();
    expect(screen.queryByLabelText(/Genauigkeit/)).toBeNull();
  });
});

/**
 * B-522: der Übungsbildschirm kennt die Restversuche des Tests (`testAttemptsLeft`) und weiß, ob das Kind vom verpatzten
 * Test kommt. Er bietet keinen Test an, den der Server heute ablehnen würde, und feiert nach einer Niederlage nicht.
 */
describe("SohnPractice nach dem Test", () => {
  beforeEach(async () => { await setLanguage("de"); });

  function renderFrom(state?: { nachTest: boolean }) {
    return render(
      <MemoryRouter initialEntries={[{ pathname: "/sohn/practice/5", state }]}>
        <Routes><Route path="/sohn/practice/:positionId" element={<SohnPractice />} /></Routes>
      </MemoryRouter>,
    );
  }

  // AK 4. Rot, wenn der Knopf nur an `testable` hängt oder der Leerbildschirm die aufgebrauchten Versuche verschweigt.
  it("bietet bei aufgebrauchten Versuchen keinen Test an und sagt, wann es weitergeht", async () => {
    startSession.mockResolvedValue({ id: 11, testable: true, testAttemptsLeft: 0 });
    cards.mockImplementation(() => Promise.resolve([]));
    renderFrom({ nachTest: true });

    expect(await screen.findByText(de.sohn.practice.nothingDueAfterTest)).toBeTruthy();
    expect(screen.getByText(de.sohn.practice.attemptsUsedUp)).toBeTruthy();
    expect(screen.queryByRole("button", { name: de.sohn.practice.toTest })).toBeNull();
  });

  // AK 4 von der Startseite: dieselbe Lage ohne Herkunft. Rot, wenn der Titel nur am Navigation-State hinge.
  it("feiert auch von der Startseite nicht, wenn die Versuche aufgebraucht sind", async () => {
    startSession.mockResolvedValue({ id: 11, testable: true, testAttemptsLeft: 0 });
    cards.mockImplementation(() => Promise.resolve([]));
    renderFrom();

    expect(await screen.findByText(de.sohn.practice.nothingDueAfterTest)).toBeTruthy();
    expect(screen.queryByText(de.sohn.practice.nothingDue)).toBeNull();
  });

  // AK 5. Rot, wenn das Rundenende nur `testable` prüft.
  it("zeigt am Rundenende kein Weiter zum Test, wenn keine Versuche übrig sind", async () => {
    startSession.mockResolvedValue({ id: 11, testable: true, testAttemptsLeft: 0 });
    cards.mockImplementation(() => Promise.resolve([card(0, "eins")]));
    review.mockResolvedValueOnce(outcome(true, "eins"));
    renderFrom();
    await answerTyped("eins");
    fireEvent.click(screen.getByRole("button", { name: de.sohn.practice.next }));

    expect(await screen.findByText(de.sohn.practice.roundDone)).toBeTruthy();
    expect(screen.queryByRole("button", { name: de.sohn.practice.onToTest })).toBeNull();
  });

  // Gegenprobe zu AK 5 – sonst bliebe der Fall oben auch grün, wenn der Knopf nie erschiene. Rot, wenn das Gate auch bei
  // übrigen Versuchen sperrt.
  it("zeigt am Rundenende Weiter zum Test, solange Versuche übrig sind", async () => {
    startSession.mockResolvedValue({ id: 11, testable: true, testAttemptsLeft: 1 });
    cards.mockImplementation(() => Promise.resolve([card(0, "eins")]));
    review.mockResolvedValueOnce(outcome(true, "eins"));
    renderFrom();
    await answerTyped("eins");
    fireEvent.click(screen.getByRole("button", { name: de.sohn.practice.next }));

    expect(await screen.findByRole("button", { name: de.sohn.practice.onToTest })).toBeTruthy();
  });

  // AK 6. Rot, wenn der Titel den State nicht liest (dann stünde wieder „Nichts fällig 🎉“) oder der Knopf fehlt.
  it("feiert nach dem Test nicht, bietet ihn aber an, solange Versuche übrig sind", async () => {
    startSession.mockResolvedValue({ id: 11, testable: true, testAttemptsLeft: 1 });
    cards.mockImplementation(() => Promise.resolve([]));
    renderFrom({ nachTest: true });

    expect(await screen.findByText(de.sohn.practice.nothingDueAfterTest)).toBeTruthy();
    expect(screen.queryByText(de.sohn.practice.nothingDue)).toBeNull();
    expect(screen.getByRole("button", { name: de.sohn.practice.toTest })).toBeTruthy();
  });

  // Code-Review B-522: der State wird nach dem Lesen aus dem Verlaufseintrag entfernt, die Seite behält ihn aber. Rot,
  // wenn der Eintrag `nachTest` behält (ein späteres „Zurück" unterdrückte die Feier wieder) oder wenn das Entfernen
  // die Seite den gelesenen Wert vergessen lässt (dann stünde „Nichts fällig 🎉" da).
  it("räumt nachTest nach dem Lesen aus dem Verlauf, ohne die Seite umzustimmen", async () => {
    startSession.mockResolvedValue({ id: 11, testable: true, testAttemptsLeft: 1 });
    cards.mockImplementation(() => Promise.resolve([]));
    function Ortsstand() {
      return <output data-testid="ortsstand">{JSON.stringify(useLocation().state ?? null)}</output>;
    }
    render(
      <MemoryRouter initialEntries={[{ pathname: "/sohn/practice/5", state: { nachTest: true } }]}>
        <Routes><Route path="/sohn/practice/:positionId" element={<SohnPractice />} /></Routes>
        <Ortsstand />
      </MemoryRouter>,
    );

    expect(await screen.findByText(de.sohn.practice.nothingDueAfterTest)).toBeTruthy();
    expect(screen.getByTestId("ortsstand").textContent).toBe("null");
    expect(screen.queryByText(de.sohn.practice.nothingDue)).toBeNull();
  });

  // AK 7. Rot, wenn der neue Titel auch ohne Herkunft und mit übrigen Versuchen erscheint.
  it("bleibt von der Startseite mit übrigen Versuchen beim gewohnten Nichts fällig", async () => {
    startSession.mockResolvedValue({ id: 11, testable: true, testAttemptsLeft: 2 });
    cards.mockImplementation(() => Promise.resolve([]));
    renderFrom();

    expect(await screen.findByText(de.sohn.practice.nothingDue)).toBeTruthy();
    expect(screen.getByRole("button", { name: de.sohn.practice.toTest })).toBeTruthy();
  });
});

/** B-598: eine aufgegebene Lernkarte kommt einmal ans Ende der Runde. */
describe("SohnPractice, Lernkarte am Rundenende", () => {
  beforeEach(async () => { await setLanguage("de"); });

  // Rot, wenn die Runde `repeatsAtEnd` überginge: der Server hat die Karte an seine Folge gehängt, die Runde hier hätte
  // sie nicht – sie endete nach einer Karte, und die Folgen liefen auseinander.
  it("hängt die Karte an, wenn der Server sie wiederholt", async () => {
    cards.mockImplementation(() => Promise.resolve([{
      itemIndex: 0, stage: 8, type: "Vocabulary", prompt: "gehen", reveal: "walk", learnCard: true, introduction: true,
    } as PracticeCard]));
    review.mockResolvedValueOnce({ ...outcome(false, "walk"), repeatsAtEnd: true });
    renderPractice();

    fireEvent.click(await screen.findByRole("button", { name: de.sohn.lernkarte.uncover }));
    fireEvent.change(screen.getByLabelText(de.sohn.lernkarte.copy), { target: { value: "walk" } });
    fireEvent.click(screen.getByRole("button", { name: de.sohn.lernkarte.ok }));
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: de.sohn.lernkarte.giveUp })); });

    expect(review).toHaveBeenCalledWith(expect.anything(), expect.anything(), expect.anything(),
      { itemIndex: 0, givenAnswer: null, gaveUp: true });
    expect(screen.getByText(de.sohn.lektion.learnAgain)).toBeTruthy();
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: de.sohn.practice.next })); });

    expect(screen.getByText(/Karte 2 \/ 2/)).toBeTruthy();
    expect(screen.getByRole("button", { name: de.sohn.lernkarte.uncover })).toBeTruthy();
  });
});

/*
 * B-686 (Entscheidung 1): der Buchbezug (B-588) stand auf dem weggefallenen Blatt vor dem Start und steht jetzt im Kopf
 * der Runde – gelesen aus der Plan-Übersicht, nur mit Seite.
 */
describe("SohnPractice, Buchbezug im Kopf", () => {
  beforeEach(async () => { await setLanguage("de"); });
  const mitPosition = (bookPages: string | null) => overview.mockResolvedValue({
    currentStreak: 0,
    today: { positions: [{ positionId: 5, seriesName: "Access 3", seriesUnitLabel: "Unit 2", bookPages }] },
  });

  // Rot, wenn die Runde den Buchbezug nicht zeigt (er wäre mit dem Blatt verschwunden) oder die Pille anders gebaut wird.
  it("zeigt Reihe, Unit und Seite der Position", async () => {
    mitPosition("42");
    renderPractice();
    expect((await screen.findByText("Access 3 · Unit 2 · S. 42")).classList).toContain("pfad-buchbezug");
  });

  // Rot, wenn die Pille auch ohne Seite erscheint – als halbe „Reihe · Unit“ (B-588 AK 8). Erst nach der Antwort der
  // Übersicht geprüft, sonst stünde die Abwesenheit schon vor dem Lesen fest.
  it("zeigt ohne Seite keine Pille", async () => {
    mitPosition(null);
    renderPractice();
    await screen.findByText(/Karte 1 \/ 3/);
    await act(async () => { await overview.mock.results[0]?.value; });
    expect(overview).toHaveBeenCalledWith(3);
    expect(document.querySelector(".pfad-buchbezug")).toBeNull();
  });
});
