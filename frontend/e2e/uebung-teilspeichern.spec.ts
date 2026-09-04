import { test, expect, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-210. Der Bearbeiten-Dialog einer Übung hat **zwei** Speicher-Knöpfe, und beide schicken denselben
 * Vollersatz-PUT. Solange der Metadaten-Knopf seine `config` aus dem geladenen Stand nahm, trug er den
 * Inhalt **vom Öffnen** zurück: Inhalt speichern → Titel ändern → „Beschreibung speichern", und die
 * gerade gespeicherten Zeilen waren weg. Ohne Fehler, ohne Meldung, und der Zeilen-Editor zeigte weiter
 * die neue Fassung — es gab nicht einmal einen sichtbaren Widerspruch.
 *
 * Warum E2E und kein Komponententest: `frontend/CLAUDE.md` zieht die Grenze bei „kein nachgebauter
 * Bildschirm mit gefälschtem `fetch`". Der Defekt lebt genau dort, wo zwei Formulare, ein geladener Stand
 * und zwei echte PUTs zusammenkommen — die Reihenfolge IST der Fall.
 *
 * Der Typ ist **Grammatik**: nur ein inhaltsbearbeitbarer Typ hat den zweiten Knopf. Mit einer
 * Vokabel-Übung wäre die Spec strukturell blind (ihre Wortpaare gehen über `/items`, nicht über die
 * Config).
 *
 * Abgrenzung zu `einkleidung.spec.ts`: die fährt dieselben zwei Knöpfe in der **anderen** Reihenfolge
 * (erst Metadaten, dann Inhalt) und deckt damit die Gegenrichtung ab. Diese hier ist die Richtung, die
 * dabei offen blieb.
 *
 * Das ist zugleich der Rollengang aus der Creator-Rolle.
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

test("Der gespeicherte Inhalt überlebt ein anschließendes „Beschreibung speichern“", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const name = await creatorMitUebung(page, "Teilspeichern");
  await dialogOeffnen(page, name);

  // ---- Schritt 1: den Inhalt ändern und speichern ----
  await page.getByLabel("Aufgabe", { exact: true }).fill("They ___ (go) to school.");
  await page.getByRole("button", { name: "Inhalt speichern" }).click();
  await expect(page.getByText("Inhalt gespeichert.")).toBeVisible();

  // ---- Schritt 2: im SELBEN Dialog den Titel ändern und speichern ----
  await page.locator("#ed-title").fill(`${name} v2`);
  await page.getByRole("button", { name: "Beschreibung speichern" }).click();
  // `exact`: der Kasten des Inhalts-Knopfs steht noch daneben, und „Inhalt gespeichert.“ enthält
  // diesen Text als Teilstring — ohne das treffen zwei Elemente.
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

test("„Inhalt speichern“ prüft den Titel selbst, statt sich einen 400 zu holen", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const name = await creatorMitUebung(page, "Titelpruefung");
  await dialogOeffnen(page, name);

  // Seit das Metadaten-Formular über beiden Editoren liegt (B-206), kann der Inhalts-Knopf einen
  // leergeräumten Titel mitschicken — der Metadaten-Knopf prüft ihn, dieser prüfte ihn nicht.
  let puts = 0;
  page.on("request", (r) => { if (r.method() === "PUT") puts += 1; });

  await page.locator("#ed-title").fill("");
  await page.getByRole("button", { name: "Inhalt speichern" }).click();

  await expect(page.getByText("Bitte einen Titel angeben.")).toBeVisible();
  // Die Aussage ist „der Server wurde gar nicht erst gefragt", nicht „es kam eine Meldung": ein
  // durchgereichter 400 sähe an der Oberfläche fast gleich aus.
  expect(puts).toBe(0);
});

test("Scheitert das Nachziehen, sagt der Dialog es — und sperrt beide Speicher-Knöpfe", async ({ page }) => {
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
  await page.getByRole("button", { name: "Inhalt speichern" }).click();

  // Beides steht da, und beides stimmt: der PUT war erfolgreich, das Nachziehen nicht.
  await expect(page.getByText("Inhalt gespeichert.")).toBeVisible();
  await expect(page.getByRole("alert")).toContainText("Weiteres Speichern ist gesperrt");
  /*
   * Der Fokus muss mitkommen. Er liegt nach dem Klick auf dem Speichern-Knopf, der gerade `disabled`
   * wurde, und fällt damit auf `<body>` — dort greift die Fokus-Falle des Dialogs nicht mehr, und Tab
   * liefe in die Seite dahinter. Im Fehlerfall bliebe das dauerhaft.
   */
  await expect(page.getByRole("button", { name: "Erneut laden" })).toBeFocused();

  await expect(page.getByRole("button", { name: "Beschreibung speichern" })).toBeDisabled();
  await expect(page.getByRole("button", { name: "Inhalt speichern" })).toBeDisabled();

  // Der Riegel hat eine Tür: geht der Abruf wieder, führt „Erneut laden" zurück in den Betrieb.
  await page.unroute(abruf);
  await page.getByRole("button", { name: "Erneut laden" }).click();
  await expect(page.getByRole("alert")).toHaveCount(0);
  await expect(page.getByRole("button", { name: "Beschreibung speichern" })).toBeEnabled();
  await expect(page.getByRole("button", { name: "Inhalt speichern" })).toBeEnabled();
});

test("Während der eine Knopf speichert, ist der andere gesperrt", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const name = await creatorMitUebung(page, "Zweitklick");
  await dialogOeffnen(page, name);

  /*
   * Der PUT wird künstlich verzögert, damit der Zustand „speichert gerade" lange genug steht, um ihn zu
   * befragen. Ohne das ist er ein paar Millisekunden lang und die Zusicherung würde ihn verpassen.
   */
  await page.route("**/api/v1/creator/textbook-series/**", async (route) => {
    if (route.request().method() === "PUT") await new Promise((r) => setTimeout(r, 3000));
    await route.continue();
  });

  await page.getByLabel("Aufgabe", { exact: true }).fill("They ___ (go) to school.");
  await page.getByRole("button", { name: "Inhalt speichern" }).click();

  /*
   * Der Kern: Die beiden Editoren halten je eine eigene `useAction`-Instanz, und deren Sperre gilt nur
   * für den eigenen Knopf. Ohne den gemeinsamen Riegel im Dialog wäre dieser hier klickbar — und sein
   * Vollersatz-PUT ginge mit der `config` von VOR dem laufenden Speichern raus. Ohne ETag am Server
   * gewinnt der letzte Schreiber, der Inhalt wäre also wieder weg.
   *
   * Enge Frist mit Absicht: Nach dem Speichern ist der Knopf ohnehin kurz gesperrt (Nachladen). Eine
   * Zusicherung ohne Frist würde einfach so lange warten und träfe damit den falschen Zustand.
   */
  await expect(page.getByRole("button", { name: "Beschreibung speichern" })).toBeDisabled({ timeout: 1000 });

  await expect(page.getByText("Inhalt gespeichert.")).toBeVisible();
  // Und die Gegenprobe: Ist alles durch, geht er wieder auf. Ein dauerhaft toter Knopf wäre kein Fix.
  await expect(page.getByRole("button", { name: "Beschreibung speichern" })).toBeEnabled();
});
