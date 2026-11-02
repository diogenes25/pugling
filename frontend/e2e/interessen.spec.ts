import { test, expect, type Page } from "@playwright/test";
import { createChild, vaterLogin } from "./helpers";

// B-50: Das Kind beschreibt sich selbst. Eigene, kurze Spec statt eines Blocks im Durchstich
// (`frontend/CLAUDE.md`: eine Fläche je Spec) – der Durchstich bleibt EIN Weg.
//
// Mit einem **eigenen** Kind statt dem geseedeten: nur so ist der Erstbesuch (AK 7) überhaupt zu sehen,
// denn der geseedete Sohn trägt bereits Interessen. Nebenbei macht es die Spec wiederholbar – sie hängt
// an keinem Seed-Zustand und zerstört keinen.
//
const FATHER = { id: "1", pin: "0000" };
const CHILD = { name: "Selbstbild-Kind", pin: "5150" };

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

test("Das Kind beschreibt sich selbst, und der Vater sieht es", async ({ browser }) => {
  // ---------- VATER: ein frisches Kind, damit der Erstbesuch echt ist ----------
  const vaterCtx = await browser.newContext();
  const vater = await vaterCtx.newPage();
  await vaterLogin(vater, FATHER);
  const childId = await createChild(vater, CHILD.name, CHILD.pin);

  // Der Sohn-Kontext fährt unter **„Bewegung reduzieren"** – die härtere Bedingung und der Grund, warum
  // die Bestätigung nach dem Speichern nicht die Feier sein darf: `.cel-layer` ist dort `display: none`
  // (und ohnehin `aria-hidden`). Genau daran fiel die erste Fassung. Die Option steht hier und nicht in
  // einem `test.use()`: die gilt den eingebauten Fixtures, dieser Kontext entsteht von Hand.
  const sohnCtx = await browser.newContext({ reducedMotion: "reduce" });
  const sohn = await sohnCtx.newPage();
  await sohnLogin(sohn, childId);

  // ---------- AK 7: der Erstbesuch wird von selbst angeboten ----------
  await sohn.getByRole("link", { name: /Los geht's/ }).click();
  await expect(sohn.locator(".screen-title", { hasText: "Das bin ich" })).toBeVisible();

  // Erster Schritt: ein Thema auf „mag ich", ein zweiter Tipp auf „Lieblingsthema".
  const kachel = sohn.getByRole("button").filter({ hasText: "auswählen" }).first();
  const thema = (await kachel.locator(".nm").innerText()).trim();
  await kachel.click();
  await expect(sohn.getByRole("button", { name: new RegExp(thema) })).toContainText("mag ich");

  // Abbrechbar: verlassen ohne zu speichern lässt den Server unberührt.
  await sohn.getByRole("button", { name: "Abbrechen" }).click();
  await sohn.goto("/sohn/ich");
  await expect(sohn.getByRole("button", { name: new RegExp(thema) })).toContainText("auswählen");

  // Jetzt derselbe Griff bis ans Ende des geführten Prozesses, samt Freitext-Wunsch.
  await sohn.getByRole("button", { name: new RegExp(thema) }).click();
  await sohn.getByRole("button", { name: new RegExp(thema) }).click();
  await expect(sohn.getByRole("button", { name: new RegExp(thema) })).toContainText("Lieblingsthema");
  for (let schritt = 0; schritt < 3; schritt++) {
    await sohn.getByRole("button", { name: "Weiter" }).click();
  }
  await sohn.locator("#wish").fill("Wellensittiche");
  await sohn.getByRole("button", { name: "Fertig" }).click();

  // Die Bestätigung liegt in der Meldungsstelle der Shell (B-151) – sie überlebt den Sprung zurück UND
  // „Bewegung reduzieren". Die Feier tut hier beides nicht.
  const bestaetigung = sohn.getByText(/Gespeichert – deine Übungen passen sich an/);
  await expect(bestaetigung).toBeVisible();
  await expect(bestaetigung.locator("xpath=ancestor-or-self::*[@aria-hidden='true']")).toHaveCount(0);

  // Fortsetzbar: der gespeicherte Stand ist beim nächsten Besuch der Ausgangspunkt …
  await sohn.goto("/sohn/ich");
  await expect(sohn.getByRole("button", { name: new RegExp(thema) })).toContainText("Lieblingsthema");
  // … und das Erstbesuch-Angebot ist eingelöst.
  await sohn.goto("/sohn");
  await expect(sohn.getByRole("link", { name: /Los geht's/ })).toHaveCount(0);

  // ---------- VATER: dieselbe Angabe an der gewohnten Stelle ----------
  await vater.goto(`/vater/kind/${childId}`);
  await expect(vater.getByRole("heading", { name: "Interessen", exact: true })).toBeVisible();
  // Der Wert des Auswahlfelds trägt die Aussage: 3 = Lieblingsthema, gesetzt vom Kind.
  await expect(vater.getByRole("combobox", { name: `Gewichtung für ${thema}` })).toHaveValue("3");
  // Und der Freitext-Wunsch landet sichtbar im Kind-Profil – kein geheimer Kanal. Es ist dasselbe Feld,
  // das der Vater ohnehin pflegt (`Child.Interests`), kein eigener Ablageort.
  await expect(vater.locator("#kd-interests")).toHaveValue(/Wellensittiche/);
});
