import { StrictMode } from "react";
import { act, fireEvent, render, screen } from "@testing-library/react";
import { Link, MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "../lib/api";
import { setLanguage } from "../lib/i18n";
import { ArcadeScope } from "../lib/chromeText";
import type { TestItem } from "../lib/types";

/**
 * B-320: scheitert das Beantworten einer Klausurfrage, sieht das Kind die Meldung. Vorher las der Bildschirm sie aus
 * `action.message` des Renders, in dem der Klick entstand – beim ersten Fehlschlag `null`, also nichts.
 */
const startTest = vi.fn();
const nextTest = vi.fn();
const answerTest = vi.fn();
const submitTest = vi.fn();
const celebrate = vi.fn();
const setStreak = vi.fn();
const refreshWallet = vi.fn();

// Ein echter Skin: der Ergebnis-Bildschirm zeichnet das Maskottchen (der Neustart-Fall erreicht ihn).
const { SKINS } = await import("../lib/skins");
vi.mock("./SohnApp", () => ({
  useSohn: () => ({ planId: 3, refreshWallet: () => refreshWallet(), setStreak: (...a: unknown[]) => setStreak(...a), skin: SKINS[0], celebrate: (...a: unknown[]) => celebrate(...a) }),
}));
vi.mock("../lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("../lib/api")>();
  return {
    ...original,
    api: {
      startTest: (...a: unknown[]) => startTest(...a),
      nextTest: (...a: unknown[]) => nextTest(...a),
      answerTest: (...a: unknown[]) => answerTest(...a),
      submitTest: (...a: unknown[]) => submitTest(...a),
      overview: () => Promise.resolve({ currentStreak: 4 }),
    },
  };
});

const { SohnTest } = await import("./SohnTest");

const item: TestItem = { itemIndex: 0, prompt: "the horse", stage: 3, reveal: null, choices: ["das Pferd", "das Schaf"] };

beforeEach(() => {
  startTest.mockReset().mockResolvedValue({ attemptId: 9, stage: 3, totalItems: 2 });
  nextTest.mockReset().mockResolvedValue({ item, done: false, cursor: 0, total: 2 });
  answerTest.mockReset();
});

function renderTest() {
  return render(
    <MemoryRouter initialEntries={["/sohn/test/5"]}>
      <Routes><Route path="/sohn/test/:positionId" element={<SohnTest />} /></Routes>
    </MemoryRouter>,
  );
}

describe("SohnTest, Stufe je Frage (B-476)", () => {
  afterEach(async () => { await setLanguage("de"); });

  // Der Test heißt „Tippen“, diese Frage kommt aber als Auswahl. Rot, wenn das Etikett die Stufe des Versuchs zeigt.
  it("benennt die Stufe der Frage, nicht die des Versuchs", async () => {
    await setLanguage("de");
    startTest.mockResolvedValue({ attemptId: 9, stage: 4, totalItems: 2 });
    nextTest.mockResolvedValue({ item: { ...item, stage: 6 }, done: false, cursor: 0, total: 2 });
    renderTest();

    expect(await screen.findByText("Auswahl")).toBeTruthy();
    expect(screen.queryByText("Tippen")).toBeNull();
  });
});

