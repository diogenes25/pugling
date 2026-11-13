import { act, fireEvent, render, screen, within } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
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
      overview: () => Promise.resolve({ currentStreak: 0 }),
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

  // Rot, wenn das Weiterschalten in den Erfolgsweg rückt (`next()` im `try`) oder der innere Fang fehlt (`run` liefert
  // dann `false`): eine gescheiterte Bewertung ließe das Kind auf der Karte stehen.
  it("rückt auch nach einer gescheiterten Bewertung weiter", async () => {
    review.mockRejectedValueOnce(new Error("offline"));
    render(
      <MemoryRouter initialEntries={["/sohn/practice/5"]}>
        <Routes><Route path="/sohn/practice/:positionId" element={<SohnPractice />} /></Routes>
      </MemoryRouter>,
    );

    const input = await screen.findByRole("textbox");
    fireEvent.change(input, { target: { value: "one" } });
    fireEvent.submit(input.closest("form")!);

    expect(await screen.findByText("zwei")).toBeTruthy();
  });

  // B-333 (Nachschau zu B-330): nur `plan_inactive` beendet die Runde, jeder andere Server-Fehler nicht. Ein 403 mit anderem
  // Code, damit beide naheliegenden Verkürzungen fallen: `e instanceof ApiError` und `e.status === 403` statt des Codes.
  it("rückt nach einem anderen Server-Fehler weiter, auch bei 403", async () => {
    review.mockRejectedValueOnce(new ApiError(403, "Forbidden.", "trace-6", "forbidden"));
    render(
      <MemoryRouter initialEntries={["/sohn/practice/5"]}>
        <Routes><Route path="/sohn/practice/:positionId" element={<SohnPractice />} /></Routes>
      </MemoryRouter>,
    );

    const input = await screen.findByRole("textbox");
    fireEvent.change(input, { target: { value: "one" } });
    fireEvent.submit(input.closest("form")!);

    expect(await screen.findByText("zwei")).toBeTruthy();
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

    expect(await screen.findByText(de.sohn.problem.plan_inactive)).toBeTruthy();
    expect(screen.queryByText("zwei")).toBeNull();
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
    expect(screen.getByText("die Strasse").tagName).toBe("S");
    // B-439: die Lösung steht IN der Karte neben der eigenen Antwort – nicht nur in der Ansage-Region, die hier sr-only ist.
    const held = mistakeCard();
    expect(within(held).getByText("Lösung: die Straße")).toBeTruthy();
    expect(within(held).getByText("die Strasse").tagName).toBe("S");
    expect(toastRegion().classList.contains("sr-only")).toBe(true);
    // Der Fokus steht auf „Weiter" – Enter trägt das Kind weiter, ohne erst zu zielen.
    const weiter = screen.getByRole("button", { name: de.sohn.practice.next });
    expect(document.activeElement).toBe(weiter);

    fireEvent.click(weiter);
    expect(await screen.findByText("zwei")).toBeTruthy();
    expect(screen.queryByText("die Strasse")).toBeNull();
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
