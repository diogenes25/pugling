import { test, expect, type Page } from "@playwright/test";

// End-to-End des vertikalen Vokabel-Durchstichs:
//   Vater legt (Web) Vokabeln + Lehrplan an  →  Sohn arbeitet (App) Übung + Test ab
//   →  Punkte fließen, Vater sieht den Fortschritt.
// Beide Rollen laufen in getrennten Browser-Kontexten (isoliertes localStorage).

const FATHER = { id: "1", pin: "0000" };
const CHILD = { id: "1", pin: "1111" };

async function vaterLogin(page: Page) {
  await page.goto("/vater");
  await page.locator("#fid").fill(FATHER.id);
  await page.locator("#pin").fill(FATHER.pin);
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();
}

async function addVocab(page: Page, word: string, translation: string) {
  await page.getByRole("link", { name: "Vokabeln" }).click();
  await page.getByPlaceholder("house").fill(word);
  await page.getByPlaceholder("Haus").fill(translation);
  await page.getByRole("button", { name: "Speichern" }).click();
  await expect(page.locator(".banner.ok")).toContainText(word);
}

async function sohnLogin(page: Page) {
  await page.goto("/sohn");
  await page.locator("#childId").fill(CHILD.id);
  for (const d of CHILD.pin.split("")) {
    await page.locator(".keys button", { hasText: new RegExp(`^${d}$`) }).first().click();
  }
  await page.getByRole("button", { name: "▶ LOS" }).click();
}

test("Vater erstellt Plan, Sohn arbeitet ihn ab, Punkte fließen", async ({ browser }) => {
  // ---------- VATER (Web) ----------
  const vaterCtx = await browser.newContext();
  const vater = await vaterCtx.newPage();
  await vaterLogin(vater);

  await addVocab(vater, "cat", "Katze");
  await addVocab(vater, "dog", "Hund");
  await addVocab(vater, "sun", "Sonne");

  // Neuen Lehrplan anlegen
  await vater.getByRole("link", { name: "Neuer Plan" }).click();
  await expect(vater.getByRole("heading", { name: /Neuer Lehrplan/ })).toBeVisible();

  // Warten bis Vokabelliste geladen ist, dann die ersten drei Vokabeln auswählen.
  const vocabSection = vater.locator("section.card").nth(1);
  await expect(vocabSection.locator("input[type=checkbox]").first()).toBeVisible();
  const boxes = vocabSection.locator("input[type=checkbox]");
  const pick = Math.min(3, await boxes.count());
  for (let i = 0; i < pick; i++) await boxes.nth(i).check();

  await vater.getByRole("button", { name: "Lehrplan erstellen" }).click();
  await expect(vater).toHaveURL(/\/vater\/plan\/\d+$/);
  await expect(vater.getByRole("heading", { name: "Inhalte & Leitner-Boxen" })).toBeVisible();
  const planUrl = vater.url();

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
  for (let i = 0; i < total; i++) {
    await sohn.getByRole("button", { name: "Umdrehen 🔄" }).click();
    await sohn.getByRole("button", { name: "Gewusst!" }).click();
  }
  await expect(sohn.getByText("RUNDE FERTIG!")).toBeVisible();

  // Weiter zum Tagestest
  await sohn.getByRole("button", { name: /Weiter zum Test/ }).click();
  await expect(sohn.getByText("Tagestest")).toBeVisible();

  // SelfAssess: alle aufdecken, dann als "gewusst" markieren
  const reveal = sohn.getByRole("button", { name: "Aufdecken 🔄" });
  const revealCount = await reveal.count();
  for (let i = 0; i < revealCount; i++) await reveal.first().click();
  const known = sohn.getByRole("button", { name: "Gewusst", exact: true });
  const knownCount = await known.count();
  for (let i = 0; i < knownCount; i++) await known.nth(i).click();

  await sohn.getByRole("button", { name: /Abgeben/ }).click();
  await expect(sohn.getByText("SIEG!")).toBeVisible();

  // Wallet: Münzen wurden gutgeschrieben (Test bestanden + ggf. Leitner-Übung)
  await sohn.goto("/sohn/skins");
  const coins = sohn.locator(".chip", { hasText: "🪙" }).first();
  await expect(coins).toBeVisible();
  const balance = Number((await coins.textContent())!.replace(/\D/g, ""));
  expect(balance).toBeGreaterThan(0);

  // ---------- VATER sieht Fortschritt ----------
  await vater.goto(planUrl);
  await expect(vater.getByText("Punkte gesamt")).toBeVisible();
  // Tagesverlauf-Tabelle zeigt den bestandenen Test des Sohns.
  await expect(vater.getByText(/bestanden \d+%/).first()).toBeVisible();
  // Punkte gesamt > 0 (Übung + Test sind beim Vater angekommen).
  const totalCard = vater.locator(".vater-grid .card").first();
  await expect(totalCard).toContainText("Punkte gesamt");
  expect(Number((await totalCard.textContent())!.replace(/\D/g, ""))).toBeGreaterThan(0);

  await vaterCtx.close();
  await sohnCtx.close();
});
