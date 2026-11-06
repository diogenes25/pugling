import { test, expect, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-270: Die Bewertung einer Übung gehört dorthin, wo der Vater sie auswählt – in die Übungsauswahl des Planbaus, nicht
 * nur in die Verwaltungsliste. Diese Spec ist der Rollengang aus der Vater-Rolle: zwei eigene Übungen in einem eigenen
 * Fach, eine davon von einem ZWEITEN Konto bewertet (die eigene darf man nicht bewerten). Im Planbau zeigt jede Zeile ihre
 * Bewertung, und „beste Bewertung zuerst" stellt die bewertete vor die unbewertete – im Katalog steht sie dahinter.
 *
 * Rot, wenn die Zeile die Bewertung nicht zeigt oder die Reihenfolge-Wahl die Abfrage nicht umstellt.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);

async function token(page: Page) {
  return page.evaluate(() => localStorage.getItem("pugling.token"));
}

async function api(page: Page, method: "POST" | "PUT", url: string, data: unknown, bearer?: string | null) {
  const res = await page.request.fetch(url, {
    method, data, headers: { Authorization: `Bearer ${bearer ?? await token(page)}` },
  });
  expect(res.ok(), `${method} ${url}: ${res.status()}`).toBeTruthy();
  return res.json();
}

test("Die Übungsauswahl im Planbau zeigt die Bewertung und sortiert nach ihr", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const fach = `Bewertungsfach ${RUN}`;
  const erste = `Aaa unbewertet ${RUN}`;
  const zweite = `Zzz gut bewertet ${RUN}`;

  const subjectId = (await api(page, "POST", "/api/v1/creator/subjects", { name: fach })).id;
  const seriesId = (await api(page, "POST", "/api/v1/creator/textbook-series", { name: `Bewertungsreihe ${RUN}`, subjectId })).id;
  const unitId = (await api(page, "POST", `/api/v1/creator/textbook-series/${seriesId}/units`, { label: "Unit 1", orderIndex: 1 })).id;
  const uebung = (title: string, orderIndex: number) => api(page, "POST",
    `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/vocabulary`, {
      title, orderIndex, rewardPoints: 10,
      config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: [{ front: `hello ${title}`, back: "hallo" }] },
    });
  // Im Katalog steht die unbewertete VORN (orderIndex 1): nur so beweist „beste zuerst" eine Umstellung.
  await uebung(erste, 1);
  const gut = (await uebung(zweite, 2)).id;

  // Ein zweites Konto bewertet – die eigene Übung darf ihr Besitzer nicht bewerten (`cannot_rate_own_exercise`).
  const fremd = await page.request.post("/api/v1/supervisor/adults", { data: { name: `Bewerter ${RUN}`, pin: "4321" } });
  expect(fremd.ok()).toBeTruthy();
  const fremdId = (await fremd.json()).id;
  const login = await page.request.post("/api/v1/auth/adult", { data: { adultId: fremdId, pin: "4321" } });
  expect(login.ok()).toBeTruthy();
  await api(page, "PUT", `/api/v1/creator/exercises/${gut}/rating`, { stars: 5, comment: null }, (await login.json()).token);

  await page.goto("/vater/plan/new");
  await page.locator("#plan-title").fill(`Bewertungsplan ${RUN}`);
  await page.getByRole("combobox", { name: "Kind" }).selectOption("1");
  await page.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(page).toHaveURL(/\/vater\/plan\/\d+$/);
  await page.locator('select[aria-label="Fach-Filter"]').selectOption({ label: fach });

  const radios = page.getByRole("radiogroup", { name: "Übung wählen" }).getByRole("radio");
  await expect(radios).toHaveCount(2);
  await expect(page.getByRole("radio", { name: new RegExp(zweite) })).toBeEnabled();
  // Die Zeile trägt ihre Bewertung – die unbewertete sagt es auch.
  await expect(page.getByRole("radio", { name: new RegExp(`${zweite}.*★ 5,0 \\(1 Bewertung\\)`) })).toBeVisible();
  await expect(page.getByRole("radio", { name: new RegExp(`${erste}.*noch unbewertet`) })).toBeVisible();
  // Katalog-Reihenfolge: die unbewertete vorn – der Ausgangszustand, gegen den die Umstellung misst.
  await expect(radios.first()).toHaveAccessibleName(new RegExp(erste));

  await page.getByLabel("Trefferliste sortieren", { exact: true }).selectOption("rating");
  await expect(radios.first()).toHaveAccessibleName(new RegExp(zweite));
});
