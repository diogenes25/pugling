import { useCallback, useEffect, useState } from "react";
import { api, errorMessage } from "../lib/api";
import { confirmAction } from "../lib/ui";
import { ACTION_EMOJI, priceLabel, unitAmount } from "../lib/shop";
import type { InventoryItem, MyActivation, ShopAvailableListing, ShopView } from "../lib/types";
import { useSohn } from "./SohnApp";

type Tab = "buy" | "stuff" | "requests";

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

  async function requestActivation(item: InventoryItem, quantity: number) {
    if (busy) return;
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
      </div>

      {loading ? <div className="loading">Lade Shop…</div>
        : error ? <div className="banner err">{error}</div>
        : !view ? null
        : tab === "buy" ? <BuyTab listings={view.available} busy={busy} onBuy={buy} />
        : tab === "stuff" ? <StuffTab inventory={view.inventory} busy={busy} onActivate={requestActivation} />
        : <RequestsTab activations={activations} />}

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
  inventory: InventoryItem[];
  busy: boolean;
  onActivate: (item: InventoryItem, quantity: number) => void;
}) {
  if (inventory.length === 0)
    return <p className="sub">Noch nichts gekauft. Hol dir im Tab <b>Kaufen</b> etwas Schönes! 🎁</p>;
  return (
    <div className="list">
      {inventory.map((item) => <InventoryRow key={item.shopArticleId} item={item} busy={busy} onActivate={onActivate} />)}
    </div>
  );
}

function InventoryRow({ item, busy, onActivate }: {
  item: InventoryItem;
  busy: boolean;
  onActivate: (item: InventoryItem, quantity: number) => void;
}) {
  const [qty, setQty] = useState(item.quantity);
  const clamped = Math.min(Math.max(1, qty || 1), item.quantity);
  return (
    <div className="card">
      <div className="row">
        <span style={{ fontSize: 26 }} aria-hidden="true">{ACTION_EMOJI[item.actionType]}</span>
        <div style={{ flex: 1 }}>
          <b>{item.title}</b>
          <div className="sub">Du hast {unitAmount(item.quantity, item.unitType)}</div>
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
          disabled={busy} onClick={() => onActivate(item, clamped)}>
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
