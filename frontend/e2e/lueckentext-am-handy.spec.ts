import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin, pfadStart } from "./helpers";

/*
 * B-466 – ein Lückentext im Tagestest, am Handy (375 × 812, das kleinste Format aus dem Prüfgang). Der Stoff entsteht
 * über die API: drei nummerierte Sätze, je in einer eigenen Zeile, wie im Access-Lehrwerk („Question tags“). Die Sätze
 * sind absichtlich kurz: ohne Umbruch passen alle drei sicher in eine Zeile, auch mit einer anderen Schrift (CI).
 *
 * Was jeder Fall prüft – und welche Änderung ihn rot macht:
 * 1. Satz 2 beginnt unter Satz 1 → rot, sobald `.cloze-prompt` die Zeilenumbrüche wieder als Leerzeichen setzt.
 * 2. „Später weiter“ liegt ganz im Fenster → zweiter Test (längstes Etikett); rot, sobald die Kopfzeile nicht umbricht.
 *    Mit dem Etikett eines Lückentexts („Tippen“) passte sie schon vor B-466 – darum steht die Prüfung nicht im ersten.
 * 3. Das Lösungsfeld hat mindestens 16 px Schrift → rot bei 15 px (iOS-Safari zoomt darunter beim Antippen).
 *
 * Eigenes Kind: je Kind und Fach ist genau ein aktiver Plan spielbar.
 */

const RUN = Date.now().toString(36).slice(-5);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

test.use({ viewport: { width: 375, height: 812 } });

test("Lückentext im Tagestest am Handy: eine Zeile je Satz, kein Zoom-Feld", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const childPin = "8642";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `Handy ${RUN}`, pin: childPin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Handy-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Handy-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Unit 1", orderIndex: 1 });
  const exerciseId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/cloze`, headers, {
    title: `Question tags ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: {
      text: "1. Go, {{1}}\n2. Be, {{2}}\n3. Do, {{3}}",
      gaps: [
        { index: 1, answer: "won't you?" },
        { index: 2, answer: "will you?" },
        { index: 3, answer: "won't you?" },
      ],
    },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers,
    { childId, subjectId, title: `Handy-Plan ${RUN}`, durationDays: 7, active: true });
  const positionId = await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, headers,
    { exerciseId, stage: 4, cadence: "Daily", useLeitner: false, orderStrategy: "Serial" });

  await sohnLogin(page, { id: String(childId), pin: childPin });
  // Über den Link der Tagesmission, nicht per `goto`: ein Neuladen verliert die Anmeldung.
  await pfadStart(page, "test");
  await expect(page).toHaveURL(new RegExp(`/sohn/test/${positionId}$`));

  const prompt = page.locator(".cloze-prompt");
  await expect(prompt).toContainText("2. Be,");

  // 1. Je Satz eine Zeile: die Oberkante von „2.“ liegt unter der Unterkante von „1.“.
  const lines = await prompt.evaluate((el) => {
    const rectOf = (needle: string) => {
      const walker = document.createTreeWalker(el, NodeFilter.SHOW_TEXT);
      for (let n = walker.nextNode(); n; n = walker.nextNode()) {
        const at = n.textContent?.indexOf(needle) ?? -1;
        if (at < 0) continue;
        const range = document.createRange();
        range.setStart(n, at);
        range.setEnd(n, at + needle.length);
        return range.getBoundingClientRect();
      }
      return null;
    };
    const first = rectOf("1. Go");
    const second = rectOf("2. Be");
    return first && second ? { firstBottom: first.bottom, secondTop: second.top } : null;
  });
  expect(lines, "beide Satzanfänge stehen im Text").not.toBeNull();
  expect(lines!.secondTop, "Satz 2 beginnt in einer neuen Zeile").toBeGreaterThanOrEqual(lines!.firstBottom - 1);

  // 3. Das Lösungsfeld zoomt nicht: mindestens 16 px Schrift.
  const field = page.getByRole("textbox", { name: "Lösung" });
  await expect(field).toBeVisible();
  const fontSize = await field.evaluate((el) => parseFloat(getComputedStyle(el).fontSize));
  expect(fontSize, "Schriftgröße des Lösungsfelds").toBeGreaterThanOrEqual(16);
});

/*
 * Fall 2: der Kopf mit zweistelligem Zähler („Frage 1 / 10“). Seit B-526 trägt er keine Stufen-Pille mehr – das
 * ✕ („Später weiter“) steht links, der Balken gibt nach. Rot, sobald der Kopf seitlich übersteht oder das ✕
 * aus dem Bild gerät.
 */
