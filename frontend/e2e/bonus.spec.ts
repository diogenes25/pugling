import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin } from "./helpers";

/*
 * Bonus-Training – Extra-Übungen zum aktuellen Thema, offen erst nach der Tagespflicht. Die Regeln (Form je Durchgang,
 * Münzen nur für geprüfte Treffer, Tagesdeckel, Einmal-Übung) hält der Server und prüft `ExtraTrainingTests`; hier
 * liegt, was nur die laufende App zeigt: die Sperre auf dem Pfad, das Öffnen nach erfüllter Pflicht, eine getippte
 * Einmal-Übung, die Münzen bringt und danach abgehakt ist, und eine Vokabel-Runde zum Aufwärmen ohne Münzen.
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

test("Das Bonus-Training öffnet nach der Tagespflicht, zahlt eine getippte Übung einmal und wärmt Vokabeln ohne Münzen auf", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const pin = "8653";
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Bonus ${RUN}`, pin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `Bonus-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
    name: `Bonus-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: `Unit ${RUN}`, orderIndex: 1 });
  const VOKABELN = `Tiere ${RUN}`, RECHNEN = `Rechnen ${RUN}`;
  const exVokabeln = await postId(request, `${units}/${unitId}/vocabulary`, vater, {
    title: VOKABELN, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: WORDS.map(([front, back]) => ({ front, back })) },
  });
  await postId(request, `${units}/${unitId}/arithmetic`, vater, {
    title: RECHNEN, orderIndex: 2, rewardPoints: 10,
    config: { problems: [{ prompt: "2 + 3", answer: 5, tolerance: 0 }, { prompt: "4 × 4", answer: 16, tolerance: 0 }] },
  });

  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater, { childId, title: `Bonus-Plan ${RUN}`, durationDays: 7, active: true });
  const positionId = await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, vater,
    { exerciseId: exVokabeln, stage: 4, cadence: "Daily", useLeitner: false });

  // Vor der Pflicht: der Einstieg steht gesperrt unter dem Pfad.
  await sohnLogin(page, { id: String(childId), pin });
  await expect(page.getByText("🔒 Erst die Tagesziele – dann öffnet das Bonus-Training.")).toBeVisible();
  await expect(page.getByRole("link", { name: "Zum Bonus-Training" })).toHaveCount(0);

  // Die Pflicht über die API erfüllen: ein bestandener Test der Position.
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
  await page.getByRole("link", { name: "Zum Bonus-Training" }).click();
  await expect(page.getByRole("heading", { name: "Bonus-Training" })).toBeVisible();
  const zeile = (titel: string) => page.locator(".bonus-zeile", { hasText: titel });
  await expect(zeile(VOKABELN)).toContainText("Karteikarte · Aufwärmen, ohne Münzen");
  await expect(zeile(RECHNEN)).toContainText("Tippen · 2 🪙 je Treffer");

  // Die Einmal-Übung: getippt, am Ende ausgewertet, danach abgehakt.
  await zeile(RECHNEN).getByRole("link", { name: `Spielen: ${RECHNEN}` }).click();
  for (let i = 0; i < 2; i++) {
    const frage = (await page.locator(".fcard").innerText()).includes("2 + 3") ? "5" : "16";
    await page.getByRole("textbox", { name: "Lösung" }).fill(frage);
    await page.getByRole("button", { name: "Weiter →" }).click();
  }
  await expect(page.getByText("2 / 2 richtig")).toBeVisible();
  await expect(page.getByText("+4 🪙 verdient")).toBeVisible();
  await page.getByRole("button", { name: "Zurück zum Bonus-Training" }).click();
  await expect(zeile(RECHNEN)).toContainText("✓ Geschafft");
  await expect(zeile(RECHNEN).getByRole("link")).toHaveCount(0);
  await expect(page.getByText("Heute verdient: 4 / 30 🪙")).toBeVisible();

  // Die Vokabeln: Aufwärmen per Karteikarte zahlt nichts, und die Runde zählt als gespielt.
  // Zwei Karten, dann „Später weiter“: die Runde setzt bei Karte 3 fort, statt von vorn zu beginnen.
  const karteWeiter = async () => {
    await page.getByRole("button", { name: "Aufdecken 🔄" }).click();
    await page.getByRole("button", { name: "Gewusst", exact: true }).click();
  };
  await zeile(VOKABELN).getByRole("link", { name: `Spielen: ${VOKABELN}` }).click();
  await karteWeiter();
  await karteWeiter();
  await expect(page.getByText("Karte 3 / 4")).toBeVisible();
  await page.getByRole("button", { name: "Später weiter" }).click();
  await zeile(VOKABELN).getByRole("link", { name: `Weiterspielen: ${VOKABELN}` }).click();
  await expect(page.getByText("Karte 3 / 4")).toBeVisible();
  await karteWeiter();
  await karteWeiter();
  await expect(page.getByText("4 / 4 richtig")).toBeVisible();
  await expect(page.getByText("Diesmal ohne Münzen.")).toBeVisible();
  await page.getByRole("button", { name: "Zurück zum Bonus-Training" }).click();
  await expect(zeile(VOKABELN)).toContainText("1× gespielt");
});
