import { useId, useState } from "react";
import { StatusBanner } from "../components/StatusBanner";
import { FieldLabel, InfoHint } from "../components/InfoHint";
import { PAGE_SIZE, Pager } from "../components/ListControls";
import { api } from "../lib/api";
import { useAction, type ActionState } from "../lib/useAction";
import { useAsync } from "../lib/useAsync";
import { useChildSelection } from "../lib/useChildSelection";
import { confirmAction } from "../lib/ui";
import { ACTION_LABEL, ACTION_OPTIONS, REFILL_LABEL, UNIT_LABEL, UNIT_OPTIONS, priceLabel, unitAmount } from "../lib/shop";
import { augenblick, beginnDesLerntags, formatDate, formatDateIn, heuteImLerntag, LERNTAG_ZONE } from "../lib/datum";
import { bestaetigeFreigabe, entscheideEinloesung } from "./einloesungen";
import type {
  ActionType, ActivationRequest, ChildResponse, CreateShopArticleDto, CreateShopListingDto,
  InventoryItem, Paged, ShopArticle, ShopListing, ShopPurchase, ShopRefillKind, UnitType, UpdateShopListingDto,
} from "../lib/types";

/** Auffüll-Regeln zur Auswahl (Reihenfolge = wie oft man sie braucht). */
const REFILL_KINDS: ShopRefillKind[] = ["None", "Daily", "TwiceDaily", "Weekly", "Monthly", "Once"];

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

      {list.loading && list.data === null ? <div className="loading">Lade…</div> : list.error ? <div className="banner err">{list.error}</div> : (
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
export function ArticleRow({ article, selected, action, onToggleListings, onSaved, onRemove }: {
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
      // B-789: leer heißt `""` – der Server leert damit; `null` hieße „unverändert“, die alte Beschreibung bliebe stehen.
      description: form.description.trim(), unitType: form.unitType, actionType: form.actionType,
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
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
          disabled={action.busy} onClick={onRemove}>Löschen</button>
      </td>
    </tr>
  );
}

/** Der Auffüll-Plan eines Angebots, wie ihn Anlegen und Bearbeiten teilen (B-645). */
type RefillPlan = {
  maxStock: number;
  refillQuantity: number;
  refillKind: ShopRefillKind;
  refillDayOfWeek: Weekday;
  /** Datum `yyyy-mm-dd` für „Einmalig"; leer = noch nicht gewählt. */
  refillDate: string;
  /**
   * Reste verfallen (Menge je Termin = Obergrenze). Als eigener Merker, nicht aus den Zahlen abgeleitet: beim Tippen
   * der Obergrenze („1" auf dem Weg zu „12") wären Menge und Obergrenze kurz gleich, und aus Ansparen würde Verfallen.
   */
  expires: boolean;
};

type Weekday = NonNullable<CreateShopListingDto["refillDayOfWeek"]>;

const WEEKDAYS: { value: Weekday; label: string; short: string }[] = [
  { value: "Monday", label: "Montag", short: "Mo" },
  { value: "Tuesday", label: "Dienstag", short: "Di" },
  { value: "Wednesday", label: "Mittwoch", short: "Mi" },
  { value: "Thursday", label: "Donnerstag", short: "Do" },
  { value: "Friday", label: "Freitag", short: "Fr" },
  { value: "Saturday", label: "Samstag", short: "Sa" },
  { value: "Sunday", label: "Sonntag", short: "So" },
];

/**
 * Die Felder des Plans für den Server. Wochentag und Datum gehen nur mit, wo die Art sie braucht – sonst
 * verlangte der Server sie nicht, hielte sie aber fest. Die Menge je Termin zählt nur bei einer auffüllenden Art.
 *
 * `loadedRefillAtUtc` ist der Termin, wie der Server ihn geliefert hat (B-782): Steht im Feld noch sein Datum, geht er
 * unverändert zurück. Das Feld kennt nur den Tag; ein Termin mit Uhrzeit käme sonst als Mitternacht zurück, der Server
 * läse einen neuen Termin und füllte jedes Kind noch einmal auf.
 */
/** Der Lerntag eines Server-Zeitpunkts – ein Termin um 23:00 UTC ist schon der Folgetag (B-730). */
const tagDes = (refillAtUtc: string) => heuteImLerntag(augenblick(refillAtUtc));

function planDto(plan: RefillPlan, loadedRefillAtUtc?: string | null) {
  return {
    maxStock: plan.maxStock,
    refillKind: plan.refillKind,
    refillQuantity: plan.refillKind === "None" ? null : plan.refillQuantity,
    refillDayOfWeek: plan.refillKind === "Weekly" ? plan.refillDayOfWeek : null,
    refillAtUtc: plan.refillKind !== "Once" || !plan.refillDate ? null
      : loadedRefillAtUtc && tagDes(loadedRefillAtUtc) === plan.refillDate ? loadedRefillAtUtc
      // Mitternacht deutscher Zeit wie jedes andere Auffüllen, nicht 00:00 UTC = 1 bzw. 2 Uhr (B-730).
      : beginnDesLerntags(plan.refillDate),
  };
}

/** Prüft, was der Server sonst mit 400 ablehnte – als Satz am Formular statt als Fehlercode. */
function planProblem(plan: RefillPlan): string | null {
  if (plan.refillKind === "None") return null;
  if (plan.maxStock < 1) return "Ein Angebot, das sich auffüllt, braucht eine Obergrenze von mindestens 1.";
  if (plan.refillQuantity < 1 || plan.refillQuantity > plan.maxStock)
    return "„+ je Termin“ muss zwischen 1 und der Obergrenze liegen.";
  return null;
}

/** Pflichtfeld leer → Speichern gesperrt, keine Meldung (B-233): „Einmalig" ohne Datum. */
const planIncomplete = (plan: RefillPlan) => plan.refillKind === "Once" && !plan.refillDate;

function refillText(l: ShopListing): string {
  let text = REFILL_LABEL[l.refillKind];
  if (l.refillKind === "Weekly" && l.refillDayOfWeek)
    text += ` (${WEEKDAYS.find((d) => d.value === l.refillDayOfWeek)?.short ?? l.refillDayOfWeek})`;
  // Der Termin liegt als Beginn eines Lerntags (deutsche Zeit) – er wird in dieser Zone gelesen, nicht in der des Browsers.
  if (l.refillKind === "Once" && l.refillAtUtc) text += ` (${formatDateIn(l.refillAtUtc, LERNTAG_ZONE)})`;
  // Ansparen sichtbar machen, bei jeder Art – sonst sähe ein Spar-Angebot aus wie eines, dessen Reste verfallen.
  if (l.refillKind !== "None" && l.refillKind !== "Once" && l.refillQuantity < l.maxStock)
    text += `, +${l.refillQuantity} bis ${l.maxStock}`;
  return text;
}

/** Auffüllen, Wochentag/Datum, Obergrenze und Menge je Termin – geteilt von Anlegen und Bearbeiten. */
function RefillFields({ uid, plan, onChange }: { uid: string; plan: RefillPlan; onChange: (p: RefillPlan) => void }) {
  const up = <K extends keyof RefillPlan>(k: K, v: RefillPlan[K]) => onChange({ ...plan, [k]: v });
  return (
    <>
      {/* Ohne Auffüllen ist ein Angebot nach `maxStock` Käufen je Kind dauerhaft leer – das ist oft gewollt
          (einmalige Belohnung), aber es muss eine Entscheidung sein, keine Nebenwirkung. */}
      <div className="field"><FieldLabel htmlFor={`${uid}-refill`} topic="shopRefill">Auffüllen</FieldLabel>
        <select id={`${uid}-refill`} value={plan.refillKind}
          onChange={(e) => up("refillKind", e.target.value as ShopRefillKind)}>
          {REFILL_KINDS.map((k) => <option key={k} value={k}>{REFILL_LABEL[k]}</option>)}
        </select></div>
      {plan.refillKind === "Weekly" && (
        <div className="field" style={{ maxWidth: 140 }}><label htmlFor={`${uid}-day`}>Wochentag</label>
          <select id={`${uid}-day`} value={plan.refillDayOfWeek} onChange={(e) => up("refillDayOfWeek", e.target.value as Weekday)}>
            {WEEKDAYS.map((d) => <option key={d.value} value={d.value}>{d.label}</option>)}
          </select></div>
      )}
      {plan.refillKind === "Once" && (
        <div className="field" style={{ maxWidth: 160 }}><label htmlFor={`${uid}-date`}>Datum</label>
          <input id={`${uid}-date`} type="date" value={plan.refillDate} onChange={(e) => up("refillDate", e.target.value)} /></div>
      )}
      <div className="field" style={{ maxWidth: 110 }}><FieldLabel htmlFor={`${uid}-max`} topic="shopMaxStock">Obergrenze je Kind</FieldLabel>
        <input id={`${uid}-max`} type="number" min={0} value={plan.maxStock}
          onChange={(e) => {
            const max = Number(e.target.value);
            // „Verfällt" folgt der Obergrenze; eine Spar-Menge bleibt stehen (planProblem meldet, wenn sie darüber liegt).
            onChange({ ...plan, maxStock: max, refillQuantity: plan.expires ? max : plan.refillQuantity });
          }} /></div>
      {plan.refillKind !== "None" && (
        <div className="field" style={{ maxWidth: 110 }}><FieldLabel htmlFor={`${uid}-qty`} topic="shopRefillQuantity">+ je Termin</FieldLabel>
          <input id={`${uid}-qty`} type="number" min={1} max={plan.maxStock} value={plan.refillQuantity}
            onChange={(e) => {
              const quantity = Number(e.target.value);
              onChange({ ...plan, refillQuantity: quantity, expires: quantity === plan.maxStock });
            }} /></div>
      )}
    </>
  );
}

/** „Für": leer heißt alle Kinder; so bekommt jedes Kind sein eigenes Taschengeld-Angebot (B-645, Entscheidung 12). */
function AudienceField({ uid, kids, value, onChange }: {
  uid: string; kids: ChildResponse[]; value: number[]; onChange: (ids: number[]) => void;
}) {
  if (kids.length < 2) return null;
  return (
    <fieldset className="field" style={{ border: 0, padding: 0, margin: 0 }}>
      {/* Überschrift über mehreren Feldern: kein <label for>, sonst hakte ein Klick auf „Für" das erste Kind an. */}
      <legend><span className="label-row group-label">Für (leer = alle) <InfoHint topic="shopAudience" /></span></legend>
      <div className="row" style={{ gap: 10, flexWrap: "wrap" }}>
        {kids.map((c) => (
          <label key={c.id} className="row" style={{ gap: 4 }}>
            <input id={`${uid}-aud-${c.id}`} type="checkbox" checked={value.includes(c.id)}
              onChange={(e) => onChange(e.target.checked ? [...value, c.id] : value.filter((id) => id !== c.id))} />
            {c.name}
          </label>
        ))}
      </div>
    </fieldset>
  );
}

function ListingManager({ article }: { article: ShopArticle }) {
  const uid = useId();
  const list = useAsync<ShopListing[]>(() => api.shopListings(article.id), [article.id]);
  const kids = useAsync<ChildResponse[]>(() => api.children(), []);
  const [form, setForm] = useState({ title: "", coinPrice: 100, gemPrice: 0, unitsPerPurchase: 30 });
  const [plan, setPlan] = useState<RefillPlan>({
    maxStock: 5, refillQuantity: 5, refillKind: "None", refillDayOfWeek: "Monday", refillDate: "", expires: true,
  });
  const [audience, setAudience] = useState<number[]>([]);
  const action = useAction();

  function up<K extends keyof typeof form>(k: K, v: (typeof form)[K]) {
    setForm((f) => ({ ...f, [k]: v }));
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (form.coinPrice <= 0 && form.gemPrice <= 0) { action.fail("Mindestens ein Preis muss > 0 sein."); return; }
    if (form.unitsPerPurchase <= 0) { action.fail("Menge je Kauf muss ≥ 1 sein."); return; }
    if (planIncomplete(plan)) return;
    const problem = planProblem(plan);
    if (problem) { action.fail(problem); return; }
    const ok = await action.run(() => api.createShopListing(article.id, {
      ...form, ...planDto(plan), title: form.title.trim() || null, description: null,
      childIds: audience.length > 0 ? audience : null,
    }), "Angebot angelegt.");
    if (!ok) return;
    setForm((f) => ({ ...f, title: "" }));
    list.reload();
  }

  async function toggle(l: ShopListing) {
    if (await action.run(() => api.updateShopListing(article.id, l.id, { active: !l.active }))) list.reload();
  }
  async function save(l: ShopListing, dto: UpdateShopListingDto) {
    if (await action.run(() => api.updateShopListing(article.id, l.id, dto))) list.reload();
  }
  async function fill(l: ShopListing, childId: number) {
    // „Bestand füllen" je Kind: auf die Obergrenze – das Angebot selbst bleibt unverändert.
    if (await action.run(() => api.setShopListingStock(article.id, l.id, childId, l.maxStock))) list.reload();
  }
  async function remove(l: ShopListing) {
    if (!confirmAction("Dieses Angebot löschen?")) return;
    if (await action.run(() => api.deleteShopListing(article.id, l.id))) list.reload();
  }

  const children = kids.data ?? [];
  return (
    <div className="card" style={{ marginTop: 12 }}>
      <h4 className="h-section" style={{ fontSize: 16 }}>Angebote für „{article.title}" ({UNIT_LABEL[article.unitType]})</h4>
      <form className="form-grid" onSubmit={submit} style={{ alignItems: "end" }}>
        <div className="field" style={{ minWidth: 150 }}><label htmlFor={`${uid}-t`}>Titel (optional)</label>
          <input id={`${uid}-t`} value={form.title} onChange={(e) => up("title", e.target.value)} placeholder="30 Min Fernsehen" /></div>
        <div className="field" style={{ maxWidth: 120 }}><FieldLabel htmlFor={`${uid}-units`} topic="shopUnitsPerPurchase">Menge je Kauf</FieldLabel>
          <input id={`${uid}-units`} type="number" min={1} value={form.unitsPerPurchase} onChange={(e) => up("unitsPerPurchase", Number(e.target.value))} /></div>
        <div className="field" style={{ maxWidth: 110 }}><FieldLabel htmlFor={`${uid}-coin`} topic="shopCoinPrice">Preis 🪙</FieldLabel>
          <input id={`${uid}-coin`} type="number" min={0} value={form.coinPrice} onChange={(e) => up("coinPrice", Number(e.target.value))} /></div>
        <div className="field" style={{ maxWidth: 110 }}><FieldLabel htmlFor={`${uid}-gem`} topic="shopGemPrice">Preis 💎</FieldLabel>
          <input id={`${uid}-gem`} type="number" min={0} value={form.gemPrice} onChange={(e) => up("gemPrice", Number(e.target.value))} /></div>
        <RefillFields uid={uid} plan={plan} onChange={setPlan} />
        <AudienceField uid={uid} kids={children} value={audience} onChange={setAudience} />
        <button type="submit" className="btn inline-btn" style={{ width: "auto" }} disabled={action.busy || planIncomplete(plan)}>{action.busy ? "Lege an…" : "Angebot anlegen"}</button>
      </form>
      <StatusBanner message={action.message} style={{ marginTop: 10 }} />

      {list.loading && list.data === null ? <div className="loading">Lade…</div> : list.error ? <div className="banner err">{list.error}</div> : (
        <div style={{ overflowX: "auto", marginTop: 10 }}>
          <table className="table">
            <thead><tr><th>Angebot</th><th>Menge</th><th>Preis</th><th>Für</th><th>Bestand je Kind</th><th>Auffüllen</th><th>Status</th><th></th></tr></thead>
            <tbody>
              {list.data?.map((l) => (
                <ListingRow key={l.id} listing={l} unitType={article.unitType} kids={children} busy={action.busy}
                  onFail={action.fail} onSave={(dto) => save(l, dto)} onFill={(childId) => fill(l, childId)} onToggle={() => toggle(l)} onRemove={() => remove(l)} />
              ))}
              {list.data?.length === 0 && <tr><td colSpan={8} className="muted">Noch keine Angebote – lege oben eins an.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

/** Das Bearbeiten-Formular einer Angebots-Zeile, belegt aus dem gespeicherten Angebot. */
function editForm(listing: ShopListing) {
  return {
    title: listing.title ?? "", coinPrice: listing.coinPrice, gemPrice: listing.gemPrice,
    unitsPerPurchase: listing.unitsPerPurchase,
  };
}

/**
 * Der Auffüll-Plan zum Bearbeiten. Das Datum kommt nur aus einem „einmalig“-Angebot (B-786): der Server hält einen alten
 * Termin auch an einem wiederkehrenden Angebot fest, und vorbelegt ginge er beim Wechsel auf „einmalig“ ungewählt hinaus.
 */
function editPlan(listing: ShopListing): RefillPlan {
  return {
    maxStock: listing.maxStock, refillQuantity: listing.refillQuantity, refillKind: listing.refillKind,
    refillDayOfWeek: listing.refillDayOfWeek ?? "Monday",
    refillDate: listing.refillKind === "Once" && listing.refillAtUtc ? tagDes(listing.refillAtUtc) : "",
    expires: listing.refillQuantity >= listing.maxStock,
  };
}

/** Eine Angebots-Zeile; im Bearbeiten-Modus sind Preis, Menge, Auffüll-Plan und Zielgruppe änderbar. */
export function ListingRow({ listing, unitType, kids, busy, onFail, onSave, onFill, onToggle, onRemove }: {
  listing: ShopListing; unitType: UnitType; kids: ChildResponse[];
  /**
   * Läuft eine Aktion des Angebots-Panels. Die Zeile hält keinen eigenen Zustand: das Panel teilt eine
   * `useAction`-Instanz über alle Zeilen, und dessen Sperre gilt darum listenweit. Ohne das `disabled`
   * wäre ein Klick in Zeile 7 während Zeile 3 speichert wirkungslos – **ohne** sichtbaren Grund.
   */
  busy: boolean;
  /** Meldet eine Prüfung über das eine Banner des Panels, wie beim Anlegen – keine zweite Fehlerbox je Zeile. */
  onFail: (message: string) => void;
  onSave: (dto: UpdateShopListingDto) => void;
  onFill: (childId: number) => void; onToggle: () => void; onRemove: () => void;
}) {
  const uid = useId();
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState(() => editForm(listing));
  const [plan, setPlan] = useState<RefillPlan>(() => editPlan(listing));
  const [audience, setAudience] = useState<number[]>(listing.childIds);
  // B-786: jedes Öffnen beginnt beim gespeicherten Angebot. Sonst stünde nach „Abbrechen“ das Verworfene im Formular, und
  // ein späteres „OK“ schickte es mit – ein verworfenes Datum leerte als neuer Termin jeden Bestand.
  const startEditing = () => {
    setForm(editForm(listing));
    setPlan(editPlan(listing));
    setAudience(listing.childIds);
    setEditing(true);
  };
  const nameOf = (id: number) => kids.find((c) => c.id === id)?.name ?? `#${id}`;

  if (editing) {
    return (
      <tr>
        <td colSpan={8}>
          <div className="row" style={{ gap: 8, alignItems: "flex-end", flexWrap: "wrap" }}>
            <div className="field" style={{ minWidth: 140 }}><label htmlFor={`${uid}-t`}>Titel</label>
              <input id={`${uid}-t`} value={form.title} onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))} /></div>
            <div className="field" style={{ maxWidth: 110 }}><FieldLabel htmlFor={`${uid}-u`} topic="shopUnitsPerPurchase">Menge je Kauf</FieldLabel>
              <input id={`${uid}-u`} type="number" min={1} value={form.unitsPerPurchase} onChange={(e) => setForm((f) => ({ ...f, unitsPerPurchase: Number(e.target.value) }))} /></div>
            <div className="field" style={{ maxWidth: 100 }}><FieldLabel htmlFor={`${uid}-c`} topic="shopCoinPrice">Preis 🪙</FieldLabel>
              <input id={`${uid}-c`} type="number" min={0} value={form.coinPrice} onChange={(e) => setForm((f) => ({ ...f, coinPrice: Number(e.target.value) }))} /></div>
            <div className="field" style={{ maxWidth: 100 }}><FieldLabel htmlFor={`${uid}-g`} topic="shopGemPrice">Preis 💎</FieldLabel>
              <input id={`${uid}-g`} type="number" min={0} value={form.gemPrice} onChange={(e) => setForm((f) => ({ ...f, gemPrice: Number(e.target.value) }))} /></div>
            <RefillFields uid={uid} plan={plan} onChange={setPlan} />
            <AudienceField uid={uid} kids={kids} value={audience} onChange={setAudience} />
            <button type="button" className="btn inline-btn" style={{ width: "auto" }} disabled={busy || planIncomplete(plan)}
              onClick={() => {
                if (planIncomplete(plan)) return;
                const p = planProblem(plan);
                if (p) { onFail(p); return; }
                // B-789: ein geleerter Titel geht als `""` – `null` ließe den alten beim Server stehen.
                // B-786: der geladene Termin zählt nur, wo er auch vorbelegt war – der Rest-Termin eines
                // wiederkehrenden Angebots brächte sonst seine verborgene Uhrzeit mit, wenn der Vater denselben Tag wählt.
                onSave({ ...form, ...planDto(plan, listing.refillKind === "Once" ? listing.refillAtUtc : null), title: form.title.trim(), childIds: audience });
                setEditing(false);
              }}>OK</button>
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
      <td className="muted">{listing.childIds.length === 0 ? "alle" : listing.childIds.map(nameOf).join(", ")}</td>
      <td>
        {listing.stocks.length === 0 ? <span className="muted">–</span> : listing.stocks.map((s) => (
          <div key={s.childId} className="row" style={{ gap: 6, whiteSpace: "nowrap" }}>
            <span>{s.childName}: <span className="num">{s.currentStock}/{listing.maxStock}</span></span>
            <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
              aria-label={`Bestand von ${s.childName} füllen`}
              disabled={busy || s.currentStock >= listing.maxStock} onClick={() => onFill(s.childId)}>füllen</button>
          </div>
        ))}
      </td>
      <td className="muted">{refillText(listing)}</td>
      <td>{listing.active ? <span className="pill lime">aktiv</span> : <span className="pill">inaktiv</span>}</td>
      <td style={{ whiteSpace: "nowrap" }}>
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} onClick={startEditing}>Bearbeiten</button>{" "}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={busy} onClick={onToggle}>
          {listing.active ? "Deaktivieren" : "Aktivieren"}</button>{" "}
        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
          disabled={busy} onClick={onRemove}>Löschen</button>
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
        ) : <div className="banner">Lege zuerst ein Kind an (unter „Kinder").</div>}

      {activeChild !== null && <ChildShopView key={activeChild} childId={activeChild} />}
    </section>
  );
}

export function ChildShopView({ childId }: { childId: number }) {
  const activations = useAsync<ActivationRequest[]>(() => api.childActivations(childId), [childId]);
  const [purchaseSkip, setPurchaseSkip] = useState(0);
  const purchases = useAsync<Paged<ShopPurchase>>(
    () => api.childPurchases(childId, { skip: purchaseSkip, take: PAGE_SIZE }), [childId, purchaseSkip]);
  const inventory = useAsync<InventoryItem[]>(() => api.childInventory(childId), [childId]);
  const action = useAction();
  const uid = useId();
  // B-665: welche Inventarzeile gerade das Einlösen-Feld zeigt, und mit welcher Menge.
  const [redeeming, setRedeeming] = useState<{ articleId: number; quantity: number } | null>(null);

  async function decide(r: ActivationRequest, approve: boolean) {
    // Rückfrage, Fehlersatz und Zähler-Meldung liegen in der gemeinsamen Senke (einloesungen.ts, B-749).
    if (approve && !bestaetigeFreigabe(r)) return;
    const ok = await action.run(() => entscheideEinloesung(r, approve));
    // Auch bei Misserfolg neu laden: der Satz zeigt aufs Inventar, und das muss darunter den wahren Bestand zeigen; und
    // hat ein anderer Erwachsener schon entschieden, darf die Zeile nicht weiter „Freigeben" anbieten.
    if (!ok) { activations.reload(); inventory.reload(); return; }
    activations.reload();
    inventory.reload();
  }
  async function redeem(item: InventoryItem, quantity: number) {
    if (item.shopArticleId == null) return;
    const articleId = item.shopArticleId;
    if (!confirmAction(`${unitAmount(quantity, item.unitType)} „${item.title}" jetzt selbst einlösen? `
      + "Das geht ohne Anfrage des Kindes und nimmt die Menge sofort aus dem Inventar.")) return;
    const ok = await action.run(() => api.redeemFromInventory(childId, articleId, quantity), "Eingelöst.");
    // Auch bei Misserfolg neu laden: der wahre Bestand gehört unter die Meldung.
    activations.reload();
    inventory.reload();
    if (ok) setRedeeming(null);
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
      {activations.loading && activations.data === null ? <div className="loading">Lade…</div> : activations.error ? <div className="banner err">{activations.error}</div> : (
        <div style={{ overflowX: "auto" }}>
          <table className="table">
            <thead><tr><th>Belohnung</th><th>Menge</th><th>Angefragt</th><th>Status</th><th></th></tr></thead>
            <tbody>
              {activations.data?.map((r) => (
                <tr key={r.id}>
                  <td>{r.articleTitle}</td>
                  <td className="muted">{unitAmount(r.requestedQuantity, r.unitType)}</td>
                  <td className="muted">{formatDate(r.requestedAt)}</td>
                  <td>{activationPill(r.status)}{r.requestedBy === "Supervisor" && <span className="muted"> · selbst eingelöst</span>}</td>
                  <td style={{ whiteSpace: "nowrap" }}>
                    {r.canApprove && <><button type="button" className="btn lime inline-btn" style={{ width: "auto" }}
                      disabled={action.busy} onClick={() => decide(r, true)}>Freigeben</button>{" "}</>}
                    {r.canReject && <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
                      disabled={action.busy} onClick={() => decide(r, false)}>Ablehnen</button>}
                  </td>
                </tr>
              ))}
              {activations.data?.length === 0 && <tr><td colSpan={5} className="muted">Keine Anfragen.</td></tr>}
            </tbody>
          </table>
        </div>
      )}

      <h4 className="h-section" style={{ fontSize: 16, marginTop: 14 }}>Inventar</h4>
      {inventory.loading && inventory.data === null ? <div className="loading">Lade…</div> : inventory.error ? <div className="banner err">{inventory.error}</div> : (
        <div style={{ overflowX: "auto" }}>
          <table className="table">
            <thead><tr><th>Artikel</th><th className="num">Menge</th><th></th></tr></thead>
            <tbody>
              {inventory.data?.map((i) => (
                // Bei gelöschtem Artikel ist die Id null (bezahlte Einheiten bleiben): Schlüssel und
                // Hinweis fallen auf den eingefrorenen Titel zurück.
                <tr key={i.shopArticleId ?? `weg:${i.articleNumber}`}>
                  <td>{i.title}{i.shopArticleId === null && <span className="muted"> · Artikel gelöscht</span>}</td>
                  <td className="num">{unitAmount(i.quantity, i.unitType)}</td>
                  <td style={{ whiteSpace: "nowrap" }}>
                    {/* B-665: die Ausnahme fürs Kind ohne Handy – darum unauffällig und erst nach einem Klick ein Feld. */}
                    {i.shopArticleId != null && (redeeming && redeeming.articleId === i.shopArticleId ? (
                      <span className="row" style={{ gap: 6 }}>
                        <label htmlFor={`${uid}-redeem-${i.shopArticleId}`} className="muted">Menge</label>
                        <input id={`${uid}-redeem-${i.shopArticleId}`} type="number" min={1} max={i.quantity} style={{ width: 80 }}
                          autoFocus
                          value={redeeming.quantity}
                          onChange={(e) => setRedeeming({ articleId: i.shopArticleId!, quantity: Number(e.target.value) })} />
                        <button type="button" className="btn inline-btn" style={{ width: "auto" }}
                          disabled={action.busy || redeeming.quantity < 1 || redeeming.quantity > i.quantity}
                          onClick={() => redeem(i, redeeming.quantity)}>Einlösen</button>
                        <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
                          onClick={() => setRedeeming(null)}>Abbrechen</button>
                      </span>
                    ) : (
                      <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }} disabled={action.busy}
                        aria-label={`${i.title} selbst einlösen`}
                        onClick={() => setRedeeming({ articleId: i.shopArticleId!, quantity: i.quantity })}>Selbst einlösen…</button>
                    ))}
                  </td>
                </tr>
              ))}
              {inventory.data?.length === 0 && <tr><td colSpan={3} className="muted">Inventar leer.</td></tr>}
            </tbody>
          </table>
        </div>
      )}

      <h4 className="h-section" style={{ fontSize: 16, marginTop: 14 }}>Käufe</h4>
      {purchases.loading && purchases.data === null ? <div className="loading">Lade…</div> : purchases.error ? <div className="banner err">{purchases.error}</div> : (
        <div style={{ overflowX: "auto" }}>
          <table className="table">
            <thead><tr><th>Titel</th><th>Preis</th><th>Gekauft</th><th>Status</th><th></th></tr></thead>
            <tbody>
              {purchases.data?.items.map((p) => (
                <tr key={p.id}>
                  <td>{p.title}</td>
                  <td>{priceLabel(p.coinPrice, p.gemPrice)}</td>
                  <td className="muted">{formatDate(p.purchasedAt)}</td>
                  <td>{p.status === "Owned" ? <span className="pill lime">aktiv</span> : <span className="pill">storniert</span>}</td>
                  <td style={{ whiteSpace: "nowrap" }}>
                    {/* Ein Geldpfad auf der Vater-Seite: das Stornieren erstattet Münzen bzw. Gems. */}
                    {p.canCancel && <button type="button" className="btn ghost inline-btn" style={{ width: "auto" }}
                      disabled={action.busy} onClick={() => cancel(p)}>Stornieren</button>}
                  </td>
                </tr>
              ))}
              {purchases.data?.total === 0 && <tr><td colSpan={5} className="muted">Noch keine Käufe.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
      {purchases.data && <Pager skip={purchaseSkip} take={PAGE_SIZE} total={purchases.data.total} onSkip={setPurchaseSkip} busy={purchases.loading} />}
    </>
  );
}

function activationPill(status: ActivationRequest["status"]) {
  if (status === "Approved") return <span className="pill lime">freigegeben</span>;
  if (status === "Rejected") return <span className="pill red">abgelehnt</span>;
  return <span className="pill gold">wartet</span>;
}
