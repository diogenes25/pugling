import { expect, test } from "@playwright/test";
import { sohnLogin } from "./helpers";

/*
 * B-216, die dritte Stelle – und die einzige in der Sohn-Arcade. Der Lader des Kaufverlaufs setzt `loading`
 * und `error` im selben Tick; die leere Karte fiel nach dem Klick auf „Nochmal versuchen" in ihren
 * Lade-Zweig und renderte NICHTS. Der Knopf verschwand unter dem Finger des Kindes, sein „Lädt…" war toter
 * Code, und ob überhaupt etwas passiert, war nicht zu sehen.
 *
 * Eigene Spec statt eines Blocks in `shop-verlauf.spec.ts`: dort geht es um den eigenen Kauf, hier fällt
 * absichtlich etwas aus (`frontend/CLAUDE.md`: eine Fläche je Spec). Nur die Verlaufs-Seite wird
 * abgeschossen – der Shop selbst lädt (`…/me/shop` ohne Query), damit das Kind überhaupt bis zum Tab kommt.
 *
 * Das ist zugleich der Rollengang aus der Student-Rolle.
 */

const CHILD = { id: "1", pin: "1111" };
const VERLAUF = "**/api/v1/student/me/shop?purchaseSkip=**";

test("„Nochmal versuchen“ bleibt während der Wiederholung stehen und sagt „Lädt…“", async ({ page }) => {
  await sohnLogin(page, CHILD);
  // Erst die angemeldete Arcade abwarten, dann navigieren – ein sofortiges `goto` bricht den laufenden
  // Login ab (Begründung in `shop-verlauf.spec.ts`).
  await expect(page.locator("nav.sohn-nav")).toBeVisible();

  await page.route(VERLAUF, (route) => route.abort("failed"));
  await page.goto("/sohn/shop");
  await expect(page.locator(".screen-title", { hasText: "Shop" })).toBeVisible();
  await page.getByRole("button", { name: "Verlauf" }).click();

  // Der Zustand vor dem Klick: Meldung und Knopf. `getByRole("alert")`, nicht der Fehlertext – den zeigt
  // für zwei Sekunden auch der Toast, und der ist nicht die Karte.
  const meldung = page.getByRole("alert");
  await expect(meldung).toBeVisible();
  const knopf = page.getByRole("button", { name: "Nochmal versuchen" });
  await expect(knopf).toBeVisible();

  // Ab jetzt antwortet der Server wieder – aber erst nach 2 s, lange genug, um im Fenster zu messen.
  await page.unroute(VERLAUF);
  await page.route(VERLAUF, async (route) => {
    await new Promise((r) => setTimeout(r, 2000));
    await route.fallback();
  });
  await knopf.click();

  // Im Fenster: der Knopf ist noch da, gesperrt und umbeschriftet – und der Fokus liegt auf der Meldung,
  // nicht auf `<body>` (ein gesperrter Knopf verliert ihn in Chromium, gemessen).
  const laedt = page.getByRole("button", { name: "Lädt…" });
  await expect(laedt).toBeVisible();
  await expect(laedt).toBeDisabled();
  await expect(meldung).toBeFocused();
  await expect(meldung).toBeVisible();

  // Danach sagt die Karte, was gilt: der Verlauf (leer oder gefüllt – ein anderer Spec kauft in derselben
  // Wegwerf-DB für dasselbe Kind), und keine Meldung mehr.
  await expect(page.getByText(/Noch nichts gekauft/).or(page.locator(".list .row").first())).toBeVisible();
  await expect(meldung).toHaveCount(0);
});
