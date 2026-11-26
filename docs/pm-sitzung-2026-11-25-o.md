# PM-Sitzung: Nachtlauf O – Lerntage & Fehlerart (B-644-Etappen)

**Datum:** 2026-11-25  ·  **Moderation:** PM (autonomer Nachtlauf, Lauf O von drei parallelen)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die zwei baubaren Etappen aus B-644 liefern: die Fehlerart einer getippten Antwort wird beim Antworten
gespeichert (B-732, E1a), und Sonntag sowie eine Pause des Vaters kosten weder Serie noch Münzen (B-731, E0b).

**Arbeitsbaum:** `.claude/worktrees/nachtlauf-o`, Branch `nachtlauf/2026-11-25-o` (Basis main `6fa43808`).
Ports API 5243 / Vite 5283; Rollengang nur an einer Kopie der echten DB (nie `:5200`, nie `pugling.db`).
Neue Story-Ids nur aus B-1020…B-1029, Sprints ab 191.

## Runde 1 — Auftrag statt Rollengang-Findung

Das Paket ist vom Nutzer vorgegeben (Freigabe „Paket O“). Beide Stories stehen auf `geschaetzt`, `art: Wunsch` –
nach Nachtlauf-Freigabe 8 baubar, weil ihre Entscheidungen aus B-644 (gegrillt im Dialog) stammen. Wo eine
Ausführung eine Entscheidung verlangt, die dort nicht steht, wird sie unten als offene Frage an den Vater notiert
und nicht entschieden.

**Nachprüfung der Ist-Stände (Backlog gegen Code):**

- B-732 sagt, die Akzentregel (B-624) sei nicht gebaut. **Überholt:** seit `AccentLeniency` (2026-11-25) gibt es
  `AnswerGrader.IsAccentSlip` und das Flag `ItemReviewEvent.AccentSlip` („im Üben halb gezählt“). B-732 nutzt
  diese Regel als Quelle der Art *Akzent*; das Flag bleibt, was es ist (eine Wertung), die Art ist die Diagnose.
  AK 7 („Bewertung bleibt binär“) heißt damit: B-732 ändert an Wertung, Punkten und Kasten nichts – auch nicht an
  der halben Wertung von B-624.
- B-732 nennt drei Schreibwege; am Code sind es **sechs**: Üben (`Review`), Lernkarte (`LearnCardReviewAsync`),
  Mischrunde (`MixedReviewService`), Test schrittweise (`Answer`), Test gesammelt (`Submit`) und die Fehlnennung
  einer Liste (`CreditSetAnswer`). Der Turm (`TowerGameService`) ist eine Auswahl – keine Art.

## Sprint 191 — Ziel & Umfang

**Sprint-Ziel (Vatersicht):** Ich trage für mein Kind eine Pause ein und sehe danach, dass weder der Sonntag noch die Pause
Serie oder Münzen kosten – und jede falsch getippte Antwort meines Kindes trägt gespeichert, *warum* sie falsch war.
Widerlegt, wenn an der Kopie ein Sonntag oder ein Pausentag Malus bucht bzw. die Serie bricht, oder wenn eine falsche
getippte Antwort ohne Art in der Datenbank steht.

**Umfang:** [B-732](backlog/B-732-fehlerart-wird-beim-antworten-gespeichert.md) (M, backend) und
[B-731](backlog/B-731-lerntage-pause-und-leitplanken-je-kind.md) (L, beides) – beide `geschaetzt`, beide Etappen von B-644,
ein Sprint, ein Rollengang.

**Entwickler-Brief:**

- *Quelle der Wahrheit:* die Fehlerart entsteht nur im `AnswerErrorClassifier` (Senke), gerufen an jeder Stelle, die eine
  getippte Antwort wertet; der Lerntag nur im `DutyCalendar` (Sonntag + Pausen je Kind), gelesen von Serie, Malus,
  Serien-Mission und Zeitachse des Vaters. „Heute“ bleibt bei `LearningDay`.
