import { useChromeT } from "../lib/chromeText";

/** Ob das Gerät hauptsächlich per Finger bedient wird – nur dort hilft der Hinweis aufs lange Drücken (Entscheidung 6). */
const istTouch = () => typeof window !== "undefined" && window.matchMedia?.("(pointer: coarse)").matches === true;

/**
 * B-661: eine Leiste mit den Sonderzeichen der getippten Sprache, an der Geräte-Tastatur vorbei. Ohne Zeichen keine
 * Leiste. Welche Zeichen, entscheidet der Aufrufer über `leistenZeichen` – die Leiste selbst kennt kein Wort.
 *
 * Ein Knopf darf dem Feld den Fokus nicht nehmen: am Handy klappte sonst nach jedem Zeichen die Tastatur zu (dieselbe
 * Falle wie Review B-526). Darum fangen `onPointerDown` und `onMouseDown` das Fokussieren ab (Chrome auf Windows-Touch
 * schickt ein spätes `mousedown`); das Einsetzen läuft im `onClick`, damit auch Tastatur und Screenreader die Knöpfe bedienen.
 */
export function SonderzeichenLeiste({ zeichen, onZeichen, gesperrt = false }: {
  zeichen: readonly string[];
  onZeichen: (zeichen: string) => void;
  gesperrt?: boolean;
}) {
  const t = useChromeT();
  if (zeichen.length === 0) return null;
  return (
    <div className="sonderzeichen">
      <div className="sonderzeichen-leiste" role="group" aria-label={t("play.specialChars")}>
        {zeichen.map((z) => (
          <button key={z} type="button" className="sonderzeichen-taste" disabled={gesperrt}
            aria-label={t("play.specialChar", { char: z })}
            onPointerDown={(e) => e.preventDefault()} onMouseDown={(e) => e.preventDefault()}
            onClick={() => onZeichen(z)}>{z}</button>
        ))}
      </div>
      {istTouch() && <div className="sub sonderzeichen-tipp">{t("play.longPressHint")}</div>}
    </div>
  );
}
