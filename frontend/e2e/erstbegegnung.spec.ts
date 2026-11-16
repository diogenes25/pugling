import { test, expect, type APIRequestContext, type Page } from "@playwright/test";
import { lernkarteDurchspielen, sohnLogin, pfadStart } from "./helpers";

/*
 * NP-24 „Erst lesen, dann prüfen" als Rollengang des Sohnes – mit einem FRISCHEN Plan, denn nur dort gibt es Wörter, die
 * er nie gesehen hat (Retro-Vorschlag CB vom 2026-11-08). Drei Wege in einer Fläche, weil sie ein Erlebnis sind:
 * - B-419/B-598: die erste Runde lernt jedes neue Wort auf der Lernkarte, das Ende sagt „3 neue Wörter kennengelernt" und bietet die
 *   Abfrage an.
 * - B-476: mit Leitner fragt die zweite Runde als Karteikarte – umdrehen, „Gewusst!"/„Nochmal" –, nicht mit Antwortfeld;
 *   getippt wird erst ab dem dritten Fach.
 * - B-420: eine falsche Antwort hält die Karte an (eigene Antwort + Lösung), Enter trägt weiter.
 * - B-421: „Stadt" für „die Stadt" zählt – der Artikel darf fehlen.
 *   Beide brauchen eine getippte Karte gleich nach dem Kennenlernen; die gibt es nur ohne Leitner (keine Leiter, B-476).
 *
 * Der Stoff entsteht über die API (Gegenstand ist das Spielen, nicht das Anlegen) – mit Stufe 4 (Freitext), der Stufe,
 * die der Assistent beim Ziel „Klassenarbeit" nach dem ersten Drittel schreibt (davor Selbsteinschätzung, B-423). Serielle Reihenfolge,
 * damit jeder Kartenübergang vorhersagbar ist. Eigenes Kind: je Kind ist genau ein aktiver Plan spielbar.
 */

const RUN = Date.now().toString(36).slice(-5);
const WORDS: [front: string, back: string][] = [
  [`la ville ${RUN}`, "die Stadt"],
  [`la rue ${RUN}`, "die Straße"],
  [`le magasin ${RUN}`, "das Geschäft"],
];

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

async function setUp(request: APIRequestContext, page: Page, tag: string, useLeitner: boolean) {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const childPin = "8642";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `Neuling ${tag} ${RUN}`, pin: childPin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Erstbegegnung-Fach ${tag} ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Erstbegegnung-Reihe ${tag} ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Unité 2", orderIndex: 1 });
  const exerciseId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/vocabulary`, headers, {
    title: `En ville ${tag} ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "fr", targetLang: "de", items: WORDS.map(([front, back]) => ({ front, back })) },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers,
    { childId, title: `Klassenarbeit ${tag} ${RUN}`, durationDays: 7, active: true });
  await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, headers,
    { exerciseId, stage: 4, cadence: "Daily", useLeitner, orderStrategy: "Serial" });

  await sohnLogin(page, { id: String(childId), pin: childPin });
  await pfadStart(page, "practice");
  const counter = page.locator(".pill.cyan", { hasText: /Karte \d+ \/ \d+/ });

  // ---------- B-419/B-598: Runde 1 lernt jedes neue Wort auf der Lernkarte, statt es abzufragen ----------
  for (const [i, [front, back]] of WORDS.entries()) {
    await expect(counter).toHaveText(`Karte ${i + 1} / 3`);
    // Deutsch steht da, das Fremdwort erst nach dem Aufdecken.
    await expect(page.getByText(back, { exact: true })).toBeVisible();
    await expect(page.getByText(`→ ${front}`)).toHaveCount(0);
    await expect(page.getByRole("textbox")).toHaveCount(0);
    expect(await lernkarteDurchspielen(page)).toBe(front);
  }
  await expect(page.getByText("3 neue Wörter kennengelernt")).toBeVisible();
  await page.getByRole("button", { name: /Jetzt abfragen/ }).click();
  await expect(counter).toHaveText("Karte 1 / 3");
  return counter;
}

