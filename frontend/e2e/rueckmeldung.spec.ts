import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin, pfadStart, vaterLogin, tippen } from "./helpers";

/*
 * B-628/B-629 – die Rückmeldung zur Karte, einmal ganz herum (zugleich der Rollengang von Sprint 88): der Sohn meldet im
 * Üben, der Vater sieht und erledigt, der Ersteller sieht die Zählung, der Sohn bekommt einmal den Hinweis.
 *
 * Was jeder Schritt prüft – und welche Änderung ihn rot macht:
 * 1. An der Karte gibt es „Rückmeldung“, Gründe und Text gehen ab, danach „Danke, ist angekommen!“, die Karte bleibt →
 *    rot ohne Knopf, ohne Absenden oder wenn die Meldung die Karte wegnimmt.
 * 2. Die Vater-Seite „Rückmeldungen“ zeigt Karte, Grund und Text und setzt „erledigt“ → rot ohne Seite, Navigation oder PATCH.
 * 3. Am eigenen Lückentext steht die Zählung der Gründe → rot ohne Panel oder ohne Schreibrecht-Sicht.
 * 4. Der Sohn sieht den Hinweis einmal und quittiert ihn → rot ohne Hinweis oder wenn er nach „Alles klar“ wiederkommt.
 * 5. Während eines Tests trägt die Frage keinen Rückmelde-Knopf → rot, wenn er in die Fragenansicht wandert.
 * 5b. Den Test mit „Später weiter“ verlassen, im Üben derselben Position melden → „Danke, ist angekommen!“ (B-685 AK 1,
 *    der Weg des Sohnes) → rot, solange ein offener Test die Meldung mit `feedback_during_test` sperrt.
 * 6. Nach der letzten Antwort trägt das Ergebnisblatt den Knopf je Karte (B-685 AK 3) → rot, wenn `KartenMeldung` aus dem
 *    Ergebnisblatt fällt.
 *
 * Eigenes Kind: je Kind und Fach ist genau ein aktiver Plan spielbar.
 */

const RUN = Date.now().toString(36).slice(-5);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

test("Rückmeldung: Sohn meldet eine Karte, Vater erledigt, Ersteller zählt, Sohn sieht den Hinweis", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const childPin = "6531";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `Melder ${RUN}`, pin: childPin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Melde-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Melde-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Unit 1", orderIndex: 1 });
  const exerciseTitle = `Melde-Übung ${RUN}`;
  const exerciseId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/cloze`, headers, {
    title: exerciseTitle, orderIndex: 1, rewardPoints: 10,
    config: { text: "I travel {{1}}.", gaps: [{ index: 1, answer: "eastwards", prompt: `Richtung Osten ${RUN}` }] },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers,
    { childId, subjectId, title: `Melde-Plan ${RUN}`, durationDays: 7, active: true });
  await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, headers,
    { exerciseId, cadence: "Daily", useLeitner: false, stage: 3 });

  // ---- 1. Sohn: meldet die Karte mitten im Üben ----
  await sohnLogin(page, { id: String(childId), pin: childPin });
  await pfadStart(page, "practice");
  const feld = page.getByRole("textbox", { name: "Deine Antwort" });
  await expect(feld).toBeVisible();
  await page.getByRole("button", { name: /Rückmeldung/ }).click();
  await page.getByRole("button", { name: "Falsch übersetzt" }).click();
  await page.getByLabel("Willst du noch etwas dazu schreiben?").fill(`heißt anders ${RUN}`);
  await page.getByRole("button", { name: "Absenden" }).click();
  await expect(page.getByText("Danke, ist angekommen!")).toBeVisible();
  // Die Karte bleibt stehen: die Meldung ist keine Antwort.
  await expect(feld).toBeVisible();

  // ---- 2. Vater: sieht die Meldung und erledigt sie ----
  await vaterLogin(page, { id: "1", pin: "0000" });
  await page.getByRole("link", { name: "💬 Rückmeldungen" }).click();
  const zeile = page.getByRole("row").filter({ hasText: `heißt anders ${RUN}` });
  await expect(zeile).toContainText(exerciseTitle);
  await expect(zeile).toContainText("Falsch übersetzt");
  await expect(zeile).toContainText("eastwards");
  await zeile.getByRole("button", { name: "Erledigt" }).click();
  await expect(page.getByRole("status").filter({ hasText: "Als erledigt markiert" })).toHaveCount(1);
  await expect(zeile).toHaveCount(0);

  // ---- 3. Ersteller: die Zählung an der eigenen Übung ----
  await page.goto(`/vater/exercises?subjectId=${subjectId}`);
  const uebung = page.locator("div", { hasText: exerciseTitle }).filter({ has: page.getByRole("button", { name: "💬 Rückmeldungen" }) }).last();
  await uebung.getByRole("button", { name: "💬 Rückmeldungen" }).click();
  await expect(page.getByText("Falsch übersetzt ×1").first()).toBeVisible();

  // ---- 4. Sohn: der Hinweis kommt einmal ----
  await sohnLogin(page, { id: String(childId), pin: childPin });
  const hinweis = page.getByRole("status").filter({ hasText: "wurde erledigt" });
  await expect(hinweis).toHaveCount(1);
  await page.getByRole("button", { name: "Alles klar" }).click();
  await expect(hinweis).toHaveCount(0);
  // Erst die Antwort der Hinweis-Abfrage abwarten: vorher ist die Liste leer, und „kein Hinweis“ hielte auch dann, wenn die
  // Quittung nicht gespeichert wäre.
  const hinweiseGeladen = page.waitForResponse((r) => r.url().includes(`/children/${childId}/feedback-notices`) && r.ok());
  await page.reload();
  await hinweiseGeladen;
  await expect(page.getByRole("status").filter({ hasText: "wurde erledigt" })).toHaveCount(0);

  // ---- 5. Im laufenden Test gibt es keinen Knopf (B-623, Entscheidung 5) – erst das Ergebnisblatt trägt ihn ----
  await pfadStart(page, "test");
  await expect(page.getByRole("textbox", { name: "Lösung" })).toBeVisible();
  await expect(page.getByRole("button", { name: /Rückmeldung/ })).toHaveCount(0);

  // ---- 5b. „Später weiter“, dann im Üben melden: der offene Test sperrt die Meldung nicht (B-685 AK 1) ----
  await page.getByRole("button", { name: "Später weiter" }).click();
  await expect(page).toHaveURL(/\/sohn$/);
  await pfadStart(page, "practice");
  await expect(page.getByRole("textbox", { name: "Deine Antwort" })).toBeVisible();
  await page.getByRole("button", { name: /Rückmeldung/ }).click();
  await page.getByRole("button", { name: "Falsch übersetzt" }).click();
  await page.getByRole("button", { name: "Absenden" }).click();
  await expect(page.getByText("Danke, ist angekommen!")).toBeVisible();
  await page.goto("/sohn");
  await pfadStart(page, "test");
  await expect(page.getByRole("textbox", { name: "Lösung" })).toBeVisible();

  // ---- 6. Die einzige Frage beantwortet schließt den Test ab; das Ergebnisblatt bietet die Meldung an (B-685 AK 3) ----
  await tippen(page.getByRole("textbox", { name: "Lösung" }), "eastwards");
  await page.getByRole("textbox", { name: "Lösung" }).press("Enter");
  await expect(page.getByRole("button", { name: /Rückmeldung/ })).toHaveCount(1);
});
