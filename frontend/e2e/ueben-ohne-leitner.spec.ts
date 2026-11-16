import { test, expect, type APIRequestContext } from "@playwright/test";
import { lernkarteDurchspielen, sohnLogin } from "./helpers";

/*
 * B-454: eine prüfbare Vokabel-Position OHNE Leitner-Kasten bot dem Kind nur „TEST" – es wurde Wörter gefragt, die es
 * nie gesehen hatte. Die Position entsteht über die API mit `useLeitner: false`, weil genau dieser Zustand der
 * Prüfgegenstand ist (das Formular setzt das Häkchen bei einer Dosis selbst).
 */

const RUN = Date.now().toString(36).slice(-5);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

test("Ohne Leitner-Kasten kann das Kind trotzdem erst üben, dann testen", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };
  const pin = "7531";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `Ohne-Kasten ${RUN}`, pin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Ohne-Kasten-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Ohne-Kasten-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Unité 1", orderIndex: 1 });
  const exerciseId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/vocabulary`, headers, {
    title: `Sans boîte ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "fr", targetLang: "de", items: [{ front: `le pont ${RUN}`, back: "die Brücke" }] },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers, { childId, title: `Ohne Kasten ${RUN}`, durationDays: 7, active: true });
  await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, headers,
    { exerciseId, stage: 4, cadence: "Daily", useLeitner: false });

  await sohnLogin(page, { id: String(childId), pin });
  await page.locator(".pfad-knoten").click();
  const blatt = page.getByRole("dialog");
  await expect(blatt.getByRole("link", { name: "🎯 Test", exact: true })).toBeVisible();
  // Der Kern: „Los" führt zuerst ins Üben, nicht in den Test – und zeigt das Wort mit Lösung, bevor gefragt wird: die
  // Lernkarte (B-598) zeigt das deutsche Wort und deckt das Fremdwort auf.
  await blatt.getByRole("link", { name: "▶ LOS", exact: true }).click();
  await expect(page.getByText("die Brücke", { exact: true })).toBeVisible();
  expect(await lernkarteDurchspielen(page)).toBe(`le pont ${RUN}`);
});
