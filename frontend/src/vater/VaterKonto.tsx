import { useState } from "react";
import { api } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { pointKindLabel } from "../lib/labels";
import { PAGE_SIZE, Pager } from "../components/ListControls";
import type { ChildResponse, WalletEntry } from "../lib/types";

export function VaterKonto() {
  const children = useAsync<ChildResponse[]>(() => api.children(), []);
  const [childId, setChildId] = useState<number | null>(null);
  const activeChild = childId ?? children.data?.[0]?.id ?? null;

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
              <select id="konto-child" value={activeChild ?? ""} onChange={(e) => setChildId(Number(e.target.value))}>
                {children.data.map((c) => <option key={c.id} value={c.id}>{c.name} (#{c.id})</option>)}
              </select>
            </div>
          ) : <div className="banner">Lege zuerst ein Kind an (Übersicht).</div>}
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
        <div className="card"><div className="muted">Münzstand</div><div className="h-section">🪙 {account.data?.coins ?? "…"}</div></div>
        <div className="card"><div className="muted">Gems</div><div className="h-section">💎 {account.data?.gems ?? "…"}</div></div>
      </section>

      <section>
        <h3 className="h-section">Buchungsverlauf {account.data ? `(${account.data.total})` : ""}</h3>
        {account.loading ? <div className="loading">Lade…</div> : account.error ? <div className="banner err">{account.error}</div> : (
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
