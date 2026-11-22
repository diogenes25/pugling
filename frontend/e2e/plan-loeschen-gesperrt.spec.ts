import { test, expect, type APIRequestContext } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-659 – Rollengang Sprint B2 (Nachtlauf B 2026-11-23): Der Vater will einen Plan löschen, mit dem sein Kind schon
 * gelernt hat. Die Bestätigung kündigt die Sperre an, die Plan-Seite nennt danach auf Deutsch, warum es nicht geht und was
 * stattdessen hilft („Deaktivieren“), und der Plan bleibt stehen. Die Regel hält der Server
 * (`PflichtWirdKuerSerieTests.Gespielter_Plan_Laesst_Sich_Nicht_Loeschen`, der auch den Ausweg nimmt); die Spec belegt,
 * dass der Vater den Grund dort liest, wo er gelöscht hat. Rot ohne Sperre (der Plan ist weg und die Seite springt zur
 * Übersicht) und ohne deutschen Satz (die englische Rohzeile stünde dort).
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

test("Plan-Seite: ein gespielter Plan lässt sich nicht löschen, und der Vater liest den Grund und den Ausweg", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const pin = "8659";
  const lerner = await postId(request, "/api/v1/supervisor/children", vater, { name: `Lerner ${RUN}`, pin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `Lösch-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
    name: `Lösch-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: `Unit ${RUN}`, orderIndex: 1 });
  const exerciseId = await postId(request, `${units}/${unitId}/vocabulary`, vater, {
    title: `Tiere ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: WORDS.map(([front, back]) => ({ front, back })) },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater,
    { childId: lerner, subjectId, title: `Lern-Plan ${RUN}`, durationDays: 7, active: true });
  const positionId = await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, vater,
    { exerciseId, stage: 4, cadence: "Daily", useLeitner: false });

  // Das Kind lernt: ein Test über die API.
  const kindLogin = await request.post("/api/v1/auth/child", { data: { childId: lerner, pin } });
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
  await expect(page.getByRole("heading", { name: `Lern-Plan ${RUN}` })).toBeVisible();

  // Die Bestätigung kündigt die Sperre schon an, bevor der Vater klickt.
  let frage = "";
  page.once("dialog", async (d) => { frage = d.message(); await d.accept(); });
  await page.getByRole("button", { name: "Löschen" }).click();

  await expect(page.getByText("lässt sich der Plan nicht löschen (stattdessen „Deaktivieren“)")).toBeVisible();
  await expect(page.getByText("study plan already has", { exact: false })).toHaveCount(0);
  expect(frage).toContain("lässt er sich nicht löschen");
  await expect(page).toHaveURL(new RegExp(`/vater/plan/${planId}$`));
  const plan = await request.get(`/api/v1/supervisor/study-plans/${planId}`, { headers: vater });
  expect(plan.status()).toBe(200);
});
