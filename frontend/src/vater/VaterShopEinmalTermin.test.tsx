import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { ShopListing, UpdateShopListingDto } from "../lib/types";
import { ListingRow } from "./VaterShop";

/*
 * B-782: das Datumsfeld eines „einmalig“-Angebots kennt nur den Tag. Ein Termin mit Uhrzeit (von einem API-Aufrufer
 * gesetzt) darf beim Speichern einer anderen Änderung nicht als Mitternacht zurückgehen – der Server läse einen neuen
 * Termin und füllte jedes Kind noch einmal auf (B-776).
 */
const ONCE = {
  id: 5, articleId: 2, title: "Kino", description: null, coinPrice: 10, gemPrice: 0, unitsPerPurchase: 1,
  maxStock: 1, refillQuantity: 1, refillKind: "Once", refillAtUtc: "2026-11-12T10:00:00", refillDayOfWeek: null,
  createdAt: "2026-11-10T00:00:00", childIds: [], stocks: [], active: true,
} as unknown as ShopListing;

function bearbeite(listing: ShopListing, aendern: () => void): UpdateShopListingDto {
  const onSave = vi.fn();
  render(<table><tbody><ListingRow listing={listing} unitType={"Piece" as never} kids={[]} busy={false}
    onFail={() => {}} onSave={onSave} onFill={() => {}} onToggle={() => {}} onRemove={() => {}} /></tbody></table>);
  fireEvent.click(screen.getByRole("button", { name: "Bearbeiten" }));
  aendern();
  fireEvent.click(screen.getByRole("button", { name: "OK" }));
  expect(onSave).toHaveBeenCalledTimes(1);
  return onSave.mock.calls[0][0] as UpdateShopListingDto;
}

describe("Vater-Shop – einmaliger Termin beim Bearbeiten (B-782)", () => {
  // AK 1. Rot, solange `planDto` immer `<Datum>T00:00:00Z` baut: dann geht 2026-11-12T00:00:00Z statt des geladenen Werts.
  it("schickt den geladenen Termin unverändert, wenn nur der Preis geändert wird", () => {
    const dto = bearbeite(ONCE, () =>
      fireEvent.change(screen.getByLabelText("Preis 🪙", { exact: true }), { target: { value: "12" } }));
    expect(dto.coinPrice).toBe(12);
    expect(dto.refillAtUtc).toBe("2026-11-12T10:00:00");
  });

  // AK 2. Rot, wenn der geladene Wert immer zurückgeht: das neue Datum käme nie beim Server an.
  it("schickt ein geändertes Datum als Mitternacht UTC", () => {
    const dto = bearbeite(ONCE, () =>
      fireEvent.change(screen.getByLabelText("Datum", { exact: true }), { target: { value: "2026-11-20" } }));
    expect(dto.refillAtUtc).toBe("2026-11-20T00:00:00Z");
  });

  // AK 3. Rot, wenn ein leerer geladener Termin als „gleiches Datum“ gilt: die Umstellung schickte dann nichts (oder
  // den leeren Wert), und der Server lehnte sie ab (B-776, Termin Pflicht beim Wechsel).
  it("eine Umstellung von wöchentlich auf einmalig schickt das gewählte Datum", () => {
    const weekly = { ...ONCE, refillKind: "Weekly", refillAtUtc: null, refillDayOfWeek: "Monday" } as unknown as ShopListing;
    const dto = bearbeite(weekly, () => {
      fireEvent.change(screen.getByLabelText("Auffüllen", { exact: true }), { target: { value: "Once" } });
      fireEvent.change(screen.getByLabelText("Datum", { exact: true }), { target: { value: "2026-11-20" } });
    });
    expect(dto.refillKind).toBe("Once");
    expect(dto.refillAtUtc).toBe("2026-11-20T00:00:00Z");
  });
});
