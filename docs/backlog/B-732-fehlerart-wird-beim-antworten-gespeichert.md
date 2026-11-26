---
tags: [typ/story, status/abgenommen, bereich/auswertung, bereich/training, rolle/student]
aliases: [Fehlerart, Akzentfehler, Tippfehler, Etappe E1a]
status: abgenommen
prio: P1
art: Wunsch
groesse: M
wo: backend
migration: ja
vertragsbruch: nein
quelle: B-644
grund: ""
ersetzt_durch: []
nachgeschaut: ""
---

# B-732 · Die Fehlerart einer getippten Antwort wird beim Antworten bestimmt und gespeichert (B-644, Etappe E1a)

Dritte Teilstory aus der Teilung von [B-644](B-644-tagesplaner-statt-fester-lauf.md). Die Auswertung braucht das
„Warum" hinter einer falschen Antwort: Akzent, Tippfehler, falsches Wort, Ausweichen auf ein Synonym. Heute steht nur
„richtig/falsch" in der Datenbank; ob die Antwort ein Tippfehler war, rechnet die Anzeige und vergisst es wieder.

**Stand:** Eintrittsbedingung `gegrillt` aus B-644 geerbt. Entscheidung 13 stammt vom Vater, 18 ist stellvertretend und
unbestätigt.

## User Story

