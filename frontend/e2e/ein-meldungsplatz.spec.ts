import { expect, test, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-187, B-200 und B-212 — drei Defekte, eine Wurzel: **Eine Rückmeldung gehört an einen Platz, der sie
 * überlebt, und es darf nur einer sein.**
 *
 * - B-187: Die Bestätigung des Unit-Formulars lebte in dem Element, dessen Verschwinden sie bestätigt
 *   (`onDone()` hängt das Bearbeiten-Formular aus). Dritter Fall derselben Wurzel wie B-151.
 * - B-200: Das Löschen einer Unit war **vollkommen stumm** — der Creator las am Verschwinden der Zeile ab,
 *   ob der Server zugestimmt hatte.
 * - B-212: Die Datei trug nach B-204 weiter zwei Meldungsplätze an den Formularen. Ein Fehler unter Reihe A
 *   war von keiner anderen Handlung mehr erreichbar und stand neben dem Grün, das Reihe B in den Platz der
 *   Seite legte.
 *
 * Eigene Datei, nicht angehängt an `bestaetigung-ueberlebt.spec.ts` oder `creator-lehrwerk-weg.spec.ts`
 * (`frontend/CLAUDE.md`: eine Fläche je Spec; eine Spec bricht beim ersten Rot ab). Die beiden fahren die
 * **Zeile** und ihre Meldung — hier geht es um die **Formulare** darunter.
 *
 * Das ist zugleich der Rollengang aus der Creator-Rolle.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString(36).slice(-5);

/** Legt eine Reihe an und liefert ihren Namen zurück. */
async function reiheAnlegen(page: Page, name: string) {
  await page.locator("#ns-name").fill(name);
  await page.getByRole("button", { name: "Reihe anlegen" }).click();
  // Den NAMEN mitprüfen: Beim zweiten Aufruf steht das Banner der ersten Reihe schon da, und eine
  // Zusicherung auf „steht im Katalog" allein wäre vor der Handlung erfüllt — sie könnte nie fallen.
  await expect(page.getByText(new RegExp(`${name}.{0,2} steht im Katalog`))).toBeVisible();
  return name;
}

/** Öffnet den Unit-Bereich einer Reihe. */
async function unitsOeffnen(page: Page, reihe: string) {
  await page.getByRole("row", { name: new RegExp(reihe) }).getByRole("button", { name: "Units" }).click();
}

test("Die Bestätigung des Unit-Formulars überlebt sein Zugehen — und das Löschen nennt die Unit", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.goto("/vater/lehrwerke");
  const reihe = await reiheAnlegen(page, `Meldungsplatz ${RUN}`);
  await unitsOeffnen(page, reihe);

  // ---- Eine Unit anlegen. Dieses Formular bleibt montiert; es ist die Gegenprobe zum Bearbeiten. ----
  const UNIT = "Unit 1";
  await page.locator('[id^="unit-label-new"]').fill(UNIT);
  await page.getByRole("button", { name: "Unit hinzufügen" }).click();
  await expect(page.getByText("Unit hinzugefügt.", { exact: true })).toBeVisible();

  // ---- B-187: bearbeiten, speichern — das Formular geht zu, die Bestätigung bleibt ----
  await page.getByRole("button", { name: `${UNIT} bearbeiten` }).click();
  // Über die Id, nicht über die Beschriftung: „Grammatik der Unit" steht zweimal auf dem Bildschirm —
  // im Bearbeiten-Formular UND im dauerhaft montierten Anlege-Formular darunter. Die Id unterscheidet
  // sie (`u<id>` beim Bearbeiten, `new<seriesId>` beim Anlegen).
  const grammatik = page.locator('[id^="unit-grammar-u"]');
  await expect(grammatik).toBeVisible();
  await grammatik.fill("Simple Past");
  await page.getByRole("button", { name: "Speichern" }).click();

  /*
   * Erst warten, bis das Formular wirklich weg ist: Danach ist die Zusicherung eine Aussage über den
   * Platz der SEITE und nicht mehr über den Banner im Formular, der bis hierher die Bestätigung trug und
   * mit ihr unterging. Ohne dieses Warten könnte der Fall den Frame davor treffen und wäre grün, ohne
   * etwas zu belegen.
   */
  await expect(grammatik).toHaveCount(0);
  await expect(page.getByText("Gespeichert.", { exact: true })).toBeVisible();

  // ---- B-200: Löschen war stumm. Jetzt nennt die Meldung die Unit beim Namen ----
  page.once("dialog", (d) => d.accept());
  await page.getByRole("button", { name: `${UNIT} bearbeiten` }).click();
  // `Unit löschen`, nicht `Löschen`: Der Teilstring trifft auch den Löschen-Knopf der REIHE daneben.
  await page.getByRole("button", { name: "Unit löschen", exact: true }).click();
  await expect(page.getByText(`Unit „${UNIT}" gelöscht.`, { exact: true })).toBeVisible();
  // Der Name trägt die Aussage: Die Zeile, auf die sich die Meldung bezieht, ist gerade verschwunden.
  await expect(page.getByRole("row", { name: new RegExp(UNIT) })).toHaveCount(0);
});

