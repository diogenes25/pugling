import { describe, expect, it } from "vitest";
import { settingsFrom, settingsToDto, settingsToUpdateDto, timeSlotProblem, type PositionSettings } from "./PlanPositions";
import type { PositionResponse } from "../lib/types";

/*
 * Das Zeitfenster einer Position (Punkte-Faktor je Pflicht) – geprüft wird die **Bindung** zwischen Formular
 * und Vertrag, nicht die Optik:
 *
 * 1. Das geleerte Fenster muss als `clearTimeSlots` ankommen. `null` heißt im Vertrag „nicht angegeben"; ohne
 *    den Schalter meldete das Formular „Gespeichert." und der alte Faktor verdoppelte weiter.
 * 2. Ein halb gefülltes oder rückwärts laufendes Fenster wird hier abgefangen – es sähe sonst nach einer
 *    gültigen Einstellung aus und täte nichts.
 */

const LEER: PositionSettings = {
  cadence: "Daily", goalThreshold: "", itemCount: "", orderStrategy: "WeakestFirst",
  pointsGoalMet: 20, penaltyCoins: 0,
  newContentPoints: "", comboThreshold: "", comboBonusPoints: "",
  useLeitner: true, requireTypedTest: false,
  timeSlotStart: "", timeSlotEnd: "", timeSlotMultiplier: "", timeSlotName: "",
};

const MIT_FENSTER: PositionSettings = {
  ...LEER, timeSlotStart: "13:00", timeSlotEnd: "15:00", timeSlotMultiplier: "2",
};

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
    const pos = {
      timeSlots: [{ name: "Hausaufgaben", start: "13:00:00", end: "15:00:00", multiplier: 2 }],
      cadence: "Daily", goalThreshold: null, itemCount: null, orderStrategy: "WeakestFirst",
      pointsGoalMet: 20, penaltyCoins: 0, newContentPoints: 10, comboThreshold: 5, comboBonusPoints: 5,
      useLeitner: true, requireTypedTest: false,
    } as unknown as PositionResponse;

    const s = settingsFrom(pos);

    expect(s.timeSlotStart).toBe("13:00");
    expect(s.timeSlotEnd).toBe("15:00");
    expect(s.timeSlotMultiplier).toBe("2");
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
