---
tags: [typ/story, status/ausformuliert, bereich/daten, rolle/supervisor]
aliases: [Seed-Familie, Demo-Daten inkonsistent]
status: ausformuliert
prio: P3
art: Aufräumen
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-08-rollengang.md#feedback-vater
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-429 · Seed: die Demo-Familie widerspricht dem eigenen Katalog

Der Seed ist das Erste, was ein neuer Entwickler, ein Tester oder ein Rollengang sieht – und er zeigt die App
schlechter, als sie ist. Im Rollengang vom 2026-11-08 (frische Wegwerf-DB) stand im Kind-Hub von „Sohn":
Buch „Green Line 1 · Klett – **NICHT KATALOGISIERT**", obwohl die Reihe `green-line-1` mit drei Units im
Katalog liegt; „**Kein Profil passt** zu Sohn – prüfe Klasse und Schulart", weil das Kind weder Klasse noch
Schulart trägt; der Kontoverlauf nennt das Startguthaben „**Wiederholung**"; und die französischen Vokabeln
tragen als Tipp den Artikel des **Fragewortes** („💡 la" zu „la ville", Lösung „die Stadt").

## User Story

Als *Vater*, der die App zum ersten Mal öffnet, möchte ich eine Demo-Familie sehen, die den Katalog nutzt, damit
Kind-Hub, Fachlehrer-Treffer und Kontoverlauf das zeigen, was sie können – statt Warnungen über Daten, die der
Seed selbst falsch gesetzt hat.

## Ist-Stand am Code

- `Seed.cs:247-256`: `Textbook { Title = "Green Line 1", Publisher = "Klett", Grade = 5, CurrentChapter =
  "Unit 1 – Greetings" }` – ohne `SeriesId`/`CurrentUnitId`, obwohl `TextbookSeries` „Green Line 1" mit Unit 1
  im selben Seed entsteht.
- `Seed.cs:1047-1052`: Kind „Sohn" ohne `Grade`/`SchoolType` (API: `grade: null, schoolType: "None"`), darum
  schlägt `creator/profiles/match` mit „kein Profil passt" an (Kind-Hub, Rollengang). Der Lehrer-Seed legt
  zudem kein `CreatorProfile` an (`/vater/fachlehrer` als Lehrer: „Noch kein Fachlehrer").
- `Seed.cs:1061`: Startguthaben als `PointKind.Base` („Wiederholung" in `labels.ts:8`); `:1062` Gems als
  `Achievement` („Auszeichnung") – beides irreführend, `Manual`/`ManualGems` („Papa-Buchung") trifft es.
- Französische Items mit `Hint = "la"`/`"le"` (Rollengang: Karte „la ville", Tipp „la"; Config
  `direction: front-to-back, fr→de`): der Tipp wiederholt das Fragewort statt auf die Lösung zu zeigen.

## Die echte Lücke

Vier Stellen im Seed, kein Produktcode. Alles Aufräumen: kein Verhalten ändert sich, nur die Demo-Daten.

## Offene Punkte

Keine Produktfrage; Aufräumen, autonom grillbar. Empfehlung: Buch an Reihe/Unit 1 hängen; Kind Klasse 5,
Gymnasium (passt zur Green-Line-Unit „Klasse 5"); ein Fachlehrer-Profil „Englisch Klasse 5–6, Green Line 1"
für Herrn Schmidt, damit der Match im Kind-Hub trifft; Startguthaben als `Manual`/`ManualGems`; französische
Tipps auf den Anfangsbuchstaben der Lösung („d…") oder leer. **`DocsCaptureTests`** (`docs/api-examples`)
werden sich ändern – neu erzeugen und den Diff lesen (Memory: Capture byte-stabil halten).

## Akzeptanzkriterien (Entwurf)

1. Frische DB → Kind-Hub „Sohn" zeigt das Buch mit Reihe „Green Line 1" und Unit 1 (kein „nicht katalogisiert"),
   und „Passender Fachlehrer" nennt Herrn Schmidt mit Grund `series_match`.
2. Kontoverlauf nennt „Papa-Buchung · Startguthaben" und „Papa-Geschenk (Gems) · Willkommens-Gems".
3. Kein französisches Item trägt als Tipp seinen eigenen Artikel.
4. Seed bleibt idempotent (zweiter Start ändert nichts – bestehender Test der Seed-Idempotenz bleibt grün).

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)), Ist-Stand
  belegt (`ausformuliert`); Grillen und Schätzen im Nachtlauf (Aufräumen, Freigabe 1).
