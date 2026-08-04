import { describe, expect, it } from "vitest";
import { settingsFrom, settingsToDto, settingsToUpdateDto, timeSlotProblem, type PositionSettings } from "./PlanPositions";
import type { PositionResponse, ScoringTimeSlot } from "../lib/types";

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
  cadence: "Daily", goalThreshold: "", itemCount: "", orderStrategy: "WeakestFirst",
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
    useLeitner: true, requireTypedTest: false,
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
