import { test, expect } from "@playwright/test";
import { vaterLogin, sohnLogin } from "./helpers";

// B-50: Das Kind beschreibt sich selbst. Eigene, kurze Spec statt eines Blocks im Durchstich
// (`frontend/CLAUDE.md`: eine Fläche je Spec) – der Durchstich bleibt EIN Weg.
//
// Geprüft wird genau das, was kein Komponententest tragen kann: dass der Kind-Schreibweg wirklich am
// Server ankommt und der Vater ihn an seiner gewohnten Stelle sieht. Dazu die zwei Hälften von
// Akzeptanzkriterium 3, die beide nur über einen echten Seitenwechsel sichtbar werden – fortsetzbar
// (ein erneuter Besuch zeigt den gespeicherten Stand) und abbrechbar (Verlassen ohne Speichern ändert nichts).

const FATHER = { id: "1", pin: "0000" };
// Das geseedete Kind trägt bereits Interessen (Brawl Stars, Pokémon, Fußball) mit Gewicht 2 – genau der
// Ausgangszustand, an dem sich „fortsetzbar" überhaupt zeigen lässt.
const CHILD = { id: "1", pin: "1111" };

test("Das Kind hebt ein Interesse an, und der Vater sieht es", async ({ browser }) => {
  const sohnCtx = await browser.newContext();
  const sohn = await sohnCtx.newPage();
  await sohnLogin(sohn, CHILD);
  // Auf die angemeldete Arcade warten, BEVOR navigiert wird: `sohnLogin` klickt „▶ LOS" nur an und wartet
  // sein Ergebnis nicht ab – ein sofortiges `goto` bräche den laufenden Login ab.
  await expect(sohn.locator("nav.sohn-nav")).toBeVisible();

  // Der dauerhafte Menüpunkt (Akzeptanzkriterium 7) – nicht per Direkt-URL, sondern über den Weg,
  // den das Kind tatsächlich hat.
  await sohn.goto("/sohn/konto");
  await sohn.getByRole("link", { name: /Das bin ich/ }).click();
  await expect(sohn.locator(".screen-title", { hasText: "Das bin ich" })).toBeVisible();

  // Fortsetzbar: der zuletzt gespeicherte Stand steht da, nicht ein leeres Formular.
  const pokemon = sohn.getByRole("button", { name: /Pokémon/ });
  await expect(pokemon).toHaveAttribute("aria-pressed", "true");
  await expect(pokemon).toContainText("mag ich");

  // Antippen hebt es eine Stufe an – und diese Änderung wird bewusst NICHT geschickt.
  await pokemon.click();
  await expect(pokemon).toContainText("Lieblingsthema");

  // Abbrechbar: verlassen ohne zu speichern lässt den Server unberührt.
  await sohn.getByRole("button", { name: "Abbrechen" }).click();
  await sohn.goto("/sohn/ich");
  await expect(sohn.getByRole("button", { name: /Pokémon/ })).toContainText("mag ich");

  // Jetzt derselbe Griff, diesmal bis zum Ende des geführten Prozesses.
  await sohn.getByRole("button", { name: /Pokémon/ }).click();
  await expect(sohn.getByRole("button", { name: /Pokémon/ })).toContainText("Lieblingsthema");
  for (let schritt = 0; schritt < 3; schritt++) {
    await sohn.getByRole("button", { name: "Weiter" }).click();
  }
  await sohn.getByRole("button", { name: "Fertig" }).click();
  // Die Bestätigung liegt in der Shell, nicht im Bildschirm (B-151) - sie überlebt den Sprung zurück.
  await expect(sohn.locator(".cel-title", { hasText: "GESPEICHERT!" })).toBeVisible();

  // ---------- VATER: dieselbe Angabe an der gewohnten Stelle ----------
  const vaterCtx = await browser.newContext();
  const vater = await vaterCtx.newPage();
  await vaterLogin(vater, FATHER);
  await vater.goto(`/vater/kind/${CHILD.id}`);
  await expect(vater.getByRole("heading", { name: "Interessen" })).toBeVisible();
  // Der Wert des Auswahlfelds trägt die Aussage: 3 = Lieblingsthema, gesetzt vom Kind.
  await expect(vater.getByRole("combobox", { name: "Gewichtung für Pokémon" })).toHaveValue("3");
});
