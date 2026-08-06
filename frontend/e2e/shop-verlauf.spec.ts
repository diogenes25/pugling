import { test, expect } from "@playwright/test";
import { vaterLogin, sohnLogin } from "./helpers";

// Eigene, kurze Spec statt eines weiteren Blocks in `full-flow.spec.ts` (B-110): dort liegt der Shop-Teil
// hinter der Klausur-Sequenz und läuft, solange B-109 offen ist, überhaupt nicht mit. Eine Fläche, die
// hinter einem fremden Schritt hängt, ist keine geprüfte Fläche.
//
// Geprüft wird der Weg, den kein Komponententest tragen kann (`frontend/CLAUDE.md`: kein nachgebauter
// Bildschirm mit gefälschtem `fetch`) – und zwar in genau DIESER Reihenfolge: Verlauf ansehen → kaufen →
// Verlauf noch einmal ansehen. Nur so tritt der Fehler auf, denn der Client holte den einmal geladenen
// Verlauf nie wieder, und der eigene Kauf fehlte für den Rest der Sitzung.

const FATHER = { id: "1", pin: "0000" };
const CHILD = { id: "1", pin: "1111" };

test("Der eigene Kauf steht im Verlauf, auch wenn der Verlauf vorher schon offen war", async ({ browser }) => {
  // ---------- VATER: Münzen verschenken, damit im Shop überhaupt etwas leistbar ist ----------
  // Der Weg übers Verschenken statt übers Spielen ist Absicht: diese Spec prüft den Shop, nicht die
  // Punktevergabe – und ein kurzer Aufbau hält sie unabhängig vom Übungs-Teil des Durchstichs.
  const vaterCtx = await browser.newContext();
  const vater = await vaterCtx.newPage();
  await vaterLogin(vater, FATHER);
  await vater.goto(`/vater/konto?childId=${CHILD.id}`);
  // Das Kind per Id wählen, nie per Position: ein von einem anderen Spec angelegtes Kind verschöbe den Index.
  const kindSelect = vater.getByRole("combobox", { name: "Kind" });
  await expect(kindSelect.locator("option")).not.toHaveCount(0);
  await kindSelect.selectOption(CHILD.id);
  await vater.locator("#grant-amount").fill("500");
  await vater.getByRole("button", { name: "Verschenken" }).click();
  await expect(vater.getByText(/Münzen verschenkt/)).toBeVisible();

  // ---------- SOHN ----------
  const sohnCtx = await browser.newContext();
  const sohn = await sohnCtx.newPage();
  // Kauf bestätigt per window.confirm → im E2E annehmen (Playwright verwirft den Dialog sonst).
  sohn.on("dialog", (d) => d.accept());
  await sohnLogin(sohn, CHILD);
  // Auf die angemeldete Arcade warten, BEVOR navigiert wird: `sohnLogin` klickt „▶ LOS" nur an und wartet
  // sein Ergebnis nicht ab – ein sofortiges `goto` bricht den laufenden Login ab und landet wieder auf der
  // Anmeldung. Die untere Navigation ist dafür der stabile Anker (sie hängt an keinem Plan, anders als
  // „Tagesmission": ohne Lehrplan steht dort „Noch keine Mission").
  await expect(sohn.locator("nav.sohn-nav")).toBeVisible();
  await sohn.goto("/sohn/shop");
  await expect(sohn.locator(".screen-title", { hasText: "Shop" })).toBeVisible();

  // Verlauf ZUERST öffnen – damit hat der Client ihn als „geladen" vermerkt, und das ist die Vorbedingung
  // von B-110. Gezählt statt auf „leer" geprüft: der Durchstich (`full-flow.spec.ts`) kauft in derselben
  // Wegwerf-DB für dasselbe Kind, seit B-109 behoben ist und sein Shop-Block wieder mitläuft. „Leer" war
  // nie eine Eigenschaft dieser Spec, sondern eine Nebenwirkung eines fremden Ausfalls.
  await sohn.getByRole("button", { name: "Verlauf" }).click();
  const verlaufZeilen = sohn.locator(".list .row");
  // Auf den geladenen Verlauf warten, leer ODER gefüllt: sonst zählt der nächste Schritt einen Zustand,
  // den der Client noch gar nicht geholt hat, und der Vergleich am Ende ginge gegen 0 statt gegen den Ist-Stand.
  await expect(sohn.getByText(/Noch nichts gekauft/).or(verlaufZeilen.first())).toBeVisible();
  const zeilenVorher = await verlaufZeilen.count();

  // Dann kaufen: die erste leistbare Belohnung auf Lager (nicht `.locked`).
  await sohn.getByRole("button", { name: "Kaufen" }).click();
  const buyCard = sohn.locator("button.skin:not(.locked)").first();
  await expect(buyCard).toBeVisible();
  const titel = (await buyCard.locator(".nm").innerText()).trim();
  // B-49: `useAction`s Ref-Gate muss einen Doppelklick abfangen - genau die Sperre, die vorher fehlte.
  const purchasePosts: string[] = [];
  sohn.on("request", (r) => {
    if (r.method() === "POST" && /\/shop\/listings\/\d+\/purchase$/.test(r.url())) purchasePosts.push(r.url());
  });
  await buyCard.dblclick();
  await expect(sohn.locator(".cel-title", { hasText: "GEKAUFT!" })).toBeVisible();
  expect(purchasePosts, "Ein Doppelklick darf genau einen Kauf auslösen").toHaveLength(1);

  // Und jetzt muss der Verlauf ihn zeigen. Vor B-110 stand hier weiter der zuerst geladene Stand.
  // Die ZEILENZAHL trägt die Aussage, nicht der Titel: beide Specs greifen dieselbe erste Karte, der Titel
  // kann also schon vom fremden Kauf dastehen und würde einen ausgebliebenen Nachschlag nicht bemerken.
  await sohn.getByRole("button", { name: "Verlauf" }).click();
  await expect(sohn.getByText(/Noch nichts gekauft/)).toHaveCount(0);
  await expect(verlaufZeilen).toHaveCount(zeilenVorher + 1);
  await expect(sohn.getByText(titel, { exact: false }).first()).toBeVisible();
});
