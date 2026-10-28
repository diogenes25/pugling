import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { HistoryTab } from "./SohnShop";
import { setLanguage } from "../lib/i18n";
import type { MyShopPurchase } from "../lib/types";

/**
 * Die Kaufhistorie-Karte des Sohn-Shops (B-99): ersetzt den stillen `Take(50)`-Schnitt durch ein
 * "mehr laden", das nur erscheint, solange noch nicht alle Käufe geladen sind, und die echte
 * Gesamtzahl zeigt statt sie zu verschweigen.
 *
 * Dazu die drei Zustände einer leeren Liste (B-111): nichts gekauft, noch am Laden, Laden gescheitert.
 * Nur der erste rechtfertigt eine Aussage über die Vergangenheit des Kindes.
 */
describe("HistoryTab", () => {
  const purchase = (overrides: Partial<MyShopPurchase> = {}): MyShopPurchase => ({
    id: 1, shopListingId: 1, articleNumber: "A-1", title: "Kinoabend",
    coinPrice: 50, gemPrice: 0, unitsPerPurchase: 1, status: "Owned",
    purchasedAt: "2026-08-01T12:00:00Z", closedAt: null,
    ...overrides,
  });

  it("zeigt den Hinweis 'noch nichts gekauft' nur bei einer leeren Liste OHNE Fehler", () => {
    render(<HistoryTab purchases={[]} total={0} loading={false} error={null} onLoadMore={vi.fn()} />);
    expect(screen.getByText(/Noch nichts gekauft/)).toBeTruthy();
  });

  it("zeigt 'Mehr laden' nur, solange weniger geladen ist als die Gesamtzahl", () => {
    const { rerender } = render(
      <HistoryTab purchases={[purchase()]} total={3} loading={false} error={null} onLoadMore={vi.fn()} />);
    expect(screen.getByRole("button", { name: /Mehr laden \(1 von 3\)/ })).toBeTruthy();

    // Alle geladen - kein Knopf mehr, kein "Ende der Liste"-Rätsel.
    rerender(<HistoryTab
      purchases={[purchase(), purchase({ id: 2 }), purchase({ id: 3 })]}
      total={3} loading={false} error={null} onLoadMore={vi.fn()} />);
    expect(screen.queryByRole("button", { name: /Mehr laden/ })).toBeNull();
  });

  it("meldet 'mehr laden' nach oben und sperrt den Knopf waehrend des Ladens", () => {
    const onLoadMore = vi.fn();
    const { rerender } = render(
      <HistoryTab purchases={[purchase()]} total={5} loading={false} error={null} onLoadMore={onLoadMore} />);
    fireEvent.click(screen.getByRole("button", { name: /Mehr laden/ }));
    expect(onLoadMore).toHaveBeenCalledTimes(1);

    rerender(<HistoryTab purchases={[purchase()]} total={5} loading={true} error={null} onLoadMore={onLoadMore} />);
    expect(screen.getByRole("button", { name: "Lädt…" })).toHaveProperty("disabled", true);
  });

  it("markiert stornierte Käufe", () => {
    render(<HistoryTab purchases={[purchase({ status: "Cancelled" })]} total={1} loading={false}
      error={null} onLoadMore={vi.fn()} />);
    expect(screen.getByText("storniert")).toBeTruthy();
  });

  // B-111: der Fehlerfall sah vorher aus wie "nichts gekauft" - die Karte behauptete also etwas über die
  // Vergangenheit des Kindes, was sie nach einem gescheiterten Request nicht wissen konnte.
  it("zeigt bei einem Fehler die Meldung und einen Wiederholen-Knopf statt der Leermeldung", () => {
    const onLoadMore = vi.fn();
    render(<HistoryTab purchases={[]} total={0} loading={false}
      error="Netzwerk nicht erreichbar" onLoadMore={onLoadMore} />);

    expect(screen.getByText("Netzwerk nicht erreichbar")).toBeTruthy();
    expect(screen.queryByText(/Noch nichts gekauft/)).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: "Nochmal versuchen" }));
    expect(onLoadMore).toHaveBeenCalledTimes(1);
  });

  /*
   * B-216. Der Lader setzt `loading` auf `true` und `error` auf `null` im selben Tick. Die Karte fiel damit
   * nach dem Klick auf „Nochmal versuchen" in den Lade-Zweig und renderte NICHTS: Knopf weg, Fokus auf
   * `<body>`, und die Beschriftung „Lädt…" war toter Code – im Zustand, den sie beschreibt, gab es den Knopf
   * nicht. Rot macht das eine Karte, die den letzten Fehler nicht festhält.
   */
  it("hält Meldung und Knopf während der Wiederholung fest, statt die Karte zu leeren", () => {
    const { rerender } = render(<HistoryTab purchases={[]} total={0} loading={false}
      error="Netzwerk nicht erreichbar" onLoadMore={vi.fn()} />);

    rerender(<HistoryTab purchases={[]} total={0} loading={true} error={null} onLoadMore={vi.fn()} />);

    expect(screen.getByText("Netzwerk nicht erreichbar")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Lädt…" })).toHaveProperty("disabled", true);

    // Geklappt: die Karte sagt wieder, was gilt – und nicht mehr, was war.
    rerender(<HistoryTab purchases={[]} total={0} loading={false} error={null} onLoadMore={vi.fn()} />);
    expect(screen.queryByText("Netzwerk nicht erreichbar")).toBeNull();
    expect(screen.getByText(/Noch nichts gekauft/)).toBeTruthy();
  });

  // Dieselbe Regel für die gefüllte Karte: Scheitert eine spätere Seite, bleibt die Meldung stehen, während
  // „Mehr laden" die nächste anstößt – statt beim Klick auszuhängen (Reviewer-Fund zu B-216).
  it("hält die Meldung einer gescheiterten spaeteren Seite fest, waehrend neu geladen wird", () => {
    const { rerender } = render(<HistoryTab purchases={[purchase()]} total={5} loading={false}
      error="Zeitüberschreitung" onLoadMore={vi.fn()} />);

    rerender(<HistoryTab purchases={[purchase()]} total={5} loading={true} error={null} onLoadMore={vi.fn()} />);

    expect(screen.getByText("Zeitüberschreitung")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Lädt…" })).toHaveProperty("disabled", true);
  });

  /*
   * B-228. Ein Knopf, der `disabled` wird, verliert in Chromium den Fokus (B-216, im E2E gemessen). Die vier
   * anderen Stellen legen ihn darum beim Klick auf die Meldung – die gefüllte Karte tat es nicht, ihr Fokus
   * fiel auf `<body>`. Gemessen wird der programmatische Umzug, den happy-dom kennt; den Chromium-Verlust
   * selbst deckt die Bauart. Rot macht das ein „Mehr laden" ohne `focus()`: dann ist `activeElement` der
   * `<body>`.
   */
  it("legt beim Nachladen nach einem Fehler den Fokus auf die Meldung, nicht ins Leere", () => {
    const onLoadMore = vi.fn();
    render(<HistoryTab purchases={[purchase()]} total={5} loading={false} error="Zeitüberschreitung" onLoadMore={onLoadMore} />);

    fireEvent.click(screen.getByRole("button", { name: /Mehr laden/ }));

    expect(onLoadMore).toHaveBeenCalledTimes(1);
    expect(document.activeElement).toBe(screen.getByRole("alert"));
  });

  it("sagt waehrend des ersten Ladens weder 'nichts gekauft' noch etwas ueber einen Fehler", () => {
    render(<HistoryTab purchases={[]} total={0} loading={true} error={null} onLoadMore={vi.fn()} />);
    expect(screen.queryByText(/Noch nichts gekauft/)).toBeNull();
  });

  // Scheitert erst die ZWEITE Seite, waere es falsch, die schon gezeigten Zeilen wegzunehmen: das Kind
  // verlöre Information, die es bereits hatte.
  it("behaelt geladene Zeilen, wenn eine spaetere Seite scheitert", () => {
    render(<HistoryTab purchases={[purchase()]} total={5} loading={false}
      error="Zeitüberschreitung" onLoadMore={vi.fn()} />);

    expect(screen.getByText("Kinoabend")).toBeTruthy();
    expect(screen.getByText("Zeitüberschreitung")).toBeTruthy();
    // Der vorhandene "Mehr laden"-Knopf ist hier selbst der Wiederholen-Weg.
    expect(screen.getByRole("button", { name: /Mehr laden/ })).toBeTruthy();
  });
});

