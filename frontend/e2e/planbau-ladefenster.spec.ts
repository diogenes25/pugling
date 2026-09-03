import { test, expect, type Page } from "@playwright/test";

/*
 * B-167: Dasselbe Ladefenster wie im Assistenten (B-169) — nur im **Planbau**, wo es niemand geschlossen
 * hatte. `useAsync` behält seine Daten über einen Kriterienwechsel hinweg (B-116, und das ist richtig),
 * also stehen für die Dauer der neuen Abfrage die Treffer des *vorigen* Filters da.
 *
 * Vier Symptome hatte diese Fläche, und das vierte ist das teure: `exerciseId` überlebte den Wechsel,
 * seine Markierung verschwand mit der Zeile, und der Absende-Knopf blieb bedienbar — der Vater legte eine
 * Position auf eine Übung an, die er gar nicht mehr sah.
 *
 * Eigene Datei statt eines Blocks in `assistent.spec.ts`: andere Fläche, und ein Rot soll sagen, welcher
 * der beiden Bildschirme bricht (Regel aus `frontend/CLAUDE.md`: eine Fläche je Spec). Die Verzögerung
 * kommt per `route`, nicht per gefälschter Antwort — der echte Server antwortet, nur später (Präzedenz
 * `assistent.spec.ts`, `vater-von-null.spec.ts`).
 */

const RUN = Date.now().toString().slice(-6);

async function vaterLogin(page: Page) {
  await page.goto("/vater");
  await page.locator("#fid").fill("1");
  await page.locator("#pin").fill("0000");
  await page.getByRole("button", { name: "Anmelden" }).click();
  await expect(page.getByRole("heading", { name: "Kinder" })).toBeVisible();
}

/** Legt einen leeren Plan für das geseedete Kind an und landet im Planbau. */
async function planbauOeffnen(page: Page, titel: string) {
  await page.goto("/vater/plan/new");
  await page.locator("#plan-title").fill(titel);
  await page.getByRole("combobox", { name: "Kind" }).selectOption("1");
  await page.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(page).toHaveURL(/\/vater\/plan\/\d+$/);
}

/** Fach auf Englisch stellen – danach steht die geseedete Übung „Begrüßungen" in der Trefferliste. */
async function englischFiltern(page: Page) {
  await page.locator('select[aria-label="Fach-Filter"]').selectOption({ label: "Englisch" });
  await expect(page.getByRole("radio", { name: /Begrüßungen/ })).toBeEnabled();
}

test("Solange die Trefferliste zum vorigen Filter gehört, ist keine Übung wählbar", async ({ page }) => {
  await vaterLogin(page);
  await planbauOeffnen(page, `Ladefenster ${RUN}`);
  await englischFiltern(page);

  const zeile = page.getByRole("radio", { name: /Begrüßungen/ });
  // Der Ausgangszustand, den die Zusicherungen unten NICHT prüfen dürfen: eine geladene Liste mit
  // bedienbarer Zeile und einer echten Trefferzahl. Ohne diese beiden Zeilen wäre „gesperrt" auch dann
  // wahr, wenn die Liste gar nicht da ist — und „keine Zahl" auch dann, wenn nie eine stand.
  await expect(zeile).toBeEnabled();
  await expect(page.getByText(/\(\d+ Treffer\)/)).toBeVisible();

  // Ab jetzt antwortet die Suche erst nach 2 s – lange genug, um im Fenster zu messen.
  await page.route("**/api/v1/creator/exercises?**", async (route) => {
    await new Promise((r) => setTimeout(r, 2000));
    await route.fallback();
  });

  await page.getByLabel("Freitext-Filter").fill("environment");

  // Die Fristen sind bewusst kürzer als die Verzögerung: sonst pollt die Zusicherung, bis die Antwort da
  // ist, und meldet dann etwas über den Zustand DANACH statt über das Fenster.
  await expect(zeile).toBeVisible();
  await expect(zeile).toBeDisabled({ timeout: 1200 });
  // Die Zahl ist die eigentliche Falschaussage: sie behauptet etwas über den eingestellten Filter.
  await expect(page.getByText(/\(\d+ Treffer\)/)).toBeHidden({ timeout: 1200 });
  await expect(page.getByText("(Suche läuft…)")).toBeVisible({ timeout: 1200 });

  // Und die Sperre ist ein Fenster, kein Dauerzustand: nach der Antwort ist die neue Zeile bedienbar.
  await expect(page.getByRole("radio", { name: /The environment/ })).toBeEnabled({ timeout: 10_000 });
  await expect(page.getByText(/\(\d+ Treffer\)/)).toBeVisible();
});

