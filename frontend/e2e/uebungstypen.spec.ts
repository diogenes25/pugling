import { test, expect, type Page } from "@playwright/test";

/*
 * Legt **jeden** Übungstyp über das Vater-UI an, den es im Server-Manifest gibt.
 *
 * Der Sinn ist der Abgleich zweier Seiten, die auseinanderdriften können: das Formular baut die
 * typ-spezifische Config, der Server erwartet eine bestimmte Feldform. Ein vertippter Feldname fällt
 * sonst erst auf, wenn ein Kind die Übung spielt (oder gar nicht, weil die Config still leer bleibt).
 * Darum wird hier nicht nur „201 angelegt" geprüft, sondern bei einem Typ auch der Rückweg: Bearbeiten
 * öffnen, die geladenen Werte wiederfinden, ergänzen, speichern, erneut öffnen.
 *
 * Zusätzlich fällt hier auf, wenn das Manifest einen Typ führt, den das UI nicht anlegen kann – der
 * Vergleich am Ende zählt die Einträge im Typ-Pulldown gegen die Manifest-Liste.
 */

const FATHER = { id: "1", pin: "0000" };
const RUN = Date.now().toString().slice(-6);
const SUBJECT = `E2E-Typen ${RUN}`;
const CHAPTER = "Unit 1";

async function vaterLogin(page: Page) {
  await page.goto("/vater");
  await page.locator("#fid").fill(FATHER.id);
  await page.locator("#pin").fill(FATHER.pin);
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();
}

/** Titel wählen, Typ umschalten, anlegen – und die Erfolgsmeldung abwarten. */
async function createExercise(page: Page, type: string, title: string, fill: () => Promise<void>) {
  await page.locator('select[aria-label="Übungstyp"]').selectOption(type);
  await page.locator("#ex-title").fill(title);
  await fill();
  await page.getByRole("button", { name: "Übung anlegen" }).click();
  await expect(page.getByText(`Übung „${title}" angelegt.`)).toBeVisible();
}

