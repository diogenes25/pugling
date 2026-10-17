import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-215. Der Hook fürs Typ-Manifest schluckte seinen Fehlschlag: Danach war „Server kennt null Typen" von
 * „Abruf gescheitert" nicht zu unterscheiden, und kein Bildschirm konnte den Ausfall nennen. Für den Creator
 * hieß das: „Übung anlegen" grau, in der Übungsliste kein „Bearbeiten" – und nirgends ein Grund.
 *
 * Zwei Flächen in einer Datei, bewusst: Es ist EIN Ausfall, der an zwei Stellen eine Handlung sperrt, und
 * beide Fälle sind kurz (Regel und Ausnahme in `frontend/CLAUDE.md` bzw. `ausfall-wird-genannt.spec.ts`).
 *
 * Das ist zugleich der Rollengang aus der Creator-Rolle.
 */

const FATHER = { id: "1", pin: "0000" };
const MANIFEST = "**/api/v1/creator/exercise-types";

test("Übung anlegen: Fällt das Manifest aus, steht der Grund über dem Formular – und „Erneut laden“ öffnet es wieder", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.route(MANIFEST, (route) => route.abort("failed"));
  await page.goto("/vater/exercises/neu");

  // Gesperrt war der Knopf schon vorher – neu ist, dass daneben steht, warum.
  await expect(page.getByRole("alert")).toContainText("Die Übungstypen konnten nicht geladen werden");
  await expect(page.getByRole("button", { name: "Übung anlegen" })).toBeDisabled();

  await page.unroute(MANIFEST);
  await page.getByRole("button", { name: "Erneut laden" }).click();

  await expect(page.getByRole("alert")).toHaveCount(0);
  await expect(page.getByRole("button", { name: "Übung anlegen" })).toBeEnabled();
});

test("Übungen verwalten: Fällt das Manifest aus, sagt die Liste es – statt „Typ hier nicht bearbeitbar“ ohne Grund", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.route(MANIFEST, (route) => route.abort("failed"));
  await page.goto("/vater/exercises");
  await page.locator("#ex-subject").selectOption({ label: "Englisch" });
  // Die geseedeten Englisch-Übungen gehören dem Lehrer-Konto, nicht Papa – ohne den Schalter ist die Liste leer.
  await page.getByLabel("geteilte Übungen anderer Väter anzeigen").check();

  await expect(page.getByRole("alert")).toContainText("Die Übungstypen konnten nicht geladen werden");
  // Die sichtbare Folge an der Zeile: Ohne Manifest fällt der Typname auf seinen Schlüssel zurück
  // („lesbar statt kaputt" – aber eben nur lesbar, und jetzt steht daneben, warum).
  await expect(page.getByText("· Vocabulary").first()).toBeVisible();

  await page.unroute(MANIFEST);
  await page.getByRole("button", { name: "Erneut laden" }).click();

  await expect(page.getByRole("alert")).toHaveCount(0);
  // Und das Manifest ist wirklich da: dieselbe Zeile trägt jetzt den Anzeigenamen.
  await expect(page.getByText("· Vokabelkarten").first()).toBeVisible();
  await expect(page.getByText("· Vocabulary")).toHaveCount(0);
});
