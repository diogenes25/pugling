import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-170. Der Rollengang zu dieser Story, als Spec statt als einmaliger Handgriff — sie fährt genau die
 * Folge, in der die Lücke bestand, und wiederholt sich (Bereichs-README: „Eine E2E, die den Weg fährt,
 * ist der Rollengang").
 *
 * Die Lücke: Anlegen einer Art war überall frei, Umbenennen und Löschen hing am **Fach**. Für einen neuen
 * Nutzer sind alle vier Fächer mitgeliefert und gehören niemandem — ein Tippfehler stand darum dauerhaft in
 * der Vorfilterung des Planbaus, und niemand konnte ihn wegräumen. Seit B-170 hat die Art einen eigenen
 * Eigentümer.
 *
 * Eigene Datei und nicht an `lehrwerke.spec.ts` angehängt: eine Fläche je Spec, und eine Spec bricht beim
 * ersten Rot ab und nimmt alles Nachfolgende mit (B-109). Vor dieser Story bediente **keine** Spec die
 * Katalog-Arten.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString(36).slice(-5);
const TIPPFEHLER = `Grammtik ${RUN}`;
const BERICHTIGT = `Grammatik ${RUN}`;

test("B-170: eine selbst angelegte Art im Grundbestands-Fach ist umbenennbar und löschbar", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.goto("/vater/katalog");

  // ---- Der Vorspann muss die Regel nennen, und zwar mit BEIDEN Trägern (AK 9) ----
  // Vor B-170 stand hier „nur, wer das Fach angelegt hat" — für einen neuen Nutzer also „niemand", und
  // der Satz war damit eine Absage an eine Fähigkeit, die es jetzt gibt.
  await expect(page.getByText(/umbenennen und löschen nur, wer das/)).toBeVisible();
  await expect(page.getByText(/oder\s+die\s+Art\s+selbst angelegt hat/)).toBeVisible();

  // ---- „Englisch" ist geseedet und gehört niemandem: genau der Fall der Story ----
  // `exact` ist tragend, nicht Stil: `getByLabel` vergleicht als Teilstring und trifft sonst auch
  // „Neues Fach" samt Anlegen-Knopf (Regel aus `frontend/CLAUDE.md`, hier im ersten Lauf gemessen).
  await page.getByLabel("Fach", { exact: true }).selectOption({ label: "Englisch" });
  await expect(page.getByRole("heading", { name: /^Arten/ })).toBeVisible();

  // Eine geseedete Art trägt KEINE Bedienelemente (AK 4 + AK 8, zweite Hälfte) …
  await expect(page.getByText("Vokabeln", { exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: 'Art „Vokabeln" löschen' })).toHaveCount(0);
  // … und auch keinen gesperrten Knopf, dessen Grund niemand liest (B-160).
  await expect(page.locator('input[id^="ca-category-"][disabled]')).toHaveCount(0);

  // ---- Der Tippfehler, um den es geht ----
  await page.getByLabel("Neue Art", { exact: true }).fill(TIPPFEHLER);
  await page.getByRole("button", { name: "Neue Art anlegen" }).click();
  await expect(page.getByText("Art angelegt.", { exact: true })).toBeVisible();

  // Die eigene Art trägt beide Bedienelemente — im SELBEN Fach, in dem „Vokabeln" keine hat. Das ist der
  // Kern von AK 8: entschieden wird pro Zeile, nicht pro Fach.
  const eigeneZeile = page.getByRole("button", { name: `Art „${TIPPFEHLER}" löschen` });
  await expect(eigeneZeile).toBeVisible();
  await expect(page.getByRole("button", { name: 'Art „Vokabeln" löschen' })).toHaveCount(0);

  // ---- Umbenennen (AK 3) ----
  // Die Zeile über ihren EIGENEN Löschknopf greifen und das Feld darin: `getByLabel("Art")` allein trifft
  // jede bearbeitbare Zeile (alle tragen dasselbe sichtbare Label), und ein Index wäre in einer von allen
  // Specs geteilten DB unzuverlässig. Der `aria-label` des Knopfes trägt den Namen und ist eindeutig.
  const zeile = page.locator("div.row")
    .filter({ has: page.getByRole("button", { name: `Art „${TIPPFEHLER}" löschen` }) });
  await zeile.getByLabel("Art", { exact: true }).fill(BERICHTIGT);
  await zeile.getByRole("button", { name: `Art „${TIPPFEHLER}" speichern` }).click();
  await expect(page.getByText("Art umbenannt.", { exact: true })).toBeVisible();
  await expect(page.getByRole("button", { name: `Art „${BERICHTIGT}" löschen` })).toBeVisible();

  // ---- Löschen (AK 2) — die Fähigkeit, die B-157 genommen hatte ----
  page.once("dialog", (d) => d.accept());
  await page.getByRole("button", { name: `Art „${BERICHTIGT}" löschen` }).click();

  // Erst warten, bis die Zeile wirklich weg ist, dann die Gegenprobe: die geseedete steht weiter da.
  await expect(page.getByRole("button", { name: `Art „${BERICHTIGT}" löschen` })).toHaveCount(0);
  await expect(page.getByText("Art gelöscht.", { exact: true })).toBeVisible();
  await expect(page.getByText("Vokabeln", { exact: true })).toBeVisible();
});
