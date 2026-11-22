import { test, expect, type APIRequestContext, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-700 / B-840: der Aufbau-Weg im Übungseditor. Der Creator macht eine Übung zur Vorstufe einer anderen derselben
 * Unit, speichert, öffnet neu und sieht beide Werte; die Ziel-Übung zeigt danach, dass sie Ziel ist, statt eine Auswahl
 * anzubieten. Ein zweites Speichern ohne Änderung lässt den Weg stehen (Vollersatz-PUT, B-700 AK 3).
 *
 * Katalog und Übungen entstehen über die API – Gegenstand ist der Editor, nicht das Anlegen (`frontend/CLAUDE.md`:
 * eine Fläche je Spec). Das ist zugleich der Rollengang aus der Creator-Rolle.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString(36).slice(-5);
const TARGET = `Ziel-Übung ${RUN}`;
const PREREQ = `Vorstufe ${RUN}`;

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

async function editorOeffnen(page: Page, unitId: number, seriesId: number, subjectId: number, title: string) {
  await page.goto(`/vater/exercises?subjectId=${subjectId}&seriesId=${seriesId}&seriesUnitId=${unitId}`);
  const zeile = page.getByRole("group", { name: title, exact: true });
  await zeile.getByRole("button", { name: "✏️ Bearbeiten" }).click();
  await expect(page.locator("#ed-title")).toHaveValue(title);
}

test("Der Creator macht im Editor eine Übung zur Vorstufe – und der Weg überlebt Neuöffnen und Speichern", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Aufbau-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Aufbau-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers,
    { label: "Unit 1", grade: 6, orderIndex: 1 });
  const vocab = (title: string) => ({
    title, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: [{ front: "dog", back: "Hund" }] },
  });
  const base = `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/vocabulary`;
  const targetId = await postId(request, base, headers, vocab(TARGET));
  const prereqId = await postId(request, base, headers, vocab(PREREQ));

  await vaterLogin(page, FATHER);

  // ---- Vorstufe setzen ----
  await editorOeffnen(page, unitId, seriesId, subjectId, PREREQ);
  const ziel = page.getByLabel("Vorstufe von", { exact: true });
  // Die Auswahl bietet die Ziel-Übung an, nicht die Übung selbst (B-704 AK 2).
  await expect(ziel.locator("option", { hasText: TARGET })).toHaveCount(1);
  await expect(ziel.locator("option", { hasText: PREREQ })).toHaveCount(0);
  await ziel.selectOption({ label: TARGET });
  await page.getByLabel("Stufe", { exact: true }).selectOption("2");
  await page.getByRole("button", { name: "Übung speichern" }).click();
  await expect(page.getByText("Gespeichert.")).toBeVisible();

  const saved = await (await request.get(`/api/v1/creator/exercises/${prereqId}`, { headers })).json();
  expect([saved.buildLevel, saved.buildTargetExerciseId]).toEqual([2, targetId]);

  // ---- Neu öffnen: beide Werte stehen da; ein Speichern ohne Änderung lässt sie stehen ----
  await editorOeffnen(page, unitId, seriesId, subjectId, PREREQ);
  await expect(page.getByLabel("Vorstufe von", { exact: true })).toHaveValue(String(targetId));
  await expect(page.getByLabel("Stufe", { exact: true })).toHaveValue("2");
  await page.getByRole("button", { name: "Übung speichern" }).click();
  await expect(page.getByText("Gespeichert.")).toBeVisible();
  const again = await (await request.get(`/api/v1/creator/exercises/${prereqId}`, { headers })).json();
  expect([again.buildLevel, again.buildTargetExerciseId]).toEqual([2, targetId]);

  // ---- Die Ziel-Übung sagt, dass sie Ziel ist, und bietet keine Auswahl an ----
  await editorOeffnen(page, unitId, seriesId, subjectId, TARGET);
  await expect(page.getByText(/ist Ziel von einer Vorstufe/)).toBeVisible();
  await expect(page.getByLabel("Vorstufe von", { exact: true })).toHaveCount(0);
});
