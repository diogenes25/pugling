---
tags: [typ/story, status/ausformuliert, bereich/backend, rolle/student]
aliases: [Positions-Report gibt die Lösungen preis, ItemReport trägt Answer,
  Kind liest die Lösung jeder Karte im Report, Tür C]
status: ausformuliert
prio: P1
art: Defekt
quelle: B-80 (pugling-reviewer, Befund außerhalb des Diffs)
---

# B-82 · Über den Positions-Report kann ein Kind die Lösung jeder Karte lesen

## User Story

Als **Vater** möchte ich, dass die Lösung einer Karte für mein Kind erst dann lesbar ist, wenn die Stufe sie
zeigen darf, damit der Lernbericht meine Auswertung bleibt und nicht sein Spickzettel wird.

## Ist-Stand am Code

Die **dritte Tür** in dieselbe Kammer wie [B-80](B-80-tags-geben-fremde-konfiguration-preis.md) — und von
dessen Reparatur **nicht** gedeckt, weil sie kein `ExerciseBrief` benutzt.

### 1 · Das DTO trägt die Lösung als eigenes Feld

`ItemReport` führt sie namentlich
([LearnProgressDtos.cs:30](../../backend/Pugling.Contracts/Student/LearnProgressDtos.cs)):

```csharp
public record ItemReport(int ItemIndex, string Prompt, string Answer, bool Introduced, …);
```

Gefüllt wird das Feld aus `ContentItem.Answer`
([PositionReportService.cs:53](../../backend/Pugling.Api/Services/Student/PositionReportService.cs)), und das
ist bei **jedem** Übungstyp die Lösung (Lückentext-Gap, Hörverstehen-Frage, Grammatik, Übersetzung — die
Projektionen in `BuiltInExerciseTypes.cs`).

### 2 · Der Endpunkt ist für das Kind offen — ohne Trick

Route `api/v1/student/study-plans/{planId}/positions/{positionId}/report`
([PositionReportController.cs:14](../../backend/Pugling.Api/Controllers/Student/PositionReportController.cs)),
klassenweit nur `[Authorize]` plus `[ServiceFilter(typeof(PlanOwnershipFilter))]` — und der Filter lässt einen
Student für seinen **eigenen** Plan durch. Das braucht so wenig einen Trick wie Tür B in B-80.

### 3 · Am laufenden System nachgespielt

2026-08-03, Wegwerf-DB auf `:5280` (die echte `pugling.db` unangetastet). Vater legt einen Lückentext an und
eine Position darauf; das Kind hat nie eine Karte gesehen:

```text
GET student/study-plans/{plan}/positions/{pos}/report   (Kind-Token)   → 200
  item 0: introduced=False  prompt='I ___ to school.'  answer='walked'
```

`introduced: false` ist der Punkt: die Lösung kommt **auch** für Karten, die dem Kind noch nie gezeigt
wurden. Das ist genau die Zusicherung, die `CardFacets` auf getippten Stufen hält (kein `reveal`) und an der
[B-75](B-75-lese-hoerverstehen-ohne-inhalt.md) und [B-77](B-77-liste-menge-als-folge.md) gefeilt haben.

### 4 · Der Code weiß, dass er den Vater meint

Der Service-Kommentar lautet „Answers the **supervisor's** question", der Controller „shows the **father**
for each content item" — und trotzdem liegt die Route unter `student/…` und ist für das Kind offen. Die
Absicht steht also im Code; nur die Wand fehlt.

### 5 · Die Testlage

`OwnershipTests.cs:95` prüft für diesen Endpunkt `403` bei einem **fremden** Kind — also genau an der Lücke
vorbei, dieselbe Klasse „Regel getestet, Grenzfall offen" wie bei B-77 und B-80
([docs/testplan.md](../testplan.md)).

## Die echte Lücke

Derselbe Defekttyp wie B-80s Tür B, aber **nicht** dieselbe Stelle: die Ownership-Prüfung ist richtig
(es ist der Plan des Kindes), die **Rollenreichweite eines Lese-DTOs** ist falsch. B-80/E1 hat die
Zusicherung für `ExerciseBrief` zu einer Eigenschaft des *Typs* gemacht — dieser Typ war davon nicht
betroffen, weil er die Lösung nicht als rohe Config trägt, sondern als benanntes Feld. Solange ein DTO unter
`student/…` ein `Answer` führt, ist jede künftige Auswertungssicht ein neuer Kandidat.

