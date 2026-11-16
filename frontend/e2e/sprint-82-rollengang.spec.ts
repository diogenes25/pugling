import { test, expect, type APIRequestContext, type Page } from "@playwright/test";
import { LERNKARTE_DE, lernkarteDurchspielen, pfadStart, sohnLogin } from "./helpers";

/*
 * Sprint 82 „Schreiben in der Lernsprache“ – maschineller Rollengang des Sohns an der laufenden App.
 *
 * Sprint-Ziel (Sohn): „Wo ich ein Wort schreiben muss, auf einer Vokabelkarte wie in einem Lückentext, schreibe ich das
 * englische Wort, in der Eingabeart, die mein Vater gewählt hat, und ein neues Wort lerne ich in einer Karte vom Sehen
 * übers Abschreiben bis zum Erinnern.“ Je Fall, was ihn rot macht:
 *
 * 1. B-597, Freitext-Test: eine `front-to-back`-Liste fragt das englische Wort ab (rot, sobald wieder Deutsch getippt
 *    wird – der Ring stünde nicht auf voll); eine `both`-Liste schreibt nur die Rückwärtskarte, die Vorwärtskarte wird
 *    erkannt (Auswahl deutscher Wörter) – rot, sobald beide Karten getippt oder beide gewählt werden.
 * 2. B-599, Übung Nr. 19 der echten Datenbank („Unit 1 · Question tags“, Inhalt wörtlich übernommen) auf den drei neuen
 *    Lückentext-Stufen: Kästchen (6), Buchstaben ordnen (7) – ein Anhängsel mit Leerzeichen und Apostroph lässt sich
 *    nicht mischen und fällt Karte für Karte auf Kästchen (Entscheidung 4) –, Auswahl (8) mit Ablenkern aus den anderen
 *    Lücken, alle ohne Schlusszeichen. Rot, sobald eine Stufe die falsche Eingabeart zeigt, nur die Lösung ein „?“
 *    trägt oder der Server eine richtige Antwort verwirft.
 * 3. B-598, frische Station: jede neue Karte ist eine Lernkarte (sehen → aufdecken → abschreiben → erinnern), ein
 *    „Weiß ich nicht“ bringt die Karte einmal ans Rundenende, danach fragt die Runde das englische Wort getippt ab.
 *
 * Eigene Kinder je Fall: je Kind ist genau ein aktiver Plan spielbar, die DB ist zwischen den Specs geteilt.
 */

const RUN = Date.now().toString(36).slice(-5);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), `${url}: ${res.status()} ${await res.text()}`).toBeTruthy();
  return (await res.json()).id as number;
}

async function vaterHeaders(request: APIRequestContext) {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  return { Authorization: `Bearer ${(await login.json()).token}` };
}

