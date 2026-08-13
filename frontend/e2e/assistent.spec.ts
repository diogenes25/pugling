import { test, expect, type Page } from "@playwright/test";

/*
 * B-58: der Lehrplan-Assistent hat einen echten Durchstich. Bis hierher fuhr kein Test ihn zu Ende – nur
 * `wizardFinish.ts` selbst war über sieben Unit-Fälle abgesichert. Diese Spec prüft genau die ungeprüfte
 * Naht: dass der Bildschirm aus fünf Schritten wirklich das schreibt, was im Feinschliff eingetippt wurde
 * (nicht vertauscht, `goalThreshold` gegen `penaltyCoins`), und dass ein Doppelklick auf „Lehrplan
 * erstellen" nur einen einzigen Anlege-Vorgang auslöst (B-53s Sperre am echten Knopf).
 *
 * Eigene Datei statt eines Blocks in `vater-von-null.spec.ts` (frontend/CLAUDE.md, „Eine Fläche je
 * E2E-Spec"): ein Rot muss sofort sagen, ob der Assistent oder der manuelle Weg gebrochen ist.
 */

const RUN = Date.now().toString().slice(-6);
const CHILD_NAME = `E2E-Assistent-${RUN}`;

async function vaterLogin(page: Page) {
  await page.goto("/vater");
  await page.locator("#fid").fill("1");
  await page.locator("#pin").fill("0000");
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();
}

test("Der Assistent legt Kind, Plan und Position mit den eingetippten Feinschliff-Werten an", async ({ page }) => {
  await vaterLogin(page);
  await page.goto("/vater/wizard");

  // ---------- Schritt 1: Kind (neu, damit ein wiederholter Lauf nicht kollidiert) ----------
  // Solange niemand gewaehlt hat, springt der Assistent selbst auf "existing" und waehlt das erste Kind
  // vor - die Pille muss darum explizit zurueckgeklickt werden. Seit B-18 ist dieser Klick auch dann
  // bindend, wenn die Kinderliste ERST DANACH ankommt; vorher nahm der Effekt ihn wieder zurueck, und der
  // Test haengte an einem `#wiz-name`, das nicht mehr da war.
  await page.getByRole("button", { name: "Neues Kind anlegen" }).click();
  await page.locator("#wiz-name").fill(CHILD_NAME);
  await page.locator("#wiz-pin").fill("1357");
  await page.getByRole("button", { name: "Weiter" }).click();

  // ---------- Schritt 2: Problemfeld ----------
  await page.getByLabel("Fach").selectOption({ label: "Englisch" });
  await page.getByRole("button", { name: /Regelmäßig üben/ }).click();
  await page.getByRole("button", { name: /^Normal/ }).click();
  await page.getByRole("button", { name: "Weiter" }).click();

  // ---------- Schritt 3: Übungen (auf eine einzelne, seed-stabile Übung gefiltert) ----------
  await page.getByRole("heading", { name: /Übungen wählen/ }).waitFor();
  await page.getByLabel("Übung suchen").fill("environment");
  // B-163: die Zeile nennt den Anzeigenamen aus dem Manifest, nicht den Schlüssel. Der Titel dieser Übung
  // heißt „Vocabulary: The environment" und enthält den Schlüssel selbst – „Vokabelkarten" kann darum nur
  // aus dem Manifest kommen. Ein Rückfall auf `e.type` macht das rot, und genau der war hier ungeschützt.
  await expect(page.getByRole("checkbox", { name: /The environment/ }))
    .toHaveAccessibleName(/· Vokabelkarten/);
  await page.getByRole("button", { name: "Alle wählen" }).click();
  // Seit B-18 kann "Alle waehlen" asynchron nachladen (bei mehr Treffern als geladen, take=500). Auf die
  // Zahl in der Ueberschrift warten, statt sofort weiterzuklicken: Sonst traefe "Weiter" ein noch leeres
  // `selected` und die Spec braeche mit "Bitte mindestens eine Uebung waehlen" ab, sobald der Seed-Katalog
  // ueber eine Seite waechst (B-109: ein Flackern nimmt die ganze Datei mit).
  // Ohne schliessende Klammer: seit B-161 nennt die Ueberschrift bei einer Auswahl ueber die geladene Seite
  // hinaus auch die Unsichtbaren ("(500 gewählt, davon 400 unten nicht sichtbar)"). Genau in dem Fall, fuer
  // den dieser Kommentar geschrieben wurde, haette die alte Regex versagt.
  await expect(page.getByRole("heading", { name: /\(\d+ gewählt/ })).toBeVisible();
  await page.getByRole("button", { name: "Weiter" }).click();

  // ---------- Schritt 4: Feinschliff – bewusst von der Intensitäts-Vorbelegung (80/5) abweichend ----------
  await expect(page.getByRole("heading", { name: /Feinschliff/ })).toBeVisible();
  await page.locator("#wiz-pass").fill("95");
  await page.locator("#wiz-penalty").fill("7");
  await page.getByRole("button", { name: "Weiter" }).click();

  // ---------- Schritt 5: Überblick → abschicken, doppelt geklickt ----------
  await expect(page.getByRole("heading", { name: "Überblick" })).toBeVisible();
  const childPosts: string[] = [];
  page.on("request", (r) => {
    if (r.method() === "POST" && r.url().endsWith("/api/v1/supervisor/children")) childPosts.push(r.url());
  });
  await page.getByRole("button", { name: "✅ Lehrplan erstellen" }).dblclick();

  // Der Wartepunkt für die POST-Zählung: erst nach der Navigation ist der Durchgang durch.
  await expect(page).toHaveURL(/\/vater\/plan\/\d+$/);
  expect(childPosts, "Ein Doppelklick darf genau ein neues Kind anlegen").toHaveLength(1);

  // ---------- Nachweis: die Positions-Zeile trägt die eingetippten, nicht die vertauschten Werte ----------
  const positionRow = page.getByRole("row", { name: /environment/ });
  await expect(positionRow).toBeVisible();
  await expect(positionRow).toContainText("bestehen ab 95%");
  await expect(positionRow).toContainText("Malus −7");
});
