import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin, pfadStart, vaterLogin, tippen } from "./helpers";

/*
 * B-767–B-769 – eine im Test falsch gewertete Antwort, einmal ganz herum (der Rollengang von Sprint 151): der Sohn verliert
 * den Test an einer zu streng gewerteten Antwort und meldet sie auf dem Ergebnisblatt, der Vater gibt ihm recht, der Test
 * wird nachgerechnet und das verpasste Tagesziel nachgebucht, der Sohn liest im Hinweis, was nicht neu gebucht wird.
 *
 * Was jeder Schritt prüft – und welche Änderung ihn rot macht:
 * 1. Die Meldung vom Ergebnisblatt trägt den Bezug zur Antwort → rot, wenn der Server ihn nicht einfriert (dann gibt es an
 *    der Zeile kein „Berechtigt“, nur „Erledigt“).
 * 2. Die Zeile nennt „im Test als falsch gewertet“, „Berechtigt“ erledigt (die Rückfrage wird angenommen) → rot ohne Knöpfe
 *    oder ohne Urteil im PATCH. Dass „Abbrechen“ nichts sendet, prüft `VaterRueckmeldungen.test.tsx`.
 * 3. Das Tagesziel ist danach erfüllt: die Ziel-Belohnung steht im Konto des Kindes → rot, wenn die Korrektur die Pflicht
 *    nicht erreicht (B-768 AK 2/5).
 * 4. Die erledigte Zeile zeigt „berechtigt“ und kein „Wieder öffnen“ → rot, wenn das endgültige Urteil umkehrbar aussieht.
 * 5. Der Sohn sieht den Hinweis samt dem Satz über Kasten und Missionen → rot ohne Satz (B-769 AK 3).
 *
 * Eigenes Kind: je Kind und Fach ist genau ein aktiver Plan spielbar.
 */

const RUN = Date.now().toString(36).slice(-5);
const ZIEL_PUNKTE = 15;

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

test("Berechtigte Meldung: Test nachgerechnet, Tagesziel nachgebucht, Hinweis an den Sohn", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const childPin = "6532";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `Recht ${RUN}`, pin: childPin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Recht-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Recht-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Unit 1", orderIndex: 1 });
  const exerciseId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/cloze`, headers, {
    title: `Recht-Übung ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { text: "I travel {{1}}.", gaps: [{ index: 1, answer: "eastwards", prompt: `Richtung Osten ${RUN}` }] },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers,
    { childId, subjectId, title: `Recht-Plan ${RUN}`, durationDays: 7, active: true });
  await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, headers,
    { exerciseId, cadence: "Daily", useLeitner: false, stage: 3, pointsGoalMet: ZIEL_PUNKTE });

  // ---- 1. Sohn: verliert den Test an einer Antwort und meldet sie auf dem Ergebnisblatt ----
  await sohnLogin(page, { id: String(childId), pin: childPin });
  await pfadStart(page, "test");
  const loesung = page.getByRole("textbox", { name: "Lösung" });
  await tippen(loesung, "eastward");
  await loesung.press("Enter");
  await page.getByRole("button", { name: /Rückmeldung/ }).click();
  await page.getByRole("button", { name: /Zu streng bewertet/ }).click();
  await page.getByLabel("Willst du noch etwas dazu schreiben?").fill(`nur ein s ${RUN}`);
  await page.getByRole("button", { name: "Absenden" }).click();
  await expect(page.getByText("Danke, ist angekommen!")).toBeVisible();

  // Vorher: der Test ist nicht bestanden, es gibt keine Ziel-Belohnung – sonst bewiese Schritt 3 nichts.
  const vorher = await request.get(`/api/v1/supervisor/children/${childId}/points`, { headers });
  expect(((await vorher.json()) as { entries: { kind: string }[] }).entries.filter((b) => b.kind === "Goal")).toEqual([]);

  // ---- 2. Vater: gibt ihm recht ----
  await vaterLogin(page, { id: "1", pin: "0000" });
  await page.goto(`/vater/rueckmeldungen?childId=${childId}`);
  const zeile = page.getByRole("row").filter({ hasText: `nur ein s ${RUN}` });
  await expect(zeile).toContainText("im Test als falsch gewertet");
  await expect(zeile.getByRole("button", { name: "Erledigt" })).toHaveCount(0);
  page.once("dialog", (d) => d.accept());
  await zeile.getByRole("button", { name: "Berechtigt" }).click();
  await expect(page.getByRole("status").filter({ hasText: "Als berechtigt erledigt" })).toHaveCount(1);

  // ---- 3. Das Tagesziel ist nachgebucht ----
  const punkte = await request.get(`/api/v1/supervisor/children/${childId}/points`, { headers });
  expect(punkte.ok(), await punkte.text()).toBeTruthy();
  const { entries } = (await punkte.json()) as { entries: { kind: string; amount: number }[] };
  expect(entries.filter((b) => b.kind === "Goal").map((b) => b.amount)).toEqual([ZIEL_PUNKTE]);

  // ---- 4. Die erledigte Zeile trägt ihr Urteil, endgültig ----
  await page.getByLabel("Status").selectOption("Done");
  const erledigt = page.getByRole("row").filter({ hasText: `nur ein s ${RUN}` });
  await expect(erledigt).toContainText("berechtigt");
  await expect(erledigt.getByRole("button", { name: "Wieder öffnen" })).toHaveCount(0);

  // ---- 5. Sohn: der Hinweis sagt, was nicht neu gebucht wird ----
  await sohnLogin(page, { id: String(childId), pin: childPin });
  const hinweis = page.getByRole("status").filter({ hasText: "wurde erledigt" });
  await expect(hinweis).toContainText("Kasten, Missionen");
});
