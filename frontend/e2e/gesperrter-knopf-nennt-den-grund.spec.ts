import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-160. [B-150](../../docs/backlog/B-150-verlagssperre-unsichtbar-dialog-verspricht-gegenteil.md) hat den
 * Löschen-Knopf am Verlag gesperrt, damit der Creator nicht in einen `409` läuft, und den Grund in ein
 * `title`-Attribut geschrieben — **auf den `disabled` Knopf**. Ein `disabled` Form-Control empfängt in
 * Chromium und WebKit keine Pointer-Events: Der Tooltip erschien nie, und aus dem Tab-Lauf war der Knopf
 * ebenfalls heraus. Die zweite Hälfte von B-150s Akzeptanzkriterium („der Knopf nennt den Grund") war
 * damit nur scheinbar gebaut.
 *
 * **Das ist der Rollengang** — und er gehört in einen echten Browser, weil genau eine Browser-Eigenschaft
 * die Ursache war. Der Bausteintest (`src/vater/PublisherAdmin.test.tsx`) trägt die Zustandsregel; dass ein
 * Mensch den Grund auf dem Bildschirm findet, kann nur diese Ebene sagen.
 *
 * Der Zustand ist ein **Dauerzustand des Seeds** und braucht keine Vorbereitung: An „Klett" hängt die
 * herrenlose „Green Line 1", also `foreignSeriesCount > 0`. Eigene Datei (`frontend/CLAUDE.md`: eine
 * Fläche je Spec) — `lehrwerke.spec.ts` fährt die Reihen, nicht das Verlags-Vokabular.
 */

const FATHER = { id: "1", pin: "0000" };

test("Der gesperrte Löschen-Knopf nennt seinen Grund, ohne dass jemand darüberfahren muss", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.goto("/vater/lehrwerke");

  // Die Verwaltung liegt eingeklappt hinter ihrem eigenen Knopf – sie ist eine seltene Handlung.
  await page.getByRole("button", { name: /Verlags-Vokabular verwalten/ }).click();
  // Enter, nicht nur `fill`: Gefiltert wird erst beim Absenden (`applied`). Ohne das liefe die Zeilensuche
  // gegen die ungefilterte Liste – heute eindeutig, aber die Zeile verspräche eine Eingrenzung, die nicht
  // stattfindet.
  await page.getByLabel("Verlag suchen").fill("Klett");
  await page.getByLabel("Verlag suchen").press("Enter");

  /*
   * Über den Löschen-Knopf eingeschränkt, nicht über den Namen: „Klett" steht auf dieser Seite auch in der
   * Reihen-Tabelle darüber („Green Line 1 · Klett"), ein Teilstring-Bezug träfe beide. Genau die Bauart,
   * die B-153 gerade gekostet hat.
   */
  const zeile = page.getByRole("row").filter({ has: page.getByRole("button", { name: "Löschen: Klett" }) });
  await expect(zeile).toBeVisible();

  // ---- AK 1: Der Grund steht als Text da, nicht in einem `title`, den niemand auslösen kann ----
  await expect(zeile.getByText(/darum nicht löschbar/)).toBeVisible();

  const knopf = zeile.getByRole("button", { name: "Löschen: Klett" });

  /*
   * ---- AK 3: Der Knopf ist gesperrt UND auffindbar ----
   * `aria-disabled` statt `disabled` ist der Kern. Die Fokus-Zusicherung ist die eigentliche Aussage:
   * Ein echtes `disabled` nimmt den Knopf aus dem Tab-Lauf, ein Tastatur- oder Screenreader-Nutzer träfe
   * ihn nie und erführe nichts von der Sperre. Der Browser sagt hier, was happy-dom nur nachbildet.
   */
  await expect(knopf).toHaveAttribute("aria-disabled", "true");
  await knopf.focus();
  await expect(knopf).toBeFocused();

  /*
   * ---- AK 2: Und trotzdem kommt niemand an ihn heran ----
   * Gemessen beim Bauen dieser Spec: Playwrights Actionability wertet `aria-disabled="true"` als „nicht
   * bedienbar" und verweigert den Klick (`element is not enabled`). Das ist die Aussage, die auf DIESER
   * Ebene möglich ist — jedes Werkzeug, das Actionability respektiert, prallt ab.
   *
   * Dass auch ein Klick, der trotzdem durchkommt (echter Nutzer, Skript, Tastatur), nichts auslöst, kann
   * ein E2E hier deshalb gar nicht prüfen: Es käme nie so weit. Diese Hälfte trägt der Bausteintest
   * (`PublisherAdmin.test.tsx`, „löst in diesem Zustand keine Anfrage aus") — und sie ist der Preis von
   * `aria-disabled`, denn der Browser hält den Klick nicht mehr selbst auf.
   */
  await expect(knopf).toBeDisabled();
});
