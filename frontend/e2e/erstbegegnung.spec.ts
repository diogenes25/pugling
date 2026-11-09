import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin } from "./helpers";

/*
 * NP-24 „Erst lesen, dann prüfen" als Rollengang des Sohnes – mit einem FRISCHEN Plan, denn nur dort gibt es Wörter, die
 * er nie gesehen hat (Retro-Vorschlag CB vom 2026-11-08). Drei Wege in einer Fläche, weil sie ein Erlebnis sind:
 * - B-419: die erste Runde zeigt jedes neue Wort mit Lösung, das Ende sagt „3 neue Wörter kennengelernt" und bietet die
 *   Abfrage an; erst die zweite Runde fragt mit Antwortfeld.
 * - B-420: eine falsche Antwort hält die Karte an (eigene Antwort + Lösung), Enter trägt weiter.
 * - B-421: „Stadt" für „die Stadt" zählt – der Artikel darf fehlen.
 *
 * Der Stoff entsteht über die API (Gegenstand ist das Spielen, nicht das Anlegen) – mit Stufe 4 (Freitext), genau der
 * Stufe, die der Assistent für das Ziel „Klassenarbeit" ab Tag 1 schreibt (`VaterWizard.tsx`). Serielle Reihenfolge,
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

test("Sohn lernt neue Wörter erst kennen, wird dann abgefragt und liest seine Fehler", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const childPin = "8642";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `Neuling ${RUN}`, pin: childPin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Erstbegegnung-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Erstbegegnung-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Unité 2", orderIndex: 1 });
  const exerciseId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/vocabulary`, headers, {
    title: `En ville ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "fr", targetLang: "de", items: WORDS.map(([front, back]) => ({ front, back })) },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers,
    { childId, title: `Klassenarbeit ${RUN}`, durationDays: 7, active: true });
  await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, headers,
    { exerciseId, stage: 4, cadence: "Daily", useLeitner: true, orderStrategy: "Serial" });

  await sohnLogin(page, { id: String(childId), pin: childPin });
  await page.getByRole("link", { name: /ÜBEN/ }).click();
  const counter = page.locator(".pill.cyan", { hasText: /Karte \d+ \/ \d+/ });

  // ---------- B-419: Runde 1 zeigt, statt zu fragen ----------
  for (const [i, [front, back]] of WORDS.entries()) {
    await expect(counter).toHaveText(`Karte ${i + 1} / 3`);
    await expect(page.getByText(front, { exact: true })).toBeVisible();
    await expect(page.getByText(`→ ${back}`)).toBeVisible();
    await expect(page.getByRole("textbox")).toHaveCount(0);
    await page.getByRole("button", { name: "Weiter →" }).click();
  }
  await expect(page.getByText("3 neue Wörter kennengelernt")).toBeVisible();
  await page.getByRole("button", { name: /Jetzt abfragen/ }).click();

  // ---------- Runde 2 fragt ab ----------
  await expect(counter).toHaveText("Karte 1 / 3");
  await expect(page.getByText(WORDS[0][0], { exact: true })).toBeVisible();
  await expect(page.getByText(`→ ${WORDS[0][1]}`)).toHaveCount(0);
  const answer = page.getByRole("textbox", { name: "Deine Antwort" });
  // B-421 (NP-25): ohne Artikel getippt – das Wort zählt, die Karte rückt weiter statt anzuhalten.
  await answer.fill("Stadt");
  await answer.press("Enter");

  // ---------- B-420: ein Fehler hält die Karte an ----------
  await expect(counter).toHaveText("Karte 2 / 3");
  await answer.fill("die Strasse");
  await answer.press("Enter");
  // B-439: in der angehaltenen Karte, neben der eigenen Antwort – nicht in der 1×1-px-Ansage-Region (sr-only),
  // die Playwright ebenfalls als sichtbar zählt.
  const held = page.locator(".card.mistake");
  await expect(held.getByText("Lösung: die Straße", { exact: true })).toBeVisible();
  await expect(held.locator("s", { hasText: "die Strasse" })).toBeVisible();
  // Die nächste Karte kommt NICHT von selbst – auch nach der Zeit, die der alte Toast stand (1,6 s).
  await page.waitForTimeout(2000);
  await expect(counter).toHaveText("Karte 2 / 3");
  // Der Fokus steht auf „Weiter": Enter trägt weiter, ohne dass das Kind zielen muss.
  await expect(page.getByRole("button", { name: "Weiter →" })).toBeFocused();
  await page.keyboard.press("Enter");
  await expect(counter).toHaveText("Karte 3 / 3");
  // Und zurück ins Antwortfeld: das Kind tippt weiter, ohne erst hineinzuklicken (Review-Fund).
  await expect(answer).toBeFocused();
  await answer.fill(WORDS[2][1]);
  await answer.press("Enter");
  await expect(page.getByText("RUNDE FERTIG!")).toBeVisible();
});
