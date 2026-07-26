import { test, expect, type Page } from "@playwright/test";

// End-to-End der Frage „wer kennt den Stoff dieses Kindes?":
//   Vater legt die Buchreihe an  →  eine Unit mit Themen/Grammatik/Wortschatz  →  einen Fachlehrer auf
//   genau diese Reihe  →  hinterlegt sie am Buch des Kindes  →  und sieht, dass das Profil zum Kind passt.
//
// Der Kern, den dieser Test absichert, ist nicht „ein Formular speichert", sondern die Kopplung dahinter:
// Kind-Lehrbuch und Creator-Profil zeigen auf DENSELBEN Katalog-Eintrag, und nur deshalb liefert der
// Server einen begründeten Treffer statt eines Namensvergleichs. Ohne die Reihe am Kind bleibt die
// Antwort leer – genau das prüft der Test vorher.

const FATHER = { id: "1", pin: "0000" };
const SERIES = { name: "Access E2E", publisher: "Cornelsen" };
const UNIT = { label: "Unit 3 – Growing up", grade: "8", grammar: "Present perfect vs. simple past" };
const PROFILE = "Englisch 8 Gymnasium – Access E2E";
// Eigenes Kind statt des geseedeten Sohns: die Specs teilen eine Backend-Instanz, und dieser Test ändert
// Klasse und Schulart – am gemeinsamen Kind würde er dem Haupt-Flow die Übungsauswahl verschieben.
const CHILD = { name: "Lehrwerk-Kind", pin: "5678" };

async function vaterLogin(page: Page) {
  await page.goto("/vater");
  await page.locator("#fid").fill(FATHER.id);
  await page.locator("#pin").fill(FATHER.pin);
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();
}

/** Legt ein frisches Kind an und liefert dessen Id (erste Spalte der Kinder-Tabelle). */
async function createChild(page: Page): Promise<string> {
  await page.goto("/vater");
  await page.locator("#new-child-name").fill(CHILD.name);
  await page.locator("#new-child-pin").fill(CHILD.pin);
  await page.getByRole("button", { name: "Kind anlegen" }).click();
  const row = page.getByRole("row", { name: new RegExp(CHILD.name) });
  await expect(row).toBeVisible();
  return (await row.getByRole("cell").first().textContent())!.trim();
}

test("Buchreihe, Fachlehrer und Kind treffen zusammen", async ({ page }) => {
  await vaterLogin(page);
  const childId = await createChild(page);

  // ---- Klasse und Schulart am Kind: sie entscheiden mit, welches Profil überhaupt passt ----
  await page.goto(`/vater/kind/${childId}`);
  await page.locator("#kd-grade").fill("8");
  await page.locator("#kd-school").selectOption("Gymnasium");
  await page.getByRole("button", { name: "Speichern" }).first().click();
  await expect(page.getByText("Gespeichert.").first()).toBeVisible();

  // Noch ohne Buch und ohne Profil: die Suche muss ehrlich leer bleiben.
  await expect(page.getByText(/Kein Profil passt zu/)).toBeVisible();

  // ---- Die Buchreihe (geteilter Katalog) ----
  await page.goto("/vater/lehrwerke");
  await page.locator("#ns-name").fill(SERIES.name);
  await page.locator("#ns-publisher").fill(SERIES.publisher);
  await page.locator("#ns-subject").selectOption({ label: "Englisch" });
  await page.locator("#ns-school").selectOption("Gymnasium");
  await page.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(page.getByText(/steht im Katalog/)).toBeVisible();

  const seriesRow = page.getByRole("row", { name: new RegExp(SERIES.name) });
  await expect(seriesRow).toBeVisible();
  // Frisch angelegt: noch keine Unit – der Creator kennt bis hier nur den Reihennamen.
  await expect(seriesRow.getByText("keine")).toBeVisible();

  // ---- Die Unit mit ihrem Stoff: der eigentliche Gewinn dieser Ebene ----
  await seriesRow.getByRole("button", { name: "Units" }).click();
  await page.locator('[id^="unit-label-new"]').fill(UNIT.label);
  await page.locator('[id^="unit-grade-new"]').fill(UNIT.grade);
  await page.locator('[id^="unit-topics-new"]').fill("Familie, Freundschaft");
  await page.locator('[id^="unit-grammar-new"]').fill(UNIT.grammar);
  await page.locator('[id^="unit-vocab-new"]').fill("to grow up, responsibility, to argue");
  await page.getByRole("button", { name: "Unit hinzufügen" }).click();
  await expect(page.getByText(UNIT.grammar)).toBeVisible();

  // ---- Der Fachlehrer auf genau diese Reihe ----
  await page.goto("/vater/fachlehrer");
  await page.locator("#fl-name-new").fill(PROFILE);
  await page.locator("#fl-subject-new").selectOption({ label: "Englisch" });
  await page.locator("#fl-school-new").selectOption("Gymnasium");
  await page.locator("#fl-min-new").fill("7");
  await page.locator("#fl-max-new").fill("8");
  await page.locator("#fl-series-new").selectOption({ label: `${SERIES.name} (${SERIES.publisher})` });
  await page.locator("#fl-persona-new").fill("Du bist Englischlehrer an einem Gymnasium.");
  await page.getByRole("button", { name: "Vocabulary" }).click();
  await page.getByRole("button", { name: "Fachlehrer anlegen" }).click();
  await expect(page.getByText("Fachlehrer angelegt.")).toBeVisible();
  await expect(page.getByRole("row", { name: new RegExp(PROFILE) })).toBeVisible();

  // ---- Die Reihe am Buch des Kindes: erst sie schließt die Kette ----
  await page.goto(`/vater/kind/${childId}`);
  await page.locator("#tbnew-title").fill("Access 8");
  await page.locator("#tbnew-subject").selectOption({ label: "Englisch" });
  await page.locator("#tbnew-series").selectOption({ label: `${SERIES.name} (${SERIES.publisher})` });
  // Die Unit-Auswahl hängt an der Reihe – vorher ist sie gesperrt, jetzt gefüllt.
  await page.locator("#tbnew-unit").selectOption({ label: `Kl. ${UNIT.grade}: ${UNIT.label}` });
  await page.getByRole("button", { name: "Buch hinterlegen" }).click();
  await expect(page.getByText("Buch hinterlegt.")).toBeVisible();

  const bookRow = page.getByRole("row", { name: /Access 8/ });
  await expect(bookRow).toContainText(SERIES.name);
  await expect(bookRow).toContainText(UNIT.label);

  // ---- Und die Antwort: der Fachlehrer passt, mit Begründung ----
  const matchRow = page.getByRole("row", { name: new RegExp(PROFILE) });
  await expect(matchRow).toBeVisible();
  await expect(matchRow).toContainText("beste Wahl");
  // Der Reihen-Treffer ist der Grund, warum dieses Profil gewinnt – nicht bloß Fach und Klasse.
  await expect(matchRow).toContainText("gleiche Buchreihe");
});
