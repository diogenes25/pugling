---
tags: [typ/konzept, bereich/doku]
---

# Architektur-Resümee: aktueller Study-Kern

Stand: 2026-07-06. Dieses Resümee beschreibt den aktuellen Zustand nach dem Umbau vom plan-weiten
`StudyPlanItem`/`Method`-Modell auf positionsbasierte Lehrpläne.

## 1. Ist der Code erweiterbar?

**Ja – der Rahmen ist jetzt stärker entkoppelt.** Ein `StudyPlan` ist ein Container für Kind, Titel,
Laufzeit und optionales Fach. Jede `PlanPosition` referenziert eine Katalog-`Exercise` und trägt ihre
eigenen Regeln: Stufe/Fahrplan, Item-Auswahl, Zielrhythmus, Bestehensschwelle, Leitner und Punkte.

Ein neuer Katalogtyp kostet im Normalfall:

1. `ExerciseType`-Wert und Config-`record`s ergänzen.
2. Controller mit Route/Tags/`Type` hinzufügen (CRUD kommt aus `ExerciseControllerBase<TConfig>`).
3. Falls prüfbar: `ExerciseAnswerChecker` bzw. Generator ergänzen.
4. Falls im Study-Plan spielbar: `ExerciseContentProvider`/Resolver auf `ContentItem` projizieren,
   `PositionPlayService.IsTypedStage` prüfen und Manifest aktualisieren.
5. Tests ergänzen.

Der Vorteil: Der positionsbezogene Practice-/Test-Pfad bleibt gleich:
`/study-plans/{planId}/positions/{positionId}/practice-sessions` und
`/study-plans/{planId}/positions/{positionId}/tests`.

## 2. Ist der Prozess erweiterbar?

Ja. Neue Inhalte entstehen API-first im Katalog (`Subject → Chapter → Exercise`). Der Vater baut daraus
einen Plan-Container und hängt Übungen als Positionen an. Damit kann ein Plan gemischte Übungstypen
enthalten, ohne für jedes Verfahren neue plan-weite Felder oder eigene Plan-Controller einzuführen.

## 3. Sind Logik und Ziel konsistent?

Ja. Das Ziel „Vater kontrolliert, Sohn lernt mit Spaß" spiegelt sich in den Regeln:

- **Kontrolle/Zwang:** Ownership-Filter, nur ein aktiver spielbarer Plan je Kind, `PlanInactive` für
  abgelaufene/deaktivierte Pläne, Backfill nur Vater, serverseitig erzwungene Stufen.
- **Anti-Schummel:** Der Sohn sendet Antworten, nie richtige/falsche Wahrheit; getippte Stufen und
  `requireTypedTest` verhindern bloßes Durchklicken.
- **Motivation:** Leitner, Combo/Speed, Missions/Achievements, Gems/Coins und Vater-Angebote.
- **Transparenz:** `overview`, `overview/progress`, Positionsreport und Punkte-Ledger.

## 4. Ist der Code dokumentiert?

Weitgehend ja. Öffentliche Controller/DTOs/Services tragen deutsche XML-Docs, Swagger/OpenAPI bleibt
die autoritative API-Doku, und `DocsCaptureTests` erzeugt verifizierte Beispiele unter
[docs/api-examples/](api-examples/index.md).

## 5. Offene Punkte / Empfehlungen

1. **PINs im Klartext** (`Father.Pin`, `Child.Pin`) – vor Produktion hashen, Rate-Limit/Lockout ergänzen.
2. **Zeitfenster-Verwaltung** – `TimeSlotRule` ist noch nur über Seed/DB änderbar; bei Bedarf Admin-API ergänzen.
3. **UTC-Tagesgrenzen** – Tageslogik nutzt UTC; für echte Familiennutzung ggf. Kind-Zeitzone speichern.
4. **Performance** – bei großen Datenmengen `overview/progress` gruppiert/projiziert optimieren.
5. **Store-Mandantenfrage** – Vokabel-/Cloze-Stores sind globale Bibliotheken; bei mehreren Familien ggf.
   Ownership/Autorenmodell wie beim Katalog nachziehen.
