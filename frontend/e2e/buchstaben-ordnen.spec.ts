import { test, expect, type APIRequestContext } from "@playwright/test";
import { introduceAndQuiz, sohnLogin, pfadStart } from "./helpers";

/*
 * B-05 – Buchstaben ordnen, aus dem Sohn-Sitz. Der Stoff entsteht über die API (Gegenstand ist das Spielen, nicht das
 * Anlegen): eine Vokabel-Übung mit einer Mehrwort- und drei Ein-Wort-Antworten, eine Position auf der Stufe
 * „Buchstaben ordnen“ (7) in **serieller** Reihenfolge – so ist jeder Kartenübergang vorhersagbar und prüfbar.
 * Der Sohn legt zwei Wörter per Tastatur, eines per Klick (AK 5: zwei gleichwertige Bedienarten), und die
 * Mehrwort-Antwort kommt als Buchstabenkästchen (AK 3).
 *
 * Der Fokus muss nach JEDEM Übergang auf der ersten Kachel der neuen Karte stehen (Review Lauf D, Fund 6), sonst
 * landen getippte Buchstaben auf `body`. Seit B-526 liegt zwischen zwei Karten das Blatt: nach der Antwort steht der
 * Fokus auf „Weiter“ (Enter trägt weiter), danach auf der ersten Kachel. Drei Wege führen zum Blatt:
 * 1. aus den Kästchen per Enter, 2. aus den Kacheln per Enter, 3. aus den Kacheln per Klick auf „Prüfen“.
 * Die Tastatur-Karten werden darum OHNE erneutes Fokussieren getippt.
 *
 * Eigenes Kind: je Kind ist genau ein aktiver Plan spielbar, und `full-flow` belegt Kind #1.
 */

const RUN = Date.now().toString(36).slice(-5);
/**
 * In Katalog-Reihenfolge, so spielt `orderStrategy: "Serial"` sie aus. Gezeigt wird das deutsche Wort, gelegt das
 * Fremdwort (B-597) – darum steht der Lauf-Stempel am deutschen Wort und die Antwort bleibt ein Wort.
 */
const CARDS: [prompt: string, answer: string, bedienung: "kaesten" | "tastatur" | "klick"][] = [
  [`die Stadt ${RUN}`, "the town", "kaesten"],
  [`kaufen ${RUN}`, "buy", "tastatur"],
  [`essen ${RUN}`, "eat", "klick"],
  [`gehen ${RUN}`, "go", "tastatur"],
];

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown, key = "id") {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json())[key] as number;
}

test("Sohn ordnet Buchstaben per Tastatur und per Klick, der Fokus folgt jeder Karte, eine Mehrwort-Antwort kommt als Kästchen", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const childPin = "7531";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `Ordner ${RUN}`, pin: childPin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Ordnen-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Ordnen-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Unit 1", orderIndex: 1 });
  const exerciseId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/vocabulary`, headers, {
    title: `Ordnen ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de",
      items: CARDS.map(([prompt, answer]) => ({ front: answer, back: prompt })) },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers,
    { childId, title: `Ordnen-Plan ${RUN}`, durationDays: 7, active: true });
  // Ohne Leitner (B-476): mit Leitner käme die Abfrage-Runde frisch eingeführter Wörter als Umdrehkarte (Fach 1), nicht
  // als Buchstaben ordnen. Diese Spec prüft die Bedienung der Stufe; die Leiter selbst prüft `erstbegegnung.spec.ts`.
  await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, headers,
    { exerciseId, stage: 7, cadence: "Daily", useLeitner: false, orderStrategy: "Serial" });

  await sohnLogin(page, { id: String(childId), pin: childPin });
  await pfadStart(page, "practice");
  const counter = page.locator(".pill.cyan", { hasText: /Karte \d+ \/ \d+/ });
  // B-419: neue Wörter kommen zuerst zum Kennenlernen; die Buchstaben ordnet erst die Abfrage-Runde.
  await introduceAndQuiz(page, CARDS.length);

  // Jede Antwort muss der Server als richtig werten (B-283, Fund b): durchgeklickt ist nicht gelöst.
  async function pruefen(ausloesen: () => Promise<void>) {
    const [res] = await Promise.all([
      page.waitForResponse((r) => r.url().includes("/review") && r.request().method() === "POST"),
      ausloesen(),
    ]);
    expect((await res.json()).wasCorrect, "der Server wertet die Antwort als richtig").toBe(true);
    // B-526: das grüne Blatt hält die Karte an; der Fokus steht auf „Weiter“, Enter trägt zur nächsten.
    await expect(page.locator(".lektion-blatt.richtig")).toBeVisible();
    await expect(page.getByRole("button", { name: "Weiter →" })).toBeFocused();
    await page.keyboard.press("Enter");
  }

  for (const [i, [prompt, answer, bedienung]] of CARDS.entries()) {
    await expect(counter).toHaveText(new RegExp(`Karte ${i + 1} / 4`));
    await expect(page.getByText(prompt, { exact: true })).toBeVisible();
    const pool = page.getByRole("group", { name: "Buchstaben zum Ordnen" });

    if (bedienung === "kaesten") {
      // AK 3: keine Kacheln, sondern Buchstabenkästchen – das Leerzeichen steht schon fest.
      await expect(pool).toHaveCount(0);
      const boxes = page.locator(".letterboxes input");
      await expect(boxes).toHaveCount(answer.replace(" ", "").length);
      await boxes.first().click();
      await page.keyboard.type(answer.replace(" ", ""));
      await pruefen(() => page.keyboard.press("Enter"));
      continue;
    }

    await expect(pool.getByRole("button")).toHaveCount(answer.length);
    // Der Fokus ist schon da – niemand hat ihn gesetzt, die Karte hat ihn übernommen.
    await expect(pool.getByRole("button").first()).toBeFocused();
    if (bedienung === "tastatur") {
      await page.keyboard.type(answer);
    } else {
      // Klick: je Buchstabe die erste noch freie Kachel.
      for (const ch of answer) {
        await pool.locator(`button[aria-label="Buchstabe ${ch}"][aria-disabled="false"]`).first().click();
      }
    }
    // Gelegt ist genau das Wort – angesagt in der Live-Region, Buchstabe für Buchstabe.
    await expect(page.getByRole("status").filter({ hasText: /^Gelegt:/ })).toHaveText(`Gelegt: ${answer.split("").join(" ")}`);
    if (bedienung === "tastatur") await pruefen(() => page.keyboard.press("Enter"));
    else await pruefen(() => page.getByRole("button", { name: "Prüfen" }).click());
  }

  await expect(page.getByText("RUNDE FERTIG!")).toBeVisible();
});
