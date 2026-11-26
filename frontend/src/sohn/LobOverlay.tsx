import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { api } from "../lib/api";
import { useAction } from "../lib/useAction";
import { useDialogFocus } from "../lib/useDialogFocus";
import { useErrorMessage } from "../lib/chromeText";
import type { MyPraise } from "../lib/types";

/**
 * Das Lob der Eltern (B-377): beim Öffnen der Startseite einmal alle ungelesenen Lobe (höchstens fünf, neueste zuerst),
 * je mit Absendername. „Danke!“ stempelt die gezeigten per Server (Entscheidung 5) und schließt. Lob ist kein Pflichtweg:
 * scheitert das Laden, erscheint nichts; scheitert der Stempel, schließt das Overlay trotzdem und kommt beim nächsten
 * Öffnen wieder – keine Schleife, die das Kind festhält. Der Lobtext ist Nutzerinhalt und reist als reiner Text.
 */
export function LobOverlay({ childId }: { childId: number }) {
  const [lobe, setLobe] = useState<MyPraise[]>([]);

  useEffect(() => {
    let aktiv = true;
    api.myPraise().then((l) => { if (aktiv) setLobe(l); }, () => {});
    return () => { aktiv = false; };
  }, [childId]);

  // Erst eingehängt, wenn es etwas zu zeigen gibt: die Fokus-Falle des Dialogs greift beim Einhängen.
  return lobe.length > 0 ? <LobDialog lobe={lobe} onClose={() => setLobe([])} /> : null;
}

function LobDialog({ lobe, onClose }: { lobe: MyPraise[]; onClose: () => void }) {
  const { t } = useTranslation();
  const action = useAction(useErrorMessage());
  const dialog = useRef<HTMLDivElement | null>(null);
  const knopf = useRef<HTMLButtonElement | null>(null);
  // Escape schließt ohne Stempel: das Lob bleibt ungelesen und kommt beim nächsten Öffnen wieder (Entscheidung 5).
  useDialogFocus(dialog, onClose, knopf);

  async function danke() {
    // Ohne `okText`: das Schließen ist die Rückmeldung. Scheitert der Stempel, schließt es trotzdem (siehe oben).
    await action.run(() => api.markPraiseSeen(lobe.map((l) => l.id)));
    onClose();
  }

  const titel = lobe.length === 1 ? t("lob.title", { name: lobe[0].supervisorName }) : t("lob.titleMany");
  return (
    <div className="lob-overlay">
      <div ref={dialog} className="card lob-karte" role="dialog" aria-modal="true" aria-labelledby="lob-titel">
        <div aria-hidden="true" style={{ fontSize: 40, textAlign: "center" }}>👏</div>
        <h2 id="lob-titel" className="screen-title" style={{ textAlign: "center" }}>{titel}</h2>
        <ul className="lob-liste">
          {lobe.map((l) => (
            <li key={l.id}>
              <p style={{ margin: 0 }}>{l.text}</p>
              {lobe.length > 1 && <span className="sub">{t("lob.title", { name: l.supervisorName })}</span>}
            </li>
          ))}
        </ul>
        <button ref={knopf} type="button" className="btn lime" disabled={action.busy} onClick={danke}>{t("lob.thanks")}</button>
      </div>
    </div>
  );
}