Als *Vater* möchte ich bei jedem falschen Wort sehen, ob mein Kind das Wort nicht kennt oder es nur falsch schreibt,
damit ich die Lernhilfe passend wähle. Als *Planer* (B-734) brauche ich die Fehlerart als gespeicherte Eingangsgröße,
die sich nicht ändert, wenn jemand später die Regeln anpasst.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`. Pfade relativ zu `backend/Pugling.Api/`.

- **Bewertung ist binär:** `AnswerGrader.Matches` (`Services/Shared/AnswerGrader.cs:26`), Vergleich über
  `StageMechanics.Normalize`.
- **„Falsch geschrieben" gibt es schon, nur flüchtig:** `AnswerGrader.IsMisspelled` (`:47`, B-524) misst Buchstabenabstand
  (Wörter bis fünf Buchstaben 1, darüber 2) und lässt ein Nachbarwort derselben Übung nicht als Tippfehler gelten. Es ist
  „Display only" und wird nicht gespeichert; Aufrufer: `PositionPracticeController.cs:648` und `:927`,
  `PositionTestsController.cs:447`, `ExercisePreviewService.cs:96`.
- **Speicherstellen ohne Fehlerart:** `ItemReviewEvent` (`Models/ItemProgressEntities.cs:61`) mit gegebener Antwort und
  richtig/falsch, geschrieben von `ItemProgressService.RecordAsync` (`Services/Shared/ItemProgressService.cs:37`; Aufrufer
  `PositionPracticeController.cs:731`, `:900`, `:907`); `TestItemResult` (`Models/StudyPlanEntities.cs:202`), angelegt in
  `PositionTestsController.cs:94` (freie Nennung) und `:190` (Fragenfolge).
- **Akzent:** Die Regel „Akzentfehler zählt im Üben halb" (B-624) ist **nicht gebaut**. Die Suche nach Akzent,
  Diakritik und Halbwertung in `Services/`, `Controllers/` und `Exercises/` findet nur den Apostroph-Kommentar in
  `StageMechanics.cs:55`. Die Entscheidung 18 verlangt dieselbe Vergleichsfunktion als Quelle; sie muss hier entstehen.
- **Getippt oder gewählt:** `StageMechanics.IsTyped(TestStage)` (`:17`) und `IsTyped(ClozeStage)` (`:27`), im Controller
  über `type.IsTypedStage`.
- **Kein Feld für die Stufe vor der Antwort:** `ItemReviewEvent` trägt die Stufe der Frage (`StageValue`), aber nicht das
  Leitner-Fach vor der Antwort; `RecordAsync` kennt es (`prog.Box`), bevor es fortgeschrieben wird.

## Entscheidungen (geerbt)

1. **Die Fehlerart wird beim Antworten bestimmt und mit der Antwort gespeichert, nur für getippte Antworten;
   Auswahl-Antworten tragen keine** (B-644 Entscheidung 18, stellvertretend, unbestätigt). Verworfen: nachträglich aus der
   gespeicherten Antwort bestimmen (instabil gegen Regeländerungen). Schließt aus: eine Fehlerart für Auswahl-Antworten.
2. **Vier Arten: Akzent, Tippfehler, falsches Wort, Ausweichen auf ein Synonym** (B-644 Entscheidung 13, Vater).
3. **Altdaten entfallen durch den Reset** (Entscheidung 12): bestehende Zeilen bleiben leer, es gibt kein Nachfüllen.

### Schätz-Annahmen (stellvertretend, unbestätigt)

1. **Zusätzlich speichert `ItemReviewEvent` das Leitner-Fach vor der Antwort (`BoxBefore`).** Begründung: Die
   Tagesauswertung ([B-733](B-733-tagesauswertung-und-leistungsprofil.md)) muss „gefestigt" und „zurückgefallen" je Tag
   zählen; aus dem Verlauf allein ist das Fach zu Tagesbeginn nicht rekonstruierbar. Dieselbe Migration, derselbe
   Schreibweg. Kosten: eine Spalte mehr. Verworfen: „gefestigt" nur als Momentaufnahme beim Berechnen (falsch, wenn
   nachträglich berechnet wird).
2. **Synonym heißt: die Antwort ist die richtige Lösung eines *anderen* Eintrags im Vokabelspeicher, der dieselbe
   Übersetzung trägt** (`Vocabulary.TranslationAlternatives`, `Models/VocabEntities.cs:39`, und gleiche Gegenseite). Ob
   der Speicher das hergibt, ist beim Bau nachzusehen; fällt es aus, bleibt die Art „falsches Wort" und die vierte Art
   wird eine eigene Folgestory (Theodors 11/20-Fehler „Ausweichen auf bekannte Synonyme" ist der Anlass).

## Akzeptanzkriterien

1. Lösung „café", Antwort „cafe" (nur ein Akzent fehlt) speichert die Fehlerart *Akzent* an `ItemReviewEvent` und an
   `TestItemResult`. *Rot, wenn* die Art fehlt, *Tippfehler* heißt oder erst beim Lesen entsteht.
2. Antwort mit Buchstabenabstand nach der Regel von `IsMisspelled` speichert *Tippfehler*; dieselbe Regel, kein zweiter
   Abstandsbegriff. *Rot, wenn* die Art von `IsMisspelled` abweicht (Test: beide Funktionen am selben Datensatz).
3. Eine andere Vokabel oder ein Nachbarwort der Übung speichert *falsches Wort*. *Rot, wenn* ein Nachbarwort als
   *Tippfehler* gespeichert wird (der Schutz aus `IsMisspelled`).
4. Die Antwort „Lösung eines anderen Eintrags mit gleicher Übersetzung" speichert *Synonym* (vorbehaltlich Annahme 2).
   *Rot, wenn* sie als *falsches Wort* durchgeht.
5. Eine Auswahl-Antwort und jede richtige Antwort tragen **keine** Fehlerart. *Rot, wenn* das Feld dort gefüllt ist.
6. Die Fehlerart steht mit der Antwort fest: Ändert jemand danach die Lösung der Übung, bleibt der gespeicherte Wert.
   *Rot, wenn* ein Lesepfad die Art neu berechnet.
7. Die Bewertung bleibt binär: `WasCorrect`, Punkte und Leitner-Schritt ändern sich nicht (`UebenFalschGeschriebenTests`,
   `AnswerGraderTests` bleiben unverändert grün). *Rot, wenn* eine Antwort wegen der Fehlerart als richtig oder halb zählt
   (B-624 ist ausdrücklich **nicht** Teil dieser Story).
8. `ItemReviewEvent` trägt `BoxBefore` für jede Antwort, die auf die Beherrschung zählt. *Rot, wenn* der Wert leer ist,
   obwohl ein `ItemProgress` fortgeschrieben wurde.

## Schätzung

**Größe: M** — Anker: wie der Batch-Pfad im `MediaSelector`: eine geteilte Stelle (`AnswerGrader`/`ItemProgressService`) mit
mehreren Aufrufern, eine Migration mit drei kleinen Spalten, kein Frontend.

- **`migration: ja`** — `ItemReviewEvent.ErrorKind` und `BoxBefore`, `TestItemResult.ErrorKind`, alle nullable;
  Bestandszeilen bleiben leer (Entscheidung 3). An die Kette hängen.
- **`vertragsbruch: nein`** — die Fehlerart ist intern; sie erscheint erst mit [B-733](B-733-tagesauswertung-und-leistungsprofil.md)
  in neuen DTOs. Kein bestehender Vertragstyp ändert sich.
- **`wo: backend`** — keine Oberfläche.

### Risiken

- **Synonym** ist die unsicherste Art (Annahme 2); sie darf als Folgestory fallen, ohne die drei anderen zu blockieren.
- **Tippfehler-Schwelle:** Akzent muss *vor* Tippfehler geprüft werden (ein fehlender Akzent ist Abstand 1); die Reihenfolge der
  vier Prüfungen ist die Regel und gehört in einen Test.
- **Drei Schreibwege** (`RecordAsync`-Aufrufer, `:94`, `:190`): vergisst einer die Art, bleibt die Spalte dort still leer.
  Darum je Weg ein Fall im Testweg.
- **Ein Tor über ein Artefakt beweist nur, dass es sich nicht ändert:** die Prüfung des Schreibwegs gehört in
  Integrationstests, nicht in einen Schema-Scan.

### Angriffsplan

1. `StageMechanics`: Akzent-faltender Vergleich als eine Funktion.
2. `AnswerGrader.Classify(given, item, siblings)` mit fester Prüfreihenfolge (Akzent, Tippfehler, Synonym, falsches Wort);
   `IsMisspelled` bleibt, wird aber von `Classify` mitbenutzt.
3. Migration und Modell; `RecordAsync` und die beiden Test-Schreibwege geben die Art mit.
4. Integrationstests je Schreibweg.

### Testweg

- `AnswerGraderTests`: Theorie über Lösung/Antwort/Erwartung (Akzent, Tippfehler, Nachbarwort, Synonym, leer, richtig,
  Auswahl). Rote Probe je Fall: Reihenfolge vertauschen lässt Fall „cafe" fallen.
- Neue Klasse `FehlerartTests` (Integration): Üben, Test (Fragenfolge), freie Nennung; Lösung nachträglich ändern (AK 6);
  Direktlesen der Zeile über den `DbContext`.
- Neue Migrationsklasse `FehlerartMigrationTests` nach `ActivatedAtMigrationTests`.
- Regression: `UebenFalschGeschriebenTests`, `PositionPracticeFlowTests`, `PositionTestFlowTests`, `ApostrophTests`.
- Vor der Abnahme: `pugling-reviewer`; ein Rollengang entfällt, solange die Art keine Oberfläche hat (im Verlauf benennen).

## Reihenfolge und Abhängigkeiten

Unabhängig von [B-730](B-730-tagesbegriff-in-deutscher-zeit.md) und [B-731](B-731-lerntage-pause-und-leitplanken-je-kind.md),
kann parallel laufen. Vor [B-733](B-733-tagesauswertung-und-leistungsprofil.md).

## Verlauf

- **2026-11-20** — angelegt durch Teilung von B-644 (Etappe E1a) und `gegrillt` → `geschaetzt` im Vorbereitungslauf (Sonnet):
  Eintrittsbedingung `gegrillt` aus B-644 geerbt. Nachgesehen: B-624 ist nicht gebaut (keine Akzentregel), `IsMisspelled` ist nur
  Anzeige; `BoxBefore` als nötiger Zusatz für B-733 erkannt. M, backend, Migration, kein Vertragsbruch.
- **2026-11-25** — `geschaetzt` → `in-arbeit` (Nachtlauf O, Sprint 191). Nachprüfung gegen den Code: der Ist-Stand „B-624 nicht
  gebaut“ ist überholt – `AnswerGrader.IsAccentSlip` und `ItemReviewEvent.AccentSlip` gibt es seit `AccentLeniency`. Die Art
  *Akzent* nimmt diese Regel als Quelle; das Flag bleibt die Wertung („halb gezählt“), die Art ist die Diagnose. Statt drei
  sind es **sechs** Schreibwege (Üben, Lernkarte, Mischrunde, Test schrittweise, Test gesammelt, Fehlnennung einer Liste);
  der Turm ist eine Auswahl und trägt keine Art.
- **2026-11-25** — gebaut: `AnswerGrader.SpellingSlip` (Akzent vor Tippfehler, aus `IsAccentSlip`/`IsMisspelled`),
  `AnswerErrorClassifier` als Senke (richtig → keine Art; Auswahl = die Karte bot Optionen, dieselbe Prüfung wie die
  Ausspielung `IExerciseType.Choices`; dann Rechtschreibung, Synonym aus dem Vokabelspeicher, sonst falsches Wort).
  `ItemProgressService.RecordAsync` nimmt die Art als Pflichtparameter und setzt `BoxBefore` selbst, wo der Kasten wandert.
  Der Test klassifiziert beim Antworten (`Answer`/`Submit`/`CreditSetAnswerAsync`) und reicht die Art der Testzeile bei der
  Abgabe an den Verlauf weiter – die Abgabe rechnet nicht neu (AK 6). Migration `20261125213822_AnswerErrorKind` (drei
  nullable Spalten, Bestand bleibt leer).
- **2026-11-25** — Ausführungsentscheidungen, die die `## Entscheidungen` nicht ausdrücklich nennen (aus der User Story
  abgeleitet, Kosten benannt): (a) eine **leere** getippte Antwort und ein „aufgegeben“ auf der Lernkarte sind *falsches Wort*
  („kennt das Wort nicht“) – Kosten: die Art unterscheidet „nichts getippt“ nicht von „anderes Wort“, der Verlauf (leere
  Antwort) aber schon; eine nie erreichte Testfrage bleibt ohne Art. (b) *Synonym* nur auf Karten, deren Antwort das
  Lernwort ist (Annahme 2 trägt) – der Speicher kennt keine deutschen Synonyme, und zwei Einträge mit gleichem Wort sind
  Homonyme. (c) Eine Fehlnennung in einer Liste wird gegen die noch offenen Einträge gemessen, die genaueste Art gewinnt.
