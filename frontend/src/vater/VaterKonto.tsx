import { useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { FieldLabel } from "../components/InfoHint";
import { api } from "../lib/api";
import { useAction } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import { useChildSelection } from "../lib/useChildSelection";
import { pointKindLabel } from "../lib/labels";
import { PAGE_SIZE, Pager } from "../components/ListControls";
import type { ChildResponse, Currency, WalletEntry } from "../lib/types";

export function VaterKonto() {
  const children = useAsync<ChildResponse[]>(() => api.children(), []);
  // Vorauswahl aus `?childId=` (die Links vom Kind-Hub tragen sie), sonst das erste Kind.
  const { activeChild, select } = useChildSelection(children.data);

  return (
    <>
      <section>
        <h2 className="h-section">Konto</h2>
        <p className="muted">Münz-/Gem-Stand und Buchungsverlauf deines Kindes. Ausgegeben werden die Münzen
          im <b>Familien-Shop</b> (dort verwaltest du Artikel, Käufe und Aktivierungen).</p>
        {children.loading ? <div className="loading">Lade…</div>
          : children.error ? <div className="banner err">{children.error}</div>
          : children.data && children.data.length > 0 ? (
            <div className="field" style={{ maxWidth: 320 }}>
              <label htmlFor="konto-child">Kind</label>
              <select id="konto-child" value={activeChild ?? ""} onChange={(e) => select(Number(e.target.value))}>
                {children.data.map((c) => <option key={c.id} value={c.id}>{c.name} (#{c.id})</option>)}
              </select>
            </div>
          ) : <div className="banner">Lege zuerst ein Kind an (unter „Kinder &amp; Heute").</div>}
      </section>

      {activeChild !== null && <AccountView key={activeChild} childId={activeChild} />}
    </>
  );
}

type AccountData = { coins: number; gems: number; items: WalletEntry[]; total: number };

function AccountView({ childId }: { childId: number }) {
  // Buchungsverlauf server-paginiert (Salden sind über alle Zeilen berechnet, bleiben also stabil).
  const [acctSkip, setAcctSkip] = useState(0);
  const account = useAsync<AccountData>(() => api.childPoints(childId, { skip: acctSkip, take: PAGE_SIZE }), [childId, acctSkip]);

  return (
    <>
      <section className="vater-grid">
        <div className="card">
          <div className="muted">Münzstand</div>
          <div className="h-section" style={{ color: (account.data?.coins ?? 0) < 0 ? "var(--danger, #c23a1d)" : "inherit" }}>
            🪙 {account.data?.coins ?? "…"}
          </div>
          {(account.data?.coins ?? 0) < 0 && <div className="muted" style={{ fontSize: 13 }}>Schulden aus verpasster Pflicht – schenke Münzen zum Ausgleich.</div>}
        </div>
        <div className="card"><div className="muted">Gems</div><div className="h-section">💎 {account.data?.gems ?? "…"}</div></div>
      </section>

      <GrantForm childId={childId} onGranted={account.reload} />

      <section>
        <h3 className="h-section">Buchungsverlauf {account.data ? `(${account.data.total})` : ""}</h3>
        {account.loading && account.data === null ? <div className="loading">Lade…</div> : account.error ? <div className="banner err">{account.error}</div> : (
          <div style={{ overflowX: "auto" }}>
            <table className="table">
              <thead><tr><th>Datum</th><th>Kategorie</th><th>Grund</th><th className="num">Münzen</th></tr></thead>
              <tbody>
                {account.data?.items.map((e) => (
                  <tr key={e.id}>
                    <td className="muted">{new Date(e.createdAt).toLocaleDateString()}</td>
                    <td>{pointKindLabel(e.kind)}</td>
                    <td className="muted">{e.reason}</td>
                    <td className="num" style={{ color: e.amount < 0 ? "var(--danger, #c23a1d)" : "inherit" }}>
                      {e.amount > 0 ? `+${e.amount}` : e.amount}</td>
                  </tr>
                ))}
                {account.data?.items.length === 0 && <tr><td colSpan={4} className="muted">Noch keine Buchungen.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
        {account.data && <Pager skip={acctSkip} take={PAGE_SIZE} total={account.data.total} onSkip={setAcctSkip} />}
      </section>
    </>
  );
}

/**
 * Verschenkt Münzen oder Gems an das Kind – als Belohnung außerhalb der App oder als Ausgleich für
 * Malus-Schulden. Positiver Betrag; die Währung entscheidet über die Buchungsart (Münzen bzw. Gems).
 */
function GrantForm({ childId, onGranted }: { childId: number; onGranted: () => void }) {
  const [amount, setAmount] = useState(20);
  const [currency, setCurrency] = useState<Currency>("Coins");
  const [reason, setReason] = useState("");
  const action = useAction();

  async function grant() {
    if (amount <= 0) { action.fail("Betrag muss größer als 0 sein."); return; }
    const label = currency === "Gems" ? "💎 Gems" : "🪙 Münzen";
    const ok = await action.run(
      () => api.grantPoints(childId, amount, reason.trim() || "Geschenk vom Papa", currency),
      `${amount} ${label} verschenkt.`);
    if (!ok) return;
    setReason("");
    onGranted();
  }

  return (
    <section>
      <h3 className="h-section">Verschenken</h3>
      <p className="muted">Belohne dein Kind unabhängig von der App oder gleiche Malus-Schulden aus.</p>
      <div className="row" style={{ gap: 12, alignItems: "flex-end", flexWrap: "wrap" }}>
        <div className="field" style={{ maxWidth: 120 }}>
          <FieldLabel htmlFor="grant-amount" topic="grantAmount">Betrag</FieldLabel>
          <input id="grant-amount" type="number" min={1} value={amount} onChange={(e) => setAmount(Number(e.target.value))} />
        </div>
        <div className="field" style={{ maxWidth: 150 }}>
          <FieldLabel htmlFor="grant-currency" topic="grantCurrency">Währung</FieldLabel>
          <select id="grant-currency" value={currency} onChange={(e) => setCurrency(e.target.value as Currency)}>
            <option value="Coins">🪙 Münzen</option>
            <option value="Gems">💎 Gems</option>
          </select>
        </div>
        <div className="field" style={{ flex: 1, minWidth: 200 }}>
          <label htmlFor="grant-reason">Grund (optional)</label>
          <input id="grant-reason" type="text" value={reason} placeholder="z. B. Zimmer aufgeräumt" onChange={(e) => setReason(e.target.value)} />
        </div>
        <button className="btn" disabled={action.busy} onClick={grant}>
          {action.busy ? "Verschenke…" : "Verschenken"}
        </button>
      </div>
      <StatusBanner message={action.message} />
    </section>
  );
}
