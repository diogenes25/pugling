import { describe, expect, it } from "vitest";
import type { ExerciseFilter } from "./ExerciseFilterBar";
import { planbauKriterien, planbauAuswahlSchluessel, dosisText, lernkontrolleText, schreibweise,
  REMOVE_POSITION_QUESTION, pointsFrom, settingsFrom, settingsToDto, settingsToUpdateDto, timeSlotProblem, withDose, withLeitner,
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
  useLeitner: true, forgivingDemotion: false, remediation: false, requireTypedTest: false,
  stage: "", stageStored: null, itemCountStored: null, goalThresholdStored: null, scope: "All", speedThresholdSeconds: "", speedBonusPoints: "",
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
    stage: null, scope: "All", speedThresholdSeconds: 0, speedBonusPoints: 0,
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
      stage: null, scope: "All", speedThresholdSeconds: 0, speedBonusPoints: 0,
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

/*
 * B-418: Entfernen geht nur bei einer Position ohne Übungs- und Testdaten (sonst `409 position_has_data`). Die Frage darf
 * darum keinen Verlust ankündigen – den gibt es in keinem der beiden Fälle. Rot, wenn die alte Ankündigung („gehen
 * verloren") zurückkehrt oder die Frage nicht mehr sagt, dass die Übung im Katalog bleibt.
 */
describe("Position entfernen", () => {
  it("kündigt keinen Verlust an und nennt, was bleibt", () => {
    expect(REMOVE_POSITION_QUESTION).not.toMatch(/verloren/);
    expect(REMOVE_POSITION_QUESTION).toMatch(/Übung bleibt im Katalog/);
  });
});

// B-480: die Positionszeile nennt die wirksame Dosis. Rot, wenn sie „2 neue je Tag" für eine ruhende Dosis verspricht oder
// die ruhende verschweigt.
describe("Positionszeile – Tagesdosis", () => {
  it("nennt die wirksame Dosis und sagt, wenn sie ruht", () => {
    expect(dosisText({ newItemsPerDay: 2, dailyDoseInForce: 2 })).toBe("2 neue je Tag");
    expect(dosisText({ newItemsPerDay: 2, dailyDoseInForce: 0 })).toMatch(/ruht/);
    expect(dosisText({ newItemsPerDay: 0, dailyDoseInForce: 0 })).toBeNull();
  });
});

// B-518: jedes Feld des Filters steuert die Abfrage. Rot, wenn eines in den Abhängigkeiten fehlt (die Liste bliebe beim
// Wechsel stehen). Die Feldliste kommt aus einem vollständig belegten Filter – ein neues Feld ohne Eintrag hier macht den
// Typprüfer rot (`satisfies Required<ExerciseFilter>`).
describe("Planbau – Abhängigkeiten der Suche", () => {
  it("ändert sich mit jedem Feld des Filters", () => {
    const voll = {
      subjectId: 1, seriesId: 2, seriesUnitId: 3, grade: 7, schoolType: "Gymnasium", categoryId: 4, type: "Vocabulary",
      search: "x", grammarTopicId: 5,
    } satisfies Required<ExerciseFilter>;
    const basis = JSON.stringify(planbauKriterien(voll, false));
    for (const feld of Object.keys(voll) as (keyof ExerciseFilter)[])
      expect(JSON.stringify(planbauKriterien({ ...voll, [feld]: undefined }, false)), feld).not.toBe(basis);
    // Die Reihenfolge gehört dazu (B-270).
    expect(JSON.stringify(planbauKriterien(voll, true))).not.toBe(basis);
  });

  // Review B-518: dieselbe Suche löst keine neue Abfrage aus. Rot, wenn die Freitextsuche nicht getrimmt oder ein leerer
  // Typ nicht wie „kein Typ" behandelt wird.
  it("normalisiert, was dieselbe Suche ist", () => {
    const voll = { subjectId: 1, search: "x", type: "Vocabulary" } satisfies ExerciseFilter;
    expect(planbauKriterien({ ...voll, search: " x " }, false)).toEqual(planbauKriterien(voll, false));
    expect(planbauKriterien({ ...voll, type: "", search: "  " }, false))
      .toEqual(planbauKriterien({ ...voll, type: undefined, search: undefined }, false));
  });
});

// B-540 AK 1: der Auswahl-Schlüssel des Planbaus kennt die Sortierung nicht, aber jedes Filterfeld.
describe("Planbau – Auswahl-Schlüssel", () => {
  const voll = {
    subjectId: 1, seriesId: 2, seriesUnitId: 3, grade: 7, schoolType: "Gymnasium", categoryId: 4, type: "Vocabulary",
    search: "x", grammarTopicId: 5,
  } satisfies Required<ExerciseFilter>;

  // Rot, wenn `planbauAuswahlSchluessel` die Sortierung mitnimmt (etwa `planbauKriterien(filter, besteZuerst)` statt `false`) –
  // dann leerte ein reines Umsortieren die Auswahl (der Defekt aus B-540). Die Kriterien der Abfrage ändern sich dabei sehr wohl.
  it("ist für beide Reihenfolgen gleich, während die Abfrage-Kriterien sich ändern", () => {
    expect(planbauAuswahlSchluessel(voll)).toBe(JSON.stringify(planbauKriterien(voll, false)));
    expect(JSON.stringify(planbauKriterien(voll, true))).not.toBe(planbauAuswahlSchluessel(voll));
  });

  // Rot, wenn ein Filterfeld im Schlüssel fehlt (etwa eine eigene Feldliste statt `planbauKriterien`): ein Wechsel dieses
  // Felds behielte dann die Auswahl einer Übung, die nicht mehr in der Liste steht.
  it("ändert sich mit jedem Feld des Filters", () => {
    const basis = planbauAuswahlSchluessel(voll);
    for (const feld of Object.keys(voll) as (keyof ExerciseFilter)[])
      expect(planbauAuswahlSchluessel({ ...voll, [feld]: undefined }), feld).not.toBe(basis);
  });
});

/*
 * B-16 AK 4: Stufe, Inhalte-Auswahl und Tempo-Bonus erreichen den Vertrag; „zurück auf Standard" wird beim Bearbeiten
 * zum ausdrücklichen Schalter `clearStage`, weil `stage: null` im PATCH „bleibt" heißt.
 */
describe("Positions-Formular – Stufe, Auswahl, Tempo (B-16)", () => {
  // Rot, wenn `settingsToDto` eines der vier Felder nicht sendet (heutiger Stand: keines) oder die Stufe als String schickt.
  it("sendet Stufe, Auswahl und Tempo-Werte", () => {
    const dto = settingsToDto({ ...LEER, stage: "3", scope: "New", speedThresholdSeconds: "8", speedBonusPoints: "2" });
    expect(dto.stage).toBe(3);
    expect(dto.scope).toBe("New");
    expect(dto.speedThresholdSeconds).toBe(8);
    expect(dto.speedBonusPoints).toBe(2);
  });

  // Rot, wenn ein leeres Feld als 0 statt `null` ankommt: dann schaltete „leer" (= erbt bzw. Standard) den Tempo-Bonus ab
  // bzw. setzte eine Stufe 0.
  it("leer heißt null (Standard bzw. Vorschlag der Übung)", () => {
    const dto = settingsToDto(LEER);
    expect(dto.stage).toBeNull();
    expect(dto.speedThresholdSeconds).toBeNull();
    expect(dto.speedBonusPoints).toBeNull();
  });

  // Rot, wenn `clearStage` fehlt oder nicht an der gespeicherten Stufe hängt – dann bliebe die alte Stufe nach
  // „Standard der Übung" stehen, und das Formular meldete trotzdem „Gespeichert.".
  it("schickt clearStage genau dann, wenn eine gespeicherte Stufe auf Standard zurückgeht", () => {
    expect(settingsToUpdateDto({ ...LEER, stage: "", stageStored: 3 }).clearStage).toBe(true);
    expect(settingsToUpdateDto({ ...LEER, stage: "", stageStored: null }).clearStage).toBe(false);
    expect(settingsToUpdateDto({ ...LEER, stage: "2", stageStored: 3 }).clearStage).toBe(false);
  });

  // B-547 AK 3. Rot, wenn ein Schalter fehlt oder nicht am gespeicherten Wert hängt – dann bliebe „5 Inhalte" bzw.
  // „90 %" nach dem Leeren stehen, und das Formular meldete „Gespeichert.". Die Gegenfälle machen ihn rot, wenn er
  // ohne gespeicherten Wert (nichts zu leeren) oder bei gefülltem Feld (der Wert gewönne nicht) mitkäme.
  it("schickt clearItemCount/clearGoalThreshold genau dann, wenn ein gespeicherter Wert geleert wird", () => {
    const geleert = settingsToUpdateDto({ ...LEER, itemCount: " ", itemCountStored: 5, goalThreshold: "", goalThresholdStored: 90 });
    expect([geleert.clearItemCount, geleert.clearGoalThreshold]).toEqual([true, true]);
    const nieGesetzt = settingsToUpdateDto({ ...LEER, itemCount: "", itemCountStored: null, goalThreshold: "", goalThresholdStored: null });
    expect([nieGesetzt.clearItemCount, nieGesetzt.clearGoalThreshold]).toEqual([false, false]);
    const geaendert = settingsToUpdateDto({ ...LEER, itemCount: "3", itemCountStored: 5, goalThreshold: "70", goalThresholdStored: 90 });
    expect([geaendert.clearItemCount, geaendert.clearGoalThreshold]).toEqual([false, false]);
  });

  // Rot, wenn das Anlegen die B-547-Schalter mitschickt – `CreatePositionDto` kennt sie nicht (`unknown_field`).
  it("das Anlegen kennt die Leeren-Schalter für Inhalte und Bestehen nicht", () => {
    const dto = settingsToDto({ ...LEER, itemCountStored: 5, goalThresholdStored: 90 });
    expect(["clearItemCount" in dto, "clearGoalThreshold" in dto]).toEqual([false, false]);
  });

  // Rot, wenn das Anlegen den Schalter mitschickt: `CreatePositionDto` kennt ihn nicht, der Server antwortet `unknown_field`.
  it("das Anlegen kennt den Schalter nicht", () => {
    expect("clearStage" in settingsToDto({ ...LEER, stage: "", stageStored: 3 })).toBe(false);
  });

  // Rot, wenn `settingsFrom` die gespeicherte Stufe nicht als `stageStored` merkt (dann ginge `clearStage` nie an) oder
  // Auswahl/Tempo nicht übernimmt (dann überschriebe jedes Speichern sie mit den Formular-Vorgaben).
  it("übernimmt den gespeicherten Stand", () => {
    const s = settingsFrom({
      timeSlots: null, cadence: "Daily", goalThreshold: 90, itemCount: 5, orderStrategy: "WeakestFirst",
      pointsGoalMet: 20, penaltyCoins: 0, newContentPoints: 10, comboThreshold: 5, comboBonusPoints: 5,
      useLeitner: true, requireTypedTest: false, newItemsPerDay: 0,
      stage: 4, scope: "Old", speedThresholdSeconds: 6, speedBonusPoints: 1,
    } as unknown as PositionResponse);
    expect([s.stage, s.stageStored, s.scope, s.speedThresholdSeconds, s.speedBonusPoints]).toEqual(["4", 4, "Old", "6", "1"]);
    // B-547: ohne die gemerkten Werte ginge clearItemCount/clearGoalThreshold nie an.
    expect([s.itemCount, s.itemCountStored, s.goalThreshold, s.goalThresholdStored]).toEqual(["5", 5, "90", 90]);
  });
});

/*
 * B-469 AK 1 und 3, B-550: die Positionszeile sagt, womit die Pflicht geprüft wird. Die Art kommt vom Server
 * (`learningCheck`); `lernkontrolleText` übersetzt sie nur. Je Art ein Fall – rot, wenn ihr Zweig fehlt (dann käme `undefined`
 * statt des Satzes) oder Text bzw. Ton vertauscht sind.
 */
describe("Positionszeile – Lernkontrolle (B-469, B-550)", () => {
  it("Typed: neutral „Test getippt“", () => {
    expect(lernkontrolleText({ kind: "Typed" })).toEqual({ text: "Test getippt", ton: "neutral" });
  });
  it("SelfAssessed: gelb", () => {
    expect(lernkontrolleText({ kind: "SelfAssessed" })).toEqual({ text: "Test, Selbsteinschätzung zählt", ton: "warnung" });
  });
  // Rot, wenn die Leiter gelb warnt (der Fund aus B-550) oder den Tag falsch nennt (Schwellentag statt Vortag).
  it("Ladder: neutral, mit dem Tag, ab dem getippt wird", () => {
    expect(lernkontrolleText({ kind: "Ladder", typedFromDay: 4 }))
      .toEqual({ text: "Selbsteinschätzung bis Tag 3, ab Tag 4 getippt", ton: "neutral" });
    // Ohne Tag kein erfundener Tag.
    expect(lernkontrolleText({ kind: "Ladder", typedFromDay: null }))
      .toEqual({ text: "Erst Selbsteinschätzung, dann getippt", ton: "neutral" });
  });
  it("Playthrough: gelb „zählt Durchspielen“", () => {
    expect(lernkontrolleText({ kind: "Playthrough" })).toEqual({ text: "zählt Durchspielen", ton: "warnung" });
  });
  // Rot, wenn der `NoTest`-Zweig fehlt – dann stünde wie vor B-469 nichts an einer Grammatik-Position.
  it("NoTest: gelb „kein Test – zählt Durchspielen“", () => {
    expect(lernkontrolleText({ kind: "NoTest" })).toEqual({ text: "kein Test – zählt Durchspielen", ton: "warnung" });
  });
  // Rot, wenn die unerreichbare Kombination nur gelb oder gar nicht gemeldet wird.
  it("Unreachable: rot, mit Grund", () => {
    const zeile = lernkontrolleText({ kind: "Unreachable" });
    expect(zeile?.ton).toBe("fehler");
    expect(zeile?.text).toMatch(/^Ziel nicht erreichbar: nur getippte Tests zählen/);
  });
  // AK 3. Rot, wenn ein unbekannter Typ (Server: `null`) einen Satz bekommt – er wäre geraten.
  it("null: nichts", () => {
    expect(lernkontrolleText(null)).toBeNull();
    expect(lernkontrolleText(undefined)).toBeNull();
  });
});

/*
 * B-219 AK 7: die verzeihende Abstufung reist beidseitig – Read-back aus der Antwort, Senden beim Anlegen und Ändern.
 */
describe("Ausrutscher verzeihen (B-219)", () => {
  const stand = (forgivingDemotion?: boolean) => settingsFrom({
    timeSlots: null, cadence: "Daily", goalThreshold: null, itemCount: null, orderStrategy: "WeakestFirst",
    pointsGoalMet: 20, penaltyCoins: 0, newContentPoints: 10, comboThreshold: 5, comboBonusPoints: 5,
    useLeitner: true, requireTypedTest: false, newItemsPerDay: 0, forgivingDemotion,
    stage: null, scope: "All", speedThresholdSeconds: 0, speedBonusPoints: 0,
  } as unknown as PositionResponse);

  // Rot, wenn `settingsFrom` das Feld nicht liest: dann stünde das Häkchen beim Bearbeiten aus, und jedes Speichern
  // schaltete die Milde still wieder ab.
  it("liest den gespeicherten Stand zurück", () => {
    expect(stand(true).forgivingDemotion).toBe(true);
    // Eine ältere Antwort ohne das Feld heißt „aus", nicht `undefined` im Häkchen.
    expect(stand(undefined).forgivingDemotion).toBe(false);
  });

  // Rot, wenn `settingsToDto` das Feld nicht mitschickt – Anlegen und PATCH ließen die Milde dann serverseitig aus bzw.
  // unverändert.
  it("schickt das Häkchen beim Anlegen und beim Ändern", () => {
    expect(settingsToDto({ ...LEER, forgivingDemotion: true }).forgivingDemotion).toBe(true);
    expect(settingsToUpdateDto({ ...LEER, forgivingDemotion: true }).forgivingDemotion).toBe(true);
    // Abwählen ist eine Aussage: `false`, nicht `null` – im PATCH bliebe die Milde sonst stehen.
    expect(settingsToUpdateDto({ ...LEER, forgivingDemotion: false }).forgivingDemotion).toBe(false);
  });
});

describe("schreibweise (B-597)", () => {
  it("beschriftet die gedrehte Karte und die reine Erkennungskarte, sonst nichts", () => {
    // Rot, wenn eine der beiden Flaggen nicht gelesen würde oder eine ungedrehte Karte eine Zeile bekäme.
    const zeile = { prompt: "walk", answer: "gehen", turnedWhenWritten: false, recognitionOnly: false };
    expect(schreibweise({ ...zeile, turnedWhenWritten: true })).toBe("geschrieben: gehen → walk");
    expect(schreibweise({ ...zeile, recognitionOnly: true })).toBe("nur erkennen – geschrieben wird die Gegenkarte");
    expect(schreibweise(zeile)).toBeNull();
  });
});
