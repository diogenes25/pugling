import { test, expect, type APIRequestContext, type Page } from "@playwright/test";

/*
 * B-749, der Rollengang des Vaters am Handy (390 px): das Kind kauft und will einlösen, der Vater sieht den Wunsch auf
 * seiner Startseite – mit dem Namen des Kindes, ohne ein Kind zu wählen –, der Zähler am „Shop" nennt ihn, Freigeben
 * nimmt ihn weg, und die Seite scrollt nie seitlich.
 *
 * Was sie rot macht: eine Sammelliste, die nur nach Kindwahl erscheint (der Wunsch fehlt auf `/vater`), ein Zähler, der
 * nach dem Freigeben stehen bleibt, eine Tabelle, die bei 390 px über den Rand ragt (`scrollWidth > innerWidth`).
 * Die Datenbank teilen alle Specs: gezählt wird darum relativ zum Stand vor dem Freigeben, nicht absolut.
 */

const RUN = Date.now().toString(36).slice(-5);
const CHILD = { name: `Handy-Kind ${RUN}`, pin: "8749" };

async function postJson(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), `${url}: ${await res.text()}`).toBeTruthy();
  return res.json();
}

/** Ein Kind mit Münzen, das einen Artikel des Vaters 1 kauft und 30 Minuten einlösen will. */
async function kindWillEinloesen(request: APIRequestContext) {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };
  const childId = (await postJson(request, "/api/v1/supervisor/children", vater, { name: CHILD.name, pin: CHILD.pin })).id as number;
  await postJson(request, `/api/v1/supervisor/children/${childId}/points`, vater, { amount: 200, reason: "Startgeld" });
  const articleId = (await postJson(request, "/api/v1/supervisor/shop/articles", vater, {
    articleNumber: `HANDY-${RUN}`, title: `Fernsehen ${RUN}`, unitType: "Minute", actionType: "TV",
  })).id as number;
  const listingId = (await postJson(request, `/api/v1/supervisor/shop/articles/${articleId}/listings`, vater, {
    coinPrice: 100, gemPrice: 0, unitsPerPurchase: 60, maxStock: 3,
  })).id as number;

  const kindLogin = await request.post("/api/v1/auth/child", { data: { childId, pin: CHILD.pin } });
  expect(kindLogin.ok(), await kindLogin.text()).toBeTruthy();
  const kind = { Authorization: `Bearer ${(await kindLogin.json()).token}` };
  await postJson(request, `/api/v1/student/me/shop/listings/${listingId}/purchase`, kind, {});
  await postJson(request, `/api/v1/student/me/shop/inventory/${articleId}/activate`, kind, { quantity: 30 });
}

/** Die Zahl am „Shop"; 0, wenn kein Zähler steht (dann ist nichts offen). */
async function shopZaehler(page: Page): Promise<number> {
  const badge = page.locator("nav.vater-nav").getByRole("link", { name: /Shop/ }).locator(".pill");
  return (await badge.count()) === 0 ? 0 : Number(await badge.innerText());
}

async function keinSeitlichesScrollen(page: Page) {
  const { scrollWidth, innerWidth } = await page.evaluate(() =>
    ({ scrollWidth: document.documentElement.scrollWidth, innerWidth: window.innerWidth }));
  expect(scrollWidth, "die Startseite scrollt seitlich").toBeLessThanOrEqual(innerWidth);
}

test("Der Vater gibt am Handy frei, was sein Kind einlösen will", async ({ page, request }) => {
  await kindWillEinloesen(request);

  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/vater");
  await page.locator("#fid").fill("1");
  await page.locator("#pin").fill("0000");
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Heute", exact: true })).toBeVisible();

  const wunsch = page.getByRole("list", { name: "Offene Einlöse-Wünsche" })
    .getByRole("listitem").filter({ hasText: CHILD.name });
  await expect(wunsch).toContainText(`30 Min „Fernsehen ${RUN}"`);
  // Der Zähler lädt eigenständig; erst auf ihn warten, dann den Ausgangsstand lesen.
  await expect(page.locator("nav.vater-nav").getByRole("link", { name: /Shop/ }).locator(".pill")).toBeVisible();
  const vorher = await shopZaehler(page);
  expect(vorher).toBeGreaterThanOrEqual(1);
  await keinSeitlichesScrollen(page);

  page.once("dialog", (d) => d.accept());
  await wunsch.getByRole("button", { name: /freigeben$/ }).click();

  await expect(page.getByRole("status").filter({ hasText: `„Fernsehen ${RUN}" freigegeben.` })).toBeVisible();
  await expect(wunsch).toHaveCount(0);
  await expect.poll(() => shopZaehler(page)).toBe(vorher - 1);
  await keinSeitlichesScrollen(page);
});
