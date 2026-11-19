import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin, vaterLogin } from "./helpers";

/*
 * B-584 (Rollengang Vater, AK 8 und 9): das Kind schreibt einen Test mit falschen Antworten, der Vater öffnet den Lernstand,
 * sieht die schwachen Wörter und weist sie mit einem Griff als tägliche Wiederholung zu; das Kind sieht danach die neue
 * Tagespflicht auf seinem Pfad.
 *
 * Rot, wenn der Knopf fehlt, gesperrt bleibt, der Server ablehnt (Wörter ohne Lernstand, falsches Fach) oder die neue
 * Position beim Kind nicht als spielbare Pflicht erscheint.
 *
 * Eigenes Kind: je Kind und Fach ist genau ein aktiver Plan spielbar.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString(36).slice(-5);
// Zwei Übungen, beide im Plan: sind alle Wörter einer einzigen Übung schwach, wäre die Wiederholung genau diese Übung,
// und der Server lehnte sie als doppelte Pflicht ab (B-584, AK 6 – gefunden am laufenden Produkt). Über zwei Übungen
// hinweg ist die Wortmenge neu.
const WORDS_A = [["dog", "Hund"], ["cat", "Katze"], ["cow", "Kuh"]] as const;
const WORDS_B = [["pig", "Schwein"]] as const;

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown, key = "id") {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), `${url}: ${res.status()} ${await res.text()}`).toBeTruthy();
  return (await res.json())[key] as number;
}

test("Der Vater macht schwache Wörter zur täglichen Wiederholung, das Kind sieht sie", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const pin = "6" + RUN.replace(/\D/g, "1").padEnd(3, "1").slice(0, 3);
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Schwach ${RUN}`, pin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `Schwachfach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, { name: `Schwachreihe ${RUN}`, subjectId });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: "Unit 1", orderIndex: 1 });
  const vokabeln = (title: string, words: readonly (readonly [string, string])[]) =>
    postId(request, `${units}/${unitId}/vocabulary`, vater, {
      title, orderIndex: 1, rewardPoints: 10,
      config: { direction: "front-to-back", sourceLang: "en", targetLang: "de",
        items: words.map(([front, back]) => ({ front: `${front} ${RUN}`, back })) },
    });
  const exA = await vokabeln(`Tiere ${RUN}`, WORDS_A);
  const exB = await vokabeln(`Hof ${RUN}`, WORDS_B);
  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater,
    { childId, subjectId, title: `Schwachplan ${RUN}`, durationDays: 7, active: true });
  const positions = `/api/v1/supervisor/study-plans/${planId}/positions`;
  const posA = await postId(request, positions, vater, { exerciseId: exA, cadence: "Daily", stage: 4 });
  const posB = await postId(request, positions, vater, { exerciseId: exB, cadence: "Daily", stage: 4 });

  // Das Kind schreibt beide Tests und liegt überall daneben – daraus entsteht der Lernstand mit schwachen Wörtern.
  const kindLogin = await request.post("/api/v1/auth/child", { data: { childId, pin } });
  expect(kindLogin.ok(), await kindLogin.text()).toBeTruthy();
  const kind = { Authorization: `Bearer ${(await kindLogin.json()).token}` };
  for (const [positionId, count] of [[posA, WORDS_A.length], [posB, WORDS_B.length]] as const) {
    const tests = `/api/v1/student/study-plans/${planId}/positions/${positionId}/tests`;
    const attemptId = await postId(request, tests, kind, {}, "attemptId");
    const submit = await request.post(`${tests}/${attemptId}/submit`, {
      headers: kind, data: { answers: Array.from({ length: count }, (_, itemIndex) => ({ itemIndex, givenAnswer: "falsch" })) },
    });
    expect(submit.ok(), await submit.text()).toBeTruthy();
  }

  await vaterLogin(page, FATHER);
  await page.goto(`/vater/kind/${childId}/lernstand`);
  const knopf = page.getByRole("button", { name: "Diese 4 Wörter als tägliche Wiederholung zuweisen" });
  await expect(knopf).toBeEnabled();
  await knopf.click();
  await expect(page.getByText("4 Wörter sind jetzt eine tägliche Wiederholung.")).toBeVisible();
  await expect(page.getByRole("link", { name: /Zum Plan/ })).toHaveAttribute("href", `/vater/plan/${planId}`);

  // Ein zweiter Klick mit denselben Wörtern ist dieselbe Pflicht doppelt – der Server sagt es auf Deutsch.
  await knopf.click();
  await expect(page.getByText("Genau diese Wörter übt dieser Plan schon in einer Position.")).toBeVisible();

  // Das Kind sieht die neue Tagespflicht auf seinem Pfad – seit B-686 ist der Knoten ein Link, der die Übung startet.
  await page.context().clearCookies();
  await page.evaluate(() => { localStorage.clear(); sessionStorage.clear(); });
  await sohnLogin(page, { id: String(childId), pin });
  await expect(page.getByRole("link", { name: /^Wiederholung: schwache Wörter/ })).toHaveAttribute("href", /^\/sohn\/practice\/\d+$/);
});
