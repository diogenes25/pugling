---
tags: [typ/story, status/geschaetzt, bereich/frontend, rolle/student]
aliases: [Ziele 2/2, Truhe zu]
status: geschaetzt
prio: P2
art: Defekt
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
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

## Schätzung

**Größe: M** — wie der vokabel-basierte Batch-Pfad im `MediaSelector` (B-03): ein neuer Weg durch beide Seiten des
Vertrags (Server liefert, Client liest statt zu rechnen), aber an bekannten Stellen und ohne Schema. Größer als S
(B-01), weil Backend, Vertrag, zwei Sohn-Seiten, beide Sprachdateien und drei Testebenen mitgehen; kleiner als L (E6),
weil keine Bestandsdaten berührt werden.

- **`migration: nein`** — Anzahl, Erfüllte und Art werden je Anfrage aus derselben Auswahl gerechnet wie
  `rewardsUnlocked`; nichts wird gespeichert.
- **`vertragsbruch: nein`** — drei **additive** Antwortfelder an `TodayResponse`
  (`backend/Pugling.Contracts/Student/ProgressDtos.cs:27-29`). Die `unknown_field`-Sperre gilt nur für Eingaben
  (`backend/Pugling.Api/Program.cs:47`), nicht für Antworten. `Pugling.Client` teilt die Vertragstypen
  (`backend/Pugling.Client/StudentApi.cs:132-133` liest `TodayResponse` aus `Pugling.Contracts`) – dort ist nichts
  nachzuziehen außer dem Bau; der Agent liest `today` gar nicht. Der positionale Record wird nur an einer Stelle
  gebaut (`backend/Pugling.Api/Controllers/Student/ChildTodayController.cs:35-36`). Im Frontend kommen die Typen aus
  `docs/openapi/v1.json` (`frontend/src/lib/types.ts:515`, `contract.ts` erzeugt); neue Pflichtfelder zwingen nur
  die Test-Attrappen nach (Risiko 4).
- **`wo: beides`** — Server liefert (Entscheidung 1), Startseite und Bonus-Seite lesen (AK 2–4).

### Woher die Felder kommen müssen

Aus **derselben Liste**, aus der `rewardsUnlocked` entsteht: `rewardGoals` in `ComputeChildDayAsync`
(`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:331`, nach B-611 über den Kind-Tag gewählt). Der
`ChildDayRollup` (`backend/Pugling.Api/Services/Shared/ChildDayRollup.cs:23-26`) bekommt dafür Anzahl, Erfüllte und
Art neben `RewardOutstanding`/`RewardsUnlocked`, der Controller reicht sie durch. **Nicht** im Controller oder in
`ChildTodayController` aus `day.Positions` neu filtern – genau diese zweite Rechnung ist der Defekt (AK 1). Für die
Art bietet sich das vorhandene `GoalCadence` (`Daily`/`Weekly`/`None`) an, das der Vertrag schon an
`PositionStatus.Cadence` trägt (`backend/Pugling.Contracts/Shared/ProgressDtos.cs:41`); eine Festlegung ist das
nicht, die Form entscheidet der Bau.

### Risiken

1. **Die Bonus-Seite kennt `today` nicht.** Sie liest nur `student/children/{id}/extra-training`
   (`frontend/src/sohn/SohnBonus.tsx:70`), und `ExtraTrainingOverview` trägt keine Art
   (`backend/Pugling.Contracts/Student/ExtraTrainingDtos.cs:26`; gebaut in
   `backend/Pugling.Api/Services/Student/ExtraTrainingService.cs:59-60`). Für AK 4 braucht es **entweder** ein
   viertes additives Feld an `ExtraTrainingOverview` aus demselben Rollup (`GateAsync`, `ExtraTrainingService.cs:261`,
   liest ihn schon) **oder** einen zweiten Abruf von `today` auf der Bonus-Seite. Entscheidung 2 sagt „dieselbe
   Serverangabe", nicht welcher Weg – siehe Annahme unten. Empfehlung für den Bau: das Feld an der Übersicht, weil
   der Gate denselben Rollup schon hat und die Seite keinen zweiten Ladezustand bekommt.
