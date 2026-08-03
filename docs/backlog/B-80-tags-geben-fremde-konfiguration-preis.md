---
tags: [typ/story, status/idee, bereich/backend, rolle/student]
aliases: [Tag-Endpunkt gibt Lösungen preis, ConfigJson über Tags lesbar, Transkript erreichbar]
status: idee
prio: P1
art: Defekt
quelle: B-75 (Review pugling-reviewer, Befund außerhalb des Diffs)
unverifiziert: true
---

# B-80 · Über die Tags kann ein Kind jede Übungs-Konfiguration lesen

Drei für sich harmlose Entscheidungen ergeben zusammen einen Weg, auf dem ein Kind die **vollständige
Konfiguration jeder Übung im Katalog** lesen kann – Lösungen, Alternativen, Hörverstehen-Transkripte:

1. `TagsController` trägt auf Klassenebene nur `[Authorize]`
   ([TagsController.cs:20](../../backend/Pugling.Api/Controllers/Creator/TagsController.cs)). Das ist
   **gewollt**: Das Kind darf seine Übungen selbst markieren.
2. `POST tags/{tagId}/exercises` nimmt **beliebige** Übungs-Ids und prüft nur, ob es sie gibt (`:124-141`) —
   kein Eigentum, kein `ExerciseGrant`, kein `ExecutePublic`.
3. `GET tags/{tagId}/exercises` antwortet mit `ExerciseBrief`, und der führt die **rohe `ConfigJson`**
   ([ExerciseBrief.cs:12](../../backend/Pugling.Contracts/Creator/ExerciseBrief.cs)).

Der Ablauf mit einem Kind-Token: eigenen Tag anlegen (erlaubt, `AuthAccess.cs:110`) → fremde Übung taggen →
Liste abrufen → Konfiguration lesen.

Das hebelt eine Zusicherung aus, die anderswo sorgfältig gehalten wird. Die Ausspielung gibt sich große Mühe,
die Lösung nur auf den Stufen zu zeigen, die sie zeigen dürfen; das Transkript einer Hörverstehen-Übung ist
im Vertrag ausdrücklich „for the creator only, never for the child (anti-cheat)"
([ExerciseConfigs.cs:105](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)) und wird von
[B-75](B-75-lese-hoerverstehen-ohne-inhalt.md) eigens von der Karte ferngehalten. Über diesen Weg ist es
trotzdem lesbar.

**Vorbestehend, nicht von B-75 verursacht** — dort aufgefallen, weil der Review die Frage „kommt das
Transkript irgendwo beim Kind an?" ernst genommen und nicht nur den Kartenpfad geprüft hat.

## Zu prüfen beim Ausformulieren

- **Erst nachspielen.** Der Befund ist am Code hergeleitet; ein Lauf mit echtem Kind-Token gehört davor.
- Ob `ExerciseBrief.Config` überhaupt gebraucht wird, wo er heute verwendet wird (auch Klassenarbeiten
  liefern ihn). Ein `Config`-freier Zwilling für lesende Aufrufer wäre die kleinste Reparatur.
- Ob stattdessen der **Schreib**pfad zu schließen ist (nur eigene/ausführbare Übungen taggen). Das allein
  reicht nicht: Was einem Kind zugewiesen ist, darf es taggen — und dessen Lösungen bekäme es dann immer noch.
- Ob weitere Endpunkte `ExerciseBrief` an nicht-Creator ausgeben.
- Ob eine `unauthorized`-Antwort hier richtig ist oder ein stilles Weglassen des Feldes — ein Kind soll
  seine Übungen ja weiterhin markieren können.

## Verlauf

- **2026-08-02** — angelegt aus dem `pugling-reviewer`-Befund zum Commit `dab72e3` (B-75). `prio: P1`
  vorgeschlagen, weil es eine Anti-Cheat-Zusicherung des Produkts betrifft und ohne Zutun des Vaters
  ausnutzbar ist — nicht vom Nutzer bestätigt.
