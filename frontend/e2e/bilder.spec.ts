import { test, expect, type Page } from "@playwright/test";
import { createChild, lernkarteDurchspielen, pfadStart, alsVaterStarten } from "./helpers";

// End-to-End der individualisierten Bebilderung:
//   Vater legt zwei Darstellungen desselben Motivs an  →  ordnet beide der Vokabel „la ville" zu
//   →  pflegt ein Interesse am Kind  →  Sohn übt und sieht EIN Bild  →  „anderes Bild" tauscht es aus.
//
// Der Kern, den dieser Test absichert, ist nicht „ein <img> ist da", sondern die Kette dahinter: der Vorrat
// hat mehrere Bilder, der Server wählt eines aus, und das Kind kann es wechseln, ohne dass ein zweites
// auftaucht. Genutzt wird die geseedete Übung „Vokabeln: En ville" (fr→de) wie im Haupt-Flow.
//
// Die Bilder werden als winzige PNGs hochgeladen – kein Netzzugriff, keine Fremdquelle, und der Test
// deckt zugleich die serverseitige Varianten-Erzeugung samt Auslieferung unter /media/ ab.

const FATHER = { id: "1", pin: "0000" };
const EXERCISE = "Vokabeln: En ville";
const WORD = "la ville";
// Eigenes Kind statt des geseedeten Sohns: beide Specs teilen sich eine Backend-Instanz, und pro Kind ist
// nur ein aktiver Plan spielbar (Anti-Cheat). Ohne eigenes Kind nähme dieser Test dem Haupt-Flow den Plan weg.
const CHILD = { name: "Bild-Kind", pin: "4321" };

/** Ein 1×1-Pixel-PNG in Wunschfarbe – echte, dekodierbare Bytes ohne externe Abhängigkeit. */
function pixel(color: "red" | "blue"): string {
  const png = color === "red"
    ? "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="
    : "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";
  return `data:image/png;base64,${png}`;
}

/**
 * Das **verknüpfte** Bild einer Vokabel, eingeschränkt über den „Entfernen"-Knopf seiner eigenen Kachel.
 *
 * Ohne die Einschränkung trifft der Bezug **zwei** Kacheln: Dasselbe Motiv steht hier als Verknüpfung und
 * als Treffer der Bibliothekssuche, der „schon dabei" meldet. Ob daraus eine `strict mode violation` wurde,
 * entschied allein, ob die Verknüpfungsliste beim ersten Poll schon nachgeladen war — darum flackerte der
 * Fall über drei volle Läufe (B-153). `AssetThumb` gibt dem Knopf den Namen „<Beschriftung>: <Beschreibung>";
 * der unterscheidet die beiden Kacheln.
 *
 * `exact` an beiden Bezügen: `getByRole(…, { name })` vergleicht als **Teilstring**. Eine dritte
 * Beschreibung, die eine bestehende enthält, machte die Einschränkung sonst lautlos wieder zweideutig —
 * mit derselben Meldung, und diesmal sähe die Stelle sogar repariert aus.
 */
const verknuepftesBild = (vater: Page, description: string) =>
  vater.locator("figure")
    .filter({ has: vater.getByRole("button", { name: `Entfernen: ${description}`, exact: true }) })
    .getByRole("img", { name: description, exact: true });

async function vaterLogin(page: Page) {
  await page.goto("/vater");
  await page.locator("#fid").fill(FATHER.id);
  await page.locator("#pin").fill(FATHER.pin);
  await page.getByRole("button", { name: "Anmelden" }).click();
  await alsVaterStarten(page);
}

async function sohnLogin(page: Page, childId: string) {
  await page.goto("/sohn");
  await page.locator("#childId").fill(childId);
  for (const d of CHILD.pin.split("")) {
    await page.locator(".keys button", { hasText: new RegExp(`^${d}$`) }).first().click();
  }
  await page.getByRole("button", { name: "▶ LOS" }).click();
}

