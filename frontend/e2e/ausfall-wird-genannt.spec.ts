import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-174 und B-162: Ein gescheitertes Laden wurde als „da ist nichts" ausgegeben – einmal stumm (die
 * Arten-Liste eines Fachs), einmal mit einer ausdrücklichen, FALSCHEN Handlungsanweisung („Lege welche
 * unter ‚Übungen' an", während der Katalog voll war).
 *
 * Beide Fälle brauchen einen scheiternden Abruf, und der kostet im echten Browser genau eine Zeile
 * (`page.route(…).abort()`, Muster `optionsliste-faellt-aus.spec.ts`). Damit ist diese Spec der
 * **Rollengang** beider Stories und nicht nur ein Ersatzbeleg – und die Schätzung beider Stories, die
 * „kein E2E, weil das einen abgeschalteten Server bräuchte" behauptete, ist damit widerlegt.
 *
 * Zwei Flächen in einer Datei ist hier bewusst: Es ist EIN Fehlerbild, und beide Fälle bestehen aus je
 * vier Zeilen. Die Regel „eine Fläche je Spec" schützt vor dem Abbruch-Schatten langer Durchstiche
 * (B-109); zwei Vier-Zeiler bilden keinen.
 */

const FATHER = { id: "1", pin: "0000" };

test("B-174: Scheitert die Arten-Liste, sagt die Katalogseite das – statt zu schweigen", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.goto("/vater/katalog");

  // Erst mit funktionierender Liste ein Fach wählen: nur dann ist überhaupt etwas zu laden.
  await page.locator("#ca-subject").selectOption({ label: "Englisch" });
  await expect(page.getByRole("heading", { name: /^Arten/ })).toBeVisible();

  // Nur den Arten-Abruf abschießen. Die Fächerliste lädt weiter – das ist der heimtückische Teil:
  // vorher war an der Oberfläche NICHTS zu sehen, was auf einen Ausfall hindeutete.
  await page.route("**/api/v1/creator/subjects/*/categories", (route) => route.abort("failed"));
  await page.reload();
  await page.locator("#ca-subject").selectOption({ label: "Englisch" });

  await expect(page.getByText(/^Arten:/)).toBeVisible();
});

test("B-162: Scheitert die Übungssuche, nennt der Assistent nicht den Katalog – und bietet einen Ausweg", async ({ page }) => {
  await vaterLogin(page, FATHER);

  await page.route("**/api/v1/creator/exercises?**", (route) => route.abort("failed"));
  await page.goto("/vater/wizard");

  // Zu Schritt 3 durchklicken: erst ein Kind, dann ein Fach – ohne Fach fliegt gar keine Suche.
  // Der Weg ist derselbe wie in `assistent.spec.ts`; Kind „1" ist der geseedete Sohn.
  await page.getByRole("button", { name: "Bestehendes Kind" }).click();
  await page.getByRole("combobox", { name: "Kind" }).selectOption("1");
  await page.getByRole("button", { name: "Weiter" }).click();
  await expect(page.getByRole("heading", { name: /Wo hakt es/ })).toBeVisible();
  await page.getByLabel("Fach", { exact: true }).selectOption({ label: "Englisch" });
  await page.getByRole("button", { name: "Weiter" }).click();
  await expect(page.getByRole("heading", { name: /Übungen wählen/ })).toBeVisible();

  // Der Kern: die Ursache steht da, und die falsche Anweisung NICHT.
  await expect(page.getByText(/Die Übungssuche ist gescheitert/)).toBeVisible();
  await expect(page.getByText(/Keine passenden Übungen im Katalog/)).toHaveCount(0);

  // Und der Ausweg ist bedienbar. Ohne ihn führte aus diesem Bildschirm nur F5 heraus – über den
  // Stepper zurück und wieder vor ändert keine Abhängigkeit von `useAsync`, es fliegt keine neue Abfrage.
  const knopf = page.getByRole("button", { name: "Erneut suchen" });
  await expect(knopf).toBeVisible();

  // Diesmal darf sie gelingen: der Knopf muss wirklich neu SUCHEN, nicht nur die Meldung wegräumen.
  await page.unroute("**/api/v1/creator/exercises?**");
  await knopf.click();
  // Positiv zusichern, und das ist hier tragend: `useAsync` setzt `error` schon beim Start der Abfrage
  // auf `null` (useAsync.ts). Ein bloßes „die Meldung ist weg" wäre also erfüllt, sobald irgendetwas
  // startet – die Zählzeile dagegen schaltet erst um, wenn die Antwort da ist und der Seitenschlüssel
  // nachzieht.
  await expect(page.getByText(/passende Übungen/)).toBeVisible();
  await expect(page.getByText(/Die Übungssuche ist gescheitert/)).toHaveCount(0);
});
