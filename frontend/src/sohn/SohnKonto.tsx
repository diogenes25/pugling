import { useState } from "react";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { offerPeriodLabel, pointKindLabel, redemptionStatusLabel } from "../lib/labels";
import { confirmAction } from "../lib/ui";
import type { RewardsView, Wallet } from "../lib/types";

export function SohnKonto() {
  const rewards = useAsync<RewardsView>(() => api.myRewards(), []);
  const wallet = useAsync<Wallet>(() => api.wallet(), []);
  const [busy, setBusy] = useState<number | null>(null);
  const [msg, setMsg] = useState<string | null>(null);

  function flash(text: string) { setMsg(text); setTimeout(() => setMsg(null), 2200); }

  async function buy(rewardId: number, title: string, cost: number) {
    if (busy !== null) return;
    // Kauf ist sofort und unumkehrbar (Münzen weg) – bewusst gegentippen lassen.
    if (!confirmAction(`„${title}" für ${cost} 🪙 kaufen? Die Münzen sind dann weg.`)) return;
    setBusy(rewardId);
    try {
      await api.purchaseReward(rewardId);
      rewards.reload();
      wallet.reload();
      flash(`„${title}" gekauft! 🎉 Papa löst es bald ein.`);
    } catch (e) {
      flash(errorMessage(e));
    } finally {
      setBusy(null);
    }
  }

  const coins = rewards.data?.coins ?? wallet.data?.coins ?? 0;
  const gems = wallet.data?.gems ?? 0;

  return (
    <div className="sohn-body">
      <div className="row">
        <span className="screen-title" style={{ margin: 0 }}>Mein Konto</span>
        <span className="chip" style={{ marginLeft: "auto" }}>🪙<b className="tabnum">{coins}</b></span>
        <span className="chip">💎<b className="tabnum">{gems}</b></span>
      </div>
      <p className="sub">Mit 🪙 Münzen kaufst du echte Belohnungen bei Papa (z.B. Spielzeit). Du kaufst sofort –
        die Münzen sind gleich weg, und Papa erfüllt dann seinen Teil. 💎 Gems gibt's im Avatar-Shop.</p>

      <h3 className="screen-title" style={{ fontSize: 18 }}>Angebote kaufen</h3>
      {rewards.loading ? <div className="loading">Lade…</div> : rewards.error ? <div className="banner err">{rewards.error}</div>
        : rewards.data && rewards.data.available.length === 0 ? <p className="sub">Papa hat noch keine Angebote eingerichtet.</p> : (
          <div className="skin-grid">
            {rewards.data?.available.map((r) => {
              const soldOut = r.remainingThisPeriod <= 0;
              const buyable = r.affordable && !soldOut;
              return (
                <div key={r.id} className={`skin${buyable ? "" : " locked"}`}>
                  <div className="nm">{r.title}</div>
                  {(r.planTitle || r.exerciseTitle) && (
                    <div className="sub" style={{ marginTop: 2 }}>
                      🎯 {r.exerciseTitle ? `${r.planTitle} · ${r.exerciseTitle}` : r.planTitle}
                    </div>
                  )}
                  <div className="pill gold" style={{ marginTop: 4 }}>🪙 {r.cost}</div>
                  <div className="sub" style={{ marginTop: 4 }}>
                    {offerPeriodLabel(r.period)} · noch {r.remainingThisPeriod}/{r.quantity}
                  </div>
                  <button type="button" className="btn inline-btn" style={{ width: "auto", marginTop: 6 }}
                    disabled={busy !== null || !buyable} onClick={() => buy(r.id, r.title, r.cost)}>
                    {soldOut ? "diese Periode ausverkauft" : r.affordable ? "Kaufen" : `noch ${r.cost - coins} 🪙`}
                  </button>
                </div>
              );
            })}
          </div>
        )}

      {rewards.data && rewards.data.redemptions.length > 0 && (
        <>
          <h3 className="screen-title" style={{ fontSize: 18 }}>Meine Käufe</h3>
          <div className="list">
            {rewards.data.redemptions.map((r) => (
              <div key={r.id} className="row" style={{ justifyContent: "space-between", padding: "6px 0" }}>
                <span>{r.title} <span className="sub">· 🪙 {r.cost} · gekauft {new Date(r.purchasedAt).toLocaleDateString()}</span></span>
                <span className={`pill ${r.status === "Fulfilled" ? "lime" : r.status === "Cancelled" ? "" : "gold"}`}>
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

      {msg && <div className="toast" role="status" aria-live="polite">{msg}</div>}
    </div>
  );
}
