import { test, expect, type Page } from "@playwright/test";

/*
 * Die drei Perspektiven des Vater-Webs (Betreuen / Zuweisen / Erstellen, siehe
 * docs/vater-perspektiven-plan.md) aus der Sicht der zwei Benutzertypen, die sich ein Vater-Konto teilen:
 *
 *   - der **Vater**, der betreut und zuweist,
 *   - der **Lehrer**, der Inhalte baut und kein Kind sehen will.
 *
 * Geprüft wird die eigentliche Zusage der Trennung: in einer Perspektive tauchen die Bereiche der anderen
 * **nicht** auf. Ein Test, der nur „Link existiert" prüft, hätte den Zustand vorher genauso bestanden –
 * damals lagen alle zwölf Bereiche gleichzeitig da.
 */

const FATHER = { id: "1", pin: "0000" };

async function vaterLogin(page: Page) {
  await page.goto("/vater");
  await page.locator("#fid").fill(FATHER.id);
  await page.locator("#pin").fill(FATHER.pin);
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();
}

/** Die Bereichs-Navigation der aktiven Perspektive (nicht der Umschalter darüber). */
const areaNav = (page: Page) => page.locator("nav.vater-nav");

/** Der Umschalter; seine aktive Perspektive trägt `aria-current="page"`. */
const currentPerspective = (page: Page) =>
  page.locator("nav.perspective-switch a[aria-current='page']");

test("Vater betreut: Autorenwerkzeug liegt nicht im Weg", async ({ page }) => {
  await vaterLogin(page);

  await expect(currentPerspective(page)).toContainText("Betreuen");
  // Was der Vater beim Betreuen braucht …
  for (const area of ["Klassenarbeiten", "Belohnungen", "Shop", "Kontostand"]) {
    await expect(areaNav(page).getByRole("link", { name: new RegExp(area) })).toBeVisible();
  }
  // … und was hier nichts zu suchen hat: die Werkbank des Autors.
  for (const area of ["Lehrwerke", "Fachlehrer", "Lückentexte", "Katalog", "Vokabeln"]) {
    await expect(areaNav(page).getByRole("link", { name: new RegExp(area) })).toHaveCount(0);
  }
  // Die Plan-Liste ist auf der Betreuen-Startseite nur noch ein Weg, keine Tabelle.
  await expect(page.getByRole("columnheader", { name: "Zeitraum" })).toHaveCount(0);
  await expect(page.getByRole("link", { name: /Zum Zuweisen/ })).toBeVisible();
});

test("Lehrer erstellt: kein Kind, kein Geld, keine Pflicht", async ({ page }) => {
  await vaterLogin(page);

  await page.getByRole("link", { name: /Erstellen/ }).first().click();
  await expect(page).toHaveURL(/\/vater\/inhalte$/);
  await expect(currentPerspective(page)).toContainText("Erstellen");
  await expect(page.getByRole("heading", { name: "Werkstatt" })).toBeVisible();

  // Die Bausteine sind da …
  for (const area of ["Übungen", "Vokabeln", "Lückentexte", "Katalog", "Lehrwerke", "Fachlehrer", "Bilder"]) {
    await expect(areaNav(page).getByRole("link", { name: new RegExp(area) })).toBeVisible();
  }
  // … und das Betreuen ist weg. Genau das macht die Perspektive für einen Lehrer brauchbar.
  for (const area of ["Belohnungen", "Shop", "Kontostand", "Klassenarbeiten"]) {
    await expect(areaNav(page).getByRole("link", { name: new RegExp(area) })).toHaveCount(0);
  }
});

test("Unterseiten behalten ihre Perspektive – auch beim Direktaufruf", async ({ page }) => {
  await vaterLogin(page);

  // Ein Lesezeichen auf das Anlege-Formular muss in „Erstellen" landen, nicht in „Betreuen": sonst zeigte
  // die Navigation etwas anderes als der Inhalt. Darum kommt die Perspektive aus dem Pfad.
  await page.goto("/vater/exercises/neu");
  await expect(currentPerspective(page)).toContainText("Erstellen");

  await page.goto("/vater/plan/new");
  await expect(currentPerspective(page)).toContainText("Zuweisen");

  await page.goto("/vater/kind/1");
  await expect(currentPerspective(page)).toContainText("Betreuen");
});

