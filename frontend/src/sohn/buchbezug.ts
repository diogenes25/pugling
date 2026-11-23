import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { api } from "../lib/api";

/** Die Pillen im Kopf der Runde: Buchbezug und Stufenband. */
export type RundenKopf = { buchbezug: string | null; stufe: string | null };

/**
 * Buchbezug (B-588) und Stufenband (B-841) aus **einer** Lesung der Plan-Übersicht. Der Buchbezug ist „Reihe · Unit ·
 * S. 42“ oder `null` ohne Seite; seit B-686 startet der Knoten direkt, ohne Blatt davor – darum stehen beide im Kopf der
 * Runde, im Üben wie im Test. Beiwerk: scheitert das Lesen, fehlen nur die Pillen. Das Stufenband „Stufe n von m“ steht
 * nur an einer gestuften Position (`build`), als Information, nicht als Knopf (B-704, Entscheidung 3: das Kind wählt
 * seine Stufe nicht selbst).
 */
export function useRundenKopf(planId: number | null, positionId: number): RundenKopf {
  const { t } = useTranslation();
  const [kopf, setKopf] = useState<RundenKopf>({ buchbezug: null, stufe: null });
  useEffect(() => {
    // Ein Wechsel der Position darf die Pillen der vorigen nicht stehen lassen.
    setKopf({ buchbezug: null, stufe: null });
    if (!planId || !positionId) return;
    let aktiv = true;
    api.overview(planId).then((o) => {
      const pos = o.today.positions.find((p) => p.positionId === positionId);
      if (!aktiv || !pos) return;
      setKopf({
        buchbezug: pos.bookPages
          ? t("pfad.bookRef", { series: pos.seriesName ?? "", unit: pos.seriesUnitLabel, pages: pos.bookPages })
          : null,
        stufe: pos.build ? t("pfad.buildLevel", { n: pos.build.level, of: pos.build.of }) : null,
      });
    }).catch(() => {});
    return () => { aktiv = false; };
  }, [planId, positionId, t]);
  return kopf;
}
