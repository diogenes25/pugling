import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin } from "./helpers";

/*
 * B-614, Rollengang Sohn: die Startseite gliedert nach dem Fach des Plans, nicht nach dem Fach des Lehrwerks. Ein
 * Englisch-Plan trägt eine Übung aus einer Mathe-Reihe – beide Aufgaben stehen unter „Englisch“, eine Mathe-Karte gibt
 * es nicht. Rot, solange der Server das Fach der Reihe meldet (dann zwei Fach-Karten).
 */

const RUN = Date.now().toString(36).slice(-5);
const WORDS = [["dog", "Hund"], ["cat", "Katze"]] as const;

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown, key = "id") {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json())[key] as number;
}

test("Eine fachfremde Übung im Englisch-Plan steht unter Englisch", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const pin = "8614";
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Plan-Fach ${RUN}`, pin });

  const uebung = async (subjectId: number, fach: string) => {
    const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
      name: `${fach}-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
      sourceLanguage: null, targetLanguage: null, notes: null,
    });
    const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
    const unitId = await postId(request, units, vater, { label: `Unit ${RUN}`, orderIndex: 1 });
    return postId(request, `${units}/${unitId}/vocabulary`, vater, {
      title: `${fach}-Wörter ${RUN}`, orderIndex: 1, rewardPoints: 10,
      config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: WORDS.map(([front, back]) => ({ front, back })) },
    });
  };
  const englisch = await postId(request, "/api/v1/creator/subjects", vater, { name: `Englisch ${RUN}` });
  const mathe = await postId(request, "/api/v1/creator/subjects", vater, { name: `Mathe ${RUN}` });
  const eigene = await uebung(englisch, "Englisch");
  const fremde = await uebung(mathe, "Mathe");

  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater,
    { childId, subjectId: englisch, title: `Englisch-Plan ${RUN}`, durationDays: 7, active: true });
  for (const exerciseId of [eigene, fremde])
    await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, vater,
      { exerciseId, stage: 4, cadence: "Daily", useLeitner: false });

  await sohnLogin(page, { id: String(childId), pin });
  const karten = page.locator(".fach-karte");
  await expect(karten.locator(".fach-name")).toHaveText([`Englisch ${RUN}`]);
  await expect(karten.first()).toContainText(`Englisch-Wörter ${RUN}`);
  await expect(karten.first()).toContainText(`Mathe-Wörter ${RUN}`);
  await expect(page.getByText(`Mathe ${RUN}`, { exact: true })).toHaveCount(0);
});