describe("SohnTest, Fehler beim Beantworten", () => {
  afterEach(async () => { await setLanguage("de"); });

  // Rot, wenn der Fehler wieder aus `action.message` des Klick-Renders gelesen wird: beim ersten Fehlschlag ist der
  // `null`, und der Bildschirm bleibt bei der Frage stehen.
  it("zeigt den ersten Fehlschlag", async () => {
    answerTest.mockRejectedValueOnce(new ApiError(500, "Server kaputt.", "trace-1"));
    renderTest();

    fireEvent.click(await screen.findByRole("button", { name: "das Pferd" }));

    expect(await screen.findByText("Server kaputt. (Ref: trace-1)")).toBeTruthy();
    // Gegenstück zum Fall darunter: ein vorübergehender Fehler lässt sich fortsetzen.
    expect(screen.getByRole("button", { name: "Weiter versuchen" })).toBeTruthy();
  });

  // B-330: ist der Plan nicht mehr dran, holt „Weiter versuchen“ nur dieselbe 403 – der Knopf wäre eine Falle. Rot, wenn er
  // auch hier steht.
  // Derselbe Fall beim Neustart aus dem Ergebnis: dort steht noch die Id des abgegebenen Versuchs. Rot, wenn `start()` sie
  // stehen lässt – dann böte „Weiter versuchen“ den alten Versuch an und holte nur wieder die 403 (Delta-Review B-330).
  it("bietet nach einem Neustart, den der Plan nicht mehr erlaubt, kein Weiter an", async () => {
    await setLanguage("de");
    nextTest.mockReset().mockResolvedValueOnce({ item: null, done: true, cursor: 0, total: 0 });
    submitTest.mockResolvedValueOnce({ passed: false, scorePercent: 0, passPercent: 80, correctItems: 0, totalItems: 0, stage: 3, items: [], attemptsRemaining: 1 });
    renderTest();

    const retry = await screen.findByRole("button", { name: "Nochmal versuchen" });
    startTest.mockRejectedValueOnce(new ApiError(403, "This study plan is not currently active. Ask your parent.", "trace-7", "plan_inactive"));
    fireEvent.click(retry);

    expect(await screen.findByText("Dieser Lernplan ist gerade nicht dran. Frag deine Eltern, welcher Plan jetzt gilt.")).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Weiter versuchen" })).toBeNull();
  });

  // Und nach einem Fortsetzen: erst ein vorübergehender Fehler, dann ist der Plan nicht mehr dran. Rot, wenn `resume()` den
  // Code nicht merkt – dann stünde der Knopf weiter da.
  it("nimmt das Weiter weg, wenn das Fortsetzen auf einen Plan trifft, der nicht mehr dran ist", async () => {
    await setLanguage("de");
    answerTest.mockRejectedValueOnce(new ApiError(500, "Server kaputt.", "trace-1"));
    renderTest();
    fireEvent.click(await screen.findByRole("button", { name: "das Pferd" }));
    const resume = await screen.findByRole("button", { name: "Weiter versuchen" });

    nextTest.mockRejectedValueOnce(new ApiError(403, "This study plan is not currently active. Ask your parent.", "trace-8", "plan_inactive"));
    fireEvent.click(resume);

    expect(await screen.findByText("Dieser Lernplan ist gerade nicht dran. Frag deine Eltern, welcher Plan jetzt gilt.")).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Weiter versuchen" })).toBeNull();
  });

  // B-335: der Start gelingt (die Versuchs-Id steht), aber schon die erste Frage trifft auf einen Plan, der nicht mehr dran
  // ist. Rot, wenn `start()` den Code nicht merkt – dann böte „Weiter versuchen“ den Versuch an und holte nur dieselbe 403.
  it("bietet kein Weiter an, wenn schon die erste Frage auf einen Plan trifft, der nicht mehr dran ist", async () => {
    await setLanguage("de");
    nextTest.mockReset().mockRejectedValueOnce(new ApiError(403, "This study plan is not currently active. Ask your parent.", "trace-9", "plan_inactive"));
    renderTest();

    expect(await screen.findByText("Dieser Lernplan ist gerade nicht dran. Frag deine Eltern, welcher Plan jetzt gilt.")).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Weiter versuchen" })).toBeNull();
  });

  // Gegenstück zu B-335: ein vorübergehender Fehler an derselben Stelle lässt sich fortsetzen – der Versuch läuft ja schon.
  // Rot, wenn `start()` jeden Fehler als „Plan steht“ wertet.
  it("bietet Weiter an, wenn die erste Frage vorübergehend scheitert", async () => {
    await setLanguage("de");
    nextTest.mockReset().mockRejectedValueOnce(new ApiError(500, "Server kaputt.", "trace-10"))
      .mockResolvedValueOnce({ item, done: false, cursor: 0, total: 2 });
    renderTest();

    fireEvent.click(await screen.findByRole("button", { name: "Weiter versuchen" }));

    expect(await screen.findByRole("button", { name: "das Pferd" })).toBeTruthy();
  });

  it("bietet bei einem Plan, der nicht mehr dran ist, kein Weiter an", async () => {
    await setLanguage("de");
    answerTest.mockRejectedValueOnce(new ApiError(403, "This study plan is not currently active. Ask your parent.", "trace-6", "plan_inactive"));
    renderTest();

    fireEvent.click(await screen.findByRole("button", { name: "das Pferd" }));

    expect(await screen.findByText("Dieser Lernplan ist gerade nicht dran. Frag deine Eltern, welcher Plan jetzt gilt.")).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Weiter versuchen" })).toBeNull();
  });

  // Rot, wenn der Fang den deutschen `errorMessage` statt des Arcade-Formatierers nimmt: dann stünde der deutsche Satz in
  // der englischen Arcade. Der Fall oben sieht das nicht – für einen Fehler ohne Code sagen beide dasselbe.
  it("meldet in der englischen Arcade den englischen Satz", async () => {
    await setLanguage("en");
    answerTest.mockRejectedValueOnce(new ApiError(429, "No attempts left.", "trace-2", "test_attempts_exhausted"));
    renderTest();

    fireEvent.click(await screen.findByRole("button", { name: "das Pferd" }));

    expect(await screen.findByText("You've used up your tries for today. You can have another go tomorrow.")).toBeTruthy();
  });

  // Rot, wenn das Ergebnis der Antwort nicht mehr den Zähler der Aufgabe weiterrückt – Kontrolle, dass der Erfolgsweg
  // durch den Umbau des Fehlerwegs nicht mitgerissen wird (die Antwort lief vorher wie nachher in `action.run`).
  it("rückt nach einer angenommenen Antwort zur nächsten Frage", async () => {
    answerTest.mockResolvedValueOnce({ done: false });
    nextTest.mockResolvedValueOnce({ item, done: false, cursor: 0, total: 2 })
      .mockResolvedValueOnce({ item: { ...item, itemIndex: 1, prompt: "the sheep" }, done: false, cursor: 1, total: 2 });
    renderTest();

    fireEvent.click(await screen.findByRole("button", { name: "das Pferd" }));

    expect(await screen.findByText("the sheep")).toBeTruthy();
  });

  // B-286: die Frage wechselt, der Fokus folgt ihr nicht – die neue Aufgabe kommt über die Live-Region, und zwar über
  // DIESELBE (B-134), auch nachdem ein Fehler die Karte kurz ersetzt hat. Rot, wenn die Region in der Karte sitzt (dann
  // entstünde sie nach „Weiter versuchen" neu, zusammen mit ihrem Text, und bliebe stumm), wenn sie fehlt, oder wenn
  // sie im Fehlerbild die alte Frage stehen lässt.
  it("sagt die fortgesetzte Frage in derselben Region an", async () => {
    await setLanguage("de");
    answerTest.mockRejectedValueOnce(new ApiError(500, "Server kaputt.", "trace-1"));
    // Wie der Server antwortet: die Antwort kam nicht an, der Cursor steht noch auf „the horse“ – dieselbe Frage kommt
    // wieder. Sie wird erneut angesagt, weil die Region dazwischen leer war (sie ist ja wieder dran).
    nextTest.mockResolvedValueOnce({ item, done: false, cursor: 0, total: 2 })
      .mockResolvedValueOnce({ item, done: false, cursor: 0, total: 2 });
    renderTest();
    fireEvent.click(await screen.findByRole("button", { name: "das Pferd" }));
    const resume = await screen.findByRole("button", { name: "Weiter versuchen" });
    const region = taskRegion();
    expect(region.textContent).toBe("");

    fireEvent.click(resume);
    // B-339: Bis die nächste Frage da ist, wird nichts angesagt, und die alte Karte ist nicht mehr anklickbar. Rot, wenn
    // `resume` die alte Frage beim Nachladen stehen lässt. (Hier scheiterte die Antwort; den häufigeren Fall – Antwort
    // angenommen, nur die nächste Frage scheitert – fährt der Fall darunter.)
    expect(region.textContent).toBe("");
    expect(screen.queryByRole("button", { name: "das Pferd" })).toBeNull();

    expect(await screen.findByText("Neue Aufgabe: the horse")).toBe(region);
  });

  // B-339, der Fall, für den „Weiter versuchen" vor allem da ist: die Antwort ist angenommen, nur das Holen der nächsten
  // Frage scheitert. Das beantwortete „the horse" darf danach nicht noch einmal als neue Aufgabe kommen. Rot, wenn `resume`
  // die alte Frage beim Nachladen stehen lässt – dieselbe Mutante wie der Fall darüber, keine eigene; dieser hier fährt den
  // Weg, für den die Behebung vor allem da ist.
  it("sagt nach angenommener Antwort und gescheiterter nächster Frage die alte nicht erneut an", async () => {
    await setLanguage("de");
    answerTest.mockResolvedValueOnce({ done: false });
    nextTest.mockResolvedValueOnce({ item, done: false, cursor: 0, total: 2 })
      .mockRejectedValueOnce(new ApiError(500, "Server kaputt.", "trace-2"))
      .mockResolvedValueOnce({ item: { ...item, itemIndex: 1, prompt: "the sheep" }, done: false, cursor: 1, total: 2 });
    renderTest();
    fireEvent.click(await screen.findByRole("button", { name: "das Pferd" }));
    const resume = await screen.findByRole("button", { name: "Weiter versuchen" });
    const region = taskRegion();

    fireEvent.click(resume);
    expect(region.textContent).toBe("");
    expect(screen.queryByRole("button", { name: "das Pferd" })).toBeNull();

    expect(await screen.findByText("Neue Aufgabe: the sheep")).toBe(region);
  });

  // Die englische Arcade sagt englisch an. Hält nur „immer Deutsch“ fern; ob `useChromeT` oder die globale Sprache
  // antwortet, trennt der Fall nicht – in der Arcade ist beides dieselbe.
  it("sagt die Aufgabe in der englischen Arcade englisch an", async () => {
    await setLanguage("en");
    render(
      <ArcadeScope>
        <MemoryRouter initialEntries={["/sohn/test/5"]}>
          <Routes><Route path="/sohn/test/:positionId" element={<SohnTest />} /></Routes>
        </MemoryRouter>
      </ArcadeScope>,
    );

    expect(await screen.findByText("New task: the horse")).toBe(taskRegion());
  });

  // B-325 (AK 3 von B-320). Rot, wenn `answerAndAdvance` die Sperre verliert (`action.run(fn)` → `fn()`) oder die
  // Ref-Prüfung in `useAction.run` fehlt: dann schickt ein Doppelklick zwei Antworten auf dieselbe Cursor-Frage. Die
  // Antwort hängt, bis beide Klicks durch sind.
  it("schickt bei einem Doppelklick eine Antwort und zeigt nichts", async () => {
    let ack!: (v: { done: boolean }) => void;
    answerTest.mockReturnValueOnce(new Promise((r) => { ack = r; }));
    nextTest.mockResolvedValueOnce({ item, done: false, cursor: 0, total: 2 })
      .mockResolvedValueOnce({ item: { ...item, itemIndex: 1, prompt: "the sheep" }, done: false, cursor: 1, total: 2 });
    renderTest();

    const button = await screen.findByRole("button", { name: "das Pferd" });
    // Beide Klicks in EINEM `act`: dazwischen rendert React nicht, `disabled={action.busy}` steht also noch nicht am Knopf –
    // nur so trifft der zweite Klick die Ref-Sperre selbst (gemessen: mit zwei einzelnen `fireEvent` fing `disabled` ihn ab,
    // und die Mutante „Ref-Prüfung weg“ blieb grün).
    act(() => { fireEvent.click(button); fireEvent.click(button); });
    await act(async () => { ack({ done: false }); });

    expect(await screen.findByText("the sheep")).toBeTruthy();
    expect(answerTest).toHaveBeenCalledTimes(1);
    // Fällt nur als Nebenwirkung der Mutante (die zweite, ungemockte Antwort wirft) – kein eigener Beleg für „zeigt nichts“.
    expect(document.querySelector(".error-box")).toBeNull();
  });
});

