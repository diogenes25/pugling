import { useState } from "react";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { pointKindLabel, redemptionStatusLabel } from "../lib/labels";
import type { ChildResponse, RedemptionDef, Wallet } from "../lib/types";

export function VaterKonto() {
  const children = useAsync<ChildResponse[]>(() => api.children(), []);
  const [childId, setChildId] = useState<number | null>(null);
  const activeChild = childId ?? children.data?.[0]?.id ?? null;

  return (
    <>
      <section>
        <h2 className="h-section">Konto</h2>
        <p className="muted">Punktestand, Buchungsverlauf und offene Einlöse-Anfragen deines Kindes.
          Genehmigst du eine Anfrage, werden die Münzen abgebucht.</p>
        {children.loading ? <div className="loading">Lade…</div>
          : children.error ? <div className="banner err">{children.error}</div>
          : children.data && children.data.length > 0 ? (
            <div className="field" style={{ maxWidth: 320 }}>
              <label>Kind</label>
              <select title="Kind" value={activeChild ?? ""} onChange={(e) => setChildId(Number(e.target.value))}>
                {children.data.map((c) => <option key={c.id} value={c.id}>{c.name} (#{c.id})</option>)}
              </select>
            </div>
          ) : <div className="banner">Lege zuerst ein Kind an (Übersicht).</div>}
      </section>

      {activeChild !== null && <AccountView key={activeChild} childId={activeChild} />}
    </>
  );
}

function AccountView({ childId }: { childId: number }) {
  const account = useAsync<Wallet>(() => api.childAccount(childId), [childId]);
  const redemptions = useAsync<RedemptionDef[]>(() => api.redemptionsFor(childId), [childId]);
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);

  function flash(ok: boolean, text: string) { setMsg({ ok, text }); setTimeout(() => setMsg(null), 2500); }

  async function decide(id: number, approve: boolean) {
    setBusy(true);
    try {
      if (approve) await api.approveRedemption(childId, id);
      else await api.rejectRedemption(childId, id);
      account.reload();
      redemptions.reload();
      flash(true, approve ? "Genehmigt – Münzen abgebucht." : "Abgelehnt.");
    } catch (err) {
      flash(false, errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  const open = redemptions.data?.filter((r) => r.status === "Requested") ?? [];
  const decided = redemptions.data?.filter((r) => r.status !== "Requested") ?? [];

  return (
    <>
      <section className="vater-grid">
        <div className="card"><div className="muted">Münzstand</div><div className="h-section">🪙 {account.data?.balance ?? "…"}</div></div>
        <div className="card"><div className="muted">Offene Anfragen</div><div className="h-section">{open.length}</div></div>
      </section>

      {msg && <div className={`banner ${msg.ok ? "ok" : "err"}`}>{msg.text}</div>}

      <section>
        <h3 className="h-section">Offene Einlöse-Anfragen</h3>
        {redemptions.loading ? <div className="loading">Lade…</div> : redemptions.error ? <div className="banner err">{redemptions.error}</div>
          : open.length === 0 ? <p className="muted">Keine offenen Anfragen.</p> : (
            <div style={{ overflowX: "auto" }}>
              <table className="table">
                <thead><tr><th>Prämie</th><th>Preis</th><th>Angefragt</th><th>Aktion</th></tr></thead>
                <tbody>
                  {open.map((r) => (
                    <tr key={r.id}>
                      <td>{r.title}</td>
                      <td>🪙 {r.cost}</td>
                      <td className="muted">{new Date(r.requestedAt).toLocaleDateString()}</td>
                      <td style={{ whiteSpace: "nowrap" }}>
                        <button type="button" className="btn inline-btn" style={{ width: "auto" }} disabled={busy}
                          onClick={() => decide(r.id, true)}>Genehmigen</button>{" "}
                        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy}
                          onClick={() => decide(r.id, false)}>Ablehnen</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
      </section>

      {decided.length > 0 && (
        <section>
          <h3 className="h-section">Entschieden</h3>
          <div style={{ overflowX: "auto" }}>
            <table className="table">
              <thead><tr><th>Prämie</th><th>Preis</th><th>Status</th><th>Am</th></tr></thead>
              <tbody>
                {decided.map((r) => (
                  <tr key={r.id}>
                    <td>{r.title}</td>
                    <td>🪙 {r.cost}</td>
                    <td>{r.status === "Approved" ? <span className="pill lime">{redemptionStatusLabel(r.status)}</span>
                      : <span className="pill">{redemptionStatusLabel(r.status)}</span>}</td>
                    <td className="muted">{r.decidedAt ? new Date(r.decidedAt).toLocaleDateString() : "–"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}

      <section>
        <h3 className="h-section">Buchungsverlauf</h3>
        {account.loading ? <div className="loading">Lade…</div> : account.error ? <div className="banner err">{account.error}</div> : (
          <div style={{ overflowX: "auto" }}>
            <table className="table">
              <thead><tr><th>Datum</th><th>Kategorie</th><th>Grund</th><th className="num">Münzen</th></tr></thead>
              <tbody>
                {account.data?.entries.map((e) => (
                  <tr key={e.id}>
                    <td className="muted">{new Date(e.createdAt).toLocaleDateString()}</td>
                    <td>{pointKindLabel(e.kind)}</td>
                    <td className="muted">{e.reason}</td>
                    <td className="num" style={{ color: e.amount < 0 ? "var(--danger, #c23a1d)" : "inherit" }}>
                      {e.amount > 0 ? `+${e.amount}` : e.amount}</td>
                  </tr>
                ))}
                {account.data?.entries.length === 0 && <tr><td colSpan={4} className="muted">Noch keine Buchungen.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </>
  );
}
