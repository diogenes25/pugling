import { test, expect, type Page } from "@playwright/test";

/*
 * Material **zurückziehen** – aus der Sicht dessen, der es tut.
 *
 * Der Weg existierte nur in der API: `executePublic` wurde beim Speichern durchgereicht, ein Bedienelement
 * gab es nicht. Für einen Creator ist das die einzige Rücknahme, die es gibt – Löschen verweigert eine
 * benutzte Übung, und das zu Recht: laufende Pflichten dürfen nicht unter dem Kind wegbrechen.
 *
 * Geprüft wird der ganze Bogen: Zustand sichtbar → zurückziehen → sichtbar → wieder freigeben.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);
const SUBJECT = `E2E-Freigabe ${RUN}`;
const EXERCISE = `Rücknahme ${RUN}`;

async function vaterLogin(page: Page) {
  await page.goto("/vater");
  await page.locator("#fid").fill(FATHER.id);
  await page.locator("#pin").fill(FATHER.pin);
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();
}

test("Eigene Übung zurückziehen und wieder freigeben", async ({ page }) => {
  await vaterLogin(page);

  // Fach + Kapitel im Katalog (geteilt, darum je Lauf eindeutig benannt).
  await page.goto("/vater/katalog");
  await page.getByPlaceholder("z. B. Französisch").fill(SUBJECT);
  await page.getByRole("button", { name: "Neues Fach anlegen" }).click();
  await expect(page.locator("#ca-subject")).toHaveValue(/\d+/);
  await page.getByPlaceholder("z. B. Unit 1").fill("Unit 1");
  await page.getByRole("button", { name: "Neues Kapitel anlegen" }).click();
  await expect(page.getByText("Kapitel angelegt.")).toBeVisible();

  // Eine eigene Übung anlegen – sie ist standardmäßig für alle zuweisbar.
  await page.goto("/vater/exercises/neu");
  await page.locator('select[aria-label="Fach"]').selectOption({ label: SUBJECT });
  await page.locator('select[aria-label="Kapitel"]').selectOption({ label: "Unit 1" });
  await page.locator("#ex-title").fill(EXERCISE);
  await page.locator("#vp-word").fill(`ebb${RUN}`);
  await page.locator("#vp-translation").fill("Ebbe");
  await page.getByRole("button", { name: /anlegen & wählen/ }).click();
  // Auf das Token warten: das Anlegen im Store läuft asynchron, und ohne diese Schranke klickt der Test
  // „Übung anlegen", bevor die Vokabel gewählt ist – der Server antwortet dann „mindestens eine Vokabel".
  await expect(page.locator(".token", { hasText: `ebb${RUN}→Ebbe` })).toBeVisible();
  await page.getByRole("button", { name: "Übung anlegen" }).click();
  await expect(page.getByText(`Übung „${EXERCISE}" angelegt.`)).toBeVisible();

  /*
   * In der Verwaltung: noch freigegeben, darum kein Kennzeichen.
   *
   * Die Prüfungen greifen bewusst **seitenweit** statt über einen Zeilen-Container: Fach und Kapitel sind
   * für diesen Lauf frisch angelegt und enthalten genau diese eine Übung. Ein `div`-Container per `.last()`
   * zu erraten trifft je nach Verschachtelung die falsche Ebene – und genau daran ist dieser Test erst
   * gescheitert, obwohl die Oberfläche stimmte.
   */
  await page.getByRole("link", { name: /Übungen verwalten/ }).first().click();
  await expect(page.getByText(EXERCISE, { exact: true })).toBeVisible();
  await expect(page.getByText("zurückgezogen", { exact: true })).toHaveCount(0);

  // Der Schalter liegt in der Verwendungs-Anzeige, nicht in der Zeile: eine seltene
  // Verwaltungs-Entscheidung neben der Auskunft, die sie begründet.
  await page.getByRole("button", { name: "Verwendung" }).click();

  /*
   * Zurückziehen fragt nach (`confirmAction` → `window.confirm`), Freigeben nicht: die Rücknahme wirkt auf
   * fremde Familien, die Freigabe ist harmlos. Playwright weist Dialoge von sich aus **ab** – ohne diesen
   * Handler würde der Klick also stillschweigend nichts tun, und der Test wäre grün-blind.
   */
  page.on("dialog", (d) => d.accept());
  await page.getByRole("button", { name: /Zurückziehen/ }).click();
  await expect(page.getByText("zurückgezogen", { exact: true })).toBeVisible();
  // Der Knopf kehrt seine Bedeutung um – ein Zustand, kein Einbahnweg.
  await expect(page.getByRole("button", { name: /Wieder freigeben/ })).toBeVisible();

  // Und zurück: wieder für alle zuweisbar.
  await page.getByRole("button", { name: /Wieder freigeben/ }).click();
  await expect(page.getByText("zurückgezogen", { exact: true })).toHaveCount(0);
  await expect(page.getByRole("button", { name: /Zurückziehen/ })).toBeVisible();
});