2. **Die Zeilen „Ziele heute" / „Ziele diese Woche" bleiben Client-Zählung** (`frontend/src/sohn/SohnHome.tsx:75-79`,
   `:110-121`) – AK 3 verlangt das. Wer `belohnungsZiele` entfernt, darf `zaehle` nicht mitnehmen; nur der Balken
   (`:81-82`, `:122`) und das `ziele`-Argument an `Truhe`/`BonusEinstieg` (`:139`, `:144`) wechseln auf die
   Serverfelder. `TruhenZiele` bleibt als Typ, weil `SohnBonus.tsx:1`, `:50` und `ZU_TEXT` (`SohnHome.tsx:382`) ihn
   tragen; nur die Abbildung Server-Art → `tag`/`woche`/`keine` kommt dazu.
3. **Der Bonus-Einstieg rechnet „offen" weiter selbst** aus `today.rewardsUnlocked || today.goalsTotal === 0`
   (`SohnHome.tsx:144`). Das spiegelt die Regel „ohne Pflicht nichts davor" des Gates (`ExtraTrainingService.cs:265`)
   und ist nicht Gegenstand der Story; mit der neuen Art `keine` läge eine Vereinfachung nahe – nur mitnehmen, wenn
   der Fall „Plan ohne Pflicht" danach denselben Text zeigt.
4. **Pflichtfelder im erzeugten Vertrag** brechen die Test-Attrappen beim Typcheck (`npm run build` = `tsc -b`):
   `frontend/src/sohn/tagessichtWeg.test.tsx:64-68` (`TodayResponse`) und – bei Weg 1 aus Risiko 1 –
   `frontend/src/sohn/SohnBonus.test.tsx:39-45` (`ExtraTrainingOverview`). Vitest prüft keine Typen; erst `tsc` fällt.
5. **Neuer Textschlüssel in beiden Sprachen** (`bonus.lockedDuty` bekommt ein Wochen-Gegenstück neben
   `frontend/src/i18n/de.ts:180` und `frontend/src/i18n/en.ts:169`); die Parität hält `frontend/src/lib/i18n.test.ts`. Vorbild ist
   das schon vorhandene Paar `teaserLocked`/`teaserLockedWeek` (`de.ts:177-178`).
6. **Vertragsdoku muss die Felder tragen**, sonst fällt das Doku-Tor (`backend/Pugling.Api.Tests/ConventionGuardTests.cs:675`,
   `:752`): je `<param>` an `TodayResponse` (und ggf. `ExtraTrainingOverview`). `docs/openapi/v1.json` schreibt
   `ContractDocumentTests` neu – mit committen. `docs/api-examples` erfasst `today` heute nicht (der `today`-Block in
   `docs/api-examples/study-plans.md:608ff` ist die **Plan**-Übersicht `DayOverview`, die unverändert bleibt), die
   `DocsCaptureTests` ziehen also nur nach, wenn der Bau `DayOverview` anfasst – das soll er nicht.

### Angriffsplan

1. **Voraussetzung:** B-611 abgenommen (Auswahl über den Kind-Tag), sonst liefern die neuen Felder die je-Plan-Auswahl.
2. Backend: `ChildDayRollup` um Anzahl/Erfüllte/Art aus `rewardGoals` erweitern; `TodayResponse` um drei additive
   Felder samt englischer `<param>`-Doku; `ChildTodayController` reicht durch. Bei Weg 1 (Risiko 1) dasselbe für
   `ExtraTrainingOverview` über `GateAsync`.
3. Integrationstest (AK 1) rot → grün; `dotnet build Pugling.sln`, `dotnet format Pugling.sln`, Testlauf schreibt
   `docs/openapi/v1.json`.
4. Frontend: `npm run gen:contract`; `SohnHome.tsx` liest Balken und `ziele` aus den Feldern, `belohnungsZiele`
   entfällt samt falschem Kommentar (`SohnHome.tsx:349-351`); `SohnBonus.tsx:95` wählt den Text nach Art; neuer
   Schlüssel in `de.ts`/`en.ts`.
5. Vitest: Attrappen nachziehen, AK 2 und AK 4 als neue Fälle, AK 5 bestehende Fälle; die drei `belohnungsZiele`-
   Zusicherungen (`frontend/src/sohn/SohnHome.test.tsx:346-352`) wandern als Fälle an den Server-Test.
6. `npm run build` (Typcheck) und `npm test`.

