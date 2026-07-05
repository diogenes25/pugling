import { test, expect, type Page } from "@playwright/test";

// End-to-End des vertikalen Durchstichs im Positions-Modell:
//   Vater legt (Web) einen Lehrplan-Container an und hängt eine Katalog-Übung als Position hinein
//   →  Sohn arbeitet (App) die Position ab: Üben (Leitner) + Test  →  Punkte fließen
//   →  Vater sieht den Fortschritt.
// Referenziert wird die system-geseedete Vokabel-Übung "Vokabeln: En ville" (Französisch, Unité 2); sie
// hat keinen Autor (geteilte Übung) und genug Inhalte (5), um den Combo-Meilenstein ×5 auszulösen.
// Beide Rollen laufen in getrennten Browser-Kontexten (isoliertes localStorage).

const FATHER = { id: "1", pin: "0000" };
const CHILD = { id: "1", pin: "1111" };
const EXERCISE = "Vokabeln: En ville";

async function vaterLogin(page: Page) {
  await page.goto("/vater");
  await page.locator("#fid").fill(FATHER.id);
  await page.locator("#pin").fill(FATHER.pin);
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();
}

async function sohnLogin(page: Page) {
  await page.goto("/sohn");
  await page.locator("#childId").fill(CHILD.id);
  for (const d of CHILD.pin.split("")) {
    await page.locator(".keys button", { hasText: new RegExp(`^${d}$`) }).first().click();
  }
  await page.getByRole("button", { name: "▶ LOS" }).click();
}

test("Vater erstellt Plan mit Position, Sohn arbeitet ihn ab, Punkte fließen", async ({ browser }) => {
  // ---------- VATER (Web) ----------
  const vaterCtx = await browser.newContext();
  const vater = await vaterCtx.newPage();
  await vaterLogin(vater);

  // Lehrplan = leerer Container (Titel/Kind/Laufzeit sind vorbelegt) → anlegen und auf die Plan-Seite.
  await vater.getByRole("link", { name: "Neuer Plan" }).click();
  await expect(vater.getByRole("heading", { name: /Neuer Lehrplan/ })).toBeVisible();
  // Erst wählbar, wenn die Kinder-Liste geladen ist – sonst schlägt das Anlegen mit "Kind wählen" fehl.
  const kindSelect = vater.getByRole("combobox", { name: "Kind" });
  await expect(kindSelect.locator("option")).not.toHaveCount(0);
  await kindSelect.selectOption({ index: 0 });
  await vater.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(vater).toHaveURL(/\/vater\/plan\/\d+$/);
  await expect(vater.getByRole("heading", { name: /Übungen im Plan/ })).toBeVisible();
  const planUrl = vater.url();

  // Katalog-Übung als Position hinzufügen: Tagesziel + Leitner-Kasten (so erscheint "ÜBEN" beim Sohn).
  const exSelect = vater.locator('select[aria-label="Übung"]');
  const option = exSelect.locator("option", { hasText: EXERCISE }).first();
  await expect(option).toBeAttached();
  await exSelect.selectOption(await option.getAttribute("value") ?? "");
  await vater.locator('select[aria-label="Ziel-Rhythmus"]').selectOption("Daily");
  await vater.getByRole("checkbox", { name: /Leitner/ }).check();
  await vater.getByRole("button", { name: /Position hinzufügen/ }).click();
  await expect(vater.getByRole("row", { name: new RegExp(EXERCISE) })).toBeVisible();

  // ---------- SOHN (App) ----------
  const sohnCtx = await browser.newContext();
  const sohn = await sohnCtx.newPage();
  await sohnLogin(sohn);

  // Basis: Tagesmission sichtbar
  await expect(sohn.getByText("Tagesmission")).toBeVisible();

  // Übung starten und alle fälligen Karten "gewusst"
  await sohn.getByRole("button", { name: /ÜBEN/ }).click();
  const counter = sohn.locator(".pill.cyan", { hasText: /Karte \d+ \/ \d+/ });
  await expect(counter).toBeVisible();
  const total = Number((await counter.textContent())!.match(/\/ (\d+)/)![1]);
  // Genug Karten, damit der Combo-Meilenstein ×5 sicher fällt (bewusste Coverage, kein stiller Skip).
  expect(total).toBeGreaterThanOrEqual(5);
  for (let i = 0; i < total; i++) {
    await sohn.getByRole("button", { name: "Umdrehen 🔄" }).click();
    await sohn.getByRole("button", { name: "Gewusst!" }).click();
  }
  // Motivations-Feature: ab 5 Treffern in Folge feiert die App den Combo-Meilenstein (Feier-Banner).
  await expect(sohn.locator(".cel-title", { hasText: "COMBO ×5" })).toBeVisible();
  await expect(sohn.getByText("RUNDE FERTIG!")).toBeVisible();

  // Weiter zum Test
  await sohn.getByRole("button", { name: /Weiter zum Test/ }).click();
  await expect(sohn.locator(".screen-title", { hasText: "Test" })).toBeVisible();

  // SelfAssess: alle aufdecken, dann als "gewusst" markieren
  const reveal = sohn.getByRole("button", { name: "Aufdecken 🔄" });
  const revealCount = await reveal.count();
  for (let i = 0; i < revealCount; i++) await reveal.first().click();
  const known = sohn.getByRole("button", { name: "Gewusst", exact: true });
  const knownCount = await known.count();
  for (let i = 0; i < knownCount; i++) await known.nth(i).click();

  await sohn.getByRole("button", { name: /Abgeben/ }).click();
  await expect(sohn.locator(".vtitle", { hasText: "SIEG!" })).toBeVisible();

  // Wallet: Münzen wurden gutgeschrieben (Test bestanden + ggf. Leitner-Übung)
  await sohn.goto("/sohn/skins");
  const coins = sohn.locator(".chip", { hasText: "🪙" }).first();
  await expect(coins).toBeVisible();
  const balance = Number((await coins.textContent())!.replace(/\D/g, ""));
  expect(balance).toBeGreaterThan(0);

  // ---------- VATER sieht Fortschritt ----------
  await vater.goto(planUrl);
  await expect(vater.getByText("Punkte gesamt")).toBeVisible();
  // Tagesverlauf-Tabelle zeigt den heute erledigten Tag des Sohns (Ziel erfüllt → "komplett").
  await expect(vater.locator("table .pill.lime", { hasText: "komplett" }).first()).toBeVisible();
  // Punkte gesamt > 0 (Übung + Test sind beim Vater angekommen).
  const totalCard = vater.locator(".vater-grid .card").first();
  await expect(totalCard).toContainText("Punkte gesamt");
  expect(Number((await totalCard.textContent())!.replace(/\D/g, ""))).toBeGreaterThan(0);

  await vaterCtx.close();
  await sohnCtx.close();
});
