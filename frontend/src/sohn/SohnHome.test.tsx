import { act, fireEvent, render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import "../lib/i18n";
import { DEFAULT_SKIN } from "../lib/skins";
import type { DailyBoxOpened, DailyBoxStatus, PositionStatus } from "../lib/types";

// Die Startseite hängt an Kontext, den der Pfad nur für den Begleiter braucht.
vi.mock("./SohnApp", () => ({
  useSohn: () => ({ planId: 1, setPlanId: () => {}, childId: 7, celebrate: () => {}, skin: DEFAULT_SKIN, refreshWallet }),
}));
vi.mock("../lib/auth", () => ({ useAuth: () => ({ signOut: () => {} }) }));
const refreshWallet = vi.fn();

const { cadenceKey, NeuLadenFehler, Pfad, Truhe, zielZaehler } = await import("./SohnHome");
const { truhenZiele } = await import("./pfad");
const { BonusEinstieg } = await import("./SohnBonus");
const { api, ApiError } = await import("../lib/api");
type TruhenZiele = import("./SohnHome").TruhenZiele;

const position = (over: Partial<PositionStatus>): PositionStatus => ({
  positionId: 1, exerciseId: 1, exerciseTitle: "Wörter 1", exerciseType: "Vocabulary", renderer: "flashcards",
  order: 1, cadence: "Daily", checkMode: "PositionTest", useLeitner: true, testable: true, goalMet: false,
  goalProgress: 0, dueCount: 3, poolSize: 10, pointsGoalMet: 20, introducedCount: 4, imprintedCount: 4, penaltyCoins: 5,
  seriesUnitId: 1, seriesUnitLabel: "Unit 1", ...over,
} as PositionStatus);

// Ein Mittwoch, 21:30 UTC = 22:30 MEZ – bis zum Ende des Lerntags (Mitternacht deutscher Zeit, B-730) zwei angefangene Stunden.
const JETZT = new Date(Date.UTC(2026, 10, 11, 21, 30));
const zeige = (positions: PositionStatus[]) =>
  render(<MemoryRouter><Pfad positions={positions} jetzt={JETZT} /></MemoryRouter>);
const knoten = (titel: string) => screen.getByRole("link", { name: new RegExp(`^${titel}`) });

/*
 * B-525 – der Lernpfad auf der Startseite. Die Regeln selbst prüft `pfad.test.ts`; hier steht, dass der Knoten sie
 * auch zeigt. Je Fall steht dabei, welche Änderung ihn rot macht.
 */
describe("Knoten auf dem Lernpfad", () => {
  it("gibt einer freien Position kein Häkchen, einer erfüllten Pflicht schon", () => {
    // B-467. Rot, wenn der Knoten `goalMet` ungeprüft zeigt – oder wenn das Häkchen ganz wegfällt.
    zeige([
      position({ positionId: 1, exerciseTitle: "Frei geübt", cadence: "None", goalMet: true }),
      position({ positionId: 2, exerciseTitle: "Pflicht erledigt", goalMet: true }),
    ]);
    expect(knoten("Frei geübt").textContent).not.toContain("✓");
    expect(knoten("Pflicht erledigt").textContent).toContain("✓");
    expect(knoten("Pflicht erledigt").getAttribute("aria-label")).toContain("Ziel erfüllt");
  });

  it("nennt eine freie Position Zusatzübung und verspricht keine Münz-Sperre mehr (B-657)", () => {
    // Rot, wenn die Kür-Position wieder „ohne Münzen“ ansagt – seit B-657 zahlt sie wie eine Pflicht.
    zeige([
      position({ positionId: 1, exerciseTitle: "Frei geübt", cadence: "None" }),
      position({ positionId: 2, exerciseTitle: "Pflicht", cadence: "Daily" }),
    ]);
    expect(knoten("Frei geübt").getAttribute("aria-label")).toContain("Zusatzübung");
    expect(knoten("Frei geübt").getAttribute("aria-label")).not.toContain("ohne Münzen");
    expect(knoten("Pflicht").getAttribute("aria-label")).not.toContain("Zusatzübung");
  });

  it("hebt genau einen Knoten als nächsten hervor und keinen, wenn die Pflicht erfüllt ist", () => {
    // Rot, wenn jeder offene Knoten „START“ trägt – oder wenn nach erfüllter Pflicht noch einer hervorgehoben wird.
    const { unmount } = zeige([
      position({ positionId: 1, exerciseTitle: "Erste" }),
      position({ positionId: 2, exerciseTitle: "Zweite" }),
    ]);
    expect(screen.getAllByText("START")).toHaveLength(1);
    expect(knoten("Erste").className).toContain("naechste");
    expect(knoten("Zweite").className).not.toContain("naechste");
    // Das sichtbare START ist versteckter Schmuck – der Vorlesename muss den nächsten Schritt selbst nennen.
    expect(knoten("Erste").getAttribute("aria-label")).toContain("als Nächstes dran");
    expect(knoten("Erste").getAttribute("aria-current")).toBe("step");
    expect(knoten("Zweite").getAttribute("aria-label")).not.toContain("als Nächstes dran");
    unmount();

    zeige([position({ goalMet: true })]);
    expect(screen.queryByText("START")).toBeNull();
  });

  it("zeigt am offenen Pflicht-Knoten Frist und Malus – und keinen Betrag, wenn es keinen gibt", () => {
    // Rot, wenn der Malus fehlt, oder wenn bei 0 Münzen „sonst −0 Münzen“ dastünde.
    zeige([
      position({ positionId: 1, exerciseTitle: "Mit Malus", penaltyCoins: 5 }),
      position({ positionId: 2, exerciseTitle: "Ohne Malus", penaltyCoins: 0 }),
      position({ positionId: 3, exerciseTitle: "Wochenziel", cadence: "Weekly", penaltyCoins: 1 }),
    ]);
    expect(knoten("Mit Malus").getAttribute("aria-label")).toContain("noch 2 Std., sonst −5 Münzen");
    expect(knoten("Ohne Malus").getAttribute("aria-label")).toContain("noch 2 Std.");
    expect(knoten("Ohne Malus").getAttribute("aria-label")).not.toContain("sonst");
    expect(knoten("Wochenziel").getAttribute("aria-label")).toContain("bis Sonntag, sonst −1 Münze");
    // Sichtbar am Knoten, nicht nur im Vorlesenamen – und dort versteckt, damit es nicht doppelt angesagt wird.
    // Rot, wenn die Zeile unter dem Knoten fehlt.
    const zeile = (titel: string) => knoten(titel).parentElement!.querySelector(".pfad-frist");
    expect(zeile("Mit Malus")!.textContent).toBe("⏰ noch 2 Std. · sonst −5 Münzen");
    expect(zeile("Mit Malus")!.getAttribute("aria-hidden")).toBe("true");
    expect(zeile("Ohne Malus")!.textContent).toBe("⏰ noch 2 Std.");
  });

  it("zeichnet den Stoff-Ring nur, wenn die Position Inhalte hat", () => {
    // Rot, wenn eine Position ohne Pool (Aufsatz) einen Stoff-Ring bekäme. Ihr Ziel-Ring bleibt: die Pflicht hat sie.
    zeige([
      position({ positionId: 1, exerciseTitle: "Mit Pool" }),
      position({ positionId: 2, exerciseTitle: "Aufsatz", poolSize: 0, introducedCount: 0, imprintedCount: 0 }),
      position({ positionId: 3, exerciseTitle: "Freier Aufsatz", cadence: "None", poolSize: 0, introducedCount: 0, imprintedCount: 0 }),
    ]);
    expect(knoten("Mit Pool").querySelector(".pfad-ring-anteil")).not.toBeNull();
    expect(knoten("Aufsatz").querySelector(".pfad-ring-anteil-bahn")).toBeNull();
    expect(knoten("Aufsatz").querySelector(".pfad-ring-ziel-bahn")).not.toBeNull();
    // Ohne Stoff und ohne Pflicht gibt es nichts zu füllen – kein Ring überhaupt.
    expect(knoten("Freier Aufsatz").querySelector(".pfad-ring")).toBeNull();
  });

  it("füllt den inneren Ring mit dem Stand der Pflicht und sagt ihn an", () => {
    // Rot, wenn der innere Ring fehlt, nur goalMet spiegelt (kein Bogen bei 0.5) oder an einer Kür-Position erscheint.
    zeige([
      position({ positionId: 1, exerciseTitle: "Halb", goalProgress: 0.5 }),
      position({ positionId: 2, exerciseTitle: "Offen", goalProgress: 0 }),
      position({ positionId: 3, exerciseTitle: "Kür", cadence: "None", goalMet: true, goalProgress: 1 }),
    ]);
    expect(knoten("Halb").querySelector(".pfad-ring-ziel")).not.toBeNull();
    expect(knoten("Halb").getAttribute("aria-label")).toContain("Ziel zu 50 % geschafft");
    expect(knoten("Offen").querySelector(".pfad-ring-ziel-bahn")).not.toBeNull();
    expect(knoten("Offen").querySelector(".pfad-ring-ziel")).toBeNull();
    expect(knoten("Kür").querySelector(".pfad-ring-ziel-bahn")).toBeNull();
  });

  it("zeichnet bei null kennengelernten Inhalten nur die leere Bahn", () => {
    // Rot, wenn der Bogen auch bei 0 gezeichnet wird: sein rundes Linienende malt dann einen goldenen Punkt, der
    // wie ein erster Fortschritt aussieht (im Rollengang gesehen).
    zeige([position({ exerciseTitle: "Neu", introducedCount: 0, imprintedCount: 0 })]);
    expect(knoten("Neu").querySelector(".pfad-ring")).not.toBeNull();
    expect(knoten("Neu").querySelector(".pfad-ring-anteil")).toBeNull();
  });

  it("gliedert nach der Unit und beginnt ein neues Banner, wo sie wechselt", () => {
    // Rot, wenn alle Positionen unter einem einzigen Banner stünden.
    zeige([
      position({ positionId: 1, seriesUnitId: 2, seriesUnitLabel: "Unit 2 – En ville" }),
      position({ positionId: 2, exerciseTitle: "Wörter 2", seriesUnitId: 3, seriesUnitLabel: "Unit 3 – Au marché" }),
    ]);
    expect(screen.getByRole("region", { name: "Unit 2 – En ville" })).toBeTruthy();
    expect(within(screen.getByRole("region", { name: "Unit 3 – Au marché" })).getByRole("link", { name: /^Wörter 2/ })).toBeTruthy();
  });
});

describe("Der Knoten startet die Übung direkt (B-686)", () => {
  // B-643 AK 6, Review: der Vorlesename des Knotens zählt eingeprägte, nicht gesehene Wörter. Rot, wenn er wieder
  // `introducedCount` liest – dann stünde „10 von 10“ bei einem Ring, der nicht voll ist.
  it("nennt eingeprägte Wörter, nicht gesehene (B-643)", () => {
    zeige([position({ positionId: 17, introducedCount: 10, imprintedCount: 3 })]);
    expect(knoten("Wörter 1").getAttribute("aria-label")).toContain("3 von 10 eingeprägt");
  });

  it("führt ohne Zwischenblatt ins Üben, auch wenn heute ein Test ginge", () => {
    // Rot, wenn der Knoten wieder ein Knopf würde, der ein Blatt öffnet (dann trägt er kein `href`), oder direkt in den
    // Test führte, obwohl Karten fällig sind (B-686: der Test ist am Start kein Abkürzungsweg).
    zeige([position({ positionId: 17 })]);
    expect(knoten("Wörter 1").getAttribute("href")).toBe("/sohn/practice/17");
  });

  it("startet den Test, wenn nichts zu üben ist", () => {
    // Rot, wenn der Knoten in eine leere Übungsrunde führte.
    zeige([position({ positionId: 17, dueCount: 0, introducedCount: 10, imprintedCount: 10 })]);
    expect(knoten("Wörter 1").getAttribute("href")).toBe("/sohn/test/17");
  });

  it("führt an einem nicht prüfbaren Tag ins Üben", () => {
    // Rot, wenn `testable = false` trotzdem in den Test führte – der Test antwortete mit 400 (B-96).
    zeige([position({ positionId: 17, testable: false, dueCount: 0, introducedCount: 10, imprintedCount: 10 })]);
    expect(knoten("Wörter 1").getAttribute("href")).toBe("/sohn/practice/17");
  });
});

const status = (over: Partial<DailyBoxStatus>): DailyBoxStatus => ({
  claimedToday: false, openedToday: false, waiting: 0, coinsAwarded: null, gemsAwarded: null, streakAtClaim: 0, ...over,
});
const HEUTE = "2026-11-14";
const zeigeTruhe = (box: DailyBoxStatus,
  over: { ziele?: TruhenZiele; freigeschaltet?: boolean; onVeraltet?: () => void } = {}) =>
  render(<Truhe box={box} heute={HEUTE} ziele={over.ziele ?? "tag"} freigeschaltet={over.freigeschaltet ?? false}
    onVeraltet={over.onVeraltet ?? (() => {})} />);
const truhe = () => screen.getByRole("region", { name: "Tagestruhe" });
const geoeffnet = (over: Partial<DailyBoxOpened>): DailyBoxOpened =>
  ({ coins: 25, gems: 1, streakAtClaim: 2, day: HEUTE, waitingAfter: 0, ...over });

/*
 * B-527 – die Tagestruhe am Ende des Pfads, AK 8 und die Review-Funde. Je Fall steht, welche Änderung ihn rot macht.
 */
describe("Die Tagestruhe", () => {
  afterEach(() => { vi.restoreAllMocks(); refreshWallet.mockClear(); });

  it("ist zu, solange nichts wartet: nur ein Hinweis, kein Knopf", () => {
    // Rot, wenn die Truhe im Zustand *zu* einen Knopf hat (das Kind tippte ins Leere: 409 daily_box_none).
    zeigeTruhe(status({}));
    expect(within(truhe()).queryByRole("button")).toBeNull();
    expect(truhe().textContent).toContain("Schaffe deine Tagesziele");
    expect(truhe().className).toContain("zu");
  });

  it("nennt im Zustand zu die Ziele, die der Plan wirklich hat", () => {
    // Review-Fund 9. Rot, wenn ein Plan nur mit Wochenzielen oder ganz ohne Pflicht „Schaffe deine Tagesziele“ läse –
    // ein Ziel, das es dort nicht gibt.
    const { unmount } = zeigeTruhe(status({}), { ziele: "woche" });
    expect(truhe().textContent).toContain("Schaffe deine Wochenziele");
    expect(truhe().textContent).not.toContain("Tagesziele");
    unmount();
    zeigeTruhe(status({}), { ziele: "keine" });
    expect(truhe().textContent).toContain("keine Pflicht");
    expect(truhe().textContent).not.toContain("Tagesziele");
  });

  it("leuchtet bereit, wenn eine wartet, nennt mehrere – und verrät den Inhalt nicht", () => {
    // Rot, wenn eine wartende Truhe keinen Knopf bekäme oder die Zahl bei mehreren fehlte.
    zeigeTruhe(status({ claimedToday: true, waiting: 2 }));
    expect(truhe().className).toContain("bereit");
    expect(within(truhe()).getByRole("button", { name: "Truhe öffnen" })).toBeTruthy();
    expect(truhe().textContent).toContain("2 Truhen warten");
    expect(truhe().textContent).not.toContain("Münzen");
  });

  it("zeigt offen den Inhalt der heute geöffneten Truhe", () => {
    // Rot, wenn die Truhe nach dem Neuladen (Server: openedToday) wieder zu stünde oder den Inhalt verschwiege.
    zeigeTruhe(status({ claimedToday: true, openedToday: true, coinsAwarded: 20, gemsAwarded: 2, streakAtClaim: 1 }));
    expect(truhe().className).toContain("offen");
    expect(within(truhe()).queryByRole("button")).toBeNull();
    expect(screen.getByRole("status").textContent).toBe("+20 Münzen, +2 Gems");
    expect(truhe().textContent).toContain("Heute geöffnet");
  });

  it("lässt „+0 Gems“ weg", () => {
    // Review-Fund 10. Rot, wenn bei null Gems „+0 Gems“ dastünde.
    zeigeTruhe(status({ claimedToday: true, openedToday: true, coinsAwarded: 20, gemsAwarded: 0 }));
    expect(screen.getByRole("status").textContent).toBe("+20 Münzen");
  });

  it("öffnet beim Antippen genau einmal, lädt den Saldo neu, zeigt den Inhalt und hält den Fokus auf der Truhe", async () => {
    // Rot, wenn ein Doppeltipp zwei POSTs schickt (Sperre in `useAction` fehlt) – beide Klicks in einem `act`, sonst
    // fängt schon `disabled` den zweiten (B-325) –, wenn der Knopf während des Öffnens nicht gesperrt ist, wenn der
    // Saldo nicht neu geladen wird oder der Inhalt danach fehlt. Review-Fund 7: rot, wenn der Fokus mit dem
    // verschwundenen Knopf auf `body` fällt statt auf der Truhe zu landen.
    let freigeben: (v: DailyBoxOpened) => void = () => {};
    const open = vi.spyOn(api, "openDailyBox").mockImplementation(
      () => new Promise<DailyBoxOpened>((resolve) => { freigeben = resolve; }));
    zeigeTruhe(status({ claimedToday: true, waiting: 1 }));
    const knopf = screen.getByRole("button", { name: "Truhe öffnen" });
    knopf.focus();

    act(() => { fireEvent.click(knopf); fireEvent.click(knopf); });
    expect(open).toHaveBeenCalledTimes(1);
    expect(open).toHaveBeenCalledWith(7);
    expect((knopf as HTMLButtonElement).disabled).toBe(true);

    await act(async () => { freigeben(geoeffnet({})); });
    expect(refreshWallet).toHaveBeenCalledTimes(1);
    expect(screen.getByRole("status").textContent).toBe("+25 Münzen, +1 Gems");
    expect(truhe().className).toContain("offen");
    expect(screen.queryByRole("button", { name: "Truhe öffnen" })).toBeNull();
    expect(document.activeElement).toBe(truhe());
  });

  it("zeigt eine ältere geöffnete Truhe mit Inhalt, aber nicht als „heute geöffnet“", async () => {
    // Review-Fund 8. Rot, wenn die Truhe von gestern als „Heute geöffnet“ dastünde, obwohl die heutige noch nicht
    // verdient ist.
    vi.spyOn(api, "openDailyBox").mockResolvedValue(geoeffnet({ day: "2026-11-13", coins: 11, waitingAfter: 0 }));
    zeigeTruhe(status({ waiting: 1 }));
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "Truhe öffnen" })); });
    expect(screen.getByRole("status").textContent).toBe("+11 Münzen, +1 Gems");
    expect(truhe().className).toContain("zu");
    expect(truhe().textContent).not.toContain("Heute geöffnet");
  });

  it("bleibt bereit, solange nach dem Öffnen noch eine wartet", async () => {
    // Review-Fund 8. Rot, wenn das Öffnen der ältesten Truhe die Truhe schlösse, obwohl `waitingAfter` noch eine nennt.
    vi.spyOn(api, "openDailyBox").mockResolvedValue(geoeffnet({ day: "2026-11-13", coins: 11, waitingAfter: 1 }));
    zeigeTruhe(status({ claimedToday: true, waiting: 2 }));
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "Truhe öffnen" })); });
    expect(truhe().className).toContain("bereit");
    expect(screen.getByRole("status").textContent).toBe("+11 Münzen, +1 Gems");
    expect(screen.getByRole("button", { name: "Truhe öffnen" })).toBeTruthy();
  });

  it("hört nach daily_box_none auf zu leuchten und holt den Stand neu", async () => {
    // Review-Fund 6. Rot, wenn die Truhe nach dem 409 (Doppeltipp, zweiter Tab) weiter bereit stünde und zum nächsten
    // Fehlversuch einlüde, oder wenn die Übersicht nicht neu geladen wird.
    vi.spyOn(api, "openDailyBox").mockRejectedValue(new ApiError(409, "No reward chest waiting.", undefined, "daily_box_none"));
    const onVeraltet = vi.fn();
    zeigeTruhe(status({ claimedToday: true, waiting: 1 }), { onVeraltet });
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "Truhe öffnen" })); });
    expect(onVeraltet).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole("button", { name: "Truhe öffnen" })).toBeNull();
    expect(truhe().className).not.toContain("bereit");
    expect(screen.getByRole("alert").textContent).toContain("Gerade wartet keine Truhe");
  });
});

