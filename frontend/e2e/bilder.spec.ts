import { test, expect, type Page } from "@playwright/test";

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

async function vaterLogin(page: Page) {
  await page.goto("/vater");
  await page.locator("#fid").fill(FATHER.id);
  await page.locator("#pin").fill(FATHER.pin);
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();
}

async function sohnLogin(page: Page, childId: string) {
  await page.goto("/sohn");
  await page.locator("#childId").fill(childId);
  for (const d of CHILD.pin.split("")) {
    await page.locator(".keys button", { hasText: new RegExp(`^${d}$`) }).first().click();
  }
  await page.getByRole("button", { name: "▶ LOS" }).click();
}

/** Legt ein frisches Kind an und liefert dessen Id (erste Spalte der Kinder-Tabelle). */
async function createChild(vater: Page): Promise<string> {
  await vater.goto("/vater");
  await vater.locator("#new-child-name").fill(CHILD.name);
  await vater.locator("#new-child-pin").fill(CHILD.pin);
  await vater.getByRole("button", { name: "Kind anlegen" }).click();
  const row = vater.getByRole("row", { name: new RegExp(CHILD.name) });
  await expect(row).toBeVisible();
  return (await row.getByRole("cell").first().textContent())!.trim();
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
  const childId = await createChild(vater);

  // ---------- Vorrat: zwei Darstellungen desselben Motivs ----------
  // Erst dadurch gibt es überhaupt etwas auszuwählen – ein einzelnes Bild sähe jedes Kind gleich.
  await uploadImage(vater, "Stadt als Comic-Zeichnung", "red", "Comic");
  await uploadImage(vater, "Stadt als Foto", "blue", "Foto");

  // ---------- Zuordnung an der Store-Vokabel (gilt in jeder Übung mit diesem Wort) ----------
  await vater.goto("/vater/vocab");
  await vater.getByRole("combobox", { name: "Quellsprache" }).selectOption("fr");
  await vater.getByRole("combobox", { name: "Zielsprache" }).selectOption("de");
  await vater.getByRole("textbox", { name: "Vokabel suchen" }).fill(WORD);

  const row = vater.getByRole("row", { name: new RegExp(WORD) }).first();
  await expect(row).toBeVisible();
  await row.getByRole("button", { name: /Bilder/ }).click();

  const panel = vater.getByRole("heading", { name: new RegExp(`Bilder für .${WORD}`) });
  await expect(panel).toBeVisible();
  for (const description of ["Stadt als Comic-Zeichnung", "Stadt als Foto"]) {
    await vater.getByRole("textbox", { name: "Bild suchen" }).fill(description);
    await vater.getByRole("button", { name: "Suchen" }).click();
    await vater.getByRole("button", { name: "+ Zuordnen" }).first().click();
    await expect(vater.getByRole("img", { name: description })).toBeVisible();
  }

  // ---------- Profil des Kindes: das Interesse steuert, welche Darstellung gewinnt ----------
  await vater.goto(`/vater/kind/${childId}`);
  await expect(vater.getByRole("heading", { name: "Interessen" })).toBeVisible();
  await vater.locator("#int-label").fill("Comic");
  await vater.locator("#int-facet").selectOption("Style");
  // `exact`, weil der Kind-Hub inzwischen mehrere Hinzufügen-Knöpfe trägt (Betreuer, Stundenplan) und
  // Playwright den Namen sonst als Teilzeichenkette matcht.
  await vater.getByRole("button", { name: "Hinzufügen", exact: true }).click();
  await vater.getByRole("button", { name: "Interessen speichern" }).click();
  await expect(vater.getByText("Interessen gespeichert.")).toBeVisible();

  // ---------- Plan mit der Übung, damit der Sohn sie spielen kann ----------
  await vater.getByRole("link", { name: "Neuer Plan", exact: true }).click();
  const kindSelect = vater.getByRole("combobox", { name: "Kind" });
  await expect(kindSelect.locator("option")).not.toHaveCount(0);
  await kindSelect.selectOption(childId);
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
  await sohn.getByRole("link", { name: /ÜBEN/ }).click();
  await expect(sohn.locator(".pill.cyan", { hasText: /Karte \d+ \/ \d+/ })).toBeVisible();

  // Die bebilderte Vokabel suchen: der Kartensatz ist eingefroren, „la ville" kann irgendwo stehen.
  const image = sohn.locator(".fcard img");
  for (let i = 0; i < 12 && !(await image.count()); i++) {
    await sohn.getByRole("button", { name: "Umdrehen 🔄" }).click();
    await sohn.getByRole("button", { name: "Gewusst!" }).click();
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
