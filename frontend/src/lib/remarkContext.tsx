/**
 * Anmerkungen beim Testen – der Kontext-Schnappschuss.
 *
 * Der Wert einer Anmerkung steckt nicht im Text, sondern im **Wo**: Route, Bereich, ausgewähltes Kind,
 * offene Übung. Genau das schreibt beim Testen niemand mit. Dieser Provider hält es bereit, damit das
 * Widget beim Absenden nur noch abgreifen muss.
 *
 * Zwei Wege, wie der Kontext entsteht:
 * 1. **Automatisch aus der Route** – Bereich und, wo die URL sie hergibt, die Kind-Id. Das deckt den
 *    Großteil ab, ohne dass ein einziger Screen etwas tun muss.
 * 2. **Beiträge einzelner Screens** über {@link useRemarkContribution} – dort, wo die Auswahl nur im
 *    State steht (offene Übung, aktiver Filter).
 *
 * Bewusst **ref-basiert statt State**: Ein Kontextwechsel darf keine Render-Kaskade auslösen. Gelesen
 * wird nur im Moment des Absendens, und dann genügt ein Schnappschuss.
 */
import { createContext, useContext, useEffect, useId, useMemo, useRef, type ReactNode } from "react";
import { useLocation } from "react-router-dom";
import { recentErrorsJson } from "./remarks";

/** Was ein Screen zum Kontext beisteuern kann. Alles optional – wer nichts weiß, meldet nichts. */
export type RemarkContribution = {
  childId?: number;
  exerciseId?: number;
  studyPlanId?: number;
  planPositionId?: number;
  /**
   * Freier Zustands-Schnappschuss (aktiver Filter, offenes Modal, Sortierung). **Nur IDs und
   * Filterwerte** – niemals geladene Entitäten und nichts, was ein Nutzer eingetippt hat.
   */
  extra?: Record<string, unknown>;
};

/** Der fertige Schnappschuss, wie ihn der POST auf `api/v1/remarks` erwartet. */
export type RemarkSnapshot = {
  route: string;
  appArea: string;
  childId?: number;
  exerciseId?: number;
  studyPlanId?: number;
  planPositionId?: number;
  contextJson: string | null;
  recentErrorsJson: string | null;
};

type Store = {
  contributions: Map<string, RemarkContribution>;
  location: { pathname: string; search: string };
};

const RemarkCtx = createContext<Store | null>(null);

/** Leitet den Anwendungsbereich aus dem Pfad ab – explizit, statt ihn später aus der Route zu raten. */
function areaOf(pathname: string): string {
  if (pathname.startsWith("/sohn")) return "sohn";
  if (pathname.startsWith("/vater")) return "vater";
  return "public";
}

/**
 * Liest die Bezüge, die ohnehin in der URL stehen. Spart den meisten Screens jede Verkabelung – ein
 * Screen, der eine Id nur im State hält (offenes Modal, Auswahl), ergänzt sie über
 * {@link useRemarkContribution}.
 *
 * Die Routen dazu (siehe `VaterApp`/`SohnApp`):
 * `/vater/kind/:childId[/…]` · `?childId=` · `/vater/plan/:planId` · `/sohn/practice/:positionId` ·
 * `/sohn/test/:positionId`.
 */
function idsFromUrl(pathname: string, search: string): RemarkContribution {
  const query = new URLSearchParams(search);
  const num = (value: string | null | undefined) => (value && /^\d+$/.test(value) ? Number(value) : undefined);

  return {
    childId: num(/\/kind\/(\d+)/.exec(pathname)?.[1]) ?? num(query.get("childId")),
    studyPlanId: num(/\/plan\/(\d+)/.exec(pathname)?.[1]) ?? num(query.get("planId")),
    // Üben und Abschlusstest adressieren beide die Position.
    planPositionId: num(/\/(?:practice|test)\/(\d+)/.exec(pathname)?.[1]),
    exerciseId: num(query.get("exerciseId")),
  };
}

/** Hängt den Kontext-Speicher in den Baum. Gehört um die Routen, damit jeder Screen beitragen kann. */
export function RemarkContextProvider({ children }: { children: ReactNode }) {
  const location = useLocation();
  const store = useRef<Store>({ contributions: new Map(), location }).current;
  // Route im Ref mitführen: Der Schnappschuss liest sie erst beim Absenden, ein Re-Render ist unnötig.
  store.location = { pathname: location.pathname, search: location.search };
  return <RemarkCtx.Provider value={store}>{children}</RemarkCtx.Provider>;
}

/**
 * Meldet den Beitrag eines Screens an, solange er sichtbar ist. Beim Verlassen wird er automatisch
 * wieder abgemeldet – sonst schleppte eine Anmerkung die Übungs-Id von vorhin mit.
 *
 * Der Aufrufer muss das Objekt **nicht** memoisieren: Verglichen wird über den Inhalt.
 */
export function useRemarkContribution(contribution: RemarkContribution): void {
  const store = useContext(RemarkCtx);
  const id = useId();
  const serialized = JSON.stringify(contribution);

  useEffect(() => {
    if (!store) return; // Ohne Provider (z. B. isolierter Test-Render) ist das ein No-op.
    store.contributions.set(id, JSON.parse(serialized) as RemarkContribution);
    return () => {
      store.contributions.delete(id);
    };
  }, [store, id, serialized]);
}

/**
 * Liefert eine Funktion, die **im Moment des Aufrufs** den Kontext einsammelt. Bewusst eine Funktion
 * und kein Wert: Das Widget soll den Stand beim Absenden festhalten, nicht den beim Rendern.
 */
export function useRemarkSnapshot(): () => RemarkSnapshot {
  const store = useContext(RemarkCtx);

  return useMemo(
    () => () => {
      const { pathname, search } = store?.location ?? { pathname: "", search: "" };

      // Spätere Beiträge gewinnen: Ein verschachtelter Screen (Modal über Liste) ist der genauere.
      const merged: RemarkContribution = {};
      const extras: Record<string, unknown> = {};
      for (const c of store?.contributions.values() ?? []) {
        if (c.childId != null) merged.childId = c.childId;
        if (c.exerciseId != null) merged.exerciseId = c.exerciseId;
        if (c.studyPlanId != null) merged.studyPlanId = c.studyPlanId;
        if (c.planPositionId != null) merged.planPositionId = c.planPositionId;
        if (c.extra) Object.assign(extras, c.extra);
      }

      // Die URL ist der Rückfall für jeden Bezug, den kein Screen gemeldet hat. Ohne ihn blieben
      // Felder leer, die buchstäblich in der Adresszeile stehen – und der Antwort-Skill liest genau sie.
      const url = idsFromUrl(pathname, search);

      return {
        route: pathname + search,
        appArea: areaOf(pathname),
        childId: merged.childId ?? url.childId,
        exerciseId: merged.exerciseId ?? url.exerciseId,
        studyPlanId: merged.studyPlanId ?? url.studyPlanId,
        planPositionId: merged.planPositionId ?? url.planPositionId,
        contextJson: Object.keys(extras).length > 0 ? JSON.stringify(extras) : null,
        recentErrorsJson: recentErrorsJson(),
      };
    },
    [store],
  );
}
