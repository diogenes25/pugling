import { useState } from "react";
import { api } from "../lib/api";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import { formatDate } from "../lib/datum";
import { StatusBanner } from "../components/StatusBanner";
import { ListenAusfall } from "../components/ListenAusfall";
import { Pflicht } from "../components/Pflicht";
import type { Paged, Praise } from "../lib/types";

/** Höchstlänge eines Lobs – der Server prüft dieselbe Grenze nach dem Trimmen (B-377, Entscheidung 6). */
export const LOB_MAX = 140;

/** Vier fertige Sätze (Entscheidung 6): deutsche Produkt-Strings, sie landen als gewöhnlicher Text im Feld. */
export const LOB_SAETZE = [
  "Super gemacht – ich bin stolz auf dich!",
  "Klasse, dass du heute drangeblieben bist!",
  "Toll gelernt! Weiter so!",
  "Danke, dass du dir so viel Mühe gibst!",
] as const;

/**
 * Das Lob-Panel einer Kind-Zeile in „Heute“ (B-377): vier fertige Sätze, ein Feld mit höchstens 140 Zeichen und
 * „Senden“, das bei leerem Feld gesperrt ist. Darunter die eigenen letzten Lobe an dieses Kind mit „gelesen am“
 * (Entscheidung 7: nur die eigenen) – das schließt den Kreis „Erfolge erreichen den Vater“.
 */
export function LobPanel({ id, childId, name }: { id?: string; childId: number; name: string }) {
  const [text, setText] = useState("");
  const action = useAction();
  const gesendet = useAsync<Paged<Praise>>(() => api.praiseSent(childId, 3), [childId]);

  async function senden() {
    const t = text.trim();
    if (!t) return;
    const ok = await action.run(async () => { await api.sendPraise(childId, t); }, `Lob an ${name} gesendet.`);
    if (ok) { setText(""); gesendet.reload(); }
  }

  const feldId = `lob-text-${childId}`;
  return (
    <div id={id} className="lob-panel" style={{ display: "grid", gap: 8, marginTop: 8 }}>
      <div className="row" style={{ gap: 6, flexWrap: "wrap" }} role="group" aria-label="Fertige Sätze">
        {LOB_SAETZE.map((s) => (
          <button key={s} type="button" className="btn ghost small" style={{ width: "auto" }} onClick={() => setText(s)}>{s}</button>
        ))}
      </div>
      <label htmlFor={feldId}>Dein Lob an {name} <Pflicht /></label>
      <input id={feldId} type="text" maxLength={LOB_MAX} value={text} onChange={(e) => setText(e.target.value)} />
      <div className="row" style={{ gap: 8, alignItems: "center" }}>
        <span className="muted" style={{ fontSize: 13 }}>{text.trim().length} / {LOB_MAX}</span>
        <button type="button" className="btn lime inline-btn" style={{ width: "auto", marginLeft: "auto" }}
          disabled={action.busy || !text.trim()} onClick={senden}>Senden</button>
      </div>
      <StatusBanner message={action.message} />
      <ListenAusfall was="Deine letzten Lobe" fehler={gesendet.error} laedt={gesendet.loading} onRetry={gesendet.reload}
        schluessel={childId} />
      {gesendet.data && gesendet.data.items.length > 0 && (
        <ul aria-label={`Deine Lobe an ${name}`} style={{ margin: 0, paddingLeft: 18 }}>
          {gesendet.data.items.map((p) => (
            <li key={p.id}>
              „{p.text}“ <span className="muted">· {formatDate(p.createdAt)} · {p.seenAt ? `gelesen am ${formatDate(p.seenAt)}` : "noch nicht gelesen"}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
