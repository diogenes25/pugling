---
tags: [typ/story, status/geschaetzt, bereich/ki-agent, bereich/training, rolle/creator, rolle/supervisor]
aliases: [Förder-Übungen, KI-Creator nachts, Zusammenfassung der Tagesauswertung, Etappe E5]
status: geschaetzt
prio: P2
art: Wunsch
groesse: M
wo: beides
migration: ja
vertragsbruch: nein
quelle: B-644
grund: ""
ersetzt_durch: []
---

# B-739 · Der KI-Creator baut nachts Förder-Übungen und schreibt eine kurze Zusammenfassung (B-644, Etappe E5)

Zehnte Teilstory aus der Teilung von [B-644](B-644-tagesplaner-statt-fester-lauf.md). Das Sprachmodell kommt nur **außerhalb** der Entscheidung
dazu: Der KI-Creator (Konsolen-Agent) baut zu den schwachen Wörtern eines Kindes Förder-Übungen und schreibt auf Wunsch eine kurze
Zusammenfassung der Tagesauswertung in Sätzen. Pflicht wird eine Förder-Übung erst, wenn ein Mensch sie in einen Plan übernimmt.

**Stand:** Eintrittsbedingung `gegrillt` aus B-644 geerbt. Entscheidung 10 stammt vom Vater, 21 und 25 sind stellvertretend und unbestätigt.

## User Story

