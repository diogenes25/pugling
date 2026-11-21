---
tags: [typ/story, status/abgenommen, bereich/shop, rolle/supervisor, rolle/student]
aliases: [Sofort-Auffüllung ohne Zeitpunkt]
status: abgenommen
prio: P2
art: Defekt
groesse: S
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

# B-771 · Ein Bestand ohne Auffüll-Zeitpunkt füllt bei wiederkehrender Auffüllung sofort auf

Ein Kind-Bestand ohne `LastRefilledAtUtc` schuldet bei jeder wiederkehrenden Art (täglich, zweimal täglich, wöchentlich,
monatlich) **sofort** eine Auffüllung: `ShopRefillSchedule.DueRefills` gibt für `lastUtc == null` pauschal 1 zurück
(`backend/Pugling.Api/Services/Shared/ShopRefillSchedule.cs:24`). Ohne Zeitpunkt entsteht ein Bestand auf zwei Wegen:

- als Startbestand eines Angebots mit „keine Auffüllung" (`StartStock` gibt dafür `null`, `:96`), das der Vater später
  per PATCH auf eine wiederkehrende Art umstellt – der PATCH setzt die Kind-Bestände nicht neu;
- aus der Migration `ShopStockPerChild`, die ein altes `LastRefilledAtUtc = NULL` eines wöchentlichen Angebots übernimmt.

**Folge (am Code nachgelesen, nicht im Browser gesehen):** Der Vater legt „Taschengeld" am 5. ohne Auffüllung an,
Friedrich kauft alles, der Vater stellt am 19. auf „monatlich" – beim nächsten Lesen steht der volle Bestand wieder da,
und Friedrich kauft im selben Monat ein zweites Mal. B-645 verspricht „genau einmal je Monat".

Gefunden in der Nachschau von Sprint 96 (Code-Lesung durch `pugling-reviewer`); die Tests verfehlen es, weil
`ShopRefillScheduleTests.cs:60` `Daily, null → 1` als Absicht festhält, kein Test die Art an einem Angebot mit
Beständen wechselt und der Migrationstest kein auffüllendes Angebot mit `MaxStock > 0` und leerem Zeitpunkt hat.

Mögliche Reparaturen (zu entscheiden): fällige Auffüllungen ab `lastUtc ?? listing.CreatedAt` zählen, oder beim Wechsel
der Art alle Kind-Bestände des Angebots auf „jetzt" stempeln. Rote Probe: PATCH „keine" → „monatlich" nach einem
Ausverkauf erwartet Bestand 0 bis zum Monatsersten.

## User Story

Als *Vater* möchte ich ein Angebot auf eine wiederkehrende Auffüllung umstellen, ohne dass es dabei sofort wieder voll
ist, damit „einmal im Monat" auch im Monat der Umstellung gilt.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main` (`10d45b89`).

- `ShopRefillSchedule.DueRefills` gibt für eine wiederkehrende Art bei `lastUtc == null` 1 zurück (`ShopRefillSchedule.cs:24`).
- Ein Bestand ohne Zeitpunkt entsteht nur als Startbestand bei „keine" oder bei einem noch nicht ausgelösten „einmalig"
  (`StartStock`, `:93-97`); für jede wiederkehrende Art stempelt `StartStock` „jetzt".
- Der PATCH (`ShopController.UpdateListing`) setzt `RefillKind` um, fasst die Kind-Bestände aber nicht an.
- **Echte DB (Kopie vom 2026-11-20 18:12):** 7 Angebote (5 täglich, 2 monatlich), **keine** Bestandszeile ohne
  Zeitpunkt an einem wiederkehrenden Angebot. Der Migrationsweg aus dem Fund ist damit gegenstandslos; offen ist allein
  der PATCH-Weg.

## Entscheidungen

1. **Ein Wechsel der Auffüll-Art auf eine wiederkehrende Art startet die Uhr neu:** Erst werden fällige Auffüllungen
   nach der **alten** Regel gebucht (was das Kind schon verdient hat, bleibt), dann steht jeder vorhandene Kind-Bestand auf
   „jetzt aufgefüllt". Die erste Auffüllung nach neuer Regel kommt am nächsten Stichtag. Kosten: Wer auf „täglich" umstellt,
   wartet bis morgen. Verworfen: Zählen ab `listing.CreatedAt` (ein im Oktober angelegtes Angebot füllte bei der Umstellung
   im November sofort auf); `DueRefills` für `null` auf 0 setzen (der Wert bliebe ohne Zeitpunkt und zählte nie wieder).
2. **Ein PATCH ohne Wechsel der Art (außer einem neuen Wochentag, siehe 4) und ein Wechsel auf „keine" oder „einmalig"
   fassen die Bestände nicht an.** „Einmalig"
   braucht gerade den leeren Zeitpunkt, um zum Termin auszulösen.
3. **Keine Migration:** Die Messung zeigt keine Altzeilen.
4. **Nachtrag aus dem Review:** Ein neuer Wochentag eines wöchentlichen Angebots startet die Uhr ebenfalls neu. Derselbe
   Fehler sonst: Montag → Freitag an einem Samstag zählte den Freitag davor und füllte zweimal in einer Woche auf.
5. **Nachtrag aus dem Review:** Weil der PATCH jetzt Bestandszeilen schreibt, kann er gegen einen gleichzeitigen Kauf
   verlieren. Er antwortet dann `409 concurrency_conflict` statt `500` und rollt vollständig zurück.

