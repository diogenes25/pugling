import { test, expect, type Page } from "@playwright/test";

/*
 * Material **zurückziehen** – aus der Sicht dessen, der es tut.
 *
 * Der Weg existierte nur in der API: `executePublic` wurde beim Speichern durchgereicht, ein Bedienelement
 * gab es nicht. Für einen Creator ist das die einzige Rücknahme, die es gibt – Löschen verweigert eine
 * benutzte Übung, und das zu Recht: laufende Pflichten dürfen nicht unter dem Kind wegbrechen.
 *
 * Geprüft wird der ganze Bogen: Zustand sichtbar → zurückziehen → sichtbar → wieder freigeben.
 *
 * Dazu der zweite Zeitpunkt derselben Entscheidung (B-11): privat **anlegen**, ohne den Umweg über
 * „anlegen → verwalten → zurückziehen". Beide Fälle stehen als eigenständige Tests nebeneinander, damit
 * der Default-Fall auch dann noch geprüft wird, wenn der neue rot ist.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);
const SUBJECT = `E2E-Freigabe ${RUN}`;
const EXERCISE = `Rücknahme ${RUN}`;
const PRIVAT_SUBJECT = `E2E-Privat ${RUN}`;
const PRIVAT_EXERCISE = `Privat angelegt ${RUN}`;

async function vaterLogin(page: Page) {
  await page.goto("/vater");
  await page.locator("#fid").fill(FATHER.id);
  await page.locator("#pin").fill(FATHER.pin);
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();
}

/**
 * Fach im Katalog + Lehrwerk-Reihe mit „Unit 1" darunter – seit B-106 hängt jede Übung zwingend an einer
 * Unit. Der Katalog ist unter allen Vätern geteilt, darum trägt jeder Test einen eigenen Namen: zwei Tests
 * mit demselben Fachnamen bekämen beim zweiten Anlegen eine Dublettenmeldung.
 */
async function katalogVorbereiten(page: Page, name: string) {
  await page.goto("/vater/katalog");
  await page.getByPlaceholder("z. B. Französisch").fill(name);
  await page.getByRole("button", { name: "Neues Fach anlegen" }).click();
  await expect(page.locator("#ca-subject")).toHaveValue(/\d+/);

  await page.goto("/vater/lehrwerke");
  await page.locator("#ns-name").fill(name);
  await page.locator("#ns-subject").selectOption({ label: name });
  await page.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(page.getByText(/steht im Katalog/)).toBeVisible();
  const seriesRow = page.getByRole("row", { name: new RegExp(name) });
  await seriesRow.getByRole("button", { name: "Units" }).click();
  await page.locator('[id^="unit-label-new"]').fill("Unit 1");
  await page.getByRole("button", { name: "Unit hinzufügen" }).click();
  // Über den Knopf der Unit statt über ihren Text: der Helfer läuft je Lauf zweimal gegen eine Seite, auf
  // der dann mehrere Reihen stehen. Ein seitenweites `getByText("Unit 1")` hält heute nur, weil die Seite
  // Units ausschließlich der aufgeklappten Reihe rendert – zeigte sie je eine Vorschau in der Zeile, bräche
  // der Strict Mode in BEIDEN Tests zugleich, also genau der gemeinsame Ausfall, den die Aufteilung
  // vermeiden soll.
  await expect(page.getByRole("button", { name: "Unit 1 bearbeiten" })).toBeVisible();
}

/** Wählt Fach/Reihe/Unit im Anlage-Formular und trägt den Titel ein. */
async function anlageFormularFuellen(page: Page, subject: string, title: string) {
  await page.goto("/vater/exercises/neu");
  await page.locator('select[aria-label="Fach"]').selectOption({ label: subject });
  await page.locator('select[aria-label="Reihe"]').selectOption({ label: subject });
  await page.locator('select[aria-label="Unit"]').selectOption({ label: "Unit 1" });
  await page.locator("#ex-title").fill(title);
}

/** Legt die Vokabel an, wartet auf ihr Token und schickt das Formular ab. */
async function uebungAbsenden(page: Page, wort: string, uebersetzung: string, title: string) {
  await page.locator("#vp-word").fill(wort);
  await page.locator("#vp-translation").fill(uebersetzung);
  await page.getByRole("button", { name: /anlegen & wählen/ }).click();
  // Auf das Token warten: das Anlegen im Store läuft asynchron, und ohne diese Schranke klickt der Test
  // „Übung anlegen", bevor die Vokabel gewählt ist – der Server antwortet dann „mindestens eine Vokabel".
  await expect(page.locator(".token", { hasText: `${wort}→${uebersetzung}` })).toBeVisible();
  await page.getByRole("button", { name: "Übung anlegen" }).click();
  await expect(page.getByText(`Übung „${title}" angelegt.`)).toBeVisible();
}

