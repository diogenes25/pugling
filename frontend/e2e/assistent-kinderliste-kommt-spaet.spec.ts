import { test, expect } from "@playwright/test";

/*
 * B-177: Kommt die Kinderliste erst an, NACHDEM der Vater angefangen hat, ein neues Kind einzutragen, darf
 * der Assistent ihn nicht still auf das erste bestehende Kind umschalten. Bis B-177 tat er das, solange
 * niemand die Pille „Neues Kind anlegen" angeklickt hatte: der Effekt, der „bestehendes Kind" vorwählt,
 * feuerte auf die späte Liste – auch noch in Schritt 2 oder 3, wo der Vater es nicht mehr sieht –, und der
 * Plan wäre an ein anderes Kind gegangen. Nebenbei verschob derselbe Wechsel den Filter-Schlüssel der
 * Übungssuche mitten in einer Abfrage, die Lage, die B-177 ursprünglich beschrieb.
 *
 * Eigene Datei (frontend/CLAUDE.md, „Eine Fläche je E2E-Spec"): der Fall braucht eine angehaltene
 * Kinderliste, und die soll keinen anderen Ablauf verlangsamen.
 */
test("Eine spät ankommende Kinderliste nimmt dem Vater sein neues Kind nicht weg", async ({ page }) => {
  await page.goto("/vater");
  await page.locator("#fid").fill("1");
  await page.locator("#pin").fill("0000");
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();

  // Die Liste bis zur Freigabe anhalten – nur sie, alles andere läuft normal.
  let freigeben!: () => void;
  const freigegeben = new Promise<void>((resolve) => { freigeben = resolve; });
  await page.route("**/api/v1/supervisor/children", async (route) => { await freigegeben; await route.continue(); });

  await page.goto("/vater/wizard");
  // Eintragen ohne die Pille anzuklicken – genau der Weg, auf dem der Effekt bisher noch zuschlug.
  await page.locator("#wiz-name").fill("Spät-Neu");
  await page.locator("#wiz-pin").fill("1357");
  await page.getByRole("button", { name: "Weiter" }).click();
  await expect(page.getByRole("heading", { name: /Wo hakt es/ })).toBeVisible();

  // Jetzt kommt die Liste – während der Vater schon in Schritt 2 ist und den Wechsel nicht sähe.
  const liste = page.waitForResponse((r) => r.url().endsWith("/api/v1/supervisor/children") && r.ok());
  freigeben();
  await liste;

  // Zurück zu Schritt 1: das eingetragene neue Kind steht noch da. Vorher stand hier das Pulldown mit dem
  // ersten bestehenden Kind, und `#wiz-name` war verschwunden.
  await page.getByRole("button", { name: "Zurück" }).click();
  // Erst belegen, dass React die Liste verarbeitet hat: die Pille ist gesperrt, solange keine Kinder da
  // sind. Ohne diese Wartezeile könnten die zwei Zusicherungen darunter vor dem Umschalten grün werden.
  await expect(page.getByRole("button", { name: "Bestehendes Kind" })).toBeEnabled();
  await expect(page.locator("#wiz-name")).toHaveValue("Spät-Neu");
  await expect(page.getByRole("combobox", { name: "Kind" })).toHaveCount(0);
});
