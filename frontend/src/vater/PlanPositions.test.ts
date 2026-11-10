import { describe, expect, it } from "vitest";
import {
  pointsFrom, settingsFrom, settingsToDto, settingsToUpdateDto, timeSlotProblem, withDose, withLeitner,
  type PositionSettings,
} from "./PlanPositions";
import type { ExerciseSummary, PositionResponse, ScoringTimeSlot } from "../lib/types";

/*
 * Das Zeitfenster einer Position (Punkte-Faktor je Pflicht) – geprüft wird die **Bindung** zwischen Formular
 * und Vertrag, nicht die Optik:
 *
 * 1. Das geleerte Fenster muss als `clearTimeSlots` ankommen. `null` heißt im Vertrag „nicht angegeben"; ohne
 *    den Schalter meldete das Formular „Gespeichert." und der alte Faktor verdoppelte weiter.
 * 2. Ein halb gefülltes oder rückwärts laufendes Fenster wird hier abgefangen – es sähe sonst nach einer
 *    gültigen Einstellung aus und täte nichts.
 * 3. Das Formular stellt EIN Fenster ein, der Server hält eine Liste (bis 24) und das Formular schickt
 *    `timeSlots` bei jedem Speichern mit: die per API gesetzten Fenster 2..n und die Sekunden im
 *    gespeicherten Wert müssen eine reine Punkte-Änderung überleben.
 */

const LEER: PositionSettings = {
  cadence: "Daily", goalThreshold: "", itemCount: "", newItemsPerDay: "", orderStrategy: "WeakestFirst",
  pointsGoalMet: 20, penaltyCoins: 0,
  newContentPoints: "", comboThreshold: "", comboBonusPoints: "",
  useLeitner: true, requireTypedTest: false,
  timeSlotStart: "", timeSlotEnd: "", timeSlotMultiplier: "", timeSlotName: "", timeSlotStored: null,
};

const MIT_FENSTER: PositionSettings = {
  ...LEER, timeSlotStart: "13:00", timeSlotEnd: "15:00", timeSlotMultiplier: "2",
};

/** Ein gespeicherter Stand als Formular-Zustand – der Weg, den ein Positions-Edit wirklich nimmt. */
const gespeichert = (...timeSlots: ScoringTimeSlot[]) =>
  settingsFrom({
    timeSlots, cadence: "Daily", goalThreshold: null, itemCount: null, orderStrategy: "WeakestFirst",
    pointsGoalMet: 20, penaltyCoins: 0, newContentPoints: 10, comboThreshold: 5, comboBonusPoints: 5,
    useLeitner: true, requireTypedTest: false, newItemsPerDay: 0,
  } as unknown as PositionResponse);