Als *Vater* möchte ich, dass zu den Wörtern, an denen mein Kind hängt, nachts passende Übungen entstehen und die Tagesauswertung in zwei Sätzen
erzählt wird, damit ich nicht jede Übung selbst baue. Als *Vater* möchte ich dabei die Kontrolle behalten: Nichts, was ein Modell gebaut hat,
wird ohne mich Pflicht.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`. Pfade relativ zu `backend/`.

- **Der Agent kann Übungen aus schwachen Wörtern schon:** `--use-weak` (`Pugling.Agent.Creator/AgentCommands.cs:509`, `:575`;
  `GenerationRequest.cs:36`) nimmt die schwach beherrschten Wörter eines Kindes als Wortschatz (`--child` verpflichtend, Meldung `:511`);
  Hilfetext `:935`. Ein eigener Betriebsmodus „weakness" (`--mode weakness`) ist **nicht gefunden** (Suche nach `weakness` in
  `Pugling.Agent.Creator/*.cs` ohne Treffer).
- **Der Agent läuft außerhalb der API** als Konsolen-App mit dem Konto des Vaters (`AgentCommands.cs:268`) und legt Pläne **inaktiv** an
  (`PlanPlanner.cs:101`); die Freigabe liegt beim Vater (B-681 Entscheidungen 2 und 10).
- **Einen Zeitplan („nachts") gibt es nicht** im Repo: Der Agent wird von Hand gestartet; ein Scheduler im Backend existiert nicht (siehe
  [B-734](B-734-planer-kern-und-schattenmodus.md)), und der Agent ist bewusst kein Teil der API.
- **Eine Ablage für die Zusammenfassung gibt es nicht;** [B-733](B-733-tagesauswertung-und-leistungsprofil.md) legt `DailyEvaluation` an, ohne
  Textfeld.
- **Rückmeldungen im Creator:** B-630 (Rückmeldung zur Karte) und `ExerciseFeedback` existieren (`Models/FeedbackEntities.cs`); sie sind keine
  Eingabe dieses Agentenlaufs.
- **Kein Zugriff auf Plan-Steuerung:** Der Agent entscheidet keine Pflicht; nach Rahmen A bleibt er das Werkzeug für Inhalte
  (B-644 Entscheidung 21).

## Entscheidungen (geerbt)

1. **Ein Sprachmodell kommt nur außerhalb der Entscheidung dazu: Der KI-Creator baut nachts Förder-Übungen zu den schwachen Wörtern, und auf Wunsch
   entsteht eine kurze Zusammenfassung der Tagesauswertung in Sätzen** (B-644 Entscheidung 10, Vater).
2. **Modellerzeugter Inhalt wird erst durch einen Menschen Pflicht** (Entscheidung 25, Rahmen G, stellvertretend, unbestätigt): Förder-Übungen
   entstehen als Katalog-Übung ohne Planverweis; Pflicht werden sie erst, wenn ein Mensch sie in einen Plan übernimmt (`assign`, Positionsformular,
   Einschalten der Stufung). Der Planer wählt nur aus Positionen des aktiven Plans und legt aus Modellinhalt keine Position selbst an.
3. **Der Agent entscheidet keine Pflicht** (Entscheidung 21, Rahmen A); er bleibt ein Werkzeug außerhalb der API mit dem Konto des Vaters
   (B-681 Entscheidung 2).
4. **Fällt das Sprachmodell aus, entsteht der Tagesplan trotzdem** (B-644 Kriterium 14): Der Planer hat keine Abhängigkeit auf diesen Lauf.

### Schätz-Annahmen (stellvertretend, unbestätigt)

1. **„Nachts" heißt: der Betreiber startet den Agenten per Windows-Aufgabenplanung** (`--mode weakness` neu, je Kind); kein Scheduler im Backend,
   keine Abhängigkeit des Planers. Begründung: der Agent ist bewusst kein Teil der API. Verworfen: ein Hintergrunddienst im Backend, der das Modell
   ruft (verletzte Entscheidung 21).
2. **Die Zusammenfassung liegt als Textfeld an `DailyEvaluation`** (`Summary`, nullable) und wird über `PUT supervisor/children/{childId}/evaluations/{day}/summary`
   geschrieben (Supervisor-Claim, Eigentum). Begründung: Der Agent läuft mit dem Konto des Vaters; die Auswertung bleibt die eine Quelle. Kosten: eine
   Spalte und ein Endpunkt, Migration.
3. **Förder-Übungen tragen eine Herkunftsmarke** (Tag oder Titelpräfix), damit der Vater sie im Katalog findet; sie sind **normale** Katalog-Übungen
   ohne Planverweis, ein Veröffentlichen-Flag gibt es im Modell nicht und dieser Zuschnitt führt keines ein.
4. **Die Förder-Übung entsteht nur aus schwachen Wörtern des Kindes unter der Schwelle `ItemProgress.WeakBelowPercent`** und nur, wenn heute noch keine
   Förder-Übung zu diesen Wörtern vorliegt (Idempotenz je Kind und Tag).

## Akzeptanzkriterien

1. `--mode weakness --child <id>` erzeugt aus den schwach beherrschten Wörtern des Kindes eine Übung im Katalog und legt **keine** Position und keinen
   Plan an. *Rot, wenn* der Lauf eine Position anlegt oder eine Übung dem Kind zuweist.
2. Ein zweiter Lauf am selben Tag erzeugt zu denselben Wörtern keine zweite Übung. *Rot, wenn* sich die Übungen verdoppeln.
3. Ohne schwache Wörter erzeugt der Lauf nichts und meldet das. *Rot, wenn* eine leere Übung entsteht (`EmptyExerciseGuardTests`).
4. Die Zusammenfassung wird über den Endpunkt gespeichert und im Vater-Web an der Tagesauswertung gezeigt; die Sohn-App zeigt sie nicht. *Rot, wenn* ein
   Student-Endpunkt den Text liefert.
5. Fällt der Chat-Client aus (`FakeChatClient` wirft), entsteht der Tagesplan trotzdem und der Agentenlauf endet mit einer Fehlermeldung, ohne Teilstand.
   *Rot, wenn* der Planer-Dienst eine Abhängigkeit auf den Agenten oder einen Chat-Client erhält.
6. Der Planer wählt keine Förder-Übung ohne Position: ein Plan nimmt eine Übung nur auf, wenn ein Mensch sie zugewiesen hat. *Rot, wenn* der Planer eine
   Übung ohne Planposition in einen Tagesplan legt.
7. `PUT …/summary` verlangt Supervisor-Claim und Eigentum: ein Konto nur mit Creator-Profil `403`, ein fremder Vater `404`. *Rot, wenn* ein Gate fehlt.
8. Die Migration lässt `DailyEvaluation` mit allen Zeilen bestehen (`Summary` leer). *Rot, wenn* die Migration fehlt.

## Schätzung

**Größe: M** — Anker: wie der Batch-Pfad im `MediaSelector`: ein neuer Betriebsmodus im vorhandenen Agenten (Pipeline und `--use-weak` stehen), ein
Endpunkt, eine Spalte. Das Frontend ist ein Textblock in einer bestehenden Ansicht.

- **`migration: ja`** — `DailyEvaluation.Summary` (nullable Text). An die Kette hängen.
- **`vertragsbruch: nein`** — ein neuer Endpunkt, das Antwort-DTO der Auswertung bekommt ein optionales Feld (additiv).
- **`wo: beides`** — Backend zuerst: der Agent liegt in `Pugling.Agent.Creator` (eines der fünf Backend-Projekte); die Anzeige der Zusammenfassung ist ein Textblock
  in der Vater-Ansicht aus B-733 und wird dort mit gebaut (im Verlauf benennen).

### Risiken

- **Modellqualität:** C# prüft die Lösbarkeit (B-460), nicht die Didaktik; die Qualität hängt am lokalen Modell (Ollama). Darum Rahmen G: Pflicht erst nach
  einem Menschen.
- **Lauf von Hand, nicht Teil des Servers:** Der Agent braucht ein laufendes Backend und das Konto des Vaters (PIN in der Agenten-Konfiguration,
  B-681 Entscheidung 2).
- **Idempotenz:** Ohne Marke und Prüfung entstehen jede Nacht neue Förder-Übungen derselben Wörter.
- **Zusammenfassung ohne Faktenprüfung:** Der Text ist Prosa über Zahlen; er darf die Zahlen nicht ersetzen (Anzeige neben, nicht statt).
- **Fremde Sohn-Dateien:** Diese Story fasst `frontend/src/sohn/*` nicht an.

### Angriffsplan

1. Betriebsmodus `weakness` im Agenten (Wörter holen, Übung bauen, ohne Zuweisung), Marke und Idempotenz.
2. Migration und Endpunkt für die Zusammenfassung; Agentenschritt „Satz schreiben".
3. Anzeige im Vater-Web (Textblock an der Tagesauswertung).
4. Skill `ki-creator` um den Modus ergänzen (Dokumentation).

### Testweg

- `CreatorAgentTests` (vorhandene Klasse, `FakeChatClient`): neuer Modus `weakness`, keine Position, Idempotenz, leere Wortliste, Ausfall des Clients. Rote
  Probe je AK: die Positionserzeugung im Modus einhängen lässt AK 1 fallen; die Marken-Prüfung entfernen AK 2.
- `TagesplanerTests` aus [B-734](B-734-planer-kern-und-schattenmodus.md) erhalten einen Fall „Modell aus": Planer ohne Chat-Client (AK 5).
- `ZusammenfassungTests` (Integration: Endpunkt, Gates, Sohn sieht nichts), Migration `ZusammenfassungMigrationTests` nach `ActivatedAtMigrationTests`.
- Frontend: Vitest für den Textblock; E2E-Regression `frontend/e2e/auswertung-verlauf.spec.ts` um die Zusammenfassung erweitert.
- Vor der Abnahme: `pugling-reviewer`; ein Rollengang als Vater (Zusammenfassung sehen) und der Agentenlauf einmal gegen die Wegwerf-DB (`/smoke-test`-Weg),
  nie gegen `pugling.db`.

## Reihenfolge und Abhängigkeiten

Nach [B-733](B-733-tagesauswertung-und-leistungsprofil.md) (Auswertung als Träger der Zusammenfassung) und [B-737](B-737-reset-zum-stichtag.md) (scharfer
Planer); die Förder-Übungen selbst setzen nur den Katalog und das Profil voraus und könnten früher laufen. Zuletzt der Kette: ein späterer, weicher Ausbau.

## Verlauf

- **2026-11-20** — angelegt durch Teilung von B-644 (Etappe E5) und `gegrillt` → `geschaetzt` im Vorbereitungslauf (Sonnet): Eintrittsbedingung `gegrillt`
  aus B-644 geerbt. Nachgesehen: `--use-weak` ist gebaut, ein Modus `weakness` und ein Zeitplan fehlen, eine Ablage für die Zusammenfassung fehlt. M, beides,
  Migration, kein Vertragsbruch.
