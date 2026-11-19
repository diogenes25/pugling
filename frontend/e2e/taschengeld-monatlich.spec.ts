import { test, expect, type APIRequestContext, type Page } from "@playwright/test";
import { createChild, sohnLogin, vaterLogin } from "./helpers";

/*
 * Sprint 96 (B-645, B-665) – der Rollengang von Vater und Sohn am echten Server und an der echten Oberfläche:
 *
 * Vater legt im Vater-Web ein monatliches Taschengeld nur für Kind A an (Euro, +4 je Termin bis 8) und ein
 * wöchentliches Angebot mit Wochentag für alle – beides ohne `400` (B-645 AK 6). Kind A kauft das gemeinsame Angebot
 * leer, Kind B sieht es weiter kaufbar (AK 1); B sieht A's Taschengeld gar nicht (AK 11). A kauft sein Taschengeld
 * leer und sieht „wieder da" (AK 9). Danach löst der Vater aus A's Inventar selbst ein, und A sieht
 * „Papa hat für dich eingelöst" (B-665 AK 6, 7).
 *
 * Eigene Kinder je Lauf: die Wegwerf-DB teilen sich alle Specs, ein geseedetes Kind trüge fremde Käufe.
 */

const RUN = Date.now().toString(36).slice(-5);
const FATHER = { id: "1", pin: "0000" };

async function vaterHeaders(request: APIRequestContext) {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  return { Authorization: `Bearer ${(await login.json()).token}` };
}

/** Öffnet die Angebote eines Artikels (per Artikelnummer, nie per Position). */
async function angeboteOeffnen(vater: Page, nummer: string) {
  await vater.goto("/vater/shop");
  const zeile = vater.getByRole("row").filter({ hasText: nummer });
  await zeile.getByRole("button", { name: /Angebote/ }).click();
  await expect(vater.getByRole("heading", { name: new RegExp(`Angebote für`) })).toBeVisible();
}

