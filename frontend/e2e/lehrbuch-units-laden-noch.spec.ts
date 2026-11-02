import { expect, test } from "@playwright/test";
import { createChild, vaterLogin } from "./helpers";

/*
 * B-235. Das Lehrbuch-Formular unter dem Kind lädt die Units seiner Reihe erst beim Klick auf „Bearbeiten" –
 * und solange sie unterwegs waren, beschriftete das Unit-Feld die gespeicherte Unit mit „(nicht geladen)":
 * die Beschriftung des AUSFALLS, bei jedem Öffnen. B-229 hatte dasselbe am Kind-Feld des Plan-Formulars
 * repariert (`plan-kinderliste-laedt-noch.spec.ts`) und diese Stelle für „lädt auf Seitenebene" gehalten.
 *
 * Gemessen wird IM Ladefenster: der Unit-Abruf antwortet erst nach 2 s (echter Server, nur später), die
 * Fristen der Fenster-Zusicherungen sind kürzer. Das Buch entsteht auf dem gewöhnlichen Weg (eigenes Kind,
 * Reihe und Unit aus dem Seed), damit die Spec den Zustand prüft, den das Produkt selbst herstellt.
 *
 * Das ist zugleich der Rollengang aus der Supervisor-Rolle (die Chrome-Extension war im Nachtlauf
 * 2026-10-23 nicht verbunden, Freigabe 6).
 */

const FATHER = { id: "1", pin: "0000" };
const STAMP = Date.now().toString(36).slice(-5);
const CHILD = { name: `Unitkind ${STAMP}`, pin: "4321" };

test("Solange die Units laden, sagt das Unit-Feld „lädt…“ und ist gesperrt – danach steht die Unit regulär da", async ({ page }) => {
  await vaterLogin(page, FATHER);

  // ---- Ein eigenes Kind: das geseedete trägt Lernstand, den diese Spec nicht anfassen soll ----
  const childId = await createChild(page, CHILD.name, CHILD.pin);

  // ---- Ein Buch mit Reihe und Unit aus dem Seed („Green Line 1", „Unit 1 – Greetings") ----
  const buch = `Unit-Buch ${STAMP}`;
  await page.goto(`/vater/kind/${childId}`);
  await page.locator("#tbnew-title").fill(buch);
  const reihe = page.locator("#tbnew-series option", { hasText: "Green Line 1" });
  await page.locator("#tbnew-series").selectOption((await reihe.getAttribute("value"))!);
  const unitNeu = page.locator("#tbnew-unit");
  const unitOption = unitNeu.locator("option", { hasText: "Unit 1 – Greetings" });
  await expect(unitOption).toHaveCount(1);
  await unitNeu.selectOption((await unitOption.getAttribute("value"))!);
  await page.getByRole("button", { name: "Buch hinterlegen" }).click();
  await expect(page.getByText("Buch hinterlegt.", { exact: true })).toBeVisible();

  // ---- Jetzt den Unit-Abruf verzögern und das Bearbeiten-Formular öffnen ----
  await page.route("**/api/v1/creator/textbook-series/*/units", async (route) => {
    await new Promise((r) => setTimeout(r, 2000));
    await route.fallback();
  });
  await page.getByRole("button", { name: `${buch} bearbeiten` }).click();

  // Auf das BEARBEITEN-Formular einschränken: das Anlege-Formular („tbnew") steht auf derselben Seite.
  const unit = page.locator('select[id^="tb"][id$="-unit"]:not(#tbnew-unit)');

  // Im Fenster: eine Option, die sagt, dass geladen wird – nicht, dass etwas fehlt. Vor dem Fix stand hier
  // „Unit 1 – Greetings (nicht geladen)".
  await expect(unit.locator("option:checked")).toHaveText(/\(lädt…\)$/, { timeout: 1200 });
  await expect(unit).toBeDisabled({ timeout: 1200 });
  // Kein Ausfall-Banner im Fenster. Absichtserklärung, kein Beleg für B-235: auch vor dem Fix stand hier kein
  // Banner (die Liste fällt ja nicht aus). Rot nur, wenn ein Umbau das Ladefenster als Ausfall meldet.
  // Per TEXT, nicht per `getByRole("alert")` wie im Plan-Vorbild: der Units-Banner (`ChildMaterialSection.tsx`,
  // „Units: …") trägt keine Rolle – eine Rollen-Abfrage zählte hier immer 0 (Reviewer-Fund). Die fehlende Rolle
  // ist B-134s Sweep, nicht diese Spec.
  await expect(page.getByText(/^Units: /)).toHaveCount(0, { timeout: 1200 });

  // Danach: die Unit als reguläre Option, keine Sonderbeschriftung, Feld bedienbar.
  await expect(unit.locator("option:checked")).toHaveText(/Unit 1 – Greetings$/, { timeout: 10_000 });
  await expect(unit).toBeEnabled();
  await expect(unit.locator("option", { hasText: /\((lädt…|nicht geladen)\)/ })).toHaveCount(0);
});
