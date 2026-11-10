import { test, expect, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * NP-27 Teil 2 (B-426, B-428, B-431): Der Creator sieht in den Lehrwerken je Unit die Zahl ihrer Übungen (mit Link dorthin),
 * auf „Übungen verwalten" zuerst seine eigenen Übungen über alle Fächer und im Fachlehrer-Profil die deutschen Typnamen.
 * Rollengang Creator; ein frisch registriertes Konto für die eigenen Übungen, damit „meine" eine bekannte Menge ist.
 *
 * Rot, wenn die Unit-Zeile keine Zahl oder einen Link ohne Filter trägt, „Übungen verwalten" ohne Fach leer bleibt oder die
 * Fächer nicht nennt, oder das Fachlehrer-Formular rohe Schlüssel zeigt.
 */

const RUN = Date.now().toString().slice(-6);

async function post(page: Page, url: string, data: unknown) {
  const token = await page.evaluate(() => localStorage.getItem("pugling.token"));
  const res = await page.request.post(url, { data, headers: { Authorization: `Bearer ${token}` } });
  expect(res.ok(), `POST ${url}: ${res.status()}`).toBeTruthy();
  return res.json();
}

/** Fach → Reihe → Unit 1 (+ Unit 2 leer) mit `count` Rechenübungen in Unit 1. */
async function katalog(page: Page, name: string, count: number) {
  const subjectId = (await post(page, "/api/v1/creator/subjects", { name: `${name}-Fach ${RUN}` })).id;
  const seriesId = (await post(page, "/api/v1/creator/textbook-series", { name: `${name}-Reihe ${RUN}`, subjectId })).id;
  const unit1 = (await post(page, `/api/v1/creator/textbook-series/${seriesId}/units`, { label: "Unit 1", orderIndex: 1 })).id;
  await post(page, `/api/v1/creator/textbook-series/${seriesId}/units`, { label: "Unit 2", orderIndex: 2 });
  for (let i = 0; i < count; i++) {
    await post(page, `/api/v1/creator/textbook-series/${seriesId}/units/${unit1}/arithmetic`, {
      title: `${name}-Übung ${i + 1} ${RUN}`, orderIndex: i + 1, rewardPoints: 5,
      config: { problems: [{ prompt: "1 + 1", answer: 2, tolerance: 0 }] },
    });
  }
  return { subjectId, seriesId, unit1, fach: `${name}-Fach ${RUN}`, reihe: `${name}-Reihe ${RUN}` };
}

test("Lehrwerke: je Unit die Zahl der Übungen, mit Link auf die gefilterte Liste (B-428)", async ({ page }) => {
  await vaterLogin(page, { id: "1", pin: "0000" });
  const k = await katalog(page, "Zahl", 2);

  await page.goto("/vater/lehrwerke");
  await page.getByRole("row", { name: new RegExp(k.reihe) }).getByRole("button", { name: "Units" }).click();
  await expect(page.getByRole("columnheader", { name: "Übungen" })).toBeVisible();
  const link = page.getByRole("link", { name: "2 Übungen in „Unit 1\"" });
  await expect(link).toHaveText("2");
  await expect(page.getByRole("link", { name: "0 Übungen in „Unit 2\"" })).toHaveText("0");

  await link.click();
  await expect(page).toHaveURL(new RegExp(`subjectId=${k.subjectId}.*seriesId=${k.seriesId}.*seriesUnitId=${k.unit1}`));
  await expect(page.getByRole("heading", { name: /Übungen \(2\)/ })).toBeVisible();
});

test("Übungen verwalten startet mit den eigenen Übungen aller Fächer (B-431)", async ({ page }) => {
  // Ein frisches Konto: seine eigenen Übungen sind genau die zwei, die diese Spec anlegt.
  const reg = await page.request.post("/api/v1/supervisor/adults", { data: { name: `Lehrkraft ${RUN}`, pin: "4329" } });
  expect(reg.ok()).toBeTruthy();
  const id = String((await reg.json()).id);
  await vaterLogin(page, { id, pin: "4329" });
  const a = await katalog(page, "Erstes", 1);
  const b = await katalog(page, "Zweites", 1);

  await page.goto("/vater/exercises");
  await expect(page.getByRole("heading", { name: /Meine Übungen \(2\)/ })).toBeVisible();
  await expect(page.getByRole("group", { name: new RegExp(`Erstes-Übung 1 ${RUN}`) })).toContainText(`Fach: ${a.fach}`);
  await expect(page.getByRole("group", { name: new RegExp(`Zweites-Übung 1 ${RUN}`) })).toContainText(`Fach: ${b.fach}`);

  // Fach wählen → wie bisher, ohne Fach-Angabe je Zeile; Fach abwählen → wieder „meine".
  await page.getByLabel("Fach", { exact: true }).selectOption({ label: a.fach });
  await expect(page.getByRole("heading", { name: /^Übungen \(1\)/ })).toBeVisible();
  await expect(page.getByRole("group", { name: new RegExp(`Erstes-Übung 1 ${RUN}`) })).not.toContainText("Fach:");
  await page.getByLabel("Fach", { exact: true }).selectOption({ label: "– alle Fächer –" });
  await expect(page.getByRole("heading", { name: /Meine Übungen \(2\)/ })).toBeVisible();

  // Die Zahl der Werkstatt-Kachel ist dieselbe.
  await page.goto("/vater/inhalte");
  await expect(page.locator("span", { has: page.getByText("eigene Übungen", { exact: true }) }).locator("strong")).toHaveText("2");
});

test("Fachlehrer-Profil: Übungstypen mit deutschem Namen aus dem Manifest (B-426)", async ({ page }) => {
  await vaterLogin(page, { id: "1", pin: "0000" });
  await page.goto("/vater/fachlehrer");
  const typen = page.getByRole("group", { name: "Bevorzugte Übungstypen" }).first();
  await expect(typen.getByRole("button", { name: "Lückentext" })).toBeVisible();
  // Mehr als die alten vier Typen (Birkenbihl stand nicht in der Handliste); Name und Schlüssel sind hier gleich.
  await expect(typen.getByRole("button", { name: "Birkenbihl" })).toBeVisible();
  await expect(typen.getByRole("button", { name: "Cloze" })).toHaveCount(0);
});

test("Fremde Übungen: die Unit-Zahl und die Liste hinter ihrem Link zeigen dieselben (Review-Fund zu B-428)", async ({ page, browser }) => {
  // Ein zweiter Creator legt Reihe, Unit und Übung an – für Konto 1 sind das fremde Übungen im geteilten Katalog.
  const reg = await page.request.post("/api/v1/supervisor/adults", { data: { name: `Fremdlehrer ${RUN}`, pin: "4330" } });
  expect(reg.ok()).toBeTruthy();
  const fremdId = String((await reg.json()).id);
  await vaterLogin(page, { id: fremdId, pin: "4330" });
  const k = await katalog(page, "Fremd", 1);

  // Konto 1 in einem eigenen Kontext – die Sitzung des Fremden bleibt sonst stehen.
  const ctx = await browser.newContext();
  const vater = await ctx.newPage();
  await vaterLogin(vater, { id: "1", pin: "0000" });
  await vater.goto("/vater/lehrwerke");
  await vater.getByRole("row", { name: new RegExp(k.reihe) }).getByRole("button", { name: "Units" }).click();
  const link = vater.getByRole("link", { name: "1 Übung in „Unit 1\"" });
  await expect(link).toHaveText("1");
  await link.click();
  await expect(vater.getByRole("heading", { name: /Übungen \(1\)/ })).toBeVisible();
  await expect(vater.getByRole("group", { name: new RegExp(`Fremd-Übung 1 ${RUN}`) })).toBeVisible();
  await ctx.close();
});

test("Ausfälle sagen, was fehlt: Übungsliste und Typ-Manifest (Review-Funde zu B-431/B-426)", async ({ page }) => {
  await vaterLogin(page, { id: "1", pin: "0000" });
  await page.route("**/api/v1/creator/exercises?**", (route) => route.abort("failed"));
  await page.goto("/vater/exercises");
  await expect(page.getByText(/Die Übungen konnten nicht geladen werden/)).toBeVisible();
  await expect(page.getByText("Noch keine Übungen.")).toHaveCount(0);
  await page.unroute("**/api/v1/creator/exercises?**");

  await page.route("**/api/v1/creator/exercise-types", (route) => route.abort("failed"));
  await page.goto("/vater/fachlehrer");
  await expect(page.getByText(/Die Übungstypen konnten nicht geladen werden/)).toBeVisible();
});

test("Schalter „geteilte Übungen“: folgt dem Klick, lässt das Fach stehen und folgt der Adresse (B-450)", async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 900 });
  await vaterLogin(page, { id: "1", pin: "0000" });
  await page.goto("/vater/exercises");
  const schalter = page.getByLabel("geteilte Übungen anderer Väter anzeigen");

  // Fach wählen, dann den Schalter. `check()` prüft selbst, dass der Haken nach dem Klick steht – das ist der belegte Fehler
  // (unter Wiederholung rot, wenn der Haken an der Adresse statt am eigenen Zustand hängt).
  await page.getByLabel("Fach", { exact: true }).selectOption({ label: "Englisch" });
  await schalter.check();
  // Absichtserklärung, keine Probe: das Fach bleibt in der Adresse. Mit Render-Werten in `setFilter` gemessen blieb diese Zeile
  // grün (sechs Wiederholungen) – zwischen zwei Playwright-Aktionen kommt die Transition fast immer an.
  await expect(page).toHaveURL(/subjectId=\d+.*shared=1|shared=1.*subjectId=\d+/);

  // Die Adresse ändert sich ohne Neuaufbau der Seite (Navigation auf dieselbe Route): der Haken folgt ihr.
  await page.getByRole("link", { name: "📚 Übungen" }).click();
  await expect(page).toHaveURL(/\/vater\/exercises$/);
  await expect(schalter).not.toBeChecked();
});
