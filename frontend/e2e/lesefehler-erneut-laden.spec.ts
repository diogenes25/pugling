import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-183. Ein gescheitertes Lesen bot nirgends einen erneuten Versuch: der Vater sah „Failed to fetch" und half sich mit F5 –
 * und verlor damit jede Eingabe daneben. Jetzt steht an jeder solchen Stelle `ListenAusfall` mit „Erneut laden"; der Wächter
 * `src/lesefehler.test.ts` hält, DASS es überall steht, diese Spec, DASS der Weg im echten Browser zurückführt.
 *
 * Stellvertretend die Fächer im Katalog (`VaterKatalog.tsx`): eine Seite mit genau einer Leseabfrage, ohne Abhängigkeiten.
 * Rot, wenn das Banner wieder nackt ist (kein Knopf) oder der Knopf nicht `reload` ruft (die Fächer kämen nie).
 */
const FATHER = { id: "1", pin: "0000" };
const FAECHER = (url: URL) => /\/api\/v1\/creator\/subjects$/.test(url.pathname);

test("Fällt die Fächerliste aus, bringt »Erneut laden« sie zurück, ohne die Seite neu zu laden", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.route(FAECHER, (route) => route.abort("failed"));
  await page.goto("/vater/katalog");

  const meldung = page.getByRole("alert").filter({ hasText: "Die Fächer konnten nicht geladen werden" });
  await expect(meldung).toBeVisible();

  // Das Netz ist zurück. Ein Neuladen der Seite ist ausgeschlossen: gezählt wird, dass das Dokument dasselbe bleibt.
  await page.unroute(FAECHER);
  const dokument = await page.evaluate(() => (window as unknown as { __b183?: number }).__b183 = Date.now());
  await meldung.getByRole("button", { name: "Erneut laden" }).click();

  await expect(meldung).toHaveCount(0);
  await expect(page.locator("#ca-subject").locator("option", { hasText: /^Englisch$/ })).toHaveCount(1);
  expect(await page.evaluate(() => (window as unknown as { __b183?: number }).__b183), "kein Neuladen der Seite").toBe(dokument);
});