test("Eigene Übung zurückziehen und wieder freigeben", async ({ page }) => {
  await vaterLogin(page);
  await katalogVorbereiten(page, SUBJECT);

  // Eine eigene Übung anlegen – sie ist standardmäßig für alle zuweisbar.
  await anlageFormularFuellen(page, SUBJECT, EXERCISE);
  await uebungAbsenden(page, `ebb${RUN}`, "Ebbe", EXERCISE);

  /*
   * In der Verwaltung: noch freigegeben, darum kein Kennzeichen.
   *
   * Die Prüfungen greifen bewusst **seitenweit** statt über einen Zeilen-Container: Fach und Unit sind
   * für diesen Lauf frisch angelegt und enthalten genau diese eine Übung. Ein `div`-Container per `.last()`
   * zu erraten trifft je nach Verschachtelung die falsche Ebene – und genau daran ist dieser Test erst
   * gescheitert, obwohl die Oberfläche stimmte.
   */
  await page.getByRole("link", { name: /Übungen verwalten/ }).first().click();
  await expect(page.getByText(EXERCISE, { exact: true })).toBeVisible();
  await expect(page.getByText("zurückgezogen", { exact: true })).toHaveCount(0);

  // Der Schalter liegt in der Verwendungs-Anzeige, nicht in der Zeile: eine seltene
  // Verwaltungs-Entscheidung neben der Auskunft, die sie begründet.
  await page.getByRole("button", { name: "Verwendung" }).click();

  /*
   * Zurückziehen fragt nach (`confirmAction` → `window.confirm`), Freigeben nicht: die Rücknahme wirkt auf
   * fremde Familien, die Freigabe ist harmlos. Playwright weist Dialoge von sich aus **ab** – ohne diesen
   * Handler würde der Klick also stillschweigend nichts tun, und der Test wäre grün-blind.
   */
  page.on("dialog", (d) => d.accept());
  await page.getByRole("button", { name: /Zurückziehen/ }).click();
  await expect(page.getByText("zurückgezogen", { exact: true })).toBeVisible();
  // Der Knopf kehrt seine Bedeutung um – ein Zustand, kein Einbahnweg.
  await expect(page.getByRole("button", { name: /Wieder freigeben/ })).toBeVisible();

  // Und zurück: wieder für alle zuweisbar.
  await page.getByRole("button", { name: /Wieder freigeben/ }).click();
  await expect(page.getByText("zurückgezogen", { exact: true })).toHaveCount(0);
  await expect(page.getByRole("button", { name: /Zurückziehen/ })).toBeVisible();
});

/*
 * Dieselbe Entscheidung, nur früher: Wer Material privat halten will, musste es bisher erst öffentlich
 * anlegen und dann in der Verwaltung zurückziehen – vier Schritte für eine Entscheidung, die beim Anlegen
 * genauso gut fällt. Der Test fährt genau diesen Weg OHNE den „Zurückziehen"-Knopf; würde die Checkbox
 * ihren Wert nicht in die Nutzlast schreiben, entstünde die Übung öffentlich und das Kennzeichen fehlte.
 */
test("Übung von Anfang an privat anlegen", async ({ page }) => {
  await vaterLogin(page);
  await katalogVorbereiten(page, PRIVAT_SUBJECT);

  await anlageFormularFuellen(page, PRIVAT_SUBJECT, PRIVAT_EXERCISE);

  // Vorbelegt mit dem Server-Default: Der Haken steht, das Abwählen IST die Entscheidung.
  const freigabe = page.getByLabel("Für andere Betreuer zuweisbar", { exact: true });
  await expect(freigabe).toBeChecked();
  await freigabe.uncheck();

  await uebungAbsenden(page, `prv${RUN}`, "privat", PRIVAT_EXERCISE);

  /*
   * In der Verwaltung sofort gekennzeichnet – ohne dass jemand „Zurückziehen" geklickt hätte.
   *
   * Die Prüfungen greifen seitenweit, und das ist tragfähig: Der Link „Übungen verwalten" reicht die
   * Auswahl als `?seriesUnitId=` weiter, die Liste zeigt also nur die Übungen dieser frisch angelegten
   * Unit – und darin liegt genau diese eine. Das Kennzeichen entsteht ausschließlich aus dem
   * `executePublic` der Server-Antwort; fehlte die Checkbox in der Nutzlast, entstünde die Übung mit dem
   * Server-Default `true` und der Test wäre rot.
   */
  await page.getByRole("link", { name: /Übungen verwalten/ }).first().click();
  await expect(page.getByText(PRIVAT_EXERCISE, { exact: true })).toBeVisible();
  await expect(page.getByText("zurückgezogen", { exact: true })).toBeVisible();

  // Und der bestehende Schalter kennt den Zustand: Er bietet das Freigeben an, nicht das Zurückziehen.
  await page.getByRole("button", { name: "Verwendung" }).click();
  await expect(page.getByRole("button", { name: /Wieder freigeben/ })).toBeVisible();
});
