import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-224. B-212 versprach für die Lehrwerke-SEITE genau einen Meldungsplatz, gebaut war er für die DATEI:
 * Das Verlags-Vokabular (`PublisherAdmin`) trug weiter einen eigenen `StatusBanner`. Ein grünes
 * „Gespeichert." unter dem Vokabular stand neben dem grünen „… steht im Katalog" der Seite – zwei Meldungen
 * zu zwei Handlungen in einem Blickfeld, und keine konnte die andere räumen.
 *
 * Der Weg hier ist der aus der Nachschau: Verlag anlegen → im Vokabular umbenennen → danach eine Reihe
 * anlegen. Zwei Zusicherungen tragen den Fall: Die Bestätigung des Verlags steht NICHT im Vokabular (vorher
 * genau dort), und die nächste Handlung räumt sie (vorher blieb sie stehen).
 *
 * Eigene Datei (`frontend/CLAUDE.md`: eine Fläche je Spec) – `gesperrter-knopf-nennt-den-grund.spec.ts`
 * fährt die Sperre, `bestaetigung-ueberlebt.spec.ts` die Reihen-Zeile. Das ist zugleich der Rollengang aus
 * der Creator-Rolle.
 */

const FATHER = { id: "1", pin: "0000" };
const STAMP = Date.now().toString(36).slice(-5);
const VERLAG = `Platzverlag ${STAMP}`;
const REIHE = `Platzreihe ${STAMP}`;

test("Die Bestätigung einer Verlags-Änderung steht am Platz der Seite – und die nächste Handlung räumt sie", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.goto("/vater/lehrwerke");

  // ---- Vorbereiten: ein eigener Verlag, damit Umbenennen erlaubt ist ----
  await page.locator("#ns-new-publisher").fill(VERLAG);
  await page.getByRole("button", { name: "Verlag anlegen" }).click();
  await expect(page.getByText(`„${VERLAG}" steht im Vokabular.`)).toBeVisible();

  // ---- Im Vokabular umbenennen ----
  await page.getByRole("button", { name: /Verlags-Vokabular verwalten/ }).click();
  const vokabular = page.locator("section.card", { hasText: "Verlags-Vokabular" });
  await vokabular.getByLabel("Verlag suchen").fill(VERLAG);
  await vokabular.getByLabel("Verlag suchen").press("Enter");
  await vokabular.getByLabel(`Name von ${VERLAG}`).fill(`${VERLAG} neu`);
  await vokabular.getByRole("button", { name: `„${VERLAG}" speichern` }).click();

  // ---- Der Kern: genau eine Bestätigung, und sie steht NICHT im Vokabular ----
  const bestaetigung = page.getByText("Gespeichert.", { exact: true });
  // „genau eine" war vor dem Fix AUCH wahr – nur im falschen Abschnitt. Die Zeile schützt vor „Seite UND
  // lokal doppelt"; die tragende rote Probe ist die nächste Zeile (vorher: 1 im Vokabular).
  await expect(bestaetigung).toHaveCount(1);
  await expect(vokabular.getByText("Gespeichert.", { exact: true })).toHaveCount(0);

  // ---- Die nächste Handlung räumt sie: eine Reihe anlegen ----
  await page.locator("#ns-name").fill(REIHE);
  await page.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(page.getByText(/steht im Katalog/)).toBeVisible();
  // Vorher blieb „Gespeichert." im Vokabular stehen – zwei Grüns zu zwei Handlungen.
  await expect(bestaetigung).toHaveCount(0);
});
