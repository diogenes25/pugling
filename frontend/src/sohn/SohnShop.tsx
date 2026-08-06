import { useCallback, useEffect, useRef, useState } from "react";
import { api, errorMessage } from "../lib/api";
import { confirmAction } from "../lib/ui";
import { useAction } from "../lib/useAction";
import { StatusBanner } from "../components/StatusBanner";
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
  // `buy`/`requestActivation` teilen sich eine Instanz (B-49): beide sperren dieselbe Oberfläche, und ein
  // laufender Kauf soll auch das Einlösen sperren, nicht nur sich selbst. Das Nachladen der Kaufhistorie
  // bleibt außen vor - eine reine Paginierung, keine Mutation, mit eigener Sperre (`loadingHistory`).
  const action = useAction();
  const [msg, setMsg] = useState<string | null>(null);
  const [history, setHistory] = useState<MyShopPurchase[]>([]);
  const [historyTotal, setHistoryTotal] = useState(0);
  const [historyLoaded, setHistoryLoaded] = useState(false);
  const [historyLoading, setHistoryLoading] = useState(false);
  // Eigener Fehler-State, nicht der geteilte `msg`-Toast (B-111): der Toast ist nach 2 s weg, und danach
  // stand die Karte da und behauptete „Noch nichts gekauft" – eine Aussage über die Vergangenheit des
  // Kindes, die sie nach einem gescheiterten Request nicht belegen kann.
  const [historyError, setHistoryError] = useState<string | null>(null);

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
  // Zählt jedes Verwerfen des Verlaufs. Eine Anfrage, die VOR dem Verwerfen losgeschickt wurde, darf ihr
  // Ergebnis danach nicht mehr einhängen: sie trägt den Offset des alten Stands, und ihre Seite 2 landete
  // sonst im frisch geleerten Zustand – die Liste begänne bei Zeile 21 und der eben getätigte Kauf fehlte.
  const historyGeneration = useRef(0);
  const loadMoreHistory = useCallback(async () => {
    if (loadingHistory.current) return;
    loadingHistory.current = true;
    const generation = historyGeneration.current;
    setHistoryLoading(true);
    setHistoryError(null);
    try {
      const page = await api.shopPurchasesPage(history.length, HISTORY_PAGE);
      if (generation !== historyGeneration.current) return; // Verlauf wurde inzwischen verworfen
      setHistory((prev) => [...prev, ...page.items]);
      setHistoryTotal(page.total);
      setHistoryLoaded(true);
    } catch (e) {
      // Zweimal, weil es zwei Fragen beantwortet: der Toast, dass der Klick angekommen ist, der
      // Karten-Zustand, was jetzt gilt (B-111).
      flash(errorMessage(e));
      setHistoryError(errorMessage(e));
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
    if (!confirmAction(`„${listing.title}" für ${priceLabel(listing.coinPrice, listing.gemPrice)} kaufen?`)) return;
    // Kein `okText`: die Feier (`celebrate`) IST die Erfolgsmeldung - ein Banner daneben wäre doppelt
    // (frontend/CLAUDE.md, "Erfolg darf stumm bleiben").
    await action.run(async () => {
      const next = await api.purchaseListing(listing.id);
      setView(next);
      // Den geladenen Verlauf verwerfen, nicht ergänzen (B-110): der neue Kauf sitzt am Kopf der Liste,
      // ein Anhängen aus `next.purchases` käme ohne `X-Total-Count` (der `http`-Helfer liest keine
      // Header), und „X von Y" wäre danach falsch. Ohne das Verwerfen fehlte der eigene Kauf im Verlauf,
      // solange die Sitzung läuft – und „Mehr laden" holte die zuletzt gezeigte Zeile ein zweites Mal.
      historyGeneration.current += 1;
      setHistory([]);
      setHistoryTotal(0);
      setHistoryLoaded(false);
      setHistoryError(null);
      refreshWallet(); // Münzstand im HUD real aktualisieren (nach Kauf niedriger)
      celebrate("medium", ACTION_EMOJI[listing.actionType], "GEKAUFT!", listing.title);
    });
  }

  async function requestActivation(item: MyInventoryItem, quantity: number) {
    // Papa hat den Artikel gelöscht: die Einheiten bleiben (bezahlt ist bezahlt), einlösen geht aber
    // nicht mehr, weil die Anfrage über die Artikel-Id läuft. Lieber sagen, warum, als 404 zeigen.
    // `== null` fängt beides: der Vertrag erlaubt bei einem nullable Feld auch das Fehlen des Schlüssels.
    if (item.shopArticleId == null) {
      action.fail("Das gibt es bei Papa nicht mehr – frag ihn direkt danach. 🙋");
      return;
    }
    const shopArticleId = item.shopArticleId;
    if (!confirmAction(`${unitAmount(quantity, item.unitType)} „${item.title}" bei Papa anfragen?`)) return;
    // Anders als `buy`: keine eigene Feier vorgesehen, die Rückmeldung läuft also über den Banner-Text.
    await action.run(async () => {
      await api.activateInventory(shopArticleId, quantity);
      await load(); // Inventar (Menge sinkt) + Anfragen neu laden
      setTab("requests");
    }, "Anfrage an Papa geschickt! 📨");
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
        : tab === "buy" ? <BuyTab listings={view.available} busy={action.busy} onBuy={buy} />
        : tab === "stuff" ? <StuffTab inventory={view.inventory} busy={action.busy} onActivate={requestActivation} />
        : tab === "requests" ? <RequestsTab activations={activations} />
        : <HistoryTab purchases={history} total={historyTotal} loading={historyLoading}
            error={historyError} onLoadMore={loadMoreHistory} />}

      <StatusBanner message={action.message} />
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
 *
 * Die Karte kennt drei Zustände, nicht zwei (B-111): eine leere Liste heißt nur dann "nichts gekauft",
 * wenn wirklich geladen wurde. Ist das Laden gescheitert, ist "leer" eine Unbekannte – und die als
 * Auskunft über die Vergangenheit des Kindes auszugeben, war die Lüge, die es hier zu vermeiden gilt.
 */
export function HistoryTab({ purchases, total, loading, error, onLoadMore }: {
  purchases: MyShopPurchase[]; total: number; loading: boolean; error: string | null; onLoadMore: () => void;
}) {
  if (purchases.length === 0) {
    if (error != null)
      return (
        <div className="list">
          {/* `role="alert"` wie beim Toast: der dauerhafte Befund war vorher der einzige stumme – ein
              Screenreader bekam die 2-Sekunden-Meldung, aber nicht den Zustand, der bleibt. */}
          <div className="banner err" role="alert">{error}</div>
          <button type="button" className="btn ghost small" disabled={loading} onClick={onLoadMore}>
            {loading ? "Lädt…" : "Nochmal versuchen"}
          </button>
        </div>
      );
    if (loading) return null; // Weder "nichts gekauft" noch ein Fehler steht fest, solange geladen wird.
    return <p className="sub">Noch nichts gekauft. Hol dir im Tab <b>Kaufen</b> etwas Schönes! 🎁</p>;
  }
  return (
    <div className="list">
      {/* Scheitert eine SPÄTERE Seite, bleiben die schon geladenen Zeilen stehen - der "Mehr laden"-Knopf
          darunter ist dann selbst der Wiederholen-Knopf. */}
      {error != null && <div className="banner err" role="alert">{error}</div>}
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
