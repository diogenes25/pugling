import { useId, useState } from "react";
import { api, errorMessage } from "../lib/api";
import { useAsync } from "../lib/useAsync";
import { confirmAction } from "../lib/ui";
import { ACTION_LABEL, ACTION_OPTIONS, REFILL_LABEL, UNIT_LABEL, UNIT_OPTIONS, priceLabel, unitAmount } from "../lib/shop";
import type {
  ActionType, ActivationRequest, ChildResponse, CreateShopArticleDto, CreateShopListingDto,
  InventoryItem, ShopArticle, ShopListing, ShopPurchase, UnitType,
} from "../lib/types";

/**
 * Familien-Shop aus Vater-Sicht: den Belohnungs-Katalog pflegen (Artikel + Angebote) und je Kind
 * Käufe, Inventar und Aktivierungsanfragen entscheiden. Der Shop ist der einzige Münz-Ausgabeweg –
 * hier legt der Vater fest, wofür der Sohn seine 🪙 Münzen eintauschen darf.
 */
export function VaterShop() {
  return (
    <>
      <section>
        <h2 className="h-section">Familien-Shop</h2>
        <p className="muted">Der einzige Weg, verdiente 🪙 Münzen auszugeben. Lege <b>Artikel</b> an (die Art
          der Belohnung) und dazu <b>Angebote</b> mit Preis, Menge und Bestand. Dein Kind kauft daraus und
          beantragt später das Einlösen – das gibst du unten frei.</p>
      </section>
      <ArticleCatalog />
      <ChildShopManager />
    </>
  );
}

// ─── Artikel-Katalog + Angebote ──────────────────────────────────────────────

