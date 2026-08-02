---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [Komma-Feld ablösen, Wiederhol-Felder]
status: idee
prio: P3
art: Defekt
quelle: B-65 (Entscheidung 7)
---

# B-69 · Drei Editoren tragen Alternativen als Komma-Feld — eine Alternative mit Komma geht verloren

Lückentext, Liste und Übersetzung führen ihre Alternativen als **ein** Textfeld „Alternativen
(kommagetrennt)" ([exerciseConfig.tsx:493,501,521](../../frontend/src/vater/exerciseConfig.tsx)); beim
Senden zerlegt `splitList` es am Komma (`:67-71`), beim Laden fügt `joinList` es wieder zusammen (`:74`).
Im Vertrag ist es längst ein Array (`List<string>?`,
[ExerciseConfigs.cs:80,154,218](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)) — das Komma
ist reine Oberfläche.

Der Fehler daran: **eine Alternative, die selbst ein Komma enthält, ist nicht eintragbar** und wird
stillschweigend in zwei zerrissen. Kein Fehler, keine Meldung.

[B-65](B-65-vokabel-mehrere-uebersetzungen.md) baut für den Vokabel-Editor die Gegenform als
wiederverwendbare Komponente (ein Feld je Variante, „+ Variante", Entfernen) und lässt die drei
bestehenden Stellen ausdrücklich stehen, um die Defekt-Story nicht auf L zu treiben. Diese Story zieht sie
nach.

**Zu prüfen beim Ausformulieren:** der Rückweg (`joinList`) und die E2E-Tests der drei Übungstypen hängen
mit dran; ob im Bestand bereits zerrissene Alternativen liegen, ist unbekannt.

## Verlauf

- **2026-08-02** — angelegt als Folge von B-65, Entscheidung 7.
