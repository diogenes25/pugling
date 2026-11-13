import { useCallback, useEffect, useRef, useState } from "react";
import { arcadeDatum } from "../lib/datum";
import { Trans, useTranslation } from "react-i18next";
import { api } from "../lib/api";
import { useErrorMessage } from "../lib/chromeText";
import { confirmAction } from "../lib/ui";
import { useAction } from "../lib/useAction";
import { useGemerkterFehler } from "../lib/useGemerkterFehler";
import { StatusBanner } from "../components/StatusBanner";
import { ACTION_EMOJI, priceLabel } from "../lib/shop";
import type { MyInventoryItem, MyActivation, MyShopPurchase, ShopAvailableListing, ShopView, UnitType } from "../lib/types";
import { useSohn } from "./SohnApp";

type Tab = "buy" | "stuff" | "requests" | "history";

// Wie viele Zeilen ein "Mehr laden" auf einmal nachlaedt (B-99: die Kaufhistorie hatte vorher einen
// stillen Take(50)-Deckel und keinen Weg zu aelteren Zeilen).
const HISTORY_PAGE = 20;

const STATUS_PILL = {
  Pending: { cls: "gold", label: "shop.statusPending" },
  Approved: { cls: "lime", label: "shop.statusApproved" },
  Rejected: { cls: "red", label: "shop.statusRejected" },
} as const satisfies Record<MyActivation["status"], { cls: string; label: string }>;

/**
 * Menge, Preis und Datum in der Arcade-Sprache (B-304, B-317). `unitAmount`/`priceLabel` aus `lib/shop.ts` bleiben deutsch, weil
 * das Vater-Web sie auch ruft; von `priceLabel` leiht sich die Arcade nur die sprachfreien Teile (Emoji + Zahl).
 */
export function useShopText() {
  const { t, i18n } = useTranslation();
  return {
    t,
    // Lese-Hälfte aus lib/datum: ein Zeitstempel ohne Offset ist UTC, nicht Ortszeit (B-447).
    date: (iso: string): string => arcadeDatum(iso, i18n.language),
    amount: (quantity: number, unit: UnitType): string => t(`shop.unit.${unit}`, { count: quantity }),
    price: (coinPrice: number, gemPrice: number): string =>
      coinPrice > 0 || gemPrice > 0 ? priceLabel(coinPrice, gemPrice) : t("shop.free"),
  };
}

/**
 * Familien-Shop aus Sohn-Sicht: verdiente 🪙 Münzen in echte Vater-Belohnungen umsetzen.
 * Drei Tabs – Kaufen (Angebote), Sachen (gekauftes Inventar + Einlösen beantragen) und Anfragen (Status).
 * Gegenstück zu {@link SohnSkins} (dort werden 💎 Gems gegen Charaktere getauscht).
 */
