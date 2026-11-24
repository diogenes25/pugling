import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin, tippen } from "./helpers";

/*
 * Mischrunde (B-764, B-765, B-766) – der Rollengang als Sohn: fällige Karten aus zwei Plänen in einer Runde, hinter der
 * Sperre des Bonus-Trainings. Was eine Karte zahlt und welcher Kasten sich bewegt, prüfen `MischrundeWertungTests`; hier
 * liegt, was nur die laufende App zeigt: der Einstieg neben den Bonus-Übungen, die Runde mit Karten aus zwei Fächern und
 * das Ergebnis mit Tagesdeckel und Grund. Die Wörter sind heute gelernt – darum zahlt die Runde nichts und sagt warum.
 *
 * Eigenes Kind, je Plan ein eigenes Fach: je Kind und Fach ist genau ein aktiver Plan spielbar.
 */

const RUN = Date.now().toString(36).slice(-5);
const PLAENE = [
  { fach: "Englisch", woerter: [["dog", "Hund"], ["cat", "Katze"]] },
  { fach: "Französisch", woerter: [["maison", "Haus"], ["arbre", "Baum"]] },
] as const;

type Kopf = Record<string, string>;

async function postId(request: APIRequestContext, url: string, headers: Kopf, data: unknown, key = "id") {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json())[key] as number;
}

// Ein Plan mit einer Leitner-Vokabelposition und Tagespflicht in einem eigenen Fach.
async function planAnlegen(request: APIRequestContext, vater: Kopf, childId: number, fach: string, woerter: readonly (readonly [string, string])[]) {
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `${fach} ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
    name: `Misch-Reihe ${fach} ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: `Unit ${RUN}`, orderIndex: 1 });
  const exerciseId = await postId(request, `${units}/${unitId}/vocabulary`, vater, {
    title: `Wörter ${fach} ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: woerter.map(([front, back]) => ({ front, back })) },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater, { childId, subjectId, title: `${fach}-Plan ${RUN}`, durationDays: 7, active: true });
  const positionId = await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, vater,
    { exerciseId, stage: 4, cadence: "Daily", useLeitner: true });
  return { planId, positionId };
}

// Das Kind lernt die Wörter wie im Spiel – jede neue Karte als Lernkarte, das Fremdwort aus dem Gedächtnis – und besteht
// danach den Test: die Pflicht ist erfüllt, die Wörter sind eingeführt, eingeprägt und heute fällig.
async function lernenUndTesten(request: APIRequestContext, kind: Kopf, planId: number, positionId: number, woerter: readonly (readonly [string, string])[]) {
  const prac = `/api/v1/student/study-plans/${planId}/positions/${positionId}/practice-sessions`;
  const sessionId = await postId(request, prac, kind, {});
  for (let i = 0; i < 10; i++) {
    const next = await (await request.get(`${prac}/${sessionId}/next`, { headers: kind })).json();
    if (next.done) break;
    const [front] = woerter[next.card.itemIndex];
    const res = await request.post(`${prac}/${sessionId}/review`, { headers: kind, data: { itemIndex: next.card.itemIndex, givenAnswer: front } });
    expect(res.ok(), await res.text()).toBeTruthy();
  }
  const tests = `/api/v1/student/study-plans/${planId}/positions/${positionId}/tests`;
  const attemptId = await postId(request, tests, kind, {}, "attemptId");
  const submit = await request.post(`${tests}/${attemptId}/submit`, {
    headers: kind, data: { answers: woerter.map(([, back], itemIndex) => ({ itemIndex, givenAnswer: back })) },
  });
  expect(submit.ok(), await submit.text()).toBeTruthy();
  expect((await submit.json()).passed).toBe(true);
}

test("Die Mischrunde fragt fällige Karten aus zwei Plänen und nennt am Ende Tagesdeckel und Grund", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };
  const pin = "8654";
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Misch ${RUN}`, pin });
  const plaene = [];
  for (const p of PLAENE) plaene.push(await planAnlegen(request, vater, childId, p.fach, p.woerter));

  const kindLogin = await request.post("/api/v1/auth/child", { data: { childId, pin } });
  expect(kindLogin.ok(), await kindLogin.text()).toBeTruthy();
  const kind = { Authorization: `Bearer ${(await kindLogin.json()).token}` };
  for (const [i, p] of PLAENE.entries()) await lernenUndTesten(request, kind, plaene[i].planId, plaene[i].positionId, p.woerter);

  await sohnLogin(page, { id: String(childId), pin });
  await page.getByRole("link", { name: "Zum Bonus-Training" }).click();
  await expect(page.getByRole("heading", { name: "Bonus-Training" })).toBeVisible();
  const einstieg = page.locator(".mischrunde-einstieg");
  await expect(einstieg).toContainText("4 fällige Karten aus all deinen Plänen – freiwillig, ohne Malus.");
  await einstieg.getByRole("button", { name: "Mischrunde starten" }).click();

  // Vier Karten, je eine Frage: das deutsche Wort steht da, geschrieben wird das Fremdwort – oder umgekehrt.
  const alle = PLAENE.flatMap((p) => p.woerter);
  const faecher = new Set<string>();
  for (let n = 1; n <= 4; n++) {
    await expect(page.getByText(`Karte ${n} / 4`)).toBeVisible();
    // Die Karte nennt ihr Fach – über die Runde kommen beide vor.
    const seite = await page.locator("body").innerText();
    for (const p of PLAENE) if (seite.includes(`${p.fach} ${RUN} ·`)) faecher.add(p.fach);
    const text = await page.locator(".fcard").first().innerText();
    const wort = alle.find(([front, back]) => text.includes(back) || text.includes(front))!;
    const antwort = text.includes(wort[1]) ? wort[0] : wort[1];
    await tippen(page.getByRole("textbox", { name: "Lösung" }), antwort);
    await page.getByRole("button", { name: "Weiter →" }).click();
  }

  expect([...faecher].sort()).toEqual(["Englisch", "Französisch"]);
  await expect(page.getByText("4 / 4 richtig")).toBeVisible();
  await expect(page.getByText("Ohne Münzen: diese Wörter hattest du heute schon gesehen – sie rücken trotzdem in ihrem Kasten vor.")).toBeVisible();
  await expect(page.getByText("Heute verdient: 0 / 90 🪙")).toBeVisible();
  await expect(page.getByText("Die Mischrunde ist freiwillig – verpasst du sie, kostet das nichts.")).toBeVisible();

  // Zurück im Bonus-Training ist nichts mehr fällig: der Einstieg ist weg.
  await page.getByRole("button", { name: "Zurück zum Bonus-Training" }).click();
  await expect(page.getByRole("heading", { name: "Bonus-Training" })).toBeVisible();
  await expect(page.locator(".mischrunde-einstieg")).toHaveCount(0);
});