/**
 * Legt über die Bild-Bibliothek eine Darstellung an – per <b>echtem Datei-Upload</b>. Damit läuft im Test
 * die ganze Kette: multipart → Dekodieren → Skalieren → Ablage → Auslieferung unter <c>/media/…</c>.
 * Die Beschreibung ist zugleich Alt-Text und Suchbegriff.
 */
async function uploadImage(vater: Page, description: string, color: "red" | "blue", tags: string) {
  await vater.goto("/vater/media");
  await expect(vater.getByRole("heading", { name: "Bild hinzufügen" })).toBeVisible();
  await vater.locator("#m-desc").fill(description);
  await vater.locator("#m-file").setInputFiles({
    name: `${color}.png`,
    mimeType: "image/png",
    buffer: Buffer.from(pixel(color).split(",")[1], "base64"),
  });
  await vater.locator("#m-tags").fill(tags);
  await vater.getByRole("button", { name: "Anlegen" }).click();
  await expect(vater.getByText("Bild hochgeladen")).toBeVisible();
  await expect(vater.getByRole("img", { name: description })).toBeVisible();
}

test("Vater bebildert eine Vokabel, der Sohn sieht sein Bild und kann es wechseln", async ({ browser }) => {
  const vaterCtx = await browser.newContext();
  const vater = await vaterCtx.newPage();
  await vaterLogin(vater);
  const childId = await createChild(vater, CHILD.name, CHILD.pin);

  // ---------- Vorrat: zwei Darstellungen desselben Motivs ----------
  // Erst dadurch gibt es überhaupt etwas auszuwählen – ein einzelnes Bild sähe jedes Kind gleich.
  await uploadImage(vater, "Stadt als Comic-Zeichnung", "red", "Comic");
  await uploadImage(vater, "Stadt als Foto", "blue", "Foto");

  // ---------- Zuordnung an der Store-Vokabel (gilt in jeder Übung mit diesem Wort) ----------
  await vater.goto("/vater/vocab");
  await vater.getByRole("combobox", { name: "Quellsprache" }).selectOption("fr");
  await vater.getByRole("combobox", { name: "Zielsprache" }).selectOption("de");
  await vater.getByRole("textbox", { name: "Vokabel suchen" }).fill(WORD);

  // Über die Wort-ZELLE, exakt, nicht über den Zeilennamen: der ist der ganze Zeilentext, und der
  // Server-Filter sucht `Contains` in einer zwischen den Specs geteilten DB. Eine zweite Vokabel wie
  // „la ville nouvelle" träfe ein Teilstring-Muster mit, und `.first()` griffe still eine davon (B-214).
  // `toHaveCount(1)` sagt es laut, statt die Bilder an die falsche Vokabel zu hängen.
  const row = vater.getByRole("row").filter({ has: vater.getByRole("cell", { name: WORD, exact: true }) });
  await expect(row).toHaveCount(1);
  await row.getByRole("button", { name: /Bilder/ }).click();

  /*
   * Der Tag-Editor derselben Zeile – bis B-54 hatte er **keinen** Test, obwohl vier Schreibpfade darin
   * hängen. Er steht hier und nicht in einer eigenen Strecke, weil die Store-Zeile schon offen ist; ein
   * zweiter Anlauf mit Anmeldung, Bild und Vokabel kostete eine halbe Minute für zwei Klicks.
   *
   * Erfolg bleibt hier **absichtlich stumm** (Entscheidung 2 der Story): geprüft wird der Chip, nicht ein
   * Banner. Bei mehreren Tags hintereinander wäre eine Meldung je Klick Lärm.
   */
  await row.getByRole("button", { name: /Tags/ }).click();
  await vater.getByRole("textbox", { name: "+ globaler Tag" }).fill("e2e-thema");
  // Der Knopf heißt nach seiner Eingabe: derselbe Baustein steht dreimal auf der Seite (Filter, globale
  // Tags, Kind-Tags), und mit einem gemeinsamen Namen wäre nur die Position unterscheidbar.
  await vater.getByRole("button", { name: "globaler Tag hinzufügen" }).click();
  // Über den Entfernen-Knopf und nicht über den Text: derselbe Tag steht danach zweimal auf der Seite –
  // als Chip in der Tag-Spalte der Zeile und als *entfernbarer* Chip im Editor. Nur letzterer ist gemeint.
  const chip = vater.getByRole("button", { name: "Tag e2e-thema entfernen" });
  await expect(chip).toBeVisible();
  await chip.click();
  await expect(chip).toHaveCount(0);
  // Zuklappen, damit der Bild-Abschnitt darunter eindeutig bleibt.
  await row.getByRole("button", { name: /Tags/ }).click();

  const panel = vater.getByRole("heading", { name: new RegExp(`Bilder für .${WORD}`) });
  await expect(panel).toBeVisible();
  // Die Gegenprobe unten zählt die Vorkommen **dieses** Motivs und hängt darum daran, dass zuletzt danach
  // gesucht wurde – die Treffer der Suche bleiben stehen. Als Konstante, damit die Kopplung sichtbar ist
  // und ein Umsortieren der Liste nicht ein Rot erzeugt, das wie der bewachte Defekt aussieht.
  const ZULETZT_SICHTBAR = "Stadt als Foto";
  for (const description of ["Stadt als Comic-Zeichnung", ZULETZT_SICHTBAR]) {
    await vater.getByRole("textbox", { name: "Bild suchen" }).fill(description);
    await vater.getByRole("button", { name: "Suchen" }).click();
    // Über den vollen Knopfnamen statt `.first()`: Welcher Treffer der erste ist, sagt niemand zu, und
    // ein Bezug per Position bleibt auch dann grün, wenn er das Falsche trifft.
    await vater.getByRole("button", { name: `+ Zuordnen: ${description}`, exact: true }).click();
    await expect(verknuepftesBild(vater, description)).toBeVisible();
  }

  /*
   * Die Gegenprobe zu B-153: Sie macht die Zweideutigkeit, die den Fall dreimal hat flackern lassen, zu
   * einer **festen** Aussage. Ohne sie bliebe die Einschränkung oben eine Behauptung, die nur so lange
   * hält, wie niemand den Bezug wieder aufweicht.
   */
  await expect(vater.getByRole("img", { name: ZULETZT_SICHTBAR, exact: true })).toHaveCount(2);
  /*
   * Und die zweite Kachel ist kein Zufall, sondern gewolltes Verhalten: Der Picker kennzeichnet, was schon
   * verknüpft ist, und sperrt es (`MediaPickers.tsx`) – sonst ordnete ein zweiter Klick dasselbe Bild noch
   * einmal zu. Bewusst diese Aussage und nicht ein `toHaveCount(1)` auf den eingeschränkten Bezug: Das
   * könnte nie vor der Zusicherung in der Schleife fallen, weil `toBeVisible` bereits strikt ist.
   */
  await expect(vater.getByRole("button", { name: `schon dabei: ${ZULETZT_SICHTBAR}`, exact: true }))
    .toBeDisabled();

  // ---------- Profil des Kindes: das Interesse steuert, welche Darstellung gewinnt ----------
  await vater.goto(`/vater/kind/${childId}?bereich=interessen`);
  await expect(vater.getByRole("heading", { name: "Interessen" })).toBeVisible();
  await vater.locator("#int-label").fill("Comic");
  await vater.locator("#int-facet").selectOption("Style");
  // `exact`, weil der Kind-Hub inzwischen mehrere Hinzufügen-Knöpfe trägt (Betreuer, Stundenplan) und
  // Playwright den Namen sonst als Teilzeichenkette matcht.
  await vater.getByRole("button", { name: "Hinzufügen", exact: true }).click();
  await vater.getByRole("button", { name: "Interessen speichern" }).click();
  await expect(vater.getByText("Interessen gespeichert.")).toBeVisible();

  // ---------- Plan mit der Übung, damit der Sohn sie spielen kann ----------
  // „Neuer Plan" ist eine Aktion am Bestand und liegt in der Perspektive „Zuweisen" – von der Kind-Seite
  // aus also erst dorthin.
  await vater.goto("/vater/plaene");
  await vater.getByRole("link", { name: /Neuer Plan/ }).click();
  const kindSelect = vater.getByRole("combobox", { name: "Kind" });
  await expect(kindSelect.locator("option")).not.toHaveCount(0);
  await kindSelect.selectOption(childId);
  await vater.getByRole("combobox", { name: "Fach (Pflicht)" }).selectOption({ label: "Englisch" });
  await vater.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(vater).toHaveURL(/\/vater\/plan\/\d+$/);

  const exRadio = vater.getByRole("radio", { name: new RegExp(EXERCISE) });
  await expect(exRadio).toBeVisible();
  await exRadio.check();
  await vater.locator('select[aria-label="Ziel-Rhythmus"]').selectOption("Daily");
  await vater.getByRole("checkbox", { name: /Leitner/ }).check();
  await vater.getByRole("button", { name: /Position hinzufügen/ }).click();
  await expect(vater.getByRole("row", { name: new RegExp(EXERCISE) })).toBeVisible();

  // ---------- SOHN: das Bild ist da ----------
  const sohnCtx = await browser.newContext();
  const sohn = await sohnCtx.newPage();
  await sohnLogin(sohn, childId);
  await pfadStart(sohn, "practice");
  await expect(sohn.locator(".pill.cyan", { hasText: /Karte \d+ \/ \d+/ })).toBeVisible();

  // Die bebilderte Vokabel suchen: der Kartensatz ist eingefroren, „la ville" kann irgendwo stehen. Die Position ist
  // frisch, also ist jede Karte eine Lernkarte (B-419, B-598) – weiter geht es über ihre vier Schritte. Die Lernkarte ist
  // nicht getippt, sie zeigt ihr Bild; der Bildwechsel unten wird damit auf einer Lernkarte geprüft (B-439).
  const image = sohn.locator(".fcard img");
  const counter = sohn.locator(".pill.cyan", { hasText: /Karte \d+ \/ \d+/ });
  for (let i = 0; i < 12 && !(await image.count()); i++) {
    const before = await counter.textContent();
    await lernkarteDurchspielen(sohn);
    // Erst die nächste Karte abwarten – sonst zählt die Schleife noch die alte und überspringt die gesuchte.
    await expect(counter).not.toHaveText(before ?? "");
  }
  await expect(image).toBeVisible();

  // Der Alt-Text kommt vom Server – er ist die Beschreibung des gewählten Assets, nicht irgendein Text.
  const alt = await image.getAttribute("alt");
  expect(["Stadt als Comic-Zeichnung", "Stadt als Foto"]).toContain(alt);
  const before = await image.getAttribute("src");

  // ---------- „anderes Bild": tauscht aus, das abgelehnte kommt nicht zurück ----------
  await sohn.getByRole("button", { name: /anderes Bild/ }).click();
  await expect(image).not.toHaveAttribute("src", before!);
  const after = await image.getAttribute("alt");
  expect(after).not.toBe(alt);

  // Zwei Darstellungen, eine abgelehnt → kein Vorrat mehr. Der Server sagt das, statt das Bild zu entfernen.
  await sohn.getByRole("button", { name: /anderes Bild/ }).click();
  await expect(sohn.getByText(/Mehr Bilder gibt es dafür nicht/)).toBeVisible();
  await expect(image).toBeVisible();

  await vaterCtx.close();
  await sohnCtx.close();
});
