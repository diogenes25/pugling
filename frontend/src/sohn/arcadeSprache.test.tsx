import { afterEach, describe, expect, it, vi } from "vitest";
import { render, renderHook, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { setLanguage } from "../lib/i18n";
import { AuthProvider } from "../lib/auth";
import { pointKindLabel, interestFacetLabel } from "../lib/labels";
import { priceLabel, unitAmount } from "../lib/shop";
import { SKINS } from "../lib/skins";
import { de } from "../i18n/de";
import { SohnApp } from "./SohnApp";
import { HistoryTab, useShopText } from "./SohnShop";
import { useSkinText } from "../lib/skinText";
import type { MyShopPurchase } from "../lib/types";

/*
 * B-304 – der Rahmen der Arcade auf Englisch. Je Fall steht dabei, welche Änderung ihn rot macht.
 */

// Ein gefallener Fall darf die Sprache nicht auf Englisch stehen lassen – die anderen Dateien erwarten Deutsch.
afterEach(async () => { await setLanguage("de"); });

describe("Skin-Texte", () => {
  it("kennen in den Ressourcen genau die Skin-Ids des Katalogs, mit denselben deutschen Texten", () => {
    // Rot, wenn ein Skin in `lib/skins.ts` dazukommt, ohne dass die Ressourcen ihn tragen (oder umgekehrt) – dann
    // zeigte die englische Arcade für ihn still den deutschen Rückfall.
    expect(Object.keys(de.sohn.skins.catalog).sort()).toEqual(SKINS.map((s) => s.id).sort());
    for (const s of SKINS) {
      const text = de.sohn.skins.catalog[s.id as keyof typeof de.sohn.skins.catalog];
      expect({ name: text.name, blurb: text.blurb }).toEqual({ name: s.name, blurb: s.blurb });
    }
  });

  it("folgen der Arcade-Sprache und fallen bei unbekannter Id auf den Katalogtext zurück", async () => {
    // Rot, wenn `useSkinText` den Katalogtext statt der Ressource liefert (dann bliebe „Fuchs“ stehen).
    await setLanguage("en");
    const { result } = renderHook(() => useSkinText());
    const fox = SKINS.find((s) => s.id === "fox")!;
    expect(result.current.name(fox)).toBe("Fox");
    expect(result.current.blurb(fox)).toBe("Quick and clever.");
    // Rot, wenn die Id ungeprüft in den Schlüssel wandert – dann stünde „skins.catalog.yeti.name“ im Bild.
    const yeti = { ...fox, id: "yeti", name: "Yeti", blurb: "Kalt." };
    expect(result.current.name(yeti)).toBe("Yeti");
  });
});

describe("Geteilte Helfer bleiben deutsch", () => {
  // AK 3: das Vater-Web ruft diese Helfer und ist nicht übersetzt. Rot, sobald einer von ihnen der globalen
  // i18next-Sprache folgt – ein Vater am Gerät eines englischen Kindes stünde sonst in einem halb englischen Web.
  it("auch wenn die Arcade auf Englisch steht", async () => {
    await setLanguage("en");
    expect(pointKindLabel("Base")).toBe("Wiederholung");
    expect(interestFacetLabel("Animal")).toBe("Tiere");
    expect(unitAmount(30, "Minute")).toBe("30 Min");
    expect(priceLabel(0, 0)).toBe("gratis");
  });
});

describe("Shop-Verlauf auf Englisch", () => {
  // Der Titel ist mit Absicht ein vorhandener Schlüssel: schickte jemand ihn durch `t`, würde aus „shop.cancelled“
  // sichtbar „cancelled“. Ein gewöhnlicher Titel bliebe gleich – `t` liefert bei fehlendem Schlüssel den Schlüssel.
  const purchase = (id: number, coinPrice: number): MyShopPurchase => ({
    id, shopListingId: 1, articleNumber: "A-1", title: "shop.cancelled",
    coinPrice, gemPrice: 0, unitsPerPurchase: 1, status: "Cancelled",
    purchasedAt: "2026-08-01T12:00:00Z", closedAt: null,
  });

  it("übersetzt Chrome und Preis, lässt aber den Titel des Vaters stehen", async () => {
    await setLanguage("en");
    render(<HistoryTab purchases={[purchase(1, 0)]} total={3} loading={false} error={null} onLoadMore={vi.fn()} />);
    // Rot, wenn der Knopf, die Storno-Pille oder der Gratis-Preis noch am deutschen Literal hängt.
    expect(screen.getByRole("button", { name: "Load more (1 of 3)" })).toBeTruthy();
    expect(screen.getByText("cancelled")).toBeTruthy();
    expect(screen.getByText(/^free · /)).toBeTruthy();
    // Nutzerdatum (AK 2): rot, wenn jemand den Titel durch `t` schickt.
    expect(screen.getByText("shop.cancelled")).toBeTruthy();
  });

  it("setzt im leeren Verlauf den fetten Tab-Namen in den englischen Satz", async () => {
    await setLanguage("en");
    const { container } = render(<HistoryTab purchases={[]} total={0} loading={false} error={null} onLoadMore={vi.fn()} />);
    // Rot, wenn die `<b>`-Komponente der `Trans`-Zeile verloren geht (dann stünde „<b>Buy</b>“ als Text da).
    expect(container.querySelector("b")?.textContent).toBe("Buy");
    expect(container.textContent).toBe("Nothing bought yet. Get yourself something nice in the Buy tab! 🎁");
  });
});

const UNITS = ["Stueck", "Minute", "Stunde", "Gramm", "Mal"] as const;

describe("Mengen im Shop", () => {
  it.each(UNITS.flatMap((unit) => [1, 2].map((count) => [unit, count] as const)))(
    "de: %s × %i spricht wie der Vater-Helfer", async (unit, count) => {
      // Rot, wenn `de.ts` und `UNIT_LABEL` für eine Einheit auseinanderlaufen – dann sprächen Arcade und Vater-Web
      // dieselbe Menge verschieden aus.
      const { result } = renderHook(() => useShopText());
      expect(result.current.amount(count, unit)).toBe(unitAmount(count, unit));
    });

  it("nimmt im Shop die Arcade-Sprache, nicht den deutschen Helfer", async () => {
    // Rot, wenn `amount` wieder `unitAmount` ruft (dann „2 Stück“) oder `count` vergisst (dann der Schlüsselname).
    await setLanguage("en");
    const { result } = renderHook(() => useShopText());
    expect(result.current.amount(2, "Stueck")).toBe("2 pieces");
  });

  it.each([
    ["en", 1, "Stueck", "1 piece"],
    ["en", 2, "Stueck", "2 pieces"],
    ["en", 1, "Mal", "1 time"],
    ["en", 2, "Mal", "2 times"],
  ] as const)("%s: %i %s heißt „%s“", async (lang, count, unit, expected) => {
    await setLanguage(lang);
    const { default: i18n } = await import("../lib/i18n");
    // Prüft die Ressource: rot, wenn eine englische Pluralform vertauscht ist. Eine Aufrufstelle ohne `count`
    // fängt der `amount`-Fall oben, nicht dieser.
    expect(i18n.t(`sohn:shop.unit.${unit}`, { count })).toBe(expected);
  });
});

describe("<html lang> an der Arcade-Wurzel", () => {
  it("trägt schon der Login die Sprache, und das Verlassen der Arcade fällt auf Deutsch zurück", async () => {
    await setLanguage("en");
    localStorage.removeItem("pugling.session");
    const view = render(
      <AuthProvider><MemoryRouter><SohnApp /></MemoryRouter></AuthProvider>,
    );
    // Rot, wenn der Effekt wieder in der Shell sitzt: dann bliebe der Login ohne `lang="en"`.
    expect(await screen.findByRole("button", { name: "▶ GO" })).toBeTruthy();
    expect(document.documentElement.lang).toBe("en");
    // Rot, wenn die Aufräumzeile fehlt: das Vater-Web nach der Arcade stünde dann auf Englisch.
    view.unmount();
    expect(document.documentElement.lang).toBe("de");
  });
});
