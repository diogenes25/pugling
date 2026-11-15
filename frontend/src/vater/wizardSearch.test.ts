import { describe, expect, it } from "vitest";
import { auswahlNachFilterwechsel, auswahlNachUmsortieren, unsichtbareAuswahl, wizardFilterKey, wizardSearchParams, type WizardFilter } from "./wizardSearch";

/*
 * B-18. Die Filter des Assistenten-Schritts „Übungen".
 *
 * Der Bildschirm zeigt nicht, welche Query beim Server ankommt — und genau dort saß die Lücke der Story:
 * Der Server konnte nach Art, Typ und Quelle filtern, der Assistent schickte es nur nie mit.
 */

const leer: WizardFilter = {
  subjectId: 4, grade: undefined, schoolType: undefined,
  search: "", categoryId: "", type: "", source: "", grammarTopicId: "",
};

describe("wizardSearchParams – leer heißt „alle“, nicht „leerer Wert“", () => {
  it("schickt einen ungesetzten Filter gar nicht mit", () => {
    // `categoryId: ""` als Query wäre für den Server ein Filter auf nichts – die Trefferliste bliebe
    // ohne sichtbaren Grund leer.
    expect(wizardSearchParams(leer)).toEqual({
      subjectId: 4, grade: undefined, schoolType: undefined,
      search: undefined, categoryId: undefined, type: undefined, source: undefined, take: undefined,
    });
  });

  it("nimmt die drei neuen Filter mit, sobald sie gesetzt sind", () => {
    const p = wizardSearchParams({ ...leer, categoryId: 7, type: "Cloze", source: "Green Line 1" });

    expect(p.categoryId).toBe(7);
    expect(p.type).toBe("Cloze");
    expect(p.source).toBe("Green Line 1");
  });

  it("nimmt das Grammatik-Thema mit, sobald es gesetzt ist – und sonst gar nicht (B-155)", () => {
    expect(wizardSearchParams({ ...leer, grammarTopicId: 12 }).grammarTopicId).toBe(12);
    expect(wizardSearchParams(leer).grammarTopicId).toBeUndefined();
  });

  it("trimmt die Freitexte und wirft reine Leerzeichen weg", () => {
    const p = wizardSearchParams({ ...leer, search: "  Drill ", source: "   " });

    expect(p.search).toBe("Drill");
    expect(p.source).toBeUndefined();
  });

  it("hält Quelle und Suchtext getrennt", () => {
    // Die Quelle benennt eine Lehrwerk-Stelle. In die Titelsuche gefaltet träfe „Unit 1" jede Übung,
    // deren Titel zufällig eine Unit erwähnt – deshalb zwei Parameter, nicht einer.
    const p = wizardSearchParams({ ...leer, search: "Drill", source: "Unit 1" });

    expect(p.search).toBe("Drill");
    expect(p.source).toBe("Unit 1");
  });

  it("reicht Klasse und Schulart unverändert durch", () => {
    const p = wizardSearchParams({ ...leer, grade: 8, schoolType: "Gymnasium" });

    expect(p.grade).toBe(8);
    expect(p.schoolType).toBe("Gymnasium");
  });

  /*
   * Der Kern des „Alle wählen"-Nachschlags: Er muss DIESELBEN Filter fahren wie die Liste, nur mit
   * höherem `take`. Liefe er auf einer anderen Abbildung, übernähme der Knopf andere Übungen, als der
   * Vater vor sich sieht — und das fiele erst am fertigen Plan auf.
   */
  it("ändert mit `take` nur die Seitengröße, keinen Filter", () => {
    const filter: WizardFilter = { ...leer, categoryId: 7, type: "Cloze", source: "Green Line 1", search: "Drill" };
    const { take: _ohne, ...liste } = wizardSearchParams(filter);
    const { take, ...nachschlag } = wizardSearchParams(filter, 500);

    expect(take).toBe(500);
    expect(nachschlag).toEqual(liste);
  });
});

