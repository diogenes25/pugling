import { test, expect } from "@playwright/test";
import { vaterLogin } from "./helpers";

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

  await page.locator("#new-child-name").fill(CHILD.name);
  await page.locator("#new-child-pin").fill(CHILD.pin);
  await page.getByRole("button", { name: "Kind anlegen" }).click();
  // Über den LINK, nicht über Tabelle und Zellindex: der Name steht auch in der „Heute"-Tabelle darüber,
  // dort aber als reiner Text – nur in der Kinder-Liste ist er ein Link. Das löst den
  // Strict-Mode-Verstoß ohne jede Annahme über Reihenfolge oder Spalten (Muster `vater-von-null.spec.ts`).
  const kindLink = page.getByRole("link", { name: CHILD.name });
  await expect(kindLink).toBeVisible();
  const childId = (await kindLink.getAttribute("href"))!.split("/").pop()!;

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
