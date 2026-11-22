import { test, expect, type APIRequestContext } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-673 – Rollengang Sprint 105: Der Vater will einen Plan, mit dem der ältere Bruder schon gelernt hat, dem jüngeren
 * geben. Die Plan-Seite nennt auf Deutsch, warum das nicht geht, und der Plan bleibt beim Bruder. Die Regel hält der
 * Server (`RueckwirkendePflichtTests.Gespielter_Plan_Laesst_Sich_Nicht_Umhaengen`); die Spec belegt, dass der Vater den
 * Grund an der Stelle liest, an der er gespeichert hat. Rot ohne Sperre (der Plan wechselt das Kind) und ohne deutschen
 * Satz (die englische Rohzeile stünde dort).
 *
 * Eigene Kinder: In der geteilten DB hängt jede Auswahl am Namen.
 */

const RUN = Date.now().toString(36).slice(-5);
const WORDS = [["dog", "Hund"], ["cat", "Katze"]] as const;

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown, key = "id") {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json())[key] as number;
}

test("Plan-Seite: ein gespielter Plan lässt sich keinem anderen Kind geben, und der Vater liest den Grund", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const pin = "8673";
  const bruder = await postId(request, "/api/v1/supervisor/children", vater, { name: `Bruder ${RUN}`, pin });
  const juengerer = await postId(request, "/api/v1/supervisor/children", vater, { name: `Jünger ${RUN}` });
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `Umhäng-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
    name: `Umhäng-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: `Unit ${RUN}`, orderIndex: 1 });
  const exerciseId = await postId(request, `${units}/${unitId}/vocabulary`, vater, {
    title: `Tiere ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: WORDS.map(([front, back]) => ({ front, back })) },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater,
    { childId: bruder, subjectId, title: `Bruder-Plan ${RUN}`, durationDays: 7, active: true });
  const positionId = await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, vater,
    { exerciseId, stage: 4, cadence: "Daily", useLeitner: false });

  // Der Bruder lernt: ein Test über die API.
  const kindLogin = await request.post("/api/v1/auth/child", { data: { childId: bruder, pin } });
  expect(kindLogin.ok(), await kindLogin.text()).toBeTruthy();
  const kind = { Authorization: `Bearer ${(await kindLogin.json()).token}` };
  const tests = `/api/v1/student/study-plans/${planId}/positions/${positionId}/tests`;
  const attemptId = await postId(request, tests, kind, {}, "attemptId");
  const submit = await request.post(`${tests}/${attemptId}/submit`, {
    headers: kind, data: { answers: WORDS.map((_, itemIndex) => ({ itemIndex, givenAnswer: "falsch" })) },
  });
  expect(submit.ok(), await submit.text()).toBeTruthy();

  await vaterLogin(page, { id: "1", pin: "0000" });
  await page.goto(`/vater/plan/${planId}`);
  // Der Knopf des PLANS, über seine Kopfzeile: jede Positionszeile bringt ein eigenes „Bearbeiten“ mit, und die Liste lädt
  // nach – ein ungezielter Klick hing an der Ladereihenfolge (B-790).
  await page.locator(".row").filter({ has: page.getByRole("heading", { name: `Bruder-Plan ${RUN}` }) })
    .getByRole("button", { name: "Bearbeiten" }).click();
  const kindFeld = page.getByRole("combobox", { name: "Kind" });
  await expect(kindFeld.locator("option", { hasText: `Jünger ${RUN}` })).toHaveCount(1);
  await kindFeld.selectOption(String(juengerer));
  await page.getByRole("button", { name: "Speichern" }).click();

  await expect(page.getByText("keinem anderen Kind mehr geben")).toBeVisible();
  await expect(page.getByText("study plan already has", { exact: false })).toHaveCount(0);
  const plan = await request.get(`/api/v1/supervisor/study-plans/${planId}`, { headers: vater });
  expect((await plan.json()).childId).toBe(bruder);
});
