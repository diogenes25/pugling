import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-200, zweiter und dritter Fundort. `UnitForm.remove` fährt `ein-meldungsplatz.spec.ts`; diese beiden
 * hier waren bis zum Review dieses Sprints **unbelegt** — die Reparatur war dieselbe Zeile, aber „dieselbe
 * Zeile" ist keine Messung.
 *
 * Der Defekt: `action.run(…)` ohne `okText`, danach `onDone()` ohne Meldung. Reines Schweigen. Der Vater
 * las am Verschwinden der Zeile ab, ob der Server zugestimmt hatte — und wenn er es nicht tat, sah er es
 * gar nicht.
 *
 * Zwei Flächen, eine Spec: Es ist derselbe Weg auf zwei Bildschirmen (`TextbookForm`, `ProfileForm` — die
 * Datei-Kommentare verweisen selbst aufeinander), und beide Fälle sind unabhängig voneinander lauffähig.
 *
 * Das ist zugleich der Rollengang aus der Supervisor- bzw. Creator-Rolle.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString(36).slice(-5);

test("Das Entfernen eines Buchs am Kind meldet sich — und nennt das Buch", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.goto("/vater/kind/1");

  const TITEL = `Wegbuch ${RUN}`;
  await page.getByLabel("Titel", { exact: true }).fill(TITEL);
  await page.getByRole("button", { name: "Buch hinterlegen" }).click();
  await expect(page.getByText("Buch hinterlegt.", { exact: true })).toBeVisible();

  // Über das Bearbeiten-Formular, denn nur dort steht „Entfernen" (das Anlege-Formular hat kein Buch).
  await page.getByRole("button", { name: `${TITEL} bearbeiten` }).click();
  page.once("dialog", (d) => d.accept());
  await page.getByRole("button", { name: "Entfernen", exact: true }).click();

  // Der Name trägt die Aussage (B-196): Die Zeile, auf die sich die Meldung bezieht, ist gerade
  // verschwunden — ohne ihn wäre „Entfernt." eine Meldung ohne Gegenstand.
  await expect(page.getByText(`Buch „${TITEL}" entfernt.`, { exact: true })).toBeVisible();
  // Über den Bearbeiten-Knopf der Zeile, nicht über den Titel: Den enthält die Meldung selbst — eine
  // Zusicherung darauf könnte nie grün werden, egal ob gelöscht wurde oder nicht.
  await expect(page.getByRole("button", { name: `${TITEL} bearbeiten` })).toHaveCount(0);
});

test("Das Löschen eines Fachlehrers meldet sich — und nennt das Profil", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.goto("/vater/fachlehrer");

  const NAME = `Wegprofil ${RUN}`;
  await page.locator('[id^="fl-name-new"]').fill(NAME);
  await page.getByRole("button", { name: "Fachlehrer anlegen" }).click();
  await expect(page.getByText("Fachlehrer angelegt.", { exact: true })).toBeVisible();

  await page.getByRole("button", { name: `${NAME} bearbeiten` }).click();
  page.once("dialog", (d) => d.accept());
  await page.getByRole("button", { name: "Löschen", exact: true }).click();

  await expect(page.getByText(`Fachlehrer „${NAME}" gelöscht.`, { exact: true })).toBeVisible();
  // Über den Bearbeiten-Knopf, nicht über den Namen — siehe oben.
  await expect(page.getByRole("button", { name: `${NAME} bearbeiten` })).toHaveCount(0);
});
