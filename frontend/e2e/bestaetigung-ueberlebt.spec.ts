import { expect, test } from "@playwright/test";
import { createChild, vaterLogin } from "./helpers";

/*
 * `{ exact: true }` an beiden „Gespeichert."-Zusicherungen ist nicht Stil: `getByText` vergleicht als
 * Teilstring, und auf der Kind-Seite steht „Gespeichert. Die neue PIN gilt ab dem nächsten Login des
 * Kindes." (VaterKind.tsx). Heute grün, weil diese Spec die PIN nicht anfasst — aber es ist dieselbe
 * Falle, die `kind-lehrbuch-fach.spec.ts:51` im vollen Lauf rot gemacht hat (B-189).
 *
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
  await expect(page.getByText("Gespeichert.", { exact: true })).toBeVisible();

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
  const childId = await createChild(page, KIND, "4321");

  const BUCH = `Prüfbuch ${STAMP}`;
  await page.goto(`/vater/kind/${childId}?bereich=schule`);
  await page.locator("#tbnew-title").fill(BUCH);
  await page.locator("#tbnew-chapter").fill("Unit 1");
  await page.getByRole("button", { name: "Buch hinterlegen" }).click();
  // Der Anlege-Weg funktionierte schon vorher – er steht hier als Gegenprobe, nicht als Beleg.
  // `exact` ist tragend, nicht Stil: „Noch kein Buch hinterlegt." ENTHAELT diese Meldung, und solange
  // das Neuladen laeuft, steht der Leer-Text noch da (B-189/B-198).
  await expect(page.getByText("Buch hinterlegt.", { exact: true })).toBeVisible();

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
  await expect(page.getByText("Gespeichert.", { exact: true })).toBeVisible();
});

/*
 * B-197 und B-196 - beide Haelften desselben Satzes: **Was am Meldungsplatz steht, gehoert zu der
 * Handlung, die gerade getan wurde.**
 *
 * Die Nachschau zu Sprint 1 hat beide gefunden, und beide sind Selbstwiderspruecke der Reparatur, die
 * diesen Platz erst eingefuehrt hat:
 *   B-197 - geraeumt wurde an drei Stellen, alle beim OEFFNEN eines Formulars, keine beim ABSENDEN. Ein
 *           zweiter, scheiternder Versuch zeigte seinen Fehler am Formular, waehrend oben weiter das
 *           gruene "Gespeichert." des ersten stand. Zwei widersprechende Meldungen in einem Blickfeld -
 *           genau der Schaden, gegen den der Platz gebaut wurde.
 *   B-196 - das Loeschen meldete gar nichts. B-173s Akzeptanzkriterium 2 nennt es ausdrücklich; gebaut
 *           war es nie, und der 43/43-Rollengang hat es nicht gefangen, weil keine Spec eine Reihe
 *           loescht. Ohne Meldung blieb die Bestaetigung von vorhin ueber der verschwundenen Zeile
 *           stehen und las sich als Loeschbestaetigung.
 */
test("B-197/B-196: der Meldungsplatz gehoert zur letzten Handlung", async ({ page }) => {
  const LAUF = Date.now().toString(36).slice(-5);
  const reiheA = `Platz A ${LAUF}`;
  const reiheB = `Platz B ${LAUF}`;

  await vaterLogin(page, FATHER);
  await page.goto("/vater/lehrwerke");

  for (const name of [reiheA, reiheB]) {
    await page.locator("#ns-name").fill(name);
    await page.getByRole("button", { name: "Reihe anlegen" }).click();
    await expect(page.getByText(/steht im Katalog/)).toBeVisible();
  }

  // ---- Schritt 1: ein erfolgreiches Speichern legt eine Meldung auf den Platz ----
  await page.getByRole("button", { name: `Reihe bearbeiten: „${reiheA}"` }).click();
  const nameFeld = page.getByLabel("Name der Reihe (Pflicht)", { exact: true });
  await nameFeld.fill(`${reiheA} neu`);
  await page.getByRole("button", { name: `Speichern: „${reiheA}"` }).click();
  await expect(page.getByText("Gespeichert.", { exact: true })).toBeVisible();

  // ---- Schritt 2 (B-197): ein ZWEITER Versuch derselben Sitzung meldet am Formular ----
  // Das Formular bleibt nach dem Speichern offen - das ist Absicht (B-123). Bis B-233 trug hier ein leerer
  // Name den Fall („Der Name der Reihe fehlt."); seitdem ist der Knopf bei leerem Pflichtfeld gesperrt.
  // Uebrig bleibt die zweite lokale Meldung: nochmal speichern ohne Aenderung.
  await page.getByLabel("Name der Reihe (Pflicht)", { exact: true }).fill("");
  await expect(page.getByRole("button", { name: `Speichern: „${reiheA} neu"` })).toBeDisabled();
  await page.getByLabel("Name der Reihe (Pflicht)", { exact: true }).fill(`${reiheA} neu`);
  await page.getByRole("button", { name: `Speichern: „${reiheA} neu"` }).click();
  await expect(page.getByText("Nichts geändert.", { exact: true })).toBeVisible();

  // Der Kern: das gruene "Gespeichert." von Schritt 1 darf jetzt NICHT mehr dastehen.
  await expect(page.getByText("Gespeichert.", { exact: true })).toHaveCount(0);

  // ---- Schritt 3 (B-196): Loeschen meldet, und es nennt die Reihe ----
  // Erst wieder einen gueltigen Stand herstellen und speichern, damit oben eine ALTE Meldung steht -
  // ohne sie koennte der Fall nicht zeigen, dass das Loeschen sie ersetzt statt sie zu erben.
  // Ein ANDERER Name, sonst gibt es nichts zu speichern und das Formular meldet "Nichts geaendert."
  // (am 2026-09-02 genau so gemessen - der erste Entwurf fuellte den bereits gespeicherten Wert zurueck).
  await page.getByLabel("Name der Reihe (Pflicht)", { exact: true }).fill(`${reiheA} dritt`);
  await page.getByRole("button", { name: `Speichern: „${reiheA} neu"` }).click();
  await expect(page.getByText("Gespeichert.", { exact: true })).toBeVisible();

  page.once("dialog", (d) => d.accept());
  await page.getByRole("button", { name: `Löschen: „${reiheB}"` }).click();

  // Erst warten, bis die Zeile wirklich weg ist - sonst traefe die Zusicherung den Frame davor.
  await expect(page.getByRole("row", { name: new RegExp(reiheB) })).toHaveCount(0);
  await expect(page.getByText(`Reihe „${reiheB}" gelöscht.`, { exact: true })).toBeVisible();
  // Und die Bestaetigung von vorhin ist ersetzt, nicht geerbt.
  //
  // Was diese Zusicherung rot macht: eine Fassung, die die Loeschmeldung ueber
  // `action.run(…, 'Reihe „…" geloescht.')` in den ZEILEN-Banner legte statt an den Seitenplatz. Dann
  // bliebe der Platz unangetastet, das alte "Gespeichert." staende weiter da - und die Zeile mitsamt
  // ihrem Banner verschwaende ohnehin. Ohne diesen Fall waere genau das eine gruene Alternative.
  await expect(page.getByText("Gespeichert.", { exact: true })).toHaveCount(0);
});
