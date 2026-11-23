---
tags: [typ/story, status/in-arbeit, bereich/gamification, bereich/klassenarbeit, rolle/student]
aliases: [Notenschlüssel, PercentToGrade]
status: in-arbeit
prio: P2
art: Wunsch
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-682
grund: ""
ersetzt_durch: []
---

# B-740 · Ein Notenschlüssel für die ganze App: `PercentToGrade`

Die eine Funktion, die einen Anteil (0 bis 100 Prozent) in eine Note umrechnet. Notenstufe ([B-741](B-741-notenstufe-berechnen-und-speichern.md))
und Probeklausur ([B-249](B-249-probeklausur-mit-notenprognose.md)) rufen sie auf; wer zuerst baut, legt sie an. Abgespalten aus
[B-682](B-682-notenstufe-je-fach-statt-liga.md) (Entscheidung 2), damit die gemeinsame Stelle ein eigenes, kleines Stück ist.

*Entscheidungen stellvertretend im Vorbereitungslauf 2026-11-20 (Sonnet) gefällt, unbestätigt; Herkunft B-682.*

## User Story

Als *Vater* möchte ich, dass dieselbe Leistung überall dieselbe Note ergibt, damit die Notenstufe des Kindes und die Prognose
einer Probeklausur nicht zwei Wahrheiten erzählen.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`; Pfade relativ zu `backend/Pugling.Api/`.

- Ein Notenschlüssel, `PercentToGrade` oder `Notenschlüssel`: nicht gefunden (Suche unter `backend/` und `frontend/src`, nur
  Treffer in Backlog-Texten).
- Vorbild für eine Standardtabelle in `appsettings.json`: `Scoring:TimeSlots` (`appsettings.json:14-21`), gebunden als
  Konfiguration, getestet in `ScoringTimeSlotTests`.
- Wertebereich der echten Note 1,0 bis 6,0, geprüft in `Controllers/Supervisor/KlassenarbeitenController.cs:46-47`.

## Entscheidungen

1. **Eine Funktion `PercentToGrade`, Standardtabelle in `appsettings.json` (Abschnitt wie `Scoring:TimeSlots`), keine
   Überschreibung je Fach oder Schulart im ersten Bau** (B-682 Entscheidung 2). Begründung: Zwei Schlüssel würden dieselbe Leistung
   verschieden benoten. Kosten: Die je Klassenarbeit einstellbare Variante aus B-249 (G-7) kommt erst mit B-249. Verworfen: ein
   eigener Schlüssel für die Stufe. Schließt aus: eine Stufe, die der Vater je Fach feiner einstellt.

## Akzeptanzkriterien

Je Kriterium die Änderung, die es rot macht.

1. An jeder Grenze der Tabelle liegt der Grenzwert in der besseren Note, ein Zehntelprozent darunter in der schlechteren; 0 Prozent
   ergibt die schlechteste, 100 Prozent die beste Note. *Rot, wenn* eine Vergleichsoperation von `>=` auf `>` kippt.
2. Die Tabelle steht in `appsettings.json` und nicht im Code. *Rot, wenn* ein Test mit einer anderen Tabelle (Optionen im Test
   gesetzt) dieselbe Note bekommt wie mit der Standardtabelle.
3. Werte außerhalb von 0 bis 100 werden abgelehnt oder auf den Rand gezogen, nicht in eine unsinnige Note gerechnet. *Rot, wenn*
   -5 Prozent oder 130 Prozent eine Note außerhalb 1,0 bis 6,0 liefert.
4. Es gibt genau eine Stelle für diese Umrechnung. *Rot, wenn* eine zweite Umrechnung entsteht (Aufnahme in
   `ConventionGuardTests` nur, wenn die Messung einen belastbaren Wächter hergibt; sonst Absichtserklärung).

## Schätzung

**Größe: S**, wie „`childId` aus dem Test-Pfad ziehen" (B-01): eine reine Funktion, eine Konfigurationsklasse, eine Tabelle.

- **`wo: backend`**: kein Endpunkt, keine Oberfläche.
- **`migration: nein`**: keine Spalte, keine Tabelle.
- **`vertragsbruch: nein`**: nichts in `Pugling.Contracts`; die Funktion bleibt intern.

### Risiken

- **Die Zahlen der Tabelle sind eine Produktangabe.** G-7 sagt nur „ein üblicher Prozent-Schlüssel". Vorschlag als Standard:
  Grenzen bei 92, 81, 67, 50 und 30 Prozent für die Noten 1 bis 5, darunter 6. Das ist ein Wert zum Bestätigen, kein Befund aus
  dem Code.
- **Rückgabetyp:** B-249 will eine Note (1,0 bis 6,0), die Stufe ([B-741](B-741-notenstufe-berechnen-und-speichern.md)) eine
  ganze Zahl 1 bis 6. Vorschlag: `PercentToGrade` liefert die ganze Note als `int`; Zehntelnoten bleiben Sache von B-249, falls
  es sie braucht. Entscheidet, wer zuerst baut.

### Angriffsplan

1. Optionsklasse mit der Grenztabelle, Bindung in `Program.cs` nach dem Muster der `Scoring`-Optionen, Standardwerte in
   `appsettings.json`.
2. Die Funktion als statische, reine Methode mit der Tabelle als Parameter.
3. Test, danach `dotnet build Pugling.sln` und `dotnet format Pugling.sln`.

### Testweg

- Neue Testklasse `PercentToGradeTests` (reine Unit-Tests, wie `ScoringTimeSlotTests` und `AnswerGraderTests` aufgebaut): eine
  Theorie über die Grenzen (AK 1), Randwerte (AK 3), eine abweichende Tabelle (AK 2).
- Vor der Abnahme: Agent `pugling-reviewer`; kein Rollengang nötig, da nichts sichtbar wird (Verlauf-Zeile mit dieser
  Begründung).

## Verlauf

- **2026-11-20** — angelegt aus [B-682](B-682-notenstufe-je-fach-statt-liga.md) (geteilt); `geschaetzt`: S, backend, keine
  Migration, kein Vertragsbruch (Vorbereitungslauf, Sonnet).
- **2026-11-23** — `in-arbeit` (Nachtlauf G, Sprint 127). Gebaut: `Services/Shared/GradeKey.cs` (`PercentToGrade`,
  `ShareToGrade`, Umkehr `MinPartForGrade` für B-742), `Services/Shared/GradingOptions.cs`, Tabelle
  `Grading:Thresholds` = 92/81/67/50/30 in `appsettings.json` (Vorschlag aus den Risiken übernommen; ein Wert zum
  Bestätigen), Bindung in `Program.cs`. Rückgabe ganze Note `int` (Vorschlag aus den Risiken; Zehntelnoten bleiben bei
  B-249). Vergleich als `decimal`. Keine Vorgabe im Code (AK 2): eine Listen-Eigenschaft mit Vorgabewerten würde der
  Binder ohnehin anhängen statt ersetzen. Eine kaputte Tabelle (nicht genau fünf, nicht streng fallend, außerhalb 0–100)
  wirft laut. Eigener Fund beim Prüfen, ohne Zählung (nur Kommentar): die erste Fassung behauptete, `double` lege 23/25
  unter 92 – nachgerechnet für alle Zählungen bis 400 gegen die fünf Grenzen: kein Fall; Kommentar in `GradeKey` und Test
  berichtigt.
- **2026-11-23** — AK → Test (`backend/Pugling.Api.Tests/PercentToGradeTests.cs`), Mutante → rot (Tabelle aus
  `.claude/scripts/mutant-probe.py`, gemessen an `a782b4b2` + Arbeitsbaum, 2026-11-23 23:49, Filter
  `PercentToGradeTests|ChildSubjectLevelTests`):
  AK 1 Grenze in der besseren Note → `Grenze_liegt_in_der_besseren_Note`, M1 (`>=` → `>`, `GradeKey.cs:30`) → rot ·
  Umkehr stimmt an jeder Grenze → `Umkehr_stimmt_an_jeder_Grenze_mit_der_Note_ueberein`, M2 (`Ceiling` → `Floor`, `:56`)
  → rot · kaputte Tabelle → `Kaputte_Tabelle_wird_abgewiesen`, M3 (`>=` → `>` in der Prüfung, `:69`) → rot ·
  AK 2 → `Andere_Tabelle_ergibt_andere_Note` und `Tabelle_kommt_aus_der_Konfiguration` (liest die gebundenen Optionen des
  Hosts; rot, wenn die Tabelle aus `appsettings.json` fehlt oder abweicht – nicht als Mutante gemessen) ·
  AK 3 → `Werte_ausserhalb_werden_auf_den_Rand_gezogen`: **strukturell**, nicht durch das `Math.Clamp` getragen – die
  Schleife liefert für jeden Wert ohnehin 1 bis 6; ein Mutant ohne Clamp wäre äquivalent, darum nicht gemessen ·
  AK 4 → `Es_gibt_genau_eine_Umrechnung` (Reflexion: keine Methode `…ToGrade` außerhalb `GradeKey`; rot, sobald eine
  entsteht; Absichtserklärung gegen andere Namen).
  Kein Rollengang nötig, nichts wird sichtbar (Testweg der Story); der Gang läuft mit B-743.
