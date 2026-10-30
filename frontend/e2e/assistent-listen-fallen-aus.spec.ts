import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-188. Der Assistent lädt drei Optionslisten (Kinder, Fächer, Arten) und zeigte **keinen** ihrer Fehler.
 *
 * Der schwerste Fall ist `children`: Scheitert der Abruf, bleibt `data` auf `null`, und „Bestehendes Kind"
 * ist dann gesperrt. Der Assistent behauptet damit stumm *„du hast keine Kinder"* und lässt nur noch das
 * Anlegen offen — der Vater legt ein Kind ein zweites Mal an, das es längst gibt. Das ist dieselbe falsche
 * Handlungsanweisung wie in B-162, nur zwei Schritte früher und mit einem doppelten Kind als Folge.
 *
 * Eigene Datei, nicht in `assistent.spec.ts` (`frontend/CLAUDE.md`: eine Fläche je Spec) — die fährt den
 * Durchstich, hier fällt absichtlich etwas aus. Vorbild ist `optionsliste-faellt-aus.spec.ts`: Ein
 * künstlich kaputter Server kostet bei Playwright eine Zeile, und damit ist der teuerste Beleg der
 * billigste.
 *
 * Das ist zugleich der Rollengang aus der Supervisor-Rolle.
 */

const FATHER = { id: "1", pin: "0000" };

test("Fällt die Kinderliste aus, sagt der Assistent es — statt „du hast keine Kinder“ zu behaupten", async ({ page }) => {
  await vaterLogin(page, FATHER);

  // Nur die Kinderliste abschießen. Der Rest des Assistenten lädt weiter – das ist der heimtückische Teil:
  // Vorher war an der Oberfläche nichts zu sehen, was auf einen Ausfall hindeutete.
  await page.route("**/api/v1/supervisor/children", (route) => route.abort("failed"));
  await page.goto("/vater/wizard");

  const knopf = page.getByRole("radio", { name: "Bestehendes Kind" });
  await expect(knopf).toBeVisible();
  // Auf die Karte des Schritts eingeschränkt: `getByRole("alert")` ist seitenweit und bräche in Strict
  // Mode, sobald irgendwo sonst ein zweiter Alert entsteht. Je Schritt rendert genau eine Karte.
  const meldung = page.locator("section.card").getByRole("alert");

  // Der Kern: Der Knopf bleibt zu Recht gesperrt – es gibt ja keine Liste. Was fehlte, ist der
  // Unterschied zwischen „keine da" und „nicht geladen", und der steht jetzt daneben.
  await expect(knopf).toBeDisabled();
  await expect(meldung).toContainText("Deine Kinder konnten nicht geladen werden");

  /*
   * Und der Weg zurück, ohne den die Meldung eine Verschlechterung wäre: Über den Stepper zurück und
   * wieder vor ändert KEINE Abhängigkeit von `useAsync`, es fliegt also keine neue Abfrage — ausgerechnet
   * die Bewegung, die ein Nutzer versucht, ist die wirkungslose. Der einzige andere Ausweg wäre F5, und
   * der kostet den ganzen Assistenten.
   */
  await page.unroute("**/api/v1/supervisor/children");
  await page.getByRole("button", { name: "Erneut laden" }).click();

  await expect(meldung).toHaveCount(0);
  await expect(knopf).toBeEnabled();

  // Gegenprobe: Die Liste ist wirklich da, nicht nur der Knopf wieder hell.
  await knopf.click();
  await expect(page.getByRole("combobox", { name: "Kind" }).locator("option")).not.toHaveCount(0);
});
