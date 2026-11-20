---
tags: [typ/story, status/geschaetzt, bereich/klassenarbeit, bereich/gamification, rolle/supervisor, rolle/student]
aliases: [Klausurnote wirkt, Auf- und Abstieg durch Klausur]
status: geschaetzt
prio: P2
art: Wunsch
groesse: M
wo: backend
migration: offen
vertragsbruch: nein
quelle: B-683
grund: ""
ersetzt_durch: []
---

# B-745 · Die eingetragene Klausurnote hebt oder senkt die Notenstufe

Der Rückkanal von der echten Note zur Stufe: Trägt der Vater eine Note ein oder ändert sie, ergibt sich die Stufe des Fachs als
Funktion aus dem Schnappschuss ([B-744](B-744-klausur-prognose-schnappschuss.md)) und der Note, aufwärts voll, abwärts höchstens eine
Stufe je Klausur. Abgespalten aus [B-683](B-683-notenstufe-an-der-echten-klausur-eichen.md) (Entscheidungen 1, 4, 5 zweiter Teil, 6, 7, 8).
Baut auf [B-741](B-741-notenstufe-berechnen-und-speichern.md) und B-744 auf.

*Entscheidungen stellvertretend im Vorbereitungslauf 2026-11-20 (Sonnet) gefällt, unbestätigt; Herkunft B-683.*

## User Story

