import { expect, test, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-64. Das Buch am Kind gab es zweimal: als Freitext (Titel, Verlag, Klasse) und als Verweis in den Katalog
 * (Reihe, Unit) – gleichrangig nebeneinander, und der billigere Weg gewann. Jetzt steht der Katalog zuerst,
 * eine fehlende Reihe lässt sich direkt hier anlegen, und Verlag/Klasse kommen aus dem Katalog, sobald das
 * Buch dorthin zeigt. Der getippte Freitext bleibt Rückfallebene und taucht beim Entkoppeln wieder auf.
 *
 * Eine Fläche je Spec (B-109): eigene Datei statt eines Blocks in `kind-lehrbuch-fach.spec.ts`.
 * Das ist zugleich der Rollengang der Story aus der Vater-Rolle. Kein Schritt braucht einen JS-Dialog.
 */

const FATHER = { id: "1", pin: "0000" };
const STAMP = Date.now().toString(36).slice(-5);

/** Die Daten-Zeile eines Buchs (nicht die Formular-Zeile darunter, die denselben Titel tragen kann). */
const buchZeile = (page: Page, titel: string) =>
  page.getByRole("row").filter({ has: page.getByRole("button", { name: `${titel} bearbeiten` }) });

/** Das BEARBEITEN-Formular: das Anlege-Formular („tbnew") steht auf derselben Seite. */
const bearbeiten = (page: Page) =>
  page.locator("form").filter({ has: page.locator('select[id^="tb"][id$="-series"]:not(#tbnew-series)') });

test("Katalog zuerst: Reihe inline anlegen, Verlag und Klasse aus dem Katalog, Freitext überlebt das Entkoppeln", async ({ page }) => {
  await vaterLogin(page, FATHER);

  // ---- Ein eigenes Kind ----
  const kind = `Katalogkind ${STAMP}`;
  await page.goto("/vater");
  await page.locator("#new-child-name").fill(kind);
  await page.locator("#new-child-pin").fill("4322");
  await page.getByRole("button", { name: "Kind anlegen" }).click();
  const kindZeile = page.getByRole("row", { name: new RegExp(kind) });
  await expect(kindZeile).toBeVisible();
  const childId = (await kindZeile.getByRole("cell").first().textContent())!.trim();

  // ---- Eine Reihe MIT Verlag und einer Unit MIT Klasse, auf dem gewöhnlichen Weg unter „Lehrwerke" ----
  const reihe = `Katalogreihe ${STAMP}`;
  const verlag = `Katalogverlag ${STAMP}`;
  await page.goto("/vater/lehrwerke");
  // Erst den Verlag (er wird angelegt UND in der Reihen-Auswahl gewählt), dann die Reihe.
  await page.locator("#ns-new-publisher").fill(verlag);
  await page.getByRole("button", { name: "Verlag anlegen" }).click();
  await expect(page.locator("#ns-publisher")).toHaveValue(/\d+/);
  await page.locator("#ns-name").fill(reihe);
  await page.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(page.getByText(/steht im Katalog/)).toBeVisible();
  await page.getByRole("row", { name: new RegExp(reihe) }).getByRole("button", { name: "Units" }).click();
  await page.locator('[id^="unit-label-new"]').fill("Unit 1");
  await page.locator('[id^="unit-grade-new"]').fill("7");
  await page.getByRole("button", { name: "Unit hinzufügen" }).click();
  await expect(page.getByRole("button", { name: "Unit 1 bearbeiten" })).toBeVisible();

  // ---- AK 3: ein Buch ganz ohne Katalog bleibt eintragbar ----
  const titel = `Freitextbuch ${STAMP}`;
  await page.goto(`/vater/kind/${childId}`);
  // AK 1: der Katalog ist die erste Gruppe des Formulars, der Freitext steht benannt dahinter.
  const anlegen = page.locator("form").filter({ has: page.locator("#tbnew-series") });
  await expect(anlegen.getByRole("group", { name: /Aus dem Katalog/ })).toBeVisible();
  await expect(anlegen.getByRole("group", { name: /Ohne Katalog/ })).toBeVisible();
  await page.locator("#tbnew-title").fill(titel);
  await page.locator("#tbnew-publisher").fill("Freitext-Verlag");
  await page.locator("#tbnew-grade").fill("9");
  await page.getByRole("button", { name: "Buch hinterlegen" }).click();
  await expect(page.getByText("Buch hinterlegt.", { exact: true })).toBeVisible();
  await expect(buchZeile(page, titel)).toContainText("Freitext-Verlag");
  await expect(buchZeile(page, titel)).toContainText("nicht katalogisiert");

  // ---- AK 4: verknüpfen – Verlag und Klasse kommen aus dem Katalog, nicht aus dem Freitext ----
  await page.getByRole("button", { name: `${titel} bearbeiten` }).click();
  const form = bearbeiten(page);
  await form.locator('select[id$="-series"]').selectOption({ label: `${reihe} (${verlag})` });
  await form.locator('select[id$="-unit"]').selectOption({ label: "Kl. 7: Unit 1" });
  // Schon vor dem Speichern: die Felder weichen dem Katalogwert, statt eine zweite Wahrheit anzubieten.
  await expect(form.getByText(`${verlag} · aus dem Katalog`)).toBeVisible();
  await expect(form.getByText("7 · aus der Unit")).toBeVisible();
  await expect(form.getByLabel("Verlag", { exact: true })).toHaveCount(0);
  await form.getByRole("button", { name: "Speichern" }).click();
  await expect(page.getByText("Gespeichert.", { exact: true })).toBeVisible();
  await expect(buchZeile(page, titel)).toContainText(verlag);
  await expect(buchZeile(page, titel)).not.toContainText("Freitext-Verlag");

  // ---- AK 5: entkoppeln – der getippte Verlag ist nicht verloren ----
  await page.getByRole("button", { name: `${titel} bearbeiten` }).click();
  await bearbeiten(page).locator('select[id$="-series"]').selectOption({ label: "– nicht katalogisiert –" });
  await bearbeiten(page).getByRole("button", { name: "Speichern" }).click();
  await expect(page.getByText("Gespeichert.", { exact: true })).toBeVisible();
  await expect(buchZeile(page, titel)).toContainText("Freitext-Verlag");
  await page.getByRole("button", { name: `${titel} bearbeiten` }).click();
  await expect(bearbeiten(page).getByLabel("Für Klasse", { exact: true })).toHaveValue("9");
  // Derselbe Knopf schließt wieder: sein Name bleibt „<Titel> bearbeiten“, nur der Text wechselt.
  await page.getByRole("button", { name: `${titel} bearbeiten` }).click();

  // ---- AK 2: eine noch fehlende Reihe direkt aus dem Formular anlegen ----
  const neueReihe = `Inline-Reihe ${STAMP}`;
  const zweitesBuch = `Inline-Buch ${STAMP}`;
  await anlegen.getByRole("button", { name: "+ Neue Reihe" }).click();
  await anlegen.getByLabel("Name der neuen Reihe", { exact: true }).fill(neueReihe);
  await anlegen.getByRole("button", { name: /Reihe anlegen/ }).click();
  await expect(page.getByText(`Reihe „${neueReihe}“ steht im Katalog und ist gewählt.`)).toBeVisible();
  await expect(page.locator("#tbnew-series")).toHaveValue(/\d+/);
  await page.locator("#tbnew-title").fill(zweitesBuch);
  await page.getByRole("button", { name: "Buch hinterlegen" }).click();
  await expect(page.getByText("Buch hinterlegt.", { exact: true })).toBeVisible();
  await expect(buchZeile(page, zweitesBuch)).toContainText(neueReihe);
});
