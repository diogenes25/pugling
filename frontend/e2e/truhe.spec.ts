import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin } from "./helpers";

/*
 * B-527, AK 9 – der Rollengang zur Tagestruhe: das Kind schafft sein Tagesziel, sieht am Ende des Pfads die
 * leuchtende Truhe, öffnet sie selbst und sieht den Inhalt; der Münzstand im HUD steigt um genau diesen Betrag, und
 * nach dem Neuladen steht die Truhe offen. Die Regeln (gezogen beim Verdienen, gebucht beim Öffnen, älteste zuerst,
 * nur das Kind) hält der Server (`DailyBoxTests`). Aufbau wie `belohnung-trotz-wochenziel.spec.ts`.
 *
 * Eigenes Kind: je Kind ist genau ein aktiver Plan spielbar, und die Truhe gehört dem Kind, nicht dem Plan.
 */

const RUN = Date.now().toString(36).slice(-5);
const WORDS = [["dog", "Hund"], ["cat", "Katze"], ["house", "Haus"], ["tree", "Baum"]] as const;

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown, key = "id") {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json())[key] as number;
}

test("Tagesziel geschafft: das Kind öffnet die Truhe auf dem Pfad, die Münzen kommen an", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const pin = "8655";
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Truhe ${RUN}`, pin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `Truhen-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
    name: `Truhen-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: `Unit ${RUN}`, orderIndex: 1 });
  const exerciseId = await postId(request, `${units}/${unitId}/vocabulary`, vater, {
    title: `Tiere ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: WORDS.map(([front, back]) => ({ front, back })) },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater, { childId, title: `Truhen-Plan ${RUN}`, durationDays: 7, active: true });
  const positionId = await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, vater,
    { exerciseId, stage: 4, cadence: "Daily", useLeitner: false });

  await sohnLogin(page, { id: String(childId), pin });
  const truhe = page.getByRole("region", { name: "Tagestruhe" });
  // Zu: nur der Hinweis, kein Knopf.
  await expect(truhe).toContainText("Schaffe deine Tagesziele");
  await expect(truhe.getByRole("button")).toHaveCount(0);

  // Das Tagesziel erfüllen: ein bestandener Test der Position.
  const kindLogin = await request.post("/api/v1/auth/child", { data: { childId, pin } });
  expect(kindLogin.ok(), await kindLogin.text()).toBeTruthy();
  const kind = { Authorization: `Bearer ${(await kindLogin.json()).token}` };
  const tests = `/api/v1/student/study-plans/${planId}/positions/${positionId}/tests`;
  const attemptId = await postId(request, tests, kind, {}, "attemptId");
  const submit = await request.post(`${tests}/${attemptId}/submit`, {
    headers: kind, data: { answers: WORDS.map(([, back], itemIndex) => ({ itemIndex, givenAnswer: back })) },
  });
  expect(submit.ok(), await submit.text()).toBeTruthy();

  await page.reload();
  // Bereit: die Truhe leuchtet und hat einen Knopf; ihr Inhalt ist noch nicht zu sehen.
  await expect(truhe).toHaveClass(/bereit/);
  await expect(truhe).not.toContainText("Münzen");
  const hud = page.locator(".sohn-hud b.tabnum").first();
  await expect(hud).toHaveText(/^-?\d+$/);
  const vorher = Number(await hud.textContent());

  await truhe.getByRole("button", { name: "Truhe öffnen" }).click();
  const inhalt = truhe.getByRole("status");
  // Gems sind ein Zufallsanteil ab 0 (`DailyBox:MinGems` = 0): ohne Gems entfällt der zweite Teil, statt „+0 Gems“
  // zu zeigen. Die Zusicherung verlangt darum die Münzen immer und die Gems nur, wenn es welche gab.
  await expect(inhalt).toHaveText(/^\+\d+ Münzen(, \+[1-9]\d* Gems)?$/);
  const muenzen = Number(/^\+(\d+)/.exec((await inhalt.textContent()) ?? "")![1]);
  expect(muenzen).toBeGreaterThan(0);
  await expect(hud).toHaveText(String(vorher + muenzen));

  // Nach dem Neuladen steht die Truhe offen, mit demselben Inhalt, und lässt sich nicht noch einmal öffnen.
  await page.reload();
  await expect(truhe).toHaveClass(/offen/);
  await expect(truhe.getByRole("status")).toHaveText(new RegExp(`^\\+${muenzen} Münzen`));
  await expect(truhe.getByRole("button")).toHaveCount(0);
  await expect(hud).toHaveText(String(vorher + muenzen));
});
