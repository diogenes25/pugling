import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-123: Der Creator korrigiert eine bestehende Reihe über die Oberfläche.
 *
 * Eigene Datei, nicht angehängt an `lehrwerke.spec.ts`: die trägt den Durchstich
 * Creator→Fachlehrer→Kind, und eine Spec bricht beim ersten Rot ab und nimmt alles Nachfolgende mit
 * (B-109). Der Durchstich bleibt ein Weg.
 *
 * Diese Spec ist zugleich der Rollengang der Story – sie fährt genau die Reihenfolge, in der die Lücke
 * bestand: anlegen ging, korrigieren nicht.
 */

const FATHER = { id: "1", pin: "0000" };
const STAMP = Date.now().toString(36).slice(-5);
const VERLAG = `Prüfverlag ${STAMP}`;
const ALT = `Erstname ${STAMP}`;
const NEU = `Zweitname ${STAMP}`;

test("Reihe anlegen, dann Namen ändern und Verlag entfernen", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.goto("/vater/lehrwerke");

  // ---- Anlegen, mit Verlag und Fach: nur was gesetzt ist, kann nachher entfernt werden ----
  await page.locator("#ns-new-publisher").fill(VERLAG);
  await page.getByRole("button", { name: "Verlag anlegen" }).click();
  await expect(page.getByText(`„${VERLAG}" steht im Vokabular.`)).toBeVisible();

  await page.locator("#ns-name").fill(ALT);
  await page.locator("#ns-subject").selectOption({ label: "Englisch" });
  await page.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(page.getByText(/steht im Katalog/)).toBeVisible();

  const zeile = page.getByRole("row", { name: new RegExp(ALT) });
  await expect(zeile).toContainText(VERLAG);
  await expect(zeile).toContainText("Englisch");

  // ---- Bearbeiten: Name ändern UND Verlag entfernen, in einem Absenden ----
  await page.getByRole("button", { name: `Reihe bearbeiten: „${ALT}"` }).click();

  // AK 5: Die Erklärung zum stehenbleibenden Kurznamen hängt am Namensfeld – geprüft am Popover, nicht
  // an einer Kopie neben dem Feld. Am Feld selbst steht nur der Wert.
  // Der Ausschnitt ist die AUSSAGE („Kurzname"), nicht der Satzbau: „darunter bleibt aber" wäre reine
  // Formulierung und bräche bei der ersten Textpflege. „Kurzname" kommt in keinem anderen Hilfetext vor.
  await page.getByRole("button", { name: 'Erklärung zu „Name der Reihe"' }).click();
  await expect(page.getByRole("note").filter({ hasText: "Kurzname" })).toBeVisible();
  await page.keyboard.press("Escape");
  // Belegen, dass Escape wirkt: `fill()` macht keinen Hit-Test, ein offenes Popover über dem Feld würde
  // den Test also nicht rot machen (Muster feldhilfe.spec.ts).
  await expect(page.getByRole("note")).toHaveCount(0);

  await page.getByLabel("Name der Reihe", { exact: true }).fill(NEU);
  // „– keine Angabe –" ist hier eine Handlung, kein Ausgangszustand: der Verlag war vorausgewählt.
  await page.locator('select[id^="se-publisher-"]').selectOption("");
  // Der Knopf nennt den PERSISTIERTEN Namen, im Feld steht schon der neue – darum hier `ALT`.
  await page.getByRole("button", { name: `Speichern: „${ALT}"` }).click();
  await expect(page.getByText("Gespeichert.")).toBeVisible();

  // Zuklappen, bevor die Liste geprüft wird: das offene Formular trägt denselben Namen in seinem
  // Eingabefeld, und eine Zeilen-Auswahl wäre sonst doppeldeutig. Der Knopf trägt jetzt den NEUEN
  // Namen – die Zeile ist nach dem Speichern neu geladen.
  await page.getByRole("button", { name: `Bearbeiten schließen: „${NEU}"` }).click();

  // ---- Beides steht in der Liste ----
  const neueZeile = page.getByRole("row", { name: new RegExp(NEU) });
  await expect(neueZeile).toBeVisible();
  await expect(neueZeile).not.toContainText(VERLAG);
  // Das Fach wurde nicht angefasst und muss darum stehen bleiben – der Beleg, dass nur Geändertes
  // gesendet wird und ein PATCH ohne Angabe nichts leert.
  await expect(neueZeile).toContainText("Englisch");
  await expect(page.getByRole("row", { name: new RegExp(ALT) })).toHaveCount(0);
});

