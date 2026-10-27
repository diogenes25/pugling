import { test, expect } from "@playwright/test";
import { sohnLogin, vaterLogin } from "./helpers";

/*
 * B-294 – die Sohn-Arcade auf Englisch. Der Umschalter sitzt im HUD, die Wahl liegt im `localStorage` und überlebt
 * ein Neuladen, `<html lang>` zieht mit (Screenreader lesen sonst Englisch mit deutscher Aussprache). Nutzerdaten
 * bleiben: der Plan-Titel ist der Text des Vaters, nicht eine Übersetzung.
 *
 * Deutsch ist für alle anderen Specs gepinnt (`playwright.config.ts` setzt den Schlüssel) – nur diese Spec schaltet
 * um, und sie schaltet am Ende zurück.
 */

const CHILD = { id: "1", pin: "1111" };
const FATHER = { id: "1", pin: "0000" };

test("Der Sohn stellt die Arcade auf Englisch, sie bleibt es nach dem Neuladen, und <html lang> folgt", async ({ page }) => {
  await sohnLogin(page, CHILD);
  const nav = page.locator(".sohn-nav");
  await expect(nav.getByRole("link", { name: "Basis" })).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("lang", "de");

  await page.getByRole("button", { name: "EN – auf Englisch umschalten (Sprache ist Deutsch)" }).click();
  await expect(nav.getByRole("link", { name: "Home" })).toBeVisible();
  await expect(nav.getByRole("link", { name: "Basis" })).toHaveCount(0);
  // Je nach Seed-Stand hat der Sohn heute eine Mission oder noch keine – beide Überschriften sind Chrome und
  // müssen englisch sein; die deutsche darf nicht mehr da sein.
  await expect(page.locator(".screen-title", { hasText: /Daily mission|No mission yet/ })).toBeVisible();
  await expect(page.getByText(/Tagesmission|Noch keine Mission|Diese Woche/)).toHaveCount(0);
  // Auch die Vorlese-Namen des HUD sind umgeschaltet (Screenreader-Pfad).
  // `coins?`: bei genau 1 Münze heißt es „1 coin“ (B-298) – der Saldo von Kind 1 ist mit anderen Specs geteilt.
  await expect(page.getByLabel(/^\d+ coins?$/)).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("lang", "en");

  await page.reload();
  await expect(nav.getByRole("link", { name: "Home" })).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("lang", "en");

  // B-304: auch der Shop spricht Englisch – Reiter, Einführung; der Titel „🛒 Shop“ heißt in beiden Sprachen so.
  await nav.getByRole("link", { name: "Shop" }).click();
  const areas = page.getByRole("group", { name: "Shop sections" });
  await expect(areas.getByRole("button", { name: "Buy", exact: true })).toBeVisible();
  await expect(areas.getByRole("button", { name: /^History$/ })).toBeVisible();
  await expect(page.getByText(/Kaufen|Sachen|Anfragen/)).toHaveCount(0);
  // Die Menge ist Chrome und spricht Englisch (den unübersetzten Titel, AK 2, belegt `arcadeSprache.test.tsx` – `t`
  // gäbe „10 Minuten TV“ unverändert zurück, hier wäre es nicht messbar). Das Seed-Angebot „10 Minuten TV“
  // (10 × `Minute`) löscht keine Spec. Groß-/Kleinschreibung trennt „10 min“ vom deutschen „10 Min“ – auch vom
  // Titel, der „10 Min“ nur großgeschrieben enthält.
  await expect(page.locator(".skin", { hasText: "10 Minuten TV" }).first().locator(".sub")).toHaveText(/^10 min( · |$)/);

  // B-304: der Login liegt außerhalb der Shell und folgt trotzdem der Wahl – nach dem Abmelden steht er englisch da,
  // mit `lang="en"`. Den Rückfall auf Deutsch beim Entmontieren der Arcade belegt `arcadeSprache.test.tsx`: ein
  // `page.goto` läse hier nur das statische `lang="de"` aus `index.html`, nicht die Aufräumzeile in `SohnApp`.
  await nav.getByRole("link", { name: "Skins" }).click();
  await page.getByRole("button", { name: "Sign out" }).click();
  await expect(page.getByRole("button", { name: "▶ GO" })).toBeVisible();
  await expect(page.getByText("Ready for a round?")).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("lang", "en");

  // B-304, AK 3: das Vater-Web bleibt deutsch, obwohl am selben Gerät Englisch gespeichert ist – auch dort, wo es
  // einen mit der Arcade geteilten Helfer liest: dieselbe Menge, die die Arcade eben englisch zeigte, kommt im
  // Vater-Shop aus `unitAmount` und heißt dort „10 Min“.
  await vaterLogin(page, FATHER);
  await page.goto("/vater/shop");
  await page.getByRole("row", { name: /^TV-001 Fernsehzeit/ }).getByRole("button", { name: /^Angebote/ }).click();
  // Die Mengen-Zelle, nicht die Zeile: der Titel „10 Minuten TV“ enthielte „10 Min“ schon von selbst.
  await expect(page.getByRole("row", { name: /10 Minuten TV/ }).first().locator("td").nth(1)).toHaveText("10 Min");

  // Die Wahl überlebt das Abmelden: wer sich wieder anmeldet, ist wieder in der englischen Arcade.
  await sohnLogin(page, CHILD);
  await expect(nav.getByRole("link", { name: "Home" })).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("lang", "en");

  await page.getByRole("button", { name: "DE – switch to German (language is English)" }).click();
  await expect(nav.getByRole("link", { name: "Basis" })).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("lang", "de");
});