- **2026-11-25** — Verifikation je AK-Fall (Tests in `backend/Pugling.Api.Tests/FehlerartTests.cs`):
  - AK 1 Üben → `Ueben_Speichert_Akzent_Tippfehler_Und_Falsches_Wort`, M7 → rot; AK 1 Test schrittweise/gesammelt →
    `Test_Speichert_Die_Art_An_Testzeile_Und_Verlauf` (Theorie), M14/M15/M11 → rot.
  - AK 2 eine Abstandsregel → `Tippfehler_Ist_Die_Regel_Von_IsMisspelled`, M2 → rot; Reihenfolge Akzent vor Tippfehler →
    `Rechtschreibart_Akzent_Vor_Tippfehler`, M1 → rot.
  - AK 3 Nachbarwort → „racist“-Zeile im Üben-Fall (WrongWord, nicht Typo), M2 → rot.
  - AK 4 Synonym → `Ueben_Erkennt_Das_Synonym_Aus_Dem_Speicher`, M4/M5 → rot; hinter dem Kandidaten-Deckel →
    `Synonym_Bleibt_Auch_Hinter_Vielen_Aehnlichen_Woertern` (Review-Fund, nach der Probe ergänzt, M16 ungemessen).
  - AK 5 richtig/Auswahl/Selbsteinschätzung → `Richtig_Auswahl_Und_Selbsteinschaetzung_Tragen_Keine_Art`, M3 → rot; M13
    (richtig ohne Art) fiel **am falschen Ort** (`abweichend`): die Verlaufszeile schützt zusätzlich `RecordAsync`
    (`ErrorKind = wasCorrect ? null : …`), rot wird nur die Testzeile in `Test_Speichert_…` – kein Beleg für den Üben-Fall.
  - AK 6 Art steht fest → `Art_Bleibt_Wenn_Die_Loesung_Sich_Danach_Aendert`, M11 → rot.
  - AK 7 Wertung binär → unverändert grün: `AkzentMildePfadTests`, `AnswerGraderTests`, ganze Suite (Absichtserklärung –
    keine Zeile der Wertung geändert, `git diff` an `WasCorrect`-Zuweisungen leer).
  - AK 8 `BoxBefore` → Üben-Fall (4 und 1) und Test-Fall (1), M6 → rot; Lernkarte ohne Box → `Lernkarte_Speichert_Die_Art_Des_Abrufs`.
  - Weitere Schreibwege: Lernkarte M8, Fehlnennung M9/M10, Mischrunde M12 (`FehlerartMischrundeTests`) → rot.
