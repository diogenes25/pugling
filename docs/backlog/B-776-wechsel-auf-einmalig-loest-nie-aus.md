---
tags: [typ/story, status/abgenommen, bereich/shop, rolle/supervisor, rolle/student]
aliases: [Wechsel auf einmalig löst nie aus]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Nachschau Sprint 97 / B-771 im Nachtlauf 2026-11-21 (pugling-reviewer), docs/pm-sitzung-2026-11-21.md"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-771]
nachgeschaut: "2026-11-21"
---

# B-776 · Ein Angebot, das von wiederkehrend auf „einmalig“ wechselt, füllt vorhandene Bestände nie auf

`DueRefills(Once, …)` zahlt nur aus, wenn der Bestand **keinen** Auffüll-Zeitpunkt hat
(`backend/Pugling.Api/Services/Shared/ShopRefillSchedule.cs:24`). Der PATCH lässt bei einem Wechsel auf „einmalig“ die
Bestände bewusst unangetastet (B-771 Entscheidung 2, Kommentar `ShopController.cs:291-292`: „Once keeps its empty
instant“). Das gilt aber nur für einen Bestand, der unter „keine“ entstand. Ein Bestand aus einer täglichen, wöchentlichen
oder monatlichen Zeit hat immer einen Zeitpunkt, und der Termin feuert dann nie.

## User Story

Als Vater möchte ich ein Angebot auf „einmalig zum Termin“ umstellen oder ihm einen neuen Termin geben können, damit jedes
Kind zum Termin wirklich aufgefüllt wird, auch wenn sein Bestand unter einer anderen Regel entstand.

## Ist-Stand am Code (2026-11-21 nachgelesen)

- `ShopRefillSchedule.DueRefills` (`ShopRefillSchedule.cs:24-25`): für `Once` nur `1`, wenn `nowUtc >= onceAtUtc` **und**
  `lastUtc is null`.
- `ShopController.UpdateListing` (`ShopController.cs:293-317`): Neustart der Uhr nur für wiederkehrende Arten
  (`IsRecurring`). Ein Wechsel auf `Once` und ein neuer `RefillAtUtc` lassen `LastRefilledAtUtc` aller Bestände stehen.
- Ein Bestand aus `Daily`/`TwiceDaily`/`Weekly`/`Monthly` hat immer einen Zeitpunkt: `StartStock` gibt `nowUtc` zurück
  (`ShopRefillSchedule.cs:99`), `ApplyDueRefill` setzt ihn (`ShopService.cs:590`). Ebenso ein schon gefeuertes `Once`.
- `NextRefillAtUtc(Once, …)` zeigt bei gesetztem Zeitpunkt `null` (`ShopRefillSchedule.cs:49`): der Vater liest „keine
  weitere Auffüllung“.
- Ein Geschwister ohne Zeile startet über `StartStock` ohne Zeitpunkt (`ShopRefillSchedule.cs:97`) und wird zum Termin
  aufgefüllt. Zwei Kinder desselben Angebots laufen auseinander, gegen B-645 AK 10.

**Szenario am Code nachgerechnet:** Angebot „täglich“, `MaxStock 2`, das Kind kauft am 20.11. alles. Der Vater stellt am
20.11. auf „einmalig zum 25.11.“. Am 25.11. ergibt `DueRefills` 0, der Bestand bleibt 0.

## Entscheidungen (autonom gegrillt, `art: Defekt`, Nachtlauf 2026-11-21)

1. **Wechsel auf „einmalig“ oder neuer Termin leert den Zeitpunkt aller Bestände**, nachdem die alte Regel gebucht ist,
   was sie noch schuldet. Begründung: „einmalig“ feuert genau auf einem leeren Zeitpunkt. Ein neuer Termin ist ein neues
   Ereignis. So steht jeder vorhandene Bestand dort, wo `StartStock` ein neues Geschwister hinstellt (B-645 AK 10).
   Kosten: ein Bestand, der unter dem alten Termin schon aufgefüllt war, wird zum neuen Termin noch einmal aufgefüllt.
   Das ist gewollt, denn genau dafür setzt der Vater einen neuen Termin.
2. **Erst die alte Regel buchen**, wie B-771 es für wiederkehrende Ziele tut (`ApplyDueRefill` unter der alten Regel).
   Begründung: Was das Kind unter der alten Regel schon verdient hat, geht durch das Umstellen nicht verloren.
3. **Ein PATCH, der den Termin nicht ändert, leert nichts**, auch wenn er `refillAtUtc` unverändert mitschickt.
   Begründung: Sonst würde jede Preisänderung nach dem Termin erneut auffüllen. Verglichen wird der neue gegen den alten
   Wert, nicht das Vorhandensein des Feldes.
4. **Ein Termin in der Vergangenheit feuert beim nächsten Lesen**, ohne Sonderfall. Begründung: Ein neues Kind bekommt
   dann über `StartStock` auch sofort `MaxStock`. Die Validierung verlangt keinen Zukunftstermin, und das ändert diese
   Story nicht.
5. **Kein Vertragsbruch, keine Migration:** nur die Schreiblogik des PATCH.

## Akzeptanzkriterien

1. Täglich, `MaxStock 2`, ausverkauft. Am selben Tag Wechsel auf „einmalig“ in fünf Tagen: Der Bestand bleibt 0 bis zum
   Termin, die Vater-Ansicht nennt den Termin als nächste Auffüllung, und ab dem Termin steht der Bestand auf 2.
   *Rot, wenn der Wechsel den Zeitpunkt nicht leert.*
2. „Einmalig“ ist schon gefeuert und ausverkauft. Ein neuer Termin füllt zum neuen Termin auf, vorher nicht.
   *Rot, wenn nur ein Wechsel der Art leert, nicht ein neuer Termin.*
