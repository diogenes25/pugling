import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { KeyResult, Objective } from "../lib/types";

/**
 * B-36: neu erreichte Etappen (KeyResults) und große Ziele (Objectives) feiern genau einmal.
 * Je Fall steht dabei, welche Änderung ihn rot macht.
 */
const celebrate = vi.fn();
const myObjectives = vi.fn();

vi.mock("./SohnApp", () => ({ useSohn: () => ({ childId: 7, celebrate }) }));
vi.mock("../lib/auth", () => ({ useAuth: () => ({ signOut: () => {} }) }));
vi.mock("../lib/api", () => ({ api: { myObjectives: (...a: unknown[]) => myObjectives(...a) } }));

const { MyObjectives } = await import("./MyObjectives");

const kr = (id: number, achieved: boolean, title: string): KeyResult => ({
  id, objectiveId: 1, subjectId: 1, seriesUnitId: null, exerciseId: null, scope: "Subject",
  metric: "AvgMastery", targetValue: 3, currentValue: achieved ? 3 : 1, progressPercent: achieved ? 100 : 33,
  status: achieved ? "Achieved" : "Open", title,
});
const obj = (id: number, keyResults: KeyResult[], kind: Objective["kind"] = "Committed"): Objective => {
  const done = keyResults.filter((k) => k.status === "Achieved").length;
  return {
    id, childId: 7, title: `Ziel ${id}`, motivation: null, kind, start: null, dueDate: null, active: true,
    rewardOnComplete: 50, rewardPerKeyResult: 0, achievedCount: done, totalCount: keyResults.length,
    progressPercent: Math.round((done / keyResults.length) * 100),
    status: done === keyResults.length ? "Achieved" : "Open", rewarded: false, keyResults, createdAt: "2026-10-01T00:00:00Z",
  };
};

/** Rendert einen Abruf mit diesen Zielen und wartet, bis die Liste steht (Effekt gelaufen). */
async function visit(items: Objective[]) {
  myObjectives.mockResolvedValueOnce({ items, total: items.length });
  const view = render(<MyObjectives />);
  await waitFor(() => expect(view.container.textContent).toContain("Meine großen Ziele"));
  view.unmount();
}

beforeEach(() => {
  localStorage.clear();
  celebrate.mockReset();
  myObjectives.mockReset();
});