- **2026-11-25** — Mutanten (`.claude/scripts/mutant-probe.py`, Debug), gemessen an `6fa43808 + Arbeitsbaum`, 23:18 – vor dem
  Review-Fix (Kandidaten-Deckel sortiert), danach keine erneute Probe (Lauf beendet):

  | Mutante | Stelle | erwartet | gemessen |
  | --- | --- | --- | --- |
  | M1 Reihenfolge | `AnswerGrader.cs:99` | red | red |
  | M2 eigener Abstand | `AnswerGrader.cs:100` | red | red |
  | M3 Auswahl | `AnswerErrorClassifier.cs:37` | red | red |
  | M4 Synonym aus | `AnswerErrorClassifier.cs:39` | red | red |
  | M5 Synonym ohne Bedeutung | `AnswerErrorClassifier.cs:84` | red | red |
  | M6 BoxBefore | `ItemProgressService.cs:75` | red | red |
  | M7 Üben | `PositionPracticeController.cs:765` | red | red |
  | M8 Lernkarte | `PositionPracticeController.cs:956` | red | red |
  | M9 Fehlnennung | `PositionTestsController.cs:105` | red | red |
  | M10 Fehlnennung ohne Eintrag | `PositionTestsController.cs:103` | red | red |
  | M11 Abgabe | `PositionTestsController.cs:482` | red | red |
  | M12 Mischrunde | `MixedReviewService.cs:255` | red | red |
  | M13 richtig ohne Art | `AnswerErrorClassifier.cs:37` | red | abweichend |
  | M14 schrittweise | `PositionTestsController.cs:314` | red | red |
  | M15 gesammelt | `PositionTestsController.cs:445` | red | red |

