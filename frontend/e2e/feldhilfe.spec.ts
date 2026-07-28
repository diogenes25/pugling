import { test, expect, type Page } from "@playwright/test";

/*
 * Die Feld-Erklärungen („ⓘ") an den Stellen, an denen der Vater die Lern-Ökonomie einstellt.
 *
 * Warum überhaupt geprüft: Der Wert steckt nicht im Knopf, sondern darin, dass am Knopf der **richtige**
 * Text hängt – ein `topic`-Tippfehler fällt beim Übersetzen auf, ein an das falsche Feld gehängter
 * Hinweis nicht. Deshalb prüft der Test Feld → Textinhalt, nicht „irgendein Popover geht auf".
 *
 * Der zweite Teil ist die Bedienbarkeit: Ein Popover, das nicht wieder zugeht, verdeckt das nächste
 * Eingabefeld – in einer Zeile mit elf Feldern ist das kein Schönheitsfehler.
 */

const FATHER = { id: "1", pin: "0000" };

async function vaterLogin(page: Page) {
  await page.goto("/vater");
  await page.locator("#fid").fill(FATHER.id);
  await page.locator("#pin").fill(FATHER.pin);
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();
}

/** Legt einen leeren Plan an und landet auf seiner Seite – dort steht das Positions-Formular. */
async function planAnlegen(page: Page, title: string) {
  await page.goto("/vater/plan/new");
  await page.locator("#plan-title").fill(title);
  await page.getByRole("button", { name: "Plan anlegen & Übungen hinzufügen" }).click();
  await expect(page.getByRole("heading", { name: title })).toBeVisible();
}

test("Positions-Formular: jedes erklärungsbedürftige Feld hat den passenden Hinweis", async ({ page }) => {
  await vaterLogin(page);
  await planAnlegen(page, `Feldhilfe ${Date.now()}`);

  // Feldname → ein Ausschnitt, der nur im Text DIESES Feldes vorkommt.
  const erwartet: [string, RegExp][] = [
    ["Ziel-Rhythmus", /Wochenziel“ einmal pro Woche/],
    ["Bestehen ab %", /Leer lassen heißt 80 %/],
    ["Inhalte je Durchgang", /Leer = alle Inhalte der Übung/],
    ["Reihenfolge", /Beim Start einer Sitzung wird die Reihenfolge eingefroren/],
    ["Punkte, wenn das Ziel erreicht ist", /Familien-Shop für echte Belohnungen/],
    ["Münz-Malus bei versäumter Pflicht", /Schulden sind gewollt/],
    ["Punkte für einen neuen Inhalt", /zum ersten Mal richtig beantwortet/],
    ["Combo alle … Treffer", /richtigen Antworten am Stück/],
    ["Combo-Bonuspunkte", /wenn sie zuschlägt/],
    ["Leitner-Kasten", /wandert bei richtiger Antwort ein Fach weiter/],
    ["Nur getippte Tests zählen", /Gegen Raten/],
  ];

  for (const [feld, text] of erwartet) {
    // Das Positions-Formular ist die erste Maske mit diesen Feldern; `.first()`, weil eine bereits
    // angelegte Position dieselben Hinweise im Bearbeiten-Modus mitbringt.
    const knopf = page.getByRole("button", { name: `Erklärung zu „${feld}"` }).first();
    await expect(knopf, `Hinweis zu „${feld}" fehlt`).toBeVisible();
    await knopf.click();
    await expect(page.getByRole("note").filter({ hasText: text })).toBeVisible();
    await knopf.click(); // wieder zu, sonst überdeckt er den nächsten Knopf
    await expect(page.getByRole("note")).toHaveCount(0);
  }
});

test("Hinweis schließt per Escape und per Klick daneben", async ({ page }) => {
  await vaterLogin(page);
  await planAnlegen(page, `Feldhilfe-Schliessen ${Date.now()}`);

  const knopf = page.getByRole("button", { name: 'Erklärung zu „Münz-Malus bei versäumter Pflicht"' }).first();

  await knopf.click();
  await expect(page.getByRole("note")).toBeVisible();
  await expect(knopf).toHaveAttribute("aria-expanded", "true");
  await page.keyboard.press("Escape");
  await expect(page.getByRole("note")).toHaveCount(0);
  await expect(knopf).toHaveAttribute("aria-expanded", "false");

  await knopf.click();
  await expect(page.getByRole("note")).toBeVisible();
  await page.getByRole("heading", { name: /Übungen im Plan/ }).click();
  await expect(page.getByRole("note")).toHaveCount(0);
});

test("Assistent und Plan-Seite erklären dieselbe Größe mit demselben Text", async ({ page }) => {
  await vaterLogin(page);
  await planAnlegen(page, `Feldhilfe-Gleichlaut ${Date.now()}`);

  const aufPlanSeite = page.getByRole("button", { name: 'Erklärung zu „Münz-Malus bei versäumter Pflicht"' }).first();
  await aufPlanSeite.click();
  const textPlanSeite = await page.getByRole("note").innerText();
  await page.keyboard.press("Escape");

  // Im Assistenten steht dasselbe Feld unter anderer Beschriftung („Münz-Malus bei Versäumnis") –
  // genau deshalb liegen die Texte zentral: Zwei Formulierungen wären zwei Bedeutungen.
  await page.goto("/vater/wizard");
  const weiter = page.getByRole("button", { name: "Weiter" });
  await weiter.click();                                             // 1. Kind (das geseedete ist vorgewählt)
  await page.getByLabel("Fach").selectOption({ label: "Englisch" }); // 2. Problemfeld
  await page.getByRole("button", { name: /Regelmäßig üben/ }).click();
  await page.getByRole("button", { name: /^Normal/ }).click();
  await weiter.click();
  await page.getByRole("button", { name: "Alle wählen" }).click();   // 3. Übungen
  await weiter.click();
  await expect(page.getByRole("heading", { name: /Feinschliff/ })).toBeVisible();

  const imAssistenten = page.getByRole("button", { name: 'Erklärung zu „Münz-Malus bei versäumter Pflicht"' });
  await expect(imAssistenten).toBeVisible();
  await imAssistenten.click();
  await expect(page.getByRole("note")).toBeVisible();
  expect(await page.getByRole("note").innerText()).toBe(textPlanSeite);
});
