import { test, expect, type Page } from "@playwright/test";

/*
 * B-663: am Telefon (unter 768 px) bleibt dem Vater nur das Betreuen. Kein Wahlschritt (eine Wahl mit einer Antwort ist
 * keine), kein Umschalter, und ein Lesezeichen auf die Werkstatt führt zur Startseite mit dem Hinweis „nur am Rechner".
 * Nach dem Verbreitern ist alles wieder da – die Sperre folgt der Breite, sie ist kein gemerkter Zustand.
 *
 * Was sie rot macht: `perspectivesFor` ohne Breite (Fall 1: der Umschalter steht da, `/vater/inhalte` öffnet die
 * Werkstatt), ein einmal gelesenes `matchMedia` (Fall 2: nach dem Verbreitern fehlt „Erstellen").
 */

const FATHER = { id: "1", pin: "0000" };

async function anmelden(page: Page) {
  await page.goto("/vater");
  await page.locator("#fid").fill(FATHER.id);
  await page.locator("#pin").fill(FATHER.pin);
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Heute", exact: true })).toBeVisible();
}

const umschalter = (page: Page) => page.locator("nav.perspective-switch");

test("Am Telefon gibt es nur das Betreuen, die Werkstatt verweist an den Rechner", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await anmelden(page);

  await expect(page.getByRole("button", { name: "Als Creator", exact: true })).toHaveCount(0);
  await expect(umschalter(page)).toHaveCount(0);

  await page.goto("/vater/inhalte");
  await expect(page).toHaveURL(/\/vater$/);
  await expect(page.getByRole("status").filter({ hasText: "nur am Rechner" })).toBeVisible();
  await page.goto("/vater/plaene");
  await expect(page).toHaveURL(/\/vater$/);
});

test("Nach dem Verbreitern sind alle Perspektiven wieder da", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await anmelden(page);
  await expect(umschalter(page)).toHaveCount(0);

  await page.setViewportSize({ width: 1024, height: 800 });
  for (const p of ["Betreuen", "Zuweisen", "Erstellen"]) {
    await expect(umschalter(page).getByRole("link", { name: new RegExp(p) })).toBeVisible();
  }
  await page.goto("/vater/inhalte");
  await expect(page).toHaveURL(/\/vater\/inhalte$/);
});