/** B-326: nach einem Reload mit einer Plan-Id, die es nicht mehr gibt, geht es zur Basis – die korrigiert die Wahl. */
describe("SohnTest mit einem Plan, den es nicht mehr gibt", () => {
  // Rot, wenn der Start-Fehler wieder als Meldung steht: dann liest das Kind „Study plan not found. (Ref: …)“.
  it("führt zur Basis statt die Serverzeile zu zeigen", async () => {
    startTest.mockReset().mockRejectedValue(new ApiError(404, "Study plan not found.", "trace-9", "not_found"));
    render(
      <MemoryRouter initialEntries={["/sohn/test/5"]}>
        <Routes>
          <Route path="/sohn/test/:positionId" element={<SohnTest />} />
          <Route path="/sohn" element={<p>BASIS</p>} />
        </Routes>
      </MemoryRouter>,
    );

    expect(await screen.findByText("BASIS")).toBeTruthy();
    // Absichtserklärung, kein eigener Beleg: ist die Basis da, ist der Bildschirm schon weg – rot macht ihn nur, was die
    // Zeile davor schon rot macht.
    expect(screen.queryByText(/Study plan not found/)).toBeNull();
  });

  // Rot, wenn jeder Start-Fehler zur Basis führt: ein Serverfehler ist keine verschwundene Klausur, das Kind braucht die
  // Meldung (B-320) – nur 404 heißt „gibt es nicht mehr“.
  it("zeigt einen anderen Start-Fehler weiter an", async () => {
    startTest.mockReset().mockRejectedValue(new ApiError(500, "Server kaputt.", "trace-8"));
    renderTest();

    expect(await screen.findByText("Server kaputt. (Ref: trace-8)")).toBeTruthy();
  });
});

