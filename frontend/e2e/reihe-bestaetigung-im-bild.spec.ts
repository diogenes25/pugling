import { expect, test, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-385. Seit B-381 scrollt eine Bestätigung nur noch, wenn ihr Absender „weit weg" meldet – und die Reihen-Zeile
 * meldete „nah". Damit stand „Reihe … gelöscht." in einer langen Liste unter der Tabelle außer Sicht: die Zeile war
 * weg, sonst geschah sichtbar nichts. Dasselbe gilt fürs Speichern einer Reihe, das die Zeile umsortieren oder aus
 * dem Filter fallen lassen kann (Nachschau zu Lauf U, Sprint 3).
 *
 * Beide Fälle tragen nur, wenn der Platz beim Klick unter dem Sichtbereich liegt; die Vorbedingung rechnet das nach.
 * Rote Probe: `SeriesRow` reicht `melde` statt `meldeVonWeitWeg` weiter – dann fällt je Fall die letzte Zeile.
 * Gegenprobe, dass Units weiter nicht scrollen: `unit-anlegen-bleibt-im-bild.spec.ts`.
 */

const FATHER = { id: "1", pin: "0000" };

/** Präfix „0“: die Liste sortiert nach Namen, so stehen die Reihen oben und der Platz weit darunter. */
async function langeListe(page: Page, praefix: string): Promise<string[]> {
  const stamp = Date.now().toString(36).slice(-5);
  const reihen = Array.from({ length: 8 }, (_, i) => `${praefix} ${stamp} ${i + 1}`);
  await vaterLogin(page, FATHER);
  await page.goto("/vater/lehrwerke");
  for (const name of reihen) {
    await page.locator("#ns-name").fill(name);
    await page.getByRole("button", { name: "Reihe anlegen" }).click();
    await expect(page.getByText(`„${name}"`, { exact: false }).first()).toBeVisible();
  }
  await page.setViewportSize({ width: 1024, height: 480 });
  return reihen;
}

/** Der Meldungsplatz: letztes Kind des Abschnitts „Lehrwerke". */
function platz(page: Page) {
  return page.locator("section", { has: page.getByRole("heading", { name: "Lehrwerke", exact: true }) })
    .locator(":scope > div").last();
}

/**
 * `nachruecken`: um so viel rückt der Platz ohne jeden Scroll nach oben – beim Löschen die Höhe der verschwindenden
 * Zeile (Review B-385). Ohne diesen Abstand schnitte der Platz nach dem Löschen den Sichtbereich, und
 * `toBeInViewport` würde grün, obwohl nichts gescrollt hat.
 */
async function platzLiegtUnterDemBild(page: Page, nachruecken = 0) {
  const [oben, hoehe] = await Promise.all([
    platz(page).evaluate((el) => el.getBoundingClientRect().top),
    page.evaluate(() => window.innerHeight),
  ]);
  expect(oben - nachruecken, "Vorbedingung: Platz liegt unter dem Sichtbereich").toBeGreaterThan(hoehe);
}

test("„Reihe … gelöscht.“ landet im Bild, obwohl der Platz weit unten liegt", async ({ page }) => {
  const reihen = await langeListe(page, "0 Löschreihe");
  const knopf = page.getByRole("button", { name: `Löschen: „${reihen[0]}"`, exact: true });
  await knopf.evaluate((el) => el.scrollIntoView({ block: "start" }));
  await platzLiegtUnterDemBild(page, await knopf.evaluate((el) => el.closest("tr")!.getBoundingClientRect().height));

  page.once("dialog", (d) => d.accept());
  await knopf.click();
  const meldung = page.getByText(`Reihe „${reihen[0]}" gelöscht.`, { exact: true });
  await expect(meldung).toHaveCount(1);
  await expect(meldung).toBeInViewport();
});

test("„Gespeichert.“ einer Reihe landet im Bild, obwohl der Platz weit unten liegt", async ({ page }) => {
  const reihen = await langeListe(page, "0 Speicherreihe");
  await page.getByRole("button", { name: `Reihe bearbeiten: „${reihen[0]}"`, exact: true }).click();
  await page.getByLabel("Name der Reihe (Pflicht)", { exact: true }).fill(`${reihen[0]} umbenannt`);
  const knopf = page.getByRole("button", { name: `Speichern: „${reihen[0]}"`, exact: true });
  await knopf.evaluate((el) => el.scrollIntoView({ block: "start" }));
  await platzLiegtUnterDemBild(page);

  await knopf.click();
  const meldung = page.getByText("Gespeichert.", { exact: true });
  await expect(meldung).toHaveCount(1);
  await expect(meldung).toBeInViewport();
});
