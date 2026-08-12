import { test, expect, type Page } from "@playwright/test";

/*
 * Der ganze Creator-Weg in einem Zug: Reihe anlegen → Unit anlegen → Übung über den
 * Fach/Reihe/Unit-Kaskadenpicker anlegen → in der Verwaltung wiederfinden → einem Kind zuweisen.
 *
 * Seit B-106 (2026-08-05) hängt jede Übung zwingend an einer Lehrwerk-Unit statt an einem Kapitel –
 * `lehrwerke.spec.ts` deckt Reihe/Unit/Fachlehrer-Matching ab, `uebungstypen.spec.ts` deckt jeden
 * Übungstyp einzeln ab. Keine bestehende Spec fährt aber den vollständigen Weg von der Reihe bis zur
 * gespielten Position in einem Durchgang – genau der Weg, den B-106 selbst als offenen Beleg nennt
 * (Kaskadenpicker nie im Browser bedient, nur per HTTP geprüft, siehe B-106 `## Verlauf` 2026-08-05).
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);
const SERIES = { name: `Creator-Weg ${RUN}`, publisher: "Klett" };
const UNIT_LABEL = `Unit 1 – Weg ${RUN}`;
const EXERCISE_TITLE = `Grammatik-Weg ${RUN}`;
// Eigenes Kind statt des geseedeten Sohns: die Specs teilen eine Backend-Instanz.
const CHILD = { name: `Weg-Kind ${RUN}`, pin: "4321" };

async function vaterLogin(page: Page) {
  await page.goto("/vater");
  await page.locator("#fid").fill(FATHER.id);
  await page.locator("#pin").fill(FATHER.pin);
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();
}

/** Legt ein frisches Kind an und liefert dessen Id (erste Spalte der Kinder-Tabelle). */
async function createChild(page: Page): Promise<string> {
  await page.goto("/vater");
  await page.locator("#new-child-name").fill(CHILD.name);
  await page.locator("#new-child-pin").fill(CHILD.pin);
  await page.getByRole("button", { name: "Kind anlegen" }).click();
  const row = page.getByRole("row", { name: new RegExp(CHILD.name) });
  await expect(row).toBeVisible();
  return (await row.getByRole("cell").first().textContent())!.trim();
}

test("Reihe, Unit, Übung, Zuweisung – der Creator-Weg in einem Zug", async ({ page }) => {
  await vaterLogin(page);
  const childId = await createChild(page);

  // ---- 1. Reihe anlegen (geteilter Katalog) ----
  await page.goto("/vater/lehrwerke");
  await page.locator("#ns-name").fill(SERIES.name);
  // "Klett" ist bereits als Verlag geseedet (Grundlage der Green-Line-Reihe) - direkt auswählbar.
  await page.locator("#ns-publisher").selectOption({ label: SERIES.publisher });
  await page.locator("#ns-subject").selectOption({ label: "Englisch" });
  await page.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(page.getByText(/steht im Katalog/)).toBeVisible();
  const seriesRow = page.getByRole("row", { name: new RegExp(SERIES.name) });
  await expect(seriesRow).toBeVisible();

  // ---- 2. Unit anlegen ----
  await seriesRow.getByRole("button", { name: "Units" }).click();
  await page.locator('[id^="unit-label-new"]').fill(UNIT_LABEL);
  await page.getByRole("button", { name: "Unit hinzufügen" }).click();
  await expect(page.getByText(UNIT_LABEL)).toBeVisible();

  // ---- 3. Übung anlegen – über den Knopf AN DER UNIT, nicht über die Adresse ----
  // Der Weg selbst ist hier der Prüfgegenstand: `+ Übung` reicht Fach/Reihe/Unit als Query durch, das
  // Formular startet also bereits in dieser Unit. Ein `goto("/vater/exercises/neu")` prüfte das nicht –
  // es käme mit leerem Kaskadenpicker an und verdeckte einen kaputten Link.
  await page.getByRole("link", { name: `+ Übung zu „${UNIT_LABEL}"` }).click();
  await expect(page).toHaveURL(/\/vater\/exercises\/neu\?.*seriesUnitId=\d+/);
  // Vorbelegt statt gewählt: die drei Pulldowns tragen den Kontext der Unit schon.
  await expect(page.locator('select[aria-label="Fach"]')).not.toHaveValue("");
  await expect(page.locator('select[aria-label="Reihe"]')).not.toHaveValue("");
  await expect(page.locator('select[aria-label="Unit"]')).not.toHaveValue("");
  await page.locator('select[aria-label="Übungstyp"]').selectOption("Grammar");
  await page.locator("#ex-title").fill(EXERCISE_TITLE);
  await page.getByLabel("Anweisung").fill("Setze die richtige Form ein.");
  await page.getByLabel("Aufgabe").fill("She ___ (go) to school.");
  await page.getByLabel("Lösung").fill("goes");
  await page.getByRole("button", { name: "Übung anlegen" }).click();
  await expect(page.getByText(`Übung „${EXERCISE_TITLE}" angelegt.`)).toBeVisible();

  // ---- 4. In der Verwaltung wiederfinden – derselbe Kaskadenpicker, jetzt als Filter ----
  await page.goto("/vater/exercises");
  await page.locator('select[aria-label="Fach"]').selectOption({ label: "Englisch" });
  await page.locator('select[aria-label="Reihe"]').selectOption({ label: SERIES.name });
  await page.locator('select[aria-label="Unit"]').selectOption({ label: UNIT_LABEL });
  // Die Übungsliste ist keine Tabelle (anders als die Positionsliste unten) – der Treffer steht als
  // Freitext-Zeile in der Karte.
  await expect(page.getByText(new RegExp(EXERCISE_TITLE))).toBeVisible();

  // ---- 5. Einem Kind zuweisen (Lehrplan-Position) ----
  await page.goto("/vater/plan/new");
  await page.locator("#plan-title").fill(`Plan ${RUN}`);
  const kindSelect = page.getByRole("combobox", { name: "Kind" });
  await kindSelect.selectOption(childId);
  await page.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(page).toHaveURL(/\/vater\/plan\/\d+$/);

  // Dieselbe Filterleiste wie beim Suchen, jetzt am Positions-Formular – ohne sie könnte die frische
  // Übung außerhalb der Standard-Seitengröße des geteilten Katalogs liegen.
  await page.locator('select[aria-label="Fach-Filter"]').selectOption({ label: "Englisch" });
  await page.locator('select[aria-label="Reihe-Filter"]').selectOption({ label: SERIES.name });
  await page.locator('select[aria-label="Unit-Filter"]').selectOption({ label: UNIT_LABEL });

  const exRadio = page.getByRole("radio", { name: new RegExp(EXERCISE_TITLE) });
  await expect(exRadio).toBeVisible();
  await exRadio.check();
  await page.getByRole("button", { name: /Position hinzufügen/ }).click();
  const positionRow = page.getByRole("row", { name: new RegExp(EXERCISE_TITLE) });
  await expect(positionRow).toBeVisible();
});
