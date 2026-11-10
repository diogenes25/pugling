import { test, expect } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-452: eine Vokabelliste wird eingefügt statt Zeile für Zeile getippt. Der Weg ist der Prüfgegenstand – Liste
 * einfügen, „anlegen & wählen", Übung anlegen – weil vier Wochen Stoff sonst 150 Einzelklicks sind.
 *
 * Eigene Reihe und Unit mit Lauf-Suffix: die Specs teilen eine Backend-Instanz, und Vokabeln sind global.
 */

const RUN = Date.now().toString().slice(-6);
const SERIES = `Liste-Reihe ${RUN}`;
const UNIT = `Unité 1 – ${RUN}`;
const TITLE = `Vocabulaire ${RUN}`;
// Der Lauf-Suffix am Wort hält die Paare je Lauf frisch – sonst prüfte der zweite Lauf nur „existiert schon".
const PAIRS: [string, string][] = [
  [`la ville ${RUN}`, "die Stadt"], [`le chien ${RUN}`, "der Hund"], [`la maison ${RUN}`, "das Haus"],
  [`le week-end ${RUN}`, "das Wochenende"], [`les déchets ${RUN}`, "Abfall, Müll"], [`l'école ${RUN}`, "die Schule"],
];

test("Eine eingefügte Liste wird angelegt, gewählt und zur Übung", async ({ page }) => {
  await vaterLogin(page, { id: "1", pin: "0000" });

  await page.goto("/vater/lehrwerke");
  await page.locator("#ns-name").fill(SERIES);
  await page.locator("#ns-subject").selectOption({ label: "Französisch" });
  await page.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(page.getByText(/steht im Katalog/)).toBeVisible();
  await page.getByRole("row", { name: new RegExp(SERIES) }).getByRole("button", { name: "Units" }).click();
  await page.locator('[id^="unit-label-new"]').fill(UNIT);
  await page.getByRole("button", { name: "Unit hinzufügen" }).click();
  await page.getByRole("link", { name: `+ Übung zu „${UNIT}"` }).click();

  await page.locator('select[aria-label="Übungstyp"]').selectOption("Vocabulary");
  await page.locator("#ex-title").fill(TITLE);
  await page.locator('select[aria-label="Quellsprache"]').selectOption("fr");
  await page.locator('select[aria-label="Zielsprache"]').selectOption("de");

  await page.getByRole("button", { name: "📋 Liste einfügen" }).click();
  // Eine Überschrift ohne Trenner und ein wiederholtes Paar gehören in jede echte Liste.
  const text = [`Unité 1`, ...PAIRS.map(([w, t]) => `${w} ; ${t}`), `${PAIRS[0][0]};${PAIRS[0][1]}`].join("\n");
  await page.getByLabel("Vokabelliste", { exact: true }).fill(text);
  // Die Paare stehen da, nicht nur ihre Zahl – eine am falschen Trenner gelesene Liste sähe als Zahl richtig aus.
  await expect(page.getByText(`${PAIRS.length} Paare erkannt, z. B. ${PAIRS[0][0]} → ${PAIRS[0][1]} | ${PAIRS[1][0]} → ${PAIRS[1][1]} · 1 Zeile ohne Trenner übersprungen: Unité 1`)).toBeVisible();

  await page.getByRole("button", { name: `${PAIRS.length} als fr→de anlegen & wählen` }).click();
  await expect(page.getByText(`${PAIRS.length} Vokabeln aus der Liste gewählt.`)).toBeVisible();
  // Das Komma blieb in der Übersetzung, der Bindestrich im Wort: beide Paare stehen als je EIN gewähltes Wort da.
  await expect(page.locator(".tokenlist .token")).toHaveCount(PAIRS.length);
  await expect(page.locator(".tokenlist")).toContainText(`les déchets ${RUN}→Abfall, Müll`);
  await expect(page.locator(".tokenlist")).toContainText(`le week-end ${RUN}→das Wochenende`);

  await page.getByRole("button", { name: "Übung anlegen" }).click();
  await expect(page.getByText(`Übung „${TITLE}" angelegt.`)).toBeVisible();
});
