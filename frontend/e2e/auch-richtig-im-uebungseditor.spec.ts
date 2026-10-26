import { test, expect, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-71. Gleichwertige Übersetzungen („Auch richtig") ließen sich nur im Vokabel-Store pflegen – auf dem
 * bequemeren Weg, beim Anlegen und Pflegen einer Vokabelübung, gab es kein Feld dafür. Jetzt tragen beide
 * Oberflächen eines: das Schnell-Anlegen im Assistenten (`/vater/exercises/neu`) und die Wortpaare im
 * Bearbeiten-Dialog. Beide schreiben auf DENSELBEN Store-Eintrag – darum liest der Dialog am Ende, was der
 * Assistent geschrieben hat.
 *
 * Warum E2E und kein Komponententest: `frontend/CLAUDE.md` zieht die Grenze bei „kein nachgebauter
 * Bildschirm mit gefälschtem `fetch`" – der Fall IST der Rundweg Assistent → Store → Dialog.
 *
 * Das ist zugleich der Rollengang der Story aus der Creator-Rolle. Dass die Variante im Abschlusstest als
 * richtig zählt (AK 6), hält der Integrationstest `VocabItemAlternativesTests` – der Bewertungsweg ist der
 * von B-65, hier nur über den zweiten Eingabeweg erreicht.
 */

const FATHER = { id: "1", pin: "0000" };

/** Fach + Reihe + Unit über die Oberfläche; Name ist für alle drei derselbe. */
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

test("„Auch richtig“ entsteht im Assistenten und wird im Dialog gelesen, ergänzt und neu angelegt", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const stamp = Date.now().toString(36).slice(-6);
  const name = `Auch richtig ${stamp}`;
  // Store-Schlüssel sind global: eindeutige Wörter, sonst träfe der Lauf den Eintrag eines früheren Laufs.
  const wort = `huge${stamp}`;
  const zweites = `tiny${stamp}`;
  await katalog(page, name);

  // ---- Assistent: Wort mit Variante anlegen und wählen, dann die Übung anlegen ----
  await page.goto("/vater/exercises/neu");
  await page.locator('select[aria-label="Fach"]').selectOption({ label: name });
  await page.locator('select[aria-label="Reihe"]').selectOption({ label: name });
  await page.locator('select[aria-label="Unit"]').selectOption({ label: "Unit 1" });
  await page.locator("#ex-title").fill(name);
  await page.locator("#vp-word").fill(wort);
  await page.locator("#vp-translation").fill("riesig");
  await page.getByRole("button", { name: "+ Variante (neues Wort)" }).click();
  await page.getByLabel("Auch richtig 1 (neues Wort)", { exact: true }).fill("sehr groß");
  await page.getByRole("button", { name: "+ anlegen & wählen" }).click();
  // Das Token in der Auswahl ist der Beleg, dass das Wort angelegt UND gewählt ist.
  await expect(page.getByText(`${wort}→riesig`)).toBeVisible();
  // Nach dem Anlegen ist das Variantenfeld wieder leer – sonst wanderte „sehr groß" ans nächste Wort.
  await expect(page.getByLabel("Auch richtig 1 (neues Wort)", { exact: true })).toHaveCount(0);
  await page.getByRole("button", { name: "Übung anlegen" }).click();
  await expect(page.getByText(`Übung „${name}" angelegt.`)).toBeVisible();

  // ---- Dialog: die Wortpaare lesen die Variante vom Store zurück ----
  await page.goto("/vater/exercises");
  await page.locator('select[aria-label="Fach"]').selectOption({ label: name });
  await page.locator('select[aria-label="Reihe"]').selectOption({ label: name });
  await page.locator('select[aria-label="Unit"]').selectOption({ label: "Unit 1" });
  await page.getByRole("button", { name: "✏️ Bearbeiten" }).first().click();
  await expect(page.locator("#ed-title")).toBeVisible();
  await expect(page.getByLabel(`Auch richtig 1 (${wort})`, { exact: true })).toHaveValue("sehr groß");

  // ---- Dialog: eine zweite Variante ergänzen und speichern ----
  // Ohne Änderung kein Speichern-Knopf: ein Knopf, der nichts zu tun hat, wäre eine Lüge.
  await expect(page.getByRole("button", { name: `Auch richtig für ${wort} speichern` })).toHaveCount(0);
  await page.getByRole("button", { name: `+ Variante (${wort})` }).click();
  await page.getByLabel(`Auch richtig 2 (${wort})`, { exact: true }).fill("gewaltig");
  await page.getByRole("button", { name: `Auch richtig für ${wort} speichern` }).click();
  await expect(page.getByText("Gleichwertige Übersetzungen gespeichert.")).toBeVisible();
  await expect(page.getByRole("button", { name: `Auch richtig für ${wort} speichern` })).toHaveCount(0);

  // ---- Dialog: was der Server wegräumt, verschwindet auch aus dem Feld ----
  // „Riesig" ist die Hauptübersetzung (anders geschrieben): der Server streicht sie, seine Liste bleibt gleich.
  // Das Feld muss trotzdem seinen Stand übernehmen, statt „OK“ neben einer nie gespeicherten Eingabe zu zeigen.
  await page.getByRole("button", { name: `+ Variante (${wort})` }).click();
  await page.getByLabel(`Auch richtig 3 (${wort})`, { exact: true }).fill("Riesig");
  await page.getByRole("button", { name: `Auch richtig für ${wort} speichern` }).click();
  await expect(page.getByLabel(`Auch richtig 3 (${wort})`, { exact: true })).toHaveCount(0);
  await expect(page.getByRole("button", { name: `Auch richtig für ${wort} speichern` })).toHaveCount(0);

  // ---- Dialog: ein neues Wort mit Variante ergänzen ----
  await page.locator("#ai-front").fill(zweites);
  await page.locator("#ai-back").fill("winzig");
  await page.getByRole("button", { name: "+ Variante (neues Wort)" }).click();
  await page.getByLabel("Auch richtig 1 (neues Wort)", { exact: true }).fill("sehr klein");
  await page.getByRole("button", { name: "+ anlegen & hinzufügen" }).click();
  await expect(page.getByText("Wort hinzugefügt.")).toBeVisible();
  await expect(page.getByLabel(`Auch richtig 1 (${zweites})`, { exact: true })).toHaveValue("sehr klein");

  /*
   * Neu laden statt den offenen Dialog zu befragen: er zeigt seinen eigenen Zustand und behauptete die
   * Varianten auch dann, wenn der Server sie nie gespeichert hätte. Erst der frisch geladene Stand ist Zeuge.
   */
  await page.reload();
  await page.locator('select[aria-label="Fach"]').selectOption({ label: name });
  await page.locator('select[aria-label="Reihe"]').selectOption({ label: name });
  await page.locator('select[aria-label="Unit"]').selectOption({ label: "Unit 1" });
  await page.getByRole("button", { name: "✏️ Bearbeiten" }).first().click();
  await expect(page.getByLabel(`Auch richtig 1 (${wort})`, { exact: true })).toHaveValue("sehr groß");
  await expect(page.getByLabel(`Auch richtig 2 (${wort})`, { exact: true })).toHaveValue("gewaltig");
  await expect(page.getByLabel(`Auch richtig 1 (${zweites})`, { exact: true })).toHaveValue("sehr klein");
});
