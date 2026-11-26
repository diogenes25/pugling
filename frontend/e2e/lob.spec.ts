import { test, expect } from "@playwright/test";
import { createChild, sohnLogin, vaterLogin } from "./helpers";

/*
 * B-377, Rollengang Vater → Sohn → Vater: der Vater lobt aus „Heute“, der Sohn sieht das Lob einmal mit dem Namen des
 * Vaters und dankt, beim nächsten Öffnen kommt es nicht wieder, und der Vater liest „gelesen am“. Ein eigenes Kind, damit
 * das Overlay nicht über der Startseite anderer Specs liegt (geteilte Wegwerf-DB).
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);

test("Ein Lob erreicht den Sohn genau einmal, und der Vater sieht, dass es gelesen ist", async ({ browser }) => {
  const vaterCtx = await browser.newContext();
  const vater = await vaterCtx.newPage();
  await vaterLogin(vater, FATHER);
  const name = `Lobkind ${RUN}`;
  const pin = "3770";
  const childId = await createChild(vater, name, pin);

  await vater.goto("/vater");
  const zeile = vater.getByRole("list", { name: "Kinder heute" }).getByRole("listitem")
    .filter({ has: vater.getByRole("link", { name, exact: true }) });
  await zeile.getByRole("button", { name: `${name} loben` }).click();
  const senden = zeile.getByRole("button", { name: "Senden" });
  await expect(senden).toBeDisabled();
  await zeile.getByRole("button", { name: "Toll gelernt! Weiter so!" }).click();
  await senden.click();
  await expect(vater.getByText(`Lob an ${name} gesendet.`)).toBeVisible();
  await expect(zeile.getByRole("list", { name: `Deine Lobe an ${name}` })).toContainText("noch nicht gelesen");

  const sohnCtx = await browser.newContext();
  const sohn = await sohnCtx.newPage();
  await sohnLogin(sohn, { id: childId, pin });
  const dialog = sohn.getByRole("dialog");
  await expect(dialog).toBeVisible();
  await expect(dialog).toContainText("Toll gelernt! Weiter so!");
  await expect(dialog.getByRole("heading", { name: /^Lob von / })).toBeVisible();
  await dialog.getByRole("button", { name: "Danke!" }).click();
  await expect(dialog).toHaveCount(0);

  // Beim nächsten Öffnen kommt es nicht wieder: der Stempel liegt am Server, nicht im Gerät.
  await sohn.reload();
  await expect(sohn.locator("nav.sohn-nav")).toBeVisible();
  await expect(sohn.getByRole("dialog")).toHaveCount(0);

  await vater.reload();
  const zeileNeu = vater.getByRole("list", { name: "Kinder heute" }).getByRole("listitem")
    .filter({ has: vater.getByRole("link", { name, exact: true }) });
  await zeileNeu.getByRole("button", { name: `${name} loben` }).click();
  await expect(zeileNeu.getByRole("list", { name: `Deine Lobe an ${name}` })).toContainText("gelesen am");

  await vaterCtx.close();
  await sohnCtx.close();
});
