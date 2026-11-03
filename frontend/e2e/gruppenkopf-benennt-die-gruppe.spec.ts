import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-236. Über Kästchen-Gruppen im Übungs-Formular stand eine sichtbare Überschrift („Schularten",
 * „Lern-Standards", „Freigabe (Startzustand)", „Rechenarten"), die an nichts gebunden war: ein Screenreader las
 * „Leitner-Kasten, Kontrollkästchen" ohne zu sagen, worum es geht. Jetzt trägt der `.field`-Container
 * `role="group"` und ist per `aria-labelledby` nach seiner Überschrift benannt.
 *
 * Gefragt wird über den Barrierefreiheitsbaum des echten Browsers (`getByRole("group", { name })`), nicht nach
 * Attributen: rot wird jeder Fall, wenn die Gruppe ihre Rolle verliert, ihr Name nicht mehr auf die Überschrift
 * zeigt oder die Kästchen nicht mehr in ihr stehen. Gemessen vor dem Fix: alle drei Fälle rot (B-236, Verlauf).
 */

const FATHER = { id: "1", pin: "0000" };

test("Übung anlegen: jede Kästchen-Gruppe trägt ihre Überschrift als Namen", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.goto("/vater/exercises/neu");

  const schularten = page.getByRole("group", { name: "Schularten", exact: true });
  await expect.soft(schularten.getByRole("checkbox", { name: "Gymnasium" })).toBeVisible();
  // Nichts Fremdes in der Gruppe (B-272): rot, wenn `role="group"` an einen Container rückt, der mehrere Gruppen umfasst.
  await expect.soft(schularten.getByRole("checkbox", { name: "Leitner-Kasten" })).toHaveCount(0);

  // Nur das Stichwort, nicht die Klammer dahinter: der Name ist, was vor jedem Kästchen angesagt wird.
  const standards = page.getByRole("group", { name: "Lern-Standards", exact: true });
  await expect.soft(standards.getByRole("checkbox", { name: "Leitner-Kasten" })).toBeVisible();
  await expect.soft(standards.getByRole("checkbox", { name: "nur getippte Tests" })).toBeVisible();
  await expect.soft(standards.getByRole("checkbox", { name: "Gymnasium" })).toHaveCount(0);

  const freigabe = page.getByRole("group", { name: "Freigabe (Startzustand)", exact: true });
  await expect.soft(freigabe.getByRole("checkbox", { name: "Für andere Betreuer zuweisbar" })).toBeVisible();
  await expect.soft(freigabe.getByRole("checkbox", { name: "Leitner-Kasten" })).toHaveCount(0);

  // Die Rechenarten gibt es nur beim Rechen-Drill; sie stehen im geteilten Editor (auch im Bearbeiten-Dialog).
  await page.locator('select[aria-label="Übungstyp"]').selectOption("ArithmeticDrill");
  const rechenarten = page.getByRole("group", { name: "Rechenarten", exact: true });
  await expect.soft(rechenarten.getByRole("checkbox").first()).toBeVisible();
  expect.soft(await rechenarten.getByRole("checkbox").count()).toBeGreaterThan(1);
  await expect.soft(rechenarten.getByRole("checkbox", { name: "Gymnasium" })).toHaveCount(0);
});

test("Übung bearbeiten: Schularten und Lern-Standards sind benannte Gruppen", async ({ page }) => {
  await vaterLogin(page, FATHER);
  // Vater #1 besitzt im Seed keine eigene Übung, und nur eigene lassen sich bearbeiten: Fach, Reihe, Unit und
  // eine Übung entstehen hier über die Oberfläche (Muster aus `uebung-teilspeichern.spec.ts`).
  const name = `Gruppen ${Date.now().toString().slice(-6)}`;
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

  await page.goto("/vater/exercises/neu");
  await page.locator('select[aria-label="Fach"]').selectOption({ label: name });
  await page.locator('select[aria-label="Reihe"]').selectOption({ label: name });
  await page.locator('select[aria-label="Unit"]').selectOption({ label: "Unit 1" });
  await page.locator('select[aria-label="Übungstyp"]').selectOption("Grammar");
  await page.locator("#ex-title").fill(name);
  await page.getByLabel("Anweisung", { exact: true }).fill("Setze die richtige Form ein.");
  await page.getByLabel("Aufgabe", { exact: true }).fill("He ___ (go) to school.");
  await page.getByLabel("Lösung", { exact: true }).fill("goes");
  await page.getByRole("button", { name: "Übung anlegen" }).click();
  await expect(page.getByText(`Übung „${name}" angelegt.`)).toBeVisible();

  await page.goto("/vater/exercises");
  await page.locator('select[aria-label="Fach"]').selectOption({ label: name });
  await page.getByRole("button", { name: "✏️ Bearbeiten" }).first().click();
  const dialog = page.getByRole("dialog");

  const schularten = dialog.getByRole("group", { name: "Schularten", exact: true });
  await expect.soft(schularten.getByRole("checkbox", { name: "Gymnasium" })).toBeVisible();
  await expect.soft(schularten.getByRole("checkbox", { name: "Leitner-Kasten" })).toHaveCount(0);
  const standards = dialog.getByRole("group", { name: "Lern-Standards", exact: true });
  await expect.soft(standards.getByRole("checkbox", { name: "Leitner-Kasten" })).toBeVisible();
  await expect.soft(standards.getByRole("checkbox", { name: "Gymnasium" })).toHaveCount(0);
});
