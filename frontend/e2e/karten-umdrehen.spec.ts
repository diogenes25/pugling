import { test, expect, type Page } from "@playwright/test";
import { createChild, sohnLogin, vaterLogin } from "./helpers";

// B-35: beim Aufdecken dreht sich die Karte um – in der Übung („Umdrehen") und in der Klausur („Aufdecken").
// Eine eigene Spec statt eines Blocks im Durchstich (frontend/CLAUDE.md, „Eine Fläche je E2E-Spec"): eine
// rein optische Zusicherung darf den Vater→Sohn-Weg nicht mitreißen.
// Eigenes Kind, weil je Kind genau ein aktiver Plan spielbar ist und `full-flow` Kind #1 belegt.

const FATHER = { id: "1", pin: "0000" };
const EXERCISE = "Vokabeln: En ville";
const CHILD = { name: "Wende-Kind", pin: "2468" };

/** Gelesen wird die berechnete Animation, nicht nur die Klasse: ohne die CSS-Regel hinge die Klasse leer. */
const animationOf = (page: Page, selector: string) =>
  page.locator(selector).evaluate((el) => getComputedStyle(el).animationName);

test("Die Karte dreht sich beim Aufdecken um – beim Üben und in der Klausur", async ({ browser }) => {
  // ---------- VATER: frisches Kind mit einem Plan über die Vokabel-Übung ----------
  const vater = await (await browser.newContext()).newPage();
  await vaterLogin(vater, FATHER);
  const childId = await createChild(vater, CHILD.name, CHILD.pin);

  await vater.goto("/vater/plaene");
  await vater.getByRole("link", { name: /Neuer Plan/ }).click();
  const kindSelect = vater.getByRole("combobox", { name: "Kind" });
  await expect(kindSelect.locator("option")).not.toHaveCount(0);
  await kindSelect.selectOption(childId);
  await vater.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(vater).toHaveURL(/\/vater\/plan\/\d+$/);
  const exRadio = vater.getByRole("radio", { name: new RegExp(EXERCISE) });
  await expect(exRadio).toBeVisible();
  await exRadio.check();
  await vater.locator('select[aria-label="Ziel-Rhythmus"]').selectOption("Daily");
  await vater.getByRole("checkbox", { name: /Leitner/ }).check();
  await vater.getByRole("button", { name: /Position hinzufügen/ }).click();
  await expect(vater.getByRole("row", { name: new RegExp(EXERCISE) })).toBeVisible();

  // ---------- SOHN: Üben ----------
  const sohn = await (await browser.newContext()).newPage();
  await sohnLogin(sohn, { id: childId, pin: CHILD.pin });
  await sohn.getByRole("link", { name: /ÜBEN/ }).click();
  const counter = sohn.locator(".pill.cyan", { hasText: /Karte \d+ \/ \d+/ });
  await expect(counter).toBeVisible();
  const total = Number((await counter.textContent())!.match(/\/ (\d+)/)![1]);
  expect(total, "zwei Karten: eine mit, eine ohne Bewegung").toBeGreaterThanOrEqual(2);

  for (let i = 0; i < total; i++) {
    await expect(counter).toHaveText(new RegExp(`Karte ${i + 1} / `));
    // AK 3: bei „Bewegung reduzieren" dreht sich nichts – das globale Gate in index.css nimmt die Animation.
    if (i === 1) await sohn.emulateMedia({ reducedMotion: "reduce" });
    await expect(sohn.locator(".fcard")).not.toHaveClass(/\bflipped\b/);
    await sohn.getByRole("button", { name: "Umdrehen 🔄" }).click();
    if (i < 2) {
      // Am Element selbst, nicht an einem neu gemounteten (B-109): dieselbe `.fcard` trägt die Wende.
      await expect(sohn.locator(".fcard")).toHaveClass(/\bflipped\b/);
      expect(await animationOf(sohn, ".fcard")).toBe(i === 0 ? "card-flip" : "none");
    }
    if (i === 1) await sohn.emulateMedia({ reducedMotion: null });
    await sohn.getByRole("button", { name: "Gewusst!" }).click();
  }
  await expect(sohn.getByText("RUNDE FERTIG!")).toBeVisible();

  // ---------- SOHN: Klausur (Selbsteinschätzung) ----------
  await sohn.getByRole("button", { name: /Weiter zum Test/ }).click();
  await expect(sohn.locator(".pill.cyan", { hasText: /Frage 1 \/ \d+/ })).toBeVisible();
  // Kein `if (count)`: fehlte das Aufdecken, prüfte diese Spec in der Klausur sonst still nichts.
  await expect(sohn.locator(".card.flipped")).toHaveCount(0);
  await sohn.getByRole("button", { name: "Aufdecken 🔄" }).click();
  await expect(sohn.locator(".card.flipped")).toHaveCount(1);
  expect(await animationOf(sohn, ".card.flipped")).toBe("card-flip");
  // Die Bewertung bleibt erreichbar, während die Karte sich dreht (Entscheidung 5).
  await expect(sohn.getByRole("button", { name: "Gewusst", exact: true })).toBeEnabled();
});
