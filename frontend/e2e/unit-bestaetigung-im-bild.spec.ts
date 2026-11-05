import { expect, test, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-390. B-385 holte die Bestätigung ins Bild, wenn eine Reihen-Zeile beim Löschen oder Speichern verschwinden kann –
 * die Unit-Handlungen blieben auf „nah". Löschen entfernt aber die Unit-Zeile, und Speichern einer Änderung schließt
 * das Formular: in einer langen Liste stand „Unit … gelöscht." bzw. „Gespeichert." dann unter dem Sichtbereich.
 *
 * Beide Fälle tragen nur, wenn der Platz beim Klick unter dem Sichtbereich liegt; die Vorbedingung rechnet das nach,
 * abzüglich dessen, was ohne Scroll nachrückt (Unit-Tabelle bzw. geschlossenes Formular).
 * Rote Probe: `UnitPanel` reicht dem Bearbeiten-Formular `onMeldung` statt `onMeldungZeileWeg` – dann fällt je Fall
 * die letzte Zeile. Gegenprobe, dass das Anlegen weiter nicht scrollt: `unit-anlegen-bleibt-im-bild.spec.ts`.
 */

const FATHER = { id: "1", pin: "0000" };

/** Präfix „0“ sortiert die Reihen nach oben; die erste bekommt eine Unit und steht aufgeklappt mit offenem Formular. */
async function offeneUnit(page: Page, praefix: string): Promise<string> {
  const stamp = Date.now().toString(36).slice(-5);
  const reihen = Array.from({ length: 8 }, (_, i) => `${praefix} ${stamp} ${i + 1}`);
  await vaterLogin(page, FATHER);
  await page.goto("/vater/lehrwerke");
  for (const name of reihen) {
    await page.locator("#ns-name").fill(name);
    await page.getByRole("button", { name: "Reihe anlegen" }).click();
    await expect(page.getByText(`„${name}"`, { exact: false }).first()).toBeVisible();
  }
  const unit = `Unit ${stamp}`;
  await page.getByRole("row", { name: new RegExp(reihen[0]) }).getByRole("button", { name: "Units", exact: true }).click();
  await page.locator('[id^="unit-label-new"]').fill(unit);
  await page.getByRole("button", { name: "Unit hinzufügen", exact: true }).click();
  await expect(page.getByText("Unit hinzugefügt.", { exact: true })).toHaveCount(1);
  await page.getByRole("button", { name: `${unit} bearbeiten`, exact: true }).click();
  await page.setViewportSize({ width: 1024, height: 480 });
  return unit;
}

function platz(page: Page) {
  return page.locator("section", { has: page.getByRole("heading", { name: "Lehrwerke", exact: true }) })
    .locator(":scope > div").last();
}

/** `nachruecken`: so weit rückt der Platz ohne Scroll nach oben, wenn Zeile und Formular verschwinden. */
async function platzLiegtUnterDemBild(page: Page, nachruecken: number) {
  const [oben, hoehe] = await Promise.all([
    platz(page).evaluate((el) => el.getBoundingClientRect().top),
    page.evaluate(() => window.innerHeight),
  ]);
  expect(oben - nachruecken, "Vorbedingung: Platz liegt unter dem Sichtbereich").toBeGreaterThan(hoehe);
}

/**
 * Was ohne Scroll verschwindet. Speichern: nur die Formular-Zeile. Löschen der einzigen Unit: die ganze Unit-Tabelle samt
 * Kopf – an ihre Stelle tritt „Noch keine Unit." (Review B-390). Die ganze Tabelle abzuziehen überschätzt das Nachrücken
 * um diesen Absatz; die Vorbedingung wird dadurch nur strenger.
 */
async function hoeheDesVerschwindenden(page: Page, ganzeTabelle: boolean): Promise<number> {
  const formZeile = page.locator('[id^="unit-label-u"]');
  return formZeile.evaluate((el, tabelle) =>
    (tabelle ? el.closest("table")! : el.closest("tr")!).getBoundingClientRect().height, ganzeTabelle);
}

test("„Unit … gelöscht.“ landet im Bild, obwohl der Platz weit unten liegt", async ({ page }) => {
  const unit = await offeneUnit(page, "0 Unitlöschreihe");
  const knopf = page.getByRole("button", { name: "Unit löschen", exact: true });
  await knopf.evaluate((el) => el.scrollIntoView({ block: "start" }));
  await platzLiegtUnterDemBild(page, await hoeheDesVerschwindenden(page, true));

  page.once("dialog", (d) => d.accept());
  await knopf.click();
  const meldung = page.getByText(`Unit „${unit}" gelöscht.`, { exact: true });
  await expect(meldung).toHaveCount(1);
  await expect(meldung).toBeInViewport();
});

test("„Gespeichert.“ einer Unit landet im Bild, obwohl der Platz weit unten liegt", async ({ page }) => {
  const unit = await offeneUnit(page, "0 Unitspeicherreihe");
  await page.locator('[id^="unit-label-u"]').fill(`${unit} geändert`);
  const knopf = page.getByRole("button", { name: "Speichern", exact: true });
  await knopf.evaluate((el) => el.scrollIntoView({ block: "start" }));
  await platzLiegtUnterDemBild(page, await hoeheDesVerschwindenden(page, false));

  await knopf.click();
  const meldung = page.getByText("Gespeichert.", { exact: true });
  await expect(meldung).toHaveCount(1);
  await expect(meldung).toBeInViewport();
});
