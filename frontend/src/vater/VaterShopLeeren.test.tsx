import { act, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "../lib/api";
import type { ShopArticle, ShopListing, UpdateShopListingDto } from "../lib/types";
import { useAction } from "../lib/useAction";
import { ArticleRow, ListingRow } from "./VaterShop";

/*
 * B-789: ein geleertes Feld geht als `""` zum Server – der leert damit. `null` hieße beim PATCH „unverändert“: die
 * Oberfläche meldete „gespeichert“, und der alte Wert stünde weiter da. Dass `""` am Server leert, hält
 * `ShopStockPerChildTests.B789_…`.
 */
afterEach(() => vi.restoreAllMocks());

describe("Vater-Shop – geleerte Felder (B-789)", () => {
  // AK 1. Rot mit `title: form.title.trim() || null` (Ausgangszustand).
  it("ein geleerter Angebotstitel geht als leerer Text hinaus", () => {
    const listing = {
      id: 5, articleId: 2, title: "Kino", description: "", coinPrice: 10, gemPrice: 0, unitsPerPurchase: 1, maxStock: 1,
      refillQuantity: 1, refillKind: "None", refillAtUtc: null, refillDayOfWeek: null, createdAt: "2026-11-10T00:00:00",
      childIds: [], stocks: [], active: true,
    } as unknown as ShopListing;
    const onSave = vi.fn();
    render(<table><tbody><ListingRow listing={listing} unitType={"Stueck" as never} kids={[]} busy={false}
      onFail={() => {}} onSave={onSave} onFill={() => {}} onToggle={() => {}} onRemove={() => {}} /></tbody></table>);
    fireEvent.click(screen.getByRole("button", { name: "Bearbeiten" }));
    fireEvent.change(screen.getByLabelText("Titel", { exact: true }), { target: { value: "  " } });
    fireEvent.click(screen.getByRole("button", { name: "OK" }));
    expect((onSave.mock.calls[0][0] as UpdateShopListingDto).title).toBe("");
  });

  // AK 2. Rot mit `description: form.description.trim() || null` (Ausgangszustand).
  it("eine geleerte Artikel-Beschreibung geht als leerer Text hinaus", async () => {
    const article = {
      id: 2, articleNumber: "K-1", title: "Kino", description: "Ein Film am Wochenende", unitType: "Stueck",
      actionType: "Sonstiges", createdAt: "2026-11-10T00:00:00",
    } as unknown as ShopArticle;
    const update = vi.spyOn(api, "updateShopArticle").mockResolvedValue(article as never);
    function Zeile() {
      const action = useAction();
      return <table><tbody><ArticleRow article={article} selected={false} action={action}
        onToggleListings={() => {}} onSaved={() => {}} onRemove={() => {}} /></tbody></table>;
    }
    render(<Zeile />);
    fireEvent.click(screen.getByRole("button", { name: "Bearbeiten" }));
    fireEvent.change(screen.getByLabelText("Beschreibung", { exact: true }), { target: { value: "" } });
    await act(async () => { fireEvent.click(screen.getByRole("button", { name: "OK" })); });
    expect(update).toHaveBeenCalledTimes(1);
    expect((update.mock.calls[0][1] as { description: string | null }).description).toBe("");
  });
});
