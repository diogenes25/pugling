---
tags: [typ/story, status/geschaetzt, bereich/auswertung, bereich/training, rolle/supervisor]
aliases: [Tagesauswertung, Leistungsprofil, Problemwörter, Verlauf, Etappe E1b]
status: geschaetzt
prio: P1
art: Wunsch
groesse: L
wo: beides
migration: ja
vertragsbruch: nein
quelle: B-644
grund: ""
ersetzt_durch: []
---

# B-733 · Tagesauswertung je Kind, Tag und Fach, Leistungsprofil und die Vater-Ansicht (B-644, Etappe E1b)

Vierte Teilstory aus der Teilung von [B-644](B-644-tagesplaner-statt-fester-lauf.md). Pugling schreibt je Kind, Tag und
Fach mit, was gelernt wurde und wo es hakte, und zeigt dem Vater den Verlauf und die Problemwörter. Aus den
gespeicherten Tagen entsteht das Leistungsprofil, das der Planer ([B-734](B-734-planer-kern-und-schattenmodus.md)) als
Eingang braucht. Diese Etappe wirkt auf die Kinder **nicht**, sie liest nur.

**Stand:** Eintrittsbedingung `gegrillt` aus B-644 geerbt. Entscheidung 13 stammt vom Vater, 21 ist stellvertretend und
unbestätigt.

## User Story

