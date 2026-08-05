import { useCallback, useEffect, useRef, useState } from "react";
import { api, errorMessage } from "../lib/api";
import { confirmAction } from "../lib/ui";
import { ACTION_EMOJI, priceLabel, unitAmount } from "../lib/shop";
import type { MyInventoryItem, MyActivation, MyShopPurchase, ShopAvailableListing, ShopView } from "../lib/types";
import { useSohn } from "./SohnApp";

type Tab = "buy" | "stuff" | "requests" | "history";

// Wie viele Zeilen ein "Mehr laden" auf einmal nachlaedt (B-99: die Kaufhistorie hatte vorher einen
// stillen Take(50)-Deckel und keinen Weg zu aelteren Zeilen).
const HISTORY_PAGE = 20;

const STATUS_PILL: Record<MyActivation["status"], { cls: string; label: string }> = {
  Pending: { cls: "gold", label: "wartet" },
  Approved: { cls: "lime", label: "freigegeben" },
  Rejected: { cls: "red", label: "abgelehnt" },
};

/**
 * Familien-Shop aus Sohn-Sicht: verdiente 🪙 Münzen in echte Vater-Belohnungen umsetzen.
 * Drei Tabs – Kaufen (Angebote), Sachen (gekauftes Inventar + Einlösen beantragen) und Anfragen (Status).
 * Gegenstück zu {@link SohnSkins} (dort werden 💎 Gems gegen Charaktere getauscht).
 */
