import { useState } from "react";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { pointKindLabel, redemptionStatusLabel } from "../lib/labels";
import type { RewardsView, Wallet } from "../lib/types";

export function SohnKonto() {
  const rewards = useAsync<RewardsView>(() => api.myRewards(), []);
  const wallet = useAsync<Wallet>(() => api.wallet(), []);
  const [busy, setBusy] = useState<number | null>(null);
  const [msg, setMsg] = useState<string | null>(null);

  function flash(text: string) { setMsg(text); setTimeout(() => setMsg(null), 2200); }

  async function redeem(rewardId: number, title: string) {
    if (busy !== null) return;
    setBusy(rewardId);
    try {
      await api.redeemReward(rewardId);
      rewards.reload();
      flash(`„${title}" angefragt – warte auf Papa! 🙏`);
    } catch (e) {
      flash(errorMessage(e));
    } finally {
      setBusy(null);
    }
  }

  const balance = rewards.data?.balance ?? wallet.data?.balance ?? 0;

  return (
    <div className="sohn-body">
      <div className="row">
        <span className="screen-title" style={{ margin: 0 }}>Mein Konto</span>
        <span className="chip" style={{ marginLeft: "auto" }}>🪙<b className="tabnum">{balance}</b></span>
      </div>
      <p className="sub">Verdiente Münzen gegen echte Belohnungen eintauschen (z.B. Fernsehzeit). Du fragst
        an – Papa gibt frei, dann werden die Münzen abgebucht.</p>

      <h3 className="screen-title" style={{ fontSize: 18 }}>Belohnungen holen</h3>
      {rewards.loading ? <div className="loading">Lade…</div> : rewards.error ? <div className="banner err">{rewards.error}</div>
        : rewards.data && rewards.data.available.length === 0 ? <p className="sub">Papa hat noch keine Belohnungen eingerichtet.</p> : (
          <div className="skin-grid">
            {rewards.data?.available.map((r) => (
              <div key={r.id} className={`skin${r.affordable ? "" : " locked"}`}>
                <div className="nm">{r.title}</div>
                <div className="pill gold" style={{ marginTop: 4 }}>🪙 {r.cost}</div>
                {r.alreadyRequested
                  ? <span className="pill" style={{ marginTop: 6 }}>angefragt ⏳</span>
                  : <button type="button" className="btn inline-btn" style={{ width: "auto", marginTop: 6 }}
                      disabled={busy !== null} onClick={() => redeem(r.id, r.title)}>
                      {r.affordable ? "Einlösen" : `noch ${r.cost - balance} 🪙`}
                    </button>}
              </div>
            ))}
          </div>
        )}

      {rewards.data && rewards.data.redemptions.length > 0 && (
        <>
          <h3 className="screen-title" style={{ fontSize: 18 }}>Meine Anfragen</h3>
          <div className="list">
            {rewards.data.redemptions.map((r) => (
              <div key={r.id} className="row" style={{ justifyContent: "space-between", padding: "6px 0" }}>
                <span>{r.title} <span className="sub">· 🪙 {r.cost}</span></span>
                <span className={`pill ${r.status === "Approved" ? "lime" : r.status === "Rejected" ? "" : "gold"}`}>
                  {redemptionStatusLabel(r.status)}</span>
              </div>
            ))}
          </div>
        </>
      )}

      <h3 className="screen-title" style={{ fontSize: 18 }}>Verlauf</h3>
      {wallet.loading ? <div className="loading">Lade…</div> : wallet.error ? <div className="banner err">{wallet.error}</div> : (
        <div className="list">
          {wallet.data?.entries.map((e) => (
            <div key={e.id} className="row" style={{ justifyContent: "space-between", padding: "5px 0" }}>
              <span className="sub">{pointKindLabel(e.kind)}{e.reason ? ` · ${e.reason}` : ""}</span>
              <b className="tabnum" style={{ color: e.amount < 0 ? "var(--danger,#ff6b6b)" : "var(--lime,#8bd450)" }}>
                {e.amount > 0 ? `+${e.amount}` : e.amount}</b>
            </div>
          ))}
          {wallet.data?.entries.length === 0 && <p className="sub">Noch keine Buchungen.</p>}
        </div>
      )}

      {msg && <div className="toast">{msg}</div>}
    </div>
  );
}
