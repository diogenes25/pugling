import { expect, test } from "@playwright/test";
import { vaterLogin } from "./helpers";

/*
 * B-159. Bei JEDER nicht eigenen Reihe stand „Diese Reihe hat jemand anderes angelegt" – auch bei einer aus
 * dem Grundbestand, die niemandem gehört. Im Seed sind das alle Reihen, also las jeder Creator beim ersten
 * Aufklappen einen Eigentümer, den es nicht gibt. Der Satz selbst ist im Komponententest belegt
 * (`VaterLehrwerke.test.tsx`); diese Spec belegt die VERDRAHTUNG in der Zeile – ohne sie bliebe 394/394 grün,
 * wenn jemand den Hinweis aus `SeriesRow` streicht (Reviewer-Fund, Nachtlauf 2026-10-23).
 *
 * Eigene Datei statt eines Blocks in `lehrwerke.spec.ts` (eine Fläche je Spec). Das ist zugleich der
 * Rollengang aus der Creator-Rolle – die Chrome-Extension war nicht verbunden (Freigabe 6).
 */

const FATHER = { id: "1", pin: "0000" };

test("Eine Reihe aus dem Grundbestand nennt niemanden als Ersteller", async ({ page }) => {
  await vaterLogin(page, FATHER);
  await page.goto("/vater/lehrwerke");

  // „Green Line 1" kommt aus dem Seed ohne Eigentümer; der Vater ist Creator, aber nicht ihr Owner.
  const zeile = page.getByRole("row", { name: /Green Line 1/ });
  await expect(zeile).toBeVisible();
  await zeile.getByRole("button", { name: "Units", exact: true }).click();

  await expect(page.getByText(/gehört zum Grundbestand/)).toBeVisible();
  // Der falsche Satz darf nicht bloß fehlen, er darf gar nicht entstehen.
  await expect(page.getByText(/hat jemand anderes angelegt/)).toHaveCount(0);
});
