import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin } from "./helpers";

/*
 * B-253 + B-528: Im Tab „Erfolge“ stehen der Wochenvergleich gegen die eigene Vorwoche (`GET student/me/points/week`)
 * und bei jeder offenen Mission ihre Frist („nur noch heute“ / „noch N Tage inkl. heute“). Die Rechnung prüfen
 * `WochenvergleichTests` und `MissionFristTests` mit eingefrorener Uhr; hier liegt der Blick auf die laufende App, der
 * vorher ein Handblick am Gerät war.
 *
 * Die Uhr läuft echt: die erwartete Wochenfrist rechnet die Spec aus dem UTC-Wochentag (Woche Mo–So UTC), nicht aus
 * der Antwort des Servers – sonst prüfte sie nur, dass die Oberfläche abschreibt, was der Server sagt.
 *
 * Eigene Datei (eine Fläche je Spec, B-109); eigenes, frisches Kind – darum ist seine Vorwoche leer.
 */

const RUN = Date.now().toString(36).slice(-5);
const WORDS = [["dog", "Hund"], ["cat", "Katze"]] as const;

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown, key = "id") {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json())[key] as number;
}

/** Was das Panel für eine offene Wochenmission zeigen muss: bis Sonntag (UTC) einschließlich heute. */
function erwarteteWochenfrist(now: Date): string {
  const tag = now.getUTCDay(); // 0 = Sonntag
  if (tag === 0) return "nur noch heute";
  const tage = 8 - tag; // Montag: 7 Tage inkl. heute, Samstag: 2
  return `noch ${tage} Tage inkl. heute`;
}

test("Erfolge zeigt die Woche gegen die Vorwoche und die Frist jeder offenen Mission", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const pin = "8656";
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Erfolge ${RUN}`, pin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `Erfolge-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
    name: `Erfolge-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: `Unit ${RUN}`, orderIndex: 1 });
  const exVokabeln = await postId(request, `${units}/${unitId}/vocabulary`, vater, {
    title: `Tiere ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: WORDS.map(([front, back]) => ({ front, back })) },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater, { childId, title: `Erfolge-Plan ${RUN}`, durationDays: 7, active: true });
  const positionId = await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, vater,
    { exerciseId: exVokabeln, stage: 4, cadence: "Daily", useLeitner: false });

  // Zwei offene Missionen: Ziele, die ein bestandener Test nicht erreicht, damit beide ihre Frist zeigen.
  const TAG = `Tagesmission ${RUN}`, WOCHE = `Wochenmission ${RUN}`;
  const missions = `/api/v1/supervisor/children/${childId}/missions`;
  await postId(request, missions, vater, { title: TAG, metric: "TestsPassed", target: 3, period: "Daily", rewardPoints: 1 });
  await postId(request, missions, vater, { title: WOCHE, metric: "TestsPassed", target: 5, period: "Weekly", rewardPoints: 1 });

  // Münzen dieser Woche: ein bestandener Test der Position.
  const kindLogin = await request.post("/api/v1/auth/child", { data: { childId, pin } });
  expect(kindLogin.ok(), await kindLogin.text()).toBeTruthy();
  const kind = { Authorization: `Bearer ${(await kindLogin.json()).token}` };
  const tests = `/api/v1/student/study-plans/${planId}/positions/${positionId}/tests`;
  const attemptId = await postId(request, tests, kind, {}, "attemptId");
  const submit = await request.post(`${tests}/${attemptId}/submit`, {
    headers: kind, data: { answers: WORDS.map(([, back], itemIndex) => ({ itemIndex, givenAnswer: back })) },
  });
  expect(submit.ok(), await submit.text()).toBeTruthy();

  // Ein frisches Kind ohne Kauf und ohne Malus: alles, was es besitzt, hat es diese Woche verdient.
  const wallet = await request.get("/api/v1/student/me/points", { headers: kind });
  expect(wallet.ok(), await wallet.text()).toBeTruthy();
  const coins = ((await wallet.json()) as { coins: number }).coins;
  expect(coins, "der bestandene Test muss Münzen bringen, sonst entfiele die Zeile (0/0)").toBeGreaterThan(0);

  // Wie weit jede Mission steht, liest die Spec beim Server; die Frist rechnet sie selbst.
  const status = await request.get("/api/v1/student/me/missions", { headers: kind });
  expect(status.ok(), await status.text()).toBeTruthy();
  const stand = (await status.json()) as { title: string; current: number; completed: boolean }[];
  const tag = stand.find((m) => m.title === TAG), woche = stand.find((m) => m.title === WOCHE);
  expect(tag?.completed).toBe(false);
  expect(woche?.completed).toBe(false);
  const wochenfrist = erwarteteWochenfrist(new Date());

  await sohnLogin(page, { id: String(childId), pin });
  await page.getByRole("link", { name: "Erfolge", exact: true }).click();
  await expect(page.locator(".screen-title", { hasText: "Erfolge" })).toBeVisible();

  // B-253 tragend: ohne die Zeile (oder mit Shop/Manual-Buchungen gerechnet) stünde dieser Satz nicht da.
  const woch = page.locator(".week-comparison");
  await expect(woch).toContainText(`Diese Woche ${coins} 🪙 · letzte Woche bis jetzt 0`);
  await expect(woch).toContainText("(ganze letzte Woche: 0)");

  // B-528 tragend: statt des Zeitraums („Heute“/„Diese Woche“) steht die Frist.
  const zeile = (titel: string) => page.locator("div.row", { has: page.getByText(titel, { exact: true }) });
  await expect(zeile(TAG)).toContainText(`${tag!.current}/3 · nur noch heute`);
  await expect(zeile(WOCHE)).toContainText(`${woche!.current}/5 · ${wochenfrist}`);
});
