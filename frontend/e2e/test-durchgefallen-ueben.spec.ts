import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin, pfadStart } from "./helpers";

/*
 * B-430 (c) – nach einem nicht bestandenen Tagestest führt „▶ Üben“ in die Übungsrunde DERSELBEN Position. Die Regel,
 * wann der Knopf steht, prüft `TestResult.test.tsx`; hier liegt nur die Verdrahtung, die eine Komponente nicht sieht:
 * rot, sobald der Knopf auf eine andere Route oder eine andere Position führt.
 *
 * Stoff über die API: eine Vokabel, Stufe 2 (Selbstcheck) – „Nicht gewusst“ macht den Test sicher nicht bestanden.
 * Eigenes Kind: je Kind und Fach ist genau ein aktiver Plan spielbar.
 */

const RUN = Date.now().toString(36).slice(-5);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

test("nicht bestandener Test: „▶ Üben“ öffnet die Übungsrunde dieser Position", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const childPin = "8644";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `Durchgefallen ${RUN}`, pin: childPin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Ueben-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Ueben-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Unit 1", orderIndex: 1 });
  const exerciseId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/vocabulary`, headers, {
    title: `Ein Wort ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de",
      items: [{ front: `tree ${RUN}`, back: "Baum" }] },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers,
    { childId, subjectId, title: `Ueben-Plan ${RUN}`, durationDays: 7, active: true });
  const positionId = await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, headers,
    { exerciseId, stage: 2, cadence: "Daily", useLeitner: false, orderStrategy: "Serial" });

  await sohnLogin(page, { id: String(childId), pin: childPin });
  // Über den Knoten des Pfads, nicht per `goto`: ein Neuladen verliert die Anmeldung.
  await pfadStart(page, "test");
  await expect(page).toHaveURL(new RegExp(`/sohn/test/${positionId}$`));

  await page.getByRole("button", { name: "Aufdecken 🔄" }).click();
  await page.getByRole("button", { name: "Nicht gewusst" }).click();
  await expect(page.getByText("FAST!")).toBeVisible();

  await page.getByRole("button", { name: "▶ Üben" }).click();
  await expect(page).toHaveURL(new RegExp(`/sohn/practice/${positionId}$`));
});
