import { test, expect, type Page } from "@playwright/test";
import { createChild, sohnLogin, vaterLogin } from "./helpers";

/*
 * B-425 + B-427 (NP-27): Der Vater liest im Konto je Buchung die Währung und überall dieselbe deutsche Schreibweise eines
 * Datums; der Sohn sieht in „Mein Konto" dieselbe Währung. Rollengang Vater/Sohn mit einem frischen Kind je Fall, damit die
 * Buchungen und der Plan dieser Spec die einzigen sind. Zwei Fälle, weil es zwei Flächen sind (Konto, Plan): ein Rot im einen
 * nimmt den anderen nicht mit.
 *
 * Rot, wenn eine Gem-Buchung ohne 💎 erscheint, die Spalte wieder „Münzen" heißt oder ein Datum im Browser-Format bzw. als ISO
 * erscheint.
 *
 * Die Locale des Vater-Browsers steht fest auf en-US: ohne sie entschiede der Rechner (lokal Deutsch, CI Linux en-US), und ab
 * dem 10. eines Monats passte selbst ein deutsches `toLocaleDateString()` („10.11.2026") auf das Muster – die Zusicherung
 * trüge dann nichts. Die Arcade-Sprache hängt am localStorage (`playwright.config.ts`), nicht an der Locale.
 */

test.use({ locale: "en-US" });

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);
const DEUTSCH = /^\d{2}\.\d{2}\.\d{4}$/;

async function api(page: Page, url: string, data: unknown) {
  const token = await page.evaluate(() => localStorage.getItem("pugling.token"));
  const res = await page.request.post(url, { data, headers: { Authorization: `Bearer ${token}` } });
  expect(res.ok(), `POST ${url}: ${res.status()}`).toBeTruthy();
  return res.json();
}

test("Konto nennt die Währung je Buchung und das Datum deutsch – beim Vater und beim Sohn", async ({ page, browser }) => {
  await vaterLogin(page, FATHER);
  const childId = await createChild(page, `Kontokind ${RUN}`, "4826");
  await api(page, `/api/v1/supervisor/children/${childId}/points`, { amount: 9, reason: `Gems ${RUN}`, currency: "Gems" });
  await api(page, `/api/v1/supervisor/children/${childId}/points`, { amount: 7, reason: `Münzen ${RUN}`, currency: "Coins" });

  // ---------- Vater: Konto ----------
  await page.goto(`/vater/konto?childId=${childId}`);
  await expect(page.getByRole("columnheader", { name: "Betrag" })).toBeVisible();
  const gemZeile = page.getByRole("row", { name: new RegExp(`Gems ${RUN}`) });
  await expect(gemZeile).toContainText("+9 💎");
  await expect(page.getByRole("row", { name: new RegExp(`Münzen ${RUN}`) })).toContainText("+7 🪙");
  await expect(gemZeile.getByRole("cell").first()).toHaveText(DEUTSCH);

  // ---------- Sohn: Mein Konto ----------
  const sohnCtx = await browser.newContext();
  const sohn = await sohnCtx.newPage();
  await sohnLogin(sohn, { id: childId, pin: "4826" });
  // Über den Link der Arcade, nicht per goto: der käme vor dem Ende der Anmeldung und landete wieder auf dem Login.
  await sohn.getByRole("link", { name: /Konto/ }).click();
  await expect(sohn.locator(".list .row", { hasText: `Gems ${RUN}` })).toContainText("+9 💎");
  await sohnCtx.close();
});

test("Plan-Zeitraum und Tagesverlauf stehen deutsch – auf der Plan-Seite und in der Planliste", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const childId = await createChild(page, `Datumskind ${RUN}`, "4827");
  const titel = `Datumsplan ${RUN}`;
  // Der Start liegt in der Vergangenheit, damit der Tagesverlauf Tage hat.
  const plan = await api(page, "/api/v1/supervisor/study-plans", {
    childId: Number(childId), title: titel, subjectId: null, durationDays: 14, startDate: "2026-11-08",
  });

  await page.goto(`/vater/plan/${plan.id}`);
  await expect(page.getByText("08.11.2026 – 21.11.2026")).toBeVisible();
  const ersterTag = page.locator("section", { has: page.getByRole("heading", { name: "Tagesverlauf" }) })
    .getByRole("row").nth(1).getByRole("cell").first();
  await expect(ersterTag).toHaveText(DEUTSCH);

  await page.goto(`/vater/plaene?childId=${childId}`);
  await expect(page.getByRole("row", { name: new RegExp(titel) })).toContainText("08.11.2026 – 21.11.2026");
});

test("Klassenarbeits-Termin und Ziel-Frist stehen deutsch", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const childId = await createChild(page, `Terminkind ${RUN}`, "4828");
  const arbeit = `Termin-Arbeit ${RUN}`;
  await api(page, "/api/v1/supervisor/class-tests", { childId: Number(childId), title: arbeit, scheduledDate: "2026-11-20" });
  const token = await page.evaluate(() => localStorage.getItem("pugling.token"));
  const subjects = await (await page.request.get("/api/v1/creator/subjects", { headers: { Authorization: `Bearer ${token}` } })).json();
  const englisch = subjects.find((s: { name: string }) => s.name === "Englisch");
  await api(page, `/api/v1/supervisor/children/${childId}/objectives`, {
    title: `Frist-Ziel ${RUN}`, motivation: null, kind: "Committed", start: "2026-11-09", dueDate: "2026-11-30",
    rewardOnComplete: 0, rewardPerKeyResult: 0,
    keyResults: [{ subjectId: englisch.id, seriesUnitId: null, exerciseId: null, metric: "AvgMastery", targetValue: 80, title: null }],
  });

  await page.goto(`/vater/class-tests?childId=${childId}`);
  await expect(page.getByRole("row", { name: new RegExp(arbeit) })).toContainText("20.11.2026");

  await page.goto(`/vater/kind/${childId}/ziele`);
  await expect(page.getByText(/09\.11\.2026 bis 30\.11\.2026/)).toBeVisible();
});
