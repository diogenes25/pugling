import { test, expect, type APIRequestContext } from "@playwright/test";
import { LERNKARTE_DE, pfadStart, sohnLogin } from "./helpers";

/*
 * Anti-Cheat: kein Copy-Paste in der Arcade (kopierSperre.ts). Der Trick, den es schließt: das Wort markieren, solange es
 * auf der Lernkarte steht, und im Gedächtnis-Schritt einfügen.
 *
 * Was jeder Schritt prüft – und welche Änderung ihn rot macht:
 * 1. Gegenprobe am Login (außerhalb der Sperre): ein Einfügen aus der Zwischenablage kommt im Feld an – rot, wenn der
 *    Browser im Testlauf gar nicht einfügt. Ohne sie wären die Schritte 2 und 3 leere Zusicherungen.
 * 2. Das Wort lässt sich nicht markieren (also am Handy kein Kopier-Menü) – rot ohne `user-select: none` an `.app-sohn`.
 * 3. Was sich markieren lässt, lässt sich trotzdem nicht kopieren: das abgeschriebene Wort im Feld, Strg+A, Strg+C –
 *    rot ohne den copy-Hörer (ein Feld bleibt markierbar, die CSS schützt hier nicht).
 * 4. Text von außerhalb lässt sich weder im Abschreib- noch im Gedächtnis-Schritt einfügen – rot ohne BEIDE
 *    Einfüge-Schichten (`paste` und `insertFromPaste` decken sich in Chromium gegenseitig, einzeln prüfen das die
 *    Unit-Fälle) oder wenn `useKopierSperre` nicht an der Arcade-Wurzel hängt.
 * 5. Ein ganzes Wort auf einmal (so schickt es `fill()`, so kommt am iPhone Wischtippen und Diktat) bleibt draußen, und
 *    die Arcade sagt, warum – rot ohne den Hinweis aus `beiMehrzeichen`.
 */

const RUN = Date.now().toString(36).slice(-5);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

test.use({ permissions: ["clipboard-read", "clipboard-write"] });

test("Lernkarte: das Wort lässt sich nicht kopieren, und einfügen lässt sich in keinem Schritt", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const childPin = "8642";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `Abschreiber ${RUN}`, pin: childPin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Kopier-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Kopier-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Unit 1", orderIndex: 1 });
  const exerciseId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/vocabulary`, headers, {
    title: `Kopieren ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de",
      items: [{ front: "station", back: `der Bahnhof ${RUN}` }] },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers,
    { childId, subjectId, title: `Kopier-Plan ${RUN}`, durationDays: 7, active: true });
  await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, headers,
    { exerciseId, stage: 4, cadence: "Daily", useLeitner: false, orderStrategy: "Serial" });

  const ablageSetzen = (text: string) => page.evaluate((t) => navigator.clipboard.writeText(t), text);
  const ablage = () => page.evaluate(() => navigator.clipboard.readText());

  // 1. Gegenprobe: am Login (noch keine Arcade) kommt Eingefügtes an.
  await page.goto("/sohn");
  await ablageSetzen(String(childId));
  await page.locator("#childId").focus();
  await page.keyboard.press("ControlOrMeta+V");
  await expect(page.locator("#childId")).toHaveValue(String(childId));

  await sohnLogin(page, { id: String(childId), pin: childPin });
  await pfadStart(page, "practice");
  const t = LERNKARTE_DE;

  // 2. Das Wort steht da, lässt sich aber nicht markieren.
  const wort = page.locator(".lernkarte-wort .rev");
  await expect(async () => {
    await page.getByRole("button", { name: t.aufdecken, exact: true }).click({ timeout: 2000 });
    await expect(wort).toBeVisible({ timeout: 1000 });
  }).toPass();
  const markiert = await wort.evaluate((el) => {
    const r = document.createRange();
    r.selectNodeContents(el);
    getSelection()!.removeAllRanges();
    getSelection()!.addRange(r);
    return getSelection()!.toString();
  });
  expect(markiert).not.toContain("station");

  // 3. Das abgeschriebene Wort im Feld ist markierbar – kopieren bringt es trotzdem nicht in die Zwischenablage.
  const kopie = page.getByLabel(t.abschreiben, { exact: true });
  await expect(kopie).toBeVisible();
  await ablageSetzen("vorher");
  await kopie.pressSequentially("station");
  await kopie.press("ControlOrMeta+A");
  await kopie.press("ControlOrMeta+C");
  expect(await ablage()).not.toContain("station");

  // 4. Einfügen von außen: weder beim Abschreiben noch aus dem Kopf.
  await kopie.fill("");
  await ablageSetzen("station");
  await kopie.focus();
  await page.keyboard.press("ControlOrMeta+V");
  await expect(kopie).toHaveValue("");

  await kopie.pressSequentially("station");
  await page.getByRole("button", { name: t.ok, exact: true }).click();
  const erinnern = page.getByLabel(t.erinnern, { exact: true });
  await expect(erinnern).toBeFocused();
  await page.keyboard.press("ControlOrMeta+V");
  await expect(erinnern).toHaveValue("");

  // 5. Das ganze Wort auf einmal.
  await expect(page.getByText("Bitte Buchstabe für Buchstabe tippen.")).toHaveCount(0);
  await erinnern.fill("station");
  await expect(erinnern).toHaveValue("");
  await expect(page.getByText("Bitte Buchstabe für Buchstabe tippen.")).toBeVisible();
});
