import { act, fireEvent, render, renderHook, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import { AuthProvider } from "../lib/auth";
import type { ActivationRequest, ChildrenDashboard, ChildrenOverview, FeedbackEntry } from "../lib/types";
import { offeneJeKind, VaterDashboard } from "./VaterDashboard";
import { useOffeneEinloesungenZahl } from "./einloesungen";

/*
 * B-749: die Betreuen-Startseite zeigt je Kind Antwortzahl, Tagesziel und Punkte, darunter die offenen Einlöse-Wünsche
 * aller Kinder – und keine Verwaltung mehr. Gefälscht werden die Abrufe der Seite, nicht das `fetch`.
 */
const tag = {
  date: "2026-11-20",
  children: [
    { childId: 1, name: "Friedrich", activePlans: 2, goalsTotal: 4, goalsMet: 3, pointsToday: 40, dutyDone: false, practiced: true, answersToday: 37 },
    { childId: 3, name: "Theodor", activePlans: 1, goalsTotal: 2, goalsMet: 2, pointsToday: 25, dutyDone: true, practiced: true, answersToday: 1 },
  ],
} as unknown as ChildrenDashboard;

const anfrage = (id: number, childId: number, articleTitle: string) => ({
  id, childId, articleTitle, requestedQuantity: 30, unitType: "Minute", status: "Pending",
  requestedAt: "2026-11-20T07:00:00Z", canApprove: true, canReject: true,
}) as unknown as ActivationRequest;

beforeEach(() => {
  localStorage.setItem("pugling.session", JSON.stringify({
    token: "t", role: "Supervisor", id: 1, name: "Papa", expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
  }));
  vi.stubGlobal("confirm", () => true);
  vi.spyOn(api, "childrenDaily").mockResolvedValue(tag);
  vi.spyOn(api, "childrenOverview").mockResolvedValue({ children: [] } as unknown as ChildrenOverview);
  vi.spyOn(api, "listFeedback").mockResolvedValue([]);
});
afterEach(() => { vi.restoreAllMocks(); vi.unstubAllGlobals(); localStorage.removeItem("pugling.session"); });

const zeige = async () => {
  render(<AuthProvider><MemoryRouter><VaterDashboard /></MemoryRouter></AuthProvider>);
  await act(async () => {});
};

describe("Vater-Startseite (B-749)", () => {
  // AK 1. Rot, wenn die Antwortzahl fehlt oder aus den Punkten kommt (37 ≠ 40), das Tagesziel nicht „n von m" sagt,
  // oder Tabelle, Anlegeformular bzw. Pläne-Karte zurück auf die Startseite wandern.
  it("zeigt je Kind Antworten, Tagesziel und Punkte – ohne Verwaltung", async () => {
    vi.spyOn(api, "allActivations").mockResolvedValue({ items: [], total: 0 });
    await zeige();

    const friedrich = within(screen.getByRole("list", { name: "Kinder heute" })).getAllByRole("listitem")[0];
    expect(friedrich.textContent).toContain("37 Antworten");
    expect(friedrich.textContent).toContain("3 von 4");
    expect(friedrich.textContent).toContain("40");
    const theodor = within(screen.getByRole("list", { name: "Kinder heute" })).getAllByRole("listitem")[1];
    expect(theodor.textContent).toContain("1 Antwort");
    expect(theodor.textContent).toContain("Tagesziel geschafft");

    expect(screen.queryByRole("button", { name: "Kind anlegen" })).toBeNull();
    expect(screen.queryByRole("heading", { name: "Kinder" })).toBeNull();
    expect(screen.queryByText("🎯 Lehrpläne")).toBeNull();
    expect(screen.getByText("Nichts einzulösen.")).toBeTruthy();
  });

  // AK 2. Rot, wenn die Liste ein Kind wählen lässt statt alle zu zeigen oder der Name fehlt (dann stünde „Kind #3").
  it("listet die offenen Wünsche aller Kinder mit Namen", async () => {
    vi.spyOn(api, "allActivations").mockResolvedValue({
      items: [anfrage(9, 3, "Fernsehen"), anfrage(8, 1, "Konsole")], total: 2,
    });
    await zeige();

    const zeilen = within(screen.getByRole("list", { name: "Offene Einlöse-Wünsche" })).getAllByRole("listitem");
    expect(zeilen.map((z) => z.textContent?.split(":")[0])).toEqual(["Theodor", "Friedrich"]);
    expect(screen.queryByRole("combobox")).toBeNull();
  });

  // AK 3, Liste. Rot, wenn Freigeben am falschen Kind entscheidet oder die Liste danach nicht neu lädt.
  it("gibt am Kind der Zeile frei und lädt die Liste neu", async () => {
    const liste = vi.spyOn(api, "allActivations")
      .mockResolvedValueOnce({ items: [anfrage(9, 3, "Fernsehen")], total: 1 })
      .mockResolvedValue({ items: [], total: 0 });
    const freigeben = vi.spyOn(api, "approveActivation").mockResolvedValue(anfrage(9, 3, "Fernsehen"));
    await zeige();

    fireEvent.click(screen.getByRole("button", { name: "Theodor: „Fernsehen\" freigeben" }));
    await act(async () => {});

    expect(freigeben).toHaveBeenCalledWith(3, 9);
    expect(liste).toHaveBeenCalledTimes(2);
    expect(screen.getByText("Nichts einzulösen.")).toBeTruthy();
  });

  // Review-Fund: lag die Rückfrage in `action.run`, meldete ein „Abbrechen" „freigegeben". Rot, wenn sie dorthin zurückwandert
  // (die Meldung erscheint) oder trotz Abbruch entschieden wird.
  it("meldet nach abgebrochener Rückfrage nichts und entscheidet nichts", async () => {
    vi.stubGlobal("confirm", () => false);
    vi.spyOn(api, "allActivations").mockResolvedValue({ items: [anfrage(9, 3, "Fernsehen")], total: 1 });
    const freigeben = vi.spyOn(api, "approveActivation");
    await zeige();

    fireEvent.click(screen.getByRole("button", { name: "Theodor: „Fernsehen\" freigeben" }));
    await act(async () => {});

    expect(freigeben).not.toHaveBeenCalled();
    expect(screen.queryByText(/freigegeben/)).toBeNull();
  });

  // Mehr als eine Seite offen: die Liste verschweigt den Rest nicht. Rot, wenn der Hinweis fehlt.
  it("nennt offene Wünsche jenseits der ersten Seite", async () => {
    vi.spyOn(api, "allActivations").mockResolvedValue({ items: [anfrage(9, 3, "Fernsehen")], total: 104 });
    await zeige();
    expect(screen.getByText(/und 103 weitere/)).toBeTruthy();
  });
});

describe("Zähler offener Einlösungen (B-749 AK 3)", () => {
  // Rot, wenn der Zähler nach einer Entscheidung stehen bleibt (keine Meldung, kein Neuladen) oder ohne `aktiv` abfragt.
  it("lädt nach einer Entscheidung neu und fragt für ein Lehrer-Konto nicht", async () => {
    const liste = vi.spyOn(api, "allActivations")
      .mockResolvedValueOnce({ items: [], total: 2 })
      .mockResolvedValue({ items: [], total: 1 });
    vi.spyOn(api, "approveActivation").mockResolvedValue(anfrage(9, 3, "Fernsehen"));

    const { result } = renderHook(() => useOffeneEinloesungenZahl(true, "/vater"));
    await act(async () => {});
    expect(result.current).toBe(2);

    const { entscheideEinloesung } = await import("./einloesungen");
    await act(async () => { await entscheideEinloesung(anfrage(9, 3, "Fernsehen"), true); });
    expect(result.current).toBe(1);

    liste.mockClear();
    renderHook(() => useOffeneEinloesungenZahl(false, "/vater"));
    await act(async () => {});
    expect(liste).not.toHaveBeenCalled();
  });
});

/*
 * B-950: offene Kartenmeldungen stehen auf „Heute“, je Kind gezählt, mit Link zur Rückmeldungs-Seite.
 */
const meldung = (id: number, childId: number, childName: string, count = 1) =>
  ({ id, childId, childName, count, status: "Open" }) as unknown as FeedbackEntry;

describe("Offene Rückmeldungen auf der Startseite (B-950)", () => {
  // AK 1, 3. Rot, wenn die Seite nicht nur „Open“ abfragt, nicht je Kind zählt oder nach `count` statt je Meldung zählt
  // (Friedrichs zweite Meldung trägt count 2 – gezählt werden zwei Meldungen, nicht drei), oder der Link das Kind nicht mitnimmt.
  it("zählt je Kind die offenen Meldungen und verlinkt", async () => {
    vi.spyOn(api, "allActivations").mockResolvedValue({ items: [], total: 0 });
    const abruf = vi.spyOn(api, "listFeedback").mockResolvedValue(
      [meldung(1, 3, "Theodor"), meldung(2, 1, "Friedrich"), meldung(3, 3, "Theodor", 2)]);
    await zeige();

    expect(abruf).toHaveBeenCalledWith(undefined, "Open");
    const zeilen = within(screen.getByRole("list", { name: "Offene Rückmeldungen" })).getAllByRole("listitem");
    expect(zeilen.map((z) => z.textContent)).toEqual([
      "Theodor: 2 offene Rückmeldungen ansehen →", "Friedrich: 1 offene Rückmeldung ansehen →",
    ]);
    expect(within(zeilen[0]).getByRole("link").getAttribute("href")).toBe("/vater/rueckmeldungen?childId=3");
  });

  // AK 2. Rot, wenn der Abschnitt auch ohne offene Meldung (als leere Überschrift) erscheint.
  it("zeigt ohne offene Meldung keinen Abschnitt", async () => {
    vi.spyOn(api, "allActivations").mockResolvedValue({ items: [], total: 0 });
    await zeige();
    expect(screen.queryByRole("heading", { name: "Rückmeldungen" })).toBeNull();
  });

  // AK 4. Rot, wenn ein Ladefehler den Abschnitt still verschwinden lässt.
  it("zeigt einen Ladefehler mit Wiederholen", async () => {
    vi.spyOn(api, "allActivations").mockResolvedValue({ items: [], total: 0 });
    vi.spyOn(api, "listFeedback").mockRejectedValue(new Error("kaputt"));
    await zeige();
    expect(screen.getByRole("heading", { name: "Rückmeldungen" })).toBeTruthy();
    expect(screen.getByRole("button", { name: /erneut|wiederholen/i })).toBeTruthy();
  });

  // Die reine Zählung. Rot, wenn sie nach `count` summiert.
  it("zählt Meldungen, nicht Wiederholungen", () => {
    expect(offeneJeKind([meldung(1, 3, "Theodor", 5)])).toEqual([{ childId: 3, name: "Theodor", anzahl: 1 }]);
  });

  // Review-Fund: „Erneut laden“ darf den Abschnitt samt Fehlerkasten nicht aushängen (B-216). Rot, wenn die Bedingung nur
  // `error` und Treffer kennt – der Retry setzt `error` zurück, bevor eine Antwort da ist.
  it("bleibt beim erneuten Laden stehen", async () => {
    vi.spyOn(api, "allActivations").mockResolvedValue({ items: [], total: 0 });
    vi.spyOn(api, "listFeedback").mockRejectedValueOnce(new Error("kaputt")).mockImplementationOnce(() => new Promise(() => {}));
    await zeige();
    fireEvent.click(screen.getByRole("button", { name: /erneut|wiederholen/i }));
    await act(async () => {});
    expect(screen.getByRole("heading", { name: "Rückmeldungen" })).toBeTruthy();
  });
});