3. „Einmalig“ ist gefeuert und ausverkauft. Ein PATCH nur des Preises, mit unverändert mitgeschicktem `refillAtUtc`,
   füllt nicht auf. *Rot, wenn jeder PATCH auf „einmalig“ leert.*
4. Täglich, gestern ausverkauft, die heutige Auffüllung ist noch nicht gelesen. Wechsel heute auf „einmalig“ in fünf Tagen:
   Direkt danach ist der Bestand 1 (alte Regel gebucht), nach einem Kauf 0 bis zum Termin. *Rot, wenn der Wechsel die
   alte Regel nicht bucht.*

## Angriffsplan

- `ShopController.UpdateListing`: Bedingung `resetsOnceInstant = nextRefillKind == Once && (Art geändert ||
  nextRefillAt != listing.RefillAtUtc)`. Vor dem Zuweisen bei `restartsRefillClock || resetsOnceInstant` die alte Regel
  buchen. Nach dem Zuweisen `ShopService.ClearRefillInstant(listing)` neben `RestartRefillClock`.
- Kommentar an der Stelle („Once keeps its empty instant“) auf die neue Regel ziehen.
- Tests in `ShopStockPerChildTests.cs` neben den B-771-Fällen. Die Senke ist der PATCH; ein zweiter Schreibweg für die
  Art gibt es nicht (POST legt neu an, Bestände entstehen dort erst später).

## Schätzung

**S**, backend. Ein Block im PATCH, eine statische Hilfsmethode, vier Integrationsfälle. Risiko: Die Bedingung aus
Entscheidung 3 vergleicht `DateTime?`; der Wert aus dem JSON ist UTC, der aus SQLite kommt als `Unspecified` zurück.
`==` auf `DateTime` vergleicht nur Ticks, nicht `Kind`, darum trägt der Vergleich.

## Verlauf

- **2026-11-21** — angelegt aus der Nachschau von B-771 im Nachtlauf (`idee`, `entgangen_bei: [B-771]`).
- **2026-11-21** — ausformuliert, gegrillt und geschätzt im Nachtlauf (Sprint 100, autonom, `art: Defekt`): Ist-Stand am
  Code nachgelesen, Szenario gegen `DueRefills` nachgerechnet, `unverifiziert` entfernt; fünf Entscheidungen, vier AK mit
  je einer roten Änderung; S, keine Migration, kein Vertragsbruch.
- **2026-11-21** — in-arbeit (Sprint 100). Gebaut: `ShopController.UpdateListing` mit `resetsOnceInstant`; vor dem Leeren
  wird die alte Regel gebucht (`ApplyDueRefill`), dann folgt `ShopService.ClearRefillInstant`.
  **Rote Probe vor dem Fix:** `ShopStockPerChildTests` 4 failed / 22 passed. Davon für diese Story:
  - AK 1 erwartet `2026-11-25T00:00:00Z`, gemessen `null` (:787).
  - AK 2 erwartet 1, gemessen 0 (:833).
  - AK 4 erwartet 1, gemessen 0 (:880).
  - AK 3 war vorher grün, wie erwartet: Der Fall schützt Entscheidung 3 und belegt sie per Mutante M3.
  Nach dem Fix 81/81 im Shop-Filter.
  - AK 1, täglich → einmalig → `B776_Taeglich_Auf_Einmalig_Fuellt_Zum_Termin`; M1 (`ClearRefillInstant` nie) → rot.
  - AK 2, neuer Termin → `B776_Neuer_Termin_Fuellt_Erneut_Zum_Neuen_Termin`; M2 (nur Artwechsel leert) → rot.
  - AK 3, gleicher Termin mitgeschickt → `B776_Gleicher_Termin_Fuellt_Nicht_Erneut`; M3 (jeder PATCH auf einmalig leert) → rot.
  - AK 4, alte Regel gebucht → `B776_Umstellen_Auf_Einmalig_Bucht_Die_Alte_Regel`; M4 (Buchen nur bei `restartsRefillClock`) → rot.

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | M1 | `ShopController.cs:323` (`if (resetsOnceInstant && listingId < 0)`) | red | red |
  | M2 | `ShopController.cs:299` | red | red |
  | M3 | `ShopController.cs:299` | red | red |
  | M4 | `ShopController.cs:300` | red | red |

  Gemessen mit `mutant-probe.py` an `3612afb0 + Arbeitsbaum`, Grundlinie danach grün. M1 kompilierte als `if (false)`
  nicht (unerreichbarer Code) und wurde darum als Bedingung neu gemessen.
  Nebenbefund außerhalb des Diffs: Ein aus SQLite gelesenes `RefillAtUtc` geht ohne `Z` über den Draht. Der Test vergleicht
  darum den Text.
- **2026-11-21** — abgenommen in Commit `a0b73724` (Sprint 100). Endstand: Backend 1937/1937 (Release). Rollengang E2E 4/4
  auf den Ausweichports, Vater abgenommen, Sohn und Creator als Regressionszeugen. Review: vier 🟢, Zähler 0 von 5
  (docs/pm-sitzung-2026-11-21.md, Sprint 100).
- **2026-11-21** — nachgeschaut (Retro Sprint 101), Diff `a0b73724`. Gegrept, wer `RefillAtUtc` schreibt: nur
  `VaterShop.tsx:228`, immer `<Datum>T00:00:00Z`. Gegen den Vergleich `nextRefillAt != listing.RefillAtUtc` gelesen.
  **Befund:** Ein per API mit Uhrzeit angelegter Termin zählt beim Bearbeiten im Vater-Web als neu und füllt erneut auf →
  [B-782](B-782-einmal-termin-mit-uhrzeit-faellt-beim-bearbeiten-erneut.md).