/** Eine Unit in einer eigenen Reihe; liefert die Route, unter der Übungen angelegt werden. */
async function unitRoute(request: APIRequestContext, vater: Record<string, string>, name: string) {
  const subjectId = await postId(request, "/api/v1/creator/subjects", vater, { name: `${name} Fach` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", vater, { name, subjectId });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, vater, { label: "Unit 1", orderIndex: 1 });
  return `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}`;
}

/** Kind mit einem aktiven Plan; liefert die Positions-Route. */
async function kindMitPlan(request: APIRequestContext, vater: Record<string, string>, name: string, pin: string) {
  const childId = await postId(request, "/api/v1/supervisor/children", vater, { name, pin });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", vater,
    { childId, title: `Plan ${name}`, durationDays: 7, active: true });
  return { childId, positions: `/api/v1/supervisor/study-plans/${planId}/positions` };
}

const zaehler = (page: Page) => page.locator(".pill.cyan", { hasText: /(Frage|Karte) \d+ \/ \d+/ });
const hauptknopf = (page: Page) => page.locator(".lektion-fuss button.lime");

// ---------------------------------------------------------------------------------------------------------------------

test("B-597: im Freitext-Test schreibt der Sohn das englische Wort – eine both-Liste erkennt vorwärts", async ({ page, request }) => {
  const vater = await vaterHeaders(request);
  const pin = "8261";
  const { childId, positions } = await kindMitPlan(request, vater, `Schreiber ${RUN}`, pin);
  const unit = await unitRoute(request, vater, `Schreibreihe ${RUN}`);
  const VORWAERTS = `Nur vorwärts ${RUN}`, BEIDE = `Beide Richtungen ${RUN}`;
  // Englisch vorn, Deutsch hinten (wie das Access-Buch); der Lauf-Stempel steht am deutschen Wort.
  const paare: [en: string, de: string][] = [["bridge", `Brücke ${RUN}`], ["river", `Fluss ${RUN}`]];
  const paareBeide: [en: string, de: string][] = [["castle", `Burg ${RUN}`], ["island", `Insel ${RUN}`]];
  const vokabeln = (title: string, direction: string, items: [string, string][]) =>
    postId(request, `${unit}/vocabulary`, vater, {
      title, orderIndex: 1, rewardPoints: 10,
      config: { direction, sourceLang: "en", targetLang: "de", items: items.map(([front, back]) => ({ front, back })) },
    });
  const exVor = await vokabeln(VORWAERTS, "front-to-back", paare);
  const exBeide = await vokabeln(BEIDE, "both", paareBeide);
  await postId(request, positions, vater, { exerciseId: exVor, stage: 4, cadence: "Daily", useLeitner: false, orderStrategy: "Serial" });
  await postId(request, positions, vater, { exerciseId: exBeide, stage: 4, cadence: "Daily", useLeitner: false, orderStrategy: "Serial" });

  await sohnLogin(page, { id: String(childId), pin });

  // front-to-back: jede Frage zeigt Deutsch, getippt wird Englisch.
  await pfadStart(page, "test", VORWAERTS);
  for (const [i, [en, de]] of paare.entries()) {
    await expect(zaehler(page)).toHaveText(`Frage ${i + 1} / ${paare.length}`);
    await expect(page.locator(".fcard", { hasText: de })).toBeVisible();
    // Kein Auswahl-Knopf: rot, sobald die Karte wieder auf Auswahl fiele (das Feld unten wäre dann nicht da).
    await expect(page.locator(".lektion-antwort button[aria-pressed]")).toHaveCount(0);
    await page.locator(".lektion-feld").fill(en);
    await hauptknopf(page).click();
  }
  await expect(page.locator(".ring")).toContainText(`${paare.length} / ${paare.length}`);

  // both: vier Fragen – zwei Rückwärtskarten (Deutsch gezeigt, Englisch getippt), zwei Vorwärtskarten (Englisch
  // gezeigt, Deutsch erkannt). Die Reihenfolge entscheidet der Server; gezählt wird, welche Form welche Karte bekam.
  await page.goto("/sohn");
  await pfadStart(page, "test", BEIDE);
  const total = paareBeide.length * 2;
  let getippt = 0, gewaehlt = 0;
  for (let i = 0; i < total; i++) {
    await expect(zaehler(page)).toHaveText(`Frage ${i + 1} / ${total}`);
    const vorn = (await page.locator(".fcard").first().innerText()).trim();
    const auswahl = page.locator(".lektion-antwort button[aria-pressed]");
    const englischGezeigt = paareBeide.find(([en]) => vorn.includes(en) && !paareBeide.some(([, de]) => vorn.includes(de)));
    if (englischGezeigt) {
      await expect(auswahl.first()).toBeVisible();
      await page.getByRole("button", { name: englischGezeigt[1], exact: true }).click();
      gewaehlt++;
    } else {
      const paar = paareBeide.find(([, de]) => vorn.includes(de));
      expect(paar, `die Frage „${vorn}“ zeigt ein Wort der Liste`).toBeTruthy();
      await expect(auswahl).toHaveCount(0);
      await page.locator(".lektion-feld").fill(paar![0]);
      getippt++;
    }
    await hauptknopf(page).click();
  }
  expect({ getippt, gewaehlt }).toEqual({ getippt: 2, gewaehlt: 2 });
  await expect(page.locator(".ring")).toContainText(`${total} / ${total}`);
});

// ---------------------------------------------------------------------------------------------------------------------

/** Übung Nr. 19 der echten Datenbank (English G Access G9 Band 3, Unit 1), Text und Lücken wörtlich. */
const QUESTION_TAGS = {
  text: [
    "1. Your sister plays the drums, {{1}}", "2. We aren't late, {{2}}", "3. You can swim, {{3}}",
    "4. The museum was closed yesterday, {{4}}", "5. Ben didn't call you, {{5}}", "6. They have got a dog, {{6}}",
    "7. It isn't raining, {{7}}", "8. I'm in your team, {{8}}", "9. There is a café near the station, {{9}}",
    "10. Your parents went to Scotland last summer, {{10}}",
  ].join("\n"),
  answers: ["doesn't she?", "are we?", "can't you?", "wasn't it?", "did he?", "haven't they?", "is it?", "aren't I?",
    "isn't there?", "didn't they?"],
};

for (const [stage, form, pin] of [[6, "Kästchen", "8262"], [7, "Buchstaben ordnen", "8263"], [8, "Auswahl", "8264"]] as const) {
  test(`B-599: Übung 19 (Question tags) auf Stufe ${stage} – ${form}`, async ({ page, request }) => {
    const vater = await vaterHeaders(request);
    const { childId, positions } = await kindMitPlan(request, vater, `Tags ${stage} ${RUN}`, pin);
    const unit = await unitRoute(request, vater, `Tagsreihe ${stage} ${RUN}`);
    const exerciseId = await postId(request, `${unit}/cloze`, vater, {
      title: `Unit 1 – Question tags ${stage} ${RUN}`, orderIndex: 1, rewardPoints: 20,
      config: {
        text: QUESTION_TAGS.text,
        gaps: QUESTION_TAGS.answers.map((answer, i) => ({ index: i + 1, answer, alternatives: [answer.replace(/\?$/, "")] })),
      },
    });
    await postId(request, positions, vater, { exerciseId, stage, cadence: "Daily", useLeitner: false, orderStrategy: "Serial" });

    await sohnLogin(page, { id: String(childId), pin });
    await pfadStart(page, "test");
    const n = QUESTION_TAGS.answers.length;
    for (const [i, answer] of QUESTION_TAGS.answers.entries()) {
      await expect(zaehler(page)).toHaveText(`Frage ${i + 1} / ${n}`);
      const kaesten = page.locator(".letterboxes input");
      const kacheln = page.getByRole("group", { name: "Buchstaben zum Ordnen" });
      const optionen = page.locator(".lektion-antwort button[aria-pressed]");
      if (stage === 8) {
        // Lösung plus mindestens zwei Ablenker – aus den anderen Lücken, denn Nr. 19 trägt keine eigenen. Alle in einer
        // Form, ohne Schlusszeichen (B-599, Risiko 2): ein „?“ nur an der Lösung verriete sie.
        const ohneSchluss = (s: string) => s.replace(/[?.!]+$/, "");
        await expect(optionen.first()).toBeVisible();
        const texte = (await optionen.allInnerTexts()).map((o) => o.trim());
        expect(texte.length, `Optionen der Lücke ${i + 1}`).toBeGreaterThanOrEqual(3);
        for (const o of texte) expect(QUESTION_TAGS.answers.map(ohneSchluss)).toContain(o);
        expect(texte.filter((o) => o.endsWith("?")), "kein Schlusszeichen verrät die Lösung").toEqual([]);
        await page.getByRole("button", { name: ohneSchluss(answer), exact: true }).click();
        await hauptknopf(page).click();
      } else {
        // Stufe 7: „doesn't she?“ ist kein Wort aus 2–12 Buchstaben, also fällt jede Lücke auf Kästchen.
        await expect(kacheln).toHaveCount(0);
        const buchstaben = answer.replace(/[^\p{L}\p{N}]/gu, "");
        await expect(kaesten).toHaveCount(buchstaben.length);
        await kaesten.first().click();
        await page.keyboard.type(buchstaben);
        await page.keyboard.press("Enter");
      }
    }
    await expect(page.locator(".ring")).toContainText(`${n} / ${n}`);
  });
}

// ---------------------------------------------------------------------------------------------------------------------

test("B-598: eine frische Station lehrt jedes Wort auf der Lernkarte – Aufgeben bringt es einmal ans Ende", async ({ page, request }) => {
  const vater = await vaterHeaders(request);
  const pin = "8265";
  const { childId, positions } = await kindMitPlan(request, vater, `Lerner ${RUN}`, pin);
  const unit = await unitRoute(request, vater, `Lernreihe ${RUN}`);
  const paare: [en: string, de: string][] = [["harbour", `Hafen ${RUN}`], ["lighthouse", `Leuchtturm ${RUN}`]];
  const exerciseId = await postId(request, `${unit}/vocabulary`, vater, {
    title: `Lernkarten ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: paare.map(([front, back]) => ({ front, back })) },
  });
  await postId(request, positions, vater, { exerciseId, stage: 4, cadence: "Daily", useLeitner: false, orderStrategy: "Serial" });

  await sohnLogin(page, { id: String(childId), pin });
  await pfadStart(page, "practice");
  const t = LERNKARTE_DE;

  // Karte 1: gesehen (Deutsch), aufgedeckt, abgeschrieben, aus dem Kopf geschrieben.
  await expect(zaehler(page)).toHaveText("Karte 1 / 2");
  await expect(page.getByRole("heading", { name: "Neues Wort – lerne es" })).toBeVisible();
  await expect(page.getByText(paare[0][1], { exact: true })).toBeVisible();
  expect(await lernkarteDurchspielen(page)).toBe(paare[0][0]);

  // Karte 2: im vierten Schritt „Weiß ich nicht“ – das Blatt sagt, dass das Wort wiederkommt, und die Runde wächst.
  await expect(zaehler(page)).toHaveText("Karte 2 / 2");
  const wortFeld = page.locator(".lernkarte-wort .rev");
  await expect(async () => {
    await page.getByRole("button", { name: t.aufdecken, exact: true }).click({ timeout: 2000 });
    await expect(wortFeld).toBeVisible({ timeout: 1000 });
  }).toPass();
  expect((await wortFeld.innerText()).replace(/^→\s*/, "").trim()).toBe(paare[1][0]);
  const kopie = page.getByLabel(t.abschreiben, { exact: true });
  await expect(async () => {
    await page.getByRole("button", { name: t.neu, exact: true }).click({ timeout: 2000 });
    await expect(kopie).toBeVisible({ timeout: 1000 });
  }).toPass();
  // Abschreiben verlangt das Wort, wie es dasteht: falsch abgeschrieben bleibt „OK“ gesperrt.
  await kopie.fill("lighthous");
  await expect(page.getByRole("button", { name: t.ok, exact: true })).toBeDisabled();
  await kopie.fill(paare[1][0]);
  await page.getByRole("button", { name: t.ok, exact: true }).click();
  // Im Erinnern-Schritt steht das Wort nicht mehr da.
  await expect(wortFeld).toHaveCount(0);
  await page.getByRole("button", { name: t.aufgeben, exact: true }).click();
  await expect(page.getByText("Das Wort kommt am Ende der Runde noch einmal.")).toBeVisible();
  await page.getByRole("button", { name: t.weiter, exact: true }).click();

  // Karte 3: dieselbe Lernkarte noch einmal – diesmal gewusst. Danach kein zweites Anhängen.
  await expect(zaehler(page)).toHaveText("Karte 3 / 3");
  await expect(page.getByText(paare[1][1], { exact: true })).toBeVisible();
  expect(await lernkarteDurchspielen(page)).toBe(paare[1][0]);

  // Die Abfrage danach: Deutsch gezeigt, Englisch getippt, vom Server als richtig gewertet.
  await page.getByRole("button", { name: /Jetzt abfragen/ }).click();
  for (const [i, [en, de]] of paare.entries()) {
    await expect(zaehler(page)).toHaveText(`Karte ${i + 1} / ${paare.length}`);
    await expect(page.getByText(de, { exact: true })).toBeVisible();
    await page.locator(".lektion-feld").fill(en);
    const [res] = await Promise.all([
      page.waitForResponse((r) => r.url().includes("/review") && r.request().method() === "POST"),
      page.keyboard.press("Enter"),
    ]);
    expect((await res.json()).wasCorrect, `„${en}“ für „${de}“`).toBe(true);
    await page.getByRole("button", { name: "Weiter →", exact: true }).click();
  }
  await expect(page.getByText("RUNDE FERTIG!")).toBeVisible();
});
