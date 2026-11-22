import { test, expect, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-791, Rollengang aus der Vater-Rolle: Plan-Beschreibung, Grammatik-Notiz einer Unit und Aussprache-Audio einer Vokabel
 * sind nach dem Leeren und Speichern wirklich leer – vorher meldete das Vater-Web Erfolg, und der alte Text stand nach dem
 * Neuladen wieder da (dieselbe Falle wie B-789 im Shop). Der Band der Unit (B-800) läuft im selben Gang mit.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);

async function call(page: Page, method: "get" | "post" | "patch", url: string, data?: unknown) {
  const token = await page.evaluate(() => localStorage.getItem("pugling.token"));
  const res = await page.request[method](url, { data, headers: { Authorization: `Bearer ${token}` } });
  expect(res.ok(), `${method.toUpperCase()} ${url}: ${res.status()}`).toBeTruthy();
  return res.json();
}

test("Geleerte Plan-Beschreibung, Unit-Grammatik und Audio-URL bleiben nach dem Neuladen leer", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const subjects: { id: number; name: string }[] = await call(page, "get", "/api/v1/creator/subjects");
  const subjectId = subjects.find((s) => s.name === "Englisch")!.id;

  // Plan: Beschreibung leeren.
  const plan = await call(page, "post", "/api/v1/supervisor/study-plans",
    { childId: 1, title: `Leeren ${RUN}`, subjectId, durationDays: 5, description: `Erst Vokabeln ${RUN}` });
  await page.goto(`/vater/plan/${plan.id}`);
  await expect(page.getByText(`Erst Vokabeln ${RUN}`)).toBeVisible();
  await page.getByRole("button", { name: "Bearbeiten" }).click();
  await page.getByLabel(/^Beschreibung/).fill("");
  await page.getByRole("button", { name: "Speichern" }).click();
  await expect(page.getByText(`Erst Vokabeln ${RUN}`)).toHaveCount(0);
  await page.reload();
  await expect(page.getByRole("heading", { name: new RegExp(`Leeren ${RUN}`) })).toBeVisible();
  await expect(page.getByText(`Erst Vokabeln ${RUN}`)).toHaveCount(0);

  // Unit: Grammatik leeren.
  const reihe = `Leeren-Reihe ${RUN}`;
  const series = await call(page, "post", "/api/v1/creator/textbook-series", { name: reihe, subjectId });
  await call(page, "post", `/api/v1/creator/textbook-series/${series.id}/units`,
    { label: `Unit ${RUN}`, orderIndex: 1, grade: 6, grammar: `simple past ${RUN}` });
  const unitsOeffnen = async () => {
    await page.goto("/vater/lehrwerke");
    await page.getByRole("row", { name: new RegExp(reihe) }).getByRole("button", { name: "Units" }).click();
  };
  await unitsOeffnen();
  await expect(page.getByText(`Grammatik: simple past ${RUN}`)).toBeVisible();
  await page.getByRole("button", { name: `Unit ${RUN} bearbeiten` }).click();
  // Das Bearbeiten-Formular, nicht das Anlege-Formular darunter: nur jenes trägt „Unit löschen".
  const bearbeiten = page.locator("form").filter({ has: page.getByRole("button", { name: "Unit löschen" }) });
  await bearbeiten.getByLabel("Grammatik der Unit", { exact: true }).fill("");
  await bearbeiten.getByLabel(/^Band/).fill(""); // B-800: der Band geht über den Schalter
  await bearbeiten.getByRole("button", { name: "Speichern" }).click();
  await expect(page.getByText(`Grammatik: simple past ${RUN}`)).toHaveCount(0);
  await unitsOeffnen();
  await expect(page.getByRole("button", { name: `Unit ${RUN} bearbeiten` })).toBeVisible();
  await expect(page.getByText(`Grammatik: simple past ${RUN}`)).toHaveCount(0);
  const units: { label: string; grade: number | null }[] = await call(page, "get", `/api/v1/creator/textbook-series/${series.id}/units`);
  expect(units.find((u) => u.label === `Unit ${RUN}`)?.grade, "Band nach dem Leeren").toBeNull();

  // Vokabel: Audio-URL leeren.
  const wort = `tune${RUN}`;
  const vocab = await call(page, "post", "/api/v1/creator/vocabulary", {
    sourceLanguage: "en", targetLanguage: "de", word: wort, translation: "Melodie",
    pronunciationAudioUrl: `https://example.org/${wort}.mp3`,
  });
  await page.goto(`/vater/vocab?search=${wort}`);
  await page.getByRole("row", { name: new RegExp(wort) }).getByRole("button", { name: "Bearbeiten" }).click();
  await expect(page.getByLabel("Aussprache-Audio-URL", { exact: true })).toHaveValue(`https://example.org/${wort}.mp3`);
  await page.getByLabel("Aussprache-Audio-URL", { exact: true }).fill("");
  await page.getByRole("button", { name: "OK" }).click();
  await expect(page.getByRole("button", { name: "OK" })).toHaveCount(0);
  const nachher = await call(page, "get", `/api/v1/creator/vocabulary/${vocab.id}`);
  expect(nachher.pronunciationAudioUrl).toBeNull();
  await page.reload();
  await page.getByRole("row", { name: new RegExp(wort) }).getByRole("button", { name: "Bearbeiten" }).click();
  await expect(page.getByLabel("Aussprache-Audio-URL", { exact: true })).toHaveValue("");
});
