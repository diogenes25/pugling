import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin, pfadStart } from "./helpers";

/*
 * B-635 – die Nacharbeit eines verpatzten Tests, aus dem Sohn-Sitz (zugleich der Rollengang Sohn von Sprint 85). Der Stoff
 * entsteht über die API: ein Lückentext mit drei Aufgaben, je mit der deutschen Vorgabe aus dem Test (B-633), als Position
 * mit „Nacharbeit“.
 *
 * Was jeder Schritt prüft – und welche Änderung ihn rot macht:
 * 1. Die Vorgabe steht offen über dem Text → rot, wenn `Gap.Prompt` die Karte nicht als `passage` erreicht.
 * 2. „Mach's mir leichter“ macht aus dem Feld Kästchen → rot ohne den Knopf oder ohne `easier`.
 * 3. Richtig mit Hilfe sagt „noch nicht erledigt“, der Zähler bleibt → rot, wenn die leichtere Stufe erledigt.
 * 4. Frei getippt richtig erledigt, der Zähler zählt mit; die Mehrwort-Lösung „as soon as“ zählt wie eine.
 * 5. Die nächste Runde bringt nur die offene Aufgabe (Karte 1 / 1) → rot, wenn die Runde wieder alles bringt.
 *
 * Eigenes Kind: je Kind und Fach ist genau ein aktiver Plan spielbar.
 */

const RUN = Date.now().toString(36).slice(-5);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

test("Nacharbeit: leichter schalten erledigt nicht, frei getippt schon, die nächste Runde bringt nur Offenes", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const childPin = "6420";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `Nacharbeiter ${RUN}`, pin: childPin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Nacharbeit-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Nacharbeit-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Unit 1", orderIndex: 1 });
  const exerciseId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/cloze`, headers, {
    title: `Nacharbeit Test 1 ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: {
      text: "{{1}}\nI fell {{2}} during the film.\nCall me {{3}} you can.",
      gaps: [
        { index: 1, answer: "eastwards", prompt: `Richtung Osten ${RUN}` },
        { index: 2, answer: "asleep", prompt: "Während des Films schlief ich ein." },
        { index: 3, answer: "as soon as", prompt: "Ruf mich an, sobald du kannst." },
      ],
    },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers,
    { childId, subjectId, title: `Nacharbeit-Plan ${RUN}`, durationDays: 7, active: true });
  await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, headers,
    { exerciseId, cadence: "Daily", useLeitner: false, remediation: true });

  await sohnLogin(page, { id: String(childId), pin: childPin });
  await pfadStart(page, "practice");
  const zaehler = page.locator(".pill.cyan", { hasText: /Karte \d+ \/ \d+/ });
  const stand = page.locator(".pill.cyan", { hasText: /^Runde/ });
  const feld = page.getByRole("textbox", { name: "Deine Antwort" });

  // Aufgabe 1: festgehangen – einmal leichter, dann richtig in den Kästchen. Richtig, aber nicht erledigt.
  await expect(zaehler).toHaveText("Karte 1 / 3");
  await expect(stand).toHaveText("Runde 1 · 0 von 3 erledigt");
  await expect(page.getByRole("group", { name: "Text zur Aufgabe" })).toHaveText(`Richtung Osten ${RUN}`);
  await expect(feld).toBeVisible();
  await page.getByRole("button", { name: "Mach's mir leichter" }).click();
  const kaesten = page.locator(".letterboxes input");
  await expect(kaesten).toHaveCount("eastwards".length);
  // Nach dem Leichterschalten steht der Fokus in der neuen Form (Fund 3) – getippt wird ohne Klick.
  await expect(kaesten.first()).toBeFocused();
  await page.keyboard.type("eastwards");
  await page.keyboard.press("Enter");
  await expect(page.locator(".lektion-blatt.richtig")).toBeVisible();
  await expect(page.locator(".lektion-blatt")).toContainText("Noch nicht erledigt");
  await expect(stand).toHaveText("Runde 1 · 0 von 3 erledigt");
  await page.getByRole("button", { name: "Weiter →" }).click();

  // Aufgaben 2 und 3: frei getippt – erledigt, der Zähler wächst. Nach „Weiter“ steht der Fokus im Antwortfeld, nicht auf
  // „Mach's mir leichter“ (Review Sprint 85, Fund 1): getippt wird ohne erneutes Anklicken.
  for (const [n, antwort] of [[2, "asleep"], [3, "as soon as"]] as const) {
    await expect(zaehler).toHaveText(`Karte ${n} / 3`);
    await expect(feld).toBeFocused();
    await page.keyboard.type(antwort);
    await page.keyboard.press("Enter");
    await expect(page.locator(".lektion-blatt.richtig")).toBeVisible();
    await expect(page.locator(".lektion-blatt")).not.toContainText("Noch nicht erledigt");
    await expect(stand).toHaveText(`Runde 1 · ${n - 1} von 3 erledigt`);
    await page.getByRole("button", { name: "Weiter →" }).click();
  }
  await expect(page.getByText("RUNDE FERTIG!")).toBeVisible();

  // Die nächste Runde: nur die Aufgabe, die noch offen ist.
  await page.goto("/sohn");
  await pfadStart(page, "practice");
  await expect(zaehler).toHaveText("Karte 1 / 1");
  await expect(stand).toHaveText("Runde 2 · 2 von 3 erledigt");
  await expect(page.getByRole("group", { name: "Text zur Aufgabe" })).toHaveText(`Richtung Osten ${RUN}`);
  await feld.fill("eastwards");
  await page.keyboard.press("Enter");
  await expect(stand).toHaveText("Runde 2 · 3 von 3 erledigt");
});