test("Taschengeld je Kind: eigener Bestand, Zielgruppe, wieder da; Vater löst selbst ein", async ({ browser, request }) => {
  const headers = await vaterHeaders(request);

  // ---------- VATER: zwei Kinder, Münzen, ein Euro-Artikel ----------
  const vaterCtx = await browser.newContext();
  const vater = await vaterCtx.newPage();
  vater.on("dialog", (d) => d.accept());
  await vaterLogin(vater, FATHER);
  const nameA = `Anton-${RUN}`;
  const nameB = `Berta-${RUN}`;
  const childA = await createChild(vater, nameA, "4711");
  const childB = await createChild(vater, nameB, "4712");
  for (const id of [childA, childB]) {
    const res = await request.post(`/api/v1/supervisor/children/${id}/points`, { headers, data: { amount: 500, reason: "E2E" } });
    expect(res.ok(), await res.text()).toBeTruthy();
  }
  const nummer = `GELD-${RUN}`;
  const article = await request.post("/api/v1/supervisor/shop/articles", {
    headers, data: { articleNumber: nummer, title: `Taschengeld ${RUN}`, unitType: "Euro", actionType: "Sonstiges" },
  });
  expect(article.ok(), await article.text()).toBeTruthy();
  const articleId = (await article.json()).id as number;

  // ---------- VATER-WEB: monatlich nur für A, wöchentlich mit Wochentag für alle ----------
  await angeboteOeffnen(vater, nummer);
  const form = vater.locator("form.form-grid").last();
  const anlegen = async () => {
    const created = vater.waitForResponse((r) => r.request().method() === "POST" && /\/listings$/.test(r.url()));
    await form.getByRole("button", { name: "Angebot anlegen" }).click();
    const res = await created;
    expect(res.status(), await res.text()).toBe(201);
  };

  await form.getByLabel("Titel (optional)", { exact: true }).fill(`Taschengeld A ${RUN}`);
  await form.getByLabel("Menge je Kauf", { exact: true }).fill("5");
  await form.getByLabel("Preis 🪙", { exact: true }).fill("10");
  await form.getByLabel("Auffüllen", { exact: true }).selectOption("Monthly");
  await form.getByLabel("Obergrenze je Kind", { exact: true }).fill("8");
  await form.getByLabel("+ je Termin", { exact: true }).fill("4");
  await form.getByRole("checkbox", { name: nameA }).check();
  await anlegen();

  await form.getByLabel("Titel (optional)", { exact: true }).fill(`Zocken alle ${RUN}`);
  await form.getByLabel("Auffüllen", { exact: true }).selectOption("Weekly");
  await form.getByLabel("Wochentag", { exact: true }).selectOption("Wednesday");
  await form.getByLabel("Obergrenze je Kind", { exact: true }).fill("1");
  // Die Spar-Menge des vorigen Angebots (+4) bleibt stehen, statt still der Obergrenze zu folgen – also neu setzen.
  await form.getByLabel("+ je Termin", { exact: true }).fill("1");
  await form.getByRole("checkbox", { name: nameA }).uncheck();
  await anlegen();

  // Eine Angebotszeile hat „Bearbeiten"; die Kaufliste weiter unten trägt denselben Titel, sobald gekauft wurde.
  const angebotsZeile = (titel: string) => vater.getByRole("row").filter({ hasText: titel })
    .filter({ has: vater.getByRole("button", { name: "Bearbeiten" }) });
  const zeileA = angebotsZeile(`Taschengeld A ${RUN}`);
  await expect(zeileA).toContainText("8/8");
  await expect(zeileA).not.toContainText(nameB);
  // Die Menge je Termin kam an: ohne sie füllte der Server bis zur Obergrenze auf und die Zeile zeigte kein „+4".
  await expect(zeileA).toContainText("Monatlich (am 1.), +4 bis 8");
  const zeileAlle = angebotsZeile(`Zocken alle ${RUN}`);
  await expect(zeileAlle).toContainText("Wöchentlich (Mi)");
  await expect(zeileAlle).toContainText(`${nameA}: 1/1`);
  await expect(zeileAlle).toContainText(`${nameB}: 1/1`);

  // Einmalig mit Datum (AK 6) – und Bearbeiten: der Wochentag wird über die Zeile geändert, nicht nur beim Anlegen.
  await form.getByLabel("Titel (optional)", { exact: true }).fill(`Ausflug ${RUN}`);
  await form.getByLabel("Auffüllen", { exact: true }).selectOption("Once");
  await expect(form.getByRole("button", { name: "Angebot anlegen" })).toBeDisabled();
  await form.getByLabel("Datum", { exact: true }).fill("2030-01-15");
  await anlegen();
  await expect(angebotsZeile(`Ausflug ${RUN}`)).toContainText("Einmalig (15.01.2030)");

  await zeileAlle.getByRole("button", { name: "Bearbeiten" }).click();
  // `exact`: ohne trifft „OK“ als Teilstring auch „Bestand von Kontokind … füllen“ – ein Kind, das eine andere Spec in
  // der geteilten DB anlegt und das in der Zeile des Angebots für alle steht.
  const ok = vater.getByRole("button", { name: "OK", exact: true });
  const bearbeiten = vater.getByRole("row").filter({ has: ok });
  await bearbeiten.getByLabel("Wochentag", { exact: true }).selectOption("Thursday");
  const gepatcht = vater.waitForResponse((r) => r.request().method() === "PATCH" && /\/listings\/\d+$/.test(r.url()));
  await bearbeiten.getByRole("button", { name: "OK", exact: true }).click();
  expect((await gepatcht).status()).toBe(200);
  await expect(zeileAlle).toContainText("Wöchentlich (Do)");

  // A's Taschengeld auf 1 setzen, damit ein Kauf es leert (der Knopf „füllen" setzt nur auf die Obergrenze).
  const listings = await (await request.get(`/api/v1/supervisor/shop/articles/${articleId}/listings`, { headers })).json() as
    { id: number; title: string }[];
  const taschengeldId = listings.find((l) => l.title === `Taschengeld A ${RUN}`)!.id;
  const setzen = await request.put(`/api/v1/supervisor/shop/articles/${articleId}/listings/${taschengeldId}/stock/${childA}`,
    { headers, data: { currentStock: 1 } });
  expect(setzen.ok(), await setzen.text()).toBeTruthy();

  // ---------- SOHN A: kauft das gemeinsame Angebot und sein Taschengeld leer ----------
  const sohnACtx = await browser.newContext();
  const sohnA = await sohnACtx.newPage();
  sohnA.on("dialog", (d) => d.accept());
  await sohnLogin(sohnA, { id: childA, pin: "4711" });
  await expect(sohnA.locator("nav.sohn-nav")).toBeVisible();
  await sohnA.goto("/sohn/shop");
  for (const titel of [`Zocken alle ${RUN}`, `Taschengeld A ${RUN}`]) {
    await sohnA.locator("button.skin", { hasText: titel }).click();
    await expect(sohnA.locator(".cel-title", { hasText: "GEKAUFT!" })).toBeVisible();
    await sohnA.keyboard.press("Escape");
    await sohnA.goto("/sohn/shop");
  }
  const leer = sohnA.locator("button.skin", { hasText: `Taschengeld A ${RUN}` });
  await expect(leer).toContainText("ausverkauft");
  await expect(leer).toContainText("wieder da:");

  // ---------- VATER: „füllen" je Kind – nur A's Bestand steigt, der Knopf ist danach gesperrt ----------
  await angeboteOeffnen(vater, nummer);
  await expect(zeileAlle).toContainText(`${nameA}: 0/1`);
  await zeileAlle.getByRole("button", { name: `Bestand von ${nameA} füllen` }).click();
  await expect(zeileAlle).toContainText(`${nameA}: 1/1`);
  await expect(zeileAlle.getByRole("button", { name: `Bestand von ${nameA} füllen` })).toBeDisabled();

  // ---------- SOHN B: sieht A's Taschengeld nicht, das gemeinsame Angebot aber weiter kaufbar ----------
  const sohnBCtx = await browser.newContext();
  const sohnB = await sohnBCtx.newPage();
  await sohnLogin(sohnB, { id: childB, pin: "4712" });
  await expect(sohnB.locator("nav.sohn-nav")).toBeVisible();
  await sohnB.goto("/sohn/shop");
  const gemeinsam = sohnB.locator("button.skin", { hasText: `Zocken alle ${RUN}` });
  await expect(gemeinsam).toBeVisible();
  await expect(gemeinsam).toBeEnabled();
  await expect(gemeinsam).not.toContainText("ausverkauft");
  await expect(sohnB.locator("button.skin", { hasText: `Taschengeld A ${RUN}` })).toHaveCount(0);

  // ---------- VATER: löst aus A's Inventar selbst ein (B-665) ----------
  await vater.goto(`/vater/shop?childId=${childA}`);
  // Beide Angebote hängen am selben Euro-Artikel: das Inventar steht auf 10 € (5 + 5). Am Titel und an der Menge
  // festmachen, nicht am Knopf – der weicht nach dem Klick dem Mengenfeld.
  const inventarZeile = vater.getByRole("row").filter({ hasText: `Taschengeld ${RUN}` }).filter({ hasText: "10 €" });
  await inventarZeile.getByRole("button", { name: `Taschengeld ${RUN} selbst einlösen` }).click();
  await inventarZeile.getByLabel("Menge", { exact: true }).fill("5");
  const redeemed = vater.waitForResponse((r) => r.request().method() === "POST" && /\/redeem$/.test(r.url()));
  await inventarZeile.getByRole("button", { name: "Einlösen" }).click();
  expect((await redeemed).status()).toBe(200);
  await expect(vater.getByText("Eingelöst.")).toBeVisible();
  await expect(vater.getByRole("row").filter({ hasText: "selbst eingelöst" })).toBeVisible();

  // ---------- SOHN A: sieht, dass Papa eingelöst hat ----------
  await sohnA.goto("/sohn/shop");
  await sohnA.getByRole("button", { name: "Anfragen" }).click();
  await expect(sohnA.getByText("Papa hat für dich eingelöst")).toBeVisible();

  await Promise.all([vaterCtx.close(), sohnACtx.close(), sohnBCtx.close()]);
});
