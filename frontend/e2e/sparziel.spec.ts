import { test, expect, type Page } from "@playwright/test";
import { createChild, sohnLogin, vaterLogin } from "./helpers";

/*
 * B-593, Rollengang als Sohn: ein Angebot im Shop als Sparziel anheften, den Streifen auf der Startseite sehen; der Vater
 * löscht das Angebot, der Streifen sagt „nicht mehr verfügbar“, und das Kind entfernt das Ziel. Ein eigenes Kind, damit
 * kein Streifen in die Startseite anderer Specs fällt (geteilte Wegwerf-DB).
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);

async function api(page: Page, method: "post" | "delete", url: string, data?: unknown) {
  const token = await page.evaluate(() => localStorage.getItem("pugling.token"));
  const res = await page.request[method](url, { data, headers: { Authorization: `Bearer ${token}` } });
  expect(res.ok(), `${method.toUpperCase()} ${url}: ${res.status()}`).toBeTruthy();
  return method === "post" ? res.json() : null;
}

test("Sparziel anheften, auf der Startseite sehen, und nach dem Löschen bleibt es als nicht mehr verfügbar", async ({ browser }) => {
  const vaterCtx = await browser.newContext();
  const vater = await vaterCtx.newPage();
  await vaterLogin(vater, FATHER);
  const pin = "5930";
  const childId = await createChild(vater, `Sparer ${RUN}`, pin);
  const titel = `Kinoabend ${RUN}`;
  const article = await api(vater, "post", "/api/v1/supervisor/shop/articles",
    { articleNumber: `SZ-${RUN}`, title: titel, unitType: "Stueck", actionType: "Sonstiges" });
  const listing = await api(vater, "post", `/api/v1/supervisor/shop/articles/${article.id}/listings`,
    { title: titel, coinPrice: 120, gemPrice: 0, unitsPerPurchase: 1, maxStock: 2, childIds: [Number(childId)] });

  const sohnCtx = await browser.newContext();
  const sohn = await sohnCtx.newPage();
  await sohnLogin(sohn, { id: childId, pin });
  await expect(sohn.locator("nav.sohn-nav")).toBeVisible();
  // Ohne Ziel kein Streifen (AK 10).
  await expect(sohn.getByTestId("sparziel")).toHaveCount(0);

  await sohn.goto("/sohn/shop");
  const anheften = sohn.getByRole("button", { name: `„${titel}“ als Ziel` });
  await expect(anheften).toHaveAttribute("aria-pressed", "false");
  await anheften.click();
  await expect(anheften).toHaveAttribute("aria-pressed", "true");
  await expect(sohn.getByTestId("sparziel")).toContainText(titel);

  await sohn.goto("/sohn");
  const streifen = sohn.getByTestId("sparziel");
  await expect(streifen).toContainText(titel);
  await expect(streifen).toContainText("Noch 120 Münzen");
  // Ein frisches Kind hat keinen Verlauf von sieben Tagen: keine Prognose statt einer geschönten (Entscheidung 2).
  await expect(streifen).not.toContainText("Tempo");

  await api(vater, "delete", `/api/v1/supervisor/shop/articles/${article.id}/listings/${listing.id}`);
  await sohn.reload();
  await expect(streifen).toContainText(titel);
  await expect(streifen).toContainText("Nicht mehr verfügbar");
  await sohn.getByRole("button", { name: "Ziel entfernen" }).click();
  await expect(sohn.getByTestId("sparziel")).toHaveCount(0);

  await vaterCtx.close();
  await sohnCtx.close();
});
