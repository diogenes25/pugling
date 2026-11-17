import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin, pfadStart } from "./helpers";

/*
 * B-526 – der Lektionsbildschirm: eine Auswahl wird nur markiert, gewertet wird mit dem Hauptknopf. Die Regeln der
 * Antwortfläche prüft `Lektion.test.tsx`; hier liegt, was nur die laufende App zeigt: dass der Server wirklich die
 * ZULETZT markierte Option bekommt. Die erste Markierung ist darum absichtlich die falsche – bliebe sie kleben oder
 * wertete schon der erste Tipp, endete der Test nicht mit zwei von zwei.
 *
 * Stoff über die API: zwei Vokabeln, Stufe 6 (Auswahl), ohne Karteikasten. Eigenes Kind: je Kind ein spielbarer Plan.
 */

const RUN = Date.now().toString(36).slice(-5);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

test("Test mit Auswahl: markieren, umentscheiden, „Weiter“ – gewertet wird die letzte Wahl", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const pin = "8653";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `Lektion ${RUN}`, pin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Lektion-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Lektion-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Unit 1", orderIndex: 1 });
  const PAARE: [string, string][] = [[`tree ${RUN}`, `Baum ${RUN}`], [`house ${RUN}`, `Haus ${RUN}`]];
  const exerciseId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/vocabulary`, headers, {
    title: `Auswahl ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: { direction: "front-to-back", sourceLang: "en", targetLang: "de", items: PAARE.map(([front, back]) => ({ front, back })) },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers, { childId, subjectId, title: `Lektion-Plan ${RUN}`, durationDays: 7, active: true });
  await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, headers,
    { exerciseId, stage: 6, cadence: "Daily", useLeitner: false, orderStrategy: "Serial" });

  await sohnLogin(page, { id: String(childId), pin });
  await pfadStart(page, "test");

  const zaehler = page.locator(".pill.cyan");
  const weiter = page.getByRole("button", { name: "Weiter →", exact: true });
  for (let frage = 0; frage < PAARE.length; frage++) {
    const [wort, richtig] = PAARE[frage];
    const falsch = PAARE[1 - frage][1];
    await expect(zaehler).toHaveText(`Frage ${frage + 1} / ${PAARE.length}`);
    await expect(page.getByRole("heading", { name: "Test · Wähle die richtige Antwort" })).toBeVisible();
    await expect(page.locator(".fcard", { hasText: wort })).toBeVisible();
    // Ohne Markierung ist der Hauptknopf gesperrt.
    await expect(weiter).toBeDisabled();

    // Erst die falsche Option: das wertet noch nichts – der Zähler bleibt stehen.
    await page.getByRole("button", { name: falsch, exact: true }).click();
    await expect(page.getByRole("button", { name: falsch, exact: true })).toHaveAttribute("aria-pressed", "true");
    await expect(zaehler).toHaveText(`Frage ${frage + 1} / ${PAARE.length}`);
    // Umentscheiden, dann werten.
    await page.getByRole("button", { name: richtig, exact: true }).click();
    await expect(page.getByRole("button", { name: falsch, exact: true })).toHaveAttribute("aria-pressed", "false");
    await weiter.click();
  }

  // Zwei von zwei: der Server hat je Frage die zuletzt markierte Option bekommen, nicht die erste.
  await expect(page.locator(".vtitle", { hasText: "SIEG!" })).toBeVisible();
  await expect(page.locator(".ring")).toContainText(`${PAARE.length} / ${PAARE.length}`);
});