describe("Zeitfenster einer Position", () => {
  it("schickt das Fenster als einelementige Liste – die Ablage bleibt listenfähig", () => {
    const dto = settingsToUpdateDto(MIT_FENSTER);

    expect(dto.timeSlots).toEqual([{ name: "Zeitfenster der Pflicht", start: "13:00", end: "15:00", multiplier: 2 }]);
    expect(dto.clearTimeSlots).toBe(false);
  });

  it("schickt den Schalter NICHT beim Anlegen – `CreatePositionDto` kennt ihn nicht", () => {
    // Wanderte `clearTimeSlots` je nach `settingsToDto`, blieben Build und Test grün und das Anlegen
    // scheiterte mit `400 unknown_field`.
    expect(settingsToDto(LEER)).not.toHaveProperty("clearTimeSlots");
    expect(settingsToDto(MIT_FENSTER)).not.toHaveProperty("clearTimeSlots");
  });

  it("behält einen per API gesetzten Fenster-Namen", () => {
    const dto = settingsToUpdateDto({ ...MIT_FENSTER, timeSlotName: "Hausaufgaben" });

    expect(dto.timeSlots?.[0].name).toBe("Hausaufgaben");
  });

  it("leert das Fenster über den Schalter, nicht über null", () => {
    const dto = settingsToUpdateDto(LEER);

    expect(dto.timeSlots).toBeNull();
    // Der Kern: ohne diese Zeile bliebe das gespeicherte Fenster stehen.
    expect(dto.clearTimeSlots).toBe(true);
  });

  it("liest den gespeicherten Stand ohne Sekunden zurück", () => {
    const s = gespeichert({ name: "Hausaufgaben", start: "13:00:00", end: "15:00:00", multiplier: 2 });

    expect(s.timeSlotStart).toBe("13:00");
    expect(s.timeSlotEnd).toBe("15:00");
    expect(s.timeSlotMultiplier).toBe("2");
  });

  /*
   * Der Kern beider Fälle: `timeSlots` geht bei JEDEM Speichern mit (anders als `boxIntervalDays` & Co., die
   * das DTO weglässt). Was das Formular nicht zeigt, muss es darum tragen – sonst löscht eine Punkte-Änderung
   * fremde Einstellungen und meldet „Position gespeichert.".
   */
  it("behält die per API gesetzten weiteren Fenster beim Speichern", () => {
    const s = gespeichert(
      { name: "Hausaufgaben", start: "13:00:00", end: "15:00:00", multiplier: 2 },
      { name: "Abend", start: "19:00:00", end: "20:00:00", multiplier: 3 },
      { name: "Wochenende", start: "09:00:00", end: "11:00:00", multiplier: 1.5 },
    );

    // Nur die Punkte geändert – das Zeitfenster hat der Vater nicht angesehen.
    const dto = settingsToUpdateDto({ ...s, pointsGoalMet: 30 });

    expect(dto.timeSlots).toHaveLength(3);
    expect(dto.timeSlots?.map((t) => t.name)).toEqual(["Hausaufgaben", "Abend", "Wochenende"]);
    expect(dto.clearTimeSlots).toBe(false);
  });

  it("schneidet die Sekunden des gespeicherten Fensters nicht ab", () => {
    // `TimeOnly.MaxValue` am Server – „bis Mitternacht" schrumpfte sonst auf „bis 23:59" zusammen.
    const s = gespeichert({ name: "Ganztags", start: "00:00:00", end: "23:59:59.9999999", multiplier: 2 });

    expect(settingsToUpdateDto(s).timeSlots?.[0].end).toBe("23:59:59.9999999");
    // Eine echte Änderung gewinnt dagegen – der gespeicherte Wert gilt nur für die unveränderte Anzeige.
    expect(settingsToUpdateDto({ ...s, timeSlotEnd: "22:00" }).timeSlots?.[0].end).toBe("22:00");
  });

  it("löscht beim Leeren des ersten Fensters nicht die übrigen", () => {
    const s = gespeichert(
      { name: "Hausaufgaben", start: "13:00:00", end: "15:00:00", multiplier: 2 },
      { name: "Abend", start: "19:00:00", end: "20:00:00", multiplier: 3 },
    );

    const dto = settingsToUpdateDto({ ...s, timeSlotStart: "", timeSlotEnd: "", timeSlotMultiplier: "" });

    expect(dto.timeSlots?.map((t) => t.name)).toEqual(["Abend"]);
    // Nichts zu leeren, solange ein Fenster übrig bleibt – der Schalter würde die Liste wegwerfen.
    expect(dto.clearTimeSlots).toBe(false);
  });

  it.each([
    ["gar nichts gefüllt", LEER, null],
    ["nur die Anfangszeit", { ...LEER, timeSlotStart: "13:00" }, "Zeitfenster: bitte von, bis und Faktor ausfüllen – oder alle drei leer lassen."],
    ["ohne Faktor", { ...MIT_FENSTER, timeSlotMultiplier: "" }, "Zeitfenster: bitte von, bis und Faktor ausfüllen – oder alle drei leer lassen."],
    ["rückwärts", { ...MIT_FENSTER, timeSlotStart: "15:00", timeSlotEnd: "13:00" }, "Zeitfenster: „von“ muss vor „bis“ liegen."],
    ["gleiche Uhrzeit", { ...MIT_FENSTER, timeSlotEnd: "13:00" }, "Zeitfenster: „von“ muss vor „bis“ liegen."],
    ["Faktor 0", { ...MIT_FENSTER, timeSlotMultiplier: "0" }, "Zeitfenster: der Faktor muss größer als 0 sein."],
    ["vollständig", MIT_FENSTER, null],
  ])("meldet %s richtig", (_fall, settings, erwartet) => {
    expect(timeSlotProblem(settings as PositionSettings)).toBe(erwartet);
  });
});