Als *Schüler* möchte ich, dass meine Stufe nach einer guten Arbeit steigt und nach einer schlechten höchstens einen Schritt fällt,
damit ich der Stufe vertraue; als *Vater* möchte ich, dass eine Korrektur der Note die Stufe richtigstellt, statt sie doppelt zu
ändern.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`; Pfade relativ zu `backend/Pugling.Api/`.

- Die Note wird nur über die Supervisor-Routen geschrieben: Anlegen `Controllers/Supervisor/KlassenarbeitenController.cs:92` (mit
  optionaler Note), Ändern `:129-164` (`PATCH`, `ClearGrade` bei `:152`), gültig 1,0 bis 6,0 (`:46-47`). Das Schreiben ruft keinen
  weiteren Dienst auf (`:156-164`).
- Das Ziel-Maß `ClassTestGrade` liest die **beste** Note je Fach (`Services/Shared/ObjectiveEvaluationService.cs:105`, `:123-127`);
  es bleibt unverändert, die Stufe ist eine eigene Größe.
- Stufe und Schnappschuss: nicht vorhanden bis B-741 und B-744 gebaut sind.
- Ob zwei Klassenarbeiten desselben Fachs sich in der Reihenfolge ihrer Daten sortieren lassen: ja (`ScheduledDate`, `Id` als
  Tiebreaker), eine „jüngste geschriebene Klausur des Fachs" ist also bestimmbar.
- Wallet: keine Berührung nötig (Entscheidung 8); die Wallet-Invariante gilt für diesen Pfad nicht.

## Entscheidungen

1. **Die Note setzt die Stufe aufwärts voll, abwärts höchstens eine Stufe gegenüber dem Schnappschuss** (B-683 Entscheidung 1).
   Kosten: Ein Kind auf Stufe 2 mit einer 5 steht erst nach zwei Klausuren bei 4. Verworfen: volles Absteigen. Schließt aus: Absturz
   um drei Stufen durch eine Arbeit.
2. **Die Stufe nach der Klausur ist eine reine Funktion aus Schnappschuss und Note; Eintragen oder Ändern ist idempotent je
   Klassenarbeit; eine korrigierte Note ersetzt die Wirkung der alten** (B-683 Entscheidung 5). Die laufende Stufe ändert sich nur,
   wenn es die jüngste geschriebene Klausur des Fachs ist; bei einer älteren zählt nur die Abweichung. Kosten: Klausuren ohne
   Schnappschuss wirken nicht.
3. **`ClassTestGrade` bleibt unverändert** (B-683 Entscheidung 4). Kosten: zwei Größen für „Note im Fach" (beste gegen aktuelle).
4. **Nur Klassenarbeiten mit Fach wirken; die Note trägt der Vater ein, Agent und Foto-Auswertung schlagen nur vor** (B-683
   Entscheidungen 6 und 7).
5. **Keine Münzen für Stufe oder Note** (B-683 Entscheidung 8, Rahmen F).

## Akzeptanzkriterien

1. Eine Note besser als die Schnappschuss-Stufe hebt die Stufe voll, eine schlechtere senkt sie um höchstens eine. *Rot, wenn* die
   Stufe unverändert bleibt oder um mehr als eine fällt.
2. Dieselbe Note zweimal gesendet ändert die Stufe nicht ein zweites Mal. *Rot, wenn* der `PATCH` jedes Mal neu bewertet.
3. Eine korrigierte Note ersetzt die Wirkung der alten. *Rot, wenn* zwei Eintragungen zwei Auf- oder Abstiege auslösen.
4. Die Korrektur der Note einer älteren Klausur ändert die laufende Stufe nicht. *Rot, wenn* die Stufe dabei springt.
5. Eine Klassenarbeit ohne Fach oder ohne Schnappschuss verändert keine Stufe. *Rot, wenn* sie ein beliebiges Fach trifft.
6. `BestLevel` sinkt nie, auch nicht, wenn eine Korrektur die ursprüngliche Hebung zurücknimmt. *Rot, wenn* die Korrektur den
   Bestwert überschreibt.
7. Das Eintragen der Note bucht keine Münzen. *Rot, wenn* ein `ChildPointsEntry` entsteht.
8. *Absichtserklärung.* `ClassTestGrade` liefert für dieselben Daten dasselbe wie vorher. *Rot, wenn* die Umsetzung
   `BestGradeTimesTen` ändert.

## Schätzung

**Größe: M**, wie der Batch-Pfad im `MediaSelector` (B-03): eine reine Funktion, ein Aufruf im Controller, sieben Randfälle.

- **`wo: backend`**: der Weg der Note ist vorhanden, die Anzeige folgt mit B-746 und B-743.
- **`migration: offen`**: Eine **offene Frage** hängt an dieser Story, die keine der sieben Entscheidungen beantwortet. Der
  tägliche Lauf von B-741 rechnet die Stufe aus dem Lernstand neu; der Anstieg ist dort unbegrenzt. Ein Abstieg durch die Klausur
  (zum Beispiel von 2 auf 3) würde am nächsten Tag zurückgehoben, wenn der Lernstand noch Stufe 2 hergibt, und die Klausur
  wirkte nur bis zum nächsten Zugriff. Soll die Klausur dauern, braucht die Tabelle aus B-741 einen Anker (eine Spalte, also eine
  Migration), bis neuer Lernstand ihn überholt. Soll sie das nicht, reicht die Funktion und die Migration entfällt. **Empfehlung:**
  der Anker, sonst ist die Klausur die einzige Quelle, die wirkungslos bleibt; die Frage geht in die Bestätigungsrunde, vor dem Bau
  ist sie zu klären.
- **`vertragsbruch: nein`**: `UpdateClassTestDto` und `KlassenarbeitResponse` bleiben unverändert (die Abweichung als Feld
  liegt in B-746).

### Risiken

- **Der Anker (oben)** ist der Kern der Unsicherheit. Eine gebaute Stufenwirkung, die der Lauf am Folgetag zurücknimmt, besteht alle
  Tests der Funktion und scheitert an der Zusammenarbeit der beiden Stücke.
- **Reihenfolge der Klausuren:** „jüngste geschriebene" nach `ScheduledDate`, nicht nach dem Eintragezeitpunkt; ein nachgetragener
  Altbefund darf die Stufe nicht überschreiben (AK 4).
- **`ClearGrade`:** Wird die Note gelöscht, kehrt die Stufe nicht zurück (der Zustand davor ist nicht gespeichert). Vorschlag: kein
  Zurückdrehen, und den Fall in der Dienst-Doku nennen.
- **Note zu Stufe:** Wie 4,5 in eine ganze Stufe übergeht, ist offen (Vorschlag wie in [B-740](B-740-notenschluessel-percenttograde.md):
  ganze Note, 4,5 gehört zu Stufe 4).
- **Eintragen vor dem Klausurtag** (Note schon eingetragen, Schnappschuss fehlt, weil der Termin nicht erreicht wurde): wirkt nicht
  (AK 5); der Vater sieht keine Fehlermeldung.

### Angriffsplan

1. Frage des Ankers klären (siehe oben); danach entsteht gegebenenfalls eine Migration auf der Tabelle aus B-741.
2. Reine Funktion `LevelAfterExam(predicted, grade)` im Dienst der Stufen; Anwendung nur bei „jüngste geschriebene Klausur".
3. Aufruf in `KlassenarbeitenController.Create` und `Update` nach dem Speichern der Note, im selben Request.
4. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`.

### Testweg

- Neue Testklasse `KlassenarbeitStufeTests` (Integrationstest, `PuglingWebAppFactory`, `TestClock`, Aufbau der Klassenarbeit wie in
  `KlassenarbeitenTests`): AK 1 bis 7, je Kriterium ein Test, der die benannte Änderung als rote Probe überlebt; bei AK 2 und 3 ein
  Test, der **zwei** unterschiedliche Aufrufe sendet (derselbe Wert gegen zwei Werte).
- `ObjectiveTests` (deckt `ClassTestGrade` ab) bleibt unverändert grün (AK 8).
- Vor der Abnahme: Agent `pugling-reviewer`; `/smoke-test` mit zwei Klassenarbeiten im selben Fach.

## Verlauf

- **2026-11-20** — angelegt aus [B-683](B-683-notenstufe-an-der-echten-klausur-eichen.md) (geteilt); `geschaetzt`: M, backend,
  Migration offen (Anker), kein Vertragsbruch (Vorbereitungslauf, Sonnet).
