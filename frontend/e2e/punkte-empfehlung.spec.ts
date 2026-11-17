import { test, expect } from "@playwright/test";
import { createChild, vaterLogin } from "./helpers";

/*
 * B-45: Die Punkte-Empfehlung des Creators erreicht den Supervisor.
 *
 * `Exercise.rewardPoints` war ein Pflichtfeld, das kein Bewertungspfad las — ein Creator, der dort 15
 * eintrug, bewirkte nichts und bekam darüber keine Rückmeldung. Diese Spec fährt genau den Weg, auf dem
 * die Empfehlung verpuffte: Übung mit 15 Punkten anlegen → Plan anlegen → Übung als Position wählen.
 *
 * Eigene Datei, nicht angehängt an `creator-lehrwerk-weg.spec.ts` (Regel aus `frontend/CLAUDE.md`: eine
 * Fläche je Spec). Sie ist zugleich der Rollengang der Story: die beiden Vitest-Fälle prüfen die Regel
 * (`pointsFrom`), der Integrationstest den Server — aber ob der Wert im Formular des Vaters ANKOMMT,
 * beantwortet nur die Oberfläche.
 *
 * Rot macht sie zwei Dinge: eine Trefferliste, die die Empfehlung beim Wählen nicht übernimmt (gemessen),
 * und ein fehlendes `rewardPoints` in `ExerciseSummary` — ohne das Feld im Vertrag kann das Formular es
 * nicht lesen. Der Formularaufbau ist bewusst NICHT gemeint: dort ist noch keine Übung gewählt.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);
const SERIES = { name: `Empfehlung ${RUN}`, publisher: "Klett" };
const UNIT_LABEL = `Unit 1 – Empfehlung ${RUN}`;
const EXERCISE_TITLE = `Empfohlene Übung ${RUN}`;
const EMPFEHLUNG = 15;
const CHILD = { name: `Empfehlungs-Kind ${RUN}`, pin: "4321" };

test("Die Punkte der Übung belegen das Ziel-Feld der neuen Position vor", async ({ page }) => {
  await vaterLogin(page, FATHER);

  const childId = await createChild(page, CHILD.name, CHILD.pin);

  // ---- Reihe und Unit: seit B-106 hängt jede Übung zwingend an einer Unit ----
  await page.goto("/vater/lehrwerke");
  await page.locator("#ns-name").fill(SERIES.name);
  await page.locator("#ns-publisher").selectOption({ label: SERIES.publisher });
  await page.locator("#ns-subject").selectOption({ label: "Englisch" });
  await page.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(page.getByText(/steht im Katalog/)).toBeVisible();
  const reihe = page.getByRole("row", { name: new RegExp(SERIES.name) });
  await reihe.getByRole("button", { name: "Units" }).click();
  await page.locator('[id^="unit-label-new"]').fill(UNIT_LABEL);
  await page.getByRole("button", { name: "Unit hinzufügen" }).click();
  await expect(page.getByText(UNIT_LABEL)).toBeVisible();

  // ---- Der Creator trägt seine Empfehlung ein ----
  await page.getByRole("link", { name: `+ Übung zu „${UNIT_LABEL}"` }).click();
  await page.locator('select[aria-label="Übungstyp"]').selectOption("Grammar");
  await page.locator("#ex-title").fill(EXERCISE_TITLE);
  await page.locator("#ex-points").fill(String(EMPFEHLUNG));
  await page.getByLabel("Anweisung").fill("Setze die richtige Form ein.");
  await page.getByLabel("Aufgabe").fill("She ___ (go) to school.");
  await page.getByLabel("Lösung").fill("goes");
  await page.getByRole("button", { name: "Übung anlegen" }).click();
  await expect(page.getByText(`Übung „${EXERCISE_TITLE}" angelegt.`)).toBeVisible();

  // ---- Der Supervisor nimmt sie in einen Plan ----
  await page.goto("/vater/plan/new");
  await page.locator("#plan-title").fill(`Plan ${RUN}`);
  await page.getByRole("combobox", { name: "Kind" }).selectOption(childId);
  await page.getByRole("combobox", { name: "Fach (Pflicht)" }).selectOption({ label: "Englisch" });
  await page.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(page).toHaveURL(/\/vater\/plan\/\d+$/);

  const punkteFeld = page.getByLabel("Punkte bei erreichtem Ziel", { exact: true });
  // Vor der Wahl steht der Modell-Vorgabewert: die Gegenprobe dazu, dass die 15 unten aus der ÜBUNG
  // kommt und nicht zufällig irgendwo steht.
  await expect(punkteFeld).toHaveValue("20");

  await page.locator('select[aria-label="Fach-Filter"]').selectOption({ label: "Englisch" });
  await page.locator('select[aria-label="Reihe-Filter"]').selectOption({ label: SERIES.name });
  await page.locator('select[aria-label="Unit-Filter"]').selectOption({ label: UNIT_LABEL });
  await page.getByRole("radio", { name: new RegExp(EXERCISE_TITLE) }).check();

  // Der Kern: die Empfehlung des Creators steht im Feld — überschreibbar, nicht gesperrt.
  await expect(punkteFeld).toHaveValue(String(EMPFEHLUNG));
  await expect(punkteFeld).toBeEnabled();

  await page.getByRole("button", { name: /Position hinzufügen/ }).click();
  const position = page.getByRole("row", { name: new RegExp(EXERCISE_TITLE) });
  await expect(position).toContainText(`Ziel ${EMPFEHLUNG}`);
});

/*
 * B-191 und B-192, und beide brauchen denselben Weg: ZWEI Uebungen nacheinander.
 *
 * B-191: `pointsFrom` gab bei einer Uebung ohne brauchbare Empfehlung den Wert zurueck, der gerade im
 * Feld stand - und der stammte von der vorher angeklickten Uebung. Ein Fall mit einer Uebung kann das
 * nicht messen; genau darum kam es durch die Abnahme von B-45.
 *
 * B-192: Die Grenze (0, 200] griff nur, wenn `pointsGoalMet` fehlt - und das Formular schickte immer
 * eine Zahl. Der Vater sah 100000 und bekam 20. Seit dem Katalogfeld `suggestedGoalPoints` ist beides
 * dasselbe.
 *
 * Eigener Test in derselben Datei statt einer neuen Spec: Es ist dieselbe Flaeche und derselbe
 * Bildschirm wie oben, und die Regel aus `frontend/CLAUDE.md` zielt auf den Abbruch-Schatten langer
 * Durchstiche - ein zweiter Fall neben einem gleich langen bildet keinen.
 */