- **2026-11-25** — `pugling-reviewer`: kein Blocker, ein 🟡 (Kandidaten-Deckel ohne Reihenfolge → behoben, Test ergänzt),
  🟢 Doku (Art-null-Fälle, Fehlnennung „offene Einträge“, `ConfigOf`) behoben. Migrationsprobe an einer Kopie der echten DB
  (SQLite-Backup, Quelle nur lesend; `database update` auf der Kopie, Kopie danach gelöscht): 81 Tabellen, Zeilenzahlen gleich
  bis auf `__EFMigrationsHistory` 35 → 37, alle neuen Spalten leer. Ganze Suite (Release) 2505/2509 grün; von den vier roten
  waren drei Lastflackern (einzeln grün), einer echt: `MeldungBuchtNachMigrationTests` las nach dem Zurückmigrieren
  `TestItemResult` über das Modell und kannte die neue Spalte nicht – auf eine Zählung umgestellt, danach grün.
- **2026-11-25** — **angehalten auf `in-arbeit`**: der Fehlerzähler von Sprint 191 überschritt 5 (Freigabe 3/11), der Lauf
  endete. Es fehlt der Rollengang: die Fehlerart hat keine Oberfläche; als Ersatz geplant war eine Live-Probe an der Kopie
  (Sohn tippt „ecole“, Zeile in `ItemReviewEvents` trägt `Accent`) – nicht mehr gelaufen. Nichts davon hängt an einer
  Entscheidung; der nächste Lauf kann die Probe fahren und abnehmen.
- **2026-11-26** — `in-arbeit` → `abgenommen` (Sprint 192, `docs/pm-sitzung-2026-11-26.md`). Verifikation am Endstand `4e70c583`: Backend-Suite 2609/2609 (Release), `vitest` 1514/1514, `tsc -b` grün, E2E
  `lerntage-und-pause` + `pfad` 2 passed (Ports 5244/5284). Reviews: `pugling-reviewer` (zweimal, zuletzt ohne Fund),
  `frontend-reviewer` (zwei Funde, behoben). Rollengang an einer Kopie der echten DB (`kopie-server.sh`, `:5201`, Server nach
  der letzten Änderung gestartet): Friedrich tippt im Üben falsch – `la rentree` für „la rentrée“ → `Accent` (27 Zeilen), `savoi`/`quix` → `Typo`, `eine Jahreszeitt` für „une saison“ → `WrongWord`; keine falsche getippte Antwort ohne Art. `BoxBefore` blieb in diesem Gang leer, weil keine der Antworten auf die Beherrschung zählte (Wiederholungen am Einführungstag) – AK 8 tragen die Tests. Commit `61a11f55` (Nachschärfung) und die Commits aus Lauf O.
