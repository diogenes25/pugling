import { test, expect, type Page } from "@playwright/test";

/*
 * „Von Null": ein Vater, der noch nicht existiert, richtet sich selbst ein und baut ein komplettes
 * Englisch-Szenario für ein neues Kind auf – ohne das Seed-Konto anzufassen.
 *
 * Der Sinn dieser Strecke ist die **Vollständigkeit des Weges**: jeder Schritt hier war einmal nur über
 * die API erreichbar. Sie deckt darum bewusst auch die Korrektur ab (eine bestehende Übung um ein Wort
 * ergänzen) – ohne sie wäre ein Tippfehler nur durch Löschen und Neuanlegen zu beheben, und Löschen ist
 * gesperrt, sobald die Übung in einem Plan steckt.
 *
 * Alle Namen tragen einen Lauf-Suffix, weil Fächer und Vokabeln **global** sind: ein fester Name würde beim
 * zweiten Lauf gegen dieselbe DB auf einen Unique-Index oder auf mehrere Treffer laufen.
 */

const RUN = Date.now().toString().slice(-6);
const FATHER = { name: `E2E-Vater ${RUN}`, pin: "4711" };
const CHILD = { name: `E2E-Kind ${RUN}`, pin: "2468" };
const SUBJECT = `E2E-Englisch ${RUN}`;
const CHAPTER = "Unit 1";
const EXERCISE = `Wörter Unit 1 ${RUN}`;
const WORDS: [string, string][] = [["bike", "Fahrrad"], ["tree", "Baum"], ["house", "Haus"]];
const EXTRA_WORD: [string, string] = ["river", "Fluss"];

