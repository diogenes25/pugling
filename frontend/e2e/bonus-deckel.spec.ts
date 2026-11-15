import { expect, test } from "@playwright/test";
import { createChild, vaterLogin } from "./helpers";

/*
 * B-533: Der Vater setzt den Tagesdeckel des Bonus-Trainings je Kind auf der Kind-Seite und sieht die Wirkung in
 * derselben Zeile, die aus der Sicht des Kindes gelesen wird („Heute verdient: … von …“). Die Wirkung auf die Buchung
 * selbst prüft `ExtraTrainingTests.Der_Vater_Setzt_Den_Tagesdeckel_Je_Kind`; hier liegt der Weg durch das Vater-Web.
 *
 * Eigene Datei (eine Fläche je Spec, B-109).
 */

const FATHER = { id: "1", pin: "0000" };
const STAMP = Date.now().toString(36).slice(-5);

test("Der Vater setzt den Bonus-Tagesdeckel je Kind und stellt ihn auf die Vorgabe zurück", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const childId = await createChild(page, `Deckel ${STAMP}`, "8654");
  await page.goto(`/vater/kind/${childId}?bereich=einstellungen`);

  const bereich = page.locator("section", { has: page.getByRole("heading", { name: "Bonus-Training" }) });
  // Ohne eigenen Wert gilt die Vorgabe des Servers (appsettings: 90, B-538).
  await expect(bereich.getByText("Heute verdient: 0 von 90 🪙 – Vorgabe des Servers")).toBeVisible();
  const feld = bereich.getByLabel("Tagesdeckel (Münzen)", { exact: true });
  const speichern = bereich.getByRole("button", { name: "Speichern" });
  await expect(speichern).toBeDisabled();

  // Außerhalb des Bereichs bleibt Speichern gesperrt, statt den Server einen 400 sagen zu lassen.
  await feld.fill("5000");
  await expect(speichern).toBeDisabled();
  await expect(bereich.getByText("Erlaubt sind ganze Zahlen von 0 bis 1000.")).toBeVisible();

  await feld.fill("5");
  await speichern.click();
  await expect(bereich.getByText("Tagesdeckel gespeichert.")).toBeVisible();
  await expect(bereich.getByText("Heute verdient: 0 von 5 🪙", { exact: true })).toBeVisible();

  // Nach einem Neuladen steht der eigene Wert im Feld – er kommt vom Server, nicht aus dem Formularzustand.
  await page.reload();
  await expect(feld).toHaveValue("5");

  await bereich.getByRole("button", { name: "Auf Vorgabe zurück" }).click();
  await expect(bereich.getByText("Vorgabe gilt wieder.")).toBeVisible();
  await expect(bereich.getByText("Heute verdient: 0 von 90 🪙 – Vorgabe des Servers")).toBeVisible();
  await expect(feld).toHaveValue("");
});