/*
 * Gesamt-Review des Laufs: Truhe und Bonus lesen „erfüllt“ am Server (`rewardsUnlocked`) und zählen dieselben Ziele wie
 * er (`RewardObligations`). Je Fall steht, welche Änderung ihn rot macht.
 */
describe("Belohnungen – dieselben Ziele wie der Server", () => {
  // Rot, wenn die Truhe bei erfüllten Zielen wieder „Schaffe deine Tagesziele“ sagt (`freigeschaltet` nicht gelesen).
  it("sagt bei erfüllten Zielen ohne verdiente Truhe, wann sie kommt", () => {
    zeigeTruhe(status({}), { freigeschaltet: true });
    expect(truhe().textContent).toContain("die Truhe kommt, sobald du eine Runde oder einen Test abschließt");
    expect(truhe().textContent).not.toContain("Schaffe deine");
    expect(within(truhe()).queryByRole("button")).toBeNull();
  });

  // B-612: welche Ziele zählen, wählt der Server (die Auswahl-Fälle stehen in `BelohnungUeberPlaeneTests`); hier nur
  // der Wortlaut zur gelieferten Art. Rot, wenn eine Art auf den falschen Text fällt.
  it("bildet die Zielart des Servers auf den Wortlaut ab", () => {
    expect(truhenZiele("Daily")).toBe("tag");
    expect(truhenZiele("Weekly")).toBe("woche");
    expect(truhenZiele("None")).toBe("keine");
    // B-636: die Nacharbeit ist eine Tagespflicht – rot, wenn sie auf „keine Pflicht“ fiele.
    expect(truhenZiele("UntilDone")).toBe("tag");
  });

  // B-636: rot, wenn die Nacharbeit ohne eigenen Zweig als „Zusatzübung“ auf dem Pfad steht – sie kostet Malus.
  it("nennt die Nacharbeit „bis erledigt“ beim Namen", () => {
    expect(cadenceKey("UntilDone")).toBe("home.cadenceUntilDone");
    expect(cadenceKey("None")).toBe("home.cadenceNone");
  });

  // B-636: rot, wenn „Ziele heute“ nur Tagesziele zählt – die offene Nacharbeit fehlte dann im Zähler.
  it("zählt die Nacharbeit „bis erledigt“ zu den Zielen von heute", () => {
    const { tag, woche } = zielZaehler([
      position({ positionId: 1, goalMet: true }), position({ positionId: 2, cadence: "UntilDone" }),
      position({ positionId: 3, cadence: "Weekly" }), position({ positionId: 4, cadence: "None" }),
    ]);
    expect(tag).toEqual({ met: 1, total: 2 });
    expect(woche).toEqual({ met: 0, total: 1 });
  });

  // Rot, wenn der Bonus-Einstieg bei einem Plan nur mit Wochenzielen „Erst die Tagesziele“ verlangt.
  it("nennt im Bonus-Einstieg die Wochenziele, wo es nur sie gibt", () => {
    const { unmount } = render(<MemoryRouter><BonusEinstieg offen={false} ziele="woche" /></MemoryRouter>);
    expect(screen.getByText("🔒 Erst die Wochenziele – dann öffnet das Bonus-Training.")).toBeTruthy();
    unmount();
    render(<MemoryRouter><BonusEinstieg offen={false} ziele="tag" /></MemoryRouter>);
    expect(screen.getByText("🔒 Erst die Tagesziele – dann öffnet das Bonus-Training.")).toBeTruthy();
  });

  // Rot, wenn ein gescheitertes Neuladen unsichtbar bleibt oder der Kasten beim Wiederholen aushängt (B-216).
  it("zeigt ein gescheitertes Neuladen mit Wiederholen-Knopf", () => {
    const onRetry = vi.fn();
    const { rerender } = render(<NeuLadenFehler fehler="Netz weg" laedt={false} onRetry={onRetry} />);
    expect(screen.getByRole("alert").textContent).toContain("Neu laden hat nicht geklappt: Netz weg");
    fireEvent.click(screen.getByRole("button", { name: "Nochmal versuchen" }));
    expect(onRetry).toHaveBeenCalledTimes(1);
    rerender(<NeuLadenFehler fehler={null} laedt onRetry={onRetry} />);
    expect(screen.getByRole("alert").textContent).toContain("Netz weg");
    rerender(<NeuLadenFehler fehler={null} laedt={false} onRetry={onRetry} />);
    expect(screen.getByRole("alert").textContent).toBe("");
  });
});
