import { expect, test, type Page } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-216. `useAsync.reload()` setzt `error` auf `null` und `loading` auf `true` im selben Schritt. Ein
 * Banner, das allein an `error` hing, verschwand damit samt seinem „Erneut suchen"-Knopf genau im Moment des
 * Klicks: Der Fokus fiel auf `<body>`, und während der Wiederholung stand nichts da.
 *
 * Gemessen wird IM Fenster der Wiederholung – darum antwortet die Suche beim zweiten Mal erst nach 2 s
 * (echter Server, nur später; Muster `planbau-ladefenster.spec.ts`). Die Zusicherung „der Knopf heißt jetzt
 * „Lädt…" und hat den Fokus" ist vor dem Fix unerfüllbar: es gab in diesem Fenster gar keinen Knopf.
 *
 * Zwei Flächen in einer Datei, bewusst: Es ist EIN Fehlerbild an zwei Zwillingen (Assistent, Planbau), und
 * beide Fälle sind kurz. Die Regel „eine Fläche je Spec" schützt vor dem Abbruch-Schatten langer Durchstiche
 * (B-109); zwei kurze Fälle bilden keinen. Die dritte Stelle (Sohn-Shop) hat ihre eigene Spec, weil sie eine
 * andere Rolle fährt: `shop-verlauf-wiederholen.spec.ts`.
 *
 * Das ist zugleich der Rollengang aus der Supervisor-Rolle.
 */

const FATHER = { id: "1", pin: "0000" };
// Als Prädikat, nicht als Glob: Die erste Suche des Planbaus hat KEINE Query (`…/creator/exercises`), die des
// Assistenten immer eine – ein Glob mit `?` träfe nur eine von beiden.
const SUCHE = (url: URL) => /\/api\/v1\/creator\/exercises$/.test(url.pathname);
const RUN = Date.now().toString().slice(-6);

/** Ab jetzt antwortet die Suche wieder – aber erst nach `ms`, lange genug, um im Fenster zu messen. */
async function sucheVerzoegern(page: Page, ms: number) {
  await page.unroute(SUCHE);
  await page.route(SUCHE, async (route) => {
    await new Promise((r) => setTimeout(r, ms));
    await route.fallback();
  });
}

/** Der Kern beider Fälle: Knopf drücken, und im Ladefenster steht der Kasten noch – Knopf gesperrt, umbeschriftet. */
async function wiederholenUndImFensterMessen(page: Page) {
  const kasten = page.getByRole("alert").filter({ hasText: "Die Übungssuche ist gescheitert" });
  await page.getByRole("button", { name: "Erneut suchen" }).click();

  const laedt = page.getByRole("button", { name: "Lädt…" });
  await expect(laedt).toBeVisible();
  await expect(laedt).toBeDisabled();
  // Der Fokus fällt nicht auf `<body>`: Ein gesperrter Knopf verliert ihn in Chromium (gemessen), darum
  // liegt er auf dem Kasten, der sagt, was gerade passiert.
  await expect(kasten).toBeFocused();
  // Und die Ursache steht weiter da, statt für zwei Sekunden zu verschwinden.
  await expect(kasten).toBeVisible();
}

test("Assistent: „Erneut suchen“ bleibt während der Wiederholung unter dem Finger", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.route(SUCHE, (route) => route.abort("failed"));
  await page.goto("/vater/wizard");

  // Zu Schritt 3 wie in `ausfall-wird-genannt.spec.ts`: Kind „1" ist der geseedete Sohn.
  await page.getByRole("button", { name: "Bestehendes Kind" }).click();
  await page.getByRole("combobox", { name: "Kind" }).selectOption("1");
  await page.getByRole("button", { name: "Weiter" }).click();
  await expect(page.getByRole("heading", { name: /Wo hakt es/ })).toBeVisible();
  await page.getByLabel("Fach", { exact: true }).selectOption({ label: "Englisch" });
  await page.getByRole("button", { name: "Weiter" }).click();
  await expect(page.getByText(/Die Übungssuche ist gescheitert/)).toBeVisible();

  await sucheVerzoegern(page, 2000);
  await wiederholenUndImFensterMessen(page);

  // Danach gilt wieder, was der Server sagt – positiv zugesichert, nicht nur „Banner weg".
  await expect(page.getByText(/passende Übungen/)).toBeVisible();
  await expect(page.getByText(/Die Übungssuche ist gescheitert/)).toHaveCount(0);
});

test("Planbau: „Erneut suchen“ bleibt während der Wiederholung unter dem Finger", async ({ page }) => {
  await vaterLogin(page, FATHER);

  // Erst den Plan anlegen (das braucht keine Übungssuche), dann die Suche abschießen: Sie fliegt, sobald der
  // Planbau mit seiner Filterleiste erscheint.
  await page.goto("/vater/plan/new");
  await page.locator("#plan-title").fill(`Wiederholknopf ${RUN}`);
  await page.getByRole("combobox", { name: "Kind" }).selectOption("1");
  await page.route(SUCHE, (route) => route.abort("failed"));
  await page.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(page).toHaveURL(/\/vater\/plan\/\d+$/);
  await expect(page.getByText(/Die Übungssuche ist gescheitert/)).toBeVisible();

  await sucheVerzoegern(page, 2000);
  await wiederholenUndImFensterMessen(page);

  await expect(page.getByText(/\(\d+ Treffer\)/)).toBeVisible();
  await expect(page.getByText(/Die Übungssuche ist gescheitert/)).toHaveCount(0);
});
