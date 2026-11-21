import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin } from "./helpers";

/*
 * B-608, Rollengang Sohn: die Bonus-Runde verspricht nie mehr, als eine Karte zahlt. Ein Lückentext auf „Buchstaben
 * ordnen“, dessen zweite Lücke („aren't I“) auf Kästchen zurückfällt: die erste Karte ist eine Auswahl (1 🪙), die zweite
 * wird geschrieben (3 🪙). Die Übersicht sagt darum „bis zu 3“, der Hinweis über jeder Karte ihren eigenen Satz. Rot,
 * solange Übersicht und Hinweis den Satz der Runde nennen (dann „3 🪙“ ohne „bis zu“ und auf beiden Karten).
 *
 * Eigenes Kind: je Kind und Fach ist genau ein aktiver Plan spielbar.
 */

const RUN = Date.now().toString(36).slice(-5);
const WORDS = [["dog", "Hund"], ["cat", "Katze"]] as const;

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown, key = "id") {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json())[key] as number;
}

test("Übersicht sagt „bis zu“, jede Karte nennt ihren eigenen Satz", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const pin = "8608";
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Satz je Karte ${RUN}`, pin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `Satz-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
    name: `Satz-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: `Unit ${RUN}`, orderIndex: 1 });
  const exVokabeln = await postId(request, `${units}/${unitId}/vocabulary`, vater, {
    title: `Pflicht ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: WORDS.map(([front, back]) => ({ front, back })) },
  });
  const LUECKEN = `Lücken ${RUN}`;
  await postId(request, `${units}/${unitId}/cloze`, vater, {
    title: LUECKEN, orderIndex: 2, rewardPoints: 10, defaultStage: 7, // LetterScramble
    config: { text: "Satz 1: {{1}}. Satz 2: {{2}}.", gaps: [{ index: 1, answer: "tired" }, { index: 2, answer: "aren't I" }] },
  });

  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater,
    { childId, subjectId, title: `Satz-Plan ${RUN}`, durationDays: 7, active: true });
  const positionId = await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, vater,
    { exerciseId: exVokabeln, stage: 4, cadence: "Daily", useLeitner: false });

  // Die Pflicht über die API erfüllen, damit das Bonus-Training offen ist.
  const kindLogin = await request.post("/api/v1/auth/child", { data: { childId, pin } });
  expect(kindLogin.ok(), await kindLogin.text()).toBeTruthy();
  const kind = { Authorization: `Bearer ${(await kindLogin.json()).token}` };
  const tests = `/api/v1/student/study-plans/${planId}/positions/${positionId}/tests`;
  const attemptId = await postId(request, tests, kind, {}, "attemptId");
  const submit = await request.post(`${tests}/${attemptId}/submit`, {
    headers: kind, data: { answers: WORDS.map(([front], itemIndex) => ({ itemIndex, givenAnswer: front })) },
  });
  expect(submit.ok(), await submit.text()).toBeTruthy();

  await sohnLogin(page, { id: String(childId), pin });
  await page.getByRole("link", { name: "Zum Bonus-Training" }).click();
  const zeile = page.locator(".bonus-zeile", { hasText: LUECKEN });
  await expect(zeile).toContainText("Tippen · bis zu 3 🪙 je Treffer");

  await zeile.getByRole("link", { name: `Spielen: ${LUECKEN}` }).click();
  // Karte 1: Buchstaben ordnen – eine Auswahl, sie zahlt 1.
  await expect(page.getByText("Jede richtige Antwort bringt 1 🪙 – ausgewertet wird am Ende.")).toBeVisible();
  const kacheln = page.getByRole("group", { name: "Buchstaben zum Ordnen" });
  for (const ch of "tired")
    await kacheln.locator(`button[aria-label="Buchstabe ${ch}"][aria-disabled="false"]`).first().click();
  await page.getByRole("button", { name: /Weiter/ }).click();
  // Karte 2: fällt auf Kästchen zurück – geschrieben, sie zahlt 3.
  await expect(page.getByText("Jede richtige Antwort bringt 3 🪙 – ausgewertet wird am Ende.")).toBeVisible();
});