/*
 * B-45: die Punkte-Empfehlung des Creators. `Exercise.rewardPoints` war ein Pflichtfeld, das kein
 * Bewertungspfad las – ein Creator, der dort 15 eintrug, bewirkte nichts.
 *
 * Geprüft wird die REGEL, nicht das Formular: `pointsFrom` entscheidet beim Wählen einer Übung, ob ihre
 * Empfehlung den aktuellen Wert ersetzt. Rot machen diese Fälle das Entfernen der `> 0`-Prüfung (Fälle 2
 * und 3) bzw. das Ignorieren der Empfehlung (Fall 1).
 *
 * Der DRITTE Parameter ist der Kern (B-191): „der Vater hat das gesagt" darf nicht dasselbe sein wie
 * „die vorher angeklickte Übung hat das empfohlen". Ohne ihn stand der zweite Parameter für beides, und
 * der Wert von Übung A landete in Übung B.
 *
 * Der zweite Parameter allein trug diese Unterscheidung nie: Eine noch frühere Fassung fiel auf ein
 * hartcodiertes 20 zurück und hätte eine selbst getippte 50 überschrieben (Fund des `frontend-reviewer`) –
 * die Korrektur damals war richtig, sie brauchte nur den Zustand, den sie nicht hatte.
 */
describe("Punkte-Empfehlung der Übung", () => {
  // `suggestedGoalPoints` ist das, was der Server als wirksam nennt (B-192) - `rewardPoints` bleibt roh
  // daneben stehen, damit die alten Faelle weiter zeigen, dass das Formular NICHT den rohen Wert liest.
  const uebung = (suggestedGoalPoints: number, rewardPoints = suggestedGoalPoints) =>
    ({ rewardPoints, suggestedGoalPoints }) as ExerciseSummary;
  const GETIPPT = 50;

  /*
   * B-191. Der Defekt saß nicht in einem der Faelle unten, sondern in ihrer VERKETTUNG: `current` stand
   * fuer zwei Dinge zugleich - "der Vater hat das getippt" und "die vorher angeklickte Uebung hat das
   * empfohlen". Ein Fall mit EINER Uebung kann das nicht messen, und genau darum kam es durch.
   *
   * Zugleich ist es B-45s eigenes Akzeptanzkriterium 4, woertlich: das Feld werde mit `RewardPoints`
   * vorbelegt, "bzw. 20, wenn die Uebung 0 traegt". Mit `current` stand dort weder das eine noch das
   * andere.
   */
  it("traegt die Empfehlung der vorigen Uebung nicht in die naechste", () => {
    // Uebung A empfiehlt 100 -> das Feld zeigt 100. So weit richtig.
    const nachA = pointsFrom(uebung(100), 20, false);
    expect(nachA).toBe(100);

    // Uebung B traegt ROH eine 0, empfiehlt also nichts; der Server nennt darum 20 als wirksamen
    // Vorschlag (B-192). Der rohe Nullwert ist hier tragend und kein Detail: mit einer rohen 20 waere
    // der Fall auch VOR der Reparatur gruen gewesen und haette nichts gemessen (am 2026-09-01 genau so
    // passiert und erst von der roten Probe entlarvt).
    // Vorher stand hier 100 - eine Zahl, die weder B empfiehlt noch jemand getippt hat.
    expect(pointsFrom(uebung(20, 0), nachA, false)).toBe(20);
  });

  it("laesst eine selbst getippte Zahl ueber den Uebungswechsel stehen", () => {
    // Die Gegenprobe, und sie ist tragend: Ohne sie waere die Zusicherung oben auch von einem
    // `pointsFrom` erfuellt, das den Vater einfach immer ueberschreibt.
    expect(pointsFrom(uebung(20, 0), GETIPPT, true)).toBe(GETIPPT);
    expect(pointsFrom(uebung(100), GETIPPT, true)).toBe(GETIPPT);
  });

  it("übernimmt die Empfehlung als Zielpunkte", () => {
    expect(pointsFrom(uebung(15), GETIPPT, false)).toBe(15);
  });

  it("liest den wirksamen Vorschlag und rechnet nicht selbst nach", () => {
    /*
     * Diese Zusicherung hat mit B-192 ihre Aussage GEWECHSELT, und das gehoert hingeschrieben: Vorher
     * pruefte sie, dass das Formular eine unbrauchbare Empfehlung (0, negativ) selbst abfaengt. Genau
     * das tut es jetzt nicht mehr - die Grenze (0, 200] und der Rueckfall auf 20 liegen im Server
     * (`GoalPointsPolicy`), und dort sind sie gepinnt (`PlanPositionCrudTests`:
     * `Katalogtreffer_Nennt_Den_Wirksamen_Punktevorschlag`, alle drei Faelle inklusive dem negativen).
     *
     * Was sie jetzt haelt, ist die andere Haelfte derselben Entscheidung: dass hier NICHT nachgerechnet
     * wird. Taete das Formular es doch, stuende dieselbe Regel zweimal da - und die Kopie ist das, was
     * driftet. Der rohe Wert ist darum in allen drei Faellen absichtlich unbrauchbar und wird ignoriert.
     */
    expect(pointsFrom(uebung(20, 0), GETIPPT, false)).toBe(20);
    expect(pointsFrom(uebung(20, -5), GETIPPT, false)).toBe(20);
    expect(pointsFrom(uebung(20, 100_000), GETIPPT, false)).toBe(20);
  });

  it("lässt ihn stehen, solange keine Übung gewählt ist", () => {
    // Absichtserklärung, keine Messung: unter `strict` kompiliert die realistische Verletzung gar nicht.
    expect(pointsFrom(undefined, GETIPPT, false)).toBe(GETIPPT);
  });
});