test("Fach entfernen räumt auch den Fachnamen ab", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.goto("/vater/lehrwerke");

  const name = `Fachlos ${STAMP}`;
  await page.locator("#ns-name").fill(name);
  await page.locator("#ns-subject").selectOption({ label: "Englisch" });
  await page.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(page.getByText(/steht im Katalog/)).toBeVisible();

  const zeile = page.getByRole("row", { name: new RegExp(name) });
  await expect(zeile).toContainText("Englisch");

  await page.getByRole("button", { name: `Reihe bearbeiten: „${name}"` }).click();
  await page.locator('select[id^="se-subject-"]').selectOption("");
  await page.getByRole("button", { name: `Speichern: „${name}"` }).click();
  await expect(page.getByText("Gespeichert.")).toBeVisible();
  await page.getByRole("button", { name: `Bearbeiten schließen: „${name}"` }).click();

  // Der eigentliche Fallstrick der Story: der Fach*name* ist eine gespeicherte Spalte und die Zeile
  // zeigt ihn als Rückfallebene. Räumte `clearSubject` ihn nicht mit ab, wäre „Englisch" weiter zu
  // lesen – die Reihe behauptete ein Fach, das sie nicht mehr hat.
  await expect(page.getByRole("row", { name: new RegExp(name) })).not.toContainText("Englisch");
});

/*
 * B-143, Zustand A: Ein Fach wird gelöscht, während eine Reihe darauf zeigt. Die Reihe behält den
 * Fach*namen* (gespeicherte Spalte, `SetNull` räumt nur die Id) – und genau diesen Zustand konnte das
 * Formular vorher nicht ausdrücken: es zeigte „– keine Angabe –", ein Speichern meldete „Gespeichert."
 * und der Name stand weiter da.
 *
 * Der Ausgangszustand entsteht hier auf dem GEWÖHNLICHEN Weg (Fach löschen), nicht per API-Kunstgriff –
 * das ist der Punkt der Akzeptanzbedingung.
 */
test("Ein gelöschtes Fach bleibt als Freitext sichtbar und ist über die Oberfläche wegzubekommen", async ({ page }) => {
  await vaterLogin(page, FATHER);

  // ---- Ein eigenes Fach anlegen: es wird gleich gelöscht, und das darf kein geseedetes treffen ----
  const fach = `Wegwerf-Fach ${STAMP}`;
  await page.goto("/vater/katalog");
  await page.locator("#ca-new-subject").fill(fach);
  await page.getByRole("button", { name: "Neues Fach anlegen" }).click();
  await expect(page.locator("#ca-subject")).toContainText(fach);

  // ---- Eine Reihe daran hängen ----
  const reihe = `Freitext-Reihe ${STAMP}`;
  await page.goto("/vater/lehrwerke");
  await page.locator("#ns-name").fill(reihe);
  await page.locator("#ns-subject").selectOption({ label: fach });
  await page.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(page.getByText(/steht im Katalog/)).toBeVisible();
  await expect(page.getByRole("row", { name: new RegExp(reihe) })).toContainText(fach);

  // ---- Das Fach löschen. Erlaubt, weil daran nur Katalog-Zeug hängt (B-144 sperrt nur Kind-Daten) ----
  await page.goto("/vater/katalog");
  await page.locator("#ca-subject").selectOption({ label: fach });
  page.once("dialog", (d) => d.accept());
  await page.getByRole("button", { name: `Fach „${fach}" löschen` }).click();
  await expect(page.getByText("Fach gelöscht.")).toBeVisible();

  // ---- Die Zeile zeigt den Namen weiter – und das Formular sagt jetzt dasselbe ----
  await page.goto("/vater/lehrwerke");
  await expect(page.getByRole("row", { name: new RegExp(reihe) })).toContainText(fach);

  await page.getByRole("button", { name: `Reihe bearbeiten: „${reihe}"` }).click();
  const fachFeld = page.locator('select[id^="se-subject-"]');
  // AK 1: vorausgewählt, benannt, nicht wählbar. Vorher stand hier „– keine Angabe –".
  await expect(fachFeld).toHaveValue("__freetext__");
  await expect(fachFeld.locator("option[value='__freetext__']")).toHaveText(`${fach} (Freitext)`);
  await expect(fachFeld.locator("option[value='__freetext__']")).toBeDisabled();

  // AK 2: über die bestehende Leer-Option wegzubekommen – kein eigener Knopf (Entscheidung 3).
  await fachFeld.selectOption("");
  await page.getByRole("button", { name: `Speichern: „${reihe}"` }).click();
  await expect(page.getByText("Gespeichert.")).toBeVisible();
  await page.getByRole("button", { name: `Bearbeiten schließen: „${reihe}"` }).click();

  await expect(page.getByRole("row", { name: new RegExp(reihe) })).not.toContainText(fach);
});
