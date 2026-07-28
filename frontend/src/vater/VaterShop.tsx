import { useId, useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { FieldLabel } from "../components/InfoHint";
import { api } from "../lib/api";
import { useAction, type ActionState } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import { useChildSelection } from "../lib/useChildSelection";
import { confirmAction } from "../lib/ui";
import { ACTION_LABEL, ACTION_OPTIONS, REFILL_LABEL, UNIT_LABEL, UNIT_OPTIONS, priceLabel, unitAmount } from "../lib/shop";
import type {
  ActionType, ActivationRequest, ChildResponse, CreateShopArticleDto, CreateShopListingDto,
  InventoryItem, ShopArticle, ShopListing, ShopPurchase, ShopRefillKind, UnitType, UpdateShopListingDto,
} from "../lib/types";

/** Auffüll-Regeln zur Auswahl (Reihenfolge = wie oft man sie braucht). */
const REFILL_KINDS: ShopRefillKind[] = ["None", "Daily", "TwiceDaily", "Weekly", "Once"];

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
  const action = useAction();

  function up<K extends keyof CreateShopArticleDto>(k: K, v: CreateShopArticleDto[K]) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    const title = form.title.trim();
    if (!form.articleNumber.trim()) { action.fail("Artikelnummer nötig."); return; }
    if (!title) { action.fail("Titel nötig."); return; }
    const ok = await action.run(() => api.createShopArticle({
      ...form, articleNumber: form.articleNumber.trim(), title,
      description: form.description?.trim() || null,
    }), `Artikel „${title}" angelegt.`);
    if (!ok) return;
    setForm((f) => ({ ...f, articleNumber: "", title: "", description: "" }));
    list.reload();
  }

  async function remove(a: ShopArticle) {
    if (!confirmAction(`Artikel „${a.title}" samt aller Angebote löschen? (Kaufhistorie bleibt erhalten.)`)) return;
    if (!await action.run(() => api.deleteShopArticle(a.id))) return;
    if (selected?.id === a.id) setSelected(null);
    list.reload();
  }

  return (
    <section>
      <h3 className="h-section">Artikel {list.data ? `(${list.data.length})` : ""}</h3>
      <form className="form-grid" onSubmit={submit} style={{ alignItems: "end" }}>
        <div className="field" style={{ maxWidth: 140 }}><FieldLabel htmlFor={`${uid}-nr`} topic="shopArticleNumber">Artikelnummer</FieldLabel>
          <input id={`${uid}-nr`} value={form.articleNumber} onChange={(e) => up("articleNumber", e.target.value)} placeholder="TV-001" /></div>
        <div className="field" style={{ minWidth: 180 }}><label htmlFor={`${uid}-title`}>Titel</label>
          <input id={`${uid}-title`} value={form.title} onChange={(e) => up("title", e.target.value)} placeholder="Fernsehzeit" /></div>
        <div className="field"><FieldLabel htmlFor={`${uid}-action`} topic="shopActionType">Art</FieldLabel>
          <select id={`${uid}-action`} value={form.actionType} onChange={(e) => up("actionType", e.target.value as ActionType)}>
            {ACTION_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
          </select></div>
        <div className="field"><FieldLabel htmlFor={`${uid}-unit`} topic="shopUnitType">Einheit</FieldLabel>
          <select id={`${uid}-unit`} value={form.unitType} onChange={(e) => up("unitType", e.target.value as UnitType)}>
            {UNIT_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
          </select></div>
        <div className="field" style={{ minWidth: 200 }}><label htmlFor={`${uid}-desc`}>Beschreibung</label>
          <input id={`${uid}-desc`} value={form.description ?? ""} onChange={(e) => up("description", e.target.value)} placeholder="Bildschirmzeit nach dem Lernen" /></div>
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy}>{action.busy ? "Lege an…" : "Anlegen"}</button>
      </form>
      <StatusBanner message={action.message} style={{ marginTop: 10 }} />

      {list.loading ? <div className="loading">Lade…</div> : list.error ? <div className="banner err">{list.error}</div> : (
        <div style={{ overflowX: "auto", marginTop: 10 }}>
          <table className="table">
            <thead><tr><th>Nr.</th><th>Titel</th><th>Art</th><th>Einheit</th><th></th></tr></thead>
            <tbody>
              {list.data?.map((a) => (
                <ArticleRow key={a.id} article={a} selected={selected?.id === a.id}
                  onToggleListings={() => setSelected(selected?.id === a.id ? null : a)}
                  onSaved={list.reload}
                  action={action}
                  onRemove={() => remove(a)} />
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

/**
 * Eine Artikel-Zeile, im Bearbeiten-Modus mit Feldern. Der Artikel ist die Belohnungs-*Art*; Preis und
 * Bestand hängen an seinen Angeboten. Titel und Einheit lassen sich nachträglich richtigstellen, ohne
 * die Kaufhistorie anzutasten – sie trägt ihre eigene Momentaufnahme.
 */
function ArticleRow({ article, selected, action, onToggleListings, onSaved, onRemove }: {
  article: ShopArticle; selected: boolean; action: ActionState;
  onToggleListings: () => void; onSaved: () => void; onRemove: () => void;
}) {
  const uid = useId();
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState({
    articleNumber: article.articleNumber, title: article.title,
    description: article.description ?? "", unitType: article.unitType, actionType: article.actionType,
  });
  async function save() {
    const ok = await action.run(() => api.updateShopArticle(article.id, {
      articleNumber: form.articleNumber.trim(), title: form.title.trim(),
      description: form.description.trim() || null, unitType: form.unitType, actionType: form.actionType,
    }), "Artikel gespeichert.");
    if (!ok) return;
    setEditing(false);
    onSaved();
  }

  if (editing) {
    return (
      <tr>
        <td colSpan={5}>
          <div className="row" style={{ gap: 8, alignItems: "flex-end", flexWrap: "wrap" }}>
            <div className="field" style={{ maxWidth: 130 }}><FieldLabel htmlFor={`${uid}-nr`} topic="shopArticleNumber">Artikelnummer</FieldLabel>
              <input id={`${uid}-nr`} value={form.articleNumber} onChange={(e) => setForm((f) => ({ ...f, articleNumber: e.target.value }))} /></div>
            <div className="field" style={{ minWidth: 160 }}><label htmlFor={`${uid}-title`}>Titel</label>
              <input id={`${uid}-title`} value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} /></div>
            <div className="field"><FieldLabel htmlFor={`${uid}-action`} topic="shopActionType">Art</FieldLabel>
              <select id={`${uid}-action`} value={form.actionType} onChange={(e) => setForm((f) => ({ ...f, actionType: e.target.value as ActionType }))}>
                {ACTION_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
              </select></div>
            <div className="field"><FieldLabel htmlFor={`${uid}-unit`} topic="shopUnitType">Einheit</FieldLabel>
              <select id={`${uid}-unit`} value={form.unitType} onChange={(e) => setForm((f) => ({ ...f, unitType: e.target.value as UnitType }))}>
                {UNIT_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
              </select></div>
            <div className="field" style={{ minWidth: 180 }}><label htmlFor={`${uid}-desc`}>Beschreibung</label>
              <input id={`${uid}-desc`} value={form.description} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))} /></div>
            <button type="button" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy} onClick={save}>OK</button>
            <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={action.busy} onClick={() => setEditing(false)}>Abbrechen</button>
          </div>
        </td>
      </tr>
    );
  }

  return (
    <tr style={selected ? { background: "rgba(38,217,255,.08)" } : undefined}>
      <td className="muted">{article.articleNumber}</td>
      <td>{article.title}</td>
      <td>{ACTION_LABEL[article.actionType]}</td>
      <td className="muted">{UNIT_LABEL[article.unitType]}</td>
      <td style={{ whiteSpace: "nowrap" }}>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={onToggleListings}>
          {selected ? "Angebote \u25b2" : "Angebote \u25bc"}</button>{" "}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={() => setEditing(true)}>Bearbeiten</button>{" "}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={onRemove}>Löschen</button>
      </td>
    </tr>
  );
}

function ListingManager({ article }: { article: ShopArticle }) {
  const uid = useId();
  const list = useAsync<ShopListing[]>(() => api.shopListings(article.id), [article.id]);
  const [form, setForm] = useState<CreateShopListingDto>({
    title: "", description: "", coinPrice: 100, gemPrice: 0, unitsPerPurchase: 30, currentStock: 5, maxStock: 5,
    refillKind: "None",
  });
  const action = useAction();

  function up<K extends keyof CreateShopListingDto>(k: K, v: CreateShopListingDto[K]) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (form.coinPrice <= 0 && form.gemPrice <= 0) { action.fail("Mindestens ein Preis muss > 0 sein."); return; }
    if (form.unitsPerPurchase <= 0) { action.fail("Menge je Kauf muss ≥ 1 sein."); return; }
    const ok = await action.run(() => api.createShopListing(article.id, {
      ...form, title: form.title?.trim() || null, description: form.description?.trim() || null,
    }), "Angebot angelegt.");
    if (!ok) return;
    setForm((f) => ({ ...f, title: "", description: "" }));
    list.reload();
  }

  async function toggle(l: ShopListing) {
    if (await action.run(() => api.updateShopListing(article.id, l.id, { active: !l.active }))) list.reload();
  }
  async function save(l: ShopListing, dto: UpdateShopListingDto) {
    if (await action.run(() => api.updateShopListing(article.id, l.id, dto))) list.reload();
  }
  async function refill(l: ShopListing) {
    // Schneller „auffüllen": Bestand auf Max zurücksetzen (Max unverändert).
    if (await action.run(() => api.updateShopListing(article.id, l.id, { currentStock: l.maxStock }))) list.reload();
  }
  async function remove(l: ShopListing) {
    if (!confirmAction("Dieses Angebot löschen?")) return;
    if (await action.run(() => api.deleteShopListing(article.id, l.id))) list.reload();
  }

  return (
    <div className="card" style={{ marginTop: 12 }}>
      <h4 className="h-section" style={{ fontSize: 16 }}>Angebote für „{article.title}" ({UNIT_LABEL[article.unitType]})</h4>
      <form className="form-grid" onSubmit={submit} style={{ alignItems: "end" }}>
        <div className="field" style={{ minWidth: 150 }}><label htmlFor={`${uid}-t`}>Titel (optional)</label>
          <input id={`${uid}-t`} value={form.title ?? ""} onChange={(e) => up("title", e.target.value)} placeholder="30 Min Fernsehen" /></div>
        <div className="field" style={{ maxWidth: 120 }}><FieldLabel htmlFor={`${uid}-units`} topic="shopUnitsPerPurchase">Menge je Kauf</FieldLabel>
          <input id={`${uid}-units`} type="number" min={1} value={form.unitsPerPurchase} onChange={(e) => up("unitsPerPurchase", Number(e.target.value))} /></div>
        <div className="field" style={{ maxWidth: 110 }}><FieldLabel htmlFor={`${uid}-coin`} topic="shopCoinPrice">Preis 🪙</FieldLabel>
          <input id={`${uid}-coin`} type="number" min={0} value={form.coinPrice} onChange={(e) => up("coinPrice", Number(e.target.value))} /></div>
        <div className="field" style={{ maxWidth: 110 }}><FieldLabel htmlFor={`${uid}-gem`} topic="shopGemPrice">Preis 💎</FieldLabel>
          <input id={`${uid}-gem`} type="number" min={0} value={form.gemPrice} onChange={(e) => up("gemPrice", Number(e.target.value))} /></div>
        <div className="field" style={{ maxWidth: 100 }}><FieldLabel htmlFor={`${uid}-stock`} topic="shopStock">Bestand</FieldLabel>
          <input id={`${uid}-stock`} type="number" min={0} value={form.currentStock} onChange={(e) => up("currentStock", Number(e.target.value))} /></div>
        <div className="field" style={{ maxWidth: 100 }}><FieldLabel htmlFor={`${uid}-max`} topic="shopMaxStock">Max-Bestand</FieldLabel>
          <input id={`${uid}-max`} type="number" min={0} value={form.maxStock} onChange={(e) => up("maxStock", Number(e.target.value))} /></div>
        {/* Ohne Auffüllen ist ein Angebot nach `maxStock` Käufen dauerhaft leer – das ist oft gewollt
            (einmalige Belohnung), aber es muss eine Entscheidung sein, keine Nebenwirkung. */}
        <div className="field"><FieldLabel htmlFor={`${uid}-refill`} topic="shopRefill">Auffüllen</FieldLabel>
          <select id={`${uid}-refill`} value={form.refillKind ?? "None"} onChange={(e) => up("refillKind", e.target.value as ShopRefillKind)}>
            {REFILL_KINDS.map((k) => <option key={k} value={k}>{REFILL_LABEL[k]}</option>)}
          </select></div>
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy}>{action.busy ? "Lege an…" : "Angebot anlegen"}</button>
      </form>
      <StatusBanner message={action.message} style={{ marginTop: 10 }} />

      {list.loading ? <div className="loading">Lade…</div> : list.error ? <div className="banner err">{list.error}</div> : (
        <div style={{ overflowX: "auto", marginTop: 10 }}>
          <table className="table">
            <thead><tr><th>Angebot</th><th>Menge</th><th>Preis</th><th>Bestand</th><th>Auffüllen</th><th>Status</th><th></th></tr></thead>
            <tbody>
              {list.data?.map((l) => (
                <ListingRow key={l.id} listing={l} unitType={article.unitType}
                  onSave={(dto) => save(l, dto)} onRefill={() => refill(l)} onToggle={() => toggle(l)} onRemove={() => remove(l)} />
              ))}
              {list.data?.length === 0 && <tr><td colSpan={7} className="muted">Noch keine Angebote – lege oben eins an.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

/** Eine Angebots-Zeile; im Bearbeiten-Modus sind Preis, Menge und Bestandsgrenzen änderbar. */
function ListingRow({ listing, unitType, onSave, onRefill, onToggle, onRemove }: {
  listing: ShopListing; unitType: UnitType;
  onSave: (dto: UpdateShopListingDto) => void;
  onRefill: () => void; onToggle: () => void; onRemove: () => void;
}) {
  const uid = useId();
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState({
    title: listing.title ?? "", coinPrice: listing.coinPrice, gemPrice: listing.gemPrice,
    unitsPerPurchase: listing.unitsPerPurchase, currentStock: listing.currentStock,
    maxStock: listing.maxStock, refillKind: listing.refillKind,
  });

  if (editing) {
    return (
      <tr>
        <td colSpan={7}>
          <div className="row" style={{ gap: 8, alignItems: "flex-end", flexWrap: "wrap" }}>
            <div className="field" style={{ minWidth: 140 }}><label htmlFor={`${uid}-t`}>Titel</label>
              <input id={`${uid}-t`} value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} /></div>
            <div className="field" style={{ maxWidth: 110 }}><FieldLabel htmlFor={`${uid}-u`} topic="shopUnitsPerPurchase">Menge je Kauf</FieldLabel>
              <input id={`${uid}-u`} type="number" min={1} value={form.unitsPerPurchase} onChange={(e) => setForm((f) => ({ ...f, unitsPerPurchase: Number(e.target.value) }))} /></div>
            <div className="field" style={{ maxWidth: 100 }}><FieldLabel htmlFor={`${uid}-c`} topic="shopCoinPrice">Preis 🪙</FieldLabel>
              <input id={`${uid}-c`} type="number" min={0} value={form.coinPrice} onChange={(e) => setForm((f) => ({ ...f, coinPrice: Number(e.target.value) }))} /></div>
            <div className="field" style={{ maxWidth: 100 }}><FieldLabel htmlFor={`${uid}-g`} topic="shopGemPrice">Preis 💎</FieldLabel>
              <input id={`${uid}-g`} type="number" min={0} value={form.gemPrice} onChange={(e) => setForm((f) => ({ ...f, gemPrice: Number(e.target.value) }))} /></div>
            <div className="field" style={{ maxWidth: 100 }}><FieldLabel htmlFor={`${uid}-s`} topic="shopStock">Bestand</FieldLabel>
              <input id={`${uid}-s`} type="number" min={0} value={form.currentStock} onChange={(e) => setForm((f) => ({ ...f, currentStock: Number(e.target.value) }))} /></div>
            <div className="field" style={{ maxWidth: 100 }}><FieldLabel htmlFor={`${uid}-m`} topic="shopMaxStock">Max</FieldLabel>
              <input id={`${uid}-m`} type="number" min={0} value={form.maxStock} onChange={(e) => setForm((f) => ({ ...f, maxStock: Number(e.target.value) }))} /></div>
            <div className="field"><FieldLabel htmlFor={`${uid}-r`} topic="shopRefill">Auffüllen</FieldLabel>
              <select id={`${uid}-r`} value={form.refillKind} onChange={(e) => setForm((f) => ({ ...f, refillKind: e.target.value as ShopRefillKind }))}>
                {REFILL_KINDS.map((k) => <option key={k} value={k}>{REFILL_LABEL[k]}</option>)}
              </select></div>
            <button type="button" className="btn inline-btn" style={{ width: "auto" }}
              onClick={() => { onSave({ ...form, title: form.title.trim() || null }); setEditing(false); }}>OK</button>
            <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={() => setEditing(false)}>Abbrechen</button>
          </div>
        </td>
      </tr>
    );
  }

  return (
    <tr style={{ opacity: listing.active ? 1 : 0.55 }}>
      <td>{listing.title || <span className="muted">(ohne Titel)</span>}</td>
      <td className="muted">{unitAmount(listing.unitsPerPurchase, unitType)}</td>
      <td>{priceLabel(listing.coinPrice, listing.gemPrice)}</td>
      <td className="num">{listing.currentStock}/{listing.maxStock}</td>
      <td className="muted">{REFILL_LABEL[listing.refillKind]}</td>
      <td>{listing.active ? <span className="pill lime">aktiv</span> : <span className="pill">inaktiv</span>}</td>
      <td style={{ whiteSpace: "nowrap" }}>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={onRefill}>Bestand füllen</button>{" "}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={() => setEditing(true)}>Bearbeiten</button>{" "}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={onToggle}>
          {listing.active ? "Deaktivieren" : "Aktivieren"}</button>{" "}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={onRemove}>Löschen</button>
      </td>
    </tr>
  );
}

// ─── Kind-Verwaltung: Anfragen entscheiden, Käufe stornieren, Inventar sehen ──

function ChildShopManager() {
  const children = useAsync<ChildResponse[]>(() => api.children(), []);
  // Vorauswahl aus `?childId=` (die Links vom Kind-Hub tragen sie), sonst das erste Kind.
  const { activeChild, select } = useChildSelection(children.data);

  return (
    <section>
      <h3 className="h-section">Käufe & Anfragen je Kind</h3>
      {children.loading ? <div className="loading">Lade…</div>
        : children.error ? <div className="banner err">{children.error}</div>
        : children.data && children.data.length > 0 ? (
          <div className="field" style={{ maxWidth: 320 }}>
            <label htmlFor="shop-child">Kind</label>
            <select id="shop-child" value={activeChild ?? ""} onChange={(e) => select(Number(e.target.value))}>
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
  const action = useAction();

  async function decide(r: ActivationRequest, approve: boolean) {
    if (approve && !confirmAction(`${unitAmount(r.requestedQuantity, r.unitType)} „${r.articleTitle}" freigeben? Die Einheiten werden aus dem Inventar entnommen.`)) return;
    const ok = await action.run(() => (approve
      ? api.approveActivation(childId, r.id)
      : api.rejectActivation(childId, r.id)));
    if (!ok) return;
    activations.reload();
    inventory.reload();
  }
  async function cancel(p: ShopPurchase) {
    if (!confirmAction(`Kauf „${p.title}" stornieren und ${priceLabel(p.coinPrice, p.gemPrice)} erstatten?`)) return;
    if (!await action.run(() => api.cancelPurchase(childId, p.id))) return;
    purchases.reload();
    inventory.reload();
  }

  return (
    <>
      <StatusBanner message={action.message} style={{ marginTop: 10 }} />

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
