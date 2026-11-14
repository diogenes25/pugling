import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin } from "./helpers";

/*
 * B-535: Kiste und Bonus-Training hängen nur an den Tageszielen. Ein offenes Wochenziel (die schwere Grammatik der
 * Woche) sperrt sie nicht mehr. Die Regel hält der Server (`BelohnungenTagespflichtTests`); hier liegt, was nur die
 * laufende App zeigt: die Arcade liest `rewardsUnlocked`, statt die Sperre selbst aus `dutyDone` nachzurechnen –
 * vor dem Fix blieb der Einstieg auf dem Pfad gesperrt, obwohl der Server das Training schon freigab.
 *
 * Eigenes Kind: je Kind ist genau ein aktiver Plan spielbar.
 */

const RUN = Date.now().toString(36).slice(-5);
const WORDS = [["dog", "Hund"], ["cat", "Katze"], ["house", "Haus"], ["tree", "Baum"]] as const;

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown, key = "id") {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json())[key] as number;
}

test("Tagesziel geschafft, Wochenziel offen: Kiste gebucht und Bonus-Training offen", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const pin = "8654";
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Kiste ${RUN}`, pin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `Kisten-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
    name: `Kisten-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: `Unit ${RUN}`, orderIndex: 1 });
  const vokabeln = (title: string, orderIndex: number) => postId(request, `${units}/${unitId}/vocabulary`, vater, {
    title, orderIndex, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: WORDS.map(([front, back]) => ({ front, back })) },
  });
  const exTag = await vokabeln(`Tiere ${RUN}`, 1);
  const exWoche = await vokabeln(`Grammatik ${RUN}`, 2);

  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater, { childId, title: `Kisten-Plan ${RUN}`, durationDays: 7, active: true });
  const positions = `/api/v1/supervisor/study-plans/${planId}/positions`;
  const tagId = await postId(request, positions, vater, { exerciseId: exTag, stage: 4, cadence: "Daily", useLeitner: false });
  await postId(request, positions, vater, { exerciseId: exWoche, stage: 4, cadence: "Weekly", useLeitner: false });

  await sohnLogin(page, { id: String(childId), pin });
  await expect(page.getByText("🔒 Erst die Tagespflicht – dann öffnet das Bonus-Training.")).toBeVisible();

  // Nur das Tagesziel erfüllen: ein bestandener Test der Tages-Position. Das Wochenziel bleibt offen.
  const kindLogin = await request.post("/api/v1/auth/child", { data: { childId, pin } });
  expect(kindLogin.ok(), await kindLogin.text()).toBeTruthy();
  const kind = { Authorization: `Bearer ${(await kindLogin.json()).token}` };
  const tests = `/api/v1/student/study-plans/${planId}/positions/${tagId}/tests`;
  const attemptId = await postId(request, tests, kind, {}, "attemptId");
  const submit = await request.post(`${tests}/${attemptId}/submit`, {
    headers: kind, data: { answers: WORDS.map(([, back], itemIndex) => ({ itemIndex, givenAnswer: back })) },
  });
  expect(submit.ok(), await submit.text()).toBeTruthy();

  const overview = await (await request.get(`/api/v1/student/study-plans/${planId}/overview`, { headers: kind })).json();
  expect(overview.today.dutyDone).toBe(false);
  expect(overview.today.rewardsUnlocked).toBe(true);
  expect(overview.dailyBox.claimedToday).toBe(true);

  await page.reload();
  await expect(page.getByRole("link", { name: "Zum Bonus-Training" })).toBeVisible();
  await expect(page.getByText("🔒 Erst die Tagespflicht – dann öffnet das Bonus-Training.")).toHaveCount(0);
  // Die Ziele-Karte zählt Tag und Woche getrennt: heute ist alles geschafft, die Woche läuft noch.
  await expect(page.locator(".row", { hasText: "🎯 Ziele heute" })).toContainText("1 / 1");
  await expect(page.locator(".row", { hasText: "📅 Ziele diese Woche" })).toContainText("0 / 1");
});
