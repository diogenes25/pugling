import { useState } from "react";
import { useAuth } from "../lib/auth";
import { SKINS, getSkinState, selectSkin, unlockSkin } from "../lib/skins";
import { Mascot } from "../components/Mascot";
import { useSohn } from "./SohnApp";

export function SohnSkins() {
  const { signOut } = useAuth();
  const { childId, balance, skin, setSkin } = useSohn();
  const [state, setState] = useState(() => getSkinState(childId));
  const [msg, setMsg] = useState<string | null>(null);

  function choose(id: string) {
    const s = SKINS.find((x) => x.id === id)!;
    if (state.unlocked.includes(id)) {
      const next = selectSkin(childId, id);
      setState(next);
      setSkin(s);
      setMsg(`${s.name} ausgerüstet!`);
    } else {
      const { state: next, ok } = unlockSkin(childId, id, balance);
      if (ok) {
        setState(next);
        setSkin(s);
        setMsg(`${s.name} freigeschaltet & ausgerüstet! 🎉`);
      } else {
        setMsg(`Noch ${s.cost - balance} 🪙 bis ${s.name}.`);
      }
    }
    setTimeout(() => setMsg(null), 1600);
  }

  return (
    <div className="sohn-body">
      <div className="row">
        <span className="screen-title" style={{ margin: 0 }}>Charaktere</span>
        <span className="chip" style={{ marginLeft: "auto" }}>🪙<b className="tabnum">{balance}</b></span>
      </div>
      <p className="sub">Schalte Skins mit Münzen frei, die du beim Lernen verdienst. Dein Held erscheint überall in der App.</p>

      <Mascot skin={skin} mood="hyped" size={100} />

      <div className="skin-grid">
        {SKINS.map((s) => {
          const owned = state.unlocked.includes(s.id);
          const selected = state.selected === s.id;
          return (
            <button
              type="button"
              key={s.id}
              className={`skin${selected ? " on" : ""}${owned ? "" : " locked"}`}
              onClick={() => choose(s.id)}
            >
              <div className="face" style={{ background: s.gradient }}>{s.emoji}</div>
              <div className="nm">{s.name}</div>
              <div className="sub" style={{ fontSize: 11, minHeight: 26 }}>{s.blurb}</div>
              {selected ? <span className="pill cyan">ausgerüstet</span>
                : owned ? <span className="pill lime">wählen</span>
                : <span className="pill gold">🪙 {s.cost}</span>}
            </button>
          );
        })}
      </div>

      {msg && <div className="toast">{msg}</div>}
      <button type="button" className="btn ghost" onClick={signOut} style={{ marginTop: 6 }}>Abmelden</button>
    </div>
  );
}
