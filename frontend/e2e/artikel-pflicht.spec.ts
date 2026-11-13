import { test, expect, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-421, AK 7 – der Rollengang des Creators: der Haken „Artikel muss mitgetippt werden" steht an beiden Orten, an denen
 * eine Vokabelübung eingestellt wird, und beide schreiben dieselbe Config. Angehakt beim Anlegen (Wortwähler auf
 * `/vater/exercises/neu`), im Bearbeiten-Dialog angehakt gelesen, dort abgehakt und gespeichert, nach dem Neuladen
 * abgehakt. Die erste Fassung dieser Story stand im Typ-Editor, den die App für Vokabeln nie zeigt – nur ein Weg durch
 * die echten Bildschirme findet so etwas (Review-Fund, Lauf AE).
 *
 * Dass „Stadt" ohne Haken als richtig zählt, fährt `erstbegegnung.spec.ts` aus dem Sitz des Sohnes; die Wertung selbst
 * halten `AnswerGraderTests` und `ArtikelToleranzTests`.
 */

const FATHER = { id: "1", pin: "0000" };

/** Fach + Reihe + Unit über die Oberfläche; der Name ist für alle drei derselbe. */
async function katalog(page: Page, name: string) {
  await page.goto("/vater/katalog");
  await page.getByPlaceholder("z. B. Französisch").fill(name);
  await page.getByRole("button", { name: "Neues Fach anlegen" }).click();
  await expect(page.locator("#ca-subject")).toHaveValue(/\d+/);

  await page.goto("/vater/lehrwerke");
  await page.locator("#ns-name").fill(name);
  await page.locator("#ns-subject").selectOption({ label: name });
  await page.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(page.getByText(/steht im Katalog/)).toBeVisible();
  await page.getByRole("row", { name: new RegExp(name) }).getByRole("button", { name: "Units" }).click();
  await page.locator('[id^="unit-label-new"]').fill("Unit 1");
  await page.getByRole("button", { name: "Unit hinzufügen" }).click();
  await expect(page.getByRole("button", { name: "Unit 1 bearbeiten" })).toBeVisible();
}

async function dialogOeffnen(page: Page, name: string) {
  await page.goto("/vater/exercises");
  await page.locator('select[aria-label="Fach"]').selectOption({ label: name });
  await page.locator('select[aria-label="Reihe"]').selectOption({ label: name });
  await page.locator('select[aria-label="Unit"]').selectOption({ label: "Unit 1" });
  await page.getByRole("button", { name: "✏️ Bearbeiten" }).first().click();
  await expect(page.locator("#ed-title")).toBeVisible();
}

const haken = (page: Page) => page.getByRole("checkbox", { name: "Artikel muss mitgetippt werden" });

test("Der Creator setzt die Artikel-Pflicht beim Anlegen und nimmt sie im Dialog wieder weg", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const stamp = Date.now().toString(36).slice(-6);
  const name = `Artikel ${stamp}`;
  await katalog(page, name);

  // ---- Anlegen: der Haken steht im Wortwähler, aus ----
  await page.goto("/vater/exercises/neu");
  await page.locator('select[aria-label="Fach"]').selectOption({ label: name });
  await page.locator('select[aria-label="Reihe"]').selectOption({ label: name });
  await page.locator('select[aria-label="Unit"]').selectOption({ label: "Unit 1" });
  await page.locator("#ex-title").fill(name);
  await expect(haken(page)).not.toBeChecked();
  await haken(page).check();
  // Die Erklärung kommt aus fieldHelp, nicht vom Feld.
  await expect(page.getByRole("button", { name: 'Erklärung zu „Artikel muss mitgetippt werden"' })).toBeVisible();
  await page.locator("#vp-word").fill(`ville${stamp}`);
  await page.locator("#vp-translation").fill("die Stadt");
  await page.getByRole("button", { name: "+ anlegen & wählen" }).click();
  await expect(page.getByText(`ville${stamp}→die Stadt`)).toBeVisible();
  await page.getByRole("button", { name: "Übung anlegen" }).click();
  await expect(page.getByText(`Übung „${name}" angelegt.`)).toBeVisible();

  // ---- Bearbeiten: der Dialog liest den gespeicherten Haken ----
  await dialogOeffnen(page, name);
  await expect(haken(page)).toBeChecked();

  // ---- abhaken, speichern, neu laden: der Server hält „aus" ----
  await haken(page).uncheck();
  await page.getByRole("button", { name: "Übung speichern" }).click();
  await expect(page.getByText("Gespeichert.", { exact: true })).toBeVisible();
  await page.reload();
  await dialogOeffnen(page, name);
  await expect(haken(page)).not.toBeChecked();
});