test("Der Lehrer landet nach dem Anmelden in seiner Werkstatt", async ({ page }) => {
  await vaterLogin(page);

  // Die Perspektive bewusst wählen – nur der Klick auf den Umschalter gilt als Entscheidung.
  await page.getByRole("link", { name: /Erstellen/ }).first().click();
  await expect(page).toHaveURL(/\/vater\/inhalte$/);

  await page.getByRole("button", { name: "Abmelden" }).click();
  // Bewusst über `/vater` anmelden: genau dort greift der Sprung, und nur dort – ein Sprung bei *jedem*
  // Besuch von `/vater` würde den Umschalter unbenutzbar machen. `vaterLogin` taugt hier nicht, es erwartet
  // die Kinder-Tabelle, die es in der Werkstatt zu Recht nicht gibt.
  await page.goto("/vater");
  await page.locator("#fid").fill(FATHER.id);
  await page.locator("#pin").fill(FATHER.pin);
  await page.getByRole("button", { name: "Anmelden" }).click();

  // Ohne dieses Verhalten müsste ein Lehrer nach jedem Anmelden erst an der Vater-Sicht vorbei.
  await expect(page).toHaveURL(/\/vater\/inhalte$/);
  await expect(page.getByRole("heading", { name: "Werkstatt" })).toBeVisible();
});

test("Der Sprung gilt nur fürs Anmelden – `/vater` bleibt aufrufbar und neu ladbar", async ({ page }) => {
  /*
   * Die Gegenprobe zum Test darüber, und die eigentliche Falle: die Sitzung wird beim Laden der Seite
   * **synchron** wiederhergestellt. Zählte das als frische Anmeldung, käme ein Vater, der einmal
   * „Erstellen" gewählt hat, nie wieder auf seine Startseite – weder über den Umschalter noch per
   * Lesezeichen noch mit F5.
   */
  await vaterLogin(page);
  await page.getByRole("link", { name: /Erstellen/ }).first().click();
  await expect(page).toHaveURL(/\/vater\/inhalte$/);

  // Über den Umschalter zurück – bei angemeldeter Sitzung darf nichts zurückwerfen.
  await page.getByRole("link", { name: /Betreuen/ }).first().click();
  await expect(page).toHaveURL(/\/vater$/);

  // Und ein Neuladen derselben Adresse bleibt dort. (Nach dem Klick auf „Betreuen" ist die gemerkte
  // Perspektive ohnehin wieder „betreuen" – darum vorher erneut „Erstellen" merken.)
  await page.getByRole("link", { name: /Erstellen/ }).first().click();
  await page.goto("/vater");
  await expect(page).toHaveURL(/\/vater$/);
  await expect(page.getByRole("heading", { name: "Heute" })).toBeVisible();
  await page.reload();
  await expect(page).toHaveURL(/\/vater$/);
});

test("Der Weg Erstellen → Zuweisen → Betreuen ist durchgängig verlinkt", async ({ page }) => {
  await vaterLogin(page);

  // Die Werkstatt sagt, wo der Stoff später landet …
  await page.goto("/vater/inhalte");
  await page.getByRole("link", { name: "Zuweisen", exact: true }).last().click();
  await expect(page).toHaveURL(/\/vater\/plaene$/);

  // … und das Zuweisen führt zurück zum Betreuen, wenn ein Kind fehlt bzw. über den Umschalter.
  await page.getByRole("link", { name: /Betreuen/ }).first().click();
  await expect(page).toHaveURL(/\/vater$/);
  await expect(page.getByRole("heading", { name: "Heute" })).toBeVisible();
});
