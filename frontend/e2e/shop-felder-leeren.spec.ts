import { test, expect, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-789, Rollengang aus der Vater-Rolle: ein geleerter Angebotstitel und eine geleerte Artikel-Beschreibung sind nach dem
 * Speichern wirklich leer – vorher meldete das Vater-Web „gespeichert“, und der alte Text stand nach dem Neuladen wieder da.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);

async function post(page: Page, url: string, data: unknown) {
  const token = await page.evaluate(() => localStorage.getItem("pugling.token"));
  const res = await page.request.post(url, { data, headers: { Authorization: `Bearer ${token}` } });
  expect(res.ok(), `POST ${url}: ${res.status()}`).toBeTruthy();
  return res.json();
}

test("Geleerter Angebotstitel und geleerte Artikel-Beschreibung bleiben nach dem Neuladen leer", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const artikel = `Zoobesuch ${RUN}`;
  const article = await post(page, "/api/v1/supervisor/shop/articles", {
    articleNumber: `Z-${RUN}`, title: artikel, description: `Mit Eis ${RUN}`, unitType: "Stueck", actionType: "Sonstiges",
  });
  await post(page, `/api/v1/supervisor/shop/articles/${article.id}/listings`,
    { title: `Zoo-Angebot ${RUN}`, description: null, coinPrice: 10, gemPrice: 0, unitsPerPurchase: 1, maxStock: 1 });

  await page.goto("/vater/shop");
  const artikelZeile = page.getByRole("row", { name: new RegExp(artikel) });
  await artikelZeile.getByRole("button", { name: /Angebote/ }).click();
  // `exact`: sonst trifft „OK“ als Teilstring auch „Bestand von Kontokind … füllen“ – ein Kind einer anderen Spec in der geteilten DB.
  const offen = page.getByRole("row").filter({ has: page.getByRole("button", { name: "OK", exact: true }) });

  // Angebot: Titel leeren.
  await page.getByRole("row", { name: new RegExp(`Zoo-Angebot ${RUN}`) }).getByRole("button", { name: "Bearbeiten" }).click();
  await offen.getByLabel("Titel", { exact: true }).fill("");
  await offen.getByRole("button", { name: "OK", exact: true }).click();
  await expect(page.getByText(`Zoo-Angebot ${RUN}`)).toHaveCount(0);
  await expect(page.getByText("(ohne Titel)").first()).toBeVisible();

  // Artikel: Beschreibung leeren, dann nach dem Neuladen nachsehen.
  await artikelZeile.getByRole("button", { name: "Bearbeiten" }).click();
  await offen.getByLabel("Beschreibung", { exact: true }).fill("");
  await offen.getByRole("button", { name: "OK", exact: true }).click();
  await expect(page.getByText("Artikel gespeichert.")).toBeVisible();
  await page.reload();
  await page.getByRole("row", { name: new RegExp(artikel) }).getByRole("button", { name: "Bearbeiten" }).click();
  await expect(page.getByRole("row").filter({ has: page.getByRole("button", { name: "OK", exact: true }) })
    .getByLabel("Beschreibung", { exact: true })).toHaveValue("");
});
