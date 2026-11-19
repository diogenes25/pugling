import { test, expect, type APIRequestContext } from "@playwright/test";
import { LERNKARTE_DE, pfadStart, sohnLogin, tippen } from "./helpers";

/*
 * B-661 – ein Buchstabe, den die Tastatur nicht hat, blockiert keine Lernkarte mehr (zugleich der Rollengang Sohn). Der
 * Stoff entsteht über die API: eine französische Reihe (Lernsprache `fr`) mit einem Wort, das ein „î“ trägt.
 *
 * Was jeder Schritt prüft – und welche Änderung ihn rot macht:
 * 1. Abschreiben ohne Akzent („diner“) gibt „OK“ nicht frei und sagt „Fast – achte auf die Akzente“ – rot mit einer
 *    Toleranz oder ohne den Hinweis (Entscheidung 2).
 * 2. Über die Leiste wird „dîner“ abgeschrieben; das Feld behält dabei den Fokus – rot ohne Leiste, ohne
 *    `keyboardLang` vom Server oder wenn der Knopf den Fokus nimmt (am Handy klappte die Tastatur zu).
 * 3. Aus dem Kopf über die Leiste geschrieben wertet der Server richtig – rot, wenn das eingesetzte Zeichen nicht das
 *    echte „î“ ist (die Kopiersperre verwirft es, oder es kommt doppelt).
 *
 * Eigenes Kind: je Kind und Fach ist genau ein aktiver Plan spielbar.
 */

const RUN = Date.now().toString(36).slice(-5);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

test("Sonderzeichen: „dîner“ lässt sich über die Leiste abschreiben und aus dem Kopf schreiben", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const childPin = "8643";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `Akzent-Kind ${RUN}`, pin: childPin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Französisch-Tastatur ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Tastatur-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: "fr", targetLanguage: "de", notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Unité 1", orderIndex: 1 });
  const exerciseId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/vocabulary`, headers, {
    title: `Repas ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "fr", targetLang: "de",
      items: [{ front: "dîner", back: `zu Abend essen ${RUN}` }] },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers,
    { childId, subjectId, title: `Tastatur-Plan ${RUN}`, durationDays: 7, active: true });
  await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, headers,
    { exerciseId, stage: 4, cadence: "Daily", useLeitner: false, orderStrategy: "Serial" });

  await sohnLogin(page, { id: String(childId), pin: childPin });
  await pfadStart(page, "practice");
  const t = LERNKARTE_DE;

  const wortFeld = page.locator(".lernkarte-wort .rev");
  await expect(async () => {
    await page.getByRole("button", { name: t.aufdecken, exact: true }).click({ timeout: 2000 });
    await expect(wortFeld).toBeVisible({ timeout: 1000 });
  }).toPass();
  const kopie = page.getByLabel(t.abschreiben, { exact: true });
  await expect(kopie).toBeVisible();
  const ok = page.getByRole("button", { name: t.ok, exact: true });
  const i = page.getByRole("button", { name: "î einsetzen" });

  // 1. Ohne Akzent: kein OK, aber der Hinweis.
  await tippen(kopie, "diner");
  await expect(ok).toBeDisabled();
  await expect(page.getByText("Fast – achte auf die Akzente")).toBeVisible();

  // 2. Über die Leiste: das „î“ landet an der Schreibmarke, das Feld behält den Fokus.
  await tippen(kopie, "d");
  await i.click();
  await expect(kopie).toBeFocused();
  await kopie.pressSequentially("ner");
  await expect(kopie).toHaveValue("dîner");
  await ok.click();

  // 3. Aus dem Kopf, wieder über die Leiste – der Server wertet richtig.
  const erinnern = page.getByLabel(t.erinnern, { exact: true });
  await expect(erinnern).toBeVisible();
  await tippen(erinnern, "d");
  await i.click();
  await expect(erinnern).toBeFocused();
  await erinnern.pressSequentially("ner");
  const [res] = await Promise.all([
    page.waitForResponse((r) => r.url().includes("/review") && r.request().method() === "POST"),
    ok.click(),
  ]);
  expect((await res.json()).wasCorrect, "„dîner“ über die Leiste ist richtig").toBe(true);
});
