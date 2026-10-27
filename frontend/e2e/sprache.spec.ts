import { test, expect } from "@playwright/test";
import { sohnLogin } from "./helpers";

/*
 * B-294 – die Sohn-Arcade auf Englisch. Der Umschalter sitzt im HUD, die Wahl liegt im `localStorage` und überlebt
 * ein Neuladen, `<html lang>` zieht mit (Screenreader lesen sonst Englisch mit deutscher Aussprache). Nutzerdaten
 * bleiben: der Plan-Titel ist der Text des Vaters, nicht eine Übersetzung.
 *
 * Deutsch ist für alle anderen Specs gepinnt (`playwright.config.ts` setzt den Schlüssel) – nur diese Spec schaltet
 * um, und sie schaltet am Ende zurück.
 */

const CHILD = { id: "1", pin: "1111" };

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

  // B-299: beim Verlassen der Arcade fällt `<html lang>` auf Deutsch zurück – der Login ist (noch) deutsch. Abmelden
  // statt `page.goto`: ein Seitenwechsel lädt `index.html` neu und läse dessen statisches `lang="de"`, nicht die
  // Aufräumzeile in `SohnApp`.
  await nav.getByRole("link", { name: "Skins" }).click();
  // „Abmelden“ ist hier noch ein deutsches Literal – mit B-295 wandert der Text, dann diese Zeile mit.
  await page.getByRole("button", { name: "Abmelden" }).click();
  await expect(page.getByRole("button", { name: "▶ LOS" })).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("lang", "de");

  // Die Wahl überlebt das Abmelden: wer sich wieder anmeldet, ist wieder in der englischen Arcade.
  await sohnLogin(page, CHILD);
  await expect(nav.getByRole("link", { name: "Home" })).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("lang", "en");

  await page.getByRole("button", { name: "DE – switch to German (language is English)" }).click();
  await expect(nav.getByRole("link", { name: "Basis" })).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("lang", "de");
});
