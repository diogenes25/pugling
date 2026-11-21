import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin, pfadStart, tippen } from "./helpers";

/*
 * B-639 – Rollengang Sprint 104: Der Sohn tippt im Tagestest einer Rechen-Position Dezimalzahlen so, wie er sie in der
 * Schule schreibt, mit Komma. „2,5“ für 5 : 2 und „2,3“ für 7 : 3 (gerundet, Toleranz 0,05) zählen, „2,5“ für 5 · 5
 * nicht: 2 von 3. Die Regel hält der Server (`ZahlenantwortTests`, `PositionTestFlowTests.Test_Rechenaufgabe_Mit_Komma_Und_Toleranz`);
 * die Spec belegt, dass die Eingabe aus der Arcade unverändert dort ankommt. Rot, solange das Spielen Text vergleicht
 * (0 von 3) oder „2,5“ wieder als 25 liest (3 von 3).
 *
 * Eigenes Kind: je Kind und Fach ist genau ein aktiver Plan spielbar.
 */

const RUN = Date.now().toString(36).slice(-5);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

test("Tagestest Rechnen: Dezimalzahlen mit Komma zählen", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const childPin = "8639";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `Komma ${RUN}`, pin: childPin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Komma-Mathe ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Komma-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Kapitel IV", orderIndex: 1 });
  const exerciseId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/arithmetic`, headers, {
    title: `Dezimalzahlen ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: {
      problems: [
        { prompt: "5 : 2", answer: 2.5, tolerance: 0 },
        { prompt: "7 : 3 (auf eine Stelle)", answer: 2.33, tolerance: 0.05 },
        { prompt: "5 · 5", answer: 25, tolerance: 0 },
      ],
    },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers,
    { childId, subjectId, title: `Komma-Plan ${RUN}`, durationDays: 7, active: true });
  const positionId = await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, headers,
    { exerciseId, stage: 4, cadence: "Daily", useLeitner: false, orderStrategy: "Serial" });

  await sohnLogin(page, { id: String(childId), pin: childPin });
  await pfadStart(page, "test");
  await expect(page).toHaveURL(new RegExp(`/sohn/test/${positionId}$`));

  const feld = page.getByRole("textbox", { name: "Lösung" });
  for (const [aufgabe, antwort] of [["5 : 2", "2,5"], ["7 : 3 (auf eine Stelle)", "2,3"], ["5 · 5", "2,5"]]) {
    await expect(page.locator(".word")).toHaveText(aufgabe);
    await tippen(feld, antwort);
    await feld.press("Enter");
  }

  const ring = page.locator(".ring");
  await expect(ring).toContainText("2 / 3");
});
