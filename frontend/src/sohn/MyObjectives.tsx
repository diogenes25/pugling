import { useEffect } from "react";
import { useTranslation } from "react-i18next";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import type { Objective, Paged } from "../lib/types";
import { useSohn } from "./SohnApp";
import { takeFreshAchieved } from "./seenIds";

/** Wie viele Ziele die Liste zeigt; `total` daneben nennt alle aktiven. */
const PAGE = 5;

const achievedIds = (xs: { id: number; status: string }[]) => xs.filter((x) => x.status === "Achieved").map((x) => x.id);

/**
 * „Meine großen Ziele" – die Sohn-Sicht auf die Objectives, die der Vater gesetzt hat.
 *
 * Bewusst **nur lesend**: Ziele setzt und schneidet der Vater, das Kind sieht, woran es ist. Der Server
 * liefert ausschließlich *aktive* Ziele; ein stillgelegtes taucht hier gar nicht erst auf, damit niemand
 * auf etwas hinarbeitet, das nicht mehr zählt.
 *
 * Die Währung steht dran, weil sie den Unterschied macht: 🪙 verbindlich = echte Belohnung beim Papa,
 * 💎 Dehnungsziel = Skins. Ohne diesen Hinweis sähen beide Ziele gleich aus.
 *
 * Neu erreichte Etappen und Ziele werden einmal gefeiert (B-36), nach demselben „gesehene IDs"-Muster
 * wie Missionen und Abzeichen.
 */
export function MyObjectives() {
  const { t } = useTranslation();
  const { childId, celebrate } = useSohn();
  // `v2`: ein Stand aus der Zeit vor B-257 kennt nur die ersten fünf Ziele und ist darum unbrauchbar. Der neue Schlüssel
  // lässt jedes Gerät einmal still neu seeden – eine seither neu erreichte Etappe bleibt dabei einmal ungefeiert, lieber
  // das als ein alter Erfolg mit „+N 🪙".
  const krKey = `pugling.seenKeyResults.v2.${childId}`;
  const objectiveKey = `pugling.seenObjectives.v2.${childId}`;
  // Die Gesehen-Mengen müssen jedes aktive Ziel kennen, nicht nur die fünf gezeigten: ein Ziel, das auf Platz 6 erreicht
  // wird, bliebe sonst ungesehen und würde gefeiert, sobald es nach vorn rutscht (B-257 beim ersten Besuch, B-327 bei
  // jedem späteren). Darum lädt jeder Besuch bis an die Server-Obergrenze (`PagingExtensions.MaxTake`) und die Liste
  // schneidet auf fünf; die Ziele eines Kindes sind wenige.
  const objectives = useAsync<Paged<Objective>>(() => api.myObjectives({ take: 500 }), []);

  useEffect(() => {
    const items = objectives.data?.items;
    if (!items) return;
    const krs = items.flatMap((o) => o.keyResults);
    const freshKrs = takeFreshAchieved(krKey, achievedIds(krs));
    const freshObjectives = takeFreshAchieved(objectiveKey, achievedIds(items));
    // Reihenfolge ist Absicht: es gibt nur ein Overlay, der letzte Aufruf gewinnt – schließt die letzte
    // Etappe zugleich das Ziel ab, bleibt so die größere Feier stehen.
    if (freshKrs.length > 0) {
      const k = krs.find((x) => x.id === freshKrs[0])!;
      celebrate("medium", "🎯", k.title ?? t(`objectives.untitled.${k.scope}`), t("objectives.stagesDone", { count: freshKrs.length }));
    }
    if (freshObjectives.length > 0) {
      const o = items.find((x) => x.id === freshObjectives[0])!;
      // Ohne Belohnung bucht der Server nichts – dann auch kein „+0".
      const reward = o.rewardOnComplete > 0 ? `+${o.rewardOnComplete} ${o.kind === "Committed" ? "🪙" : "💎"}` : undefined;
      celebrate("big", "🏆", t("objectives.goalDone"), reward);
    }
  }, [objectives.data, krKey, objectiveKey, celebrate, t]);

  if (objectives.loading) return <div className="card"><div className="loading">{t("objectives.loading")}</div></div>;
  // Ein Fehler hier darf den Trophäenweg nicht abwürgen – die Ziele sind Beiwerk, kein Kern der Seite.
  if (objectives.error || !objectives.data || objectives.data.items.length === 0) return null;

  return (
    <div className="card">
      <div className="row" style={{ marginBottom: 8 }}>
        <b style={{ fontSize: 13 }}>{t("objectives.title")}</b>
        {/* „offen" wäre falsch: `total` ist die Zahl aller *aktiven* Ziele – erreichte zählen mit, und
            nachrechnen kann man es hier nicht, weil nur die ersten fünf gezeigt werden. */}
        <span className="sub" style={{ marginLeft: "auto" }}>{t("objectives.count", { count: objectives.data.total })}</span>
      </div>

      {objectives.data.items.slice(0, PAGE).map((o) => (
        <div key={o.id} style={{ padding: "6px 0" }}>
          <div className="row">
            <b>{o.title}</b>
            <span className="chip" style={{ marginLeft: "auto" }}>
              {o.kind === "Committed" ? "🪙" : "💎"}<b className="tabnum">{o.rewardOnComplete}</b>
            </span>
          </div>

          {/* Der Balken ist das Wichtigste: „wie weit bin ich" liest sich schneller als jede Zahl. */}
          <div className="bar" aria-hidden><i style={{ width: `${o.progressPercent}%` }} /></div>
          <div className="sub" style={{ fontSize: 11 }}>
            {t("objectives.stages", { done: o.achievedCount, total: o.totalCount })}
            {o.status === "Achieved" ? ` · ${t("objectives.achieved")}` : o.status === "Overdue" ? ` · ${t("objectives.overdue")}` : ""}
            {o.dueDate ? ` · ${t("objectives.due", { date: o.dueDate })}` : ""}
          </div>

          {/* Nur die noch offenen Etappen: erledigte abzuhaken ist schön, aber sie verstopfen die Liste. */}
          {o.keyResults.filter((k) => k.status !== "Achieved").slice(0, 3).map((k) => (
            <div className="row" key={k.id} style={{ padding: "2px 0" }}>
              <span>⬜</span>
              <span className="sub">{k.title ?? t(`objectives.untitled.${k.scope}`)}</span>
              <span className="sub tabnum" style={{ marginLeft: "auto" }}>
                {k.currentValue}/{k.targetValue}
              </span>
            </div>
          ))}
        </div>
      ))}
    </div>
  );
}