test("Kopfzeile des Tagestests am Handy: auch mit zehn Fragen bleibt „Später weiter“ im Bild", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const childPin = "8643";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `Kopf ${RUN}`, pin: childPin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Kopf-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Kopf-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Unit 1", orderIndex: 1 });
  const words = ["one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten"];
  const exerciseId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/vocabulary`, headers, {
    title: `Zahlen ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de",
      items: words.map((w, i) => ({ front: `${w} ${RUN}`, back: `Zahl ${i + 1}` })) },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers,
    { childId, subjectId, title: `Kopf-Plan ${RUN}`, durationDays: 7, active: true });
  const positionId = await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, headers,
    { exerciseId, stage: 2, cadence: "Daily", useLeitner: false, orderStrategy: "Serial" });

  await sohnLogin(page, { id: String(childId), pin: childPin });
  await pfadStart(page, "test");
  await expect(page).toHaveURL(new RegExp(`/sohn/test/${positionId}$`));

  // Was die Pille sagte, steht jetzt in der Arbeitsanweisung – mit dem Wort „Test“ davor.
  await expect(page.getByRole("heading", { name: "Test · Weißt du es?" })).toBeVisible();
  await expect(page.locator(".pill.cyan", { hasText: /10/ })).toBeVisible();
  await expect(page.getByRole("button", { name: "Später weiter" })).toBeVisible();
  // Gemessen am LETZTEN Element des Kopfs: das ✕ steht vorn und bliebe auch in einem zu breiten Kopf im Bild – der
  // Zähler ist es, der rechts hinausgeschoben würde.
  const box = await page.locator(".lektion-kopf .pill.cyan").boundingBox();
  expect(box, "der Zähler hat eine Fläche").not.toBeNull();
  expect(box!.x + box!.width, "rechter Rand des Zählers im Fenster").toBeLessThanOrEqual(375);
  // B-479: gemessen am Scroller, der wirklich scrollt. `.sohn-body` hat `overflow-y: auto` und damit auch seitlich `auto`
  // – ein zu breiter Kopf scrollt IN ihm, die Seite (`documentElement`) bliebe dabei immer schmal.
  const body = page.locator(".sohn-body");
  const [weit, sichtbar] = await body.evaluate((el) => [el.scrollWidth, el.clientWidth]);
  expect(weit, "der Inhalt scrollt nicht seitlich").toBeLessThanOrEqual(sichtbar);
});

/*
 * Fall 3 (B-599): die Auswahl je Lücke am Handy – Lösung und vier falsche Vorschläge, also fünf Knöpfe bei 375 px. Rot,
 * sobald ein Knopf rechts aus dem Fenster ragt oder die Karte seitlich scrollt (fünf lange Tags in einer Reihe).
 */
test("Auswahl je Lücke am Handy: fünf Knöpfe passen ins Fenster", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const childPin = "8644";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `Auswahl ${RUN}`, pin: childPin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Auswahl-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Auswahl-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Unit 1", orderIndex: 1 });
  const tags = ["doesn't she?", "haven't they?", "wouldn't you?", "isn't it?", "can't we?", "didn't he?"];
  const exerciseId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/cloze`, headers, {
    title: `Auswahl je Lücke ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: {
      text: tags.map((_, i) => `${i + 1}. Satz, {{${i + 1}}}`).join("\n"),
      gaps: tags.map((answer, i) => ({ index: i + 1, answer })),
    },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers,
    { childId, subjectId, title: `Auswahl-Plan ${RUN}`, durationDays: 7, active: true });
  await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, headers,
    { exerciseId, stage: 8, cadence: "Daily", useLeitner: false, orderStrategy: "Serial" });

  await sohnLogin(page, { id: String(childId), pin: childPin });
  await pfadStart(page, "test");

  // Die Optionen tragen `aria-pressed` (markieren, dann „Prüfen“); so bleibt der Hauptknopf außen vor.
  const knoepfe = page.locator(".lektion-antwort button[aria-pressed]");
  await expect(knoepfe).toHaveCount(5);
  for (const knopf of await knoepfe.all()) {
    const box = await knopf.boundingBox();
    expect(box, "der Knopf hat eine Fläche").not.toBeNull();
    expect(box!.x + box!.width, `rechter Rand von „${await knopf.innerText()}“ im Fenster`).toBeLessThanOrEqual(375);
  }
  const body = page.locator(".sohn-body");
  const [weit, sichtbar] = await body.evaluate((el) => [el.scrollWidth, el.clientWidth]);
  expect(weit, "der Inhalt scrollt nicht seitlich").toBeLessThanOrEqual(sichtbar);
});