describe("MyObjectives feiert Teilziele", () => {
  // Rot, wenn der Erst-Seed fehlt (`first ? [] : fresh` → `fresh`): dann feiert der erste Besuch Alt-Erfolge.
  it("feiert beim allerersten Besuch nichts nach, sondern merkt sich den Stand", async () => {
    await visit([obj(1, [kr(11, true, "Unit 1"), kr(12, false, "Unit 2")])]);
    expect(celebrate).not.toHaveBeenCalled();
    expect(localStorage.getItem("pugling.seenKeyResults.v2.7")).toBe("[11]");
  });

  // Rot, wenn der KeyResult-Zweig fehlt oder statt „medium"/🎯 eine andere Stufe feiert.
  it("feiert eine neu erreichte Etappe einmal mittel", async () => {
    await visit([obj(1, [kr(11, false, "Unit 1"), kr(12, false, "Unit 2")])]);
    await visit([obj(1, [kr(11, true, "Unit 1"), kr(12, false, "Unit 2")])]);
    expect(celebrate).toHaveBeenCalledTimes(1);
    expect(celebrate).toHaveBeenCalledWith("medium", "🎯", "Unit 1", "Etappe geschafft");
  });

  // Rot, wenn die beiden Aufrufe vertauscht werden: dann überschriebe die kleine Feier die große.
  it("schließt die letzte Etappe das Ziel ab, kommt die große Feier zuletzt", async () => {
    await visit([obj(1, [kr(11, true, "Unit 1"), kr(12, false, "Unit 2")], "Stretch")]);
    await visit([obj(1, [kr(11, true, "Unit 1"), kr(12, true, "Unit 2")], "Stretch")]);
    expect(celebrate.mock.calls).toEqual([
      ["medium", "🎯", "Unit 2", "Etappe geschafft"],
      ["big", "🏆", "ZIEL GESCHAFFT!", "+50 💎"],
    ]);
  });

  // Rot, wenn der gesehene Stand nicht gelesen wird (`fresh = achievedIds`): dann feiert jeder Reload erneut.
  // (Wird er gar nicht geschrieben, bleibt dieser Fall grün – dann ist jeder Besuch ein erster; das fängt Fall 1.)
  it("feiert einen schon gesehenen Stand beim Neuladen nicht noch einmal", async () => {
    await visit([obj(1, [kr(11, false, "Unit 1")])]);
    await visit([obj(1, [kr(11, true, "Unit 1")])]);
    celebrate.mockReset();
    await visit([obj(1, [kr(11, true, "Unit 1")])]);
    expect(celebrate).not.toHaveBeenCalled();
  });

  // Rot, wenn der Abgleich den Stand wie bei Missionen überschreibt (`writeSeen(key, achievedIds)`):
  // ein Ziel, das kurz nicht auf der ersten Seite stand, würde beim Wiederauftauchen nachgefeiert.
  it("vergisst ein erreichtes Ziel nicht, nur weil es einmal nicht geladen wurde", async () => {
    await visit([obj(1, [kr(11, true, "Unit 1")]), obj(2, [kr(21, false, "Unit 9")])]);
    await visit([obj(2, [kr(21, false, "Unit 9")])]);
    await visit([obj(1, [kr(11, true, "Unit 1")]), obj(2, [kr(21, false, "Unit 9")])]);
    expect(celebrate).not.toHaveBeenCalled();
  });

  // Rot, wenn der Abgleich vergisst, was gerade wieder offen ist (`seen.delete` für sichtbare, nicht erreichte
  // IDs): der Server rechnet den Status live, eine falsche Antwort zieht das Ziel kurz zurück – und die zweite
  // Feier „+50 🪙" verspräche Münzen, die er nur einmal zahlt.
  it("feiert ein Ziel, das kurz wieder offen war, kein zweites Mal", async () => {
    await visit([obj(1, [kr(11, false, "Unit 1")])]);
    await visit([obj(1, [kr(11, true, "Unit 1")])]);
    await visit([obj(1, [kr(11, false, "Unit 1")])]);
    celebrate.mockReset();
    await visit([obj(1, [kr(11, true, "Unit 1")])]);
    expect(celebrate).not.toHaveBeenCalled();
  });

  // Rot, wenn die Feier die Zahl nicht durchreicht (fester Singular): zwei Etappen auf einmal hießen sonst „Etappe".
  it("nennt zwei auf einmal erreichte Etappen im Plural", async () => {
    // Etappe 13 ist schon vorher erreicht: so unterscheiden sich „neu erreicht" (2) und „erreicht" (3).
    await visit([obj(1, [kr(11, false, "Unit 1"), kr(12, false, "Unit 2"), kr(13, true, "Unit 3"), kr(14, false, "Unit 4")])]);
    await visit([obj(1, [kr(11, true, "Unit 1"), kr(12, true, "Unit 2"), kr(13, true, "Unit 3"), kr(14, false, "Unit 4")])]);
    expect(celebrate).toHaveBeenCalledWith("medium", "🎯", "Unit 1", "2 Etappen geschafft");
  });

  // Rot, wenn die Liste die Währung nicht nach `kind` weicht: das Dehnungsziel trüge dann die Münze des verbindlichen.
  it("zeigt in der Liste die Währung je Zielart", async () => {
    myObjectives.mockResolvedValueOnce({
      items: [obj(1, [kr(11, false, "Unit 1")]), { ...obj(2, [kr(21, false, "Unit 2")], "Stretch"), rewardOnComplete: 7 }],
      total: 2,
    });
    render(<MyObjectives />);
    expect((await screen.findByText("50")).parentElement?.textContent).toBe("🪙50");
    expect(screen.getByText("7").parentElement?.textContent).toBe("💎7");
  });

  // Rot, wenn die Belohnungszeile ohne Prüfung gebaut wird: dann stünde „+0 🪙" für ein Ziel ohne Belohnung.
  it("verspricht bei einem Ziel ohne Belohnung keine Münzen", async () => {
    const noReward = (achieved: boolean) => ({ ...obj(1, [kr(11, achieved, "Unit 1")]), rewardOnComplete: 0 });
    await visit([noReward(false)]);
    await visit([noReward(true)]);
    expect(celebrate).toHaveBeenLastCalledWith("big", "🏆", "ZIEL GESCHAFFT!", undefined);
  });
});

/**
 * B-257/B-327: die Gesehen-Mengen umfassen alle aktiven Ziele, nicht nur die fünf gezeigten – beim ersten wie bei jedem
 * späteren Besuch. Der Mock blättert wie der Server (`skip`/`take` über der ganzen Liste) – ein fester Rückgabewert sähe
 * nicht, wie viel der Bildschirm anfragt.
 */
