import { expect, test } from "@playwright/test";
import { createChild, vaterLogin } from "./helpers";

const FATHER = { id: "1", pin: "0000" };
const STAMP = Date.now().toString(36);

/*
 * B-255: der gemeinsame Helfer liefert die Id des eben angelegten Kindes – auch wenn der Name schon da ist und das
 * Dashboard ihn darum in beiden Tabellen und mehrfach trägt. Rot, wenn der Helfer die Zeile über den Namen sucht: beim
 * zweiten Anlegen trifft die Suche mehrere Zeilen (Strict-Mode-Verstoß) oder die Id des ersten Kindes.
 */
test("Ein zweites Kind gleichen Namens bekommt seine eigene Id", async ({ page }) => {
  await vaterLogin(page, FATHER);
  const name = `Doppelname ${STAMP}`;

  const erstes = await createChild(page, name, "4711");
  const zweites = await createChild(page, name, "4712");

  // Beide Kinder tragen denselben, durch STAMP eindeutigen Namen: zwei verschiedene Ids, deren zweite ein Kind dieses
  // Namens öffnet, sind genau die zwei. Die Überschrift allein unterschiede sie nicht.
  expect(zweites).not.toBe(erstes);
  await page.goto(`/vater/kind/${zweites}`);
  await expect(page.getByRole("heading", { name: new RegExp(name) })).toBeVisible();
});
