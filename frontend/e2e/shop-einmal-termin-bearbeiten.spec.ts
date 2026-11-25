import { test, expect, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-782, Rollengang aus der Vater-Rolle: ein „einmalig“-Angebot, dessen Termin ein API-Aufrufer mit Uhrzeit gesetzt hat.
 * Der Vater ändert im Vater-Web nur den Preis – der Termin muss unverändert zum Server zurückgehen. Ging er als
 * Mitternacht zurück, läse der Server einen neuen Termin und füllte jedes Kind noch einmal auf (B-776); dass der
 * unveränderte Termin am Server nichts auslöst, hält `ShopStockPerChildTests.B782_…`.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);

async function post(page: Page, url: string, data: unknown) {
  const token = await page.evaluate(() => localStorage.getItem("pugling.token"));
  const res = await page.request.post(url, { data, headers: { Authorization: `Bearer ${token}` } });
  expect(res.ok(), `POST ${url}: ${res.status()}`).toBeTruthy();
  return res.json();
}

test("Nur den Preis ändern schickt den Termin mit Uhrzeit unverändert zurück", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const artikel = `Kinoabend ${RUN}`;
  const angebot = `Einmal-Kino ${RUN}`;
  const article = await post(page, "/api/v1/supervisor/shop/articles",
    { articleNumber: `K-${RUN}`, title: artikel, description: null, unitType: "Stueck", actionType: "Sonstiges" });
  const listing = await post(page, `/api/v1/supervisor/shop/articles/${article.id}/listings`, {
    title: angebot, description: null, coinPrice: 10, gemPrice: 0, unitsPerPurchase: 1, maxStock: 1,
    refillKind: "Once", refillAtUtc: "2030-01-15T10:00:00Z",
  });

  await page.goto("/vater/shop");
  await page.getByRole("row", { name: new RegExp(artikel) }).getByRole("button", { name: /Angebote/ }).click();
  await page.getByRole("row", { name: new RegExp(angebot) }).getByRole("button", { name: "Bearbeiten" }).click();
  const zeile = page.getByRole("row").filter({ has: page.getByRole("button", { name: "OK", exact: true }) });
  await expect(zeile.getByLabel("Datum", { exact: true })).toHaveValue("2030-01-15");
  await zeile.getByLabel("Preis 🪙", { exact: true }).fill("12");

  const patch = page.waitForRequest((r) => r.method() === "PATCH" && r.url().endsWith(`/listings/${listing.id}`));
  await zeile.getByRole("button", { name: "OK", exact: true }).click();
  const body = (await patch).postDataJSON();
  expect(body.coinPrice).toBe(12);
  // Den Wert, den die Liste geliefert hat (dort ohne Offset, aus SQLite gelesen) – nicht Mitternacht.
  expect(body.refillAtUtc).toMatch(/^2030-01-15T10:00:00/);
  // Die Liste lädt nach dem Speichern neu: der neue Preis steht, der Termin ist derselbe.
  await expect(page.getByRole("row", { name: new RegExp(angebot) })).toContainText("12");
});

// B-786: ein mit „Abbrechen“ verworfenes Datum geht beim nächsten „OK“ nicht mit hinaus.
test("Ein verworfenes Datum geht nach „Abbrechen“ nicht mit dem nächsten Speichern hinaus", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const artikel = `Eisessen ${RUN}`;
  const angebot = `Einmal-Eis ${RUN}`;
  const article = await post(page, "/api/v1/supervisor/shop/articles",
    { articleNumber: `E-${RUN}`, title: artikel, description: null, unitType: "Stueck", actionType: "Sonstiges" });
  const listing = await post(page, `/api/v1/supervisor/shop/articles/${article.id}/listings`, {
    title: angebot, description: null, coinPrice: 10, gemPrice: 0, unitsPerPurchase: 1, maxStock: 1,
    refillKind: "Once", refillAtUtc: "2030-02-01T10:00:00Z",
  });

  await page.goto("/vater/shop");
  await page.getByRole("row", { name: new RegExp(artikel) }).getByRole("button", { name: /Angebote/ }).click();
  const angebotZeile = page.getByRole("row", { name: new RegExp(angebot) });
  const zeile = page.getByRole("row").filter({ has: page.getByRole("button", { name: "OK", exact: true }) });
  await angebotZeile.getByRole("button", { name: "Bearbeiten" }).click();
  await zeile.getByLabel("Datum", { exact: true }).fill("2030-03-01");
  await zeile.getByRole("button", { name: "Abbrechen" }).click();

  await angebotZeile.getByRole("button", { name: "Bearbeiten" }).click();
  await expect(zeile.getByLabel("Datum", { exact: true })).toHaveValue("2030-02-01");
  await zeile.getByLabel("Preis 🪙", { exact: true }).fill("12");
  const patch = page.waitForRequest((r) => r.method() === "PATCH" && r.url().endsWith(`/listings/${listing.id}`));
  await zeile.getByRole("button", { name: "OK", exact: true }).click();
  expect((await patch).postDataJSON().refillAtUtc).toMatch(/^2030-02-01T10:00:00/);
});
