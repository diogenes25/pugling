import { expect, test, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-291: Wechselt eine Reihe ihr Fach, während eine Unit ein Grammatik-Thema des alten Fachs trägt, verweigert der
 * Server mit 409 und nennt, wie viele Übungen und Units betroffen sind. Diese Spec ist der Rollengang aus der
 * Creator-Rolle: der Aufbau (zwei Fächer, Reihe, Unit mit Thema) läuft über die API, der Fachwechsel selbst über das
 * Bearbeiten-Formular der Reihe – dort muss die Antwort ankommen, und das Fach muss stehen bleiben.
 *
 * Rot, wenn der Server den Wechsel durchlässt (dann „Gespeichert." und das neue Fach in der Zeile) oder die
 * Oberfläche die Begründung verschluckt.
 */

const FATHER = { id: "1", pin: "0000" };
const STAMP = Date.now().toString(36).slice(-5);

async function api(page: Page, method: "POST" | "PATCH", url: string, data: unknown) {
  const token = await page.evaluate(() => localStorage.getItem("pugling.token"));
  const res = await page.request.fetch(url, { method, data, headers: { Authorization: `Bearer ${token}` } });
  expect(res.ok(), `${method} ${url}: ${res.status()}`).toBeTruthy();
  return (await res.json()) as { id: number };
}

test("Der Fachwechsel einer Reihe mit Themen des alten Fachs wird verweigert und begründet", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const alt = `Altfach ${STAMP}`;
  const neu = `Neufach ${STAMP}`;
  const reihe = `Wechselreihe ${STAMP}`;

  const altId = (await api(page, "POST", "/api/v1/creator/subjects", { name: alt })).id;
  await api(page, "POST", "/api/v1/creator/subjects", { name: neu });
  const seriesId = (await api(page, "POST", "/api/v1/creator/textbook-series", { name: reihe, subjectId: altId })).id;
  const unitId = (await api(page, "POST", `/api/v1/creator/textbook-series/${seriesId}/units`, { label: "Unit 1", orderIndex: 1 })).id;
  const topicId = (await api(page, "POST", "/api/v1/creator/grammar-topics", { label: `Thema ${STAMP}`, subjectId: altId })).id;
  await api(page, "PATCH", `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}`, { grammarTopicIds: [topicId] });

  await page.goto("/vater/lehrwerke");
  await page.getByRole("button", { name: `Reihe bearbeiten: „${reihe}"` }).click();
  await page.locator('select[id^="se-subject-"]').selectOption({ label: neu });
  await page.getByRole("button", { name: `Speichern: „${reihe}"` }).click();

  // Deutsch seit B-403 (`GERMAN_PROBLEM_TEXT`); die Anzahlen trägt nur das englische `detail`.
  await expect(page.getByText(/hängt noch an ihrem bisherigen Fach/)).toBeVisible();
  await expect(page.getByText("Gespeichert.", { exact: true })).toHaveCount(0);

  await page.reload();
  await expect(page.getByRole("row", { name: new RegExp(reihe) })).toContainText(alt);
});