describe("SohnTest im Wartebild", () => {
  // B-350: hängt der Start, steht das Kind im Bild „wird vorbereitet“. Rot, wenn das Bild wieder nur den Kasten zeigt:
  // dann gibt es keinen Knopf, und die Basis ist nur über den Browser erreichbar.
  it("bietet den Weg zur Basis an, solange die Frage noch nicht da ist", async () => {
    startTest.mockReset().mockReturnValue(new Promise(() => {}));
    render(
      <MemoryRouter initialEntries={["/sohn/test/5"]}>
        <Routes>
          <Route path="/sohn/test/:positionId" element={<SohnTest />} />
          <Route path="/sohn" element={<p>BASIS</p>} />
        </Routes>
      </MemoryRouter>,
    );
    expect(screen.getByText("Test wird vorbereitet…")).toBeTruthy();

    fireEvent.click(screen.getByRole("button", { name: "Zur Basis" }));

    expect(await screen.findByText("BASIS")).toBeTruthy();
  });

  // Review zu B-350: wer das Wartebild verlässt, hat die Klausur verlassen. Antwortet der hängende Start später, darf er
  // das Kind nicht von einer anderen Seite zurückholen (404 → Basis) und nicht dort feiern (fertig → Ergebnis). Rot, wenn
  // der Start nach dem Verlassen weiterwirkt: dann steht wieder BASIS statt SHOP bzw. `celebrate` wurde gerufen.
  // Der fertige Versuch wird trotzdem abgegeben, und die Kopfzeile zeigt danach seine Serie (B-356): rot, wenn `finish`
  // nach dem Verlassen nur eines von Guthaben und Serie auffrischt – oder wenn die Abgabe gar nicht erreicht wird und der Fall leer bleibt.
  it.each([
    ["ein 404", (s: Deferred) => s.reject(new ApiError(404, "Study plan not found.", "t", "not_found")), false],
    ["ein fertiger Versuch", (s: Deferred) => s.resolve({ attemptId: 9, stage: 3, totalItems: 0 }), true],
  ])("lässt das Kind nach dem Verlassen in Ruhe, wenn der Start spät mit %s antwortet", async (_, antwort, abgegeben) => {
    const start = deferred();
    startTest.mockReset().mockReturnValue(start.promise);
    nextTest.mockReset().mockResolvedValue({ item: null, done: true, cursor: 0, total: 0 });
    submitTest.mockReset().mockResolvedValue({ passed: true, scorePercent: 100, passPercent: 80, correctItems: 0, totalItems: 0, stage: 3, items: [], attemptsRemaining: 1 });
    celebrate.mockReset();
    setStreak.mockReset();
    refreshWallet.mockReset();
    render(
      <MemoryRouter initialEntries={["/sohn/test/5"]}>
        <Routes>
          <Route path="/sohn/test/:positionId" element={<SohnTest />} />
          <Route path="/sohn" element={<Link to="/sohn/shop">Laden</Link>} />
          <Route path="/sohn/shop" element={<p>SHOP</p>} />
        </Routes>
      </MemoryRouter>,
    );
    fireEvent.click(screen.getByRole("button", { name: "Zur Basis" }));
    fireEvent.click(await screen.findByRole("link", { name: "Laden" }));

    await act(async () => { antwort(start); await start.promise.catch(() => {}); await Promise.resolve(); });

    expect(screen.getByText("SHOP")).toBeTruthy();
    expect(celebrate).not.toHaveBeenCalled();
    expect(submitTest).toHaveBeenCalledTimes(abgegeben ? 1 : 0);
    expect(setStreak.mock.calls).toEqual(abgegeben ? [[4]] : []);
    // B-360: die Guthaben-Hälfte derselben Entscheidung – rot, wenn der Verlassen-Zweig die Münzen nicht mehr auffrischt.
    expect(refreshWallet).toHaveBeenCalledTimes(abgegeben ? 1 : 0);
  });
});

