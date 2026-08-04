---
tags: [typ/story, status/ausformuliert, bereich/backend, bereich/api, bereich/gamification]
aliases: [KeyResult-Dublette, duplicate_key_result, Meilenstein zweimal, RewardPerKeyResult zahlt doppelt]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: pugling-reviewer zum B-97-Bau, Befund 2 (2026-08-04)
grund: ""
ersetzt_durch: []
---

# B-104 · Derselbe Meilenstein zweimal: drei Schreibpfade laufen ungeprüft in einen Unique-Index, und dort hängt Geld

Dieselbe Fehlerklasse wie [B-97](B-97-unique-index-ohne-vorpruefung.md), an einer dritten Ressource — aber mit
höherem Einsatz: die drei Eindeutigkeiten über einen Objective-Meilenstein existieren, **weil sonst
`RewardPerKeyResult` doppelt zahlt**. Der Schema-Kommentar sagt das wörtlich. Geprüft wird vor dem Schreiben
in keinem der drei Pfade; der Konflikt endet als `500 internal_error`.

## User Story

Als **Vater** möchte ich beim Anlegen eines Ziels eine Meldung bekommen, wenn ich denselben Meilenstein
zweimal eintrage — damit ich ihn korrigieren kann, statt einen Serverfehler zu sehen und im Zweifel eine
Prämie zweimal auszuzahlen.

## Ist-Stand am Code

- Die drei **gefilterten** Eindeutigkeiten stehen in `Data/PuglingDbContext.cs:628-633` über
  `(ObjectiveId, SubjectId|ChapterId|ExerciseId, Metric)`. Der Kommentar `:625-627` begründet sie selbst:
  „The same milestone twice in the same goal would be a duplicate - and `RewardPerKeyResult` would pay twice."
  Drei statt einer, weil SQLite `NULL`s als verschieden behandelt.
- **Keiner** der drei Schreibpfade prüft das Tripel vor:
  1. `Services/Supervisor/ObjectiveService.cs:132-146` — zwei identische Meilensteine **in einem** POST
     `objectives` (die Dublette entsteht hier *innerhalb* eines Requests, also gegen die noch nicht
     gespeicherte Menge — anders geformt als die beiden anderen).
  2. `ObjectiveService.cs:205-224` (`AddKeyResultAsync`).
  3. `ObjectiveService.cs:229-244` (`UpdateKeyResultAsync`) — schiebt `Metric` auf einen vorhandenen
     Meilenstein desselben Geltungsbereichs.
- `ValidateKeyResultAsync` (`ObjectiveService.cs:50-87`) prüft **nur** Referenzen und Wertebereiche, kein
  Tripel — der Fehlerkanal, in den die Prüfung gehört, existiert also schon (`KeyResultResult(null, err)`).
- Kein `catch (DbUpdateException)` im `ObjectiveService`, also endet der Index-Verstoß als `500`
  `internal_error`.
- Es gibt **keinen** Fehlercode dafür in `Errors/ApiErrors.cs`.
- `Pugling.Api.Tests/ObjectiveTests.cs` enthält keinen Dublettenfall — der Defekt ist unbeobachtet.

## Die echte Lücke

Nicht „Objectives sind kaputt": die Invariante *hält*, die Datenbank lässt die Dublette nicht zu. Die Lücke
ist die **Antwort**: ein `500` statt eines `409`, den der Vater lesen und beheben kann — und ein halb
geschriebener Zustand im Fall 1, wo mehrere Meilensteine in einem Request entstehen.

Der Unterschied zu B-97 ist die Konsequenz einer *anderen* Reihenfolge: Käme die Prüfung nie, wäre irgendwann
ein Weg denkbar, auf dem die Dublette doch entsteht (etwa wenn die Filter-Bedingungen bei einem
Geltungsbereichs-Wechsel nicht mehr greifen) — und dann zahlt die Prämie zweimal. Die Prüfung im Dienst ist
also nicht nur Kosmetik am Statuscode, sie ist die zweite Verteidigungslinie vor der Auszahlung.

## Offene Punkte

1. **Ein Code oder drei?** Empfehlung: **einer** — `duplicate_key_result`, additiv in `ApiErrors`. Die drei
   Indizes sind eine Regel in drei Ausprägungen (der Geltungsbereich hat drei Formen); drei Codes wären drei
   Namen für denselben Fehler des Aufrufers.
2. **Fall 1 (Dublette innerhalb eines POST):** gegen die eingehende Liste prüfen (in-memory) **oder** erst
   speichern und den DB-Fehler fangen? Empfehlung: in-memory prüfen, bevor irgendetwas gespeichert wird — ein
   `catch` nach dem ersten `SaveChanges` hinterlässt ein Ziel mit halber Meilenstein-Menge, und das ist genau
   der Zustand, den die Guard-Clause-Regel verhindern soll.
3. **Gilt die Prüfung auch, wenn der Geltungsbereich `null` ist** (Ziel ohne Fach/Kapitel/Übung)? Der erste
   Index deckt genau diesen Fall über seinen Filter ab; die Prüfung muss die drei Formen darum
   **gleich unterscheiden wie die Filter**, sonst prüft sie zu streng (falsches 409) oder zu lasch (weiter
   500). Empfehlung: die Prüfung genau an den drei Filter-Bedingungen entlang schreiben und das im Kommentar
   an den Index binden.
4. **Nicht in ein Tor gießen** — dieselbe Entscheidung wie in B-97, Entscheidung 3: die Zuordnung
   Index → Schreibpfad ist nicht reflexiv ableitbar (47 `IsUnique`, viele ohne jeden Schreib-Endpunkt).

## Akzeptanzkriterien

1. `POST supervisor/children/{childId}/objectives` mit zwei identischen Meilensteinen (gleicher
   Geltungsbereich, gleiche Metrik) antwortet mit `409` und `code: duplicate_key_result` — **und legt kein
   Ziel an**.
2. `POST …/objectives/{objectiveId}/key-results` auf ein bestehendes Tripel antwortet ebenso.
3. `PATCH …/key-results/{keyResultId}`, das die Metrik auf einen vorhandenen Meilenstein desselben
   Geltungsbereichs schiebt, antwortet ebenso — und lässt den Meilenstein unverändert.
4. Ein Meilenstein, der seine **eigene** Metrik behält, geht weiter durch (keine Selbstkollision).
5. Je Fall ein Integrationstest in `ObjectiveTests.cs`, der **vor** der Änderung rot war (Abnahmeform
   `art: Defekt`).

## Verlauf

- **2026-08-04** — angelegt aus dem `pugling-reviewer`-Befund 2 zum B-97-Bau, direkt als `ausformuliert`:
  der Ist-Stand ist mit `Datei:Zeile` belegt und von mir am Schema und an beiden Dienstmethoden nachgeprüft
  (`PuglingDbContext.cs:628-633` samt Begründungskommentar, `ObjectiveService.cs:205-224` und `:229-244` ohne
  Tripel-Prüfung). **Bewusst nicht in B-97 aufgenommen**: B-97s Akzeptanzkriterien sind ohne diese Ressource
  erfüllt, es sind drei andere Endpunkte in einem Dienst statt zwei Controller-Actions, und die
  In-Request-Dublette (Fall 1) hat eine eigene Form. `prio: P2` wie B-97 — kein Kind ist betroffen, aber die
  Prämie ist es potenziell.
