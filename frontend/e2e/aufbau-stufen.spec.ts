import { test, expect, type APIRequestContext } from "@playwright/test";
import { createChild, pfadStart, sohnLogin, vaterLogin } from "./helpers";

/*
 * B-841: der Aufbau-Weg in der Oberfläche, als Rollengang Vater → Sohn. Der Vater wählt im Positionsformular eine Übung
 * mit Vorstufe, sieht das Feld „Start-Stufe“, setzt Stufe 1 und bekommt nur noch „frei“ oder „bis erledigt“ als Rhythmus;
 * die Zeile zeigt „Stufe 1 von 2“. Der Sohn sieht dasselbe Band im Kopf seiner Runde. Eine Übung ohne Vorstufe zeigt kein
 * Feld (B-704 AK 3).
 *
 * Katalog und Plan entstehen über die API – Gegenstand ist das Formular und das Band, nicht das Anlegen
 * (`frontend/CLAUDE.md`: eine Fläche je Spec).
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString(36).slice(-5);
const TARGET = `Stufen-Ziel ${RUN}`;
const PREREQ = `Stufen-Vorstufe ${RUN}`;
const PLAIN = `Ohne Weg ${RUN}`;

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

test("Der Vater setzt die Start-Stufe im Positionsformular, und der Sohn sieht sein Stufenband", async ({ page, browser, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Stufen-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Stufen-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers,
    { label: "Unit 1", grade: 6, orderIndex: 1 });
  const vocab = (title: string, extra: object = {}) => ({
    title, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: [{ front: "dog", back: "Hund" }] },
    ...extra,
  });
  const base = `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/vocabulary`;
  const targetId = await postId(request, base, headers, vocab(TARGET));
  await postId(request, base, headers, vocab(PREREQ, { buildLevel: 1, buildTargetExerciseId: targetId }));
  await postId(request, base, headers, vocab(PLAIN));

  await vaterLogin(page, FATHER);
  const childId = await createChild(page, `Stufen-Kind ${RUN}`, "4141");
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers,
    { childId: Number(childId), title: `Stufen-Plan ${RUN}`, subjectId, durationDays: 5 });

  await page.goto(`/vater/plan/${planId}`);
  await page.getByLabel("Fach-Filter").selectOption(String(subjectId));
  await page.getByLabel("Reihe-Filter").selectOption(String(seriesId));
  await page.getByLabel("Unit-Filter").selectOption(String(unitId));

  // ---- Ohne Vorstufe kein Feld (B-704 AK 3) ----
  // Erst nach der Antwort der Unit-Abfrage des Felds prüfen – vorher stünde die Abwesenheit schon fest. Dass das Feld
  // nicht erst einen Render später erscheint, trägt zusätzlich `StartLevelField.test.ts` (keine Optionen ohne Vorstufe).
  const unitGeladen = page.waitForResponse((r) =>
    r.url().includes(`seriesUnitId=${unitId}`) && r.url().includes("take=500") && r.ok());
  await page.getByRole("radio", { name: new RegExp(PLAIN) }).check();
  await unitGeladen;
  await expect(page.getByLabel("Ziel-Rhythmus", { exact: true })).toBeVisible();
  await expect(page.getByLabel("Start-Stufe", { exact: true })).toHaveCount(0);

  // ---- Mit Vorstufe erscheint es; Stufe 1 lässt nur „frei“ und „bis erledigt“ zu (AK 4, AK 6) ----
  await page.getByRole("radio", { name: new RegExp(TARGET) }).check();
  const start = page.getByLabel("Start-Stufe", { exact: true });
  await expect(start).toBeVisible();
  // Beschriftet nach dem Platz auf dem Weg – derselbe, den Zeile und Band zeigen.
  await expect(start.locator("option", { hasText: "Stufe 2 von 2 · Ziel-Übung" })).toHaveCount(1);
  const rhythmus = page.locator('select[aria-label="Ziel-Rhythmus"]');
  await rhythmus.selectOption("Weekly");
  await start.selectOption("1");
  await expect(rhythmus).toHaveValue("None");
  // Fällt der Weg weg, kommt das verdrängte Wochenziel zurück – keine still pflichtfreie Position.
  await start.selectOption("");
  await expect(rhythmus).toHaveValue("Weekly");
  await start.selectOption("1");
  await expect(rhythmus).toHaveValue("None");
  // Dasselbe beim Wechsel auf eine Übung ohne Weg; zurück auf das Ziel beginnt das Feld wieder ohne Weg.
  await page.getByRole("radio", { name: new RegExp(PLAIN) }).check();
  await expect(rhythmus).toHaveValue("Weekly");
  await page.getByRole("radio", { name: new RegExp(TARGET) }).check();
  await expect(start).toHaveValue("");
  await start.selectOption("1");
  await expect(rhythmus).toHaveValue("None");
  await expect(rhythmus.locator("option[value='Daily']")).toHaveCount(0);
  await expect(rhythmus.locator("option[value='Weekly']")).toHaveCount(0);
  await page.getByRole("button", { name: /Position hinzufügen/ }).click();

  const zeile = page.getByRole("row", { name: new RegExp(TARGET) });
  await expect(zeile.getByText(`Stufe 1 von 2 · gerade: ${PREREQ}`)).toBeVisible();
  const positions = await (await request.get(`/api/v1/supervisor/study-plans/${planId}/positions`, { headers })).json();
  expect(positions.map((p: { currentBuildLevel: number | null }) => p.currentBuildLevel)).toEqual([1]);

  // ---- Der Sohn sieht das Band im Kopf seiner Runde (AK 5) ----
  const sohnCtx = await browser.newContext();
  const sohn = await sohnCtx.newPage();
  await sohnLogin(sohn, { id: childId, pin: "4141" });
  await pfadStart(sohn, "practice");
  await expect(sohn.locator(".pfad-stufe")).toHaveText("Stufe 1 von 2");
  await sohnCtx.close();
});
