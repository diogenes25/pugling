import { test, expect, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-396. Ein Paar einer Übersetzungs-Übung ist an einen Eintrag im Vokabel-Store gebunden – und der gehört zu
 * EINEM Sprachpaar. Änderte der Vater im Bearbeiten-Dialog nur die Ausgangssprache, behielt der Editor die
 * Bindung: die Übung zeigte weiter auf die Vokabel des alten Sprachpaars, im neuen gab es sie nie.
 *
 * Gemessen wird am Store, nicht am Formular: nach dem Speichern muss es den Satz im NEUEN Sprachpaar geben.
 * Rot, wenn der Editor die Bindung beim Sprachwechsel mitschickt – dann legt der Server nichts an.
 *
 * Das ist zugleich der Rollengang aus der Creator-Rolle.
 */

const FATHER = { id: "1", pin: "0000" };

async function token(page: Page) {
  return page.evaluate(() => localStorage.getItem("pugling.token"));
}

/** Store-Einträge mit genau diesem Wort, als (Quelle, Ziel). */
async function eintraege(page: Page, wort: string) {
  const res = await page.request.get(`/api/v1/creator/vocabulary?word=${encodeURIComponent(wort)}`, {
    headers: { Authorization: `Bearer ${await token(page)}` },
  });
  expect(res.ok()).toBeTruthy();
  const rows = (await res.json()) as { word: string; sourceLanguage: string; targetLanguage: string }[];
  return rows.filter((r) => r.word === wort).map((r) => `${r.sourceLanguage}→${r.targetLanguage}`).sort();
}

test("Ein Wechsel der Ausgangssprache bindet das Paar im neuen Sprachpaar", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const name = `Sprachwechsel ${Date.now().toString().slice(-6)}`;
  const satz = `Where do you live, ${name}?`;

  await page.goto("/vater/katalog");
  await page.getByPlaceholder("z. B. Französisch").fill(name);
  await page.getByRole("button", { name: "Neues Fach anlegen" }).click();
  await expect(page.locator("#ca-subject")).toHaveValue(/\d+/);

  await page.goto("/vater/lehrwerke");
  await page.locator("#ns-name").fill(name);
  await page.locator("#ns-subject").selectOption({ label: name });
  await page.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(page.getByText(/steht im Katalog/)).toBeVisible();
  await page.getByRole("row", { name: new RegExp(name) }).getByRole("button", { name: "Units" }).click();
  await page.locator('[id^="unit-label-new"]').fill("Unit 1");
  await page.getByRole("button", { name: "Unit hinzufügen" }).click();
  await expect(page.getByRole("button", { name: "Unit 1 bearbeiten" })).toBeVisible();

  await page.goto("/vater/exercises/neu");
  await page.locator('select[aria-label="Fach"]').selectOption({ label: name });
  await page.locator('select[aria-label="Reihe"]').selectOption({ label: name });
  await page.locator('select[aria-label="Unit"]').selectOption({ label: "Unit 1" });
  await page.locator('select[aria-label="Übungstyp"]').selectOption("Translation");
  await page.locator("#ex-title").fill(name);
  await page.locator("#cfg-tr-src").selectOption("en");
  await page.locator("#cfg-tr-tgt").selectOption("de");
  await page.getByLabel("Satz (Ausgangssprache)", { exact: true }).fill(satz);
  await page.getByLabel("Übersetzung", { exact: true }).fill("Wo wohnst du?");
  await page.getByRole("button", { name: "Übung anlegen" }).click();
  await expect(page.getByText(`Übung „${name}" angelegt.`)).toBeVisible();
  expect(await eintraege(page, satz)).toEqual(["en→de"]);

  await page.goto("/vater/exercises");
  await page.locator('select[aria-label="Fach"]').selectOption({ label: name });
  await page.locator('select[aria-label="Reihe"]').selectOption({ label: name });
  await page.locator('select[aria-label="Unit"]').selectOption({ label: "Unit 1" });
  // Die Unit ist eindeutig benannt: genau eine Übung, also ohne Auswahl per Index.
  await expect(page.getByRole("button", { name: "✏️ Bearbeiten" })).toHaveCount(1);
  await page.getByRole("button", { name: "✏️ Bearbeiten" }).click();
  await expect(page.locator("#ed-title")).toBeVisible();

  // Nur die Ausgangssprache ändern – Satz und Übersetzung bleiben stehen.
  await page.locator("#cfg-tr-src").selectOption("fr");
  await page.getByRole("button", { name: "Inhalt speichern" }).click();
  await expect(page.getByText("Inhalt gespeichert.")).toBeVisible();

  expect(await eintraege(page, satz)).toEqual(["en→de", "fr→de"]);
});
