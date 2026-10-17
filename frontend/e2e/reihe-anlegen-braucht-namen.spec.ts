import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-225. „Reihe anlegen" war bei leerem Namen klickbar, und `submit` kehrte stumm zurück – seit B-212 ohne
 * jeden lokalen Platz, der etwas hätte sagen können. Der Verlags-Knopf daneben war längst gesperrt; der
 * Kommentar unter dem Formular behauptete das auch für die Reihe.
 *
 * Jetzt gilt für beide Knöpfe dasselbe: ohne Namen gesperrt, und das Feld nennt den Grund („Pflicht").
 * Rot vor dem Fix an der ersten Zusicherung: der Knopf war bedienbar.
 */

const FATHER = { id: "1", pin: "0000" };

test("„Reihe anlegen“ ist ohne Namen gesperrt und wird mit Namen bedienbar", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.goto("/vater/lehrwerke");

  const knopf = page.getByRole("button", { name: "Reihe anlegen" });
  const name = page.locator("#ns-name");
  // Vorbedingung, kein Fall: Das Feld startet leer (keine Vorbelegung). Sie steht hier, damit ein Rot an
  // der nächsten Zeile „gesperrt obwohl leer" bedeutet und nicht „irgendwer hat das Feld vorbelegt".
  await expect(name).toHaveValue("");
  await expect(knopf).toBeDisabled();

  // Nur Leerzeichen sind kein Name – derselbe `trim()` wie beim Verlags-Knopf.
  await name.fill("   ");
  await expect(knopf).toBeDisabled();

  await name.fill("Ein Name");
  await expect(knopf).toBeEnabled();

  // Der Grund steht am Feld, nicht in einem `title` am gesperrten Knopf (B-160).
  await expect(page.getByText("(Pflicht)")).toBeVisible();
});