test("Ein Fehler am Formular überlebt keine spätere Handlung an einer anderen Reihe", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.goto("/vater/lehrwerke");

  /*
   * Zwei Reihen: an A scheitert gleich das Speichern, B wird danach gelöscht. Beide zuerst anlegen, und
   * seit B-212 ist das auch nötig: `NewSeries` meldet inzwischen an denselben Platz, das Anlegen würde
   * die rote Meldung also tatsächlich räumen. (In der ersten Fassung dieser Spec stand hier derselbe
   * Satz mit einer falschen Begründung — damals hatte `NewSeries` noch seinen eigenen Platz, und genau
   * dieser blinde Fleck war der Fund, den der Review daran aufgehängt hat.)
   */
  const A = await reiheAnlegen(page, `Fehlerplatz A ${RUN}`);
  const B = await reiheAnlegen(page, `Fehlerplatz B ${RUN}`);

  // ---- Ein Server-Fehler am Formular von A ----
  await page.getByRole("button", { name: `Reihe bearbeiten: „${A}"` }).click();
  await page.getByLabel("Name der Reihe", { exact: true }).fill(`${A} neu`);
  /*
   * Der Ausfall wird erzwungen statt provoziert: Welche Eingabe der Server ablehnt, ist eine Zusage, die
   * diese Spec nicht braucht — geprüft wird, WO die Meldung landet. Vorbild `optionsliste-faellt-aus.spec.ts`.
   */
  await page.route("**/api/v1/creator/textbook-series/*", (route) =>
    (route.request().method() === "PATCH" ? route.abort("failed") : route.continue()));
  await page.getByRole("button", { name: `Speichern: „${A}"` }).click();

  const fehler = page.getByText(/Failed to fetch|fehlgeschlagen|Netzwerk/);
  await expect(fehler).toBeVisible();
  await page.unroute("**/api/v1/creator/textbook-series/*");

  // ---- Und jetzt eine gelungene Handlung an der ANDEREN Reihe ----
  page.once("dialog", (d) => d.accept());
  await page.getByRole("button", { name: `Löschen: „${B}"` }).click();
  await expect(page.getByRole("row", { name: new RegExp(B) })).toHaveCount(0);
  await expect(page.getByText(`Reihe „${B}" gelöscht.`, { exact: true })).toBeVisible();

  /*
   * Der Kern: Die rote Meldung ist weg. Vor der Reparatur war sie unerreichbar — sie lebte im Banner des
   * Formulars von A, und kein Aufrufer kann einen fremden lokalen Platz räumen. Rot und grün standen
   * gleichzeitig im Blickfeld, und keines sagte, zu welcher Handlung es gehört.
   */
  await expect(fehler).toHaveCount(0);
});
