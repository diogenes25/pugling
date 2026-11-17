import { test, expect, type APIRequestContext } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-39 (Rollengang Vater): das Kind erfüllt heute seine Tagespflicht, der Vater öffnet die Übersicht, schaltet auf „Tag“
 * und liest am heutigen Balken seines Kindes „1 von 1 Zielen – Pflicht geschafft“; „Woche“ und „Monat“ zeigen ebenfalls
 * Balken, und die Kindzeile führt in den Lernstand.
 *
 * Rot, wenn der Endpunkt die Tagespflicht nicht summiert, der Umschalter keine neuen Balken holt oder der Link fehlt.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString(36).slice(-5);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown, key = "id") {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), `${url}: ${res.status()} ${await res.text()}`).toBeTruthy();
  return (await res.json())[key] as number;
}

test("Der Vater sieht die erfüllte Tagespflicht seines Kindes auf der Zeitachse", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const pin = "7" + String(Date.now() % 1000).padStart(3, "0");
  const name = `Zeit ${RUN}`;
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name, pin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `Zeitfach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, { name: `Zeitreihe ${RUN}`, subjectId });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: "Unit 1", orderIndex: 1 });
  const exerciseId = await postId(request, `${units}/${unitId}/vocabulary`, vater, {
    title: `Zeitwörter ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: [{ front: `dog ${RUN}`, back: "Hund" }] },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater,
    { childId, subjectId, title: `Zeitplan ${RUN}`, durationDays: 7, active: true });
  const positionId = await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, vater,
    { exerciseId, cadence: "Daily", stage: 4 });

  const kindLogin = await request.post("/api/v1/auth/child", { data: { childId, pin } });
  expect(kindLogin.ok(), await kindLogin.text()).toBeTruthy();
  const kind = { Authorization: `Bearer ${(await kindLogin.json()).token}` };
  const tests = `/api/v1/student/study-plans/${planId}/positions/${positionId}/tests`;
  const attemptId = await postId(request, tests, kind, {}, "attemptId");
  const submit = await request.post(`${tests}/${attemptId}/submit`, { headers: kind, data: { answers: [{ itemIndex: 0, givenAnswer: `dog ${RUN}` }] } }); // B-597: das Fremdwort
  expect(submit.ok(), await submit.text()).toBeTruthy();

  await vaterLogin(page, FATHER);
  await page.goto("/vater");
  await page.getByRole("button", { name: "Tag", exact: true }).click();
  const achse = page.getByRole("list", { name: `Zeitachse von ${name}` });
  await expect(achse.getByRole("listitem").last()).toContainText("1 von 1 Zielen – Pflicht geschafft ✓");
  await expect(achse.getByRole("listitem")).toHaveCount(14);

  await page.getByRole("button", { name: "Monat", exact: true }).click();
  await expect(achse.getByRole("listitem").last()).toContainText("1 von 1 Zielen – Pflicht geschafft ✓");

  await expect(page.getByRole("link", { name: `Lernstand von ${name}` })).toHaveAttribute("href", `/vater/kind/${childId}/lernstand`);
});