/** Registrierung inkl. Auto-Login; liefert die frisch vergebene Vater-Id. */
async function registerFather(page: Page): Promise<string> {
  await page.goto("/vater");
  await page.getByRole("radio", { name: "Neu registrieren" }).click();
  await page.locator("#reg-name").fill(FATHER.name);
  await page.locator("#reg-pin").fill(FATHER.pin);
  await page.locator("#reg-pin2").fill(FATHER.pin);
  await page.getByRole("button", { name: "Konto anlegen" }).click();

  // Nach dem Auto-Login ist das Dashboard da; die Id steht im Kopf der Seite.
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();
  const badge = await page.locator("header.vater-top a").filter({ hasText: FATHER.name }).innerText();
  const id = badge.match(/#(\d+)/)?.[1];
  expect(id, "Vater-Id muss im Kopf stehen – sie ist der Login-Name").toBeTruthy();
  return id!;
}

test("Vater legt sich selbst an und richtet ein Englisch-Szenario von Null ein", async ({ browser }) => {
  const vaterCtx = await browser.newContext();
  const vater = await vaterCtx.newPage();

  // ---------- 1. Konto anlegen ----------
  const fatherId = await registerFather(vater);

  // ---------- 2. Kind mit PIN anlegen ----------
  // Die PIN ist der Login des Kindes; ohne sie kommt es nicht in seine App (Schritt 9 prüft genau das).
  await vater.locator("#new-child-name").fill(CHILD.name);
  await vater.locator("#new-child-grade").fill("5");
  await vater.locator("#new-child-pin").fill(CHILD.pin);
  await vater.getByRole("button", { name: "Kind anlegen" }).click();
  await expect(vater.getByText("Kind angelegt.")).toBeVisible();

  const childLink = vater.getByRole("link", { name: CHILD.name });
  await expect(childLink).toBeVisible();
  const childId = (await childLink.getAttribute("href"))!.split("/").pop()!;

  // ---------- 3. Stammdaten des Kindes nachziehen ----------
  await childLink.click();
  await expect(vater.getByRole("heading", { name: CHILD.name })).toBeVisible();
  await vater.locator("#kd-grade").fill("6");
  await vater.locator("#kd-interests").fill("Fußball, Minecraft");
  // `exact`, weil daneben „Interessen speichern" steht (die gewichteten Tags sind ein eigener Editor).
  await vater.getByRole("button", { name: "Speichern", exact: true }).click();
  await expect(vater.getByText("Gespeichert.", { exact: true })).toBeVisible();
  // Gegenprobe über den Server, nicht über das Formular: neu laden und den Wert lesen.
  await vater.reload();
  await expect(vater.locator("#kd-grade")).toHaveValue("6");

  // Ein geleertes Feld muss auch wirklich leeren: im PATCH heißt `null` „nicht angegeben", der Löschwunsch
  // läuft darum über einen Clear-Schalter. Ohne ihn meldete das UI „Gespeichert." und der Wert kam zurück.
  await vater.locator("#kd-grade").fill("");
  await vater.getByRole("button", { name: "Speichern", exact: true }).click();
  await expect(vater.getByText("Gespeichert.", { exact: true })).toBeVisible();
  await vater.reload();
  await expect(vater.locator("#kd-grade")).toHaveValue("");
  // Klasse wieder setzen – sie filtert die Übungssuche, das Kind soll vollständig bleiben.
  await vater.locator("#kd-grade").fill("6");
  await vater.getByRole("button", { name: "Speichern", exact: true }).click();
  await expect(vater.getByText("Gespeichert.", { exact: true })).toBeVisible();

  // ---------- 4. Katalog: Fach, Kapitel, Vokabeln, Übung ----------
  await vater.goto("/vater/exercises");
  await vater.getByPlaceholder("z. B. Französisch").fill(SUBJECT);
  await vater.getByRole("button", { name: "Fach anlegen" }).click();
  await expect(vater.locator('select[aria-label="Fach"]')).toHaveValue(/\d+/);

  await vater.getByPlaceholder("z. B. Unit 1").fill(CHAPTER);
  await vater.getByRole("button", { name: "Kapitel anlegen" }).click();
  await expect(vater.locator('select[aria-label="Kapitel"]')).toHaveValue(/\d+/);

  await vater.locator("#ex-title").fill(EXERCISE);
  // Vokabeln direkt aus dem Editor in den Store legen und übernehmen (en→de ist die Vorbelegung).
  for (const [word, translation] of WORDS) {
    await vater.locator("#vp-word").fill(word);
    await vater.locator("#vp-translation").fill(translation);
    await vater.getByRole("button", { name: /anlegen & wählen/ }).click();
    await expect(vater.locator(".token", { hasText: `${word}→${translation}` })).toBeVisible();
  }
  await vater.getByRole("button", { name: "Übung anlegen" }).click();
  await expect(vater.getByText(`Übung „${EXERCISE}" angelegt.`)).toBeVisible();

  // ---------- 5. Übung korrigieren: ein viertes Wort ergänzen ----------
  const exerciseRow = vater.locator("div", { hasText: EXERCISE }).last();
  await exerciseRow.getByRole("button", { name: /Bearbeiten/ }).click();
  const dialog = vater.getByRole("dialog", { name: new RegExp(`Übung bearbeiten: ${EXERCISE}`) });
  await expect(dialog.getByRole("heading", { name: new RegExp(`Bearbeiten · ${EXERCISE}`) })).toBeVisible();
  // Die Wortpaare stehen als eigene Ebene mit stabilen Ids – Ergänzen lässt den Rest (und den Lernstand) unberührt.
  await expect(dialog.getByRole("heading", { name: /Wortpaare \(3\)/ })).toBeVisible();
  await dialog.locator("#ai-front").fill(EXTRA_WORD[0]);
  await dialog.locator("#ai-back").fill(EXTRA_WORD[1]);
  await dialog.getByRole("button", { name: /anlegen & hinzufügen/ }).click();
  await expect(dialog.getByRole("heading", { name: /Wortpaare \(4\)/ })).toBeVisible();
  await expect(dialog.getByRole("cell", { name: EXTRA_WORD[0], exact: true })).toBeVisible();
  await dialog.getByRole("button", { name: "Schließen" }).click();

  // ---------- 5b. Ein Typ, den dieses UI nicht kennt, darf die Liste nicht zerlegen ----------
  /*
   * Das Backend führt mehr Übungstypen als das Vater-UI (Grammar, Translation, Reading … – der KI-Creator
   * legt Grammar tatsächlich an). Solche Übungen dürfen in der Verwaltung erscheinen, aber weder einen
   * Bearbeiten-Weg anbieten (es gibt kein Routen-Segment für sie) noch die Seite zum Absturz bringen.
   * Angelegt wird sie über die API, weil das UI sie bewusst nicht anlegen kann.
   */
  const token = await vater.evaluate(() => localStorage.getItem("pugling.token"));
  const subjectId = await vater.locator('select[aria-label="Fach"]').inputValue();
  const chapterId = await vater.locator('select[aria-label="Kapitel"]').inputValue();
  const created = await vater.request.post(
    `/api/v1/creator/subjects/${subjectId}/chapters/${chapterId}/grammar`,
    {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        title: `Grammatik ${RUN}`, orderIndex: 99, rewardPoints: 5,
        config: { instruction: "Setze ein.", tasks: [{ prompt: "I ___ tired.", answer: "am" }] },
      },
    });
  expect(created.ok(), await created.text()).toBeTruthy();

  // Kapitel ab- und wieder anwählen statt neu zu laden: ein Reload würde Fach/Kapitel zurücksetzen und
  // damit die Übungsliste ganz ausblenden.
  const chapterSelect = vater.locator('select[aria-label="Kapitel"]');
  await chapterSelect.selectOption("");
  await chapterSelect.selectOption(chapterId);
  const grammarRow = vater.locator("div", { hasText: `Grammatik ${RUN}` }).last();
  await expect(grammarRow).toContainText("Typ hier nicht bearbeitbar");
  await expect(grammarRow.getByRole("button", { name: /Bearbeiten/ })).toHaveCount(0);
  // Die eigene, bearbeitbare Übung ist weiterhin da – die Liste lebt.
  await expect(vater.getByRole("button", { name: /🧪 Ausprobieren/ }).first()).toBeVisible();

  // ---------- 6. Lehrplan mit Position (inkl. Münz-Malus) ----------
  await vater.goto("/vater/plan/new");
  await vater.locator("#plan-title").fill(`Plan ${RUN}`);
  const kindSelect = vater.getByRole("combobox", { name: "Kind" });
  await expect(kindSelect.locator("option")).not.toHaveCount(0);
  await kindSelect.selectOption(childId);
  await vater.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(vater).toHaveURL(/\/vater\/plan\/\d+$/);

  const exRadio = vater.getByRole("radio", { name: new RegExp(EXERCISE) });
  await expect(exRadio).toBeVisible();
  await exRadio.check();
  await vater.locator('select[aria-label="Ziel-Rhythmus"]').selectOption("Daily");
  await vater.locator('input[aria-label="Bestehen ab Prozent"]').fill("80");
  // Der „Stick": verpasste Pflicht kostet Münzen. Vorher war er im geführten Weg nicht erreichbar.
  await vater.locator('input[aria-label="Münz-Malus bei gerissener Pflicht"]').fill("5");
  await vater.getByRole("checkbox", { name: /Leitner/ }).check();
  await vater.getByRole("button", { name: /Position hinzufügen/ }).click();
  const positionRow = vater.getByRole("row", { name: new RegExp(EXERCISE) });
  await expect(positionRow).toBeVisible();
  await expect(positionRow).toContainText("Malus −5");
  await expect(positionRow).toContainText("bestehen ab 80%");

  // ---------- 7. Familien-Shop: Artikel + Angebot ----------
  await vater.goto(`/vater/shop?childId=${childId}`);
  await vater.getByLabel("Artikelnummer").fill(`TV-${RUN}`);
  await vater.getByLabel("Titel", { exact: true }).fill(`Fernsehzeit ${RUN}`);
  await vater.getByRole("button", { name: "Anlegen" }).click();
  await expect(vater.getByText(`Artikel „Fernsehzeit ${RUN}" angelegt.`)).toBeVisible();

  await vater.getByRole("row", { name: new RegExp(`Fernsehzeit ${RUN}`) })
    .getByRole("button", { name: /Angebote/ }).click();
  await vater.getByRole("button", { name: "Angebot anlegen" }).click();
  await expect(vater.getByText("Angebot angelegt.")).toBeVisible();

  // ---------- 8. Lernziel auf Fach-Ebene ----------
  await vater.goto(`/vater/kind/${childId}/ziele`);
  // Über das Label statt eine feste Id: der Scope-Wähler steht mehrfach im DOM (Lernziel + Etappen)
  // und vergibt seine Ids darum je Instanz.
  await vater.getByLabel("Fach", { exact: true }).selectOption({ label: SUBJECT });
  await vater.getByLabel("Titel (optional)").fill("Unit 1 sitzt");
  await vater.getByRole("button", { name: "Lernziel anlegen" }).click();
  await expect(vater.getByText("Lernziel angelegt.")).toBeVisible();
  const goalRow = vater.getByRole("row", { name: /Unit 1 sitzt/ });
  await expect(goalRow).toContainText("offen");
  await expect(goalRow).toContainText("mindestens 80 %");

  // ---------- 8b. Lernstand: beide Sichten laden (noch ohne Fortschritt) ----------
  // Sichert die Routen und die Lesezugriffe auf die student/-Endpunkte ab, die der Vater mitlesen darf.
  await vater.goto(`/vater/kind/${childId}/lernstand`);
  await expect(vater.getByText("Kein Wort unter 50 %", { exact: false })).toBeVisible();
  await vater.getByRole("radio", { name: /Nach Katalog/ }).click();
  await expect(vater.getByRole("heading", { name: "Fächer" })).toBeVisible();

  // ---------- 9. Das Kind kommt mit der gesetzten PIN herein und sieht den Plan ----------
  const sohnCtx = await browser.newContext();
  const sohn = await sohnCtx.newPage();
  await sohn.goto("/sohn");
  await sohn.locator("#childId").fill(childId);
  for (const d of CHILD.pin.split("")) {
    await sohn.locator(".keys button", { hasText: new RegExp(`^${d}$`) }).first().click();
  }
  await sohn.getByRole("button", { name: "▶ LOS" }).click();
  await expect(sohn.getByText("Tagesmission")).toBeVisible();
  await expect(sohn.getByText(new RegExp(EXERCISE))).toBeVisible();

  // ---------- 10. Der Vater kann sich mit seiner Id wieder anmelden ----------
  const wieder = await (await browser.newContext()).newPage();
  await wieder.goto("/vater");
  await wieder.locator("#fid").fill(fatherId);
  await wieder.locator("#pin").fill(FATHER.pin);
  await wieder.getByRole("button", { name: "Anmelden" }).click();
  await expect(wieder.getByRole("heading", { name: "Kinder" })).toBeVisible();
  await expect(wieder.getByRole("link", { name: CHILD.name })).toBeVisible();
});