- *Guards:* Wertung bleibt binär (B-732 AK 7); Pausen nur der Supervisor mit Eigentum (`ChildOwnershipFilter`, 404 fremd,
  403 Lehrer-Konto); Leitplanken auf den zusammengeführten Werten geprüft.
- *Migrationen:* `AnswerErrorKind` (B-732), danach eine für B-731 – beide nur anhängen, Probe an einer Kopie der echten DB
  (Zeilenzahlen gleich).
- *Testweg:* `FehlerartTests` (+ Mischrunde), `LerntageTests` mit eigener Factory (Sonntag frei); der Rest der Suite läuft
  mit `LearningDay:SundayFree=false`, damit wanduhr-abhängige Tests nicht am Wochentag hängen. Vitest für die zwei Masken.
- *Reihenfolge:* B-732 zuerst (kleiner, ohne Oberfläche), dann B-731 Backend, dann Frontend.

## Iteration 191 — umgesetzt

- **B-732 (Backend):** `AnswerErrorClassifier` als Senke, `AnswerGrader.SpellingSlip` (Akzent vor Tippfehler), sechs
  Schreibwege geben die Art mit, `RecordAsync` setzt `BoxBefore`; Migration `20261125213822_AnswerErrorKind`.
- **B-731 (Backend):** `DutyCalendar`/`ChildDutyDays` (Montag–Samstag außerhalb der Pausen), gelesen von Serie, Malus,
  Serien-Mission und Zeitachse; `ChildPausesController`; fünf Leitplanken an `Child`/`ChildResponse`/`UpdateChildDto` samt
  `ClearPlannerLimits`; Client-Methoden; Migration `20261125222202_LerntagePause`; `LearningDay:SundayFree` (Produktion an, in
  der Basis-Factory der Tests aus).
- **B-731 (Frontend):** `PauseSection`, `LeitplankenSection` im Reiter „Einstellungen“ des Kind-Hubs; Vitest; E2E geschrieben.
- **Verifikation:** Backend ganze Suite (Release) 2505/2509, davon drei Lastflackern (einzeln grün; drei Nachtläufe teilten die
  Maschine, zeitweise 1 GB freier Speicher) und ein echter Fund (Migrationstest las die neue Spalte); danach die betroffenen
  Klassen 48/48 grün. Vitest 1472/1473 (der eine rote `SohnBonus.test.tsx` einzeln 34/34 grün, nicht berührt), neue Datei 5/5.
  `tsc -b` ohne Fehler. `dotnet format Pugling.sln --verify-no-changes` sauber. B-732-Mutanten 14 rot, 1 abweichend; B-731-Mutanten
  nicht gemessen. Migrationsprobe an einer Kopie der echten DB: beide Migrationen eingespielt, alle 81 Tabellen mit gleicher
  Zeilenzahl (außer der Migrationshistorie, 35 → 37), neue Spalten leer, Kopie gelöscht.

## Halt — Fehlerzähler von Sprint 191 überschritten

Gezählt nach Freigabe 3/10/11 (ein Fund zählt, wenn seine Behebung Code oder Tests ändert; rote Wächter und Bestandstests
zählen als 🟡):

| # | Fund | Quelle | behoben |
| --- | --- | --- | --- |
| 1 | Synonym-Kandidaten ohne Reihenfolge vor dem Deckel | `pugling-reviewer` B-732, 🟡 | ja (+ Test) |
| 2 | `UnpaginatedArrayGetCount` 34 → 35 (Pausen-Liste) | `ConventionGuardTests` rot | ja (Pin-Zeile) |
| 3 | `ChildPause.ChildId` ohne abgenommenes Löschverhalten | `SchemaGuardTests` rot | ja (Pin-Zeile) |
| 4 | `clearPlannerLimits` ohne Fall | `PatchSemanticsTests` rot | ja (Fall) |
| 5 | Migrationstest liest neue Spalte nach Zurückmigrieren | `MeldungBuchtNachMigrationTests` rot | ja |
| 6–9 | Bestätigung der Leitplanken geht verloren (🔴), Zurücklesen ungetestet (🟡), dritter „Speichern“ (🟡) | `frontend-reviewer` B-731 | nein → B-1020 |
| 10 | Einführungstag auf Sonntag/Pause | `pugling-reviewer` B-731, 🟡 | nein → B-1021 |
| 11 | Sohn-Tag zeigt Malus-Frist am Sonntag/in Pause | `pugling-reviewer` B-731, 🟡 | nein → B-1022 |
| 12 | Hausaufgaben-Malus in der Pause | `pugling-reviewer` B-731, 🟡 | nein → Frage B-1023 |

