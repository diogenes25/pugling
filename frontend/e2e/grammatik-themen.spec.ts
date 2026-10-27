import { test, expect, type APIRequestContext } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-155, AK 10: Grammatik als Thema statt Freitext. Der Creator legt im Anlege-Formular ein neues Thema an und
 * versieht zwei Übungen in **verschiedenen Reihen und Klassenstufen** damit; eine dritte Übung derselben Reihe
 * bekommt es nicht. Der Vater findet beim Planbau über die achte Facette „Grammatik-Thema“ genau die zwei.
 *
 * Katalog (Fach, zwei Reihen mit je einer Unit, Plan) entsteht über die API – Gegenstand ist das Thema, nicht das
 * Anlegen von Reihen (das hat `lehrwerke.spec.ts`). Eine Fläche je Spec (`frontend/CLAUDE.md`).
 *
 * Das ist zugleich der Rollengang aus der Creator- (Thema anlegen, zuweisen) und der Vater-Rolle (finden).
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString(36).slice(-5);
const TOPIC = `Present perfect ${RUN}`;
const WITH_A = `Perfekt A ${RUN}`;
const WITH_B = `Perfekt B ${RUN}`;
const WITHOUT = `Ohne Thema ${RUN}`;

async function postId(request: APIRequestContext, url: string, headers: Record<string, string>, data: unknown) {
  const res = await request.post(url, { headers, data });
  expect(res.ok(), await res.text()).toBeTruthy();
  return (await res.json()).id as number;
}

test("Der Creator ordnet Regelaufgaben einem Grammatik-Thema zu, und der Vater findet sie darüber", async ({ page, request }) => {
  const login = await request.post("/api/v1/auth/adult", { data: { adultId: 1, pin: "0000" } });
  expect(login.ok(), await login.text()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };

  const subjectName = `Themen-Fach ${RUN}`;
  const subjectId = await postId(request, "/api/v1/creator/subjects", headers, { name: subjectName });
  const seriesOf = async (name: string, grade: number) => {
    const seriesId = await postId(request, "/api/v1/creator/textbook-series", headers, {
      name, publisherId: null, subjectName: null, subjectId, schoolTypes: null,
      sourceLanguage: null, targetLanguage: null, notes: null,
    });
    const unitId = await postId(request, `/api/v1/creator/textbook-series/${seriesId}/units`, headers,
      { label: `Unit ${grade}`, grade, orderIndex: 1 });
    return { seriesId, unitId, name, unitLabel: `Unit ${grade}` };
  };
  const seriesA = await seriesOf(`Reihe A ${RUN}`, 5);
  const seriesB = await seriesOf(`Reihe B ${RUN}`, 7);
  const planId = await postId(request, "/api/v1/supervisor/study-plans", headers,
    { childId: 1, title: `Themen-Plan ${RUN}`, durationDays: 7, active: false });

  await vaterLogin(page, FATHER);

  /** Eine Regelaufgabe im Anlege-Formular – mit oder ohne das Thema. */
  async function anlegen(series: typeof seriesA, title: string, mitThema: boolean) {
    await page.goto(`/vater/exercises/neu?subjectId=${subjectId}&seriesId=${series.seriesId}&seriesUnitId=${series.unitId}`);
    await expect(page.locator('select[aria-label="Unit"]')).toHaveValue(String(series.unitId));
    await page.locator('select[aria-label="Übungstyp"]').selectOption("Grammar");
    await page.locator("#ex-title").fill(title);
    await page.getByLabel("Anweisung", { exact: true }).fill("Setze das Present perfect ein.");
    await page.getByLabel("Aufgabe", { exact: true }).fill("She ___ (eat) already.");
    await page.getByLabel("Lösung", { exact: true }).fill("has eaten");
    if (mitThema) {
      if (await page.locator("#ex-grammar-topics").locator(`option:has-text("${TOPIC}")`).count() === 0) {
        // Das Thema entsteht nebenbei im Formular – idempotent über den Slug.
        await page.getByLabel("Neues Grammatik-Thema").fill(TOPIC);
        await page.getByRole("button", { name: "Thema anlegen" }).click();
        await expect(page.getByText(`„${TOPIC}“ steht im Vokabular.`)).toBeVisible();
      } else {
        await page.locator("#ex-grammar-topics").selectOption({ label: TOPIC });
        await page.getByRole("button", { name: "Thema hinzufügen" }).click();
      }
      await expect(page.getByRole("button", { name: `Thema „${TOPIC}“ entfernen` })).toBeVisible();
    } else {
      await expect(page.getByText(/Kein Grammatik-Thema/)).toBeVisible();
    }
    await page.getByRole("button", { name: "Übung anlegen" }).click();
    await expect(page.getByText(`Übung „${title}" angelegt.`)).toBeVisible();
  }

  await anlegen(seriesA, WITH_A, true);
  await anlegen(seriesB, WITH_B, true);
  await anlegen(seriesA, WITHOUT, false);

  // Rückweg (AK 7): der Bearbeiten-Dialog zeigt das Thema mit Namen.
  await page.goto(`/vater/exercises?subjectId=${subjectId}`);
  await page.locator('select[aria-label="Fach"]').selectOption({ label: subjectName });
  await page.locator('select[aria-label="Reihe"]').selectOption({ label: seriesB.name });
  await page.locator('select[aria-label="Unit"]').selectOption({ label: seriesB.unitLabel });
  await expect(page.getByText(WITH_B, { exact: true })).toBeVisible();
  // Genau eine Übung in dieser Unit – kein Griff per Index ins Ungewisse.
  await expect(page.getByRole("button", { name: "✏️ Bearbeiten" })).toHaveCount(1);
  await page.getByRole("button", { name: "✏️ Bearbeiten" }).click();
  await expect(page.locator("#ed-title")).toHaveValue(WITH_B);
  await expect(page.getByRole("button", { name: `Thema „${TOPIC}“ entfernen` })).toBeVisible();
  await page.getByRole("button", { name: "Schließen", exact: true }).click();

  // ---- Der Vater findet beim Planbau über das Thema: beide Reihen, beide Klassen, die dritte nicht ----
  await page.goto(`/vater/plan/${planId}`);
  await page.locator('select[aria-label="Fach-Filter"]').selectOption({ label: subjectName });
  await expect(page.getByRole("radio", { name: new RegExp(WITHOUT) })).toBeVisible();
  await page.locator('select[aria-label="Grammatik-Thema-Filter"]').selectOption({ label: TOPIC });
  await expect(page.getByRole("radio", { name: new RegExp(WITH_A) })).toBeVisible();
  await expect(page.getByRole("radio", { name: new RegExp(WITH_B) })).toBeVisible();
  await expect(page.getByRole("radio", { name: new RegExp(WITHOUT) })).toHaveCount(0);
  await expect(page.getByText("(2 Treffer)")).toBeVisible();
});
