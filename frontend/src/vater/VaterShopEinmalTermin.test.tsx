import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { ChildResponse, ShopListing, UpdateShopListingDto } from "../lib/types";
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

/*
 * B-786: „OK“ schickt nur einen Termin, den der Vater in diesem Formular sieht und gewählt hat – keinen Rest-Termin eines
 * früheren „einmalig“ und keinen, den er mit „Abbrechen“ verworfen hat.
 */
function zeile(listing: ShopListing, kids: ChildResponse[] = []) {
  const onSave = vi.fn();
  render(<table><tbody><ListingRow listing={listing} unitType={"Piece" as never} kids={kids} busy={false}
    onFail={() => {}} onSave={onSave} onFill={() => {}} onToggle={() => {}} onRemove={() => {}} /></tbody></table>);
  return onSave;
}

describe("Vater-Shop – Bearbeiten beginnt beim gespeicherten Angebot (B-786)", () => {
  // AK 1. Rot, solange das Datum unabhängig von der Art aus dem Termin vorbelegt wird: dann stünde 2026-11-12 im Feld, „OK“
  // wäre frei und der Rest-Termin ginge ungewählt hinaus.
  it("ein wöchentliches Angebot mit Rest-Termin öffnet „einmalig“ mit leerem Datum und gesperrtem OK", () => {
    zeile({ ...ONCE, refillKind: "Weekly", refillDayOfWeek: "Monday" } as unknown as ShopListing);
    fireEvent.click(screen.getByRole("button", { name: "Bearbeiten" }));
    fireEvent.change(screen.getByLabelText("Auffüllen", { exact: true }), { target: { value: "Once" } });
    expect((screen.getByLabelText("Datum", { exact: true }) as HTMLInputElement).value).toBe("");
    expect((screen.getByRole("button", { name: "OK" }) as HTMLButtonElement).disabled).toBe(true);
  });

  // Review B-786: wählt der Vater nach dem Wechsel selbst den Tag des Rest-Termins, geht Mitternacht hinaus, nicht dessen
  // verborgene Uhrzeit. Rot, wenn `planDto` den geladenen Termin auch bei einem nicht-einmaligen Angebot vergleicht.
  it("der selbst gewählte Tag des Rest-Termins geht als Mitternacht hinaus, nicht mit dessen Uhrzeit", () => {
    const onSave = zeile({ ...ONCE, refillKind: "Weekly", refillDayOfWeek: "Monday" } as unknown as ShopListing);
    fireEvent.click(screen.getByRole("button", { name: "Bearbeiten" }));
    fireEvent.change(screen.getByLabelText("Auffüllen", { exact: true }), { target: { value: "Once" } });
    fireEvent.change(screen.getByLabelText("Datum", { exact: true }), { target: { value: "2026-11-12" } });
    fireEvent.click(screen.getByRole("button", { name: "OK" }));
    expect((onSave.mock.calls[0][0] as UpdateShopListingDto).refillAtUtc).toBe("2026-11-12T00:00:00Z");
  });

  // AK 2. Rot, wenn „Bearbeiten“ den Plan nicht neu belegt: das verworfene 2026-11-20 stünde im Feld und ginge als neuer
  // Termin hinaus.
  it("nach „Abbrechen“ steht wieder der gespeicherte Termin, und er geht unverändert hinaus", () => {
    const onSave = zeile(ONCE);
    fireEvent.click(screen.getByRole("button", { name: "Bearbeiten" }));
    fireEvent.change(screen.getByLabelText("Datum", { exact: true }), { target: { value: "2026-11-20" } });
    fireEvent.click(screen.getByRole("button", { name: "Abbrechen" }));
    fireEvent.click(screen.getByRole("button", { name: "Bearbeiten" }));
    expect((screen.getByLabelText("Datum", { exact: true }) as HTMLInputElement).value).toBe("2026-11-12");
    fireEvent.change(screen.getByLabelText("Preis 🪙", { exact: true }), { target: { value: "12" } });
    fireEvent.click(screen.getByRole("button", { name: "OK" }));
    expect((onSave.mock.calls[0][0] as UpdateShopListingDto).refillAtUtc).toBe("2026-11-12T10:00:00");
  });

  // AK 3. Rot, wenn „Bearbeiten“ das Formular nicht neu belegt: der verworfene Preis 99 stünde im Feld.
  it("nach „Abbrechen“ steht wieder der gespeicherte Preis", () => {
    zeile(ONCE);
    fireEvent.click(screen.getByRole("button", { name: "Bearbeiten" }));
    fireEvent.change(screen.getByLabelText("Preis 🪙", { exact: true }), { target: { value: "99" } });
    fireEvent.click(screen.getByRole("button", { name: "Abbrechen" }));
    fireEvent.click(screen.getByRole("button", { name: "Bearbeiten" }));
    expect((screen.getByLabelText("Preis 🪙", { exact: true }) as HTMLInputElement).value).toBe("10");
  });

  // Wie AK 3 für die Zielgruppe. Rot, wenn „Bearbeiten“ sie nicht neu belegt: das verworfene Häkchen bei Ben stünde noch.
  it("nach „Abbrechen“ steht wieder die gespeicherte Zielgruppe", () => {
    const kids = [{ id: 1, name: "Ada" }, { id: 2, name: "Ben" }] as unknown as ChildResponse[];
    zeile(ONCE, kids);
    fireEvent.click(screen.getByRole("button", { name: "Bearbeiten" }));
    fireEvent.click(screen.getByRole("checkbox", { name: "Ben" }));
    fireEvent.click(screen.getByRole("button", { name: "Abbrechen" }));
    fireEvent.click(screen.getByRole("button", { name: "Bearbeiten" }));
    expect((screen.getByRole("checkbox", { name: "Ben" }) as HTMLInputElement).checked).toBe(false);
  });

  // AK 4. Rot, wenn das Datum immer leer vorbelegt wird: ein „einmalig“-Angebot öffnete ohne seinen Termin.
  it("ein einmaliges Angebot öffnet mit seinem Datum", () => {
    zeile(ONCE);
    fireEvent.click(screen.getByRole("button", { name: "Bearbeiten" }));
    expect((screen.getByLabelText("Datum", { exact: true }) as HTMLInputElement).value).toBe("2026-11-12");
  });
});
