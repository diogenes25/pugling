import { test, expect, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-206. Die Einkleidung einer Übung war unsichtbar: Der KI-Creator trug sie ein, das Formular reichte sie
 * stumm durch, und ein anderswo gelöschtes Stichwort machte das Speichern unmöglich — mit einer Meldung,
 * die eine Größe nannte, für die es kein Feld gab.
 *
 * Eigene, kurze Spec (`frontend/CLAUDE.md`: eine Fläche je Spec). Gefahren wird der Weg, den kein
 * Komponententest tragen kann, und zwar als **Kreis**: einkleiden → Löschen wird verweigert → ausziehen →
 * Löschen geht. Nur so ist belegt, dass der Riegel eine Tür hat und nicht bloß eine Wand ist.
 *
 * Der Typ ist **Grammatik** und nicht Vokabel, und das ist tragend: nur ein inhaltsbearbeitbarer Typ hat
 * den zweiten Speicher-Knopf („Inhalt speichern"). Der baute seine Nutzlast früher aus dem geladenen Stand
 * und drehte damit jede Metadaten-Änderung still zurück — mit einer Vokabel-Übung wäre die Spec dafür
 * strukturell blind gewesen.
 *
 * Das ist zugleich der Rollengang aus der Creator-Rolle.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);
const SUBJECT = `Einkleidung ${RUN}`;
const EXERCISE = `Eingekleidete Übung ${RUN}`;
const TAG = `Seifenkiste ${RUN}`;

/** Fach + Reihe + Unit — seit B-106 hängt jede Übung zwingend an einer Unit. */
async function katalogVorbereiten(page: Page) {
  await page.goto("/vater/katalog");
  await page.getByPlaceholder("z. B. Französisch").fill(SUBJECT);
  await page.getByRole("button", { name: "Neues Fach anlegen" }).click();
  await expect(page.locator("#ca-subject")).toHaveValue(/\d+/);

  await page.goto("/vater/lehrwerke");
  await page.locator("#ns-name").fill(SUBJECT);
  await page.locator("#ns-subject").selectOption({ label: SUBJECT });
  await page.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(page.getByText(/steht im Katalog/)).toBeVisible();
  const reihe = page.getByRole("row", { name: new RegExp(SUBJECT) });
  await reihe.getByRole("button", { name: "Units" }).click();
  await page.locator('[id^="unit-label-new"]').fill("Unit 1");
  await page.getByRole("button", { name: "Unit hinzufügen" }).click();
  await expect(page.getByRole("button", { name: "Unit 1 bearbeiten" })).toBeVisible();
}

/**
 * Ein Stichwort entsteht in dieser App **nebenbei** — es gibt kein Anlege-Formular in der Verwaltung.
 * Der kürzeste Weg ohne Datei-Upload ist der Interessen-Editor am Kind (create-if-missing).
 */
async function stichwortAnlegen(page: Page) {
  await page.goto("/vater/kind/1");
  await expect(page.getByRole("heading", { name: "Interessen", exact: true })).toBeVisible();
  await page.locator("#int-label").fill(TAG);
  // `exact`, sonst trifft der Teilstring den Knopf „Betreuer hinzufügen" derselben Seite.
  await page.getByRole("button", { name: "Hinzufügen", exact: true }).click();
  await page.getByRole("button", { name: "Interessen speichern" }).click();
  await expect(page.getByText("Interessen gespeichert.")).toBeVisible();
}

/** Die Schlagwort-Verwaltung, auf dieses eine Stichwort gefiltert. */
async function stichwortSuchen(page: Page) {
  await page.goto("/vater/media");
  // Die Verwaltung liegt eingeklappt hinter ihrem eigenen Knopf – sie ist eine seltene Handlung.
  await page.getByRole("button", { name: /Schlagwort-Vokabular verwalten/ }).click();
  // Enter im Feld statt „Suchen": die Seite trägt zwei gleichnamige Knöpfe (Schlagworte und Bilder),
  // und das Feld schickt sein eigenes Formular ab – kein Raten, welcher der richtige ist.
  await page.getByLabel("Schlagwort suchen").fill(TAG);
  await page.getByLabel("Schlagwort suchen").press("Enter");
}

/** Öffnet den Bearbeiten-Dialog der frisch angelegten Übung. */
async function dialogOeffnen(page: Page) {
  await page.goto("/vater/exercises");
  await page.locator('select[aria-label="Fach"]').selectOption({ label: SUBJECT });
  await page.locator('select[aria-label="Reihe"]').selectOption({ label: SUBJECT });
  await page.locator('select[aria-label="Unit"]').selectOption({ label: "Unit 1" });
  await expect(page.getByText(EXERCISE, { exact: true })).toBeVisible();
  await page.getByRole("button", { name: "✏️ Bearbeiten" }).first().click();
  // Auf den DIALOG warten, nicht auf „irgendeine Combobox": `{ name: "" }` ist ein Teilstring-Vergleich und
  // trifft jedes Auswahlfeld der Seite – auch die drei Filter dahinter. Die Zusicherung könnte nie fallen.
  await expect(page.locator("#ed-title")).toBeVisible();
}

test("Die Einkleidung ist sichtbar, änderbar — und sperrt das Löschen ihres Stichworts", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await katalogVorbereiten(page);
  await stichwortAnlegen(page);

  // ---- Übung anlegen (ohne Einkleidung: die setzt sonst der KI-Creator) ----
  await page.goto("/vater/exercises/neu");
  await page.locator('select[aria-label="Fach"]').selectOption({ label: SUBJECT });
  await page.locator('select[aria-label="Reihe"]').selectOption({ label: SUBJECT });
  await page.locator('select[aria-label="Unit"]').selectOption({ label: "Unit 1" });
  await page.locator('select[aria-label="Übungstyp"]').selectOption("Grammar");
  await page.locator("#ex-title").fill(EXERCISE);
  await page.getByLabel("Anweisung").fill("Setze die richtige Form ein.");
  await page.getByLabel("Aufgabe").fill("She ___ (go) to school.");
  await page.getByLabel("Lösung").fill("goes");
  await page.getByRole("button", { name: "Übung anlegen" }).click();
  await expect(page.getByText(`Übung „${EXERCISE}" angelegt.`)).toBeVisible();

  // ---- AK 2: „nicht eingekleidet" steht ausdrücklich da, statt leer zu bleiben ----
  await dialogOeffnen(page);
  await expect(page.getByText(/Nicht eingekleidet/)).toBeVisible();

  // Der Begriff ist erklärungsbedürftig und steht darum in `fieldHelp.ts`, nicht am Feld.
  const erklaerung = page.getByLabel('Erklärung zu „Eingekleidet in"', { exact: true });
  await erklaerung.click();
  await expect(page.getByText(/ändern nicht, WAS geübt wird/)).toBeVisible();
  // Über denselben Knopf zu, nicht per Escape: das schlösse den ganzen Dialog mit.
  await erklaerung.click();
  await expect(page.getByText(/ändern nicht, WAS geübt wird/)).toHaveCount(0);

  // ---- Einkleiden und speichern ----
  await page.locator("#ed-dressing").selectOption({ label: TAG });
  await page.getByRole("button", { name: "Einkleidung hinzufügen" }).click();
  await expect(page.getByText(/Nicht eingekleidet/)).toHaveCount(0);
  await page.getByRole("button", { name: "Beschreibung speichern" }).click();
  await expect(page.getByText("Gespeichert.")).toBeVisible();

  /*
   * Und jetzt der zweite Speicher-Knopf desselben Dialogs. Er schickt denselben Vollersatz-PUT; baute er
   * seine Nutzlast weiter aus dem geladenen Stand, drehte er die gerade gespeicherte Einkleidung still
   * zurück — eine gespeicherte Änderung, die ein Klick daneben rückgängig macht.
   */
  await page.getByRole("button", { name: "Inhalt speichern" }).click();
  await expect(page.getByText("Inhalt gespeichert.")).toBeVisible();

  // ---- AK 1: nach dem Neuöffnen steht der NAME da, nicht die Id ----
  await page.reload();
  await dialogOeffnen(page);
  await expect(page.getByRole("button", { name: `Einkleidung „${TAG}" entfernen` })).toBeVisible();
  await page.getByRole("button", { name: "Schließen", exact: true }).click();

  // ---- AK 6: Löschen wird verweigert und nennt den Grund, bevor überhaupt gefragt wird ----
  // Playwright weist Dialoge von sich aus ab. Der Handler ist hier eine Falle mit Absicht: Käme eine
  // Rückfrage, wäre das der Defekt (ein Dialog, der etwas verspricht, was der Server verweigert).
  let gefragt = false;
  page.on("dialog", (d) => { gefragt = true; d.dismiss(); });
  // Über die Suche, nicht über die erste Seite: die Taxonomie ist global und gedeckelt (`take=200`),
  // ein frisches Stichwort steht dort nicht zwangsläufig.
  await stichwortSuchen(page);
  const zeile = page.getByRole("row", { name: new RegExp(TAG) });
  await expect(zeile).toBeVisible();
  await zeile.getByRole("button", { name: "Löschen" }).click();
  await expect(page.getByText(/kleidet 1 Übung\(en\) ein/)).toBeVisible();
  expect(gefragt, "Ein gesperrtes Löschen fragt nicht erst nach").toBe(false);

  // ---- Die Tür: ausziehen, dann geht das Löschen ----
  await dialogOeffnen(page);
  await page.getByRole("button", { name: `Einkleidung „${TAG}" entfernen` }).click();
  await expect(page.getByText(/Nicht eingekleidet/)).toBeVisible();
  await page.getByRole("button", { name: "Beschreibung speichern" }).click();
  await expect(page.getByText("Gespeichert.")).toBeVisible();

  page.removeAllListeners("dialog");
  page.on("dialog", (d) => d.accept());
  await stichwortSuchen(page);
  await page.getByRole("row", { name: new RegExp(TAG) }).getByRole("button", { name: "Löschen" }).click();
  await expect(page.getByText("Schlagwort gelöscht.")).toBeVisible();
});