export function SohnShop() {
  const { coins, gems, refreshWallet, celebrate } = useSohn();
  const { t, amount, price } = useShopText();
  const [tab, setTab] = useState<Tab>("buy");
  const [view, setView] = useState<ShopView | null>(null);
  const [activations, setActivations] = useState<MyActivation[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  // `buy`/`requestActivation` teilen sich eine Instanz (B-49): beide sperren dieselbe Oberfläche, und ein
  // laufender Kauf soll auch das Einlösen sperren, nicht nur sich selbst. Das Nachladen der Kaufhistorie
  // bleibt außen vor - eine reine Paginierung, keine Mutation, mit eigener Sperre (`loadingHistory`).
  const errorMessage = useErrorMessage();
  const action = useAction(errorMessage);
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
    if (!confirmAction(t("shop.confirmBuy", { title: listing.title, price: price(listing.coinPrice, listing.gemPrice) }))) return;
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
      celebrate("medium", ACTION_EMOJI[listing.actionType], t("shop.boughtTitle"), listing.title);
    });
  }

  async function requestActivation(item: MyInventoryItem, quantity: number) {
    // Papa hat den Artikel gelöscht: die Einheiten bleiben (bezahlt ist bezahlt), einlösen geht aber
    // nicht mehr, weil die Anfrage über die Artikel-Id läuft. Lieber sagen, warum, als 404 zeigen.
    // `== null` fängt beides: der Vertrag erlaubt bei einem nullable Feld auch das Fehlen des Schlüssels.
    if (item.shopArticleId == null) {
      action.fail(t("shop.articleGone"));
      return;
    }
    const shopArticleId = item.shopArticleId;
    if (!confirmAction(t("shop.confirmRequest", { amount: amount(quantity, item.unitType), title: item.title }))) return;
    // Anders als `buy`: keine eigene Feier vorgesehen, die Rückmeldung läuft also über den Banner-Text.
    await action.run(async () => {
      await api.activateInventory(shopArticleId, quantity);
      await load(); // Inventar (Menge sinkt) + Anfragen neu laden
      setTab("requests");
    }, t("shop.requestSent"));
  }

  return (
    <div className="sohn-body">
      <div className="row">
        <span className="screen-title" style={{ margin: 0 }}>{t("shop.title")}</span>
        <span className="chip" style={{ marginLeft: "auto" }}>🪙<b className="tabnum">{coins}</b></span>
        <span className="chip">💎<b className="tabnum">{gems}</b></span>
      </div>
      <p className="sub"><Trans i18nKey="shop.intro" components={{ b: <b /> }} /></p>

      <div className="row" role="group" aria-label={t("shop.areas")} style={{ gap: 6 }}>
        <TabButton active={tab === "buy"} onClick={() => setTab("buy")}>{t("shop.tabBuy")}</TabButton>
        <TabButton active={tab === "stuff"} onClick={() => setTab("stuff")}>
          {t("shop.tabStuff")}{view && view.inventory.length > 0 ? ` (${view.inventory.length})` : ""}
        </TabButton>
        <TabButton active={tab === "requests"} onClick={() => setTab("requests")}>
          {t("shop.tabRequests")}{activations.some((a) => a.status === "Pending") ? " •" : ""}
        </TabButton>
        <TabButton active={tab === "history"} onClick={openHistoryTab}>{t("shop.tabHistory")}</TabButton>
      </div>

      {loading ? <div className="loading">{t("shop.loading")}</div>
        : error ? <div className="banner err">{error}</div>
        : !view ? null
        : tab === "buy" ? <BuyTab listings={view.available} busy={action.busy} onBuy={buy} />
        : tab === "stuff" ? <StuffTab inventory={view.inventory} busy={action.busy} onActivate={requestActivation} />
        : tab === "requests" ? <RequestsTab activations={activations} />
        : <HistoryTab purchases={history} total={historyTotal} loading={historyLoading}
            error={historyError} onLoadMore={loadMoreHistory} />}

      <StatusBanner message={action.message} />
      <div className={`live-region${msg ? " toast" : ""}`} role="status" aria-live="polite">{msg}</div>
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
  const { t, amount, price } = useShopText();
  if (listings.length === 0)
    return <p className="sub">{t("shop.noListings")}</p>;
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
              {amount(l.unitsPerPurchase, l.unitType)}
              {l.description ? ` · ${l.description}` : ""}
            </div>
            {soldOut ? <span className="pill red">{t("shop.soldOut")}</span>
              : <span className={`pill ${l.affordable ? "gold" : "red"}`}>{price(l.coinPrice, l.gemPrice)}</span>}
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
    return <p className="sub"><Trans i18nKey="shop.nothingBought" components={{ b: <b /> }} /></p>;
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
  const { t, amount } = useShopText();
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
            {t("shop.youHave", { amount: amount(item.quantity, item.unitType) })}
            {weg && ` · ${t("shop.goneAtDad")}`}
          </div>
        </div>
      </div>
      <div className="row" style={{ marginTop: 8, gap: 8 }}>
        <input
          type="number"
          min={1}
          max={item.quantity}
          value={qty}
          aria-label={t("shop.redeemAmount", { max: item.quantity })}
          onChange={(e) => setQty(Number(e.target.value))}
          style={{ width: 90, background: "#0c0e2c", border: "1.5px solid var(--stroke)", borderRadius: 12, color: "var(--ink)", padding: 10, fontSize: 16 }}
        />
        <button type="button" className="btn small lime" style={{ width: "auto", flex: 1 }}
          disabled={busy || weg} onClick={() => onActivate(item, clamped)}>
          {t("shop.redeem")}
        </button>
      </div>
    </div>
  );
}

