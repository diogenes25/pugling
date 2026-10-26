import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-268, Fehlerweg im echten Browser. „Reihe anlegen & wählen“ trägt `disabled={busy}`; ein Knopf, der mit dem
 * Fokus darauf gesperrt wird, gibt den Fokus im Browser an `body` ab (die HTML-Regel „focus fixup“). jsdom setzt
 * diese Regel nicht um – darum steht der Fall hier und nicht nur im Komponententest. Scheitert das Anlegen, soll
 * die Tastaturnutzerin im Namensfeld weitermachen können, statt ihren Platz zu verlieren.
 *
 * Das Scheitern kommt aus `page.route` (500): Gegenstand ist der Fokus, nicht der Grund des Fehlers.
 */

const FATHER = { id: "1", pin: "0000" };

test("scheitert „Reihe anlegen & wählen“, steht der Fokus im Namensfeld statt auf body", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.route("**/api/v1/creator/textbook-series", (route) =>
    route.request().method() === "POST"
      ? route.fulfill({ status: 500, contentType: "application/problem+json",
        body: JSON.stringify({ title: "Server error", status: 500, detail: "Probe", code: "internal_error" }) })
      : route.continue());
  await page.goto("/vater/kind/1");

  const formular = page.locator("form").filter({ has: page.locator("#tbnew-series") });
  await formular.getByRole("button", { name: "+ Neue Reihe" }).click();
  const name = formular.getByLabel("Name der neuen Reihe", { exact: true });
  await name.fill("Fokusprobe");
  // Per Tastatur auslösen: der Knopf hat den Fokus, wenn er gesperrt wird – genau der Weg einer Tastaturnutzerin.
  const anlegen = formular.getByRole("button", { name: /Reihe anlegen/ });
  await anlegen.focus();
  await page.keyboard.press("Enter");

  await expect(formular.getByText(/Probe|Server error|fehlgeschlagen/).first()).toBeVisible();
  await expect.poll(() => page.evaluate(() => document.activeElement?.id || document.activeElement?.tagName))
    .toBe("tbnew-neue-reihe");
});
