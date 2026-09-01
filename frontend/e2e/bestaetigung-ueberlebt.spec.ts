import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-173 und B-151: Eine Bestätigung, die IN dem Element sitzt, dessen Verschwinden sie bestätigt, ist nie
 * zu lesen. Zwei belegte Wege dorthin – hier beide, weil sie eine Wurzel und einen Meldungsplatz teilen.
 *
 * Eigene Datei, nicht angehängt an `lehrwerk-bearbeiten.spec.ts` (Regel aus `frontend/CLAUDE.md`: eine
 * Fläche je Spec; eine Spec bricht beim ersten Rot ab und nimmt alles Nachfolgende mit, B-109).
 *
 * Diese Spec IST der Rollengang beider Stories: echter Browser, echter Server, genau die Klickfolge, in
 * der die Lücke bestand. Ein Komponententest kann sie nicht ersetzen – der Verlust entsteht erst durch
 * ein erfolgreiches Server-Schreiben und das Neuladen danach.
 */

const FATHER = { id: "1", pin: "0000" };
const STAMP = Date.now().toString(36).slice(-5);
const VERLAG = `Filterverlag ${STAMP}`;
const REIHE = `Fällt raus ${STAMP}`;

test("B-173: Speichern, das die Zeile aus dem Filter wirft, zeigt trotzdem seine Bestätigung", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.goto("/vater/lehrwerke");

  // ---- Vorbereiten: eine Reihe MIT Verlag, denn nur ein gesetzter Verlag kann entfernt werden ----
  await page.locator("#ns-new-publisher").fill(VERLAG);
  await page.getByRole("button", { name: "Verlag anlegen" }).click();
  await expect(page.getByText(`„${VERLAG}" steht im Vokabular.`)).toBeVisible();

  await page.locator("#ns-name").fill(REIHE);
  await page.locator("#ns-publisher").selectOption({ label: VERLAG });
  await page.getByRole("button", { name: "Reihe anlegen" }).click();
  await expect(page.getByText(/steht im Katalog/)).toBeVisible();

  // ---- Auf genau diesen Verlag filtern: jetzt steht genau eine Zeile in der Liste ----
  await page.getByLabel("Verlag-Filter").selectOption({ label: VERLAG });
  await expect(page.getByRole("row", { name: new RegExp(REIHE) })).toBeVisible();

  // ---- Der Kern: den Verlag entfernen. Damit verlässt die Zeile ihre eigene Filterung. ----
  await page.getByRole("button", { name: `Reihe bearbeiten: „${REIHE}"` }).click();
  await page.locator('select[id^="se-publisher-"]').selectOption("");
  await page.getByRole("button", { name: `Speichern: „${REIHE}"` }).click();

  // ---- Die REIHENFOLGE ist hier tragend, nicht Geschmack ----
  // Zuerst warten, bis die Zeile wirklich weg ist. Andersherum gemessen (rote Probe am 2026-09-01):
  // `toBeVisible()` pollt und trifft das Banner in dem Frame, in dem es noch steht – die Zusicherung
  // hielt damit VOR wie NACH der Reparatur und konnte gar nicht fallen.
  await expect(page.getByRole("row", { name: new RegExp(REIHE) })).toHaveCount(0);
  await expect(page.getByText("Gespeichert.")).toBeVisible();

  // ---- Und die leere Liste sagt jetzt, WARUM sie leer ist ----
  await expect(page.getByText(/Keine Reihe passt zu dieser Filterung/)).toBeVisible();
  // Die falsche Handlungsanweisung darf nicht danebenstehen: ihr zu folgen erzeugte eine Dublette im
  // geteilten Katalog.
  await expect(page.getByText("Noch kein Lehrwerk. Lege unten eines an.")).toHaveCount(0);
});

test("B-151: Das Lehrbuch-Formular schließt beim Speichern – die Bestätigung bleibt sichtbar", async ({ page }) => {
  await vaterLogin(page, FATHER);

  // Ein eigenes Kind: das geseedete trägt Lernstand, den diese Spec nicht anfassen soll (Muster
  // `kind-lehrbuch-fach.spec.ts`).
  const KIND = `Meldungskind ${STAMP}`;
  await page.goto("/vater");
  await page.locator("#new-child-name").fill(KIND);
  await page.locator("#new-child-pin").fill("4321");
  await page.getByRole("button", { name: "Kind anlegen" }).click();
  const kindZeile = page.getByRole("row", { name: new RegExp(KIND) });
  await expect(kindZeile).toBeVisible();
  const childId = (await kindZeile.getByRole("cell").first().textContent())!.trim();

  const BUCH = `Prüfbuch ${STAMP}`;
  await page.goto(`/vater/kind/${childId}`);
  await page.locator("#tbnew-title").fill(BUCH);
  await page.locator("#tbnew-chapter").fill("Unit 1");
  await page.getByRole("button", { name: "Buch hinterlegen" }).click();
  // Der Anlege-Weg funktionierte schon vorher – er steht hier als Gegenprobe, nicht als Beleg.
  await expect(page.getByText("Buch hinterlegt.")).toBeVisible();

  // ---- Bearbeiten: `onDone` schließt das Formular und nahm den Banner mit aus dem DOM ----
  await page.getByRole("button", { name: `${BUCH} bearbeiten` }).click();

  // Auf das BEARBEITEN-Formular eingrenzen: Das Anlege-Formular darunter trägt dieselben Felder und
  // einen zweiten Absende-Knopf. Seine Ids beginnen mit `tbnew`, die des Bearbeitens mit `tb<id>` –
  // ohne diese Grenze bricht jeder Zugriff als „strict mode violation" ab, und zwar unabhängig davon,
  // ob die Reparatur wirkt.
  const bearbeiten = page.locator("form")
    .filter({ has: page.locator('input[id$="-chapter"]:not(#tbnew-chapter)') });
  await bearbeiten.locator('input[id$="-chapter"]').fill(`Unit ${STAMP}`);
  await bearbeiten.getByRole("button", { name: "Speichern" }).click();

  // Wieder die Reihenfolge: erst warten, bis das Formular WEG ist – danach muss die Meldung noch da
  // sein. Andersherum träfe die Zusicherung den Frame, in dem beides noch steht.
  await expect(bearbeiten).toHaveCount(0);
  await expect(page.getByText("Gespeichert.")).toBeVisible();
});
