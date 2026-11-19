---
tags: [typ/story, status/geschaetzt, bereich/agent, rolle/creator]
aliases: [KI-Creator aus Hausaufgabe, create --homework]
status: geschaetzt
prio: P2
art: Wunsch
groesse: M
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-651
grund: ""
ersetzt_durch: []
wartet_auf: Bestätigung der Sonnet-Entscheidungen
---

# B-656 · KI-Creator: Übung aus einer WebUntis-Hausaufgabe erzeugen (`homework`, `create --homework`)

Dies ist der KI-Creator-Teil aus [B-651](B-651-webuntis-hausaufgaben-als-pflicht.md). Die Story hängt an der
API aus [B-654](B-654-webuntis-connector-backend.md). Die Methoden dafür in `Pugling.Client` entstehen hier.

## User Story

Als **Creator** möchte ich mit dem KI-Creator aus einer offenen Hausaufgabe meines Kindes eine Übung im
Katalog erzeugen. Den Übungstyp wähle ich selbst; in den Plan übernehme ich sie wie bisher mit `assign`.

## Ist-Stand am Code

Steht mit Belegen (`Datei:Zeile`) in [B-651 → Ist-Stand am Code](B-651-webuntis-hausaufgaben-als-pflicht.md#ist-stand-am-code)
(Stand `main` 2adb3744, 2026-11-18). Vor dem Bauen gegen den dann aktuellen Code prüfen. Für diese Story
nachgesehen am 2026-11-19:

- Die Verben des Agenten laufen über einen `switch` in `AgentCommands.RunAsync`
  (`backend/Pugling.Agent.Creator/AgentCommands.cs:199-229`); `homework` fehlt.
- `--topic` landet als `GenerationRequest.Topic` im Briefing
  (`AgentCommands.cs:562`, `backend/Pugling.Agent.Creator/Briefing/BriefingBuilder.cs:118`).
- Die Reihe kommt heute aus Profil oder Buch des Kindes (`BestProfileForChildAsync`, `AgentCommands.cs:666`;
  `SeriesOfChildsBookAsync`, `:620`). Ein Fach als Ausgangspunkt gibt es dort nicht, aber der Client kann Profile
  nach Fach filtern (`CreatorApi.MatchProfilesAsync(childId, subjectId)`, `backend/Pugling.Client/CreatorApi.cs:141`).
- Die Unit wählt der Agent über das aktuelle Lehrbuch des Kindes (`CurrentUnitOfChildAsync`, `AgentCommands.cs:590`).
- Fehler des Servers und Bedienfehler fängt `Program` lesbar ab (`AgentUsageException`, `PuglingApiException`,
  `backend/Pugling.Agent.Creator/Program.cs:48-73`).
- `Pugling.Client` kennt noch keinen Hausaufgaben-Aufruf (nicht gefunden); `SupervisorApi` ist die Stelle
  (`backend/Pugling.Client/SupervisorApi.cs:8`).

## Entscheidungen

Übernommen aus [B-651 → Entscheidungen](B-651-webuntis-hausaufgaben-als-pflicht.md#entscheidungen) (vom Vater im
Dialog bestätigt). Hier tragen vor allem 8 (die Reihe kommt über `subjectId`) und 9 (Hausaufgabe als Eingabe der
bestehenden `create`-Pipeline, C# entscheidet).

_Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt (gilt für 10 bis 13)._

- Entscheidung 10: **„Offen“ heißt: nicht erledigt, also einschließlich verpasster Aufgaben; verpasste tragen die Marke
    `verpasst`.** Begründung: WebUntis setzt `completed` nicht verlässlich (im November standen alle 14 Aufgaben
    auf `false`), eine Aufgabe, die der Vater als Stoff will, bleibt Stoff. Verworfen: nur noch nicht fällige
    Aufgaben. Kosten: die Liste enthält Altlasten, begrenzt durch den Zeitraum (Nr. 11). Ausgeschlossener Fall:
    eine verpasste Aufgabe, aus der man keine Übung mehr erzeugen kann; die User Story verlangt das nicht.
- Entscheidung 11: **Der Zeitraum ist vorgegeben: 14 Tage zurück bis 30 Tage voraus, mit `--from`/`--to` (`yyyy-MM-dd`)
    überschreibbar.** Begründung: Der Probe-Login hat ±30 Tage geholt (Verlauf von B-654), die Rückschau kürzer,
    weil alte verpasste Aufgaben keinen Stoff mehr liefern. Verworfen: kein Vorgabezeitraum, Pflichtangabe.
    Kosten: eine ältere Aufgabe braucht `--from`. Ausgeschlossener Fall: `create --homework` mit einer Id, die nur
    außerhalb des Vorgabezeitraums liegt, scheitert ohne `--from` mit „nicht gefunden“.
- Entscheidung 12: **`create --homework` holt die Aufgabe live (Entscheidung 4 aus B-651: keine Kopie) und nimmt Text und
    Bemerkung als Thema ins Briefing; Lehrkraft und Kindname bleiben draußen. `--homework` zusammen mit
    `--topic`, `--words` oder ohne `--child` ist ein Bedienfehler.** Begründung: Der Aufgabentext ist die einzige
    Stoffquelle, ein zweites Thema würde ihn überschreiben oder verwässern; Namen gehören nicht in ein Prompt.
    Verworfen: Text und `--topic` verketten. Kosten: Wer beides will, schreibt das Thema selbst. Ausgeschlossener
    Fall: eine Aufgabe mit Wortliste vom Vater; dafür gilt weiter `create --words`.
- Entscheidung 13: **Die Reihe kommt aus dem Fach der Aufgabe, über das bestehende Profil-/Buchverfahren mit `subjectId` als
    Filter. Passt keine oder mehr als eine Reihe, ist das ein Bedienfehler mit dem Hinweis auf `--series`.**
    Begründung: Das ist das Verfahren aus B-473 bis B-519; ein zweiter Weg zur Reihe wäre die geratene Schublade,
    die dort gerade abgestellt wurde. Verworfen: die erste Reihe des Fachs nehmen. Kosten: Kinder mit zwei Büchern
    im Fach müssen `--series` angeben.

## Akzeptanzkriterien

Aus B-651 Nummer 7, ausgearbeitet; je Kriterium die Änderung, die es rot macht.

1. `homework --child <id>` listet die offenen Aufgaben (Entscheidung 10, 11) mit WebUntis-Id, Fach, Fälligkeit,
   `verpasst`-Marke und dem Text. Rot macht es: das Verb entfernen (`Unbekanntes Kommando`) oder die erledigten
   Aufgaben mitlisten.
2. `create --type <Typ> --child <id> --homework <id>` erzeugt die Übung in der Reihe, die zum zugeordneten Fach
   gehört (Entscheidung 13), und der Aufgabentext steht im Briefing, das das Modell bekommt. Rot macht es:
   `--homework` ignorieren (Thema leer) oder die Reihe aus dem ersten Buch des Kindes raten statt aus dem Fach.
3. Eine Aufgabe ohne zugeordnetes Fach bricht mit einer verständlichen Meldung ab; es gibt weder einen Modellaufruf
   noch einen angelegten Eintrag im Katalog. Rot macht es: die Prüfung weglassen (die Übung entstünde in einer
   geratenen Reihe).
4. Eine unbekannte Aufgaben-Id, ein `--homework` zusammen mit `--topic` oder `--words` und `--homework` ohne
   `--child` sind Bedienfehler (`AgentUsageException`) und legen nichts an (Entscheidung 12). Rot macht es: den
   Fehlerzweig weglassen (Exception anderer Art oder eine Übung ohne Thema).
5. Das Briefing enthält weder den Namen der Lehrkraft noch den des Kindes aus der Hausaufgabe. Rot macht es:
   `Lehrkraft` ins Thema schreiben.
6. Ein `school_login_failed` oder `school_unreachable` der API endet als lesbare Meldung ohne Stapelspur. Rot
   macht es: die Ausnahme in `homework` abfangen und verschlucken oder umpacken.

## Schätzung

**Größe: M.** Anker ist der Batch-Pfad im `MediaSelector` (B-03): ein neuer Pfad in einem bestehenden System. Ein
Verb, ein Schalter in `create` samt Reihenwahl über das Fach, eine Client-Methode, keine neue Struktur. Nicht S,
weil die Reihenwahl das heikelste Stück des Agenten berührt (B-473, B-481, B-506, B-510, B-519 sind alle
Korrekturen daran).

- **`migration: nein`.** Der Agent hat keine Datenhaltung, und diese Story berührt kein Schema.
- **`vertragsbruch: nein`.** Die DTOs entstehen in B-654 (Hausaufgabe samt `subjectId?`). Hier kommt nur eine
  Methode in `Pugling.Client` dazu, additiv. `Pugling.Contracts` ändert sich in dieser Story nicht.
- **`wo: backend`.** Die Agent-Projekte liegen unter `backend/`; kein Frontend. (Der Vater-Web-Teil ist
  [B-655](B-655-webuntis-hausaufgaben-vater-web.md).)

**Abhängigkeit:** B-656 braucht **B-654** (Endpunkt `supervisor/children/{childId}/school-homework`, DTO und den
Fake-Connector in der Testfabrik), und zwar mindestens bis zur Abnahme des Endpunkts. **B-655 ist keine
Voraussetzung**: beide hängen unabhängig an B-654, und der Agent ruft den Endpunkt wie das Vater-Web.

### Risiken

1. **B-654 liefert die Felder noch nicht verbindlich.** Die Schätzung nimmt die Liste aus B-654 AK 4 an (Id, Fach,
   `subjectId?`, Lehrkraft, Aufgabedatum, Fälligkeit, Text, erledigt, verpasst). Die **Bemerkung** (`remark`)
   fehlt dort, obwohl B-651 sie in der Lücke nennt; fehlt sie im DTO, nimmt Entscheidung 12 nur den Text.
2. **Die Reihenwahl ist fehleranfällig.** `BestProfileForChildAsync` filtert heute nicht nach Fach
   (`AgentCommands.cs:666`); der Filter über `MatchProfilesAsync(childId, subjectId)` ist neu verdrahtet, und
   dieselbe Stelle hat fünf Fixes hinter sich. Gegenmaßnahme: ein Test je Verzweigung (keine, eine, mehrere Reihen
   im Fach).
3. **Fremder Text im Prompt.** Der Aufgabentext kommt von der Schule und geht ins Briefing. Er ist Daten, keine
   Anweisung; die feste Regelfolge des Briefings (`Persona`/`Didactics` vor dem Regelblock) bleibt davor. Das Modell
   läuft lokal (Ollama), es verlässt also nichts das Haus.
4. **Seitenverweise geben keinen Stoff** („S. 80 Nr. 1+2“), wie B-651 Entscheidung 9 festhält. Das Ergebnis ist
   dann eine Übung zu einem dünnen Thema; die Auflösung über das Buch-Markdown ist eine Folge-Story.
5. **Live-Abruf in `create`.** Ist die Schule nicht erreichbar, scheitert `create --homework` (nicht `create`
   allgemein). Gewollt (Entscheidung 4 aus B-651).

### Angriffsplan

1. **Client:** `SupervisorApi.GetSchoolHomeworkAsync(childId, from, to, ct)` als einzeilige Methode über die DTOs
   aus B-654 (`backend/Pugling.Client/SupervisorApi.cs`).
2. **Verb `homework`:** neuer Zweig in `AgentCommands.RunAsync`; `--child` Pflicht, `--from`/`--to` mit
   Vorgabezeitraum; Ausgabe wie `profiles` als schmale Tabelle. Offen heißt nicht erledigt.
3. **`create --homework`:** in `BuildRequestAsync` die Aufgabe laden, Bedienfehler prüfen (AK 3, 4), Thema aus Text
   (und Bemerkung) setzen, die Reihe über das Fach der Aufgabe wählen (Profile mit `subjectId`, sonst
   `SeriesOfChildsBookAsync`).
4. **Hilfetext und Doku:** `PrintUsage` (`AgentCommands.cs:858`), `backend/Pugling.Agent.Creator/README.md`,
   `backend/Pugling.Agent.Creator/CLAUDE.md` und der Skill `ki-creator` kennen die neuen Optionen.
5. `dotnet build Pugling.sln` und `dotnet format Pugling.sln` vor dem Commit.

### Testweg

- Neue Klasse `HomeworkAgentTests` in `backend/Pugling.Api.Tests` (Nachbar `CreatorAgentTests`; dort liegt der
  Aufbau `BuildAgent` und `Commands` mit dem `FakeChatClient`), gegen den **Fake-Connector aus B-654** in der
  Testfabrik: Liste (AK 1), Übung in der Reihe des Faches mit dem Text im Briefing (AK 2, den Prompt liest man am
  `FakeChatClient`), Aufgabe ohne Fach legt nichts an (AK 3), Bedienfehler (AK 4), kein Lehrkraftname im Prompt
  (AK 5), Fehlercode der API als lesbare Meldung (AK 6).
- Kein E2E im Frontend: die Story berührt keine Oberfläche.
- `/smoke-test` gibt es für den Agenten nicht; stattdessen ein Probelauf `homework --child <id>` und
  `create --dry-run --homework <id>` gegen das Heimnetz, sobald B-654 mit dem echten Zugang läuft.
- Vor der Abnahme: Agent `pugling-reviewer`.

## Verlauf

- **2026-11-18** — angelegt durch Teilen von B-651 beim Schätzen (Vater: Variante A, drei Stories nach
  Schicht); Entscheidungen aus B-651 übernommen, damit auf `gegrillt`.
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet): M, backend, keine Migration, kein Vertragsbruch;
  hängt an B-654, nicht an B-655. Das eine Akzeptanzkriterium aus B-651 in sechs zerlegt, je mit roter Änderung.
  Entscheidungen 10 bis 13 (offen, Zeitraum, Thema, Reihe) stellvertretend, unbestätigt (`wartet_auf`).
