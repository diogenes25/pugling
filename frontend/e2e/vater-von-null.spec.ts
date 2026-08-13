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

  // ---------- 2a. Ein Fehler beim Anlegen erscheint ROT (B-54) ----------
  /*
   * Bis zum 2026-08-01 schrieben Erfolg und Fehler in *dieselbe* Variable, die fest als `banner ok`
   * gerendert wurde: „Kind angelegt." und „Name schon vergeben" sahen identisch aus.
   *
   * Der Fehler wird abgefangen statt provoziert, weil der Server hier keinen hergibt: `ChildrenController`
   * prüft nur den leeren Namen, und den fängt das Formular schon vorher ab.
   */
  await vater.route("**/api/v1/supervisor/children", async (route) => {
    // Nur das Anlegen abfangen – **dieselbe** Adresse liefert per GET die Kinderliste, die die Seite
    // unmittelbar danach wieder braucht.
    if (route.request().method() !== "POST") return route.fallback();
    await route.fulfill({
      status: 409,
      contentType: "application/problem+json",
      body: JSON.stringify({ status: 409, title: "Conflict", detail: "Name schon vergeben" }),
    });
  });
  await vater.locator("#new-child-name").fill(CHILD.name);
  await vater.getByRole("button", { name: "Kind anlegen" }).click();
  await expect(vater.locator(".banner.err")).toContainText("Name schon vergeben");
  // Der Kern des Defekts: dieser Text stand vorher im grünen Kasten.
  await expect(vater.locator(".banner.ok")).toHaveCount(0);
  // Und die Eingabe steht noch da – geleert wird nur bei Erfolg, sonst wäre der zweite Anlauf Abtipparbeit.
  await expect(vater.locator("#new-child-name")).toHaveValue(CHILD.name);
  await vater.unroute("**/api/v1/supervisor/children");

  // ---------- 2. Kind mit PIN anlegen ----------
  // Die PIN ist der Login des Kindes; ohne sie kommt es nicht in seine App (Schritt 9 prüft genau das).
  //
  // Zugleich der Beleg für B-61 AK 4: `addChild` ruft nach dem Anlegen `children.reload()` auf; die
  // GET-Antwort wird hier künstlich verzögert, damit die Prüfung unten nicht zufällig grün ist, weil der
  // Server ohnehin schneller war als der Assert. Ohne den `data === null`-Wächter (VaterDashboard.tsx)
  // ersetzte `{children.loading ? "Lade…" : …}` die schon sichtbare (noch leere) Tabelle kurz durch die
  // Ladeanzeige – strukturell geprüft, nicht zeitkritisch (Entscheidung 7).
  let releaseChildrenReload: (() => void) | undefined;
  const childrenReloadHeld = new Promise<void>((resolve) => { releaseChildrenReload = resolve; });
  await vater.route("**/api/v1/supervisor/children", async (route) => {
    if (route.request().method() !== "GET") return route.fallback();
    await childrenReloadHeld;
    return route.fallback();
  });

  await vater.locator("#new-child-name").fill(CHILD.name);
  await vater.locator("#new-child-grade").fill("5");
  await vater.locator("#new-child-pin").fill(CHILD.pin);
  await vater.getByRole("button", { name: "Kind anlegen" }).click();
  await expect(vater.getByText("Kind angelegt.")).toBeVisible();
  // Die verzögerte Kinderliste läuft noch – die Sektion darf jetzt keine Ladeanzeige zeigen.
  const kinderSection = vater.locator("section", { has: vater.getByRole("heading", { name: "Kinder" }) });
  await expect(kinderSection.locator(".loading")).toHaveCount(0);
  releaseChildrenReload!();
  await vater.unroute("**/api/v1/supervisor/children");

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

  // ---------- 4. Katalog: Fach (Katalog) + Lehrwerk-Reihe/Unit (Lehrwerke) ----------
  // Seit B-106 hängt jede Übung an einer Lehrwerk-Unit statt an einem Kapitel (entfernt).
  await vater.goto("/vater/katalog");
  await vater.getByPlaceholder("z. B. Französisch").fill(SUBJECT);
  await vater.getByRole("button", { name: "Neues Fach anlegen" }).click();
  await expect(vater.locator("#ca-subject")).toHaveValue(/\d+/);

  // ---------- 4a. Ein abgelehntes Anlegen (Namensduplikat) darf den Feldinhalt nicht wegwerfen (B-61) ----------
  // Kapitel sind seit B-106 nach /vater/lehrwerke gewandert; der real noch existierende Dedup-Pfad in
  // CatalogAdmin ist die "Art" (ExerciseCategoriesController.Create lehnt einen zweiten gleichen Namen im
  // selben Fach mit ApiErrors.Conflict ab). Vorher leerte `NewName.onSubmit` das Feld unbedingt (B-61 AK 1/2).
  const ART = `Grammatik ${RUN}`;
  await vater.getByPlaceholder("z. B. Grammatik").fill(ART);
  await vater.getByRole("button", { name: "Neue Art anlegen" }).click();
  await expect(vater.getByText("Art angelegt.")).toBeVisible();

  await vater.getByPlaceholder("z. B. Grammatik").fill(ART);
  await vater.getByRole("button", { name: "Neue Art anlegen" }).click();
  await expect(vater.locator(".banner.err")).toBeVisible();
  // Der Kern des Defekts: der getippte Text stand vorher nicht mehr da, egal ob abgelehnt oder nicht.
  await expect(vater.getByPlaceholder("z. B. Grammatik")).toHaveValue(ART);

  await vater.goto("/vater/lehrwerke");
  await vater.locator("#ns-name").fill(SUBJECT);
  await vater.locator("#ns-subject").selectOption({ label: SUBJECT });
  await vater.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(vater.getByText(/steht im Katalog/)).toBeVisible();
  const seriesRow = vater.getByRole("row", { name: new RegExp(SUBJECT) });
  await seriesRow.getByRole("button", { name: "Units" }).click();
  await vater.locator('[id^="unit-label-new"]').fill(CHAPTER);
  await vater.getByRole("button", { name: "Unit hinzufügen" }).click();
  await expect(vater.getByText(CHAPTER)).toBeVisible();

  // ---------- 4b. Vokabeln und Übung anlegen (eigene Route) ----------
  await vater.goto("/vater/exercises/neu");
  await vater.locator('select[aria-label="Fach"]').selectOption({ label: SUBJECT });
  await vater.locator('select[aria-label="Reihe"]').selectOption({ label: SUBJECT });
  await vater.locator('select[aria-label="Unit"]').selectOption({ label: CHAPTER });

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
  // Bearbeiten ist die Daueraufgabe und liegt in der Verwaltung; „Übungen verwalten" nimmt Fach und
  // Unit als Query mit, damit die Liste ohne erneutes Auswählen dieselbe Unit zeigt.
  await vater.getByRole("link", { name: /Übungen verwalten/ }).first().click();
  await expect(vater).toHaveURL(/\/vater\/exercises\?subjectId=\d+&seriesId=\d+&seriesUnitId=\d+/);
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

  // Titelbild und Rechte hängen an der Übung, nicht an ihrem Inhalt – beide waren bisher nur über die API
  // erreichbar. Wer eine Übung anlegt, bekommt automatisch das Owner-Recht; ohne es könnte er sie an
  // keinen zweiten Vater weitergeben, und die „geteilte Bibliothek" wäre nur Theorie.
  await expect(dialog.getByRole("heading", { name: "Titelbild" })).toBeVisible();
  await expect(dialog.getByRole("heading", { name: /Rechte \(1\)/ })).toBeVisible();
  await expect(dialog.getByRole("row", { name: new RegExp(FATHER.name) })).toContainText("Verwalten");

  // Die übungslokale Bild-Übersteuerung sitzt an der Wortzeile: sie schlägt die Bilder der Vokabel, gilt
  // aber nur in dieser Übung. Eingeklappt, damit 30 Wörter nicht 30 Abfragen auslösen.
  await dialog.getByRole("button", { name: `Bild für ${WORDS[0][0]} nur in dieser Übung` }).click();
  await expect(dialog.getByText("Keine Übersteuerung", { exact: false })).toBeVisible();

  await dialog.getByRole("button", { name: "Schließen" }).click();

  // ---------- 5b. Eine per API angelegte Übung ist im UI vollwertig verwaltbar ----------
  /*
   * Der KI-Creator legt Übungen direkt über die API an (hier: Grammatik). Sie müssen in der Verwaltung
   * ankommen wie eigene: mit dem deutschen Namen aus dem Typ-Manifest und einem Bearbeiten-Weg. Vorher
   * kannte das UI nur sechs der zwölf Server-Typen – eine solche Übung ließ die Seite mit `undefined`
   * abstürzen. Das Anlegen aller Typen *durch das UI* deckt `uebungstypen.spec.ts` ab.
   */
  const token = await vater.evaluate(() => localStorage.getItem("pugling.token"));
  const subjectId = await vater.locator('select[aria-label="Fach"]').inputValue();
  const seriesId = await vater.locator('select[aria-label="Reihe"]').inputValue();
  const seriesUnitId = await vater.locator('select[aria-label="Unit"]').inputValue();
  const created = await vater.request.post(
    `/api/v1/creator/textbook-series/${seriesId}/units/${seriesUnitId}/grammar`,
    {
      headers: { Authorization: `Bearer ${token}` },
      data: {
        title: `Grammatik ${RUN}`, orderIndex: 99, rewardPoints: 5,
        config: { instruction: "Setze ein.", tasks: [{ prompt: "I ___ tired.", answer: "am" }] },
      },
    });
  expect(created.ok(), await created.text()).toBeTruthy();

  // Unit ab- und wieder anwählen statt neu zu laden: ein Reload würde Fach/Unit zurücksetzen und
  // damit die Übungsliste ganz ausblenden.
  const unitSelect = vater.locator('select[aria-label="Unit"]');
  await unitSelect.selectOption("");
  await unitSelect.selectOption(seriesUnitId);
  const grammarRow = vater.locator("div", { hasText: `Grammatik ${RUN}` }).last();
  // Der Name kommt aus dem Manifest, nicht aus einer Tabelle im Frontend – und geprüft wird jetzt
  // „Regelaufgaben", ein Wort, das der TITEL nicht enthält. Vorher stand hier `toContainText("Grammatik")`,
  // und der Titel „Grammatik <RUN>" erfüllte die Zusicherung schon: sie konnte nicht fehlschlagen (B-163).
  await expect(grammarRow).toContainText("Regelaufgaben");
  await expect(grammarRow.getByRole("button", { name: /Bearbeiten/ })).toBeVisible();
  // Und die zuvor im UI angelegte Vokabelübung steht weiter daneben – die Liste lebt.
  // `exact`, weil der Titel auch in der Erfolgsmeldung darüber steht.
  await expect(vater.getByText(EXERCISE, { exact: true })).toBeVisible();

  // ---------- 5c. Lehrwerk korrigieren: Unit umbenennen ----------
  /*
   * Reihen und Units sind **globaler** Katalog – ein Tippfehler stand für alle Väter da und war nur über
   * die API zu heilen. Die Verwaltung liegt seit B-106 unter Lehrwerke; die Gegenprobe wandert damit zurück
   * auf die Übungen-Seite: **deren** Unit-Pulldown muss die Umbenennung mitbekommen, sonst arbeitet der
   * Vater weiter mit einem Namen, den es nicht mehr gibt.
   */
  await vater.goto("/vater/lehrwerke");
  const lehrwerkRow = vater.getByRole("row", { name: new RegExp(SUBJECT) });
  await lehrwerkRow.getByRole("button", { name: "Units" }).click();
  await vater.getByRole("button", { name: `${CHAPTER} bearbeiten` }).click();
  const unitLabelField = vater.locator('input[id^="unit-label-u"]');
  await expect(unitLabelField).toHaveValue(CHAPTER);
  await unitLabelField.fill(`${CHAPTER} korrigiert`);
  await vater.getByRole("button", { name: "Speichern" }).click();
  // Kein Warten auf eine Erfolgsmeldung: das Editor-Panel schließt sich nach dem Speichern selbst
  // (`onDone` setzt `editing` zurück) und nimmt seinen eigenen `StatusBanner` dabei mit. Die Gegenprobe ist
  // darum die persistierte Zeile in der (weiter offenen) Unit-Tabelle, nicht ein flüchtiger Banner.
  await expect(vater.getByText(`${CHAPTER} korrigiert`)).toBeVisible();

  await vater.goto("/vater/exercises");
  await vater.locator('select[aria-label="Fach"]').selectOption(subjectId);
  await vater.locator('select[aria-label="Reihe"]').selectOption(seriesId);
  await expect(vater.locator('select[aria-label="Unit"]')).toContainText(`${CHAPTER} korrigiert`);

  // ---------- 5d. Kind-Hub: Betreuung und Stundenplan ----------
  await vater.goto(`/vater/kind/${childId}`);
  // Beim Anlegen wird der Vater als Betreuer verknüpft. Der **letzte** Betreuer lässt sich nicht entziehen –
  // das Kind wäre für niemanden mehr erreichbar –, darum steht in seiner Zeile kein Entfernen-Knopf.
  const supervisorRow = vater.getByRole("row", { name: new RegExp(FATHER.name) });
  await expect(supervisorRow).toContainText("Vater");
  await expect(supervisorRow.getByRole("button", { name: /Entfernen/ })).toHaveCount(0);

  // Der Stundenplan ist Profilwissen, kein Lehrplan: er sagt, worauf heute der Schwerpunkt liegt.
  await vater.locator("#tt-subject").selectOption({ label: SUBJECT });
  await vater.locator("#tt-day").selectOption("Tuesday");
  await vater.locator("#tt-time").fill("1. Stunde");
  await vater.getByRole("button", { name: "Eintragen" }).click();
  await expect(vater.locator(".token", { hasText: SUBJECT })).toContainText("1. Stunde");
  // Über den Tages-Kopf, nicht über den Text: „Dienstag" steht auch als Option im Wochentag-Pulldown.
  await expect(vater.locator("span").filter({ hasText: "Dienstag" })).toBeVisible();

  // ---------- 5e. Lückentext-Store: ein Trägertext als Lerngrundlage ----------
  /*
   * Trägertexte sind wie der Vokabel-Store *unabhängig* von einer einzelnen Übung – ohne diese
   * Oberfläche musste der Vater denselben Satz in jede Übung neu tippen. Die Lücken hängen über den
   * Platzhalter am Text, darum entsteht die Lösungszeile erst, wenn der Text steht.
   */
  await vater.goto("/vater/lueckentexte");
  await vater.locator("#cz-key").fill(`cz-e2e-${RUN}`);
  await vater.locator("#cz-title").fill(`Begrüßungen ${RUN}`);
  await vater.locator("#cz-text").fill("Good {{1}}, how {{2}} you?");
  await vater.locator("#cz-text").blur();
  await vater.getByRole("textbox", { name: "Lösung für Lücke 1" }).fill("morning");
  await vater.getByRole("textbox", { name: "Lösung für Lücke 2" }).fill("are");
  await vater.getByRole("button", { name: "Trägertext anlegen" }).click();
  await expect(vater.getByText(`Trägertext „Begrüßungen ${RUN}" angelegt.`)).toBeVisible();

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
  // `exact`, weil neben dem Feld ein „ⓘ" mit dem Namen „Erklärung zu „Artikelnummer"" steht – ohne
  // das Flag greift der Teilstring-Vergleich auch den Hinweis-Knopf ab.
  await vater.getByLabel("Artikelnummer", { exact: true }).fill(`TV-${RUN}`);
  await vater.getByLabel("Titel", { exact: true }).fill(`Fernsehzeit ${RUN}`);
  await vater.getByRole("button", { name: "Anlegen" }).click();
  await expect(vater.getByText(`Artikel „Fernsehzeit ${RUN}" angelegt.`)).toBeVisible();

  await vater.getByRole("row", { name: new RegExp(`Fernsehzeit ${RUN}`) })
    .getByRole("button", { name: /Angebote/ }).click();
  await vater.getByRole("button", { name: "Angebot anlegen" }).click();
  await expect(vater.getByText("Angebot angelegt.")).toBeVisible();

  // ---------- 8. Großes Ziel (OKR) – der Sohn muss es in Schritt 9 sehen ----------
  // Ziele setzt der Vater, das Kind sieht nur, woran es ist: die Sohn-Sicht ist bewusst rein lesend.
  await vater.goto(`/vater/kind/${childId}/ziele`);
  // `exact`, sonst trifft „Ziel anlegen" auch „+ Großes Ziel anlegen" (Playwright matcht den Namen als
  // Teilzeichenkette).
  await vater.getByRole("button", { name: "+ Großes Ziel anlegen", exact: true }).click();
  await vater.locator("#ob-title").fill(`Englisch aufholen ${RUN}`);
  await vater.getByRole("button", { name: "Ziel anlegen", exact: true }).click();
  await expect(vater.getByText("Großes Ziel angelegt.")).toBeVisible();

  // ---------- 8a. Etappe (Key Result) am großen Ziel ----------
  /*
   * Ein Ziel ohne Etappe nennt die Oberfläche selbst unerreichbar („Noch keine Etappen – ohne sie kann das
   * Ziel nicht erreicht werden"). Die Etappe trägt den Scope-Wähler, die Messlatte und den Status –
   * **dieselbe Fläche**, die bis zum 2026-08-01 hier am *Lernziel* geprüft wurde. Die Lernziel-Ebene ist
   * gelöscht (DB-Umbau E13), das Key Result hat ihre Rolle geerbt; der Abschnitt wurde darum umgeschrieben
   * statt gestrichen, sonst wäre mit dem Rot auch die Abdeckung weg.
   *
   * Nicht abgedeckt und bewusst offen: die Gegenrichtung „höchstens" (siehe unten, warum sie hier nicht
   * prüfbar ist).
   */
  // Alles Weitere **innerhalb der Karte dieses Ziels**: der Scope-Wähler und das Etappen-Formular stehen
  // je Karte einmal und zusätzlich im Anlege-Formular. Ohne die Eingrenzung hinge die Strecke daran, dass
  // es zufällig nur ein Ziel gibt – ein zweites machte jeden Label-Zugriff hier zur Strict-Mode-Verletzung.
  const ziel = vater.locator(".card").filter({ hasText: `Englisch aufholen ${RUN}` });
  await expect(ziel).toContainText("Noch keine Etappen");

  await ziel.getByRole("button", { name: "+ Etappe", exact: true }).click();
  // Über das Label statt eine feste Id: der Scope-Wähler vergibt seine Ids je Instanz (`useId`).
  await ziel.getByLabel("Fach", { exact: true }).selectOption({ label: SUBJECT });
  // Die Messlatte ausdrücklich wählen und einen **Zielwert abseits der Vorbelegung** (80) setzen: sonst
  // bewiese die Zeile unten nur, dass das Formular vorbelegt ist, nicht dass die Eingabe im POST landet.
  // `exact` bei jedem Label, sonst greift der Teilstring-Vergleich den „ⓘ"-Knopf daneben ab.
  //
  // Eine Obergrenz-Messlatte (`MaxWeakItems`) wäre die reizvollere Wahl – sie deckte die Gegenrichtung
  // „höchstens" ab. Sie taugt hier aber nicht: ein Kind ohne jeden Lernstand hat **null** schwache Wörter
  // und erfüllt jede Obergrenze sofort. Die Etappe stünde auf „erreicht/100 %", und der Status wäre
  // trivial statt geprüft.
  await ziel.getByLabel("Messlatte", { exact: true }).selectOption("AvgMastery");
  await ziel.getByLabel("Zielwert", { exact: true }).fill("90");
  await ziel.getByLabel("Titel (optional)", { exact: true }).fill("Unit 1 sitzt");
  /*
   * **Doppelklick** statt Klick (B-54, AK 4). Bis zum 2026-08-01 ging die Karte am Schreib-Primitiv vorbei:
   * kein Ref-Gate, kein `disabled` – und das Formular schließt erst *nach* dem `await`, also liefen beide
   * Klicks los.
   *
   * Gezählt werden die **abgeschickten POSTs**, nicht die entstandenen Etappen. Das ist nicht dasselbe, und
   * der Unterschied ist am 2026-08-01 gemessen worden: mit dem alten Stand gingen **zwei** POSTs hinaus, und
   * trotzdem stand danach „0/1 Etappen" – der zweite Schreibvorgang verlor serverseitig das Rennen um die
   * SQLite-Schreibsperre. Eine Zusicherung über die Etappen-Zahl wäre also **vor und nach** der Reparatur
   * grün gewesen und hätte nichts bewiesen. Der Doppel-POST ist der Defekt; ihn zu zählen ist die Prüfung.
   *
   * Was den zweiten Klick hier abfängt, ist übrigens vor allem das `disabled`: React schreibt `busy` im
   * `await`-Yield zwischen den beiden Klicks an den Knopf, und ein gesperrter Knopf feuert kein Ereignis.
   * Das Ref-Gate ist das Netz für den Fall, dass beide Klicks *denselben* Tick treffen – geprüft wird es
   * dort, wo es sitzt (`useAction.test.tsx`), nicht hier.
   */
  const krPosts: string[] = [];
  vater.on("request", (r) => {
    if (r.method() === "POST" && r.url().includes("/key-results")) krPosts.push(r.url());
  });
  await ziel.getByRole("button", { name: "Etappe übernehmen" }).dblclick();
  // AK 3: die Erfolgsmeldung selbst. Vorher gab es keine – ein fehlgeschlagener Aufruf äußerte sich als
  // Timeout auf die Zeile unten statt als lesbare Meldung. Sie ist zugleich der Wartepunkt für die
  // POST-Zählung darunter: erst wenn sie steht, ist der Durchgang durch.
  await expect(ziel.getByText("Etappe angelegt.")).toBeVisible();
  // AK 4. Gemessen **nach** der Meldung, damit ein verspäteter zweiter POST noch mitgezählt würde.
  expect(krPosts, "Ein Doppelklick darf genau einen Schreibvorgang auslösen").toHaveLength(1);

  const krRow = ziel.getByRole("row", { name: /Unit 1 sitzt/ });
  // Bereich: der Server liefert die *Ebene* („Subject", seit B-59 ein Enum-Wert), nicht den Fachnamen –
  // ein Fach ohne Unit und ohne Übung muss dort landen, sonst hat der Scope-Wähler still eingeengt.
  await expect(krRow).toContainText("Subject");
  // Richtung **und** der eingetippte Wert – „mindestens 80 %" wäre die durchgereichte Vorbelegung.
  await expect(krRow).toContainText("mindestens 90 %");
  // Ohne jeden Lernstand ist die Etappe offen – der Status wird bei jeder Abfrage neu gerechnet.
  await expect(krRow).toContainText("offen");
  // Und die Karte hat die Etappe übernommen: ohne sie stünde hier weiter „0/0".
  await expect(ziel).toContainText("0/1 Etappen");

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

  // Und auf dem Trophäenweg steht das große Ziel des Vaters – vorher war es nur über die API sichtbar.
  await sohn.getByRole("link", { name: /Weg/ }).click();
  await expect(sohn.getByText("🎯 Meine großen Ziele")).toBeVisible();
  await expect(sohn.getByText(`Englisch aufholen ${RUN}`)).toBeVisible();

  // ---------- 10. Der Vater kann sich mit seiner Id wieder anmelden ----------
  const wieder = await (await browser.newContext()).newPage();
  await wieder.goto("/vater");
  await wieder.locator("#fid").fill(fatherId);
  await wieder.locator("#pin").fill(FATHER.pin);
  await wieder.getByRole("button", { name: "Anmelden" }).click();
  await expect(wieder.getByRole("heading", { name: "Kinder" })).toBeVisible();
  await expect(wieder.getByRole("link", { name: CHILD.name })).toBeVisible();
});
