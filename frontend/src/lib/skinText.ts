import { useTranslation } from "react-i18next";
import { de } from "../i18n/de";
import type { Skin } from "./skins";

/**
 * Name und Werbezeile eines Skins in der Sprache der Arcade (B-304). Die Texte kommen über die Skin-Id aus den
 * Ressourcen; `lib/skins.ts` bleibt der visuelle Katalog (Ids, Emoji, Farbe, Kosten) und trägt seine deutschen
 * Texte nur noch als Rückfall für eine Id, die die Ressourcen nicht kennen – nie ein Schlüsselname im Bild.
 */
type SkinId = keyof typeof de.sohn.skins.catalog;
export const isCatalogSkin = (id: string): id is SkinId => Object.prototype.hasOwnProperty.call(de.sohn.skins.catalog, id);

export function useSkinText() {
  const { t } = useTranslation();
  return {
    name: (s: Skin): string => (isCatalogSkin(s.id) ? t(`skins.catalog.${s.id}.name`) : s.name),
    blurb: (s: Skin): string => (isCatalogSkin(s.id) ? t(`skins.catalog.${s.id}.blurb`) : s.blurb),
  };
}
