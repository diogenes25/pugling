---
tags: [typ/story, status/idee, bereich/backend, rolle/supervisor]
aliases: [Stufe ohne Validierung, Stage 99, Unbekannte Stufe deckt die Lösung auf]
status: idee
prio: P2
art: Defekt
quelle: B-76 (Review pugling-reviewer, Befund 7)
unverifiziert: true
---

# B-79 · Die Stufe einer Position wird gegen nichts geprüft

`POST`/`PATCH` auf `…/study-plans/{planId}/positions` nehmen `Stage` als nackten `int` entgegen und
schreiben ihn ungeprüft an die Position
([PlanPositionsController.cs:113](../../backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs)
und `:155`). Jeder Übungstyp führt aber eine Liste seiner gültigen Stufen (`IExerciseType.StageOptions`),
und gegen die wird nicht abgeglichen. `Stage = 99` ist damit setzbar.

Das bleibt nicht folgenlos, weil die Stufe entscheidet, **ob die Karte die Lösung zeigt**: `IsTypedStage`
fällt für einen unbekannten Wert auf `false` zurück, und `CardFacets` liefert dann `Reveal` mit der
Antwort. Eine unsinnige Stufe macht die Übung also nicht kaputt — sie deckt sie auf.

Der Weg dorthin führt über den Supervisor, nicht über das Kind (die Stufe kommt beim Spielen aus dem
Fahrplan). Es ist also kein Selbstbetrugs-Pfad des Kindes, sondern ein Versehen, das lautlos wirkt: Wer
sich vertippt, bekommt keine Fehlermeldung, sondern eine Position, die die Lösungen verschenkt.

## Warum das jetzt auffällt

Vorbestehend, aber durch [B-76](B-76-lueckentext-karte-ohne-luecke.md) sichtbarer geworden. Seit dessen
Entscheidung E6 sind **alle drei** `ClozeStage`-Werte getippt — ein unbekannter Wert ist damit der einzig
verbliebene Weg zu einer aufgedeckten Lückentext-Karte. Vorher traf derselbe Fehler noch eine reguläre
Stufe und fiel darum weniger auf.

## Zu prüfen beim Ausformulieren

- Ob `StageOptions` wirklich für **alle** Typen vollständig ist — sonst weist die Validierung gültige
  Stufen ab. `ClozeStage.WordBank = 1` steht zum Beispiel im Enum, aber in keiner `StageOptions`-Liste
  (bewusst, siehe Kommentar in `ClozeEntities.cs:9-11`).
- Ob es Bestandsdaten mit einer Stufe außerhalb der Liste gibt (Seed prüfen, dann eine echte DB).
- Wo die Prüfung sitzt: im Controller (dann braucht sie einen `ApiErrors`-Code) oder als geteilte
  Hilfsfunktion neben `StageForDay`. Auch der Vorschau-Endpunkt nimmt einen `stageOverride`
  (`ExercisePreviewService.cs:35`) — derselbe Fall.
- Ob `IsTypedStage` zusätzlich **fail-safe** werden sollte: bei unbekannter Stufe eher `true` (nichts
  verraten) als `false`. Zwei Netze statt einem, und das zweite kostet eine Zeile.

## Ein kleinerer Befund aus derselben Runde

`ClozeExerciseType.Choices` deserialisiert die ganze `ClozeConfig` **je Karte**
([BuiltInExerciseTypes.cs:143](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)) — bei 20
Lücken 20 Parses derselben Zeichenkette für einen `GET …/cards`. Unkritisch und bewusst nicht mit B-76
behoben (der Typ ist zustandslos, ein Cache wäre die erste Ausnahme davon); hier notiert, damit es nicht
verlorengeht.

## Verlauf

- **2026-08-02** — angelegt aus dem `pugling-reviewer`-Befund zum Commit `1125ee6` (B-76), Punkt 7.
  `prio: P2`: Es wirkt heute an niemandem — es braucht erst einen Vertipper des Supervisors —, aber die
  Folge ist eine stillschweigend aufgedeckte Übung. Nicht vom Nutzer bestätigt.
