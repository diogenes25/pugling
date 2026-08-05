import { test, expect } from "@playwright/test";
import { vaterLogin, sohnLogin } from "./helpers";

// End-to-End des vertikalen Durchstichs im Positions-Modell:
//   Vater legt (Web) einen Lehrplan-Container an und hängt eine Katalog-Übung als Position hinein
//   →  Sohn arbeitet (App) die Position ab: Üben (Leitner) + Test  →  Punkte fließen
//   →  Vater sieht den Fortschritt.
// Referenziert wird die system-geseedete Vokabel-Übung "Vokabeln: En ville" (Französisch, Unité 2); sie
// hat keinen Autor (geteilte Übung) und genug Inhalte (5), um den Combo-Meilenstein ×5 auszulösen.
// Beide Rollen laufen in getrennten Browser-Kontexten (isoliertes localStorage).

const FATHER = { id: "1", pin: "0000" };
const CHILD = { id: "1", pin: "1111" };
const EXERCISE = "Vokabeln: En ville";

test("Vater erstellt Plan mit Position, Sohn arbeitet ihn ab, Punkte fließen", async ({ browser }) => {
  // ---------- VATER (Web) ----------
  const vaterCtx = await browser.newContext();
  const vater = await vaterCtx.newPage();
  await vaterLogin(vater, FATHER);

  // Lehrplan = leerer Container (Titel/Kind/Laufzeit sind vorbelegt) → anlegen und auf die Plan-Seite.
  // Der Weg läuft über die Perspektive „Zuweisen": dort liegen die Pläne, und „+ Neuer Plan" ist eine
  // Aktion am Bestand, keine Navigation. Der Klick auf den Umschalter prüft ihn gleich mit.
  await vater.getByRole("link", { name: /Zuweisen/ }).first().click();
  await expect(vater).toHaveURL(/\/vater\/plaene$/);
  await vater.getByRole("link", { name: /Neuer Plan/ }).click();
  await expect(vater.getByRole("heading", { name: /Neuer Lehrplan/ })).toBeVisible();
  // Erst wählbar, wenn die Kinder-Liste geladen ist – sonst schlägt das Anlegen mit "Kind wählen" fehl.
  // Das Kind per Id wählen, nicht per Position: die Liste ist nach Namen sortiert, und ein von einem
  // anderen Spec angelegtes Kind würde sonst still den Index verschieben.
  const kindSelect = vater.getByRole("combobox", { name: "Kind" });
  await expect(kindSelect.locator("option")).not.toHaveCount(0);
  await kindSelect.selectOption(CHILD.id);
  await vater.getByRole("button", { name: /Plan anlegen/ }).click();
  await expect(vater).toHaveURL(/\/vater\/plan\/\d+$/);
  await expect(vater.getByRole("heading", { name: /Übungen im Plan/ })).toBeVisible();
  const planUrl = vater.url();

  // Katalog-Übung als Position hinzufügen: Tagesziel + Leitner-Kasten (so erscheint "ÜBEN" beim Sohn).
  // Die Übung wird im Radiogroup-Katalog per Radio ausgewählt (ohne Filter listet er alle Übungen).
  const exRadio = vater.getByRole("radio", { name: new RegExp(EXERCISE) });
  await expect(exRadio).toBeVisible();
  await exRadio.check();
  await vater.locator('select[aria-label="Ziel-Rhythmus"]').selectOption("Daily");
  await vater.getByRole("checkbox", { name: /Leitner/ }).check();
  await vater.getByRole("button", { name: /Position hinzufügen/ }).click();
  await expect(vater.getByRole("row", { name: new RegExp(EXERCISE) })).toBeVisible();

  // ---------- SOHN (App) ----------
  const sohnCtx = await browser.newContext();
  const sohn = await sohnCtx.newPage();
  await sohnLogin(sohn, CHILD);

  // Basis: Tagesmission sichtbar
  await expect(sohn.getByText("Tagesmission")).toBeVisible();

  // Übung starten und alle fälligen Karten "gewusst" (die Übungs-Kachel navigiert per Link).
  await sohn.getByRole("link", { name: /ÜBEN/ }).click();
  const counter = sohn.locator(".pill.cyan", { hasText: /Karte \d+ \/ \d+/ });
  await expect(counter).toBeVisible();
  // Aus einer laufenden Runde muss ein Ausweg auf dem Schirm stehen (nicht nur Browser-Zurück).
  // Nur Sichtprüfung: ein Klick würde die Runde beenden und den restlichen Durchstich abschneiden.
  await expect(sohn.getByRole("button", { name: "Runde beenden" })).toBeVisible();
  const total = Number((await counter.textContent())!.match(/\/ (\d+)/)![1]);
  // Genug Karten, damit der Combo-Meilenstein ×5 sicher fällt (bewusste Coverage, kein stiller Skip).
  expect(total).toBeGreaterThanOrEqual(5);
  for (let i = 0; i < total; i++) {
    await sohn.getByRole("button", { name: "Umdrehen 🔄" }).click();
    await sohn.getByRole("button", { name: "Gewusst!" }).click();
  }
  // Motivations-Feature: ab 5 Treffern in Folge feiert die App den Combo-Meilenstein (Feier-Banner).
  await expect(sohn.locator(".cel-title", { hasText: "COMBO ×5" })).toBeVisible();
  await expect(sohn.getByText("RUNDE FERTIG!")).toBeVisible();

  // Weiter zum Test
  await sohn.getByRole("button", { name: /Weiter zum Test/ }).click();
  await expect(sohn.locator(".screen-title", { hasText: "Test" })).toBeVisible();

  // Klausur ist strikt server-getrieben: eine Frage nach der anderen (kein Zurück). Getippte Stufen tragen ein
  // Eingabefeld, die Selbsteinschätzung erst "Aufdecken" und dann "Gewusst" – die letzte Antwort schließt den
  // Test automatisch ab (kein Sammel-"Abgeben" mehr).
  const testCounter = sohn.locator(".pill.cyan", { hasText: /Frage \d+ \/ \d+/ });
  await expect(testCounter).toBeVisible();
  const testTotal = Number((await testCounter.textContent())!.match(/\/ (\d+)/)![1]);

  const answerOne = async () => {
    const revealBtn = sohn.getByRole("button", { name: "Aufdecken 🔄" });
    if (await revealBtn.count()) {
      // Erst denken, dann aufdecken: vor dem Aufdecken darf die Bewertung NICHT stehen, sonst liest das Kind
      // die Lösung und tippt "Gewusst" – die Selbsteinschätzung wäre wertlos.
      await expect(sohn.getByRole("button", { name: "Gewusst", exact: true })).toHaveCount(0);
      await revealBtn.first().click();
    }
    await sohn.getByRole("button", { name: "Gewusst", exact: true }).click();
  };

  // Erste Frage beantworten, dann die Klausur VERLASSEN und wieder betreten: der Versuch wird am Cursor
  // fortgesetzt (der Server gibt den laufenden zurück) – er fängt nicht bei Frage 1 neu an und verbraucht
  // keinen zweiten der begrenzten Versuche.
  await answerOne();
  await expect(testCounter).toHaveText(`Frage 2 / ${testTotal}`);
  await sohn.getByRole("button", { name: "Später weiter" }).click();
  await expect(sohn.getByText("Tagesmission")).toBeVisible();
  // Auf die Positions-Karte der Übung gescoped, nicht global: jede testbare Karte trägt genau einen
  // TEST-Link, aber sobald ein Plan eine zweite testbare Position bekommt, träfe ein ungescopter Locator
  // zwei Treffer und Playwrights Strict-Mode bräche ab (B-62) - heute geht das nur gut, weil dieser Plan
  // nur eine Position trägt.
  await sohn.locator(".card", { hasText: EXERCISE }).getByRole("link", { name: /TEST/ }).click();
  await expect(testCounter).toHaveText(`Frage 2 / ${testTotal}`);

  for (let i = 1; i < testTotal; i++) await answerOne();
  await expect(sohn.locator(".vtitle", { hasText: "SIEG!" })).toBeVisible();

  // Wallet: Münzen wurden gutgeschrieben (Test bestanden + ggf. Leitner-Übung)
  await sohn.goto("/sohn/skins");
  const coins = sohn.locator(".chip", { hasText: "🪙" }).first();
  await expect(coins).toBeVisible();
  // Der HUD lädt die Wallet nach dem Reload asynchron nach (Start bei 0) – auf eine positive Zahl warten.
  await expect(coins).toContainText(/[1-9]/);
  const balance = Number((await coins.textContent())!.replace(/\D/g, ""));
  expect(balance).toBeGreaterThan(0);

  // ---------- SOHN: Familien-Shop (einziger Münz-Ausgabeweg) ----------
  // Verdiente Münzen gegen eine echte Belohnung eintauschen. Kauf/Einlösen bestätigen per window.confirm
  // → im E2E den Dialog annehmen (Playwright verwirft ihn sonst automatisch).
  sohn.on("dialog", (d) => d.accept());
  await sohn.goto("/sohn/shop");
  await expect(sohn.locator(".screen-title", { hasText: "Shop" })).toBeVisible();
  // Erste kaufbare Belohnung (leistbar + auf Lager → nicht ".locked") kaufen.
  const buyCard = sohn.locator("button.skin:not(.locked)").first();
  await expect(buyCard).toBeVisible();
  await buyCard.click();
  // Der Kauf wird gefeiert; danach liegt die Ware im Inventar-Tab "Sachen" (Einlösen möglich).
  await expect(sohn.locator(".cel-title", { hasText: "GEKAUFT!" })).toBeVisible();
  await sohn.getByRole("button", { name: /^Sachen/ }).click();
  await expect(sohn.getByRole("button", { name: "Einlösen beantragen" }).first()).toBeVisible();
  // Kaufhistorie (B-99): der eben getätigte Kauf ist im Verlauf-Tab sichtbar - der Tab existierte vorher
  // gar nicht, die Käufe lagen unerreichbar im Bundle.
  await sohn.getByRole("button", { name: "Verlauf" }).click();
  await expect(sohn.locator(".list .row").first()).toBeVisible();

  // ---------- VATER sieht Fortschritt ----------
  await vater.goto(planUrl);
  await expect(vater.getByText("Punkte gesamt")).toBeVisible();
  // Tagesverlauf-Tabelle zeigt den heute erledigten Tag des Sohns (Ziel erfüllt → "komplett").
  await expect(vater.locator("table .pill.lime", { hasText: "komplett" }).first()).toBeVisible();
  // Punkte gesamt > 0 (Übung + Test sind beim Vater angekommen).
  const totalCard = vater.locator(".vater-grid .card").first();
  await expect(totalCard).toContainText("Punkte gesamt");
  expect(Number((await totalCard.textContent())!.replace(/\D/g, ""))).toBeGreaterThan(0);

  // ---------- VATER liest den Positions-Report (Vater-only) ----------
  // Der Report liegt unter `supervisor/…` und ist rollen-gegated, weil jede Zeile die Lösung trägt – auch
  // für Karten, die das Kind nie gesehen hat. Ohne diesen Klick prüft *nichts* im Frontend den Pfad: er ist
  // ein Template-String, ein falsches Ebenen-Präfix ginge durch `tsc` und `vite build` still hindurch.
  await vater.getByRole("row", { name: new RegExp(EXERCISE) }).getByRole("button", { name: /Report/ }).click();
  // Die Kopfzeile rendert nur bei 200 – bei 403/404 stünde hier stattdessen ein `.banner err`.
  await expect(vater.getByText(/eingeführt · .* sitzen sicher/)).toBeVisible();
  // Spalte „Lösung" gefüllt: der Vater darf sie sehen (Akzeptanzkriterium „sein UI verliert keine Spalte").
  // Erst auf die Report-Tabelle einengen: sie steht *innerhalb* der Positionszeile, und ohne die Einengung
  // trifft `row` auch die umschließende Zeile – deren einzige Zelle die ganze Tabelle enthält, womit `nth(1)`
  // eine Spalte zu weit links landet. Die Zeile wird über ihren Inhalt gewählt, nie über einen Index.
  const report = vater.locator("table table");
  await expect(report.getByRole("columnheader", { name: "Lösung" })).toBeVisible();
  await expect(report.getByRole("row", { name: /la ville/ }).getByRole("cell").nth(1)).toHaveText("die Stadt");

  // ---------- VATER sieht den plan-übergreifenden Lernstand ----------
  // Die Gegenprobe zum Positions-Report: hier zählt das *Wort* über alle Übungen, nicht die Position.
  // Nur nach echtem Üben gefüllt – deshalb steht die Prüfung am Ende dieses Durchstichs.
  await vater.goto(`/vater/kind/${CHILD.id}/lernstand`);
  await expect(vater.getByRole("heading", { name: /^Wörter/ })).toBeVisible();
  // Alles „gewusst" geklickt → keine schwachen Wörter; ohne Filter müssen die geübten Wörter auftauchen.
  await vater.getByRole("checkbox", { name: /nur schwache/ }).uncheck();
  await expect(vater.locator("table tbody tr").first()).toBeVisible();
  await expect(vater.locator("table .pill", { hasText: /%/ }).first()).toBeVisible();

  // Katalog-Drilldown: das Fach der geübten Übung ist über den aktiven Plan zugewiesen.
  await vater.getByRole("radio", { name: /Nach Katalog/ }).click();
  await expect(vater.locator(".card .pill.lime", { hasText: "aktiv" }).first()).toBeVisible();

  await vaterCtx.close();
  await sohnCtx.close();
});