/*
 * B-161. Die zwei Ableitungen, an denen die Auswahl des Assistenten hängt.
 *
 * Der Defekt, den sie festhalten: `selected` überlebte jeden Filterwechsel, und weil „Alle wählen" bis zu
 * 500 Ids schreibt, während nur die geladene Seite gerendert wird, konnte der fertige Plan Positionen
 * tragen, die der gezeigte Filter ausschließt — mit Pflichtziel, also mit Münz-Malus fürs Kind. `selected`
 * bedeutete „aus dieser Trefferliste gewählt" UND „irgendwann früher gewählt".
 */
describe("wizardFilterKey – was als andere Suche zählt", () => {
  it("bleibt gleich, wenn sich nichts Wirksames ändert", () => {
    expect(wizardFilterKey(leer)).toBe(wizardFilterKey({ ...leer }));
  });

  it("ändert sich bei jedem der acht Kriterien", () => {
    const anders: Partial<WizardFilter>[] = [
      { subjectId: 5 }, { grade: 8 }, { schoolType: "Gymnasium" },
      { search: "Drill" }, { categoryId: 7 }, { type: "Cloze" }, { source: "Green Line 1" }, { grammarTopicId: 12 },
    ];
    for (const feld of anders) {
      expect(wizardFilterKey({ ...leer, ...feld })).not.toBe(wizardFilterKey(leer));
    }
  });

  it("wirft die Auswahl NICHT weg, wenn nur ein Leerschlag dazukommt", () => {
    // Ginge der Schlüssel über die Rohfelder, kostete jedes Tippen im Suchfeld die Auswahl — auch das
    // Löschen des letzten Leerzeichens. Er geht darum über `wizardSearchParams`, das schon trimmt.
    expect(wizardFilterKey({ ...leer, search: "Drill " })).toBe(wizardFilterKey({ ...leer, search: "Drill" }));
  });

  it("nennt `take` nicht im Schluessel – Absichtserklaerung, kein Beleg", () => {
    // Ehrlich beschriftet: `wizardFilterKey` ruft `wizardSearchParams` ohne `take` auf, und
    // `JSON.stringify` wirft `undefined` ohnehin weg – der Fall bleibt also auch gruen, wenn man das
    // Rest-Spread entfernt. Er dokumentiert die Absicht („eine groessere Seite ist dieselbe Suche"),
    // beweist sie aber nicht. Der Beleg dafuer ist der Fall darueber, der jedes der acht Kriterien
    // einzeln durchgeht.
    expect(wizardFilterKey({ ...leer, categoryId: 7 })).not.toContain("take");
  });
});

describe("unsichtbareAuswahl – wie viele Gewählte die Liste nicht zeigt", () => {
  it("zählt null, solange jede Wahl gerendert ist", () => {
    expect(unsichtbareAuswahl([1, 2, 3], [1, 2, 3, 4])).toBe(0);
  });

  it("zählt genau die, die nicht in der geladenen Seite stehen", () => {
    // Der Fall nach „Alle wählen": 500 gewählt, 100 geladen.
    const gewaehlt = Array.from({ length: 500 }, (_, i) => i + 1);
    const geladen = gewaehlt.slice(0, 100);
    expect(unsichtbareAuswahl(gewaehlt, geladen)).toBe(400);
  });

  it("zählt null bei leerer Auswahl, auch ohne geladene Liste", () => {
    expect(unsichtbareAuswahl([], [])).toBe(0);
  });

  it("zählt alle, wenn die Liste leer ist – das ist der Widerspruch, den die Zahl sichtbar macht", () => {
    expect(unsichtbareAuswahl([7, 8], [])).toBe(2);
  });
});

