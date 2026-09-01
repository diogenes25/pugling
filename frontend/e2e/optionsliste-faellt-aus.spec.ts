import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-172: Die Optionsliste eines Referenzfeldes fällt aus — und das Feld darf trotzdem nicht behaupten,
 * es sei nichts gesetzt.
 *
 * Eigene Datei, nicht angehängt an `lehrwerk-bearbeiten.spec.ts` (Regel aus `frontend/CLAUDE.md`: eine
 * Fläche je Spec). Dass es diese Spec überhaupt gibt, ist eine Korrektur an der Schätzung der Story: die
 * hatte „keine neue E2E" damit begründet, der Fall bräuchte einen künstlich kaputten Server. Das stimmt —
 * nur kostet der bei Playwright eine Zeile (`page.route(…).abort()`), und damit ist der teuerste Beleg
 * der billigste geworden.
 *
 * Geprüft wird im echten Chromium, weil genau hier eine Browser-Eigenschaft tragend ist: Ein `<select>`
 * mit Anzeigegröße 1 wählt ohne passende Option die erste selbst. Unter happy-dom gemessen — aber
 * verlassen will man sich darauf nur, wenn es ein Browser bestätigt.
 */

const FATHER = { id: "1", pin: "0000" };
const STAMP = Date.now().toString(36).slice(-5);
const REIHE = `Ausfall ${STAMP}`;

test("Fehlt die Fächerliste, zeigt das Feld das geladene Fach – und der Ausfall ist sichtbar", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.goto("/vater/lehrwerke");

  // Mit funktionierender Liste eine Reihe MIT Fach anlegen: nur ein gesetztes Fach kann verloren gehen.
  await page.locator("#ns-name").fill(REIHE);
  await page.locator("#ns-subject").selectOption({ label: "Englisch" });
  await page.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(page.getByText(/steht im Katalog/)).toBeVisible();

  // Nur den Fächer-Abruf abschießen. Die Reihenliste lädt weiter – das ist der heimtückische Teil:
  // vorher war an der Oberfläche nichts zu sehen, was auf einen Ausfall hindeutete.
  await page.route("**/api/v1/creator/subjects", (route) => route.abort("failed"));
  await page.reload();

  await expect(page.getByText(/^Fächer:/)).toBeVisible();

  await page.getByRole("button", { name: `Reihe bearbeiten: „${REIHE}"` }).click();
  const feld = page.locator('select[id^="se-subject-"]');

  // Der Kern: das Feld zeigt „Englisch (nicht geladen)", nicht „– keine Angabe –".
  await expect(feld).toHaveValue(/\d+/);
  const gewaehlt = feld.locator("option:checked");
  await expect(gewaehlt).toHaveText("Englisch (nicht geladen)");
  expect(await gewaehlt.isDisabled()).toBe(false);

  // Und der Weg zurück ist offen: leeren, es sich anders überlegen, zurück auf das echte Fach.
  const echtes = await feld.inputValue();
  await feld.selectOption("");
  await expect(feld).toHaveValue("");
  await feld.selectOption(echtes);
  await expect(feld).toHaveValue(echtes);

  // Gegenprobe zum eigentlichen Schaden: nichts gewählt, nichts geändert – also nichts zu speichern.
  await page.getByRole("button", { name: `Speichern: „${REIHE}"` }).click();
  await expect(page.getByText("Nichts geändert.")).toBeVisible();
});
