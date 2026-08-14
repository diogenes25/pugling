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

/*
 * Bis auf Schritt 3 vorrücken, mit **namentlich gewähltem** Kind. Die Vorbelegung zu erben wäre eine
 * Auswahl per Position (die Liste kommt `OrderBy(Name)`, und der Durchstich oben legt ein „E2E-Assistent-…"
 * an, das vor „Sohn" sortiert) – und sie ist hier tragend, weil Klasse und Schulart des Kindes den Katalog
 * SERVERSEITIG filtern: „Begrüßungen" ist Klasse 5–6, „Vocabulary: The environment" 8–10. Ein Kind mit
 * gesetzter Klasse ließe je eine der beiden Zeilen verschwinden, und der Fall stürbe an „element not found"
 * – an einer Stelle, die die Ursache nicht nennt.
 */
async function waehleSohnUndEnglisch(page: Page) {
  await page.getByRole("button", { name: "Bestehendes Kind" }).click();
  await page.getByRole("combobox", { name: "Kind" }).selectOption("1");
  await page.getByRole("button", { name: "Weiter" }).click();

  await expect(page.getByRole("heading", { name: /Wo hakt es/ })).toBeVisible();
  await page.getByLabel("Fach", { exact: true }).selectOption({ label: "Englisch" });
  await page.getByRole("button", { name: "Weiter" }).click();
  await expect(page.getByRole("heading", { name: /Übungen wählen/ })).toBeVisible();
}

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
  await page.getByLabel("Fach", { exact: true }).selectOption({ label: "Englisch" });
  await page.getByRole("button", { name: /Regelmäßig üben/ }).click();
  await page.getByRole("button", { name: /^Normal/ }).click();
  await page.getByRole("button", { name: "Weiter" }).click();

  // ---------- Schritt 3: Übungen (auf eine einzelne, seed-stabile Übung gefiltert) ----------
  await page.getByRole("heading", { name: /Übungen wählen/ }).waitFor();
  await page.getByLabel("Übung suchen", { exact: true }).fill("environment");
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
  // `[1-9]\d*` statt `\d+` (B-171): Die Ueberschrift heisst ab dem ersten Render "Übungen wählen (0 gewählt)"
  // – die Zahl steht unbedingt da. `\d+` traf also die Null, war im Ausgangszustand schon wahr und wartete
  // auf NICHTS, waehrend dieser Kommentar das Warten als seinen Zweck nennt. Gemessen mit weggelassenem
  // "Alle waehlen"-Klick: die alte Fassung blieb GRUEN und der Fall starb erst 10 s spaeter an
  // `/Feinschliff/` – an einer Stelle, die die Ursache nicht nennt. Die neue faellt hier, mit Grund.
  await expect(page.getByRole("heading", { name: /\([1-9]\d* gewählt/ })).toBeVisible();
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

/*
 * B-169: Das Ladefenster. `useAsync` behält seine Daten über einen Kriterienwechsel hinweg (B-116, und das
 * ist richtig – sonst hängen aufgeklappte Bereiche aus). Damit stehen für die Dauer der neuen Abfrage die
 * Zeilen des *vorigen* Filters da. Waren sie anklickbar, entstand eine Auswahl, die zu einem Filter gehört,
 * der nicht mehr gilt – und die der Effekt nie wieder leert, weil sich der Schlüssel nicht erneut ändert.
 *
 * Eigener `test()` statt eines Blocks im Durchstich oben: dieselbe Fläche, aber ein Rot soll sagen, ob das
 * Ladefenster offen ist oder der Assistent insgesamt bricht.
 *
 * Die Verzögerung kommt per `route`, nicht per gefälschter Antwort: der echte Server antwortet, nur später
 * (Präzedenz `vater-von-null.spec.ts:57`). Ein Komponententest wäre hier konventionswidrig – der
 * nachgebaute Bildschirm mit gefälschtem `fetch` ist ausgeschlossen (frontend/CLAUDE.md).
 */
test("Solange die Trefferliste zum vorigen Filter gehört, ist kein Kästchen anklickbar", async ({ page }) => {
  await vaterLogin(page);
  await page.goto("/vater/wizard");

  await waehleSohnUndEnglisch(page);

  // Der Ausgangszustand, den die Zusicherung unten NICHT prüfen darf: eine geladene Liste mit bedienbarem
  // Kästchen. Ohne diese Zeile wäre „gesperrt" auch dann wahr, wenn die Liste gar nicht da ist.
  const zeile = page.getByRole("checkbox", { name: /Begrüßungen/ });
  await expect(zeile).toBeEnabled();

  // Ab jetzt antwortet die Suche erst nach 2 s – lange genug, um im Fenster zu klicken.
  await page.route("**/api/v1/creator/exercises?**", async (route) => {
    await new Promise((r) => setTimeout(r, 2000));
    await route.fallback();
  });

  // Der Kriterienwechsel. Der Effekt leert die Auswahl und setzt den Schlüssel weiter; die alte Zeile steht
  // noch, weil `data` erst mit der Antwort wechselt.
  await page.getByLabel("Übung suchen", { exact: true }).fill("environment");

  // Die eigentliche Aussage: die alte Zeile ist noch sichtbar UND gesperrt. Die Frist ist bewusst kürzer
  // als die Verzögerung – sonst pollt die Zusicherung, bis die Antwort da und die alte Zeile weg ist, und
  // meldet dann „element(s) not found" statt des eigentlichen Befunds „war bedienbar".
  await expect(zeile).toBeVisible();
  await expect(zeile).toBeDisabled({ timeout: 1200 });

  // Und die Sperre ist ein Fenster, kein Dauerzustand (AK 2): nach der Antwort ist die neue Zeile bedienbar.
  await expect(page.getByRole("checkbox", { name: /The environment/ })).toBeEnabled({ timeout: 10_000 });
});

/*
 * B-169, zweite Tür — vom `frontend-reviewer` gefunden, nachdem die erste geschlossen war. Im FEHLERZWEIG
 * von `useAsync` gilt die Begründung des Kästchen-Gates nicht: `data` bleibt stehen, `loading` fällt auf
 * `false`, und der Effekt auf `[exercises.data]` läuft nie (die Referenz ändert sich nicht). Das Kästchen ist
 * damit korrekt gesperrt — „Alle wählen" aber nicht, weil sein `exercises.loading` schon wieder falsch ist.
 * Ein Klick wählt dann die Ids der veralteten Liste, `unsichtbar` vergleicht gegen genau diese Liste und
 * meldet 0, und „Weiter" lässt durch: derselbe P1-Schaden, nur über den Nachbarknopf.
 */
test("Scheitert die Suche, ist auch der Knopf »Alle wählen« gesperrt", async ({ page }) => {
  await vaterLogin(page);
  await page.goto("/vater/wizard");
  await waehleSohnUndEnglisch(page);

  const alleWaehlen = page.getByRole("button", { name: "Alle wählen" });
  await expect(alleWaehlen).toBeEnabled();

  // Ab jetzt scheitert die Suche. `data` bleibt die alte Seite, `loading` fällt zurück – und ohne das Gate
  // wäre der Knopf sofort wieder bedienbar.
  await page.route("**/api/v1/creator/exercises?**", (route) =>
    route.fulfill({ status: 500, contentType: "application/problem+json",
      body: JSON.stringify({ status: 500, title: "Server error", detail: "B-169 Probe" }) }));

  await page.getByLabel("Übung suchen", { exact: true }).fill("environment");

  // Die alten Zeilen stehen noch (das ist die B-116-Regel, gewollt) – und beides ist gesperrt.
  await expect(page.getByRole("checkbox", { name: /Begrüßungen/ })).toBeDisabled();
  await expect(alleWaehlen).toBeDisabled();
});