test("Sohn lernt neue Wörter erst kennen und dreht sie dann als Karteikarte um", async ({ page, request }) => {
  const counter = await setUp(request, page, "L", true);

  // ---------- B-476: Runde 2 ist die Karteikarte ----------
  for (const [i, [front, back]] of WORDS.entries()) {
    await expect(counter).toHaveText(`Karte ${i + 1} / 3`);
    await expect(page.getByText(front, { exact: true })).toBeVisible();
    // Kein Antwortfeld und keine Lösung, bevor das Kind umdreht.
    await expect(page.getByRole("textbox")).toHaveCount(0);
    await expect(page.getByText(`→ ${back}`)).toHaveCount(0);
    await page.getByRole("button", { name: /Aufdecken/ }).click();
    await expect(page.getByText(`→ ${back}`)).toBeVisible();
    // Das zweite Wort weiß er nicht – „Nochmal" ist eine Antwort wie „Gewusst!".
    await page.getByRole("button", { name: i === 1 ? "Nochmal" : "Gewusst!" }).click();
  }
  await expect(page.getByText("RUNDE FERTIG!")).toBeVisible();
});

test("Ohne Leitner tippt der Sohn gleich nach dem Kennenlernen und liest seine Fehler", async ({ page, request }) => {
  const counter = await setUp(request, page, "T", false);

  // ---------- Runde 2 fragt ab ----------
  // B-597: Freitext zeigt das deutsche Wort und fragt das Fremdwort ab.
  await expect(page.getByText(WORDS[0][1], { exact: true })).toBeVisible();
  await expect(page.getByText(`→ ${WORDS[0][0]}`)).toHaveCount(0);
  const answer = page.getByRole("textbox", { name: "Deine Antwort" });
  // B-421 (NP-25): ohne Artikel getippt – das Wort zählt. Seit B-526 zeigt das grüne Blatt den Treffer, bis „Weiter“.
  await answer.fill(WORDS[0][0].replace(/^la /, ""));
  await answer.press("Enter");
  await expect(page.locator(".lektion-blatt.richtig")).toBeVisible();
  await expect(page.getByRole("button", { name: "Weiter →" })).toBeFocused();
  await page.keyboard.press("Enter");

  // ---------- B-420: ein Fehler hält die Karte an ----------
  await expect(counter).toHaveText("Karte 2 / 3");
  await answer.fill("la ru");
  await answer.press("Enter");
  // B-439: im roten Blatt, neben der eigenen Antwort – nicht in der 1×1-px-Ansage-Region (sr-only), die Playwright
  // ebenfalls als sichtbar zählt.
  const held = page.locator(".lektion-blatt.falsch");
  await expect(held.getByText(`Lösung: ${WORDS[1][0]}`, { exact: true })).toBeVisible();
  await expect(held.locator("s", { hasText: "la ru" })).toBeVisible();
  // Die nächste Karte kommt NICHT von selbst – auch nach der Zeit, die der alte Toast stand (1,6 s).
  await page.waitForTimeout(2000);
  await expect(counter).toHaveText("Karte 2 / 3");
  // Der Fokus steht auf „Weiter": Enter trägt weiter, ohne dass das Kind zielen muss.
  await expect(page.getByRole("button", { name: "Weiter →" })).toBeFocused();
  await page.keyboard.press("Enter");
  await expect(counter).toHaveText("Karte 3 / 3");
  // Und zurück ins Antwortfeld: das Kind tippt weiter, ohne erst hineinzuklicken (Review-Fund).
  await expect(answer).toBeFocused();
  await answer.fill(WORDS[2][0]);
  await answer.press("Enter");
  await expect(page.locator(".lektion-blatt.richtig")).toBeVisible();
  await page.keyboard.press("Enter");
  await expect(page.getByText("RUNDE FERTIG!")).toBeVisible();
  // B-526: zwei von drei getippten Antworten stimmten – die Genauigkeit zählt, was der Server geprüft hat.
  await expect(page.getByLabel("Genauigkeit 67 %")).toBeVisible();
});
