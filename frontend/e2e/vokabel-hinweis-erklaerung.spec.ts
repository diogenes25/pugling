import { test, expect, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-607, Rollengang aus der Creator-Rolle: im Bearbeiten-Dialog einer Vokabelübung erklärt das „ⓘ" an der Spalte
 * „Hinweis", wo das Kind den Hinweis zu sehen bekommt – nur beim Üben auf der Auswahlstufe. Dass der Server sich so
 * verhält, halten die Festschreibungen in `VokabelHinweisReichweiteTests`; hier geht es darum, dass der Creator es
 * erfährt.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);

async function api(page: Page, url: string, data: unknown) {
  const token = await page.evaluate(() => localStorage.getItem("pugling.token"));
  const res = await page.request.post(url, { data, headers: { Authorization: `Bearer ${token}` } });
  expect(res.ok(), `POST ${url}: ${res.status()}`).toBeTruthy();
  return res.json();
}

test("Die Spalte „Hinweis“ im Bearbeiten-Dialog erklärt, wo der Hinweis erscheint", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const name = `Hinweisfach ${RUN}`;
  const subjectId = (await api(page, "/api/v1/creator/subjects", { name })).id;
  const seriesId = (await api(page, "/api/v1/creator/textbook-series", { name, subjectId })).id;
  const unitId = (await api(page, `/api/v1/creator/textbook-series/${seriesId}/units`, { label: "Unit 1", orderIndex: 1 })).id;
  await api(page, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/vocabulary`, {
    title: name, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de",
      items: [{ front: `city${RUN}`, back: "Stadt", hint: "beginnt mit S" }] },
  });

  await page.goto("/vater/exercises");
  await page.locator('select[aria-label="Fach"]').selectOption({ label: name });
  await page.locator('select[aria-label="Reihe"]').selectOption({ label: name });
  await page.locator('select[aria-label="Unit"]').selectOption({ label: "Unit 1" });
  await page.getByRole("button", { name: "✏️ Bearbeiten" }).first().click();
  await expect(page.getByLabel(`Hinweis für city${RUN}`, { exact: true })).toHaveValue("beginnt mit S");

  await page.getByRole("button", { name: "Erklärung zu „Hinweis\"" }).click();
  await expect(page.getByText(/nur beim Üben auf der Auswahlstufe, im Abschlusstest nie/)).toBeVisible();
  await expect(page.getByText(/Bei „Deutsch → Fremdwort“ erscheint er auf keiner Stufe/)).toBeVisible();
});