## Entwurf der Akzeptanzkriterien

- Ein Kind-Token bekommt über den Positions-Report **keine** Lösung — insbesondere nicht für Karten mit
  `introduced: false`.
- Der Vater sieht den Report unverändert vollständig; sein UI verliert keine Spalte.
- **Regressionstest, vorher rot**: Kind-Token liest den Report seiner eigenen Position; die Antwort trägt
  keine Lösung.

## Offene Punkte

Jeder mit Empfehlung — das ist das Material der Grill-Runde.

1. **Welcher Schnitt: Feld leeren oder Route gaten?** `Answer` für ein Student-Token weglassen (nullable,
   die Rolle entscheidet) **oder** den Endpunkt auf `[Authorize(Roles = Roles.Supervisor)]` heben.
   *Empfehlung:* **erst zählen, dann entscheiden** (siehe Punkt 2). Gaten ist billiger und deckt sich mit dem
   Kommentar; ein leeres Feld verlagert die Zusicherung dagegen wieder in den Endpunkt — genau das, was
   B-80/E1 abgeschafft hat. Wenn ein Sohn-UI den Report zeigt, ist Gaten aber ausgeschlossen.
2. **Hat `answer` überhaupt einen Verbraucher?** Bei B-80 hatte das entsprechende Feld **keinen**, und das
   machte die Reparatur dort billig. Hier ist ein Vater-UI wahrscheinlich (der Report ist für ihn gedacht).
   *Empfehlung:* `frontend/src/` und `Pugling.Client` zählen, bevor über Punkt 1 entschieden wird — nicht
   vermuten.
3. **Zieht das Ebenen-Präfix mit?** Liegt die Route falsch unter `student/…`?
   *Empfehlung:* **nicht anfassen**, wie B-80/E5 es für die Klausur entschieden hat — mit dem Unterschied,
   dass hier der Kommentar den Vater als Leser benennt. Als dokumentierter Geruch festhalten, nicht als
   eigene Story.
4. **Sagen weitere Felder zu viel?** `prompt` ist unkritisch (es steht auf der Karte), `box`/`dueOn`/
   `testsCorrect` sind Lernstand, den das Kind lesen darf.
   *Empfehlung:* nur `answer` schneiden.
5. **Hat der Nachbar `ChildLearnProgressController` dieselbe Bauart?**
   *Empfehlung:* beim Ausformulieren mitprüfen. Der Reviewer hat `ChildVocabularyProgressController` geprüft:
   der gibt Wort/Übersetzung nur zu **bereits beantworteten** Items heraus, ist also eine andere Familie —
   nahe an [B-81](B-81-vokabel-tags-geben-uebersetzungen-preis.md). `ChildLearnProgress` ist **nicht**
   einzeln geprüft.

## Verlauf

- **2026-08-03** — angelegt aus dem `pugling-reviewer`-Befund zur Abnahme von B-80, und gleich
  **ausformuliert**: der Ist-Stand ist am Code belegt *und* am laufenden System nachgespielt (ein Kind-Token
  liest `answer` für eine Karte mit `introduced: false`), also wäre `idee` mit seinem `unverifiziert: true`
  die falsche Stufe gewesen — der Index-Wächter hat das prompt gemeldet. Nicht dem Reviewer geglaubt, sondern
  selbst nachgespielt; er hatte den Befund nur aus dem Code gelesen.
  `prio: P1` in Analogie zu B-80 vorgeschlagen (dieselbe Anti-Cheat-Zusicherung, ohne Zutun des Vaters
  ausnutzbar) — nicht vom Nutzer bestätigt.
  Bewusst **nicht** in B-80 eingefaltet: dessen Akzeptanzkriterien sind auf die *Konfiguration* geschnitten
  und wörtlich erfüllt; hier trägt ein **anderes** DTO die Lösung als eigenes Feld, an einem anderen
  Endpunkt, mit einer eigenen Entscheidung (Feld leeren vs. Route gaten). Handhabung wie B-76 → B-79 und
  B-80 → B-81.
