---
tags: [typ/story, status/gegrillt, bereich/frontend, rolle/student]
aliases: [Ziele 2/2, Truhe zu]
status: gegrillt
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Nachschau Sprint 83 im Nachtlauf 2026-11-17 (docs/pm-sitzung-2026-11-17-nachtlauf.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-606]
---

# B-612 · Die Startseite zählt die Belohnungsziele anders als der Server – „2/2" neben einer geschlossenen Truhe

`SohnHome.tsx` rechnet mit `belohnungsZiele` (~:334) über die **flache** Positionsliste aller Pläne nach („gibt es
irgendwo Tagesziele, zählen nur Tagesziele"); der Server wählt **je Plan** (`RewardObligations`, B-611). Mit einem
Tagesziel-Plan und einem Wochenziel-Plan zeigt die Startseite „Ziele heute 2/2" und einen vollen Balken, während die
Truhe „Schaffe deine Tagesziele…" sagt und der Bonus gesperrt ist. Der Kommentar „dieselbe Auswahl wie
`RewardObligations`" (~:331) stimmt bei mehreren Plänen nicht mehr. Das Kind merkt so etwas sofort.

Naheliegend: Belohnungsziel-Zahlen und -Art vom Server in `TodayResponse` liefern (Regel an der Senke) statt im Client
nachzurechnen; hängt an der Regel aus B-611.

## User Story

Als *Sohn* möchte ich, dass Zielbalken, Truhe und Bonus-Hinweis auf der Startseite dasselbe sagen, damit ich weiß, was
mir zur Truhe noch fehlt, statt bei „2 / 2" und vollem Balken vor einer verschlossenen Truhe zu stehen. Als *Vater*
möchte ich, dass die Belohnungsregel an genau einer Stelle steht, damit eine Regeländerung (B-611) nicht zwei Stände
erzeugt.

## Ist-Stand am Code

Gelesen am 2026-11-17 auf `f9cd2a38`; nicht im Browser nachgestellt (zwei aktive Pläne mit verschiedenem Takt gibt es an
den echten Kindern heute nicht, siehe B-611). **Die Behauptung der Idee stimmt**; die Zeilen sind `:349-353`, nicht
`~:331`/`~:334`.

**Der Client wählt flach, der Server je Plan.**

- `belohnungsZiele` filtert die Positionen **aller** Pläne: Tagesziele, wenn es irgendwo eins gibt, sonst alle Ziele mit
  Takt; daraus `art` (`tag`/`woche`/`keine`), `met`, `total` (`frontend/src/sohn/SohnHome.tsx:353-361`). Ihr Kommentar
  behauptet „dieselbe Auswahl wie `PositionProgressService.RewardObligations`" (`SohnHome.tsx:349-351`).
- Der Server wählt je Plan und vereinigt danach (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:331`,
  belegt in B-611). Die beiden Auswahlen fallen genau dann auseinander, wenn ein Plan Tagesziele hat und ein anderer
  Plan Wochenziele, aber keine Tagesziele.
- `TodayResponse` liefert nur das Ergebnis `RewardsUnlocked`, keine Zahlen und keine Art
  (`backend/Pugling.Contracts/Student/ProgressDtos.cs:27-29`); `GoalsTotal`/`GoalsMet` zählen die volle Pflicht.

**Was die Startseite daraus macht** (Plan A: zwei Tagesziele erfüllt; Plan B: ein Wochenziel offen):

- Ziele-Karte (sichtbar, weil `dutyDone: false`): „🎯 Ziele heute 2 / 2" und „📅 Ziele diese Woche 0 / 1" – beide Zeilen
  zählen je Takt über alle Pläne (`SohnHome.tsx:75-79`, `:110-121`), der Balken nimmt `belohnungsZiele` ⇒ **100 %**
  (`:81-82`, `:122`).
- Truhe: zu und nicht freigeschaltet (`rewardsUnlocked: false` vom Server) ⇒ Text nach `art = "tag"`: „Schaffe deine
  Tagesziele, dann öffnet sich die Truhe." (`SohnHome.tsx:139`, `:382`, `:445`; `frontend/src/i18n/de.ts:66`).
- Bonus-Einstieg: gesperrt ⇒ „🔒 Erst die Tagesziele – dann öffnet das Bonus-Training."
  (`SohnHome.tsx:144`, `frontend/src/sohn/SohnBonus.tsx:58`, `de.ts:177`).

Das Kind sieht also zwei erfüllte Tagesziele, einen vollen Balken und zweimal „erst die Tagesziele". Der einzige Hinweis
auf den wahren Grund steht eine Seite weiter: die Bonus-Seite listet die offenen Belohnungsziele vom Server, hier
„… (Wochenziel) offen" (`SohnBonus.tsx:96-100`).

**Zusätzlicher Fund, unabhängig von mehreren Plänen:** Die Bonus-Seite selbst sagt bei einer Sperre immer „Erst die
Tagesziele – dann öffnet das Bonus-Training." (`SohnBonus.tsx:95`, `de.ts:180`), auch bei einem einzigen Plan nur mit
Wochenzielen; der Einstieg auf der Startseite unterscheidet die Art (`SohnBonus.tsx:58`), die Seite nicht. Die
Übersicht des Bonus-Trainings trägt keine Art, nur `Outstanding` (`backend/Pugling.Api/Services/Student/ExtraTrainingService.cs:59`).

**Nicht betroffen:** `frontend/src/sohn/pfad.ts` trägt keine Belohnungsregel. `naechstePositionId` hebt das erste offene
Tagesziel hervor, sonst das erste offene Wochenziel (`pfad.ts:41-44`); `frist` rechnet je Position (`:70-74`).

**Tests:** Die Vitest-Fälle zu `belohnungsZiele` prüfen nur flache Listen
(`frontend/src/sohn/SohnHome.test.tsx:346-352`); kein Fall mit zwei Plänen. Die Zusicherung „dieselben Ziele wie der
Server" (`SohnHome.test.tsx:333-336`) ist damit nur für einen Plan belegt.

## Die echte Lücke

Die Belohnungsauswahl steht zweimal – am Server und im Client –, und der Vertrag gibt dem Client nur das Ja/Nein, nicht
die Zahlen. Mit Regel (a) aus B-611 fiele der heute sichtbare Widerspruch von selbst weg (der Client rechnet flach genau
(a)); die doppelte Regel bliebe und bräche bei der nächsten Änderung wieder, spätestens mit dem Stundenplan-Takt
(B-621). Daneben nennt die Bonus-Seite die falsche Zielart.

## Offene Punkte

1. ~~**Liefert der Server die Belohnungsziele in `TodayResponse` mit?**~~ → Entscheidung 1.
2. ~~**Bleibt B-612 nötig, wenn B-611 Regel (a) wählt?**~~ → [B-611](B-611-wochenplan-sperrt-die-truhe-aller-plaene.md)
   Entscheidung 4 (zwei Stories, B-611 zuerst).
3. ~~**Gehört die Bonus-Seite in diese Story?**~~ → Entscheidung 2.
4. ~~**Soll die geschlossene Truhe nennen, was ihr fehlt?**~~ → Entscheidung 3.

## Entscheidungen

Grill-Runde mit dem Vater, 2026-11-17. Alle nach Empfehlung.

1. **Der Server liefert Anzahl, Erfüllte und Art (Tag/Woche/keine) der Belohnungsziele in `TodayResponse`;** die
   Startseite rechnet nicht mehr selbst (`belohnungsZiele` entfällt bzw. liest nur noch die Felder). *Begründung:* Regel
   an der Senke – genau die Nachrechnung im Client ist mit mehreren Plänen verrottet. *Kosten:* drei additive
   Vertragsfelder, `Pugling.Client`, Frontend-Typen; kein Bruch.
2. **Die Bonus-Seite nennt die richtige Art,** statt bei jeder Sperre „Erst die Tagesziele" (`SohnBonus.tsx:95`,
   `de.ts:180`) – auch im Ein-Plan-Fall nur mit Wochenzielen. *Begründung:* dieselbe Serverangabe, dieselbe Story.
   *Kosten:* ein Textschlüssel, ein Vitest-Fall.
3. **Keine Liste fehlender Ziele an der Truhe in dieser Story.** *Begründung:* Mit stimmigen Zahlen und Art ist der
   Widerspruch behoben; eine Liste ist ein neuer Wunsch, keine Reparatur. *Kosten:* Die Liste bleibt auf der
   Bonus-Seite; der Vater hat keine eigene Idee dafür verlangt.

## Akzeptanzkriterien

Final (Entscheidungen 1–2; vorher Entwurf), geschrieben für Punkt 1 (Serverfelder) und Regel (a) aus B-611. Je Fall die Änderung, die ihn rot macht.

1. `student/children/{id}/today` liefert Anzahl, Erfüllte und Art der Belohnungsziele aus **derselben** Auswahl wie
   `rewardsUnlocked`: Plan A mit zwei Tageszielen, Plan B nur mit einem Wochenziel ⇒ Art Tag, 0/2 vorher und 2/2
   nachher, `rewardsUnlocked` jeweils genau dann `true`, wenn erfüllt = Anzahl > 0. *Rot, wenn die Felder neben
   `RewardObligations` eigens gerechnet werden und eine der beiden Auswahlen abweicht.*
2. Die Startseite nimmt Balken und Truhen-/Bonus-Text aus diesen Feldern: ein Render-Test mit einer `today`-Antwort,
   deren Positionen flach nachgerechnet etwas anderes ergäben als die Serverfelder (etwa alle Tagesziele erfüllt, Server
   meldet Art Woche 0/1), zeigt Balken 0 % und „Schaffe deine Wochenziele…". *Rot, solange `belohnungsZiele` aus
   `today.positions` rechnet (`SohnHome.tsx:81`).*
3. Die Zeilen „Ziele heute" und „Ziele diese Woche" zählen weiter je Takt über alle Pläne (B-535-Review). *Rot, wenn sie
   durch die Belohnungszahlen ersetzt werden.*
4. Die Bonus-Seite nennt bei einer Sperre die Zielart, an der sie hängt: ein Plan nur mit Wochenzielen ⇒ „Erst die
   Wochenziele…". *Rot mit dem festen `bonus.lockedDuty` (`SohnBonus.tsx:95`).*
5. Mit einem aktiven Plan sieht die Startseite aus wie heute: die bestehenden Fälle aus `SohnHome.test.tsx:337-362`
   bleiben inhaltlich gleich (die Auswahl-Fälle wandern an den Server-Test, nicht ersatzlos weg).

## Verlauf

- **2026-11-17** — angelegt aus der Nachschau von Sprint 83 (🟡, `pugling-reviewer`), `entgangen_bei: [B-606]`.
- **2026-11-17** — ausformuliert: flache Client-Auswahl (`SohnHome.tsx:353-361`) gegen Auswahl je Plan am Server
  (`PositionProgressService.cs:331`) belegt, der Fall „2 / 2, voller Balken, Truhe und Bonus ‚erst die Tagesziele'" am
  Code durchgerechnet; `TodayResponse` trägt keine Belohnungszahlen; Zusatzfund: die Bonus-Seite sagt auch bei reinen
  Wochenzielen „Erst die Tagesziele" (`SohnBonus.tsx:95`). Nicht im Browser nachgestellt.
- **2026-11-17** — gegrillt mit dem Vater: Entscheidungen 1–3, alle nach Empfehlung; Punkt 2 durch B-611 Entscheidung 4 erledigt; Akzeptanzkriterien final.
