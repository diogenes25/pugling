import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin, vaterLogin } from "./helpers";

/*
 * B-750/B-751 – der Rollengang des Sohns: der Vater plant eine Klassenarbeit mit Vokabelstoff, das Kind sieht auf seiner
 * Startseite, wann sie ist und wie viele Wörter es schon sicher kann. Der Stoff entsteht über die API (Gegenstand ist die
 * Sohn-App, nicht das Anlegen): ein eigenes Kind je Lauf, damit keine fremde Arbeit davor steht. Termin = UTC-heute + 5
 * Tage; der Server zählt in deutscher Zeit (B-730), die zwischen Mitternacht und ein bzw. zwei Uhr schon einen Tag weiter ist
 * – darum „in 4 oder 5 Tagen“.
 */

const RUN = Date.now().toString(36).slice(-5);
const TAG = 86_400_000;
const iso = (tage: number) => new Date(Date.now() + tage * TAG).toISOString().slice(0, 10);

async function postJson(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), `${url}: ${res.status()} ${await res.text()}`).toBeTruthy();
  return res.json();
}

test("das Kind sieht seine nächste Klassenarbeit und wie sicher es im Stoff ist", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };
  const pin = "8662";
  const childId = (await postJson(request, "/api/v1/supervisor/children", headers, { name: `KA-Countdown ${RUN}`, pin })).id;
  const subjectId = (await postJson(request, "/api/v1/creator/subjects", headers, { name: `KA-Countdown-Fach ${RUN}` })).id;
  const seriesId = (await postJson(request, "/api/v1/creator/textbook-series", headers, { name: `KA-Countdown-Reihe ${RUN}`, subjectId })).id;
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = (await postJson(request, units, headers, { label: "Unit 1", orderIndex: 1 })).id;
  const exerciseId = (await postJson(request, `${units}/${unitId}/vocabulary`, headers, {
    title: `KA-Countdown-Wörter ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de",
      items: [{ front: `apple ${RUN}`, back: "Apfel" }, { front: `pear ${RUN}`, back: "Birne" }] },
  })).id;
  // Zwei Arbeiten: die spätere steht zuerst im Anlegen – gezeigt wird trotzdem die frühere (B-750 AK 4).
  await postJson(request, "/api/v1/supervisor/class-tests", headers,
    { childId, title: `Spätere Arbeit ${RUN}`, scheduledDate: iso(20), subjectId });
  await postJson(request, "/api/v1/supervisor/class-tests", headers,
    { childId, title: `Vokabeltest ${RUN}`, scheduledDate: iso(5), subjectId, exerciseIds: [exerciseId] });

  await sohnLogin(page, { id: String(childId), pin });

  const karte = page.getByRole("region", { name: "Nächste Klassenarbeit" });
  await expect(karte).toBeVisible();
  await expect(karte).toContainText(`Vokabeltest ${RUN}`);
  await expect(karte).toContainText(`KA-Countdown-Fach ${RUN}`);
  await expect(karte).toContainText(/in [45] Tagen/);
  await expect(karte).not.toContainText("Spätere Arbeit");
  // B-751: zwei Wörter im Bündel, noch keins geübt.
  await expect(karte).toContainText("0 von 2 Wörtern sicher");

  // B-949: der Vater liest in seiner Liste dieselbe Zahl wie das Kind auf seiner Karte.
  await vaterLogin(page, { id: "1", pin: "0000" });
  await page.goto(`/vater/class-tests?childId=${childId}`);
  const zeile = page.getByRole("row").filter({ hasText: `Vokabeltest ${RUN}` });
  await expect(zeile).toContainText("0 von 2 Wörtern sicher");
  await expect(page.getByRole("row").filter({ hasText: `Spätere Arbeit ${RUN}` })).not.toContainText("Wörtern sicher");
});
