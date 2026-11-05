import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-381. Seit B-232 scrollte JEDE Bestätigung zum Meldungsplatz unter der Reihen-Tabelle – auch „Unit hinzugefügt."
 * aus dem Formular in einer aufgeklappten Zeile. Wer Units hintereinander anlegt, verlor dabei bei jedem Speichern
 * sein offenes Formular aus dem Bild (Nachschau zu Sprint 1, 2026-11-05).
 *
 * Getragen wird der Fall vom Knopf „Unit hinzufügen": er steht beim Klick oben im Bild, der Platz weit darunter.
 * Scrollt die Seite zum Platz, verlässt der Knopf den Sichtbereich. Die Vorbedingung rechnet das nach, sonst trüge
 * der Fall nichts. Rote Probe: `holeBestaetigung` am Effekt ignorieren (jede Bestätigung scrollt) – dann fällt die
 * letzte Zeile.
 *
 * Eigene Datei (`frontend/CLAUDE.md`: eine Fläche je Spec); der entfernte Absender fährt
 * `bestaetigung-scrollt-zum-platz.spec.ts` weiter. Zugleich der Rollengang aus der Creator-Rolle.
 */

const FATHER = { id: "1", pin: "0000" };
const STAMP = Date.now().toString(36).slice(-5);
const REIHEN = Array.from({ length: 8 }, (_, i) => `Bildreihe ${STAMP} ${i + 1}`);

test("„Unit hinzufügen“ lässt das offene Formular im Bild", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.goto("/vater/lehrwerke");
  // Mehrere Reihen, damit zwischen der ersten und dem Platz unter der Tabelle mehr als eine Bildschirmhöhe liegt.
  for (const name of REIHEN) {
    await page.locator("#ns-name").fill(name);
    await page.getByRole("button", { name: "Reihe anlegen" }).click();
    await expect(page.getByText(`„${name}"`, { exact: false }).first()).toBeVisible();
  }
  await page.setViewportSize({ width: 1024, height: 480 });

  const zeile = page.getByRole("row", { name: new RegExp(REIHEN[0]) });
  await zeile.getByRole("button", { name: "Units" }).click();
  await page.locator('[id^="unit-label-new"]').fill(`Unit ${STAMP}`);
  const knopf = page.getByRole("button", { name: "Unit hinzufügen" });
  await knopf.evaluate((el) => el.scrollIntoView({ block: "start" }));

  // Vorbedingung: der Platz (letztes Kind des Abschnitts „Lehrwerke") liegt so weit unter dem Sichtbereich, dass ein
  // Scroll dorthin den Knopf aus dem Bild schöbe.
  const platz = page.locator("section", { has: page.getByRole("heading", { name: "Lehrwerke", exact: true }) })
    .locator(":scope > div").last();
  const [platzOben, knopfUnten, hoehe] = await Promise.all([
    platz.evaluate((el) => el.getBoundingClientRect().top),
    knopf.evaluate((el) => el.getBoundingClientRect().bottom),
    page.evaluate(() => window.innerHeight),
  ]);
  expect(platzOben - hoehe, "Vorbedingung: Platz weit genug unter dem Bild").toBeGreaterThan(knopfUnten);

  await knopf.click();
  await expect(page.getByText("Unit hinzugefügt.", { exact: true })).toHaveCount(1);
  await expect(knopf).toBeInViewport();
});