### Testweg

- **Backend:** Fälle für AK 1 in der Integrationstestklasse aus B-611 (gleicher Aufbau: Plan A zwei Tagesziele,
  Plan B ein Wochenziel) – Felder vor und nach dem Erfüllen gegen `rewardsUnlocked`. Dazu die drei Auswahl-Fälle aus
  `SohnHome.test.tsx:346-352` als Server-Fälle mit **einem** Plan (Tag+Woche ⇒ Tag 1/1; Woche+ohne ⇒ Woche 1/1; nur
  ohne ⇒ keine 0/0), damit AK 5 „nicht ersatzlos weg" belegt ist. Bei Weg 1 ein Fall an `ExtraTrainingTests`
  (Plan nur mit Wochenzielen ⇒ Art Woche).
- **Vitest:** `frontend/src/sohn/tagessichtWeg.test.tsx` rendert die ganze Startseite gegen eine `today`-Antwort
  (`:63-68`) – dort AK 2 (Positionen flach „alle Tagesziele erfüllt", Server „Woche 0/1" ⇒ Balken 0 %, „Schaffe deine
  Wochenziele…") und AK 3 (die zwei Zeilen zählen weiter je Takt). AK 4 in `frontend/src/sohn/SohnBonus.test.tsx`
  (Sperre mit Art Woche ⇒ „Erst die Wochenziele…"). AK 5: `SohnHome.test.tsx:337-362` bis auf den gewanderten
  Auswahl-Fall unverändert grün.
- **E2E:** die zwei Pläne mit verschiedenem Takt fährt schon die Rollengang-Spec aus B-611; um eine Zusicherung auf
  den Balken bzw. den Truhen-Text erweitern statt einer weiteren Spec – sie zeigt dieselbe Fläche. Das ist der
  Rollengang dieser Story.
- **Reviewer:** `pugling-reviewer` (Vertrag, Rollup) **und** `frontend-reviewer` (Startseite, Bonus-Seite, i18n).

### Annahme (vom Vater bestätigt am 2026-11-17)

- **Die Bonus-Seite bekommt ihre Art über ein Feld an `ExtraTrainingOverview`,** nicht über einen zweiten Abruf von
  `today`. Entscheidung 1 nennt nur `TodayResponse`, Entscheidung 2 „dieselbe Serverangabe"; ein viertes additives
  Feld an einem zweiten DTO steht in keiner Entscheidung und keinem Kostenpunkt. Kein Bruch, aber vor dem Bau kurz
  bestätigen lassen.

## Verlauf

- **2026-11-17** — angelegt aus der Nachschau von Sprint 83 (🟡, `pugling-reviewer`), `entgangen_bei: [B-606]`.
- **2026-11-17** — ausformuliert: flache Client-Auswahl (`SohnHome.tsx:353-361`) gegen Auswahl je Plan am Server
  (`PositionProgressService.cs:331`) belegt, der Fall „2 / 2, voller Balken, Truhe und Bonus ‚erst die Tagesziele'" am
  Code durchgerechnet; `TodayResponse` trägt keine Belohnungszahlen; Zusatzfund: die Bonus-Seite sagt auch bei reinen
  Wochenzielen „Erst die Tagesziele" (`SohnBonus.tsx:95`). Nicht im Browser nachgestellt.
- **2026-11-17** — gegrillt mit dem Vater: Entscheidungen 1–3, alle nach Empfehlung; Punkt 2 durch B-611 Entscheidung 4 erledigt; Akzeptanzkriterien final.
- **2026-11-17** — geschätzt: M (wie B-03, ein neuer Weg durch beide Seiten des Vertrags), beides, keine Migration, kein
  Vertragsbruch (additive Antwortfelder aus `rewardGoals`, `PositionProgressService.cs:331`); Annahme offen: die
  Bonus-Seite bekommt ihre Art über ein Feld an `ExtraTrainingOverview`, weil sie `today` nicht liest
  (`SohnBonus.tsx:70`). Setzt B-611 voraus; Reviewer `pugling-reviewer` und `frontend-reviewer`.
- **2026-11-17** — Annahme der Schätzung vom Vater bestätigt: die Bonus-Seite bekommt die Zielart über ein Feld an `ExtraTrainingOverview`.