describe("Tagesdosis einer Position (B-04)", () => {
  it("schickt ein leeres Feld als 0, nicht als null – sonst bliebe im PATCH die alte Dosis stehen", () => {
    expect(settingsToDto(LEER).newItemsPerDay).toBe(0);
  });

  it("schickt die eingetragene Dosis als Zahl", () => {
    expect(settingsToDto({ ...LEER, newItemsPerDay: "8" }).newItemsPerDay).toBe(8);
  });

  it("zeigt eine gespeicherte 0 als leeres Feld und eine Dosis als Zahl", () => {
    const stand = (newItemsPerDay: number) => settingsFrom({
      timeSlots: null, cadence: "Daily", goalThreshold: null, itemCount: null, orderStrategy: "WeakestFirst",
      pointsGoalMet: 20, penaltyCoins: 0, newContentPoints: 10, comboThreshold: 5, comboBonusPoints: 5,
      useLeitner: true, requireTypedTest: false, newItemsPerDay,
    } as unknown as PositionResponse);
    expect(stand(0).newItemsPerDay).toBe("");
    expect(stand(8).newItemsPerDay).toBe("8");
  });
});

describe("Dosis und Leitner-Kasten gehören zusammen (B-04)", () => {
  const OHNE_KASTEN: PositionSettings = { ...LEER, useLeitner: false };

  it("eine eingetragene Dosis setzt das Häkchen", () => {
    expect(withDose(OHNE_KASTEN, "8")).toMatchObject({ newItemsPerDay: "8", useLeitner: true });
  });

  it("ein leeres oder Null-Feld setzt es nicht", () => {
    expect(withDose(OHNE_KASTEN, "").useLeitner).toBe(false);
    expect(withDose(OHNE_KASTEN, "0").useLeitner).toBe(false);
  });

  it("das Abwählen des Kastens leert die Dosis, das Anwählen lässt sie stehen", () => {
    expect(withLeitner({ ...LEER, newItemsPerDay: "8" }, false)).toMatchObject({ newItemsPerDay: "", useLeitner: false });
    expect(withLeitner({ ...OHNE_KASTEN, newItemsPerDay: "8" }, true).newItemsPerDay).toBe("8");
  });

  it("der PATCH trägt die Dosis mit – ein geleertes Feld als 0", () => {
    expect(settingsToUpdateDto({ ...LEER, newItemsPerDay: "" }).newItemsPerDay).toBe(0);
    expect(settingsToUpdateDto({ ...LEER, newItemsPerDay: "8" }).newItemsPerDay).toBe(8);
  });
});