describe("SohnTest unter StrictMode", () => {
  // Im Dev hängt StrictMode einmal aus und wieder ein. Rot, wenn das Verlassen-Merkmal beim Wiedereinhängen nicht
  // zurückgesetzt wird: dann gilt die Klausur als verlassen, und der 404 führt nicht mehr zur Basis.
  it("führt nach dem Doppel-Einhängen bei einem 404 weiter zur Basis", async () => {
    startTest.mockReset().mockRejectedValue(new ApiError(404, "Study plan not found.", "trace-9", "not_found"));
    render(
      <StrictMode>
        <MemoryRouter initialEntries={["/sohn/test/5"]}>
          <Routes>
            <Route path="/sohn/test/:positionId" element={<SohnTest />} />
            <Route path="/sohn" element={<p>BASIS</p>} />
          </Routes>
        </MemoryRouter>
      </StrictMode>,
    );

    expect(await screen.findByText("BASIS")).toBeTruthy();
  });
});

type Deferred = { promise: Promise<unknown>; resolve: (v: unknown) => void; reject: (e: unknown) => void };
function deferred(): Deferred {
  let resolve!: (v: unknown) => void, reject!: (e: unknown) => void;
  const promise = new Promise<unknown>((res, rej) => { resolve = res; reject = rej; });
  return { promise, resolve, reject };
}

/** Die Ansage-Region der Aufgabe (das `sr-only`-Absatz-Element). */
function taskRegion(): HTMLElement {
  const region = screen.getAllByRole("status").find((el) => el.tagName === "P" && el.classList.contains("sr-only"));
  if (!region) throw new Error("keine Ansage-Region");
  return region;
}
