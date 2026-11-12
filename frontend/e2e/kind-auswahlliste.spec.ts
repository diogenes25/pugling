import { test, expect, type Page } from "@playwright/test";
import { createChild, vaterLogin } from "./helpers";

/*
 * B-207. Die Interessen-Auswahl des Kindes zeigt nur Schlagworte, die ein Creator für Kinder freigegeben hat. Gefahren
 * wird der ganze Weg: ein neues Schlagwort ist für das Kind unsichtbar, der Vater gibt es in der Schlagwort-Verwaltung
 * frei, danach steht es als Kachel da. Beleg ist jeweils die Seite des Kindes nach dem Neuladen, nicht die Meldung.
 */

const FATHER = { id: "1", pin: "0000" };
const CHILD = { name: "Auswahl-Kind", pin: "5152" };
const RUN = Date.now().toString(36).slice(-6);
const LABEL = `Kletterwand ${RUN}`;

async function sohnLogin(page: Page, childId: string) {
  await page.goto("/sohn");
  await page.locator("#childId").fill(childId);
  for (const d of CHILD.pin.split("")) {
    await page.locator(".keys button", { hasText: new RegExp(`^${d}$`) }).first().click();
  }
  await page.getByRole("button", { name: "▶ LOS" }).click();
  await expect(page.locator("nav.sohn-nav")).toBeVisible();
}

test("Ein Schlagwort erscheint beim Kind erst nach der Freigabe", async ({ browser, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const created = await request.post("/api/v1/creator/interest-tags", {
    headers: { Authorization: `Bearer ${(await login.json()).token}` },
    data: { label: LABEL, facet: "Sport" },
  });
  expect(created.ok(), await created.text()).toBeTruthy();

  const vaterCtx = await browser.newContext();
  const vater = await vaterCtx.newPage();
  await vaterLogin(vater, FATHER);
  const childId = await createChild(vater, CHILD.name, CHILD.pin);

  const sohnCtx = await browser.newContext();
  const sohn = await sohnCtx.newPage();
  await sohnLogin(sohn, childId);
  await sohn.goto("/sohn/ich");
  // Erst auf eine geladene Kachel warten, sonst wäre „nicht da" auch während des Ladens wahr.
  await expect(sohn.getByRole("button").filter({ hasText: "auswählen" }).first()).toBeVisible();
  await expect(sohn.getByRole("button", { name: new RegExp(LABEL) })).toHaveCount(0);

  await vater.goto("/vater/media");
  await vater.getByRole("button", { name: /Schlagwort-Vokabular verwalten/ }).click();
  await vater.getByLabel("Schlagwort suchen", { exact: true }).fill(LABEL);
  await vater.getByLabel("Schlagwort suchen", { exact: true }).press("Enter");
  await vater.getByLabel(`${LABEL} für Kinder freigeben`, { exact: true }).check();
  await vater.getByRole("button", { name: `Sport „${LABEL}" speichern` }).click();
  await expect(vater.getByText("Gespeichert.")).toBeVisible();

  await sohn.reload();
  await expect(sohn.getByRole("button", { name: new RegExp(LABEL) })).toBeVisible();

  await vaterCtx.close();
  await sohnCtx.close();
});
