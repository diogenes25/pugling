import { test, expect, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-209. `SavedNotice` sitzt in der Sohn-Shell **über** den Routen und bleibt die ganze Sitzung montiert.
 * Es räumte seine Meldung nie ab: „Gespeichert – deine Übungen passen sich an." stand nach dem Speichern
 * der Interessen über Shop, Weg und Konto weiter da, als hätte das Kind dort gerade etwas getan.
 *
 * Eigene Datei (`frontend/CLAUDE.md`: eine Fläche je Spec). Nicht an `interessen.spec.ts` angehängt: die
 * prüft die Meldung genau dort, wo sie **richtig** ist — auf `/sohn` selbst —, und genau deshalb ist ihr
 * der Fehler nie aufgefallen. Und nicht an `bestaetigung-ueberlebt.spec.ts`: die fährt das Vater-Web.
 *
 * Das ist zugleich der Rollengang aus der Sohn-Rolle: Der Bausteintest
 * (`src/components/SavedNotice.test.tsx`) trägt die Regel, dieser Weg trägt die Arcade — die Shell, die
 * echte Navigationsleiste und den Sprung, den `SohnInterests` nach dem Speichern macht.
 */

const FATHER = { id: "1", pin: "0000" };
const CHILD = { name: "Meldungs-Kind", pin: "5151" };
const MELDUNG = /Gespeichert – deine Übungen passen sich an/;

async function sohnLogin(page: Page, childId: string) {
  await page.goto("/sohn");
  await page.locator("#childId").fill(childId);
  for (const d of CHILD.pin.split("")) {
    await page.locator(".keys button", { hasText: new RegExp(`^${d}$`) }).first().click();
  }
  await page.getByRole("button", { name: "▶ LOS" }).click();
  // Auf die angemeldete Arcade warten: „▶ LOS" wird nur angeklickt, sein Ergebnis nicht abgewartet –
  // ein sofortiges `goto` bräche den laufenden Login ab.
  await expect(page.locator("nav.sohn-nav")).toBeVisible();
}

test("Die Bestätigung des Kindes bleibt auf ihrem Bildschirm", async ({ browser }) => {
  // ---------- VATER: ein frisches Kind, damit der geführte Erstbesuch offen steht ----------
  const vaterCtx = await browser.newContext();
  const vater = await vaterCtx.newPage();
  await vaterLogin(vater, FATHER);
  await vater.locator("#new-child-name").fill(CHILD.name);
  await vater.locator("#new-child-pin").fill(CHILD.pin);
  await vater.getByRole("button", { name: "Kind anlegen" }).click();
  // Die Id-Spalte mitmatchen: Der Name allein trifft auch die Betreuungs-Zeile des Dashboards – dieselbe
  // Seite trägt zwei Tabellen mit diesem Kind. Der Griff steht so schon in `interessen.spec.ts`.
  const zeile = vater.getByRole("row", { name: new RegExp(`^\\d+ ${CHILD.name}`) });
  await expect(zeile).toBeVisible();
  const childId = (await zeile.getByRole("cell").first().textContent())!.trim();

  // ---------- SOHN: Interessen speichern, damit es überhaupt eine Meldung gibt ----------
  /*
   * Unter **„Bewegung reduzieren"**, wie in `interessen.spec.ts`: Die Feier (`CelebrationLayer`) ist dort
   * `display: none`. Damit prüft diese Spec zweifelsfrei `SavedNotice` und nicht versehentlich den
   * Schmuck darüber – und sie fährt zugleich die härtere der beiden Bedingungen.
   */
  const sohnCtx = await browser.newContext({ reducedMotion: "reduce" });
  const sohn = await sohnCtx.newPage();
  await sohnLogin(sohn, childId);
  // Die Navigationsleiste der Arcade, nicht „irgendein Link mit diesem Wort": Der Weg-Bildschirm trägt
  // einen eigenen Knopf „Zur Basis", und ein Teilstring-Bezug träfe beide.
  const leiste = (name: string) => sohn.locator("nav.sohn-nav").getByRole("link", { name, exact: true });
  await sohn.goto("/sohn/ich");

  /*
   * Ohne eine einzige Auswahl durchklicken: `save()` kennt keine Mindestangabe, und WAS gespeichert wird,
   * ist hier gleichgültig — geprüft wird, wohin die Bestätigung danach gehört. Was das Kind auswählt,
   * prüft `interessen.spec.ts`; diese Spec an denselben Vorrat zu hängen, machte sie ohne Gewinn
   * zerbrechlich.
   */
  await expect(sohn.getByRole("button", { name: "Weiter" })).toBeVisible();
  for (let schritt = 0; schritt < 3; schritt++) {
    await sohn.getByRole("button", { name: "Weiter" }).click();
  }
  await sohn.getByRole("button", { name: "Fertig" }).click();

  // ---------- Auf ihrem Bildschirm steht sie zu Recht ----------
  await expect(sohn.getByText(MELDUNG)).toBeVisible();

  /*
   * Und jetzt der Fall: Ein Tippen in der Navigationsleiste wechselt den Bildschirm, nicht die Shell.
   * Vorher stand die Meldung danach über **jedem** folgenden Bildschirm bis zum Abmelden — eine
   * Bestätigung für etwas, das hier nie stattgefunden hat.
   */
  await leiste("Shop").click();
  await expect(sohn).toHaveURL(/\/sohn\/shop$/);
  await expect(sohn.getByText(MELDUNG)).toHaveCount(0);

  // Zwei Bildschirme weiter immer noch weg – der erste Wechsel könnte sie auch nur verdeckt haben.
  await leiste("Weg").click();
  await expect(sohn).toHaveURL(/\/sohn\/progress$/);
  await expect(sohn.getByText(MELDUNG)).toHaveCount(0);

  // Gegenprobe: Auch die Rückkehr an den Ort holt sie nicht zurück. Der Verlaufseintrag wurde beim
  // ersten Zeigen geleert; käme sie hier wieder, hinge sie an der Sitzung statt am Ereignis.
  await leiste("Basis").click();
  await expect(sohn).toHaveURL(/\/sohn$/);
  await expect(sohn.getByText(MELDUNG)).toHaveCount(0);

  await vaterCtx.close();
  await sohnCtx.close();
});
