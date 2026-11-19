import { createContext, useContext, type ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { ApiError, errorMessage } from "./api";

/**
 * Sprache geteilter Komponenten (B-305). `LetterBoxes`, `ClozePrompt` & Co. rendert die Arcade **und** die
 * Übungs-Vorschau des Vater-Webs. Das Vater-Web ist nicht übersetzt (B-85, Entscheidung 3) – folgten die Bausteine
 * der globalen Sprache, stünde ein Vater am Gerät eines englischen Kindes in einer halb englischen Vorschau.
 * Darum folgen sie der Wahl nur innerhalb von {@link ArcadeScope} und sprechen außerhalb fest Deutsch.
 */
const InArcade = createContext(false);

/** Markiert den Teilbaum der Sohn-Arcade; nur hier folgen geteilte Komponenten der Sprachwahl. */
export function ArcadeScope({ children }: { children: ReactNode }) {
  return <InArcade.Provider value={true}>{children}</InArcade.Provider>;
}

/** `t` für geteilte Komponenten: in der Arcade deren Sprache, sonst Deutsch. */
export function useChromeT() {
  const inArcade = useContext(InArcade);
  const { t, i18n } = useTranslation();
  return inArcade ? t : (i18n.getFixedT("de") as typeof t);
}

/**
 * `errorMessage` in der Sprache der Arcade. Die Codes, die ein Kind erreichen, haben einen eigenen Satz je Sprache
 * (`problem.*`; Deutsch wortgleich mit `errorMessage`, damit dieselbe Sperre in Arcade und Vater-Web gleich heißt).
 * Alles Übrige ist auf Deutsch genau `errorMessage` und auf Englisch der `detail`-Text des Servers, der ohnehin
 * englisch ist – statt eines deutschen Satzes in einer englischen Arcade.
 */
export function useErrorMessage(): (e: unknown) => string {
  // Die Sprache wird beim Aufruf über `i18n.language` gelesen; `i18n.t` folgt ihr ebenso.
  const { i18n } = useTranslation();
  return (e) => {
    if (e instanceof ApiError && e.code && isProblemKey(e.code)) return i18n.t(`problem.${e.code}`);
    if (i18n.language !== "en") return errorMessage(e);
    if (e instanceof ApiError) return e.traceId ? `${e.message} (Ref: ${e.traceId})` : e.message;
    return e instanceof Error ? e.message : String(e);
  };
}

const PROBLEM_KEYS = [
  "test_attempts_exhausted", "exercise_empty", "no_checkable_content", "plan_inactive",
  // B-415: Sperren, die nur das Kind auslöst.
  "skin_already_unlocked", "shop_insufficient_stock", "media_no_alternative", "media_not_on_card",
  "interest_wish_limit_reached", "concurrency_conflict",
  // B-499: 400er aus Kauf und Einlösen (`insufficient_inventory` erreicht auch den Vater, darum ohne Anrede).
  "insufficient_gems", "insufficient_coins", "shop_listing_inactive", "insufficient_inventory",
  // Bonus-Training (B-533): die Sperren des Kindes; B-543: der volle Deckel vor einer Übung fester Form; B-538: heute
  // nichts mehr wert; B-545: die Übung änderte sich während der Runde.
  "extra_training_locked", "extra_exercise_done", "extra_run_completed", "extra_cap_reached", "extra_run_stale",
  // B-527: die Tagestruhe ist schon offen (Doppeltipp, zweiter Tab).
  "daily_box_none",
  // B-635: leichter geht nur die aktuelle Karte und nur bis zur leichtesten Form.
  "cannot_ease",
  // B-628: im laufenden Test wird erst auf dem Ergebnisblatt gemeldet.
  "feedback_during_test",
] as const;
type ProblemKey = (typeof PROBLEM_KEYS)[number];
const isProblemKey = (code: string): code is ProblemKey => (PROBLEM_KEYS as readonly string[]).includes(code);
