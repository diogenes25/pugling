import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { pointKindLabel } from "../lib/labels";
import type { Paged, WalletBalance, WalletEntry } from "../lib/types";

export function SohnKonto() {
  const wallet = useAsync<WalletBalance>(() => api.wallet(), []);
  const history = useAsync<Paged<WalletEntry>>(() => api.walletEntries(), []);

  const coins = wallet.data?.coins ?? 0;
  const gems = wallet.data?.gems ?? 0;

  return (
    <div className="sohn-body">
      <div className="row">
        <span className="screen-title" style={{ margin: 0 }}>Mein Konto</span>
        <span className="chip" style={{ marginLeft: "auto" }}>🪙<b className="tabnum">{coins}</b></span>
        <span className="chip">💎<b className="tabnum">{gems}</b></span>
      </div>
      <p className="sub">Fürs Lernen verdienst du 🪙 Münzen und 💎 Gems. Münzen gibst du im <b>🛒 Shop</b> für
        echte Belohnungen aus, Gems im <b>🎭 Skins</b>-Shop für Charaktere.</p>

      <h3 className="screen-title" style={{ fontSize: 18 }}>Verlauf</h3>
      {history.loading ? <div className="loading">Lade…</div> : history.error ? <div className="banner err">{history.error}</div> : (
        <div className="list">
          {history.data?.items.map((e) => (
            <div key={e.id} className="row" style={{ justifyContent: "space-between", padding: "5px 0" }}>
              <span className="sub">{pointKindLabel(e.kind)}{e.reason ? ` · ${e.reason}` : ""}</span>
              <b className="tabnum" style={{ color: e.amount < 0 ? "var(--danger,#ff6b6b)" : "var(--lime,#8bd450)" }}>
                {e.amount > 0 ? `+${e.amount}` : e.amount}</b>
            </div>
          ))}
          {history.data?.items.length === 0 && <p className="sub">Noch keine Buchungen.</p>}
        </div>
      )}
    </div>
  );
}
