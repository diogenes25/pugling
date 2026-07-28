import { test, expect, type Page } from "@playwright/test";

// Das Anmerkungs-Widget: erfassen, die Id zurückbekommen, die eigene Liste lesen.
//
// Besonderheit dieses Specs: Alle anderen Tests schalten das Widget ab (globaler localStorage-Schalter in
// playwright.config.ts) – es liefe im Dev-Modus sonst überall mit und könnte Klicks abfangen. Hier wird
// der Schalter für diesen einen Test wieder aufgehoben, sonst wäre das Widget nirgends geprüft.
//
// Was der Test wirklich absichert, ist nicht „ein Formular speichert", sondern der Kreis, für den das
// Feature existiert: Beobachtung → **Id** → damit geht man zu Claude Code. Bleibt die Id aus, ist das
// Feature wertlos, auch wenn der Eintrag in der Datenbank steht.

const FATHER = { id: "1", pin: "0000" };

test.use({
  storageState: { cookies: [], origins: [] }, // hebt den globalen Abschalter auf
});

async function vaterLogin(page: Page) {
  await page.goto("/vater");
  await page.locator("#fid").fill(FATHER.id);
  await page.locator("#pin").fill(FATHER.pin);
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();
}

test("Vater erfasst eine Anmerkung, bekommt die Log-Id und findet sie in seiner Liste", async ({ page }) => {
  await vaterLogin(page);

  // Auf eine Unterseite wechseln: Der Kontext-Mitschnitt soll die Route festhalten, nicht die Startseite.
  await page.goto("/vater/exercises");

  const widget = page.getByRole("button", { name: "Anmerkung erfassen" });
  await expect(widget).toBeVisible();
  await widget.click();

  const text = `E2E-Beobachtung ${Date.now()}`;
  await page.getByLabel("Text der Anmerkung").fill(text);
  await page.getByRole("button", { name: "🐞 Bug" }).click();
  await page.getByRole("button", { name: "Speichern" }).click();

  // Der eigentliche Ertrag: die Id, mit der die Frage in Claude Code eingelöst wird.
  const confirmation = page.getByText(/Gespeichert als #\d+/);
  await expect(confirmation).toBeVisible();
  const id = (await confirmation.textContent())!.match(/#(\d+)/)![1];

  // Das Feld ist wieder leer – das nächste Notieren beginnt ohne Aufräumen.
  await expect(page.getByLabel("Text der Anmerkung")).toHaveValue("");

  // Lesesicht: die eigene Liste zeigt den Eintrag samt Status.
  await page.getByRole("button", { name: "Meine" }).click();
  const entry = page.getByRole("article").filter({ hasText: `#${id}` });
  await expect(entry).toBeVisible();
  await expect(entry).toContainText(text);
  await expect(entry).toContainText("offen");
});

test("Enter sendet, Escape schließt – ein Feld, ein Tastendruck", async ({ page }) => {
  await vaterLogin(page);

  // Alt+A ist das Kürzel; bewusst mit Modifier, damit es keine Formulareingabe kapert.
  await page.keyboard.press("Alt+a");
  const field = page.getByLabel("Text der Anmerkung");
  await expect(field).toBeVisible();

  await field.fill(`Per Tastatur ${Date.now()}`);
  await field.press("Enter");
  await expect(page.getByText(/Gespeichert als #\d+/)).toBeVisible();

  await field.press("Escape");
  await expect(field).toBeHidden();
});

test("Sohn-Arcade: erfassen möglich, Navigation bleibt bedienbar", async ({ page }) => {
  // Der Sohn-Login: Helden-Nummer + PIN über das Ziffernfeld.
  await page.goto("/sohn");
  await page.locator("#childId").fill("1");
  for (const d of "1111".split("")) {
    await page.locator(".keys button", { hasText: new RegExp(`^${d}$`) }).first().click();
  }
  await page.getByRole("button", { name: "▶ LOS" }).click();
  await expect(page.getByText("Tagesmission")).toBeVisible();

  // Der eigentliche Prüfpunkt dieses Tests: Das Widget darf die Arcade-Navigation nicht überdecken.
  // `.sohn-nav` klebt unten – ein Launcher bei 12px läge darüber und finge deren Klicks ab.
  await expect(page.getByRole("button", { name: "Anmerkung erfassen" })).toBeVisible();
  await page.getByRole("link", { name: /Shop/ }).click();
  await expect(page).toHaveURL(/\/sohn\/shop/);

  // Erfassen aus der Sohn-Sicht: Der Server hält die Rolle fest, unter der notiert wurde.
  await page.keyboard.press("Alt+a");
  await page.getByLabel("Text der Anmerkung").fill(`Aus der Arcade ${Date.now()}`);
  await page.getByRole("button", { name: "Speichern" }).click();
  await expect(page.getByText(/Gespeichert als #\d+/)).toBeVisible();
});

test("Leerer Text wird abgewiesen, ohne etwas zu speichern", async ({ page }) => {
  await vaterLogin(page);
  await page.keyboard.press("Alt+a");

  await page.getByLabel("Text der Anmerkung").fill("   ");
  await page.getByRole("button", { name: "Speichern" }).click();

  await expect(page.getByText("Bitte etwas eintragen.")).toBeVisible();
  await expect(page.getByText(/Gespeichert als #\d+/)).toBeHidden();
});

test("Verlauf im Widget: nachhaken und den eigenen Beitrag wiederfinden", async ({ page }) => {
  await vaterLogin(page);
  await page.keyboard.press("Alt+a");

  const text = `Mit Verlauf ${Date.now()}`;
  await page.getByLabel("Text der Anmerkung").fill(text);
  await page.getByRole("button", { name: "Speichern" }).click();
  const confirmation = page.getByText(/Gespeichert als #\d+/);
  await expect(confirmation).toBeVisible();
  const id = (await confirmation.textContent())!.match(/#(\d+)/)![1];

  await page.getByRole("button", { name: "Meine" }).click();
  const entry = page.getByRole("article").filter({ hasText: `#${id}` });
  await expect(entry).toBeVisible();

  // Frisch erfasst gibt es keinen Beitrag – der Knopf lädt zum Nachhaken ein statt eine Zahl zu zeigen.
  await entry.getByRole("button", { name: "Nachhaken" }).click();

  const beitrag = `Ergaenzung ${Date.now()}`;
  await entry.getByLabel(`Beitrag zu Anmerkung ${id}`).fill(beitrag);
  await entry.getByLabel(`Beitrag zu Anmerkung ${id}`).press("Enter");

  // Der Verlauf zeigt den Beitrag; damit ist der Rückkanal aus dem UI belegt.
  await expect(entry).toContainText(beitrag);
  await expect(entry.getByRole("button", { name: /Verlauf zu/ })).toBeVisible();
});

test("Anmerkungs-Seite: filtern, Verlauf lesen, Stand ändern", async ({ page }) => {
  await vaterLogin(page);
  await page.keyboard.press("Alt+a");

  const text = `Fuer die Seite ${Date.now()}`;
  await page.getByLabel("Text der Anmerkung").fill(text);
  await page.getByRole("button", { name: "🎨 UI" }).click();
  await page.getByRole("button", { name: "Speichern" }).click();
  await expect(page.getByText(/Gespeichert als #\d+/)).toBeVisible();

  await page.goto("/vater/anmerkungen");
  const card = page.getByRole("article").filter({ hasText: text });
  await expect(card).toBeVisible();

  // Kontext-Filter: „offen" ist die Vorgabe, die Einordnung kam aus dem Schnellklick im Widget.
  await page.getByLabel("Kategorie-Filter").selectOption("Ui");
  await expect(card).toBeVisible();

  // Nachhaken über die Seite – hier ist Platz für mehr als eine Zeile.
  await card.getByRole("button", { name: /Nachhaken|Verlauf/ }).click();
  const beitrag = `Von der Seite aus ${Date.now()}`;
  await card.getByLabel("Beitrag").fill(beitrag);
  await card.getByRole("button", { name: "Senden" }).click();
  await expect(card).toContainText(beitrag);

  // Der Stand lässt sich hier direkt setzen (der Skill macht das sonst über die API). Die Zeile fällt
  // damit aus dem Filter „offen" – die Rückmeldung muss das überleben, sonst hätte man eine Änderung
  // ohne jede Bestätigung.
  await card.getByLabel(/^Stand von Anmerkung/).selectOption("Planned");
  await expect(page.getByText(/Stand gesetzt: #\d+ → eingeplant/)).toBeVisible();
  await expect(card).toBeHidden();

  // Und unter „eingeplant" ist sie wieder da.
  await page.getByLabel("Status-Filter").selectOption("Planned");
  await expect(page.getByRole("article").filter({ hasText: text })).toBeVisible();

  // Kontenübergreifend lesen steht jedem Vater offen (Schalter `Remarks:GlobalRead`, in der Entwicklung
  // an) – ohne Admin-Rolle und ohne Flag am Konto. Genau diese Sicht nutzt der Nachbereitungs-Skill.
  const alleKonten = page.getByRole("checkbox", { name: "alle Konten" });
  await expect(alleKonten).toBeVisible();
  await alleKonten.check();
  await expect(alleKonten).toBeChecked();
  // Kein Fehlerbanner: Die Abfrage mit scope=all wird beantwortet, nicht abgewiesen.
  await expect(page.locator(".banner.err")).toHaveCount(0);
});