function ArticleCatalog() {
  const uid = useId();
  const list = useAsync<ShopArticle[]>(() => api.shopArticles(), []);
  const [selected, setSelected] = useState<ShopArticle | null>(null);
  const [form, setForm] = useState<CreateShopArticleDto>({
    articleNumber: "", title: "", description: "", unitType: "Minute", actionType: "TV",
  });
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  function up<K extends keyof CreateShopArticleDto>(k: K, v: CreateShopArticleDto[K]) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.articleNumber.trim()) { setMsg({ ok: false, text: "Artikelnummer nötig." }); return; }
    if (!form.title.trim()) { setMsg({ ok: false, text: "Titel nötig." }); return; }
    setBusy(true);
    try {
      await api.createShopArticle({
        ...form, articleNumber: form.articleNumber.trim(), title: form.title.trim(),
        description: form.description?.trim() || null,
      });
      setMsg({ ok: true, text: `Artikel „${form.title.trim()}" angelegt.` });
      setForm((f) => ({ ...f, articleNumber: "", title: "", description: "" }));
      list.reload();
    } catch (err) {
      setMsg({ ok: false, text: errorMessage(err) });
    } finally {
      setBusy(false);
    }
  }

  async function remove(a: ShopArticle) {
    if (!confirmAction(`Artikel „${a.title}" samt aller Angebote löschen? (Kaufhistorie bleibt erhalten.)`)) return;
    try {
      await api.deleteShopArticle(a.id);
      if (selected?.id === a.id) setSelected(null);
      list.reload();
    } catch (err) { setMsg({ ok: false, text: errorMessage(err) }); }
  }

  return (
    <section>
      <h3 className="h-section">Artikel {list.data ? `(${list.data.length})` : ""}</h3>
      <form className="form-grid" onSubmit={submit} style={{ alignItems: "end" }}>
        <div className="field" style={{ maxWidth: 140 }}><label htmlFor={`${uid}-nr`}>Artikelnummer</label>
          <input id={`${uid}-nr`} value={form.articleNumber} onChange={(e) => up("articleNumber", e.target.value)} placeholder="TV-001" /></div>
        <div className="field" style={{ minWidth: 180 }}><label htmlFor={`${uid}-title`}>Titel</label>
          <input id={`${uid}-title`} value={form.title} onChange={(e) => up("title", e.target.value)} placeholder="Fernsehzeit" /></div>
        <div className="field"><label htmlFor={`${uid}-action`}>Art</label>
          <select id={`${uid}-action`} value={form.actionType} onChange={(e) => up("actionType", e.target.value as ActionType)}>
            {ACTION_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
          </select></div>
        <div className="field"><label htmlFor={`${uid}-unit`}>Einheit</label>
          <select id={`${uid}-unit`} value={form.unitType} onChange={(e) => up("unitType", e.target.value as UnitType)}>
            {UNIT_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
          </select></div>
        <div className="field" style={{ minWidth: 200 }}><label htmlFor={`${uid}-desc`}>Beschreibung</label>
          <input id={`${uid}-desc`} value={form.description ?? ""} onChange={(e) => up("description", e.target.value)} placeholder="Bildschirmzeit nach dem Lernen" /></div>
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={busy}>{busy ? "…" : "Anlegen"}</button>
      </form>
      {msg && <div role="status" aria-live="polite" className={`banner ${msg.ok ? "ok" : "err"}`} style={{ marginTop: 10 }}>{msg.text}</div>}

      {list.loading ? <div className="loading">Lade…</div> : list.error ? <div className="banner err">{list.error}</div> : (
        <div style={{ overflowX: "auto", marginTop: 10 }}>
          <table className="table">
            <thead><tr><th>Nr.</th><th>Titel</th><th>Art</th><th>Einheit</th><th></th></tr></thead>
            <tbody>
              {list.data?.map((a) => (
                <tr key={a.id} style={selected?.id === a.id ? { background: "rgba(38,217,255,.08)" } : undefined}>
                  <td className="muted">{a.articleNumber}</td>
                  <td>{a.title}</td>
                  <td>{ACTION_LABEL[a.actionType]}</td>
                  <td className="muted">{UNIT_LABEL[a.unitType]}</td>
                  <td style={{ whiteSpace: "nowrap" }}>
                    <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
                      onClick={() => setSelected(selected?.id === a.id ? null : a)}>
                      {selected?.id === a.id ? "Angebote ▲" : "Angebote ▼"}</button>{" "}
                    <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={() => remove(a)}>Löschen</button>
                  </td>
                </tr>
              ))}
              {list.data?.length === 0 && <tr><td colSpan={5} className="muted">Noch keine Artikel.</td></tr>}
            </tbody>
          </table>
        </div>
      )}

      {selected && <ListingManager key={selected.id} article={selected} />}
    </section>
  );
}

function ListingManager({ article }: { article: ShopArticle }) {
  const uid = useId();
  const list = useAsync<ShopListing[]>(() => api.shopListings(article.id), [article.id]);
  const [form, setForm] = useState<CreateShopListingDto>({
    title: "", description: "", coinPrice: 100, gemPrice: 0, unitsPerPurchase: 30, currentStock: 5, maxStock: 5,
  });
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  function up<K extends keyof CreateShopListingDto>(k: K, v: CreateShopListingDto[K]) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (form.coinPrice <= 0 && form.gemPrice <= 0) { setMsg({ ok: false, text: "Mindestens ein Preis muss > 0 sein." }); return; }
    if (form.unitsPerPurchase <= 0) { setMsg({ ok: false, text: "Menge je Kauf muss ≥ 1 sein." }); return; }
    setBusy(true);
    try {
      await api.createShopListing(article.id, {
        ...form, title: form.title?.trim() || null, description: form.description?.trim() || null,
      });
      setMsg({ ok: true, text: "Angebot angelegt." });
      setForm((f) => ({ ...f, title: "", description: "" }));
      list.reload();
    } catch (err) {
      setMsg({ ok: false, text: errorMessage(err) });
    } finally {
      setBusy(false);
    }
  }

  async function toggle(l: ShopListing) {
    try { await api.updateShopListing(article.id, l.id, { active: !l.active }); list.reload(); }
    catch (err) { setMsg({ ok: false, text: errorMessage(err) }); }
  }
  async function refill(l: ShopListing) {
    // Schneller „auffüllen": Bestand auf Max zurücksetzen (Max unverändert).
    try { await api.updateShopListing(article.id, l.id, { currentStock: l.maxStock }); list.reload(); }
    catch (err) { setMsg({ ok: false, text: errorMessage(err) }); }
  }
  async function remove(l: ShopListing) {
    if (!confirmAction("Dieses Angebot löschen?")) return;
    try { await api.deleteShopListing(article.id, l.id); list.reload(); }
    catch (err) { setMsg({ ok: false, text: errorMessage(err) }); }
  }

  return (
    <div className="card" style={{ marginTop: 12 }}>
      <h4 className="h-section" style={{ fontSize: 16 }}>Angebote für „{article.title}" ({UNIT_LABEL[article.unitType]})</h4>
      <form className="form-grid" onSubmit={submit} style={{ alignItems: "end" }}>
        <div className="field" style={{ minWidth: 150 }}><label htmlFor={`${uid}-t`}>Titel (optional)</label>
          <input id={`${uid}-t`} value={form.title ?? ""} onChange={(e) => up("title", e.target.value)} placeholder="30 Min Fernsehen" /></div>
        <div className="field" style={{ maxWidth: 120 }}><label htmlFor={`${uid}-units`}>Menge je Kauf</label>
          <input id={`${uid}-units`} type="number" min={1} value={form.unitsPerPurchase} onChange={(e) => up("unitsPerPurchase", Number(e.target.value))} /></div>
        <div className="field" style={{ maxWidth: 110 }}><label htmlFor={`${uid}-coin`}>Preis 🪙</label>
          <input id={`${uid}-coin`} type="number" min={0} value={form.coinPrice} onChange={(e) => up("coinPrice", Number(e.target.value))} /></div>
        <div className="field" style={{ maxWidth: 110 }}><label htmlFor={`${uid}-gem`}>Preis 💎</label>
          <input id={`${uid}-gem`} type="number" min={0} value={form.gemPrice} onChange={(e) => up("gemPrice", Number(e.target.value))} /></div>
        <div className="field" style={{ maxWidth: 100 }}><label htmlFor={`${uid}-stock`}>Bestand</label>
          <input id={`${uid}-stock`} type="number" min={0} value={form.currentStock} onChange={(e) => up("currentStock", Number(e.target.value))} /></div>
        <div className="field" style={{ maxWidth: 100 }}><label htmlFor={`${uid}-max`}>Max-Bestand</label>
          <input id={`${uid}-max`} type="number" min={0} value={form.maxStock} onChange={(e) => up("maxStock", Number(e.target.value))} /></div>
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={busy}>{busy ? "…" : "Angebot anlegen"}</button>
      </form>
      {msg && <div role="status" aria-live="polite" className={`banner ${msg.ok ? "ok" : "err"}`} style={{ marginTop: 10 }}>{msg.text}</div>}

      {list.loading ? <div className="loading">Lade…</div> : list.error ? <div className="banner err">{list.error}</div> : (
        <div style={{ overflowX: "auto", marginTop: 10 }}>
          <table className="table">
            <thead><tr><th>Angebot</th><th>Menge</th><th>Preis</th><th>Bestand</th><th>Auffüllen</th><th>Status</th><th></th></tr></thead>
            <tbody>
              {list.data?.map((l) => (
                <tr key={l.id} style={{ opacity: l.active ? 1 : 0.55 }}>
                  <td>{l.title || <span className="muted">(ohne Titel)</span>}</td>
                  <td className="muted">{unitAmount(l.unitsPerPurchase, article.unitType)}</td>
                  <td>{priceLabel(l.coinPrice, l.gemPrice)}</td>
                  <td className="num">{l.currentStock}/{l.maxStock}</td>
                  <td className="muted">{REFILL_LABEL[l.refillKind]}</td>
                  <td>{l.active ? <span className="pill lime">aktiv</span> : <span className="pill">inaktiv</span>}</td>
                  <td style={{ whiteSpace: "nowrap" }}>
                    <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={() => refill(l)}>Bestand füllen</button>{" "}
                    <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={() => toggle(l)}>
                      {l.active ? "Deaktivieren" : "Aktivieren"}</button>{" "}
                    <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={() => remove(l)}>Löschen</button>
                  </td>
                </tr>
              ))}
              {list.data?.length === 0 && <tr><td colSpan={7} className="muted">Noch keine Angebote – lege oben eins an.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

// ─── Kind-Verwaltung: Anfragen entscheiden, Käufe stornieren, Inventar sehen ──

function ChildShopManager() {
  const children = useAsync<ChildResponse[]>(() => api.children(), []);
  const [childId, setChildId] = useState<number | null>(null);
  const activeChild = childId ?? children.data?.[0]?.id ?? null;

  return (
    <section>
      <h3 className="h-section">Käufe & Anfragen je Kind</h3>
      {children.loading ? <div className="loading">Lade…</div>
        : children.error ? <div className="banner err">{children.error}</div>
        : children.data && children.data.length > 0 ? (
          <div className="field" style={{ maxWidth: 320 }}>
            <label htmlFor="shop-child">Kind</label>
            <select id="shop-child" value={activeChild ?? ""} onChange={(e) => setChildId(Number(e.target.value))}>
              {children.data.map((c) => <option key={c.id} value={c.id}>{c.name} (#{c.id})</option>)}
            </select>
          </div>
        ) : <div className="banner">Lege zuerst ein Kind an (Übersicht).</div>}

      {activeChild !== null && <ChildShopView key={activeChild} childId={activeChild} />}
    </section>
  );
}

function ChildShopView({ childId }: { childId: number }) {
  const activations = useAsync<ActivationRequest[]>(() => api.childActivations(childId), [childId]);
  const purchases = useAsync<ShopPurchase[]>(() => api.childPurchases(childId), [childId]);
  const inventory = useAsync<InventoryItem[]>(() => api.childInventory(childId), [childId]);
  const [msg, setMsg] = useState<string | null>(null);

  async function decide(r: ActivationRequest, approve: boolean) {
    if (approve && !confirmAction(`${unitAmount(r.requestedQuantity, r.unitType)} „${r.articleTitle}" freigeben? Die Einheiten werden aus dem Inventar entnommen.`)) return;
    try {
      if (approve) await api.approveActivation(childId, r.id);
      else await api.rejectActivation(childId, r.id);
      activations.reload();
      inventory.reload();
    } catch (err) { setMsg(errorMessage(err)); }
  }
  async function cancel(p: ShopPurchase) {
    if (!confirmAction(`Kauf „${p.title}" stornieren und ${priceLabel(p.coinPrice, p.gemPrice)} erstatten?`)) return;
    try {
      await api.cancelPurchase(childId, p.id);
      purchases.reload();
      inventory.reload();
    } catch (err) { setMsg(errorMessage(err)); }
  }

  return (
    <>
      {msg && <div role="status" aria-live="polite" className="banner err" style={{ marginTop: 10 }}>{msg}</div>}

      <h4 className="h-section" style={{ fontSize: 16, marginTop: 14 }}>Offene Aktivierungsanfragen</h4>
      {activations.loading ? <div className="loading">Lade…</div> : activations.error ? <div className="banner err">{activations.error}</div> : (
        <div style={{ overflowX: "auto" }}>
          <table className="table">
            <thead><tr><th>Belohnung</th><th>Menge</th><th>Angefragt</th><th>Status</th><th></th></tr></thead>
            <tbody>
              {activations.data?.map((r) => (
                <tr key={r.id}>
                  <td>{r.articleTitle}</td>
                  <td className="muted">{unitAmount(r.requestedQuantity, r.unitType)}</td>
                  <td className="muted">{new Date(r.requestedAt).toLocaleDateString()}</td>
                  <td>{activationPill(r.status)}</td>
                  <td style={{ whiteSpace: "nowrap" }}>
                    {r.canApprove && <><button type="button" className="btn lime inline-btn" style={{ width: "auto" }} onClick={() => decide(r, true)}>Freigeben</button>{" "}</>}
                    {r.canReject && <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={() => decide(r, false)}>Ablehnen</button>}
                  </td>
                </tr>
              ))}
              {activations.data?.length === 0 && <tr><td colSpan={5} className="muted">Keine Anfragen.</td></tr>}
            </tbody>
          </table>
        </div>
      )}

      <h4 className="h-section" style={{ fontSize: 16, marginTop: 14 }}>Inventar</h4>
      {inventory.loading ? <div className="loading">Lade…</div> : inventory.error ? <div className="banner err">{inventory.error}</div> : (
        <div style={{ overflowX: "auto" }}>
          <table className="table">
            <thead><tr><th>Artikel</th><th className="num">Menge</th></tr></thead>
            <tbody>
              {inventory.data?.map((i) => (
                <tr key={i.shopArticleId}><td>{i.title}</td><td className="num">{unitAmount(i.quantity, i.unitType)}</td></tr>
              ))}
              {inventory.data?.length === 0 && <tr><td colSpan={2} className="muted">Inventar leer.</td></tr>}
            </tbody>
          </table>
        </div>
      )}

      <h4 className="h-section" style={{ fontSize: 16, marginTop: 14 }}>Käufe</h4>
      {purchases.loading ? <div className="loading">Lade…</div> : purchases.error ? <div className="banner err">{purchases.error}</div> : (
        <div style={{ overflowX: "auto" }}>
          <table className="table">
            <thead><tr><th>Titel</th><th>Preis</th><th>Gekauft</th><th>Status</th><th></th></tr></thead>
            <tbody>
              {purchases.data?.map((p) => (
                <tr key={p.id}>
                  <td>{p.title}</td>
                  <td>{priceLabel(p.coinPrice, p.gemPrice)}</td>
                  <td className="muted">{new Date(p.purchasedAt).toLocaleDateString()}</td>
                  <td>{p.status === "Owned" ? <span className="pill lime">aktiv</span> : <span className="pill">storniert</span>}</td>
                  <td style={{ whiteSpace: "nowrap" }}>
                    {p.canCancel && <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={() => cancel(p)}>Stornieren</button>}
                  </td>
                </tr>
              ))}
              {purchases.data?.length === 0 && <tr><td colSpan={5} className="muted">Noch keine Käufe.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}

function activationPill(status: ActivationRequest["status"]) {
  if (status === "Approved") return <span className="pill lime">freigegeben</span>;
  if (status === "Rejected") return <span className="pill red">abgelehnt</span>;
  return <span className="pill gold">wartet</span>;
}