Als *Vater* möchte ich je Fach und Tag nachlesen, was mein Kind gelernt hat und wo es Probleme gab, mit dem Warum
(Fehlerart), damit ich nicht nur eine Wortliste sehe. Als *Planer* brauche ich das Profil (Beherrschung, tragfähiges
Tempo, typische Fehlerarten, Belastbarkeit) als gespeicherte, nachrechenbare Größe.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`. Pfade relativ zu `backend/Pugling.Api/`.

- **Rohdaten sind vollständig:** `ItemReviewEvent` mit Antwort, richtig/falsch, Zeit (`Models/ItemProgressEntities.cs:61`),
  `TestItemResult` und `TestAttempt` (`Models/StudyPlanEntities.cs:202`, `:163`), `PracticeSession.ActiveSeconds`
  (`:112`), `ItemProgress` mit Beherrschung in Prozent und Einführungstag (`ItemProgressEntities.cs:18`, `:41`, `:49`).
- **Fehlerart und Fach vor der Antwort fehlen heute**; sie kommen mit [B-732](B-732-fehlerart-wird-beim-antworten-gespeichert.md).
- **Eine gespeicherte Tagesauswertung und ein Profil gibt es nicht** (keine Entität, kein Dienst).
- **Lesepfade für die Vater-Seite:** `ChildrenDashboardService` (`Services/Supervisor/ChildrenDashboardService.cs:68`) rechnet
  Tageswerte pro Aufruf; die schwachen Wörter liefert `ChildVocabularyProgressController` (`WeakBelowPercent = 50`).
  Die Vater-Zeitachse liegt in `frontend/src/vater/ChildrenTimeline.tsx`.
- **SQLite-Falle:** Die Hierarchie-Sicht (`ChildLearnProgressTests`) musste Aggregationen auf der Client-Seite rechnen; das
  wiederholt sich hier (Summen und Gruppen über `DateOnly`).
- Navigation und Perspektiven: `frontend/src/vater/navigation.ts:57`, `:72`.

## Entscheidungen (geerbt)

1. **Die Auswertung speichert Mengen und Fehlerarten je Kind, Tag und Fach** (B-644 Entscheidung 13, Vater). Vater-Web:
   Verlauf über die Wochen (gekonnte Wörter, neue Wörter je Tag, erfüllte und verpasste Pflicht), Problemwörter mit
   Fehlerart, Tagesplan mit Begründung. Die Sohn-App sieht nur die eigene Kurve (in [B-738](B-738-eingriffe-bonus-und-sohn-begruendung.md)).
2. **Das Profil ist je Kind und Fach gleitend über die letzten 7 und 28 Tage** (B-644 Entwurf, Baustein 2).
3. **Der Planer ist regelbasiert, ein Sprachmodell nimmt nicht an der Auswertung teil** (Entscheidungen 10 und 21).

### Schätz-Annahmen (stellvertretend, unbestätigt)

1. **Das Profil wird berechnet, nicht gespeichert:** ein Dienst rechnet es bei Bedarf aus den gespeicherten Tagen der
   letzten 28 Tage. Begründung: Die gespeicherte Auswertung ist die stabile Basis (Entscheidung 13); eine zweite Tabelle
   müsste bei jeder Regeländerung neu befüllt werden. Kosten: Rechenzeit je Planerlauf (28 Zeilen je Fach, gering).
   Verworfen: Tabelle `PerformanceProfile`.
2. **Nur abgeschlossene Tage werden gespeichert;** der laufende Tag wird beim Lesen live gerechnet und nicht abgelegt.
   Ein Tag gilt als abgeschlossen, sobald der Ortstag ([B-730](B-730-tagesbegriff-in-deutscher-zeit.md)) vorbei ist; die
   Berechnung läuft träge beim ersten Zugriff und in der Planer-Zeitschaltung ([B-734](B-734-planer-kern-und-schattenmodus.md))
   idempotent über alle noch fehlenden Tage der letzten 28.
3. **Zählregeln je Tag und Fach:** *neu* = Einführungstag des `ItemProgress` ist der Tag; *richtig*/*falsch* =
   gezählte Antworten; *gefestigt* = richtig und `BoxBefore + 1` erreicht das oberste Fach (`ItemProgress.MaxBox`);
   *zurückgefallen* = falsch bei `BoxBefore` über 1; *Fehlerarten* = Zähler je Art (JSON-Spalte); geübte Zeit = Summe
   `ActiveSeconds`; Pflicht erfüllt/verpasst = Ergebnis der Tageszustände des Dienstes `PositionProgressService`. Kosten:
   eine Näherung, bis der Planer eigene Tagesaufgaben kennt ([B-736](B-736-pflicht-aus-der-tagesaufgabe.md)).
4. **Pause- und Sonntagstage tragen keine Zeile** (Lerntag aus [B-731](B-731-lerntage-pause-und-leitplanken-je-kind.md));
   die Kurve zeigt eine Lücke, keinen Nullwert. Wird an einem freien Tag trotzdem geübt, entsteht eine Zeile.
5. **Der Reset ([B-737](B-737-reset-zum-stichtag.md)) löscht die Auswertungen mit,** weil sie aus gelöschten Antworten
   entstanden sind und das Profil ohne Vorgeschichte starten soll (Entscheidung 12).

## Akzeptanzkriterien

1. Für jeden abgeschlossenen Lerntag, an dem das Kind geübt hat, entsteht je Fach genau eine Zeile (eindeutiger Index
   auf Kind, Tag, Fach); zweimaliges Berechnen legt keine zweite an und ändert die erste nicht. *Rot, wenn* der Index
   fehlt oder das Berechnen nicht idempotent ist.
2. Die Zeile trägt neue, gefestigte und zurückgefallene Wörter, richtige und falsche Antworten, geübte Sekunden, die Zähler je
   Fehlerart und erfüllte und verpasste Pflicht. *Rot, wenn* eine Spalte aus dem Verlauf eines Tages anders hervorgeht als
   die Zählregel (Test mit bekanntem Antwortverlauf).
3. Die gespeicherten Tage bleiben stabil, wenn sich die Antworten später ändern oder die Regeln wechseln. *Rot, wenn* ein
   Lesepfad einen abgeschlossenen Tag neu aus den Ereignissen rechnet.
4. Das Profil je Fach liefert Beherrschung, tragfähiges Tempo (im Mittel gefestigte neue Wörter je Lerntag), typische
   Fehlerarten und Belastbarkeit (Anteil erfüllter Pflichttage) über 7 und 28 Tage, gerechnet aus den gespeicherten Tagen.
   *Rot, wenn* es aus den Rohereignissen statt aus den Tageszeilen gerechnet wird oder ein Fenster fehlt.
5. `GET supervisor/children/{childId}/evaluations` (mit Zeitraum, seitenweise) und `…/profile` liefern Verlauf und Profil;
   ein Konto nur mit Creator-Profil bekommt `403`, ein fremder Vater `404`. *Rot, wenn* ein Gate fehlt.
6. Kein Student-Endpunkt gibt einen Auswertungs- oder Profil-Typ zurück; ein Student-Token bekommt auf den Pfaden `403`.
   *Rot, wenn* ein Student-Endpunkt Fehlerarten oder Zahlen des Bruders liefert (Reflexionsprüfung nach dem Muster
   `Actions_Geben_Nur_Vertragstypen_Zurueck`).
7. Das Vater-Web zeigt Verlauf (neue und gefestigte Wörter, erfüllte und verpasste Pflicht je Tag und Fach) und die
   Problemwörter mit Fehlerart; ein Tag ohne Zeile ist eine Lücke. *Rot, wenn* ein Pausentag als Null gezeichnet wird oder
   ein Problemwort ohne seine Art erscheint, obwohl sie gespeichert ist.
8. Die Migration legt die Tabelle leer an; bestehende Daten bleiben. *Rot, wenn* die Migration fehlt oder die
   JSON-Spalte keinen `ValueComparer` trägt (Tor G7 in `SchemaGuardTests`).

## Schätzung

**Größe: L** — Anker: eine DB-Umbau-Etappe wie E6: neue Tabelle mit Migration, ein Rechendienst über vier Quellen,
vier Lese-Endpunkte, eine neue Vater-Ansicht. Am oberen Rand; sprengt sie beim Bau, ist das Frontend als eigene Story
(neue Id, `quelle: B-733`) abzuspalten.

- **`migration: ja`** — Tabelle `DailyEvaluation` (Kind, Tag, Fach, Zähler, Fehlerarten als JSON, Anlagezeit), eindeutiger Index
  (Kind, Tag, Fach). An die Kette hängen.
- **`vertragsbruch: nein`** — nur neue DTOs und Endpunkte; kein bestehender Vertragstyp ändert sich.
- **`wo: beides`** — Backend zuerst, dann Vater-Web (`ChildrenTimeline.tsx`-Nachbarschaft, neue Maske, Eintrag in
  `navigation.ts`).

### Risiken

- **„Gefestigt" und „zurückgefallen" hängen an `BoxBefore`** aus B-732; ohne die Spalte ist die Zählung nicht zu bauen.
- **Fehlende Tage nach Serverausfall:** Die träge Nachberechnung muss die letzten 28 Tage idempotent füllen können;
  ein Ausfall von mehr als 28 Tagen bleibt eine Lücke.
- **SQLite-Aggregation:** Gruppieren über `DateOnly` im Speicher rechnen, nicht in der Abfrage (Erfahrung aus der
  Lernstand-Hierarchie).
- **Endpunkt-Abdeckung und Paging:** `EndpointCoverageGuard` und `Unpaginierte_Array_GETs_Sind_Gepinnt` verlangen Aufruf und
  Seitenbildung je neuem Endpunkt.
- **Fremde Sohn-Dateien:** Der Arbeitsbaum trägt ungecommittete Frontend-Arbeit (LearningDad, `frontend/src/sohn/*`);
  diese Story fasst `sohn/` nicht an.

### Angriffsplan

1. Migration und Modell `DailyEvaluation`, Migrationstest.
2. Rechendienst (eine Methode für einen Tag und ein Kind, idempotent), Zählregeln als eigene, getestete Funktionen.
3. Profil-Dienst über die gespeicherten Tage.
4. Endpunkte mit Gates, Paging und Reflexionsprüfung für Student.
5. Vater-Web: Verlauf und Problemwörter, Lesen vom Server.

### Testweg

- Neue Klasse `TagesauswertungTests` (Integration mit `TestClock`, Antwortverlauf über die Student-API, danach Direktlesen
  der Zeile): Zählregeln, Idempotenz, Stabilität nach Änderung der Antworten. Rote Probe je AK: Index entfernen lässt AK 1
  fallen; den Lesepfad auf Rohereignisse umstellen lässt AK 3 fallen.
- `ProfilTests` (Fenster 7 und 28, Pausen- und Sonntagslücken).
- `ConventionGuardTests`: Student-Aktionen geben keinen Auswertungs-Typ zurück (Rot-Probe: einen solchen Rückgabetyp an einen
  Student-Endpunkt hängen). `OwnershipMatrixTests` deckt die neuen Pfade über den Ownership-Filter.
- Migration: `TagesauswertungMigrationTests` nach `ActivatedAtMigrationTests`.
- Frontend: Vitest für die Verlaufsmaske; E2E neu `frontend/e2e/auswertung-verlauf.spec.ts` (Kind übt, nächster Tag, Vater
  sieht Verlauf und Problemwort mit Art), Regression `frontend/e2e/vater-zeitachse.spec.ts`.
- Vor der Abnahme: `pugling-reviewer`, `frontend-reviewer`, Rollengang als Vater.

## Reihenfolge und Abhängigkeiten

Nach [B-730](B-730-tagesbegriff-in-deutscher-zeit.md), [B-731](B-731-lerntage-pause-und-leitplanken-je-kind.md) und
[B-732](B-732-fehlerart-wird-beim-antworten-gespeichert.md). Vor [B-734](B-734-planer-kern-und-schattenmodus.md).

## Verlauf

- **2026-11-20** — angelegt durch Teilung von B-644 (Etappe E1b) und `gegrillt` → `geschaetzt` im Vorbereitungslauf (Sonnet):
  Eintrittsbedingung `gegrillt` aus B-644 geerbt. Nachgesehen: Rohdaten vollständig, aber das Fach vor der Antwort fehlt (nach
  B-732 verlegt). Profil berechnet statt gespeichert (Schätz-Annahme). L, beides, Migration, kein Vertragsbruch.
