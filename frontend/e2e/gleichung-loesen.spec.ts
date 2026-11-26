import { test, expect, type APIRequestContext } from "@playwright/test";
import { pfadStart, sohnLogin, tippen, vaterLogin } from "./helpers";

/*
 * B-710/B-712 – Vater legt eine „Gleichung lösen“-Übung im Editor an (nur Koeffizienten), der Sohn gibt die Lösungsmengen im
 * Tagestest in getrennte Felder ein bzw. tippt „keine Lösung“ an. Die Wertung hält der Server (`QuadraticEquationExerciseTypeTests`,
 * `QuadraticEquationApiTests`); die Spec belegt den Weg vom Formular bis zur Karte: die Config trägt nur a, b, c und die
 * Toleranz, und „2;4“ sowie „keine Lösung“ kommen unverändert als richtige Antworten an. Rot, wenn das Formular den Typ
 * nicht anlegen kann, ein Feld verliert (die zweite Gleichung hätte eine Lösung) oder das Spielen am Server vorbei wertet.
 *
 * Eigenes Kind: je Kind und Fach ist genau ein aktiver Plan spielbar.
 */

const RUN = Date.now().toString(36).slice(-5);

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

test("Gleichung lösen: Vater legt per Koeffizienten an, Sohn tippt die Lösungsmengen", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const childPin = "8710";
  const childId = await postId(request, "/api/v1/supervisor/children", headers, { name: `Gleichung ${RUN}`, pin: childPin });
  const subjectName = `Gleichung-Mathe ${RUN}`;
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: subjectName });
  const seriesName = `Gleichung-Reihe ${RUN}`;
  const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
    name: seriesName, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
    sourceLanguage: null, targetLanguage: null, notes: null,
  });
  await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers, { label: "Kapitel IV", orderIndex: 1 });

  // ---------- Vater: Editor, zwei Gleichungen ----------
  await vaterLogin(page, { id: "1", pin: "0000" });
  await page.goto("/vater/exercises/neu");
  await page.locator('select[aria-label="Fach"]').selectOption({ label: subjectName });
  await page.locator('select[aria-label="Reihe"]').selectOption({ label: seriesName });
  await page.locator('select[aria-label="Unit"]').selectOption({ label: "Kapitel IV" });
  await page.locator('select[aria-label="Übungstyp"]').selectOption("QuadraticEquation");
  const title = `Normalform ${RUN}`;
  await page.locator("#ex-title").fill(title);
  await page.getByLabel("a (vor x²)").fill("1");
  await page.getByLabel("b (vor x)").fill("-6");
  await page.getByLabel("c", { exact: true }).fill("8");
  await expect(page.getByText("x² − 6x + 8 = 0")).toBeVisible();
  await page.getByRole("button", { name: "+ Gleichung" }).click();
  await page.getByLabel("a (vor x²)").nth(1).fill("1");
  await page.getByLabel("b (vor x)").nth(1).fill("0");
  await page.getByLabel("c", { exact: true }).nth(1).fill("1");
  await page.getByRole("button", { name: "Übung anlegen" }).click();
  await expect(page.getByText(`Übung „${title}" angelegt.`)).toBeVisible();

  // Was das Formular geschickt hat: nur Koeffizienten und Toleranz, kein Lösungsfeld.
  const base = `/api/v1/creator/textbook-series/${seriesId}/units`;
  const units = await (await request.get(base, { headers })).json() as { id: number }[];
  const unitId = units[0].id;
  const list = await (await request.get(`${base}/${unitId}/quadratic-equation`, { headers })).json() as
    { id: number; title: string; config: { equations: Record<string, number>[] } }[];
  const exercise = list.find((e) => e.title === title)!;
  expect(exercise.config.equations).toEqual([
    { a: 1, b: -6, c: 8, tolerance: 0.01 },
    { a: 1, b: 0, c: 1, tolerance: 0.01 },
  ]);

  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers,
    { childId, subjectId, title: `Gleichung-Plan ${RUN}`, durationDays: 7, active: true });
  const positionId = await postId(request, `/api/v1/supervisor/study-plans/${planId}/positions`, headers,
    { exerciseId: exercise.id, cadence: "Daily", useLeitner: false, orderStrategy: "Serial" });

  // ---------- Sohn: Tagestest im Textfeld ----------
  await sohnLogin(page, { id: String(childId), pin: childPin });
  await pfadStart(page, "test");
  await expect(page).toHaveURL(new RegExp(`/sohn/test/${positionId}$`));

  // B-712: zwei Zahlenfelder statt eines Textfelds mit Trenner-Regel – „4“ und „2“ gehen als „2;4“ hinaus.
  const erstes = page.getByRole("textbox", { name: "Lösung 1", exact: true });
  const zweites = page.getByRole("textbox", { name: "Lösung 2", exact: true });
  await expect(page.locator(".word")).toContainText("x^2 - 6x + 8 = 0");
  await tippen(erstes, "4");
  await tippen(zweites, "2");
  await zweites.press("Enter");

  // Die zweite Gleichung hat keine Lösung: der Umschalter sperrt die Felder und schickt den festen Text.
  await expect(page.locator(".word")).toContainText("x^2 + 1 = 0");
  await expect(erstes).toHaveValue("");
  const keine = page.getByRole("button", { name: "keine Lösung", exact: true });
  await keine.click();
  await expect(keine).toHaveAttribute("aria-pressed", "true");
  await page.getByRole("button", { name: "Weiter →", exact: true }).click();

  await expect(page.locator(".ring")).toContainText("2 / 2");
});
