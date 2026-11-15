import { expect, test, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-227. Das Fehler-Gedächtnis aus B-216 hielt den letzten Fehler fest, solange geladen wird – aber
 * `useAsync` setzt `error = null, loading = true` nicht nur bei `reload()`, sondern auch bei einem Wechsel
 * der Abhängigkeiten. Nach einer gescheiterten Suche stand darum beim Drehen am Filter der ALTE Fehler mit
 * einem „Lädt…"-Knopf da, für eine Abfrage, die es nicht mehr gab; die Zählzeile sagte „gescheitert", wo eine
 * neue Suche lief.
 *
 * Gemessen wird IM Fenster der neuen Suche – darum antwortet sie erst nach 2 s (echter Server, nur später;
 * Muster `planbau-ladefenster.spec.ts`), und die Fristen der Fenster-Zusicherungen sind KÜRZER als die
 * Verzögerung. Ohne das pollte „Banner weg" bis zur Antwort und wäre auch vor dem Fix grün: nach der Antwort
 * verschwand das Banner schon immer. Die tragende Zeile ist „Suche läuft…" – vor dem Fix stand dort
 * „gescheitert" bis zur Antwort und danach die Trefferzahl, nie „läuft".
 *
 * Zwei Flächen in einer Datei, wie in `wiederholknopf-bleibt.spec.ts`: EIN Fehlerbild an zwei Zwillingen,
 * beide Fälle kurz. Das ist zugleich der Rollengang aus der Supervisor-Rolle.
 */

const FATHER = { id: "1", pin: "0000" };
// Als Prädikat, nicht als Glob: Die erste Suche des Planbaus hat KEINE Query, die des Assistenten immer eine.
const SUCHE = (url: URL) => /\/api\/v1\/creator\/exercises$/.test(url.pathname);
const RUN = Date.now().toString().slice(-6);

/** Ab jetzt antwortet die Suche wieder – aber erst nach `ms`, lange genug, um im Fenster zu messen. */
async function sucheVerzoegern(page: Page, ms: number) {
  await page.unroute(SUCHE);
  await page.route(SUCHE, async (route) => {
    await new Promise((r) => setTimeout(r, ms));
    await route.fallback();
  });
}

/** Der Kern beider Fälle: Im Fenster der NEUEN Suche steht weder Banner noch „gescheitert", sondern „läuft". */
async function imFensterDerNeuenSucheMessen(page: Page, laeuft: RegExp) {
  await expect(page.getByText(laeuft)).toBeVisible({ timeout: 1200 });
  await expect(page.getByText(/Die Übungssuche ist gescheitert/)).toHaveCount(0, { timeout: 1200 });
  await expect(page.getByRole("button", { name: "Lädt…" })).toHaveCount(0, { timeout: 1200 });
}

test("Assistent: Ein Filterwechsel nach gescheiterter Suche zeigt die neue Suche, nicht den alten Fehler", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.route(SUCHE, (route) => route.abort("failed"));
  await page.goto("/vater/wizard");

  // Zu Schritt 3 wie in `wiederholknopf-bleibt.spec.ts`: Kind „1" ist der geseedete Sohn.
  await page.getByRole("radio", { name: "Bestehendes Kind" }).click();
  await page.getByRole("combobox", { name: "Kind" }).selectOption("1");
  await page.getByRole("button", { name: "Weiter" }).click();
  await expect(page.getByRole("heading", { name: /Wo hakt es/ })).toBeVisible();
  await page.getByLabel("Fach (Pflicht)", { exact: true }).selectOption({ label: "Englisch" });
  // B-423: das vorbelegte Ziel „Klassenarbeit" verlangt einen Termin – diese Spec meint die freie Dauer.
  await page.getByRole("radio", { name: /Regelmäßig üben/ }).click();
  await page.getByRole("button", { name: "Weiter" }).click();
  await expect(page.getByText(/Die Übungssuche ist gescheitert/)).toBeVisible();

  await sucheVerzoegern(page, 2000);
  // Der Typ-Filter ändert den Schlüssel der Suche – eine ANDERE Abfrage, keine Wiederholung. Per Label, nicht
  // per Index (Hausregel): „Vokabelkarten" ist der Anzeigename des Typs `Vocabulary` aus dem Manifest.
  await page.locator("#wiz-type").selectOption({ label: "Vokabelkarten" });

  await imFensterDerNeuenSucheMessen(page, /^Suche läuft…$/);

  // Danach gilt, was der Server sagt – positiv zugesichert, nicht nur „Banner weg".
  await expect(page.getByText(/passende Übungen/)).toBeVisible({ timeout: 10_000 });
});

test("Planbau: Ein Filterwechsel nach gescheiterter Suche zeigt die neue Suche, nicht den alten Fehler", async ({ page }) => {
  await vaterLogin(page, FATHER);

  // Erst den Plan anlegen (braucht keine Übungssuche), dann die Suche abschießen: Sie fliegt, sobald der
  // Planbau mit seiner Filterleiste erscheint.
  await page.goto("/vater/plan/new");
  await page.locator("#plan-title").fill(`Filterwechsel ${RUN}`);
  await page.getByRole("combobox", { name: "Kind" }).selectOption("1");
  await page.route(SUCHE, (route) => route.abort("failed"));
  await page.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(page).toHaveURL(/\/vater\/plan\/\d+$/);
  await expect(page.getByText(/Die Übungssuche ist gescheitert/)).toBeVisible();

  await sucheVerzoegern(page, 2000);
  await page.locator('select[aria-label="Fach-Filter"]').selectOption({ label: "Englisch" });

  await imFensterDerNeuenSucheMessen(page, /^\(Suche läuft…\)$/);

  await expect(page.getByText(/\(\d+ Treffer\)/)).toBeVisible({ timeout: 10_000 });
});
