import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-229. Das Plan-Formular lädt die Kinderliste erst beim Klick auf „Bearbeiten" – und solange sie unterwegs
 * ist, kannte das Kind-Feld seinen Wert nicht und beschriftete ihn „(nicht geladen)" (die Sonderoption aus
 * B-194). Wörtlich wahr, aber die Beschriftung des AUSFALLS – bei jedem Öffnen, auch wenn alles klappt.
 *
 * Gemessen wird IM Ladefenster: Die Liste antwortet erst nach 2 s (echter Server, nur später), die Fristen
 * der Fenster-Zusicherungen sind kürzer. Nur die Liste (`…/supervisor/children` ohne Pfad dahinter) wird
 * verzögert; der einzelne Datensatz `…/children/1`, aus dem die Seite den Namen zieht, lädt sofort – genau so
 * entsteht die Beschriftung mit Namen. Das Gegenstück (die Liste fällt aus → „(nicht geladen)" + Banner)
 * steht in `plan-kinderliste-faellt-aus.spec.ts` und bleibt unverändert.
 *
 * Das ist zugleich der Rollengang aus der Supervisor-Rolle.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);

test("Solange die Kinderliste lädt, sagt das Kind-Feld „lädt…“ und ist gesperrt – danach steht das Kind regulär da", async ({ page }) => {
  await vaterLogin(page, FATHER);

  await page.goto("/vater/plan/new");
  await page.locator("#plan-title").fill(`Kinderliste lädt ${RUN}`);
  await page.getByRole("combobox", { name: "Kind" }).selectOption("1");
  await page.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(page).toHaveURL(/\/vater\/plan\/\d+$/);

  await page.route("**/api/v1/supervisor/children", async (route) => {
    await new Promise((r) => setTimeout(r, 2000));
    await route.fallback();
  });
  await page.getByRole("button", { name: "Bearbeiten" }).click();

  // Im Fenster: eine Option, die sagt, dass geladen wird – nicht, dass etwas fehlt. Vor dem Fix stand hier
  // „(nicht geladen)".
  const kind = page.getByRole("combobox", { name: "Kind" });
  await expect(kind.locator("option:checked")).toHaveText(/\(lädt…\)$/, { timeout: 1200 });
  await expect(kind).toBeDisabled({ timeout: 1200 });
  // Kein Ausfall-Banner im Fenster – mit kurzer Frist, damit die Zeile zum Fenster gehört und nicht zum
  // Zustand danach. Absichtserklärung, kein Beleg für B-229: auch vor dem Fix stand hier kein Banner (die
  // Liste fällt ja nicht aus). Rot wird sie nur, wenn ein künftiger Umbau das Ladefenster als Ausfall meldet.
  await expect(page.getByRole("alert")).toHaveCount(0, { timeout: 1200 });

  // Danach: das Kind als reguläre Option, keine Sonderbeschriftung, Feld bedienbar.
  await expect(kind.locator("option:checked")).toHaveText(/\(#1\)$/, { timeout: 10_000 });
  await expect(kind).toBeEnabled();
  await expect(kind.locator("option", { hasText: /\((lädt…|nicht geladen)\)/ })).toHaveCount(0);
});