/**
 * B-317: das Kaufdatum folgt der Arcade-Sprache, nicht der Locale des Browsers. Zwei Fälle, weil die Locale der
 * Testumgebung selbst eine der beiden Antworten gibt (lokal unter Windows gemessen: Deutsch; die Linux-CI vermutlich
 * en-US) – die alte Fassung ohne Sprache fängt darum je Umgebung nur einer der beiden. Plattformunabhängig fängt jeder
 * Fall die Mutante „fest in der anderen Sprache“.
 */
describe("HistoryTab, Datum", () => {
  afterEach(async () => { await setLanguage("de"); });
  const bought = [{
    id: 1, shopListingId: 1, articleNumber: "A-1", title: "Kinoabend", coinPrice: 50, gemPrice: 0, unitsPerPurchase: 1,
    status: "Owned", purchasedAt: "2026-08-01T12:00:00Z", closedAt: null,
  } satisfies MyShopPurchase];

  // Rot, wenn das Datum fest englisch formatiert wird – oder ohne Sprache in einer nicht deutschen Umgebung.
  it("steht auf Deutsch deutsch", () => {
    render(<HistoryTab purchases={bought} total={1} loading={false} error={null} onLoadMore={vi.fn()} />);
    expect(screen.getByText(/1\.8\.2026/)).toBeTruthy();
  });

  // Rot, wenn das Datum fest deutsch formatiert wird – oder ohne Sprache in einer deutschen Umgebung (lokal gemessen).
  it("steht auf Englisch englisch", async () => {
    await setLanguage("en");
    render(<HistoryTab purchases={bought} total={1} loading={false} error={null} onLoadMore={vi.fn()} />);
    expect(screen.getByText(/8\/1\/2026/)).toBeTruthy();
  });
});
