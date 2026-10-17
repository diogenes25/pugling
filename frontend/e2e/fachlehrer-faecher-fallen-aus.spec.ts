import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-185. Beim Anlegen eines Fachlehrers leitet die gewählte Reihe das Fach ab. War die Fächerliste nicht
 * geladen, hatte dieser Wert keine Option: Das Feld fiel auf „– fachneutral –", der Hinweis daneben sagte
 * „aus dem Lehrwerk übernommen", und der POST trug die Id trotzdem. Drei Aussagen, zwei davon falsch.
 *
 * Nur die Fächerliste wird abgeschossen (`…/creator/subjects`, ohne Pfad dahinter); die Reihen laden, sonst
 * gäbe es nichts abzuleiten. Die Seite nennt den Ausfall schon („Fächer: …", B-172) – neu ist, dass das
 * Feld ihm nicht mehr widerspricht.
 *
 * Das ist zugleich der Rollengang aus der Creator-Rolle.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);

test("Fällt die Fächerliste aus, zeigt das Fach-Feld das abgeleitete Fach – und der POST trägt dieselbe Id", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.route("**/api/v1/creator/subjects", (route) => route.abort("failed"));
  await page.goto("/vater/fachlehrer");
  await expect(page.getByText(/^Fächer:/)).toBeVisible();

  // Eine geseedete Reihe mit Katalog-Fach: „Green Line 1" (Englisch). Über den Optionstext gegriffen, weil
  // die Beschriftung den Verlag mitträgt und dessen Name nicht zur Spec gehört.
  const reihe = page.locator("#fl-series-new");
  await expect(reihe.locator("option", { hasText: "Green Line 1" })).toHaveCount(1);
  const wert = await reihe.locator("option", { hasText: "Green Line 1" }).getAttribute("value");
  await reihe.selectOption(wert!);

  // Der Kern: Das Feld zeigt das abgeleitete Fach als gewählte Option – nicht „– fachneutral –".
  const fach = page.locator("#fl-subject-new");
  await expect(fach).toHaveValue(/^\d+$/);
  await expect(fach.locator("option:checked")).toHaveText("Englisch (nicht geladen)");
  await expect(page.getByText("aus dem Lehrwerk übernommen").first()).toBeVisible();

  // Und was gespeichert wird, ist dasselbe, was das Feld zeigt.
  await page.locator("#fl-name-new").fill(`Fächer-Ausfall ${RUN}`);
  // Stillgelegt anlegen: Die Specs teilen eine Datenbank, und ein aktives Englisch-Profil ohne Klassen- und
  // Schulart-Grenze passt zu JEDEM Kind – `lehrwerke.spec.ts` prüft für ein frisches Kind „Kein Profil passt",
  // und das stimmte im vollen Lauf plötzlich nicht mehr (gemessen 2026-10-17). Ein stillgelegtes Profil ist
  // ein harter Ausschluss im Matching und ändert an der hier geprüften Id nichts.
  await page.getByRole("checkbox", { name: /^Aktiv/ }).uncheck();
  const post = page.waitForRequest((r) => r.method() === "POST" && r.url().endsWith("/api/v1/creator/profiles"));
  await page.getByRole("button", { name: "Fachlehrer anlegen" }).click();
  const rumpf = JSON.parse((await post).postData() ?? "{}") as { subjectId?: number | null };
  expect(String(rumpf.subjectId)).toBe(await fach.inputValue());
  await expect(page.getByText("Fachlehrer angelegt.")).toBeVisible();
});
