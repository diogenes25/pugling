import { test, expect, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-409: „Verwendung" an einer Übung muss jede Stelle bei den eigenen Kindern nennen, die das Löschen sperrt – auch eine
 * Ziel-Etappe. Vorher stand bei einer Übung, die nur an einem Ziel hing, „Wird derzeit nirgends eingesetzt … Löschen ist
 * möglich", und der Klick auf „Löschen" endete in der 409-Meldung. Rollengang aus der Vater-Rolle: Übung anlegen, ein Ziel
 * mit einer Etappe auf genau diese Übung, dann „Verwendung" in der Übungsliste.
 *
 * Rot, wenn das Panel die Ziele nicht kennt (dann steht der „nirgends"-Satz da) oder der Server sie nicht liefert.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);

async function api(page: Page, url: string, data: unknown) {
  const token = await page.evaluate(() => localStorage.getItem("pugling.token"));
  const res = await page.request.post(url, { data, headers: { Authorization: `Bearer ${token}` } });
  expect(res.ok(), `POST ${url}: ${res.status()}`).toBeTruthy();
  return res.json();
}

test("Verwendung nennt die Ziel-Etappe, an der die Übung hängt", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const fach = `Zielfach ${RUN}`;
  const uebungTitel = `Zielwörter ${RUN}`;
  const zielTitel = `Unit sicher ${RUN}`;

  const subjectId = (await api(page, "/api/v1/creator/subjects", { name: fach })).id;
  const seriesId = (await api(page, "/api/v1/creator/textbook-series", { name: `Zielreihe ${RUN}`, subjectId })).id;
  const unitId = (await api(page, `/api/v1/creator/textbook-series/${seriesId}/units`, { label: "Unit 1", orderIndex: 1 })).id;
  const exerciseId = (await api(page, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/vocabulary`, {
    title: uebungTitel, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: [{ front: `goal ${RUN}`, back: "Ziel" }] },
  })).id;
  await api(page, "/api/v1/supervisor/children/1/objectives", {
    title: zielTitel, motivation: null, kind: "Committed", start: null, dueDate: null,
    rewardOnComplete: 0, rewardPerKeyResult: 0,
    keyResults: [{ subjectId, seriesUnitId: unitId, exerciseId, metric: "AvgMastery", targetValue: 80, title: "Achtzig Prozent" }],
  });

  await page.goto(`/vater/exercises?subjectId=${subjectId}`);
  const zeile = page.getByRole("group", { name: uebungTitel });
  await zeile.getByRole("button", { name: "Verwendung" }).click();

  await expect(zeile.getByText(new RegExp(`^Ziele: ${zielTitel} – Achtzig Prozent \\(`))).toBeVisible();
  await expect(zeile.getByText(/nirgends eingesetzt/)).toHaveCount(0);
});
