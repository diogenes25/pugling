import { useState } from "react";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { pointKindLabel, redemptionStatusLabel } from "../lib/labels";
import { confirmAction } from "../lib/ui";
import { PAGE_SIZE, Pager } from "../components/ListControls";
import type { ChildResponse, Paged, RedemptionDef, RewardRedemptionStatus, WalletEntry } from "../lib/types";

const REDEMPTION_FILTERS: { value: "" | RewardRedemptionStatus; label: string }[] = [
  { value: "", label: "Alle" }, { value: "Purchased", label: "Offen" },
  { value: "Fulfilled", label: "Erfüllt" }, { value: "Cancelled", label: "Storniert" },
];

export function VaterKonto() {
  const children = useAsync<ChildResponse[]>(() => api.children(), []);
  const [childId, setChildId] = useState<number | null>(null);
  const activeChild = childId ?? children.data?.[0]?.id ?? null;

  return (
    <>
      <section>
        <h2 className="h-section">Konto</h2>
        <p className="muted">Münz-/Gem-Stand, Buchungsverlauf und offene Käufe deines Kindes. Beim Kauf sind
          die Münzen sofort weg – du erfüllst den Kauf real (oder stornierst mit Rückerstattung).</p>
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
  // Offene-Käufe-Kennzahl unabhängig vom Tabellenfilter (nur die Gesamtzahl; take:0 lädt keine Zeilen).
  const openCount = useAsync<number>(() => api.redemptionsFor(childId, { status: "Purchased", take: 0 }).then((r) => r.total), [childId]);
  // Eine paginierte Einlösungs-Tabelle mit Status-Filter (statt zwei client-gesplitteter Tabellen –
  // der Server-Status-Filter ist einwertig, „abgeschlossen" ließe sich nicht sauber paginieren).
  const [redStatus, setRedStatus] = useState<"" | RewardRedemptionStatus>("");
  const [redSkip, setRedSkip] = useState(0);
  const redemptions = useAsync<Paged<RedemptionDef>>(
    () => api.redemptionsFor(childId, { status: redStatus || undefined, skip: redSkip, take: PAGE_SIZE }),
    [childId, redStatus, redSkip]);
  // Statuswechsel springt auf Seite 1 zurück – in der Render-Phase, damit nicht erst mit altem skip nachgeladen wird.
  const [prevRedStatus, setPrevRedStatus] = useState(redStatus);
  if (prevRedStatus !== redStatus) { setPrevRedStatus(redStatus); setRedSkip(0); }
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);

  function flash(ok: boolean, text: string) { setMsg({ ok, text }); setTimeout(() => setMsg(null), 2500); }

  async function decide(id: number, fulfill: boolean) {
    if (!fulfill && !confirmAction("Diesen Kauf wirklich stornieren? Die Münzen werden dem Kind zurückgebucht.")) return;
    setBusy(true);
    try {
      if (fulfill) await api.fulfillRedemption(childId, id);
      else await api.cancelRedemption(childId, id);
      account.reload();
      redemptions.reload();
      openCount.reload();
      flash(true, fulfill ? "Erfüllt." : "Storniert – Münzen zurückerstattet.");
    } catch (err) {
      flash(false, errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <section className="vater-grid">
        <div className="card"><div className="muted">Münzstand</div><div className="h-section">🪙 {account.data?.coins ?? "…"}</div></div>
        <div className="card"><div className="muted">Gems</div><div className="h-section">💎 {account.data?.gems ?? "…"}</div></div>
        <div className="card"><div className="muted">Offene Käufe</div><div className="h-section">{openCount.data ?? "…"}</div></div>
      </section>

      {msg && <div className={`banner ${msg.ok ? "ok" : "err"}`} role="status" aria-live="polite">{msg.text}</div>}

      <section>
        <div className="row" style={{ alignItems: "center", gap: 8, flexWrap: "wrap" }}>
          <h3 className="h-section" style={{ margin: 0 }}>Prämien-Einlösungen {redemptions.data ? `(${redemptions.data.total})` : ""}</h3>
          <label className="row" style={{ marginLeft: "auto", gap: 6, alignItems: "center", fontSize: 13 }}>
            <span className="muted">Status</span>
            <select aria-label="Status-Filter" value={redStatus} onChange={(e) => setRedStatus(e.target.value as "" | RewardRedemptionStatus)}>
              {REDEMPTION_FILTERS.map((f) => <option key={f.value} value={f.value}>{f.label}</option>)}
            </select>
          </label>
        </div>
        {redemptions.loading ? <div className="loading">Lade…</div> : redemptions.error ? <div className="banner err">{redemptions.error}</div>
          : redemptions.data?.items.length === 0 ? <p className="muted">Keine Einlösungen in dieser Auswahl.</p> : (
            <div style={{ overflowX: "auto" }}>
              <table className="table">
                <thead><tr><th>Angebot</th><th>Preis</th><th>Status</th><th>Gekauft</th><th>Aktion</th></tr></thead>
                <tbody>
                  {redemptions.data?.items.map((r) => (
                    <tr key={r.id}>
                      <td>{r.title}</td>
                      <td>🪙 {r.cost}</td>
                      <td>{r.status === "Fulfilled" ? <span className="pill lime">{redemptionStatusLabel(r.status)}</span>
                        : r.status === "Purchased" ? <span className="pill gold">{redemptionStatusLabel(r.status)}</span>
                        : <span className="pill">{redemptionStatusLabel(r.status)}</span>}</td>
                      <td className="muted">{new Date(r.purchasedAt).toLocaleDateString()}</td>
                      <td style={{ whiteSpace: "nowrap" }}>
                        {r.status === "Purchased" ? (
                          <>
                            <button type="button" className="btn inline-btn" style={{ width: "auto" }} disabled={busy}
                              onClick={() => decide(r.id, true)}>Erfüllen</button>{" "}
                            <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy}
                              onClick={() => decide(r.id, false)}>Stornieren</button>
                          </>
                        ) : <span className="muted">{r.fulfilledAt ? new Date(r.fulfilledAt).toLocaleDateString() : "–"}</span>}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        {redemptions.data && <Pager skip={redSkip} take={PAGE_SIZE} total={redemptions.data.total} onSkip={setRedSkip} />}
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