## Akzeptanzkriterien

1. „keine" → „monatlich" nach Ausverkauf: Der Bestand bleibt bis zum Monatsersten 0 und ist danach wieder da. *Rot, wenn*
   die Umstellung sofort auffüllt.
2. „keine" → „täglich": am selben Tag 0, am nächsten Tag aufgefüllt. *Rot, wenn* sofort aufgefüllt wird oder nie.
3. „täglich" → „monatlich" mit einer fälligen, noch nicht gebuchten Tages-Auffüllung: Sie wird gebucht, danach gilt der Monat.
   *Rot, wenn* die verdiente Auffüllung beim Umstellen verloren geht.
4. „keine" → „einmalig" mit Termin in der Zukunft löst zum Termin aus. *Rot, wenn* die Umstellung auch hier stempelt.
5. Wöchentlich Montag → Freitag am Samstag: kein Auffüllen bis zum nächsten Freitag. *Rot, wenn* nur ein Wechsel der Art
   neu startet.
6. *Absichtserklärung.* Verliert der PATCH gegen einen gleichzeitigen Kauf, antwortet er 409. Nicht durch einen Test
   gedeckt: der Wettlauf bräuchte einen Abfang-Haken im DbContext des Test-Hosts, den es dort nicht gibt.

## Schätzung

**Größe: S**, wie B-672 (eine Grenzbedingung in einem Pfad mit Uhr-Tests). `wo: backend`, keine Migration (Messung oben),
kein Vertragsbruch.

### Angriffsplan

1. Integrationstests in `ShopStockPerChildTests` mit eingefrorener Uhr (AK 1–4), rot gegen den Ausgangsstand.
2. `ShopService.RestartRefillClock` (alte Regel buchen, dann stempeln) und der Aufruf im PATCH nur bei Wechsel auf eine
   wiederkehrende Art.

## Verlauf

- **2026-11-20** — angelegt aus der Nachschau von Sprint 96 (`idee`, `entgangen_bei: [B-645]`).
- **2026-11-20** — auf Wunsch des Nutzers („Ja, B-771 hier") in einem Zug `ausformuliert` → `gegrillt` → `geschaetzt`: Ist-Stand am Code und an einer DB-Kopie gemessen, Entscheidungen 1–3 als Defekt stellvertretend gefällt (S, backend).
- **2026-11-20** — gebaut: `ShopRefillSchedule.IsRecurring`, `ShopService.RestartRefillClock`, Aufruf in
  `ShopController.UpdateListing`, 409 bei Kollision. AK 1 → `B771_Umstellen_Auf_Monatlich_Fuellt_Erst_Zum_Monatsersten`,
  AK 2 → `…_Auf_Taeglich_Fuellt_Am_Naechsten_Tag`, AK 3 → `…_Bucht_Die_Faellige_Auffuellung_Der_Alten_Regel` (alle drei rot
  gegen `10d45b89`: 2 statt 0, 1 statt 0, 0 statt 1), AK 4 → `…_Auf_Einmalig_Loest_Zum_Termin_Aus` (vorher schon grün, Wächter
  des Fixes), AK 5 → `B771_Neuer_Wochentag_Fuellt_Erst_Am_Neuen_Tag`; dazu `B771_Woechentlich_Auf_Monatlich_…` (Wechsel
  zwischen zwei wiederkehrenden Arten). Review `pugling-reviewer`: Wochentag und 409 eingearbeitet (Entscheidungen 4, 5).
  Mutanten:

| Mutante | Stelle | erwartet | gemessen | Anmerkung |
| M1 kein Neustart | backend/Pugling.Api/Controllers/Supervisor/ShopController.cs:293 | red | red |  |
| M2 alte Regel nicht gebucht | backend/Pugling.Api/Controllers/Supervisor/ShopController.cs:296 | red | red |  |
| M3 auch Einmalig stempeln | backend/Pugling.Api/Controllers/Supervisor/ShopController.cs:293 | red | red |  |
| M4 Stempel fehlt | backend/Pugling.Api/Services/Shared/ShopService.cs:603 | red | red |  |
| M5 Wochentag startet nicht neu | backend/Pugling.Api/Controllers/Supervisor/ShopController.cs:294 | red | red |  |

- **2026-11-20** — `abgenommen` mit `3d872f69`. Der Rollengang ist die Integrationssuite mit eingefrorener Uhr, also der
  Weg des Vaters über die API. Die E2E-Spec `taschengeld-monatlich` bleibt durch die fremde Celebration-Arbeit rot
  (`.cel-title`) und belegt hier nichts. Am echten Shop gab es nach der Messung auf der DB-Kopie keine betroffenen Zeilen.
  Offen ist nichts.
- **2026-11-21** — Nachschau (Nachtlauf, pugling-reviewer): Reihenfolge im PATCH (`ShopController.cs:288-317`, alte Regel vor dem Überschreiben), Verlustfreiheit von `DueRefills` in `(last, now]` nachgerechnet, `Include(Stocks)` in `LoadListingForUpdateAsync`, 409 über die Concurrency-Tokens (`PuglingDbContext.cs:912`, `:920`). **Entgangen:** der Nachbarpfad wiederkehrend → einmalig feuert nie → B-776.