describe("auswahlNachFilterwechsel – die Regel, die den Defekt traegt", () => {
  it("tut nichts, solange die Kriterien dieselben sind", () => {
    expect(auswahlNachFilterwechsel([1, 2], "A", "A")).toBeNull();
  });

  it("verwirft die Auswahl, sobald die Kriterien wechseln", () => {
    // Das ist der Defekt: vorher ueberlebte `selected` jeden Filterwechsel, und der fertige Plan trug
    // Positionen, die der gezeigte Filter ausschliesst.
    const folge = auswahlNachFilterwechsel([1, 2, 3], "A", "B");

    expect(folge?.selected).toEqual([]);
    expect(folge?.hinweis).toContain("3 Übungen");
  });

  it("schweigt beim Wechsel, wenn nichts gewaehlt war", () => {
    // Sonst begruesst der Assistent den Vater mit „Auswahl zurueckgesetzt", bevor er etwas getan hat.
    const folge = auswahlNachFilterwechsel([], "A", "B");

    expect(folge?.selected).toEqual([]);
    expect(folge?.hinweis).toBeNull();
  });

  it("beugt bei genau einer verworfenen Uebung", () => {
    // „1 Übungen" liest sich wie ein vergessener Platzhalter; das Haus beugt an vergleichbarer Stelle.
    expect(auswahlNachFilterwechsel([7], "A", "B")?.hinweis).toContain("(1 Übung)");
    // Und die Gegenprobe, damit der Fall nicht bloss „enthaelt irgendwas" prueft:
    expect(auswahlNachFilterwechsel([7, 8], "A", "B")?.hinweis).toContain("(2 Übungen)");
  });

  it("nennt die Zahl der verworfenen, nicht bloss dass etwas geschah", () => {
    expect(auswahlNachFilterwechsel(Array.from({ length: 500 }, (_, i) => i), "A", "B")?.hinweis)
      .toContain("500");
  });
});

/** B-408: „beste Bewertung zuerst" im Assistenten – eine Reihenfolge, kein Suchkriterium. */
describe("Sortierwunsch im Assistenten (B-408)", () => {
  // AK 1. Rot, wenn der Wunsch die Abfrage nicht erreicht oder aufsteigend sortiert.
  it("fragt mit beste-zuerst nach rating absteigend, ohne den Wunsch nach keinem von beiden", () => {
    const mit = wizardSearchParams({ ...leer, besteZuerst: true });
    expect([mit.sort, mit.dir]).toEqual(["rating", "desc"]);
    // „Alle wählen" baut seine Abfrage aus derselben Funktion und fragt damit dieselbe Reihenfolge.
    expect(wizardSearchParams({ ...leer, besteZuerst: true }, 500)).toMatchObject({ sort: "rating", dir: "desc", take: 500 });
    const ohne = wizardSearchParams(leer);
    expect([ohne.sort, ohne.dir]).toEqual([undefined, undefined]);
  });

  // AK 2. Rot, wenn die Sortierung in den Schlüssel gerät – dann verwürfe der Assistent die Auswahl beim Umsortieren.
  it("laesst den Auswahl-Schluessel beim Umsortieren gleich", () => {
    expect(wizardFilterKey({ ...leer, besteZuerst: true })).toBe(wizardFilterKey(leer));
  });
});

/** B-408, Entscheidung 3: die Regel für den Planbau (der Bau dort ist B-540). */
describe("auswahlNachUmsortieren", () => {
  // AK 3. Rot, wenn die Regel immer leert (der heutige Planbau).
  it("behaelt eine Auswahl, die in der neuen Seite steht, ohne Hinweis", () => {
    expect(auswahlNachUmsortieren([7], [3, 7, 9])).toEqual({ selected: [7], hinweis: null });
  });

  // AK 3. Rot, wenn die Regel immer behält (B-167 kehrte zurück: die Auswahl zeigte auf eine unsichtbare Übung).
  it("leert eine Auswahl, die aus der Seite gerutscht ist, mit Hinweis", () => {
    const folge = auswahlNachUmsortieren([7], [3, 9]);
    expect(folge.selected).toEqual([]);
    expect(folge.hinweis).toContain("(1 Übung)");
  });
});
