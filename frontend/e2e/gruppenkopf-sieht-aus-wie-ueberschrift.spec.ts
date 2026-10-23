import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-176. Die Feld-Typografie (12 px, fett, Versalien) hing am Elementnamen `label`. Als der Review-Fix zu
 * B-11 die Gruppen-Überschrift „Freigabe (Startzustand)" aus a11y-Gründen zu einem `span` machte, ging die
 * Optik mit – in derselben Karte stand sie als Fließtext neben zwei Überschriften.
 *
 * Gemessen wird der BERECHNETE Stil im echten Browser gegen das echte Stylesheet: Typografie ist in happy-dom
 * nicht zusicherbar, hier schon. Vorbild ist das `<label>` „Lern-Standards" derselben Karte; die Zusicherung
 * fragt nach Gleichheit mit ihm, nicht nach festen Zahlen – ändert jemand die Feld-Typografie, bleibt sie grün,
 * solange beide zusammen wandern. Vor dem Fix: `text-transform` „none" statt „uppercase".
 *
 * Der zweite Fall ist eine Absichtserklärung zu Entscheidung 3 der Story: Das „i" des Hinweis-Knopfs steht an
 * fünf Stellen INNERHALB des Gruppenkopfs; `text-transform` wird vererbt – aber nicht in Formular-Controls,
 * dort setzt der UA-Stylesheet es zurück. Gemessen: grün vor dem Fix, grün mit der Kopf-Regel und OHNE eigene
 * Gegenregel (die erste Fassung hatte eine und hielt sie für tragend). Rot wird er nur, wenn jemand dem Knopf
 * ausdrücklich `text-transform: inherit` gibt.
 *
 * Das ist zugleich der Rollengang aus der Supervisor-Rolle (Chrome-Extension nicht verbunden, Freigabe 6);
 * ob die Karte mit drei gleichen Überschriften auch GUT aussieht, bleibt ein Blick des Menschen.
 */

const FATHER = { id: "1", pin: "0000" };
const EIGENSCHAFTEN = ["text-transform", "font-weight", "font-size", "letter-spacing"] as const;

test("„Freigabe (Startzustand)“ trägt dieselbe Typografie wie die Feld-Beschriftung daneben", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.goto("/vater/exercises/neu");

  const vorbild = page.locator("label").filter({ hasText: /^Lern-Standards/ });
  const kopf = page.locator("span.label-row").filter({ hasText: /^Freigabe/ });
  await expect(vorbild).toBeVisible();
  await expect(kopf).toBeVisible();

  for (const eigenschaft of EIGENSCHAFTEN) {
    const erwartet = await vorbild.evaluate((el, e) => getComputedStyle(el).getPropertyValue(e), eigenschaft);
    await expect(kopf, eigenschaft).toHaveCSS(eigenschaft, erwartet);
  }
});

test("Das „i“ des Hinweis-Knopfs bleibt klein, auch im Gruppenkopf", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.goto("/vater/vocab");

  const kopf = page.locator("span.label-row").filter({ hasText: /^Auch richtig/ }).first();
  const knopf = kopf.locator(".info-hint-btn");
  await expect(knopf).toBeVisible();
  await expect(knopf).toHaveCSS("text-transform", "none");
});
