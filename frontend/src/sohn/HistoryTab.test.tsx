import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { HistoryTab } from "./SohnShop";
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
