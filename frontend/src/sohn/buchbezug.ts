import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { api } from "../lib/api";

/**
 * Der Buchbezug einer Position (B-588) als fertiger Text „Reihe · Unit · S. 42“, oder `null` ohne Seite. Seit B-686
 * startet der Knoten direkt, ohne Blatt davor – darum steht der Bezug im Kopf der Runde, im Üben wie im Test. Gelesen
 * aus der Plan-Übersicht; Beiwerk: scheitert das Lesen, fehlt nur die Pille.
 */
export function useBuchbezug(planId: number | null, positionId: number): string | null {
  const { t } = useTranslation();
  const [buchbezug, setBuchbezug] = useState<string | null>(null);
  useEffect(() => {
    // Ein Wechsel der Position darf die Pille der vorigen nicht stehen lassen.
    setBuchbezug(null);
    if (!planId || !positionId) return;
    let aktiv = true;
    api.overview(planId).then((o) => {
      const pos = o.today.positions.find((p) => p.positionId === positionId);
      if (aktiv && pos?.bookPages)
        setBuchbezug(t("pfad.bookRef", { series: pos.seriesName ?? "", unit: pos.seriesUnitLabel, pages: pos.bookPages }));
    }).catch(() => {});
    return () => { aktiv = false; };
  }, [planId, positionId, t]);
  return buchbezug;
}