function RequestsTab({ activations }: { activations: MyActivation[] }) {
  const { t, amount, date } = useShopText();
  if (activations.length === 0)
    return <p className="sub"><Trans i18nKey="shop.noRequests" components={{ b: <b /> }} /></p>;
  return (
    <div className="list">
      {activations.map((a) => {
        const pill = STATUS_PILL[a.status];
        return (
          <div key={a.id} className="row" style={{ justifyContent: "space-between", padding: "8px 0" }}>
            <div>
              <b>{a.articleTitle}</b>
              <div className="sub">{amount(a.requestedQuantity, a.unitType)} · {date(a.requestedAt)}</div>
            </div>
            <span className={`pill ${pill.cls}`}>{t(pill.label)}</span>
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
  /*
   * Der letzte Fehler überlebt den Start der Wiederholung (B-216). Der Lader setzt `loading` und `error`
   * im selben Tick; ohne das Gedächtnis fiel die leere Karte nach dem Klick auf „Nochmal versuchen" in den
   * Lade-Zweig und renderte NICHTS – der Knopf verschwand unter dem Finger, und sein „Lädt…" war toter
   * Code. Vor den frühen `return`s, weil es ein Hook ist.
   */
  const { t, price, date } = useShopText();
  const gescheitert = useGemerkterFehler(error, loading);
  // Ein gesperrter Knopf verliert in Chromium den Fokus – darum wandert er beim Klick auf die Meldung
  // (Begründung in `ListenAusfall.tsx`).
  const meldung = useRef<HTMLDivElement>(null);
  // Ohne Meldung nimmt die Liste selbst den Fokus (B-231, Entscheidung des Nutzers): sonst fiele er beim
  // gesperrten „Mehr laden" auf `<body>`, und der Sohn stünde nach dem Laden wieder am Seitenanfang. Auch
  // wenn die letzte Seite den Knopf ganz verschwinden lässt, bleibt der Fokus so in der Liste.
  const liste = useRef<HTMLDivElement>(null);
  if (purchases.length === 0) {
    if (gescheitert != null)
      return (
        <div className="list">
          {/* `role="alert"` wie beim Toast: der dauerhafte Befund war vorher der einzige stumme – ein
              Screenreader bekam die 2-Sekunden-Meldung, aber nicht den Zustand, der bleibt. */}
          <div className="banner err" role="alert" tabIndex={-1} ref={meldung}>{gescheitert}</div>
          <button type="button" className="btn ghost small" disabled={loading}
            onClick={() => { meldung.current?.focus(); onLoadMore(); }}>
            {loading ? t("shop.loadingMore") : t("shop.retry")}
          </button>
        </div>
      );
    if (loading) return null; // Weder "nichts gekauft" noch ein Fehler steht fest, solange geladen wird.
    return <p className="sub"><Trans i18nKey="shop.nothingBought" components={{ b: <b /> }} /></p>;
  }
  return (
    <div className="list" tabIndex={-1} ref={liste}>
      {/* Scheitert eine SPÄTERE Seite, bleiben die schon geladenen Zeilen stehen - der "Mehr laden"-Knopf
          darunter ist dann selbst der Wiederholen-Knopf. */}
      {/* Auch hier `gescheitert`: Sonst verschwände die Meldung, sobald „Mehr laden" die nächste Seite
          anstößt – derselbe Aushänger wie oben, nur mit stehenbleibendem Knopf (Reviewer-Fund, B-216).
          Und dieselbe Fokusregel wie oben (B-228): der gesperrte Knopf verliert den Fokus, die Meldung nimmt
          ihn – und ohne Meldung die Liste (B-231). */}
      {gescheitert != null && <div className="banner err" role="alert" tabIndex={-1} ref={meldung}>{gescheitert}</div>}
      {purchases.map((p) => (
        <div key={p.id} className="row" style={{ justifyContent: "space-between", padding: "8px 0" }}>
          <div>
            <b>{p.title}</b>
            <div className="sub">{price(p.coinPrice, p.gemPrice)} · {date(p.purchasedAt)}</div>
          </div>
          {p.status === "Cancelled" && <span className="pill red">{t("shop.cancelled")}</span>}
        </div>
      ))}
      {purchases.length < total && (
        <button type="button" className="btn ghost small" style={{ marginTop: 8 }} disabled={loading}
          onClick={() => { (meldung.current ?? liste.current)?.focus(); onLoadMore(); }}>
          {loading ? t("shop.loadingMore") : t("shop.loadMore", { shown: purchases.length, total })}
        </button>
      )}
    </div>
  );
}
