import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useAction } from "../lib/useAction";
import { useErrorMessage } from "../lib/chromeText";
import { StatusBanner } from "../components/StatusBanner";
import { arcadeDatum } from "../lib/datum";
import type { SavingsGoal } from "../lib/types";

/**
 * Die Zeilen unter dem Titel eines Sparziels (B-593), als reine Funktion des Server-Stands – damit Startseite und Shop
 * dasselbe sagen. Die Tage heißen „bei deinem Tempo etwa“ (Entscheidung 1): eine Prognose, keine Zusage von Pflicht-Tagen;
 * ohne Zahl vom Server (kurzer Verlauf, nichts verdient, reiner Gem-Preis) gibt es keine Tageszeile.
 */
export function useSparzielZeilen() {
  const { t, i18n } = useTranslation();
  return (goal: SavingsGoal): string[] => {
    if (!goal.available) return [t("sparziel.gone")];
    const coins = goal.missingCoins ?? 0;
    const gems = goal.missingGems ?? 0;
    const zeilen: string[] = [];
    if (coins === 0 && gems === 0) zeilen.push(t("sparziel.reached"));
    else {
      const fehlt = [coins > 0 ? t("sparziel.missingCoins", { count: coins }) : null,
        gems > 0 ? t(coins > 0 ? "sparziel.missingGems" : "sparziel.missingGemsOnly", { count: gems }) : null];
      zeilen.push(fehlt.filter(Boolean).join(" "));
      if (goal.daysAtPace != null && coins > 0) zeilen.push(t("sparziel.days", { count: goal.daysAtPace }));
    }
    // B-645 wie im Shop: der eigene Bestand ist leer, das Ziel bleibt, das Datum sagt, wann es wiederkommt (Entscheidung 8).
    if (goal.currentStock === 0 && goal.nextRefillAtUtc)
      zeilen.push(t("sparziel.soldOut", { when: arcadeDatum(goal.nextRefillAtUtc, i18n.language) }));
    return zeilen;
  };
}

/**
 * Der Sparziel-Streifen der Startseite (B-593, Entscheidung 4): Titel, was noch fehlt, und die Tempo-Prognose; er führt in
 * den Shop. Ohne Ziel erscheint nichts. Ein verschwundenes Angebot bleibt mit „nicht mehr verfügbar“ stehen (Entscheidung 6);
 * entfernen kann das Kind es dann hier.
 */
export function SparzielStreifen({ goal, onRemove, busy = false, imShop = false }: {
  goal: SavingsGoal | null | undefined;
  /** Der Weg, das Ziel loszuwerden – auf der Startseite nur bei einem nicht mehr verfügbaren Ziel, im Shop immer. */
  onRemove?: () => void;
  busy?: boolean;
  /** Im Shop selbst: kein Link in den Shop, dafür immer „Ziel entfernen“. */
  imShop?: boolean;
}) {
  const { t } = useTranslation();
  const zeilen = useSparzielZeilen();
  if (!goal) return null;
  return (
    <section className={`card sparziel${goal.available ? "" : " weg"}`} aria-label={t("sparziel.title")} data-testid="sparziel">
      <div className="row" style={{ gap: 8 }}>
        <span aria-hidden="true" style={{ fontSize: 24 }}>🎯</span>
        <div style={{ flex: 1 }}>
          <div className="sub">{t("sparziel.title")}</div>
          <b>{goal.title}</b>
          {zeilen(goal).map((z) => <div key={z} className="sub">{z}</div>)}
        </div>
        {goal.available && !imShop
          ? <Link to="/sohn/shop" className="btn gold small" style={{ width: "auto" }} aria-label={t("nav.shop")}>🛒</Link>
          : onRemove && (
            <button type="button" className="btn ghost small" style={{ width: "auto" }} disabled={busy} onClick={onRemove}>
              {t("sparziel.remove")}
            </button>
          )}
      </div>
    </section>
  );
}

/**
 * Das Sparziel auf der Startseite (B-593): lädt das eigene Ziel und zeigt den Streifen. Beiwerk wie die Notenstufe – ein
 * gescheiterter Abruf lässt den Streifen weg, die Startseite hängt nie an ihm.
 */
export function SparzielStart({ childId }: { childId: number }) {
  const goal = useAsync<SavingsGoal | undefined>(() => api.mySavingsGoal(), [childId]);
  const action = useAction(useErrorMessage());
  async function entfernen() {
    await action.run(async () => { await api.clearMySavingsGoal(); goal.reload(); });
  }
  return (
    <>
      <SparzielStreifen goal={goal.data} busy={action.busy} onRemove={entfernen} />
      <StatusBanner message={action.message} />
    </>
  );
}
