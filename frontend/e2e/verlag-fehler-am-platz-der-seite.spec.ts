import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-224, der rote Pfad. Das Verlags-Vokabular meldet seit B-224 nach oben – auch seine FEHLER. Dieser Weg war
 * bis zur Nachschau vom 2026-10-18 ungetestet: Das Akzeptanzkriterium nannte den 409 „im Seed gar nicht
 * auslösbar", was nur fürs Löschen stimmt (Sperre am Knopf). Beim Umbenennen sperrt der OK-Knopf nur bei
 * leerem Namen, und ein vergebener Name liefert 409 `DuplicatePublisher` – „Klett" ist geseedet.
 *
 * Zwei Zusicherungen tragen: Die Fehlermeldung steht NICHT im Vokabular-Abschnitt (vor B-224 genau dort), und
 * sie steht am Platz der Seite. Eigene Datei (`frontend/CLAUDE.md`: eine Fläche je Spec) neben
 * `verlag-meldet-am-platz-der-seite.spec.ts`, das den grünen Pfad fährt.
 */

const FATHER = { id: "1", pin: "0000" };
const STAMP = Date.now().toString(36).slice(-5);
const VERLAG = `Fehlerverlag ${STAMP}`;

test("Ein Fehler einer Verlags-Änderung steht am Platz der Seite, nicht im Vokabular", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.goto("/vater/lehrwerke");

  // ---- Vorbereiten: ein eigener Verlag, damit Umbenennen erlaubt ist ----
  await page.locator("#ns-new-publisher").fill(VERLAG);
  await page.getByRole("button", { name: "Verlag anlegen" }).click();
  await expect(page.getByText(`„${VERLAG}" steht im Vokabular.`)).toBeVisible();

  // ---- Im Vokabular auf einen vergebenen Namen umbenennen ----
  await page.getByRole("button", { name: /Verlags-Vokabular verwalten/ }).click();
  const vokabular = page.locator("section.card", { hasText: "Verlags-Vokabular" });
  await vokabular.getByLabel("Verlag suchen").fill(VERLAG);
  await vokabular.getByLabel("Verlag suchen").press("Enter");
  await vokabular.getByLabel(`Name von ${VERLAG}`).fill("Klett");
  await vokabular.getByRole("button", { name: `„${VERLAG}" speichern` }).click();

  // ---- Der Kern: der Fehler steht am Platz der Seite, nicht im Abschnitt ----
  const fehler = page.locator(".banner.err");
  await expect(fehler).toHaveCount(1);
  await expect(vokabular.locator(".banner.err")).toHaveCount(0);
  // Und er sagt, was sperrt – nicht nur „irgendein Fehler". Seit B-411 auf Deutsch (`duplicate_publisher`).
  await expect(fehler).toContainText("Ein anderer Verlag trägt diesen oder einen fast gleichen Namen schon");
});
