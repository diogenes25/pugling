import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-194. Der achte Fall der Regel aus B-172: Fiel die Kinderliste aus, stand im Bearbeiten-Formular des
 * Plans ein LEERES Kind-Feld – für einen Plan, der ein Kind hat – und `children.error` wurde nirgends
 * gerendert. Kein Datenverlust (ohne Optionen feuert kein `change`), aber ein unsichtbarer Ausfall.
 *
 * Nur die Liste wird abgeschossen (`…/supervisor/children`, ohne Pfad dahinter): Der einzelne Datensatz
 * `…/children/1`, aus dem die Seite den Namen zieht, lädt weiter – genau so entsteht die Beschriftung
 * „<Name> (nicht geladen)" ohne eine Vertragsänderung.
 *
 * Das ist zugleich der Rollengang aus der Supervisor-Rolle.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);

test("Fällt die Kinderliste aus, zeigt das Plan-Formular das geladene Kind und sagt, was fehlt", async ({ page }) => {
  await vaterLogin(page, FATHER);

  // Der Plan entsteht mit funktionierender Liste – erst danach fällt sie aus.
  await page.goto("/vater/plan/new");
  await page.locator("#plan-title").fill(`Kinderliste ${RUN}`);
  await page.getByRole("combobox", { name: "Kind" }).selectOption("1");
  await page.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(page).toHaveURL(/\/vater\/plan\/\d+$/);

  await page.route("**/api/v1/supervisor/children", (route) => route.abort("failed"));
  await page.getByRole("button", { name: "Bearbeiten" }).click();

  // Der Kern: Das Feld zeigt das Kind des Plans als gewählte Option – als „nicht geladen" beschriftet –
  // und darüber steht, warum. Vorher: leeres Feld, keine Meldung.
  const kind = page.getByRole("combobox", { name: "Kind" });
  await expect(kind.locator("option:checked")).toHaveText(/\(nicht geladen\)$/);
  await expect(page.getByRole("alert")).toContainText("Kinder:");
  // Ein Plan ohne Kind ist kein wählbarer Zustand: genau eine Option, keine leere Wahl.
  await expect(kind.locator("option")).toHaveCount(1);

  // Speichert der Vater jetzt nur den Titel, reist keine `childId` mit – das Feld steht ja unverändert.
  // Absichtserklärung, kein Beleg für B-194: Das galt auch vor dem Fix (ohne Optionen feuert kein `change`).
  // Rot wird es nur durch eine Regression des Bauteils an dieser Stelle (eine Sonderoption mit leerem Wert,
  // ein `onChange` beim Montieren) – dafür steht es hier, beschriftet nach der Regel der Root-CLAUDE.md.
  await page.getByLabel("Titel", { exact: true }).fill(`Kinderliste ${RUN} v2`);
  const patch = page.waitForRequest((r) => r.method() === "PATCH" && /\/supervisor\/study-plans\/\d+$/.test(r.url()));
  await page.getByRole("button", { name: "Speichern" }).click();
  const rumpf = JSON.parse((await patch).postData() ?? "{}") as Record<string, unknown>;
  expect(rumpf.title).toBe(`Kinderliste ${RUN} v2`);
  expect(rumpf).not.toHaveProperty("childId");
  await expect(page.getByText("Änderungen gespeichert.")).toBeVisible();
});
