import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-232. Der Meldungsplatz der Lehrwerke-Seite liegt unter der Reihen-Tabelle; das Verlags-Vokabular meldet
 * von zwei Abschnitten tiefer dorthin. Bis B-232 scrollte die Seite nur bei einem FEHLER zum Platz – nach
 * einem geglückten Umbenennen sah der Creator nur den OK-Knopf verschwinden (im Rollengang vom 2026-10-18
 * gemessen: 505 px Abstand bei 889 px Höhe).
 *
 * Getragen wird der Fall von `toBeInViewport`: `toBeVisible` prüft nicht den Sichtbereich und war darum vor
 * dem Fix schon grün. Die niedrige Fensterhöhe sorgt dafür, dass Knopf und Platz nicht zugleich sichtbar
 * sind. Rote Probe: die Bedingung `!meldung.ok` zurück in den Effekt – dann fällt die letzte Zeile.
 *
 * Eigene Datei (`frontend/CLAUDE.md`: eine Fläche je Spec), neben `verlag-meldet-am-platz-der-seite.spec.ts`,
 * das den Platz prüft, aber nicht die Sicht. Zugleich der Rollengang aus der Creator-Rolle.
 */

const FATHER = { id: "1", pin: "0000" };
const STAMP = Date.now().toString(36).slice(-5);
const VERLAG = `Sichtverlag ${STAMP}`;

test("Eine Bestätigung aus dem Verlags-Vokabular scrollt in den Sichtbereich", async ({ page }) => {
  await page.setViewportSize({ width: 1024, height: 480 });
  await vaterLogin(page, FATHER);
  await page.goto("/vater/lehrwerke");

  await page.locator("#ns-new-publisher").fill(VERLAG);
  await page.getByRole("button", { name: "Verlag anlegen" }).click();
  await expect(page.getByText(`„${VERLAG}" steht im Vokabular.`)).toBeVisible();

  await page.getByRole("button", { name: /Verlags-Vokabular verwalten/ }).click();
  const vokabular = page.locator("section.card", { hasText: "Verlags-Vokabular" });
  await vokabular.getByLabel("Verlag suchen").fill(VERLAG);
  await vokabular.getByLabel("Verlag suchen").press("Enter");
  await vokabular.getByLabel(`Name von ${VERLAG}`).fill(`${VERLAG} neu`);
  const speichern = vokabular.getByRole("button", { name: `„${VERLAG}" speichern` });
  await speichern.scrollIntoViewIfNeeded();
  // Vorbedingung, sonst trüge der Fall nichts: Der Platz liegt beim Klick OBERHALB des Sichtbereichs. Er ist
  // leer und damit 0 px hoch – `toBeInViewport` wäre für ihn immer falsch –, darum zählt seine Lage. Der Platz
  // ist das letzte Kind des Abschnitts mit der Überschrift „Lehrwerke".
  const platz = page.locator("section", { has: page.getByRole("heading", { name: "Lehrwerke", exact: true }) })
    .locator(":scope > div").last();
  expect(await platz.evaluate((el) => el.getBoundingClientRect().top)).toBeLessThan(0);
  await speichern.click();

  await expect(page.getByText("Gespeichert.", { exact: true })).toBeInViewport();
});