test("Scheitert die Suche, bleiben die alten Zeilen gesperrt", async ({ page }) => {
  await vaterLogin(page);
  await planbauOeffnen(page, `Fehlerzweig ${RUN}`);
  await englischFiltern(page);

  const zeile = page.getByRole("radio", { name: /Begrüßungen/ });
  await expect(zeile).toBeEnabled();

  /*
   * Der Fehlerzweig ist die zweite Tür, und ohne den Schlüsselvergleich stünde sie offen: `data` bleibt
   * die alte Seite, `loading` fällt auf `false`, und der Effekt auf `[exercises.data]` läuft nie (die
   * Referenz ändert sich nicht). Eine Sperre über `loading` wäre hier also schon wieder auf – dieselbe
   * Lücke, die B-169 im Assistenten am Nachbarknopf gefunden hat.
   */
  await page.route("**/api/v1/creator/exercises?**", (route) =>
    route.fulfill({ status: 500, contentType: "application/problem+json",
      body: JSON.stringify({ status: 500, title: "Server error", detail: "B-167 Probe" }) }));

  const gescheitert = page.waitForResponse(
    (r) => r.url().includes("/creator/exercises") && r.status() === 500);
  await page.getByLabel("Freitext-Filter").fill("environment");
  await gescheitert;

  // Die alten Zeilen stehen noch (B-116-Regel, gewollt) – und sind gesperrt.
  await expect(zeile).toBeVisible();
  await expect(zeile).toBeDisabled();
});

test("Ein Filterwechsel verwirft die Auswahl – und sagt es", async ({ page }) => {
  await vaterLogin(page);
  await planbauOeffnen(page, `Auswahl ${RUN}`);
  await englischFiltern(page);

  const hinzufuegen = page.getByRole("button", { name: /Position hinzufügen/ });
  // Ohne Auswahl ist der Knopf gesperrt – der Ausgangszustand, gegen den die letzte Zusicherung erst
  // etwas aussagt.
  await expect(hinzufuegen).toBeDisabled();

  await page.getByRole("radio", { name: /Begrüßungen/ }).check();
  await expect(hinzufuegen).toBeEnabled();

  /*
   * Der Kern: Vor B-167 überlebte `exerciseId` diesen Wechsel. Die Zeile verschwand aus der Liste, ihre
   * Markierung mit ihr – der Knopf blieb bedienbar, und ein Klick legte eine Position auf eine Übung an,
   * die der Vater nicht mehr sah.
   */
  await page.getByLabel("Freitext-Filter").fill("environment");

  await expect(page.getByText(/Auswahl zurückgesetzt \(1 Übung\)/)).toBeVisible({ timeout: 10_000 });
  await expect(hinzufuegen).toBeDisabled();
});

test("Ein Leerschlag am Ende der Suche ist dieselbe Suche und behält die Auswahl", async ({ page }) => {
  await vaterLogin(page);
  await planbauOeffnen(page, `Leerschlag ${RUN}`);
  await englischFiltern(page);

  const suche = page.getByLabel("Freitext-Filter");
  await suche.fill("Begrüßungen");
  const zeile = page.getByRole("radio", { name: /Begrüßungen/ });
  await expect(zeile).toBeEnabled({ timeout: 10_000 });
  await zeile.check();

  const hinzufuegen = page.getByRole("button", { name: /Position hinzufügen/ });
  await expect(hinzufuegen).toBeEnabled();

  /*
   * Der Schlüssel läuft über die **normalisierten** Kriterien. Ohne das `trim` wäre jeder Tastendruck im
   * Freitextfeld ein Kriterien-Wechsel, und der Vater verlöre seine Auswahl beim Weitertippen – die
   * Reparatur wäre dann schlimmer als der Defekt.
   */
  /*
   * Der Leerschlag löst **gar keine** Abfrage mehr aus: dieselbe normalisierte Kriterienliste ist zugleich
   * die Abhängigkeitsliste von `useAsync`. Das ist zu belegen, und die Abwesenheit eines Ereignisses
   * braucht ein begrenztes Fenster — daher der Zähler plus die kurze Frist. Die erste Fassung dieses
   * Falls prüfte stattdessen `toBeHidden()` auf einen nie sichtbaren Hinweis und `toBeEnabled()` auf
   * einen schon aktiven Knopf: zwei Zusicherungen über den AUSGANGSZUSTAND, beim ersten Poll wahr.
   */
  let suchen = 0;
  page.on("request", (r) => { if (r.url().includes("/creator/exercises?")) suchen++; });

  await suche.fill("Begrüßungen ");
  await page.waitForTimeout(500);

  expect(suchen).toBe(0);
  await expect(zeile).toBeChecked();
  await expect(page.getByText(/Auswahl zurückgesetzt/)).toBeHidden();
  await expect(hinzufuegen).toBeEnabled();

  /*
   * Die Gegenprobe im selben Fall, und ohne sie wäre die Aussage oben nichts wert: Eine ECHTE Änderung
   * feuert sehr wohl und verwirft die Auswahl. Erst das Paar trennt „richtig normalisiert" von
   * „reagiert überhaupt nicht mehr auf das Suchfeld".
   */
  const antwort = page.waitForResponse((r) => r.url().includes("/creator/exercises?"));
  await suche.fill("environment");
  await antwort;

  expect(suchen).toBeGreaterThan(0);
  await expect(page.getByText(/Auswahl zurückgesetzt \(1 Übung\)/)).toBeVisible();
  await expect(hinzufuegen).toBeDisabled();
});
