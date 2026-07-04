# Tagging & Klassenarbeiten

Zwei zusammenhängende Backend-Features (reine API-Phase, noch kein Frontend – wie beim übrigen
Lern-Katalog):

1. **Tagging** – Vater *und* Sohn markieren Katalog-Übungen mit frei benannten Schlagwörtern
   (z. B. „Unit 5", „unregelmäßige Verben"), etwa um sie als relevant für eine bestimmte
   Klassenarbeit zu kennzeichnen.
2. **Klassenarbeiten** – der Vater plant Arbeiten, weist ihnen relevante Übungen zu und trägt nach
   dem Schreiben die Note nach. Daraus lässt sich **gezielt für eine anstehende Arbeit üben** bzw.
   lassen sich **Übungen schlecht benoteter Arbeiten wiederholen**.

## Datenmodell

Der Lern-Katalog (`Subject → Chapter → Exercise`) bleibt **kindneutral**. Alles Kindbezogene hängt
an Verknüpfungstabellen:

```
Child ─┬─< Tag ─────< ExerciseTag >───── Exercise (Katalog)
       │
       └─< Klassenarbeit ─┬─< KlassenarbeitExercise >── Exercise   (direkte Zuordnung)
                          └─< KlassenarbeitTag >──────── Tag        (Zuordnung über Tag)
```

- **`Tag`** – pro Kind, Name je Kind eindeutig; `CreatedBy` (Vater/Sohn), optionale `Color`.
- **`ExerciseTag`** – markiert eine Übung mit einem Tag; `TaggedByRole` hält fest, wer markiert hat.
- **`Klassenarbeit`** – `ChildId`, optional `SubjectId`, `Title`, `Topic`, `ScheduledDate`,
  `Status` (`Planned`/`Written`/`Cancelled`), `Grade` (1,0–6,0) + `GradeComment`.
- **`KlassenarbeitExercise`** – direkte Zuordnung einer Übung.
- **`KlassenarbeitTag`** – verknüpft einen Tag; **alle** so markierten Übungen gelten als relevant.

**Relevante Übungen einer Klassenarbeit** = direkt zugewiesene Übungen **∪** Übungen der verknüpften
Tags (dublettenfrei). So kann man entweder einzeln zuweisen oder über einen Tag ganze Themenblöcke
auf einmal einbeziehen.

## Rechte

- **Taggen** (Tag anlegen/ändern/löschen, Übungen markieren): **Vater und Sohn**, jeweils nur im
  Rahmen des eigenen Kindes (Ownership über `AuthAccess.OwnsChildAsync`).
- **Klassenarbeiten** anlegen, ändern, benoten, Übungen/Tags zuweisen: **nur Vater** – passend zur
  App-Philosophie „Vater hat die Kontrolle".
- **Lesen** (Liste/Detail) sowie **Üben** und **Wiederholen**: beide Rollen.

## API

Alle Endpunkte unter `[Authorize]`; JWT wie üblich per PIN-Login (`POST /api/auth/father` bzw.
`/api/auth/child`).

### Tags – `api/tags`

| Methode & Pfad | Rolle | Zweck |
|---|---|---|
| `GET /api/tags?childId=` | beide | Tags eines Kindes (mit Anzahl markierter Übungen) |
| `POST /api/tags` | beide | Tag anlegen `{ childId, name, color? }` |
| `PATCH /api/tags/{id}` | beide | umbenennen / Farbe ändern |
| `DELETE /api/tags/{id}` | beide | Tag löschen (entfernt Markierungen + KA-Verknüpfungen) |
| `POST /api/tags/{id}/exercises` | beide | Übungen markieren `{ exerciseIds: [] }` |
| `DELETE /api/tags/{id}/exercises/{exerciseId}` | beide | Markierung entfernen |
| `GET /api/tags/{id}/exercises` | beide | Übungen dieses Tags |
| `GET /api/tags/for-exercise/{exerciseId}?childId=` | beide | Tags einer Übung im Kontext des Kindes |

### Klassenarbeiten – `api/klassenarbeiten`

| Methode & Pfad | Rolle | Zweck |
|---|---|---|
| `GET /api/klassenarbeiten?childId=&status=&subjectId=` | beide | Liste (optional gefiltert) |
| `GET /api/klassenarbeiten/{id}` | beide | Detail inkl. direkt zugewiesener Übungen |
| `POST /api/klassenarbeiten` | Vater | planen/anlegen; optional `exerciseIds`/`tagIds` gleich mitgeben |
| `PATCH /api/klassenarbeiten/{id}` | Vater | ändern, **Note nachtragen**, Status setzen |
| `DELETE /api/klassenarbeiten/{id}` | Vater | löschen |
| `POST /api/klassenarbeiten/{id}/exercises` | Vater | Übungen direkt zuweisen `{ exerciseIds: [] }` |
| `DELETE /api/klassenarbeiten/{id}/exercises/{exerciseId}` | Vater | Zuordnung entfernen |
| `POST /api/klassenarbeiten/{id}/tags/{tagId}` | Vater | Tag verknüpfen |
| `DELETE /api/klassenarbeiten/{id}/tags/{tagId}` | Vater | Tag-Verknüpfung lösen |
| `GET /api/klassenarbeiten/{id}/practice` | beide | **gezielt üben**: relevante Übungen + Tage bis Termin |
| `GET /api/klassenarbeiten/repeat?childId=&minBadGrade=` | beide | **wiederholen**: Übungen aller Arbeiten mit Note ≥ Grenze (Standard 4,0) |

### Note & „schlechte Note"

Deutsche Skala **1,0 (sehr gut) … 6,0 (ungenügend)**; höhere Zahl = schlechter. Der
`repeat`-Endpunkt sammelt die Übungen aller *geschriebenen* Arbeiten mit `Grade ≥ minBadGrade`
(Standard `4.0`). Wird beim `PATCH` eine Note gesetzt und der Status ist noch `Planned`, wechselt er
automatisch auf `Written`.

## Hinweis zur Datenbank

Das Schema wird per `EnsureCreated()` erzeugt (noch keine EF-Migrationen). Nach diesen neuen
Entitäten muss die Entwicklungs-DB **einmal neu angelegt** werden: `pugling.db` löschen und die API
neu starten – der Seed legt Beispiel-Tags und zwei Beispiel-Klassenarbeiten (eine geplant, eine mit
schlechter Note) an.
