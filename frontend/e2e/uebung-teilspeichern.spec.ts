import { test, expect, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * Der Bearbeiten-Dialog einer Übung hat seit B-534 **einen** Speichern-Knopf für Inhalt und Beschreibung.
 * Vorher waren es zwei, und beide schickten denselben Vollersatz-PUT – B-210 war der Fall, in dem der eine
 * den Inhalt des anderen still zurückdrehte. Mit einem Knopf gibt es diese Reihenfolge nicht mehr; was
 * bleibt, ist die Aussage dahinter: **eine** Speicherung trägt Inhalt **und** Metadaten, und keins von
 * beiden geht verloren.
 *
 * Warum E2E und kein Komponententest: `frontend/CLAUDE.md` zieht die Grenze bei „kein nachgebauter
 * Bildschirm mit gefälschtem `fetch`". Der Defekt lebt dort, wo Formular, geladener Stand und echter PUT
 * zusammenkommen.
 *
 * Der Typ ist **Grammatik**: nur ein inhaltsbearbeitbarer Typ trägt den Inhalt in der Config. Mit einer
 * Vokabel-Übung wäre die Spec strukturell blind (ihre Wortpaare gehen über `/items`).
 */

const FATHER = { id: "1", pin: "0000" };

/** Fach + Reihe + Unit + eine Grammatik-Übung mit einer Zeile. Liefert den Übungstitel zurück. */
async function creatorMitUebung(page: Page, marke: string) {
  const name = `${marke} ${Date.now().toString().slice(-6)}`;

  await page.goto("/vater/katalog");
  await page.getByPlaceholder("z. B. Französisch").fill(name);
  await page.getByRole("button", { name: "Neues Fach anlegen" }).click();
  await expect(page.locator("#ca-subject")).toHaveValue(/\d+/);

  await page.goto("/vater/lehrwerke");
  await page.locator("#ns-name").fill(name);
  await page.locator("#ns-subject").selectOption({ label: name });
  await page.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(page.getByText(/steht im Katalog/)).toBeVisible();
  await page.getByRole("row", { name: new RegExp(name) }).getByRole("button", { name: "Units" }).click();
  await page.locator('[id^="unit-label-new"]').fill("Unit 1");
  await page.getByRole("button", { name: "Unit hinzufügen" }).click();
  await expect(page.getByRole("button", { name: "Unit 1 bearbeiten" })).toBeVisible();

  await page.goto("/vater/exercises/neu");
  await page.locator('select[aria-label="Fach"]').selectOption({ label: name });
  await page.locator('select[aria-label="Reihe"]').selectOption({ label: name });
  await page.locator('select[aria-label="Unit"]').selectOption({ label: "Unit 1" });
  await page.locator('select[aria-label="Übungstyp"]').selectOption("Grammar");
  await page.locator("#ex-title").fill(name);
  await page.getByLabel("Anweisung", { exact: true }).fill("Setze die richtige Form ein.");
  await page.getByLabel("Aufgabe", { exact: true }).fill("He ___ (go) to school.");
  await page.getByLabel("Lösung", { exact: true }).fill("goes");
  await page.getByRole("button", { name: "Übung anlegen" }).click();
  await expect(page.getByText(`Übung „${name}" angelegt.`)).toBeVisible();

  return name;
}

/** Öffnet den Bearbeiten-Dialog der einzigen Übung dieses Fachs. */
async function dialogOeffnen(page: Page, name: string) {
  await page.goto("/vater/exercises");
  await page.locator('select[aria-label="Fach"]').selectOption({ label: name });
  await page.locator('select[aria-label="Reihe"]').selectOption({ label: name });
  await page.locator('select[aria-label="Unit"]').selectOption({ label: "Unit 1" });
  await page.getByRole("button", { name: "✏️ Bearbeiten" }).first().click();
  // Auf den DIALOG warten: `#ed-title` gibt es nur in ihm.
  await expect(page.locator("#ed-title")).toBeVisible();
}

test("Ein Speichern sichert Inhalt und Titel zusammen", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const name = await creatorMitUebung(page, "Teilspeichern");
  await dialogOeffnen(page, name);

  // Rot, sobald einer der beiden Teile nicht in der Nutzlast des einen Knopfs landet.
  await page.getByLabel("Aufgabe", { exact: true }).fill("They ___ (go) to school.");
  await page.locator("#ed-title").fill(`${name} v2`);
  await expect(page.getByRole("button", { name: /speichern/i })).toHaveCount(1);
  await page.getByRole("button", { name: "Übung speichern" }).click();
  await expect(page.getByText("Gespeichert.", { exact: true })).toBeVisible();
  // Die Kopfzeile liest den GELADENEN Stand, nicht die Listenzeile: sonst stünde oben weiter der alte
  // Name neben dem neuen im Feld — und der vorgelesene Dialogname wäre falsch.
  await expect(page.getByRole("heading", { name: `✏️ Bearbeiten · ${name} v2` })).toBeVisible();

  /*
   * Neu laden, nicht den offenen Dialog befragen: Er zeigt seinen eigenen Zeilen-Zustand und behauptet
   * damit auch dann die neue Fassung, wenn der Server längst die alte hält. Erst der frisch geladene
   * Stand ist ein Zeuge.
   */
  await page.reload();
  // Mit dem ursprünglichen Namen: er benennt Fach/Reihe der Filter, nicht die umbenannte Übung.
  await dialogOeffnen(page, name);
  await expect(page.getByLabel("Aufgabe", { exact: true })).toHaveValue("They ___ (go) to school.");
  await expect(page.locator("#ed-title")).toHaveValue(`${name} v2`);
});

