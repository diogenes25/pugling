---
tags: [typ/story, status/abgenommen, bereich/shop, rolle/supervisor]
aliases: [SetStock-Antwort veraltet]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Nachschau Sprint 96 (08fd05a6) in Sprint 97, 2026-11-20"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-645]
nachgeschaut: "2026-11-21"
---

# B-772 · Die Antwort auf „Bestand setzen" zeigt die Geschwister mit veraltetem Bestand

`ShopService` materialisiert beim Setzen des Bestands nur die Zeile des einen Kindes (`ShopService.cs:146`), gibt aber
über `ToView` die Bestände **aller** berechtigten Kinder zurück (`:151`): Geschwister erscheinen ohne ihre fällige
Auffüllung, und ein Geschwister ohne Zeile fehlt ganz. Das Vater-Web lädt danach ohnehin neu (`VaterShop.tsx:356`) und
zeigt richtig; falsch liest es nur ein Client oder Agent, der der Antwort traut. Gefunden in der Nachschau von Sprint 96.

## User Story

Als Client oder Agent des Vaters möchte ich der Antwort auf „Bestand setzen“ trauen können, damit ich nicht noch einmal
laden muss, um die Bestände der Geschwister richtig zu sehen.

## Ist-Stand am Code (2026-11-21 nachgelesen)

- `ShopService.SetStockAsync` (`ShopService.cs:135-152`): `MaterializeStocksAsync([listing], _ => [childId], …)` nur für das
  eine Kind, danach `ToView(listing, linked, nowUtc)` über **alle** berechtigten Kinder.
- `ToView` zeigt nur vorhandene Zeilen (`ShopService.cs` `ToView`, `listing.Stocks.Where(eligible)`): Ein Geschwister ohne
  Zeile fehlt, eines mit fälliger Auffüllung steht auf dem alten Wert.
- Der PATCH antwortet richtig über `ToViewAsync` (`ShopController.cs:328`): Es materialisiert alle berechtigten Kinder zur
  Anzeige, nach dem Speichern, ohne erneut zu speichern.

## Entscheidungen (autonom gegrillt, `art: Defekt`, Nachtlauf 2026-11-21)

1. **Die Antwort entsteht über `ToViewAsync`**, wie beim PATCH. Begründung: eine Senke für die Vater-Sicht eines geladenen
   Angebots, statt einer zweiten Regel.
2. **Geschrieben wird weiter nur der Bestand des einen Kindes.** Die Geschwister werden zur Anzeige berechnet, nicht
   gespeichert. Begründung: Ein breiterer Schreibsatz würde jeden gleichzeitigen Kauf eines Geschwisters zum 409 für den
   Vater machen. Die Liste und der Kauf schreiben dieselbe Auffüllung ohnehin beim nächsten Lesen.

## Akzeptanzkriterien

1. Zwei Kinder, tägliches Angebot, beide ausverkauft. Am nächsten Tag setzt der Vater den Bestand des einen: Die Antwort
   zeigt das Geschwister aufgefüllt. *Rot, wenn die Antwort weiter über `ToView` ohne Materialisieren entsteht.*
2. Ein Geschwister ohne Bestandszeile erscheint in der Antwort mit seinem Startbestand. *Rot wie AK 1.*
3. Das Setzen schreibt keine Zeile eines Geschwisters: Die Zahl der Bestandszeilen ist nach dem Setzen dieselbe wie davor
   plus höchstens die des einen Kindes. *Rot, wenn vor dem Speichern alle Kinder materialisiert werden.*

## Angriffsplan

`SetStockAsync`: nach `TrySaveAsync` `ToViewAsync(supervisorId, listing, nowUtc, ct)` statt `ToView(…)`. Tests in
`ShopStockPerChildTests.cs` neben `Bestand_Setzen_Prueft_Grenze_Zielgruppe_Und_Kind`, mit `StockRowsAsync` für AK 3.

## Schätzung

**XS**, backend. Eine Zeile, drei Fälle. Kein Vertragsbruch: Die Antwort hat dieselbe Form, nur richtige Werte.

## Verlauf

- **2026-11-20** — angelegt aus der Nachschau von Sprint 96 (`idee`, `entgangen_bei: [B-645]`).
- **2026-11-21** — ausformuliert, gegrillt und geschätzt im Nachtlauf (Sprint 100, autonom, `art: Defekt`): Fundstellen
  nachgelesen, zwei Entscheidungen (Senke `ToViewAsync`, Schreibsatz bleibt beim einen Kind), drei AK; XS.
- **2026-11-21** — in-arbeit (Sprint 100). `SetStockAsync` antwortet nach dem Speichern über `ToViewAsync`.
  **Rote Probe vor dem Fix:** `B772_Bestand_Setzen_Zeigt_Geschwister_Aktuell_Und_Schreibt_Sie_Nicht` erwartet für das
  Geschwister 1, gemessen 0 (:922). Danach grün.
  - AK 1 und AK 2 (Geschwister aufgefüllt, Neuling mit Startbestand) → derselbe Test; M5 (Antwort wieder über `ToView`) → rot.
  - AK 3 (keine Geschwister-Zeile geschrieben, die Zeilenzahl bleibt 2) → derselbe Test; M6 (vor dem Speichern alle
    berechtigten Kinder materialisieren) → rot.

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | M5 | `ShopService.cs:153` | red | red |
  | M6 | `ShopService.cs:146` | red | red |

  Gemessen mit `mutant-probe.py` an `3612afb0 + Arbeitsbaum`.
- **2026-11-21** — abgenommen in Commit `a0b73724` (Sprint 100). Endstand: Backend 1937/1937 (Release). Rollengang E2E 4/4
  auf den Ausweichports, Vater abgenommen, Sohn und Creator als Regressionszeugen. Review: vier 🟢, Zähler 0 von 5
  (docs/pm-sitzung-2026-11-21.md, Sprint 100).
- **2026-11-21** — nachgeschaut (Retro Sprint 101), Diff `a0b73724`, `SetStockAsync:146-153` gelesen:
  - Die Materialisierung vor dem Speichern betrifft nur `childId`.
  - `ToViewAsync` läuft erst nach `TrySaveAsync`.
  - Kein späteres `SaveChanges` im Request: Aktion, Filter und Middleware gegengelesen, wie der Reviewer.
  Kein Befund.
