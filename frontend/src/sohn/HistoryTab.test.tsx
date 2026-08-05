import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { HistoryTab } from "./SohnShop";
import type { MyShopPurchase } from "../lib/types";

/**
 * Die Kaufhistorie-Karte des Sohn-Shops (B-99): ersetzt den stillen `Take(50)`-Schnitt durch ein
 * "mehr laden", das nur erscheint, solange noch nicht alle Käufe geladen sind, und die echte
 * Gesamtzahl zeigt statt sie zu verschweigen.
 */
describe("HistoryTab", () => {
  const purchase = (overrides: Partial<MyShopPurchase> = {}): MyShopPurchase => ({
    id: 1, shopListingId: 1, articleNumber: "A-1", title: "Kinoabend",
    coinPrice: 50, gemPrice: 0, unitsPerPurchase: 1, status: "Owned",
    purchasedAt: "2026-08-01T12:00:00Z", closedAt: null,
    ...overrides,
  });

  it("zeigt einen Hinweis statt einer leeren Liste, solange noch nichts geladen ist", () => {
    render(<HistoryTab purchases={[]} total={0} loading={false} onLoadMore={vi.fn()} />);
    expect(screen.getByText(/Noch nichts gekauft/)).toBeTruthy();
  });

  it("zeigt 'Mehr laden' nur, solange weniger geladen ist als die Gesamtzahl", () => {
    const { rerender } = render(
      <HistoryTab purchases={[purchase()]} total={3} loading={false} onLoadMore={vi.fn()} />);
    expect(screen.getByRole("button", { name: /Mehr laden \(1 von 3\)/ })).toBeTruthy();

    // Alle geladen - kein Knopf mehr, kein "Ende der Liste"-Rätsel.
    rerender(<HistoryTab
      purchases={[purchase(), purchase({ id: 2 }), purchase({ id: 3 })]}
      total={3} loading={false} onLoadMore={vi.fn()} />);
    expect(screen.queryByRole("button", { name: /Mehr laden/ })).toBeNull();
  });

  it("meldet 'mehr laden' nach oben und sperrt den Knopf waehrend des Ladens", () => {
    const onLoadMore = vi.fn();
    const { rerender } = render(
      <HistoryTab purchases={[purchase()]} total={5} loading={false} onLoadMore={onLoadMore} />);
    fireEvent.click(screen.getByRole("button", { name: /Mehr laden/ }));
    expect(onLoadMore).toHaveBeenCalledTimes(1);

    rerender(<HistoryTab purchases={[purchase()]} total={5} loading={true} onLoadMore={onLoadMore} />);
    expect(screen.getByRole("button", { name: "Lädt…" })).toHaveProperty("disabled", true);
  });

  it("markiert stornierte Käufe", () => {
    render(<HistoryTab purchases={[purchase({ status: "Cancelled" })]} total={1} loading={false} onLoadMore={vi.fn()} />);
    expect(screen.getByText("storniert")).toBeTruthy();
  });
});