test("Eine unsinnige Empfehlung wird nicht vorbelegt - und leckt nicht in die naechste Uebung", async ({ page }) => {
  const LAUF = Date.now().toString().slice(-6);
  const reiheName = `Grenzfall ${LAUF}`;
  const unitLabel = `Unit 1 - Grenzfall ${LAUF}`;
  const brauchbar = `Brauchbare Uebung ${LAUF}`;
  const unsinnig = `Unsinnige Uebung ${LAUF}`;
  const kind = { name: `Grenz-Kind ${LAUF}`, pin: "4321" };

  await vaterLogin(page, FATHER);
  const childId = await createChild(page, kind.name, kind.pin);

  await page.goto("/vater/lehrwerke");
  await page.locator("#ns-name").fill(reiheName);
  await page.locator("#ns-subject").selectOption({ label: "Englisch" });
  await page.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(page.getByText(/steht im Katalog/)).toBeVisible();
  const reihe = page.getByRole("row", { name: new RegExp(reiheName) });
  await reihe.getByRole("button", { name: "Units" }).click();
  await page.locator('[id^="unit-label-new"]').fill(unitLabel);
  await page.getByRole("button", { name: "Unit hinzufügen" }).click();
  await expect(page.getByText(unitLabel)).toBeVisible();

  // ---- Zwei Uebungen: eine mit plausibler Empfehlung, eine jenseits jeder Grenze ----
  async function uebungAnlegen(titel: string, punkte: number) {
    await page.goto("/vater/lehrwerke");
    await page.getByRole("row", { name: new RegExp(reiheName) })
      .getByRole("button", { name: "Units" }).click();
    await page.getByRole("link", { name: `+ Übung zu „${unitLabel}"` }).click();
    await page.locator('select[aria-label="Übungstyp"]').selectOption("Grammar");
    await page.locator("#ex-title").fill(titel);
    await page.locator("#ex-points").fill(String(punkte));
    await page.getByLabel("Anweisung").fill("Setze die richtige Form ein.");
    await page.getByLabel("Aufgabe").fill("She ___ (go) to school.");
    await page.getByLabel("Lösung").fill("goes");
    await page.getByRole("button", { name: "Übung anlegen" }).click();
    await expect(page.getByText(`Übung „${titel}" angelegt.`)).toBeVisible();
  }
  await uebungAnlegen(brauchbar, 40);
  await uebungAnlegen(unsinnig, 100000);

  await page.goto("/vater/plan/new");
  await page.locator("#plan-title").fill(`Grenzplan ${LAUF}`);
  await page.getByRole("combobox", { name: "Kind" }).selectOption(childId);
  await page.getByRole("combobox", { name: "Fach (Pflicht)" }).selectOption({ label: "Englisch" });
  await page.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(page).toHaveURL(/\/vater\/plan\/\d+$/);

  const punkteFeld = page.getByLabel("Punkte bei erreichtem Ziel", { exact: true });
  await page.locator('select[aria-label="Fach-Filter"]').selectOption({ label: "Englisch" });
  await page.locator('select[aria-label="Reihe-Filter"]').selectOption({ label: reiheName });
  await page.locator('select[aria-label="Unit-Filter"]').selectOption({ label: unitLabel });

  // Erst die brauchbare: sie belegt mit 40 vor, wie B-45 es verspricht.
  await page.getByRole("radio", { name: new RegExp(brauchbar) }).check();
  await expect(punkteFeld).toHaveValue("40");

  // Dann die unsinnige: nicht 100000, denn das waere eine Zahl, die beim Anlegen zu 20 wuerde (B-192).
  // Dass hier nicht 40 steht, ist KEIN Beleg fuer B-191 - seit der Server immer eine plausible Zahl
  // nennt, ist der Rueckfall auf den bisherigen Wert auf diesem Pfad gar nicht erreichbar. B-191 belegt
  // der Fall darunter, und diese Unterscheidung gehoert hingeschrieben: die gemessene rote Probe
  // ("Expected 20, Received 100000") war rot fuer B-192, nicht fuer B-191.
  await page.getByRole("radio", { name: new RegExp(unsinnig) }).check();
  await expect(punkteFeld).toHaveValue("20");

  // Und was dasteht, gilt auch: die angelegte Position traegt genau diese Zahl.
  await page.getByRole("button", { name: /Position hinzufügen/ }).click();
  const position = page.getByRole("row", { name: new RegExp(unsinnig) });
  await expect(position).toContainText("Ziel 20");
});

