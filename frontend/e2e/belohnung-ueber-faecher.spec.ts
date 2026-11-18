import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin } from "./helpers";

/*
 * B-611/B-612, Rollengang Sohn: mit zwei Fächern sagen Truhe, Bonus und Startseite dasselbe. Französisch hat ein
 * Tagesziel, Mathe nur ein Wochenziel. Ist das Tagesziel geschafft, ist die Truhe verdient und das Bonus-Training offen –
 * das offene Wochenziel im anderen Fach sperrt sie nicht mehr (vorher wählte der Server die Belohnungsziele je Plan, und
 * der Mathe-Plan ohne Tagesziel gab sein Wochenziel ab). Balken und Truhen-Text kommen aus den Feldern des Servers.
 *
 * Eigenes Kind: je Kind und Fach ist genau ein aktiver Plan spielbar – zwei Fächer, zwei aktive Pläne.
 */

const RUN = Date.now().toString(36).slice(-5);
const WORDS = [["chien", "Hund"], ["chat", "Katze"], ["maison", "Haus"]] as const;

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown, key = "id") {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json())[key] as number;
}

test("Tagesziel in Französisch geschafft, Wochenziel in Mathe offen: Truhe verdient, Bonus offen", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const pin = "8655";
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Zwei Fächer ${RUN}`, pin });

  // Je Fach eine Reihe mit einer Unit und einer Vokabelübung, je Fach ein aktiver Plan mit einer Position.
  const fachPlan = async (fach: string, cadence: "Daily" | "Weekly") => {
    const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `${fach} ${RUN}` });
    const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
      name: `${fach}-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
      sourceLanguage: null, targetLanguage: null, notes: null,
    });
    const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
    const unitId = await postId(request, units, vater, { label: `Unit ${RUN}`, orderIndex: 1 });
    const exerciseId = await postId(request, `${units}/${unitId}/vocabulary`, vater, {
      title: `${fach}-Wörter ${RUN}`, orderIndex: 1, rewardPoints: 10,
      config: { direction: "front-to-back", sourceLang: "fr", targetLang: "de", items: WORDS.map(([front, back]) => ({ front, back })) },
    });
    const planId = await postId(request, "/api/v1/supervisor/study-plans", vater,
      { childId, subjectId, title: `${fach}-Plan ${RUN}`, durationDays: 7, active: true });
    const positionId = await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, vater,
      { exerciseId, stage: 4, cadence, useLeitner: false });
    return { planId, positionId };
  };
  const franz = await fachPlan("Französisch", "Daily");
  await fachPlan("Mathe", "Weekly");

  await sohnLogin(page, { id: String(childId), pin });
  await expect(page.getByText("Schaffe deine Tagesziele, dann öffnet sich die Truhe.")).toBeVisible();
  await expect(page.getByText("🔒 Erst die Tagesziele – dann öffnet das Bonus-Training.")).toBeVisible();

  // Nur das Französisch-Tagesziel erfüllen: ein bestandener Test. Das Mathe-Wochenziel bleibt offen.
  const kindLogin = await request.post("/api/v1/auth/child", { data: { childId, pin } });
  expect(kindLogin.ok(), await kindLogin.text()).toBeTruthy();
  const kind = { Authorization: `Bearer ${(await kindLogin.json()).token}` };
  const tests = `/api/v1/student/study-plans/${franz.planId}/positions/${franz.positionId}/tests`;
  const attemptId = await postId(request, tests, kind, {}, "attemptId");
  const submit = await request.post(`${tests}/${attemptId}/submit`, {
    // B-597: Freitext fragt das Fremdwort ab, die Vorderseite.
    headers: kind, data: { answers: WORDS.map(([front], itemIndex) => ({ itemIndex, givenAnswer: front })) },
  });
  expect(submit.ok(), await submit.text()).toBeTruthy();

  const today = await (await request.get(`/api/v1/student/children/${childId}/today`, { headers: kind })).json();
  expect(today.dutyDone).toBe(false);
  expect(today.rewardsUnlocked).toBe(true);
  expect([today.rewardCadence, today.rewardGoalsMet, today.rewardGoalsTotal]).toEqual(["Daily", 1, 1]);
  expect(today.dailyBox.claimedToday).toBe(true);

  await page.reload();
  // Die Truhe wartet und der Bonus ist offen – kein „erst die Tagesziele“ mehr, obwohl Mathe offen ist.
  await expect(page.getByRole("button", { name: "Truhe öffnen" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Zum Bonus-Training" })).toBeVisible();
  await expect(page.getByText("Schaffe deine Tagesziele, dann öffnet sich die Truhe.")).toHaveCount(0);
  // Der Balken zählt die Belohnungsziele (1/1) – rot, wenn er alle Pflichtziele mischt (dann 50 %); dass er sie vom
  // Server statt aus den Positionen nimmt, hält der Vitest in tagessichtWeg.test.tsx. Die Zeilen zählen je Takt.
  await expect(page.locator(".bar.lime i").first()).toHaveAttribute("style", /width: 100%/);
  await expect(page.locator(".row", { hasText: "🎯 Ziele heute" })).toContainText("1 / 1");
  await expect(page.locator(".row", { hasText: "📅 Ziele diese Woche" })).toContainText("0 / 1");
});
