import { useEffect } from "react";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { useSohn } from "./SohnApp";
import type { PlanResponse } from "../lib/types";

/**
 * Der Plan, mit dem die Arcade arbeitet: der gemerkte, solange es ihn unter den Plänen des Kindes noch gibt, sonst der
 * spielbare, sonst der erste (server-autoritative Affordance statt eigener Regel). Die Startseite zeigt alle Pläne
 * (B-606); gemerkt wird der Plan mit „Los“ an einer Position, gelesen von Üben, Test und Fortschritt – der Weg lud
 * vorher die gemerkte Id ungeprüft und zeigte bei einem gelöschten Plan die rohe Serverzeile (B-254). Weicht die Wahl
 * von der gemerkten Id ab, wird sie gemerkt.
 */
export function useChosenPlan() {
  const { planId, setPlanId } = useSohn();
  const plans = useAsync<PlanResponse[]>(() => api.plans(), []);
  const chosen: PlanResponse | undefined = plans.data
    ? plans.data.find((p) => p.id === planId) ?? plans.data.find((p) => p.isPlayable) ?? plans.data[0]
    : undefined;

  useEffect(() => {
    if (chosen && chosen.id !== planId) setPlanId(chosen.id);
  }, [chosen, planId, setPlanId]);

  return { plans, chosen };
}
