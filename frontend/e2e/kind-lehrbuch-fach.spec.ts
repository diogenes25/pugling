import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-148: Das Lehrbuch eines Kindes verlor bei JEDEM Speichern den Fachnamen, sobald sein Fach gelöscht
 * war — der `clearSubject`-Schalter entstand aus dem Momentanwert, und „das Feld zeigt nichts" hieß
 * damit fälschlich „der Nutzer hat geleert".
 *
 * Eigene Datei statt eines Blocks in `lehrwerke.spec.ts`: die trägt den Durchstich
 * Creator→Fachlehrer→Kind, und eine Spec bricht beim ersten Rot ab und nimmt alles Nachfolgende mit
 * (B-109). Eine Fläche je Spec.
 *
 * Diese Spec ist zugleich der **Rollengang** der Story, und anders als bei B-127/B-143/B-144 ist er hier
 * ohne Ausrede zu führen: Der Weg hängt an keinem `confirmAction`, das die Chrome-Extension blockieren
 * würde. Nur das Löschen des Fachs braucht einen Dialog, und den nimmt Playwright.
 *
 * Der Ausgangszustand entsteht auf dem GEWÖHNLICHEN Weg (Fach anlegen, Buch daran hängen, Fach löschen),
 * nicht per API-Kunstgriff — sonst prüfte die Spec einen Zustand, den das Produkt so nie herstellt.
 */

const FATHER = { id: "1", pin: "0000" };
const STAMP = Date.now().toString(36).slice(-5);
const CHILD = { name: `Fachkind ${STAMP}`, pin: "4321" };

test("Ein gelöschtes Fach am Lehrbuch überlebt das Speichern anderer Felder", async ({ page }) => {
  await vaterLogin(page, FATHER);

  // ---- Ein eigenes Kind: das geseedete trägt Lernstand, den diese Spec nicht anfassen soll ----
  await page.goto("/vater");
  await page.locator("#new-child-name").fill(CHILD.name);
  await page.locator("#new-child-pin").fill(CHILD.pin);
  await page.getByRole("button", { name: "Kind anlegen" }).click();
  const kindZeile = page.getByRole("row", { name: new RegExp(CHILD.name) });
  await expect(kindZeile).toBeVisible();
  const childId = (await kindZeile.getByRole("cell").first().textContent())!.trim();

  // ---- Ein eigenes Fach: es wird gleich gelöscht, und das darf kein geseedetes treffen ----
  const fach = `Wegwerf-Fach ${STAMP}`;
  await page.goto("/vater/katalog");
  await page.locator("#ca-new-subject").fill(fach);
  await page.getByRole("button", { name: "Neues Fach anlegen" }).click();
  await expect(page.locator("#ca-subject")).toContainText(fach);

  // ---- Das Buch am Kind, mit diesem Fach ----
  const buch = `Freitext-Buch ${STAMP}`;
  await page.goto(`/vater/kind/${childId}`);
  await page.locator("#tbnew-title").fill(buch);
  await page.locator("#tbnew-subject").selectOption({ label: fach });
  await page.locator("#tbnew-chapter").fill("Unit 1");
  await page.getByRole("button", { name: "Buch hinterlegen" }).click();
  await expect(page.getByText("Buch hinterlegt.")).toBeVisible();
  await expect(page.getByRole("row", { name: new RegExp(buch) })).toContainText(fach);

  // ---- Das Fach löschen. Erlaubt: daran hängt nur ein Lehrbuch, und dessen Bezug ist optional (B-144) ----
  await page.goto("/vater/katalog");
  await page.locator("#ca-subject").selectOption({ label: fach });
  page.once("dialog", (d) => d.accept());
  await page.getByRole("button", { name: `Fach „${fach}" löschen` }).click();
  await expect(page.getByText("Fach gelöscht.")).toBeVisible();

  // ---- Die Zeile zeigt den Namen weiter (gewollte Rückfallebene), und das Formular sagt jetzt dasselbe ----
  await page.goto(`/vater/kind/${childId}`);
  // Die DATEN-Zeile, nicht die Formular-Zeile darunter: beide tragen den Buchtitel, und nur die erste
  // hat den Bearbeiten-Knopf. Ohne diese Unterscheidung schlägt jede Zusicherung als „strict mode
  // violation" fehl, sobald das Formular offen ist.
  const buchZeile = page.getByRole("row")
    .filter({ has: page.getByRole("button", { name: `${buch} bearbeiten` }) });
  await expect(buchZeile).toContainText(fach);

  await page.getByRole("button", { name: `${buch} bearbeiten` }).click();

  // Auf das BEARBEITEN-Formular einschränken: Das Anlege-Formular („tbnew") steht auf derselben Seite
  // und trägt dieselben Beschriftungen, ebenso wie die Stammdaten weiter oben ihr eigenes „Speichern".
  const formular = page.locator("form").filter({
    has: page.locator('select[id^="tb"][id$="-subject"]:not(#tbnew-subject)'),
  });
  const fachFeld = formular.locator('select[id$="-subject"]');

  // AK 3: vorausgewählt, benannt, nicht wählbar. Vorher stand hier „– keine Angabe –".
  await expect(fachFeld).toHaveValue("__freetext__");
  await expect(fachFeld.locator("option[value='__freetext__']")).toHaveText(`${fach} (Freitext)`);
  await expect(fachFeld.locator("option[value='__freetext__']")).toBeDisabled();

  // ---- AK 1, der Kern der Story: ein ANDERES Feld ändern, das Fach nicht anfassen ----
  await formular.getByLabel("Kapitel als Freitext", { exact: false }).fill("Unit 7");
  await formular.getByRole("button", { name: "Speichern" }).click();
  // Erst zuklappen abwarten: solange das Formular steht, trägt seine Zeile denselben Text.
  await expect(formular).toHaveCount(0);
  await expect(buchZeile).toContainText("Unit 7");
  // Vor dieser Story stand hier „–": das Speichern des Kapitels hatte den Fachnamen mitgenommen.
  await expect(buchZeile).toContainText(fach);

  // ---- AK 4: über die bestehende Leer-Option ist er trotzdem wegzubekommen ----
  await page.getByRole("button", { name: `${buch} bearbeiten` }).click();
  await fachFeld.selectOption("");
  await formular.getByRole("button", { name: "Speichern" }).click();
  await expect(formular).toHaveCount(0);
  await expect(buchZeile).not.toContainText(fach);
});