test("Jeder Übungstyp des Manifests lässt sich im UI anlegen", async ({ page }) => {
  await vaterLogin(page);
  await page.goto("/vater/exercises");

  // Fach + Kapitel einmal anlegen (global, darum je Lauf eindeutig benannt).
  await page.getByPlaceholder("z. B. Französisch").fill(SUBJECT);
  await page.getByRole("button", { name: "Fach anlegen" }).click();
  await expect(page.locator('select[aria-label="Fach"]')).toHaveValue(/\d+/);
  await page.getByPlaceholder("z. B. Unit 1").fill(CHAPTER);
  await page.getByRole("button", { name: "Kapitel anlegen" }).click();
  await expect(page.locator('select[aria-label="Kapitel"]')).toHaveValue(/\d+/);

  // ---------- Leseverständnis ----------
  await createExercise(page, "Reading", `Lesen ${RUN}`, async () => {
    await page.locator("#cfg-reading-text").fill("Tom goes to Brighton by train. The trip takes two hours.");
    await page.getByLabel("Frage").fill("Where does Tom go?");
    await page.getByLabel("Antwort", { exact: false }).first().fill("Brighton");
  });

  // ---------- Hörverständnis (Multiple-Choice-Frage) ----------
  await createExercise(page, "Listening", `Hören ${RUN}`, async () => {
    await page.locator("#cfg-audio").fill("https://example.org/dialog.mp3");
    await page.locator("#cfg-transcript").fill("A: Where are you from? B: I'm from Leeds.");
    await page.getByLabel("Frage").fill("Where is B from?");
    await page.getByLabel("Antwort", { exact: false }).first().fill("Leeds");
    await page.getByLabel("Auswahl", { exact: false }).fill("Leeds, York, Hull");
  });

  // ---------- Aufsatz (mit Bewertungskriterium) ----------
  await createExercise(page, "Essay", `Aufsatz ${RUN}`, async () => {
    await page.locator("#cfg-prompt").fill("Schreibe einen Brief über deine Hobbys.");
    await page.locator("#cfg-minwords").fill("80");
    await page.locator("#cfg-maxwords").fill("150");
    await page.getByLabel("Kriterium").fill("Aufbau");
    // `exact`: Sonst trifft der Teilstring-Vergleich auch das „ⓘ" der Übungs-Punkte
    // („Erklärung zu „Punkte der Übung"") – und das ist kein Eingabefeld.
    await page.getByLabel("Punkte", { exact: true }).first().fill("5");
  });

  // ---------- Grammatik ----------
  await createExercise(page, "Grammar", `Grammatik ${RUN}`, async () => {
    await page.getByLabel("Anweisung").fill("Setze die richtige Form ein.");
    await page.getByLabel("Aufgabe").fill("He ___ (go) to school.");
    await page.getByLabel("Lösung").fill("goes");
    await page.getByLabel("Regel-Hinweis", { exact: false }).fill("3. Person Singular: -s");
  });

  // ---------- Übersetzung (landet im Vokabel-Store) ----------
  await createExercise(page, "Translation", `Übersetzen ${RUN}`, async () => {
    await page.locator("#cfg-tr-src").selectOption("en");
    await page.locator("#cfg-tr-tgt").selectOption("de");
    await page.getByLabel("Satz (Ausgangssprache)").fill("Where do you live?");
    await page.getByLabel("Übersetzung").fill("Wo wohnst du?");
  });

  // ---------- Rechen-Drill (nur Regeln, keine Zeilen) ----------
  await createExercise(page, "ArithmeticDrill", `Drill ${RUN}`, async () => {
    await page.getByRole("checkbox", { name: /Multiplikation/ }).check();
    await page.locator("#cfg-min").fill("2");
    await page.locator("#cfg-max").fill("10");
    await page.locator("#cfg-count").fill("8");
    await page.locator("#cfg-seed").fill("42");
  });

  // Alle sechs stehen in der Kapitel-Liste – mit ihrem deutschen Namen aus dem Manifest.
  for (const label of ["Leseverständnis", "Hörverständnis", "Aufsatz", "Grammatik", "Übersetzung", "Rechen-Drill"]) {
    await expect(page.getByText(`· ${label}`, { exact: false }).first()).toBeVisible();
  }

  // ---------- Rückweg: Inhalt wird geladen, ergänzt und bleibt erhalten ----------
  const grammarRow = page.locator("div", { hasText: `Grammatik ${RUN}` }).last();
  await grammarRow.getByRole("button", { name: /Bearbeiten/ }).click();
  const dialog = page.getByRole("dialog", { name: new RegExp(`Grammatik ${RUN}`) });
  // Der geladene Inhalt muss im Editor stehen – sonst schreibt Speichern eine leere Config zurück.
  await expect(dialog.getByLabel("Aufgabe")).toHaveValue("He ___ (go) to school.");
  await expect(dialog.getByLabel("Lösung")).toHaveValue("goes");
  await expect(dialog.getByLabel("Anweisung")).toHaveValue("Setze die richtige Form ein.");

  await dialog.getByRole("button", { name: "+ Aufgabe" }).click();
  await dialog.getByLabel("Aufgabe").nth(1).fill("She ___ (have) a dog.");
  await dialog.getByLabel("Lösung").nth(1).fill("has");
  await dialog.getByRole("button", { name: "Inhalt speichern" }).click();
  await expect(dialog.getByText("Inhalt gespeichert.")).toBeVisible();

  // Neu öffnen: beide Aufgaben müssen da sein (Schreiben und Zurücklesen passen zueinander).
  await dialog.getByRole("button", { name: "Schließen" }).click();
  await grammarRow.getByRole("button", { name: /Bearbeiten/ }).click();
  await expect(dialog.getByLabel("Aufgabe").nth(1)).toHaveValue("She ___ (have) a dog.");
  await dialog.getByRole("button", { name: "Schließen" }).click();

  // ---------- Testmodus: durchspielbar, wo es Aufgaben gibt ----------
  // Grammatik hat prüfbare Einzelaufgaben → der Testmodus spielt sie aus.
  await grammarRow.getByRole("button", { name: /Ausprobieren/ }).click();
  const preview = page.getByRole("dialog", { name: new RegExp(`Testmodus: Grammatik ${RUN}`) });
  await expect(preview.getByText("He ___ (go) to school.")).toBeVisible();
  await preview.getByRole("button", { name: "Schließen" }).click();

  // Ein Aufsatz hat keine – das muss als Eigenschaft des Typs erklärt werden, nicht als Fehler aussehen.
  const essayRow = page.locator("div", { hasText: `Aufsatz ${RUN}` }).last();
  await essayRow.getByRole("button", { name: /Ausprobieren/ }).click();
  await expect(page.getByText(/keine einzeln prüfbaren Aufgaben/)).toBeVisible();
  await page.getByRole("dialog", { name: new RegExp(`Testmodus: Aufsatz ${RUN}`) })
    .getByRole("button", { name: "Schließen" }).click();

  // ---------- Das Pulldown deckt das Manifest ab ----------
  // Ein Typ, den der Server führt und das UI nicht anlegen kann, wäre eine stille Lücke.
  const manifest = await page.request.get("/api/v1/creator/exercise-types", {
    headers: { Authorization: `Bearer ${await page.evaluate(() => localStorage.getItem("pugling.token"))}` },
  });
  expect(manifest.ok()).toBeTruthy();
  const serverTypes = ((await manifest.json()) as { type: string }[]).map((m) => m.type).sort();
  const uiTypes = await page.locator('select[aria-label="Übungstyp"] option').evaluateAll(
    (os) => os.map((o) => (o as HTMLOptionElement).value).sort());
  expect(uiTypes).toEqual(serverTypes);
});
