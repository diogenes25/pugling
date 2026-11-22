import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useSohn } from "./SohnApp";
import { truhenZiele } from "./pfad";
import type { ExtraTrainingOverview, OverviewResponse } from "../lib/types";

/**
 * B-660: der Leerzustand des Übens ist kein Ende. Eine erledigte Aufgabe lässt sich noch einmal spielen – für Münzen nur
 * im Bonus-Training, dem einen Münzweg für Zusatzrunden (Entscheidung 1: dort sinkt der Wert je Runde 100/50/25/0, mit
 * Deckel und Sperre). Drei Lagen: die Übung liegt nicht im Pool (Nicht-Vokabel-Pflicht, B-533 Entscheidung 6) → nichts,
 * denn eine Wiederholung zahlte dort von Anfang an 0; die Sperre ist zu → der Satz, wie viele Ziele noch fehlen; die Sperre
 * ist offen → ein Knopf in **diese** Übung samt ihrem heutigen Wert (bei vollem Tagesdeckel ohne Münzen). Das freie
 * Üben ohne Münzen ist eine eigene Story (B-821).
 *
 * Beiwerk wie der Wochenvergleich der Erfolge: ein Fehler beim Laden lässt den Block still weg, statt den Leerzustand mit
 * einem Fehlerkasten zu belegen. Ein Tippen startet nie ungefragt eine bezahlte Runde (Entscheidung 2) – der Knopf führt
 * in die Runde, die dort erst startet.
 */
export function NochEinmal({ planId, positionId }: { planId: number; positionId: number }) {
  const { t } = useTranslation();
  const { childId } = useSohn();
  // Die Übung der Position kennt das Üben nicht; die Tagesübersicht des Plans nennt sie (kein Vertragsfeld nötig).
  const overview = useAsync<OverviewResponse>(() => api.overview(planId), [planId]);
  const bonus = useAsync<ExtraTrainingOverview>(() => api.extraTraining(childId), [childId]);

  const o = overview.data;
  const b = bonus.data;
  if (!o || !b) return null;
  const exerciseId = o.today.positions.find((p) => p.positionId === positionId)?.exerciseId;
  // Der Server liefert den Pool auch bei geschlossener Sperre. Liegt die Übung nicht darin (Nicht-Vokabel-Pflicht, B-533
  // Entscheidung 6), versprächen beide Lagen etwas, das nie kommt – also erst der Pool, dann die Sperre.
  const e = b.exercises.find((x) => x.exerciseId === exerciseId);
  if (!e) return null;

  if (!b.unlocked) {
    // Die Sperre zählt die Ziele, die der Server als offen meldet; ohne offene Ziele (kein Plan, gesperrter Tag) gibt es
    // hier nichts zu versprechen.
    if (b.outstanding.length === 0) return null;
    return (
      <p className="sub" data-testid="noch-einmal">
        {t(truhenZiele(b.rewardCadence) === "woche" ? "practice.againLockedWeek" : "practice.againLocked", { count: b.outstanding.length })}
      </p>
    );
  }

  // Ein voller Tagesdeckel zahlt nichts mehr, auch wenn die Übung selbst noch 100 % wert wäre (Muster der Bonus-Liste,
  // `SohnBonus.tsx`): Vokabeln dürfen weiter, eine feste Übung wartet bis morgen.
  const capReached = b.dailyCoinCap != null && b.coinsToday >= b.dailyCoinCap;
  // Nicht im Pool, oder eine feste Übung, die heute nur noch ihre Lösungen zeigen würde: kein Knopf (Muster der Bonus-Liste).
  if (!e.repeatable && (e.done || capReached)) return null;
  return (
    <div className="card" data-testid="noch-einmal">
      <p className="sub" style={{ margin: "0 0 8px" }}>
        {e.nextRunPercent > 0 && !capReached ? t("practice.againWorth", { percent: e.nextRunPercent }) : t("practice.againNoCoins")}
      </p>
      <Link to={`/sohn/bonus/${e.exerciseId}`} className="btn lime" style={{ textDecoration: "none", textAlign: "center" }}>
        {/* Eine angefangene Runde wird fortgesetzt, nie ersetzt (`openRunId`) – der Knopf sagt das wie die Bonus-Liste. */}
        {e.openRunId != null ? t("bonus.resume") : t("practice.againPlay")}
      </Link>
    </div>
  );
}