test("Speichern prüft den Titel selbst, statt sich einen 400 zu holen", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const name = await creatorMitUebung(page, "Titelpruefung");
  await dialogOeffnen(page, name);

  let puts = 0;
  page.on("request", (r) => { if (r.method() === "PUT") puts += 1; });

  await page.locator("#ed-title").fill("");
  await page.getByRole("button", { name: "Übung speichern" }).click();

  await expect(page.getByText("Bitte einen Titel angeben.")).toBeVisible();
  // Die Aussage ist „der Server wurde gar nicht erst gefragt", nicht „es kam eine Meldung": ein
  // durchgereichter 400 sähe an der Oberfläche fast gleich aus.
  expect(puts).toBe(0);
});

test("Scheitert das Nachziehen, sagt der Dialog es — und sperrt das Speichern", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const name = await creatorMitUebung(page, "Nachladen");
  await dialogOeffnen(page, name);

  /*
   * Erst JETZT abschießen: Derselbe Abruf füllt den Dialog beim Öffnen, vorher gesetzt bekäme man einen
   * leeren Dialog statt eines veralteten. Nur der Einzelabruf fällt aus — der PUT liegt unter
   * `textbook-series/…` und geht weiter durch. Genau das ist der heikle Zustand: gespeichert ist
   * gespeichert, aber der Dialog weiß ab hier nicht mehr, worauf er aufsetzt.
   */
  const abruf = "**/api/v1/creator/exercises/*";
  await page.route(abruf, (route) => route.abort("failed"));

  await page.getByLabel("Aufgabe", { exact: true }).fill("They ___ (go) to school.");
  await page.getByRole("button", { name: "Übung speichern" }).click();

  // Beides steht da, und beides stimmt: der PUT war erfolgreich, das Nachziehen nicht.
  await expect(page.getByText("Gespeichert.", { exact: true })).toBeVisible();
  await expect(page.getByRole("alert")).toContainText("Weiteres Speichern ist gesperrt");
  /*
   * Der Fokus muss mitkommen. Er liegt nach dem Klick auf dem Speichern-Knopf, der gerade `disabled`
   * wurde, und fällt damit auf `<body>` — dort greift die Fokus-Falle des Dialogs nicht mehr, und Tab
   * liefe in die Seite dahinter. Im Fehlerfall bliebe das dauerhaft.
   */
  await expect(page.getByRole("button", { name: "Erneut laden" })).toBeFocused();
  await expect(page.getByRole("button", { name: "Übung speichern" })).toBeDisabled();

  // Der Riegel hat eine Tür: geht der Abruf wieder, führt „Erneut laden" zurück in den Betrieb.
  await page.unroute(abruf);
  await page.getByRole("button", { name: "Erneut laden" }).click();
  await expect(page.getByRole("alert")).toHaveCount(0);
  await expect(page.getByRole("button", { name: "Übung speichern" })).toBeEnabled();
});
