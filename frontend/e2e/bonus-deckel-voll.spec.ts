import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin } from "./helpers";

/*
 * B-543: Ist der Bonus-Tagesdeckel voll, bietet eine Übung fester Form (keine Vokabeln) keinen Start an – der Server wiese ihn ab
 * (`extra_cap_reached`), und ohne die Sperre verbrannte der Start sie für 0 Münzen. Eine Vokabelzeile behält „Spielen“
 * (Üben ohne Münzen bleibt erlaubt). Die Regeln prüft `ExtraTrainingTests`; hier liegt der Blick auf die laufende App,
 * der vorher ein Handblick am Gerät war.
 *
 * Gefahren wird der Deckel > 0 (Entscheidung 4, „morgen wieder“): Deckel 4, eine erste Rechen-Übung zahlt 2 × 2 🪙
 * und füllt ihn, die zweite Rechen-Übung wartet dann bis morgen.
 *
 * Eigene Datei (eine Fläche je Spec, B-109); eigenes Kind, weil je Kind genau ein aktiver Plan spielbar ist.
 */

const RUN = Date.now().toString(36).slice(-5);
const WORDS = [["dog", "Hund"], ["cat", "Katze"]] as const;

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown, key = "id") {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json())[key] as number;
}

test("Bei vollem Bonus-Deckel wartet eine Übung fester Form bis morgen, Vokabeln bleiben spielbar", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const pin = "8655";
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Deckel voll ${RUN}`, pin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `Deckel-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
    name: `Deckel-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: `Unit ${RUN}`, orderIndex: 1 });
  const VOKABELN = `Tiere ${RUN}`, ERSTE = `Rechnen eins ${RUN}`, ZWEITE = `Rechnen zwei ${RUN}`;
  const exVokabeln = await postId(request, `${units}/${unitId}/vocabulary`, vater, {
    title: VOKABELN, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: WORDS.map(([front, back]) => ({ front, back })) },
  });
  const exErste = await postId(request, `${units}/${unitId}/arithmetic`, vater, {
    title: ERSTE, orderIndex: 2, rewardPoints: 10,
    config: { problems: [{ prompt: "2 + 3", answer: 5, tolerance: 0 }, { prompt: "4 × 4", answer: 16, tolerance: 0 }] },
  });
  const exZweite = await postId(request, `${units}/${unitId}/arithmetic`, vater, {
    title: ZWEITE, orderIndex: 3, rewardPoints: 10,
    config: { problems: [{ prompt: "1 + 1", answer: 2, tolerance: 0 }] },
  });

  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater, { childId, title: `Deckel-Plan ${RUN}`, durationDays: 7, active: true });
  const positionId = await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, vater,
    { exerciseId: exVokabeln, stage: 4, cadence: "Daily", useLeitner: false });

  // Der eigene Deckel des Kindes: klein genug, dass eine Rechen-Übung ihn füllt.
  const cap = await request.patch(`/api/v1/supervisor/children/${childId}`, { headers: vater, data: { bonusDailyCoinCap: 4 } });
  expect(cap.ok(), await cap.text()).toBeTruthy();

  // Die Pflicht über die API erfüllen – erst dann öffnet das Bonus-Training.
  const kindLogin = await request.post("/api/v1/auth/child", { data: { childId, pin } });
  expect(kindLogin.ok(), await kindLogin.text()).toBeTruthy();
  const kind = { Authorization: `Bearer ${(await kindLogin.json()).token}` };
  const tests = `/api/v1/student/study-plans/${planId}/positions/${positionId}/tests`;
  const attemptId = await postId(request, tests, kind, {}, "attemptId");
  const submit = await request.post(`${tests}/${attemptId}/submit`, {
    headers: kind, data: { answers: WORDS.map(([, back], itemIndex) => ({ itemIndex, givenAnswer: back })) },
  });
  expect(submit.ok(), await submit.text()).toBeTruthy();

  // Die erste Rechen-Übung über die API spielen: 2 Treffer × 3 🪙 = 6, gekappt auf 4 – der Deckel ist voll.
  const bonus = `/api/v1/student/children/${childId}/extra-training`;
  const start = await request.post(`${bonus}/${exErste}/runs`, { headers: kind });
  expect(start.ok(), await start.text()).toBeTruthy();
  const run = (await start.json()) as { runId: number; cards: { itemIndex: number; prompt: string | null }[] };
  const result = await request.post(`${bonus}/runs/${run.runId}/submit`, {
    headers: kind,
    data: { answers: run.cards.map((c) => ({ itemIndex: c.itemIndex, givenAnswer: c.prompt?.includes("2 + 3") ? "5" : "16", wasKnown: null })) },
  });
  expect(result.ok(), await result.text()).toBeTruthy();
  const graded = (await result.json()) as { coinsToday: number; dailyCoinCap: number | null };
  expect(graded).toMatchObject({ coinsToday: 4, dailyCoinCap: 4 });

  // Der Server sperrt den Start der zweiten Rechen-Übung (Entscheidung 1) – die Zeile darf ihn also nicht anbieten.
  const refused = await request.post(`${bonus}/${exZweite}/runs`, { headers: kind });
  expect(refused.status(), await refused.text()).toBe(409);
  expect(await refused.text()).toContain("extra_cap_reached");

  await sohnLogin(page, { id: String(childId), pin });
  await page.getByRole("link", { name: "Zum Bonus-Training" }).click();
  await expect(page.getByRole("heading", { name: "Bonus-Training" })).toBeVisible();
  await expect(page.getByText("Heute verdient: 4 / 4 🪙", { exact: true })).toBeVisible();
  await expect(page.getByText(
    "Tagesdeckel erreicht – Vokabeln üben darfst du weiter, ohne Münzen. Die anderen Übungen warten bis morgen.",
    { exact: true })).toBeVisible();

  const zeile = (titel: string) => page.locator(".bonus-zeile", { hasText: titel });
  // Tragend für B-543: ohne die Sperre stünde hier „Spielen“ statt des Satzes (und der Link führte in einen 409).
  await expect(zeile(ZWEITE)).toContainText("Morgen wieder – heute würde sie dir nur die Lösungen zeigen, ohne Münzen");
  await expect(zeile(ZWEITE).getByRole("link")).toHaveCount(0);
  // Seit B-538 ist auch die gespielte Übung wieder spielbar (heute zur Hälfte) – am vollen Deckel wartet sie wie die zweite.
  await expect(zeile(ERSTE)).toContainText("Morgen wieder – heute würde sie dir nur die Lösungen zeigen, ohne Münzen");
  await expect(zeile(ERSTE).getByRole("link")).toHaveCount(0);
  // Vokabeln dürfen weiter, ohne Münzen (Entscheidung 5).
  await expect(zeile(VOKABELN).getByRole("link", { name: `Spielen: ${VOKABELN}` })).toBeVisible();
  await expect(zeile(VOKABELN)).not.toContainText("Morgen wieder");
});