/*
 * B-191, und ohne diesen Fall waere der Fix UNBEOBACHTET (Reviewer-Fund): Kein Test tippte je in das
 * Punkte-Feld, `punkteGetippt` war in jedem Lauf `false`, und `pointsFrom(ex, cur, false)` ist Zeichen
 * fuer Zeichen dasselbe wie eine Fassung ganz ohne den Parameter. Man konnte das Flag durch das Literal
 * `false` ersetzen und alles blieb gruen - der Kern der Story lag ausserhalb jeder Zusicherung.
 *
 * Die Reihenfolge ist zwingend, weil das Flag einwegig ist: erst tippen, dann wechseln.
 */
test("Was der Vater selbst tippt, ueberlebt den Uebungswechsel - bis die Position steht", async ({ page }) => {
  const LAUF = Date.now().toString().slice(-6);
  const reiheName = `Getippt ${LAUF}`;
  const unitLabel = `Unit 1 - Getippt ${LAUF}`;
  const ersteUebung = `Erste Uebung ${LAUF}`;
  const zweiteUebung = `Zweite Uebung ${LAUF}`;
  const kind = { name: `Tipp-Kind ${LAUF}`, pin: "4321" };

  await vaterLogin(page, FATHER);
  const childId = await createChild(page, kind.name, kind.pin);

  await page.goto("/vater/lehrwerke");
  await page.locator("#ns-name").fill(reiheName);
  await page.locator("#ns-subject").selectOption({ label: "Englisch" });
  await page.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(page.getByText(/steht im Katalog/)).toBeVisible();
  await page.getByRole("row", { name: new RegExp(reiheName) })
    .getByRole("button", { name: "Units" }).click();
  await page.locator('[id^="unit-label-new"]').fill(unitLabel);
  await page.getByRole("button", { name: "Unit hinzufügen" }).click();
  await expect(page.getByText(unitLabel)).toBeVisible();

  async function uebungAnlegen(titel: string, punkte: number) {
    await page.goto("/vater/lehrwerke");
    await page.getByRole("row", { name: new RegExp(reiheName) })
      .getByRole("button", { name: "Units" }).click();
    await page.getByRole("link", { name: `+ Übung zu „${unitLabel}"` }).click();
    await page.locator('select[aria-label="Übungstyp"]').selectOption("Grammar");
    await page.locator("#ex-title").fill(titel);
    await page.locator("#ex-points").fill(String(punkte));
    await page.getByLabel("Anweisung").fill("Setze die richtige Form ein.");
    await page.getByLabel("Aufgabe").fill("She ___ (go) to school.");
    await page.getByLabel("Lösung").fill("goes");
    await page.getByRole("button", { name: "Übung anlegen" }).click();
    await expect(page.getByText(`Übung „${titel}" angelegt.`)).toBeVisible();
  }
  await uebungAnlegen(ersteUebung, 40);
  await uebungAnlegen(zweiteUebung, 60);

  await page.goto("/vater/plan/new");
  await page.locator("#plan-title").fill(`Tippplan ${LAUF}`);
  await page.getByRole("combobox", { name: "Kind" }).selectOption(childId);
  await page.getByRole("combobox", { name: "Fach (Pflicht)" }).selectOption({ label: "Englisch" });
  await page.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(page).toHaveURL(/\/vater\/plan\/\d+$/);

  const punkteFeld = page.getByLabel("Punkte bei erreichtem Ziel", { exact: true });
  await page.locator('select[aria-label="Fach-Filter"]').selectOption({ label: "Englisch" });
  await page.locator('select[aria-label="Reihe-Filter"]').selectOption({ label: reiheName });
  await page.locator('select[aria-label="Unit-Filter"]').selectOption({ label: unitLabel });

  await page.getByRole("radio", { name: new RegExp(ersteUebung) }).check();
  await expect(punkteFeld).toHaveValue("40");

  // ---- Der Vater sagt etwas Eigenes ----
  await punkteFeld.fill("77");

  // ---- Und der Uebungswechsel raeumt es NICHT weg. Ohne setPunkteGetippt(true) staende hier 60. ----
  await page.getByRole("radio", { name: new RegExp(zweiteUebung) }).check();
  await expect(punkteFeld).toHaveValue("77");

  await page.getByRole("button", { name: /Position hinzufügen/ }).click();
  await expect(page.getByRole("row", { name: new RegExp(zweiteUebung) })).toContainText("Ziel 77");

  // ---- Mit der Position endet die Aussage: die naechste Uebung belegt wieder vor. ----
  // Ohne setPunkteGetippt(false) im Anlegen stuende hier weiter 77 - Position 2..n truegen dann eine
  // Zahl, die fuer SIE niemand gesagt hat (zweiter Reviewer-Fund).
  await page.getByRole("radio", { name: new RegExp(ersteUebung) }).check();
  await expect(punkteFeld).toHaveValue("40");
});
