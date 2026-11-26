import { test, expect, type APIRequestContext } from "@playwright/test";
import { vaterLogin } from "./helpers";
import { formatDate, heuteImLerntag } from "../src/lib/datum";

/*
 * B-733 (Rollengang Vater, AK 7): das Kind lernt heute zwei Wörter kennen und vertippt sich im Test; der Vater öffnet im Kind-Hub „Auswertung“ und
 * liest den heutigen Tag (live) mit der offenen Pflicht, gestern als Lücke – nicht als Nulltag – und das Problemwort mit der
 * getippten Antwort. Die Zählregeln abgeschlossener Tage und ihre Stabilität belegen die
 * Integrationstests (`TagesauswertungTests`); die API spielt nur heute.
 *
 * Rot, wenn die Seite nicht vom Server liest, einen Tag ohne Zeile weglässt oder als Null zeichnet, oder ein Problemwort
 * ohne seine getippte Antwort erscheint.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString(36).slice(-5);
const WOERTER = [[`dog${RUN}`, "Hund"], [`cat${RUN}`, "Katze"]] as const;

type Kopf = Record<string, string>;

async function postId(request: APIRequestContext, url: string, headers: Kopf, data: unknown, key = "id") {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), `${url}: ${res.status()} ${await res.text()}`).toBeTruthy();
  return (await res.json())[key] as number;
}

test("Der Vater liest Verlauf mit Lücke und das Problemwort mit der getippten Antwort", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const vater = { Authorization: `Bearer ${(await login.json()).token}` };

  const childPin = "7" + String(Date.now() % 1000).padStart(3, "0");
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name: `Auswertung ${RUN}`, pin: childPin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `Auswertungsfach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, {
    name: `Auswertungs-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const units = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const unitId = await postId(request, units, vater, { label: `Unit ${RUN}`, orderIndex: 1 });
  const exerciseId = await postId(request, `${units}/${unitId}/vocabulary`, vater, {
    title: `Wörter ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: WOERTER.map(([front, back]) => ({ front, back })) },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater,
    { childId, subjectId, title: `Plan ${RUN}`, durationDays: 7, active: true });
  const positionId = await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, vater,
    { exerciseId, stage: 4, cadence: "Daily", useLeitner: true });

  // Das Kind lernt die Wörter im Üben richtig kennen (Lernkarten tragen kein Urteil) und vertippt sich dann im Test beim
  // ersten Wort (der Test fragt das Fremdwort, B-597) – eine gewertete falsche Antwort mit Tippfehler; der Test fällt durch,
  // die Tagespflicht bleibt offen.
  const kindLogin = await request.post("/api/v1/auth/child", { data: { childId, pin: childPin } });
  expect(kindLogin.ok(), await kindLogin.text()).toBeTruthy();
  const kind = { Authorization: `Bearer ${(await kindLogin.json()).token}` };
  const prac = `/api/v1/student/study-plans/${planId}/positions/${positionId}/practice-sessions`;
  const sessionId = await postId(request, prac, kind, {});
  for (let i = 0; i < 10; i++) {
    const next = await (await request.get(`${prac}/${sessionId}/next`, { headers: kind })).json();
    if (next.done) break;
    const [front] = WOERTER[next.card.itemIndex];
    const res = await request.post(`${prac}/${sessionId}/review`, { headers: kind, data: { itemIndex: next.card.itemIndex, givenAnswer: front } });
    expect(res.ok(), await res.text()).toBeTruthy();
  }
  const tests = `/api/v1/student/study-plans/${planId}/positions/${positionId}/tests`;
  const attemptId = await postId(request, tests, kind, {}, "attemptId");
  const submit = await request.post(`${tests}/${attemptId}/submit`, {
    headers: kind, data: { answers: WOERTER.map(([front], itemIndex) => ({ itemIndex, givenAnswer: itemIndex === 0 ? `${front}x` : front })) },
  });
  expect(submit.ok(), await submit.text()).toBeTruthy();

  await vaterLogin(page, FATHER);
  await page.goto(`/vater/kind/${childId}`);
  await page.getByRole("link", { name: "🔎 Auswertung" }).click();
  await expect(page.getByRole("heading", { name: `Auswertung · Auswertung ${RUN}` })).toBeVisible();

  const verlauf = page.getByRole("table", { name: "Verlauf" });
  const heute = verlauf.getByRole("row").filter({ hasText: "(heute, läuft)" });
  await expect(heute).toContainText(`Auswertungsfach ${RUN}`);
  await expect(heute).toContainText("offen 1");
  // Gestern hat das Kind nicht geübt: eine Lücke, kein Nulltag – gesucht über das Datum, nie über die Position.
  const gestern = new Date(Date.parse(`${heuteImLerntag()}T00:00:00Z`) - 86_400_000).toISOString().slice(0, 10);
  await expect(verlauf.getByRole("row").filter({ hasText: formatDate(gestern) })).toContainText("Lücke – nicht geübt");

  const woerter = page.getByRole("table", { name: "Problemwörter" });
  const zeile = woerter.getByRole("row").filter({ hasText: WOERTER[0][0] });
  await expect(zeile).toContainText("1 / 0");
  await expect(zeile).toContainText(`„${WOERTER[0][0]}x“`);
  // Keine Art: am ersten Tag fragt der Test ein neues Wort als Auswahl ab, und eine Auswahl trägt nach B-732 keine Fehlerart
  // (B-1030, verworfen). Die Seite zeigt dann „–“ statt einer erfundenen Art. Arten entstehen erst an Wiederholungstagen, die
  // eine E2E nicht vorspulen kann – ihre Darstellung tragen `VaterAuswertung.test.tsx`, ihre Zählung `TagesauswertungTests`.
  await expect(zeile.getByRole("cell").nth(3)).toHaveText("–");
});
