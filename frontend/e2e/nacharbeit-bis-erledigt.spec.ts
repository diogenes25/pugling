import { test, expect, type APIRequestContext } from "@playwright/test";
import { sohnLogin, pfadStart, tippen, vaterLogin } from "./helpers";

/*
 * B-636/B-637 – die Nacharbeit als Pflicht „täglich, bis erledigt“, aus dem Vater-Sitz (zugleich der Rollengang Vater von
 * Sprint 86). Der Stoff entsteht über die API wie in `nacharbeit.spec.ts`; eingestellt wird im Vater-Web.
 *
 * Was jeder Schritt prüft – und welche Änderung ihn rot macht:
 * 1. „täglich, bis erledigt“ und der Abschlussbonus sind im Positionsformular wählbar und werden gespeichert → rot, wenn
 *    der Rhythmus fehlt, das Bonusfeld nicht erscheint oder der Bonus nicht in den PATCH reist.
 * 2. Die letzte frei getippte Aufgabe feiert den Abschluss mit dem Bonus → rot, wenn die Antwort `completionBonus` nicht
 *    trägt oder die Arcade ihn nicht zeigt.
 * 3. Der Report zeigt je Aufgabe Vorgabe, „erledigt“ und die Stufen, dazu „geschafft am“ → rot ohne die Nacharbeits-Sicht.
 *
 * Eigenes Kind: je Kind und Fach ist genau ein aktiver Plan spielbar.
 */

const RUN = Date.now().toString(36).slice(-5);
// ClozeStage.TranslationFreeText – die Stufe, auf der eine Nacharbeits-Aufgabe beginnt.
const FREITEXT = 3;

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

test("Nacharbeit bis erledigt: Vater stellt ein, Kind schafft sie, Bonus und Report stimmen", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const childPin = "6421";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `Abschluss ${RUN}`, pin: childPin });
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: `Abschluss-Fach ${RUN}` });
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: `Abschluss-Reihe ${RUN}`, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Unit 1", orderIndex: 1 });
  const exerciseId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units/${unitId}/cloze`, headers, {
    title: `Abschluss Test ${RUN}`, orderIndex: 1, rewardPoints: 10,
    config: {
      text: "{{1}}\nI fell {{2}} during the film.",
      gaps: [
        { index: 1, answer: "eastwards", prompt: `Richtung Osten ${RUN}` },
        { index: 2, answer: "asleep", prompt: "Während des Films schlief ich ein." },
      ],
    },
  });
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers,
    { childId, subjectId, title: `Abschluss-Plan ${RUN}`, durationDays: 7, active: true });
  await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, headers,
    { exerciseId, cadence: "Daily", useLeitner: false, remediation: true });

  // ---- 1. Vater: Rhythmus „täglich, bis erledigt“ mit Abschlussbonus ----
  await vaterLogin(page, { id: "1", pin: "0000" });
  await page.goto(`/vater/plan/${planId}`);
  const zeile = page.getByRole("row", { name: new RegExp(`Abschluss Test ${RUN}`) });
  await zeile.getByRole("button", { name: "Bearbeiten" }).click();
  await page.getByLabel("Ziel-Rhythmus", { exact: true }).selectOption({ label: "täglich, bis erledigt" });
  await page.getByLabel("Abschlussbonus 🪙", { exact: true }).fill("30");
  await page.getByRole("button", { name: "OK" }).click();
  await expect(zeile).toContainText("täglich, bis erledigt");
  await expect(zeile).toContainText("Abschluss +30🪙");

  // ---- 2. Sohn: beide Aufgaben frei getippt – die letzte schließt mit dem Bonus ab ----
  await sohnLogin(page, { id: String(childId), pin: childPin });
  await pfadStart(page, "practice");
  const feld = page.getByRole("textbox", { name: "Deine Antwort" });
  await tippen(feld, "eastwards");
  await page.keyboard.press("Enter");
  await page.getByRole("button", { name: "Weiter →" }).click();
  await tippen(feld, "asleep");
  await page.keyboard.press("Enter");
  // Dauerhaft, nicht nur im 2-s-Jubel (der ist aria-hidden): die Ansage und das Blatt nennen den Bonus.
  await expect(page.getByRole("status").filter({ hasText: "Nacharbeit geschafft! +30 🪙 Abschlussbonus" })).toHaveCount(1);
  await expect(page.locator(".lektion-blatt")).toContainText("🪙");

  // ---- 3. Vater: der Report zeigt je Aufgabe den Stand, die Zeile „geschafft am“ ----
  await vaterLogin(page, { id: "1", pin: "0000" });
  await page.goto(`/vater/plan/${planId}`);
  await expect(zeile).toContainText("geschafft am");
  await zeile.getByRole("button", { name: "📊 Report" }).click();
  await expect(page.getByText("Nacharbeit: 2/2 erledigt")).toBeVisible();
  // Die Zeile der inneren Report-Tabelle – die äußere Panel-Zeile enthält denselben Text und steht davor.
  const aufgabe = page.getByRole("row").filter({ hasText: `Richtung Osten ${RUN}` }).last();
  await expect(aufgabe).toContainText("erledigt");
  // Der Stufenname kommt aus dem Typ-Manifest, nicht aus dem Test: rot, wenn die Tabelle auf „Stufe n“ zurückfällt.
  const typen = await (await request.get("/api/v1/creator/exercise-types", { headers })).json();
  const cloze = typen.find((ty: { type: string }) => ty.type === "Cloze");
  const freitext = cloze.stages.find((st: { value: number; label: string }) => st.value === FREITEXT).label as string;
  await expect(aufgabe).toContainText(`${freitext} ×1`);
});
