import "i18next";
import type { de } from "./de";

/**
 * Typisierte Schlüssel (B-294): `t("home.laoding")` ist damit ein Übersetzungsfehler, kein Bild mit dem
 * Schlüssel als Text – „mechanische Tore statt Disziplin“ für die eine Stelle, die ein Paritätstest nicht sieht
 * (Verwendung gegen Ressource). Deutsch ist die Quelle der Schlüssel; `en.ts` muss sie spiegeln (`i18n.test.ts`).
 */
declare module "i18next" {
  interface CustomTypeOptions {
    defaultNS: "sohn";
    resources: typeof de;
    returnNull: false;
  }
}