Mit den Review-Funden zu B-731 stand der Zähler über 5 – der Lauf endet hier, nicht nur der Sprint. Beide Stories bleiben
`in-arbeit` mit `wartet_auf`; nichts ist `abgenommen`. Ohne Zählung (🟢 bzw. Kosmetik): Doku-Zeilen am Modell und an der
Fehlnennung, `ConfigOf`-Einheitlichkeit, BOM/Whitespace, die `AuthProvider`-Hülle im eigenen neuen Vitest.

**Rollengang:** nicht gelaufen (Halt vor Step 6). Vater-Seite als E2E geschrieben, nicht gefahren; Sohn-Seite nur über
Integrationstests (`LerntageTests`); Creator unberührt (kein Pfad im Diff).

## Offene Fragen an den Vater

1. **Wochenpflicht und Pause (B-731):** stellvertretend entschieden „eine Woche, die eine Pause berührt, kostet keinen Malus“.
   Kosten: ein einzelner Krankheitstag erlässt das ganze Wochenziel. Alternative: nur eine ganz pausierte Woche ist frei.
2. **Hausaufgaben in der Pause (B-1023):** soll eine in der Pause fällige Hausaufgabe weiter Malus kosten?
3. **Leere Antwort (B-732):** stellvertretend „nichts getippt / aufgegeben = falsches Wort (Wort nicht gekannt)“ – passt das zu
   dem, was du in der Auswertung (B-733) sehen willst?
4. **Testsuite ohne freien Sonntag (B-731):** die meisten Tests rechnen mit `SundayFree=false`, nur eine Klasse mit dem echten
   Sonntag. Einverstanden, oder soll die Suite auf eine feste Uhr umziehen?

## Retrospektive

**Nachschau:** nicht gelaufen – der Lauf endete am Fehlerzähler, bevor Step 8 begann; `backlog-index.sh` bewusst nicht
gestartet (es schreibt `INDEX.md`, das in diesem parallelen Lauf nicht neu erzeugt werden darf). Offen für den nächsten Lauf.

**Was die eigenen Tore durchgelassen haben:** Die Story B-731 nannte als Leser der Lerntage nur Serie und Malus; vier weitere
Leser (Einführungstag, Sohn-Tag, Hausaufgaben, Zeitachse/Mission) fand erst der Reviewer bzw. die eigene Suche. Die
Schätzung „L“ war zu klein für einen Nachtlauf mit Fünfer-Zähler.

**Vorschlag (nicht gelandet, Freigabe 3):** In `docs/nachtlauf.md` Freigabe 11 präzisieren: eine Pin-Zeile, die ein reflexiver
Wächter für ein **im Brief angekündigtes** neues Element verlangt (neuer Endpunkt, neuer Fremdschlüssel, neuer Clear-Schalter),
ist der vorgesehene Arbeitsschritt, kein Fund. Hier machten drei solche Zeilen drei von fünf Zählerpunkten aus und beendeten die
Nacht vor dem Rollengang. Kosten: ein unangekündigter Pin wäre weiter ein Fund – die Ankündigung im Entwickler-Brief ist die
Grenze, nicht das Urteil des Agenten.

## Offene Roadmap

Siehe `docs/backlog/`: B-731 und B-732 `in-arbeit` (je `wartet_auf`), neu B-1020…B-1023. Reihenfolge-Vorschlag: zuerst B-732
abnehmen (nur die Live-Probe fehlt), dann B-1020–B-1022 als Defekte im selben Zug wie die Abnahme von B-731, B-1023 im Dialog.