describe("MyObjectives, Ziele jenseits der ersten Seite", () => {
  async function visitPaged(all: Objective[]) {
    myObjectives.mockImplementationOnce(({ skip = 0, take = 100 }: { skip?: number; take?: number } = {}) =>
      Promise.resolve({ items: all.slice(skip, skip + take), total: all.length }));
    const view = render(<MyObjectives />);
    await waitFor(() => expect(view.container.textContent).toContain("Meine großen Ziele"));
    return view;
  }
  const open = (id: number) => obj(id, [kr(id * 10, false, `Unit ${id}`)]);
  const done = (id: number) => obj(id, [kr(id * 10, true, `Unit ${id}`)]);

  // Rot, wenn der erste Besuch wieder nur die ersten fünf lädt (`take: 5`): Ziel 6 war schon erreicht, wurde aber nie als
  // gesehen gemerkt – rutscht es nach vorn, feiert die App einen alten Erfolg mit „+50 🪙".
  it("feiert ein beim ersten Besuch schon erreichtes sechstes Ziel später nicht", async () => {
    (await visitPaged([open(1), open(2), open(3), open(4), open(5), done(6)])).unmount();
    (await visitPaged([done(6)])).unmount();
    expect(celebrate).not.toHaveBeenCalled();
  });

  // Rot, wenn die Schlüssel nicht versioniert sind: ein Gerät mit dem Stand von vor B-257 kennt nur die ersten fünf und
  // gälte nicht als erster Besuch – Ziel 6 würde gefeiert.
  it("seedet ein Gerät mit altem Stand einmal neu", async () => {
    localStorage.setItem("pugling.seenKeyResults.7", "[]");
    localStorage.setItem("pugling.seenObjectives.7", "[]");
    (await visitPaged([open(1), open(2), open(3), open(4), open(5), done(6)])).unmount();
    (await visitPaged([done(6)])).unmount();
    expect(celebrate).not.toHaveBeenCalled();
  });

  // Rot, wenn der erste Besuch alle geladenen Ziele zeigt statt der ersten fünf: die Liste ist ein Ausschnitt, die Zahl
  // daneben nennt alle.
  it("zeigt beim ersten Besuch trotzdem nur die ersten fünf", async () => {
    const view = await visitPaged([open(1), open(2), open(3), open(4), open(5), open(6)]);
    expect(screen.getByText("Ziel 5")).toBeTruthy();
    expect(screen.queryByText("Ziel 6")).toBeNull();
    view.unmount();
  });

  // B-327. Rot, wenn ein späterer Besuch wieder nur die erste Seite lädt: Ziel 6, erreicht während es hinten stand, würde
  // nie als gesehen gemerkt und beim Vorrücken nachgefeiert. Der erste Besuch macht alle folgenden zu späteren – so
  // trennt sich der Fall vom Erst-Besuch oben.
  it("feiert ein hinten erreichtes Ziel rechtzeitig und nicht noch einmal, wenn es vorrückt", async () => {
    (await visitPaged([open(1)])).unmount();
    (await visitPaged([open(1), open(2), open(3), open(4), open(5), done(6)])).unmount();
    // Entscheidung 2 von B-327: frisch erreicht wird gefeiert, auch wenn das Ziel nicht in der Liste steht. Rot, wenn nur
    // die gezeigten fünf gefeiert werden.
    expect(celebrate).toHaveBeenLastCalledWith("big", "🏆", "ZIEL GESCHAFFT!", "+50 🪙");
    celebrate.mockReset();
    (await visitPaged([done(6)])).unmount();
    expect(celebrate).not.toHaveBeenCalled();
  });
});

/** B-317: eine Etappe ohne Titel nennt, was sie misst, statt des rohen Enum-Werts – in der Liste und in der Feier. */
describe("MyObjectives, Etappe ohne Titel", () => {
  const untitled = (achieved: boolean, scope: KeyResult["scope"] = "SeriesUnit", id = 21): KeyResult =>
    ({ ...kr(id, achieved, "x"), title: null, scope });

  // Rot, wenn die Liste wieder `k.scope` zeigt („SeriesUnit“) – oder einen festen Schlüssel statt eines je Bereich
  // (dann stünde zweimal derselbe Text da).
  it("zeigt in der Liste eine Beschreibung je Bereich", async () => {
    myObjectives.mockResolvedValueOnce({
      items: [obj(2, [untitled(false), untitled(false, "Exercise", 23), kr(22, false, "Unit 2")])], total: 1,
    });
    render(<MyObjectives />);

    expect(await screen.findByText("Etappe in einer Unit")).toBeTruthy();
    expect(screen.getByText("Etappe in einer Übung")).toBeTruthy();
    expect(screen.queryByText("SeriesUnit")).toBeNull();
  });

  // Rot, wenn die Feier wieder `k.scope` als Titel nimmt.
  it("feiert mit derselben Beschreibung", async () => {
    await visit([obj(2, [untitled(false), kr(22, false, "Unit 2")])]);
    await visit([obj(2, [untitled(true), kr(22, false, "Unit 2")])]);
    expect(celebrate).toHaveBeenCalledWith("medium", "🎯", "Etappe in einer Unit", "Etappe geschafft");
  });
});

// B-517: das Datum eines großen Ziels steht in der Sprache der Arcade, nicht als ISO-Text. Rot, wenn `dueDate` roh in den
// Satz geht (vor B-447 stand dort „bis 2026-11-20").
describe("MyObjectives – Termin", () => {
  it("zeigt den Termin als Datum der Arcade, nicht als ISO", async () => {
    myObjectives.mockResolvedValueOnce({ items: [{ ...obj(9, [kr(1, false, "Etappe")]), dueDate: "2026-11-20" }], total: 1 });
    const view = render(<MyObjectives />);
    await waitFor(() => expect(view.container.textContent).toContain("Ziel 9"));

    expect(view.container.textContent).toContain("20.11.2026");
    expect(view.container.textContent).not.toContain("2026-11-20");
    view.unmount();
  });
});

