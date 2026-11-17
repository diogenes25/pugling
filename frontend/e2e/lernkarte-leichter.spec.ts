import { test, expect, type APIRequestContext } from "@playwright/test";
import { lernkarteDurchspielen, LERNKARTE_DE, pfadStart, sohnLogin, tippen } from "./helpers";

/*
 * B-643 – die Lernkarte mit „Mach's mir leichter“, aus dem Sohn-Sitz (zugleich der Rollengang Sohn von Sprint 92). Der Stoff
 * entsteht über die API: zwei neue Wörter in serieller Reihenfolge.
 *
 * Was jeder Schritt prüft – und welche Änderung ihn rot macht:
 * 1. Im vierten Schritt schaltet „Mach's mir leichter“ Stufe für Stufe: Kästchen, Buchstabensalat, Auswahl – rot ohne den
 *    Knopf oder wenn der Server die Lernkarte nicht leichter schaltet.
 * 2. Die Auswahl bietet Wörter der Lernsprache; richtig gewählt sagt das Blatt „kommt noch einmal“ – rot, wenn eine
 *    Hilfe-Antwort das Wort einprägt.
 * 3. Das Wort kommt am Rundenende als GANZE Lernkarte wieder (Aufdecken, Abschreiben, Erinnern) – rot mit der alten
 *    Grenze oder wenn die Wiederholung die leichtere Form behält.
 * 4. Frei erinnert ist es eingeprägt: der goldene Ring sagt „2 von 2 eingeprägt“.
 *
 * Eigenes Kind: je Kind und Fach ist genau ein aktiver Plan spielbar.
 */

const RUN = Date.now().toString(36).slice(-5);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

test("Lernkarte: leichter schalten bis zur Auswahl, das Wort kommt als ganze Lernkarte wieder, frei erinnert ist es eingeprägt", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const childPin = "8642";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `Einpräger ${RUN}`, pin: childPin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Lernkarten-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Lernkarten-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Unit 1", orderIndex: 1 });
  const exerciseId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/vocabulary`, headers, {
    title: `Lernkarten ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de",
      items: [{ front: "station", back: `der Bahnhof ${RUN}` }, { front: "train", back: `der Zug ${RUN}` }] },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers,
    { childId, subjectId, title: `Lernkarten-Plan ${RUN}`, durationDays: 7, active: true });
  await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, headers,
    { exerciseId, stage: 4, cadence: "Daily", useLeitner: false, orderStrategy: "Serial" });

  await sohnLogin(page, { id: String(childId), pin: childPin });
  await pfadStart(page, "practice");
  const zaehler = page.locator(".pill.cyan", { hasText: /Karte \d+ \/ \d+/ });
  const t = LERNKARTE_DE;

  // Karte 1: sehen, „neu“, abschreiben – dann im vierten Schritt dreimal leichter.
  await expect(zaehler).toHaveText("Karte 1 / 2");
  const wortFeld = page.locator(".lernkarte-wort .rev");
  await expect(async () => {
    await page.getByRole("button", { name: t.aufdecken, exact: true }).click({ timeout: 2000 });
    await expect(wortFeld).toBeVisible({ timeout: 1000 });
  }).toPass();
  const kopie = page.getByLabel(t.abschreiben, { exact: true });
  await expect(async () => {
    await page.getByRole("button", { name: t.neu, exact: true }).click({ timeout: 2000 });
    await expect(kopie).toBeVisible({ timeout: 1000 });
  }).toPass();
  await tippen(kopie, "station");
  await page.getByRole("button", { name: t.ok, exact: true }).click();

  const leichter = page.getByRole("button", { name: "Mach's mir leichter" });
  await leichter.click();
  const kaesten = page.locator(".letterboxes input");
  await expect(kaesten).toHaveCount("station".length);
  await expect(kaesten.first()).toBeFocused();
  await leichter.click();
  await expect(page.getByRole("group", { name: "Buchstaben zum Ordnen" })).toBeVisible();
  await leichter.click();
  const optionen = page.locator(".lektion-option");
  await expect(optionen.filter({ hasText: "train" })).toHaveCount(1);
  await expect(leichter).toHaveCount(0);
  await optionen.filter({ hasText: "station" }).click();
  const [res] = await Promise.all([
    page.waitForResponse((r) => r.url().includes("/review") && r.request().method() === "POST"),
    page.getByRole("button", { name: t.ok, exact: true }).click(),
  ]);
  const outcome = await res.json();
  expect(outcome.wasCorrect, "mit Hilfe richtig").toBe(true);
  expect(outcome.repeatsAtEnd, "mit Hilfe ist nicht eingeprägt").toBe(true);
  await expect(page.getByText("Das Wort kommt am Ende der Runde noch einmal.")).toBeVisible();
  await page.getByRole("button", { name: t.weiter, exact: true }).click();

  // Karte 2: frei erinnert.
  await expect(zaehler).toHaveText("Karte 2 / 3");
  expect(await lernkarteDurchspielen(page)).toBe("train");

  // Karte 3: das erste Wort als ganze Lernkarte – wieder mit Aufdecken und Abschreiben, diesmal frei erinnert.
  await expect(zaehler).toHaveText("Karte 3 / 3");
  expect(await lernkarteDurchspielen(page)).toBe("station");
  await expect(page.getByRole("button", { name: /Jetzt abfragen/ })).toBeVisible();

  // Der goldene Ring zählt eingeprägte Wörter.
  await page.goto("/sohn");
  await page.locator(".pfad-knoten").click();
  await expect(page.getByRole("dialog")).toContainText("2 von 2 eingeprägt");
});