export function SohnShop() {
  const { coins, gems, refreshWallet, celebrate } = useSohn();
  const [tab, setTab] = useState<Tab>("buy");
  const [view, setView] = useState<ShopView | null>(null);
  const [activations, setActivations] = useState<MyActivation[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);
  const [history, setHistory] = useState<MyShopPurchase[]>([]);
  const [historyTotal, setHistoryTotal] = useState(0);
  const [historyLoaded, setHistoryLoaded] = useState(false);
  const [historyLoading, setHistoryLoading] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [v, a] = await Promise.all([api.shopView(), api.myActivations()]);
      setView(v);
      setActivations(a);
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  // Lädt die nächste Seite der Kaufhistorie nach ("mehr laden"); beim ersten Öffnen des Verlauf-Tabs
  // wird die erste Seite geholt (B-99: kein stiller Abschnitt mehr bei Zeile 50).
  // Sperre über ein Ref, nicht über `historyLoading` (State): der State steht erst nach dem Re-Render,
  // ein zweiter Aufruf im selben Tick (Tab erneut geöffnet, bevor die erste Seite durch ist) sähe die
  // Sperre also noch offen und hängte dieselben Zeilen doppelt an (Muster judging.current/useAction).
  const loadingHistory = useRef(false);
  const loadMoreHistory = useCallback(async () => {
    if (loadingHistory.current) return;
    loadingHistory.current = true;
    setHistoryLoading(true);
    try {
      const page = await api.shopPurchasesPage(history.length, HISTORY_PAGE);
      setHistory((prev) => [...prev, ...page.items]);
      setHistoryTotal(page.total);
      setHistoryLoaded(true);
    } catch (e) {
      flash(errorMessage(e));
    } finally {
      loadingHistory.current = false;
      setHistoryLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [history.length]);

  function openHistoryTab() {
    setTab("history");
    if (!historyLoaded) void loadMoreHistory();
  }

  function flash(text: string) {
    setMsg(text);
    setTimeout(() => setMsg(null), 2000);
  }

  async function buy(listing: ShopAvailableListing) {
    if (busy) return;
    if (!confirmAction(`„${listing.title}" für ${priceLabel(listing.coinPrice, listing.gemPrice)} kaufen?`)) return;
    setBusy(true);
    try {
      const next = await api.purchaseListing(listing.id);
      setView(next);
      refreshWallet(); // Münzstand im HUD real aktualisieren (nach Kauf niedriger)
      celebrate("medium", ACTION_EMOJI[listing.actionType], "GEKAUFT!", listing.title);
    } catch (e) {
      flash(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  async function requestActivation(item: MyInventoryItem, quantity: number) {
    if (busy) return;
    // Papa hat den Artikel gelöscht: die Einheiten bleiben (bezahlt ist bezahlt), einlösen geht aber
    // nicht mehr, weil die Anfrage über die Artikel-Id läuft. Lieber sagen, warum, als 404 zeigen.
    // `== null` fängt beides: der Vertrag erlaubt bei einem nullable Feld auch das Fehlen des Schlüssels.
    if (item.shopArticleId == null) {
      flash("Das gibt es bei Papa nicht mehr – frag ihn direkt danach. 🙋");
      return;
    }
    if (!confirmAction(`${unitAmount(quantity, item.unitType)} „${item.title}" bei Papa anfragen?`)) return;
    setBusy(true);
    try {
      await api.activateInventory(item.shopArticleId, quantity);
      flash("Anfrage an Papa geschickt! 📨");
      await load(); // Inventar (Menge sinkt) + Anfragen neu laden
      setTab("requests");
    } catch (e) {
      flash(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="sohn-body">
      <div className="row">
        <span className="screen-title" style={{ margin: 0 }}>🛒 Shop</span>
        <span className="chip" style={{ marginLeft: "auto" }}>🪙<b className="tabnum">{coins}</b></span>
        <span className="chip">💎<b className="tabnum">{gems}</b></span>
      </div>
      <p className="sub">Deine 🪙 Münzen fürs Lernen gibst du hier gegen echte Belohnungen von Papa aus. Erst
        <b> kaufen</b>, dann bei <b>Sachen</b> das Einlösen beantragen – Papa gibt es frei.</p>

      <div className="row" role="group" aria-label="Shop-Bereiche" style={{ gap: 6 }}>
        <TabButton active={tab === "buy"} onClick={() => setTab("buy")}>Kaufen</TabButton>
        <TabButton active={tab === "stuff"} onClick={() => setTab("stuff")}>
          Sachen{view && view.inventory.length > 0 ? ` (${view.inventory.length})` : ""}
        </TabButton>
        <TabButton active={tab === "requests"} onClick={() => setTab("requests")}>
          Anfragen{activations.some((a) => a.status === "Pending") ? " •" : ""}
        </TabButton>
        <TabButton active={tab === "history"} onClick={openHistoryTab}>Verlauf</TabButton>
      </div>

      {loading ? <div className="loading">Lade Shop…</div>
        : error ? <div className="banner err">{error}</div>
        : !view ? null
        : tab === "buy" ? <BuyTab listings={view.available} busy={busy} onBuy={buy} />
        : tab === "stuff" ? <StuffTab inventory={view.inventory} busy={busy} onActivate={requestActivation} />
        : tab === "requests" ? <RequestsTab activations={activations} />
        : <HistoryTab purchases={history} total={historyTotal} loading={historyLoading} onLoadMore={loadMoreHistory} />}

      {msg && <div className="toast" role="status" aria-live="polite">{msg}</div>}
    </div>
  );
}

function TabButton({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      type="button"
      aria-pressed={active ? "true" : "false"}
      className={`btn small${active ? "" : " ghost"}`}
      style={{ width: "auto", flex: 1 }}
      onClick={onClick}
    >
      {children}
    </button>
  );
}

function BuyTab({ listings, busy, onBuy }: {
  listings: ShopAvailableListing[];
  busy: boolean;
  onBuy: (l: ShopAvailableListing) => void;
}) {
  if (listings.length === 0)
    return <p className="sub">Papa hat noch keine Belohnungen in den Shop gestellt.</p>;
  return (
    <div className="skin-grid">
      {listings.map((l) => {
        const soldOut = l.currentStock <= 0;
        const canBuy = l.affordable && !soldOut && !busy;
        return (
          <button
            type="button"
            key={l.id}
            className={`skin${canBuy ? "" : " locked"}`}
            onClick={() => onBuy(l)}
            disabled={!canBuy}
          >
            <div className="face" style={{ background: "linear-gradient(180deg,#2a2f6b,#171a44)" }}>
              {ACTION_EMOJI[l.actionType]}
            </div>
            <div className="nm">{l.title || l.articleTitle}</div>
            <div className="sub" style={{ fontSize: 11, minHeight: 26 }}>
              {unitAmount(l.unitsPerPurchase, l.unitType)}
              {l.description ? ` · ${l.description}` : ""}
            </div>
            {soldOut ? <span className="pill red">ausverkauft</span>
              : <span className={`pill ${l.affordable ? "gold" : "red"}`}>{priceLabel(l.coinPrice, l.gemPrice)}</span>}
          </button>
        );
      })}
    </div>
  );
}

function StuffTab({ inventory, busy, onActivate }: {
  inventory: MyInventoryItem[];
  busy: boolean;
  onActivate: (item: MyInventoryItem, quantity: number) => void;
}) {
  if (inventory.length === 0)
    return <p className="sub">Noch nichts gekauft. Hol dir im Tab <b>Kaufen</b> etwas Schönes! 🎁</p>;
  return (
    <div className="list">
      {inventory.map((item) => (
        // Der Schlüssel fällt auf den Titel zurück: bei gelöschtem Artikel ist die Id null, und zwei
        // verwaiste Posten dürften sich nicht denselben Schlüssel teilen.
        <InventoryRow key={item.shopArticleId ?? `weg:${item.title}`} item={item} busy={busy} onActivate={onActivate} />
      ))}
    </div>
  );
}

function InventoryRow({ item, busy, onActivate }: {
  item: MyInventoryItem;
  busy: boolean;
  onActivate: (item: MyInventoryItem, quantity: number) => void;
}) {
  const [qty, setQty] = useState(item.quantity);
  const clamped = Math.min(Math.max(1, qty || 1), item.quantity);
  const weg = item.shopArticleId === null;
  return (
    <div className="card">
      <div className="row">
        <span style={{ fontSize: 26 }} aria-hidden="true">{ACTION_EMOJI[item.actionType]}</span>
        <div style={{ flex: 1 }}>
          <b>{item.title}</b>
          <div className="sub">
            Du hast {unitAmount(item.quantity, item.unitType)}
            {weg && " · gibt's bei Papa nicht mehr"}
          </div>
        </div>
      </div>
      <div className="row" style={{ marginTop: 8, gap: 8 }}>
        <input
          type="number"
          min={1}
          max={item.quantity}
          value={qty}
          aria-label={`Menge zum Einlösen (max ${item.quantity})`}
          onChange={(e) => setQty(Number(e.target.value))}
          style={{ width: 90, background: "#0c0e2c", border: "1.5px solid var(--stroke)", borderRadius: 12, color: "var(--ink)", padding: 10, fontSize: 15 }}
        />
        <button type="button" className="btn small lime" style={{ width: "auto", flex: 1 }}
          disabled={busy || weg} onClick={() => onActivate(item, clamped)}>
          Einlösen beantragen
        </button>
      </div>
    </div>
  );
}

function RequestsTab({ activations }: { activations: MyActivation[] }) {
  if (activations.length === 0)
    return <p className="sub">Noch keine Anfragen gestellt. Löse bei <b>Sachen</b> etwas ein.</p>;
  return (
    <div className="list">
      {activations.map((a) => {
        const pill = STATUS_PILL[a.status];
        return (
          <div key={a.id} className="row" style={{ justifyContent: "space-between", padding: "8px 0" }}>
            <div>
              <b>{a.articleTitle}</b>
              <div className="sub">{unitAmount(a.requestedQuantity, a.unitType)} · {new Date(a.requestedAt).toLocaleDateString()}</div>
            </div>
            <span className={`pill ${pill.cls}`}>{pill.label}</span>
          </div>
        );
      })}
    </div>
  );
}

/**
 * Kaufhistorie mit "mehr laden" statt einer stillen Abschnittsgrenze (B-99): der Server sagt über
 * `total` die echte Gesamtzahl, damit "X von Y" ehrlich ist statt einer Liste, die einfach aufhört.
 */
export function HistoryTab({ purchases, total, loading, onLoadMore }: {
  purchases: MyShopPurchase[]; total: number; loading: boolean; onLoadMore: () => void;
}) {
  if (!loading && purchases.length === 0)
    return <p className="sub">Noch nichts gekauft. Hol dir im Tab <b>Kaufen</b> etwas Schönes! 🎁</p>;
  return (
    <div className="list">
      {purchases.map((p) => (
        <div key={p.id} className="row" style={{ justifyContent: "space-between", padding: "8px 0" }}>
          <div>
            <b>{p.title}</b>
            <div className="sub">{priceLabel(p.coinPrice, p.gemPrice)} · {new Date(p.purchasedAt).toLocaleDateString()}</div>
          </div>
          {p.status === "Cancelled" && <span className="pill red">storniert</span>}
        </div>
      ))}
      {purchases.length < total && (
        <button type="button" className="btn ghost small" style={{ marginTop: 8 }} disabled={loading} onClick={onLoadMore}>
          {loading ? "Lädt…" : `Mehr laden (${purchases.length} von ${total})`}
        </button>
      )}
    </div>
  );
}
