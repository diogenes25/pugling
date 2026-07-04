import { useEffect, useState } from "react";
import { api, errorMessage } from "../lib/api";
import { useAuth } from "../lib/auth";
import { SKINS, skinById } from "../lib/skins";
import { Mascot } from "../components/Mascot";
import { useSohn } from "./SohnApp";

export function SohnSkins() {
  const { signOut } = useAuth();
  const { gems, refreshWallet, skin, setSkin } = useSohn();
  const [owned, setOwned] = useState<string[]>([]);
  const [selected, setSelected] = useState<string>(skin.id);
  const [ready, setReady] = useState(false);
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);

  // Besitz & Auswahl sind server-autoritativ – beim Öffnen frisch laden. Bis der Zustand da ist,
  // bleibt `ready` false: Sonst gälten alle Skins kurz als "nicht besessen" und ein früher Tipp auf
  // einen bereits besessenen Skin würde fälschlich einen Kauf auslösen (Server-409).
  useEffect(() => {
    api.skins().then((s) => { setOwned(s.owned); setSelected(s.selected); })
      .catch((e) => setMsg(errorMessage(e)))
      .finally(() => setReady(true));
  }, []);

  function flash(text: string) {
    setMsg(text);
    setTimeout(() => setMsg(null), 1800);
  }

  async function choose(id: string) {
    if (busy || !ready) return; // erst handeln, wenn der Server-Besitz geladen ist
    const s = SKINS.find((x) => x.id === id)!;
    setBusy(true);
    try {
      // Bereits freigeschaltet -> nur ausrüsten. Sonst kaufen (Server bucht Gems ab).
      const state = owned.includes(id) ? await api.equipSkin(id) : await api.purchaseSkin(id);
      const bought = !owned.includes(id);
      setOwned(state.owned);
      setSelected(state.selected);
      setSkin(skinById(state.selected));
      refreshWallet(); // Gem-Stand im HUD real aktualisieren (nach Kauf niedriger)
      flash(bought ? `${s.name} freigeschaltet & ausgerüstet! 🎉` : `${s.name} ausgerüstet!`);
    } catch (e) {
      flash(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="sohn-body">
      <div className="row">
        <span className="screen-title" style={{ margin: 0 }}>Charaktere</span>
        <span className="chip" style={{ marginLeft: "auto" }}>💎<b className="tabnum">{gems}</b></span>
      </div>
      <p className="sub">Schalte Skins mit 💎 Gems frei, die du durch Boni beim Lernen verdienst (Combos, schnelle Antworten, Missionen). Dein Held erscheint überall in der App – auch auf anderen Geräten.</p>

      <Mascot skin={skin} mood="hyped" size={100} />

      {!ready ? <div className="loading">Lade Charaktere…</div> : (
      <div className="skin-grid">
        {SKINS.map((s) => {
          const isOwned = owned.includes(s.id);
          const isSelected = selected === s.id;
          return (
            <button
              type="button"
              key={s.id}
              className={`skin${isSelected ? " on" : ""}${isOwned ? "" : " locked"}`}
              onClick={() => choose(s.id)}
              disabled={busy}
            >
              <div className="face" style={{ background: s.gradient }}>{s.emoji}</div>
              <div className="nm">{s.name}</div>
              <div className="sub" style={{ fontSize: 11, minHeight: 26 }}>{s.blurb}</div>
              {isSelected ? <span className="pill cyan">ausgerüstet</span>
                : isOwned ? <span className="pill lime">wählen</span>
                : <span className="pill gold">💎 {s.cost}</span>}
            </button>
          );
        })}
      </div>
      )}

      {msg && <div className="toast">{msg}</div>}
      <button type="button" className="btn ghost" onClick={signOut} style={{ marginTop: 6 }}>Abmelden</button>
    </div>
  );
}
