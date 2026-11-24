---
tags: [typ/protokoll, bereich/pm, bereich/klassenarbeit]
---

# PM-Sitzung: Nachtlauf I – Klassenarbeit-Vorbereitung

**Datum:** 2026-11-23  ·  **Moderation:** PM (Nachtlauf, unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die Klassenarbeit aus der Vater-Ecke holen: das Kind sieht sie kommen, sieht seinen Stand, und vor dem Termin
erzwingt die App die Vorbereitung.

Auftrag: „Starte 3 Nachtläufe“ – dieser ist Lauf I, gelesen als der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den
Freigaben 1–11, in einem eigenen Worktree (Branch `worktree-agent-af78f7ed53d7ecfb9`, Basis `a782b4b2`), parallel zu den
Läufen G und H. Paket fest vorgegeben: B-750, B-751, B-752, B-758, B-757. Story-Ids dieses Laufs: B-940 … B-959, Sprints
133–135. Nicht angefasst: B-702/B-703/B-882 und die Aufbau-Stufen-Logik, Notenstufe (B-740–746), Mischrunde (B-764–766).

## Runde 1 — Auswahl

Kein neuer Befund unter `docs/anmerkungen/` (jüngster vom 2026-08-02). Alle fünf Stories stehen auf `geschaetzt` mit
`art: Wunsch` → baubar nach Freigabe 8; die Quelle B-248 trägt zudem die Freigabe des Nutzers vom 2026-10-23 (Autonomer
Modus, Freigabe 9). Die Entscheidungen sind stellvertretend im Vorbereitungslauf 2026-11-20 getroffen – auf Anweisung des
Nutzers („Entscheide die offenen Fragen nach deiner Empfehlung“), also keine Entscheidung dieser Nacht.

**Vor dem Bau geprüft (B-752 Risiko „Tagesplaner“):** B-644 ist seit 2026-11-20 `verworfen` (geteilt in B-730–B-739 und
B-680); von den Teilen ist nur B-730 (Tagesbegriff) gebaut, E3 (Wegfall der festen Pflicht an der Position) steht als
B-736 auf `geschaetzt`. Die Positions-Pflicht (`Cadence`, `PenaltyCoins`) besteht unverändert. B-752 Entscheidung 9
(„Teil (c) steht für sich, B-680 darf es später ersetzen“) beantwortet die Frage – kein Halt.

| Sprint | Stories | Thema |
| --- | --- | --- |
| 133 | B-750, B-751 | Das Kind sieht seine nächste Klassenarbeit und wie sicher es im Vokabelstoff ist |
| 134 | B-752, B-758, B-757 | Vor dem Termin entsteht eine tägliche Vorbereitungs-Pflicht, die der Arbeit folgt und die der Vater einstellt |

## Sprint 133 — Ziel & Umfang

**Sprint-Ziel:** Der Sohn sieht auf seiner Startseite, wann seine nächste Klassenarbeit ist und wie viele Wörter ihres Stoffs
er schon sicher kann – ohne Note, und kein anderes Kind sieht sie.
**Umfang:** B-750 (M, beides), B-751 (M, beides).
**Entwickler-Brief:** Quelle der Wahrheit für „welche Übungen gehören zur Arbeit“ ist eine geteilte Funktion
(`ClassTestBundle`, die Senke), die auch der Vater-Pfad `practice`/`repeat` liest. Zwei Lesepfade unter
`student/children/{childId}/class-tests` hinter `ChildOwnershipFilter`; „heute“ nur aus `LearningDay`. Eigene Antwortform ohne
Note. Bereitschaft: Nenner alle Vokabel-Items des Bündels, Zähler `MasteryPercent >= WeakBelowPercent`. Keine Migration.
Testweg: `KlassenarbeitCountdownTests`, `KlassenarbeitBereitschaftTests`, `KlassenarbeitKarte.test.tsx`,
E2E `klassenarbeit-countdown.spec.ts`.

## Iteration 133 — umgesetzt

**Backend:** `Services/Shared/ClassTestBundle.cs`, `Controllers/Student/ChildClassTestsController.cs` (`upcoming`,
`{id}/readiness`), `Contracts/Student/ClassTestCountdownDtos.cs`, zwei Client-Methoden; `KlassenarbeitenController` liest das
Bündel aus der geteilten Funktion. **Frontend:** `sohn/KlassenarbeitKarte.tsx` in beiden Zweigen von `SohnHome`, Texte de/en.

**Verifikation:** volle Suite siehe Abnahme; Frontend `vitest` 1393/1393, `tsc -b` grün; E2E `klassenarbeit-countdown` +
`pfad` 2 passed (Ports 5231/5204).

Mutanten-Probe Backend (`a782b4b2 + Arbeitsbaum`, 2026-11-23 23:36 und 23:53; M6/M8 im ersten Lauf `ungemessen`, weil die
Mutante nicht kompilierte – neu formuliert und gemessen; M14 im ersten Lauf **grün** → Testlücke, siehe unten):

| Mutante | Stelle | gemessen |
|---|---|---|
| M1 Status ignoriert | ChildClassTestsController.cs:37 | red |
| M2 Datum ignoriert | ChildClassTestsController.cs:37 | red |
| M3 späteste zuerst | ChildClassTestsController.cs:38 | red |
| M4 Tage +1 | ChildClassTestsController.cs:42 | red |
| M5 Note in der Antwortform | ClassTestCountdownDtos.cs:11 | red |
| M6 Betreuungsfilter ab | ChildClassTestsController.cs:22 | red |
| M7 Arbeit nicht am Kind geprüft | ChildClassTestsController.cs:54 | red |
| M8 Typfilter fehlt | ChildClassTestsController.cs:64 | red |
| M9 kein Maß wird Zahl | ChildClassTestsController.cs:74 | red |
| M10 fremdes Kind zählt | ChildClassTestsController.cs:70 | red |
| M11 Schwelle `>` | ChildClassTestsController.cs:71 | red |
| M12 Nenner nur geübte | ChildClassTestsController.cs:68 | red |
| M13 nur direkte Übungen | ClassTestBundle.cs:23 | red |
| M14 Dubletten | ClassTestBundle.cs:23 | red (nach Testergänzung) |
| M15 nur Tag-Übungen | ClassTestBundle.cs:23 | red |

Mutanten-Probe Frontend (`KlassenarbeitKarte.tsx`, 23:54 und 00:05): F1 späteste Arbeit, F2 leerer Kasten, F3 „heute“ fehlt,
F4 Tage fehlen, F5 kein Maß als Zahl, F6 ohne Übungen „kein Maß“, F7 Zähler/Nenner vertauscht, F8 falsche Arbeit gefragt,
F9 Datum fehlt, F10 `stale` nicht gelesen, F11 Bereitschafts-Id ungeprüft – alle **red**, Grundlinie danach grün.

**Review:** `pugling-reviewer` keine 🔴/🟡; 🟢 Bündel-Regel rückwärts in `ExerciseCatalogController.cs:296-300` (Kommentar der
Senke präzisiert, nicht umgebaut – Creator-Fläche außerhalb des Pakets), doppelte Unterabfrage (belassen). `frontend-reviewer`:
🟡 Datum fehlte (AK 1) → behoben mit Test; 🟢 Lesefehler-Kommentar, zwei Wächter ohne Test (jetzt F10/F11), E2E-Kommentar zur
Zeitzone – behoben ohne Zählung.

**Fehlerzähler Sprint 133: 4** (Freigabe 3/10/11; alle behoben, keiner offen): (1) M14 grün – Dubletten-Regel ohne Test, Test
auf die Funktion ergänzt (eigener Fund); (2) Datum fehlte in der Karte (🟡 Reviewer); (3) roter Wächter
`Unpaginierte_Array_GETs_Sind_Gepinnt` (Zählerzeile 31 → 32); (4) roter Wächter
`Sammlungs_Segment_Traegt_Hoechstens_Einen_Platzhalternamen` (`{classTestId}` → `{id}`).

## Runde 133 — Abnahme

- **Sohn — abgenommen (maschinell):** „Auf meiner Startseite steht mein Vokabeltest mit Datum und „in 5 Tagen“, und darunter,
  wie viele Wörter ich schon sicher kann. Die spätere Arbeit steht nicht davor.“ Gegangen in E2E
  `klassenarbeit-countdown.spec.ts` (echter Browser gegen frischen Server, Ports 5231/5204) nach der letzten Änderung. Nicht
  beurteilt: wie die Karte sich anfühlt (Platz über dem Pfad) – das sieht ein Mensch am Gerät.
- **Vater — Regressionszeuge:** `practice`/`repeat` laufen über die geteilte Bündel-Regel; `KlassenarbeitenTests` unverändert
  grün. Er sieht in dieser Runde nichts Neues.
- **Creator — Regressionszeuge:** kein Pfad des Creators im Diff; Suite grün.
- **Belege:** Backend 2185/2185 (Release, nach der letzten Änderung; Ausgangsstand 2175), Frontend `vitest` 1393/1393, `tsc -b`
  grün, E2E 2 passed (`klassenarbeit-countdown`, `pfad`). Commit `65f4d2a7`.

## Retrospektive Sprint 133

**Nachschau:** B-863 (Sprint 125, Nachtlauf E – der jüngste abgenommene Sprint ohne `nachgeschaut`): acht übersetzte Stellen
einzeln gelesen und ein `grep` über deutsche Funktionswörter in `//`-Zeilen des Testprojekts (0 Treffer) – kein Befund,
`nachgeschaut: "2026-11-24"` gesetzt. Den Index (`backlog-index.sh`) habe ich nicht gezogen: `INDEX.md` gehört in diesem
Parallel-Lauf nicht mir.

**Was die eigenen Tore durchgelassen haben:** (a) Die Mutanten-Probe fand eine Regel ohne Test (Dubletten im Bündel), weil
jeder Leser mit `Contains` filtert und eine Dublette schluckt – die Senke war nur über ihre Leser geprüft. (b) Die Karte
erfüllte AK 1 ohne Datum; mein eigener Test war als „AK 1“ beschriftet und prüfte das Datum nicht – genau die Familie aus der
Root-`CLAUDE.md` („eine Zusicherung, die den Ausgangszustand prüft“), hier: ein AK-Fall ohne Zeile. Gefunden vom
`frontend-reviewer`, nicht von einem Tor. (c) Zwei Konventions-Wächter erst im vollen Lauf – richtig so, sie sind dafür da.

**Vorschlag (nicht gelandet, Freigabe 3):** (DB) Je AK-Kriterium mit Aufzählung („Titel, Fach, Datum und Tage“) eine
`## Verlauf`-Zeile je **Glied der Aufzählung**, nicht je Kriterium – die pm-loop-Regel „one line per AK case“ deckt das dem
Wortlaut nach, aber ich habe das Kriterium als einen Fall gelesen. Mechanismus wäre eine Zeile in pm-loop Step 5:
„Ein AK mit Aufzählung hat so viele Fälle wie Glieder.“

**Bewusst ungedeckt:** die Rückwärts-Kopie der Bündel-Regel in `ExerciseCatalogController.cs:296-300` (Creator-Fläche, außerhalb
des Pakets; im Kommentar der Senke benannt).

## Sprint 134 — Ziel & Umfang

**Sprint-Ziel:** Plant der Vater eine Klassenarbeit, entsteht im Fenster davor beim nächsten Login des Sohns eine tägliche
Pflicht über ihren Stoff – ohne rückwirkenden Malus, mit einem Standard-Malus je Tag statt je Übung, endend am Vortag; sie
folgt Verschieben, Absagen und Schreiben, und der Vater stellt das Fenster ein und sieht die Last.
**Umfang:** B-752 (L, backend, Migration), B-758 (M, backend), B-757 (S, frontend). B-752 war als L geschätzt; beim Bau nicht
faktisch XL (eine Senke, eine Migration) – nicht weiter geteilt.
**Entwickler-Brief:** Senke `ClassTestPrepService` (Abgleich vor jeder Abrechnung: Login, Kauf; dazu PATCH/DELETE der Arbeit).
Idempotenz über `Klassenarbeit.PrepStartedOn` und `PlanPosition.SourceKlassenarbeitId`; Pflicht-Ende über die mit B-659 geteilte
Spalte `DutyUntil`, Schließen über `CloseDutyAsync`; die Abrechnung liest `DutyUntil` als Obergrenze. Migration
`ClassTestPrepDuty` ohne Backfill. Testweg: `VorbereitungsPflichtTests` (eingefrorene Uhr, Montag 2030-03-04),
`VorbereitungsPflichtFolgtTests`, `VorbereitungsPflichtMigrationTests`, E2E `klassenarbeit-vorbereitung.spec.ts`.

## Iteration 134 — umgesetzt

**Backend:** `f1be5aba` (B-752), `03e77040` (B-758), `d0fe6477` (Review-Fixes). **Frontend:** `1333ae02` (B-757).
Migration `20261123232647_ClassTestPrepDuty`: `PlanPositions.SourceKlassenarbeitId` (FK `SetNull`, Index),
`Klassenarbeiten.PrepWindowDays`, `Klassenarbeiten.PrepStartedOn`; SQLite baut `PlanPositions` dabei neu (FK) – der
Migrationstest prüft, dass Malus-Zeilen, Lernstand und Versuche das überleben. **An einer Kopie der echten DB nicht geprobt**
(für den Nachtlauf tabu) – das gehört vor den nächsten Heimnetz-Start.

**Verifikation:** Backend-Suite 2212/2215 im vollen Lauf (22 min unter Last dreier paralleler Nachtläufe); die drei roten
(`OpenApiExampleTests`, `ContractDocumentTests`, `SecurityHardeningTests.Login_mit_unbekannter_Id…` – Zeit- bzw. Laufzeitmessungen)
einzeln danach 16/16 grün. Frontend `tsc -b` grün, `VaterClassTests.test.tsx` 8/8; E2E `klassenarbeit-vorbereitung` 1 passed
(5231/5204). Mutanten-Probe: nur die Review-Fixes gemessen – R1 (Speichern löscht das Ende) red, R2 (Start rückwirkend) red,
R3 (Anspruch auf den Start entfernt) **green** wie erwartet: kein deterministischer Test für gleichzeitige Logins (B-944). Die
vorbereitete Probe über die AK-Fälle (27 Mutanten) ist **nicht gelaufen**: der Lauf endete vorher.

**Review:** `pugling-reviewer` – 🔴 1, 🟡 5; `frontend-reviewer` – 🟡 2. **Fehlerzähler Sprint 134:** (1) eigener Fund beim Bauen:
zwei Arbeiten im selben Abgleich legten dieselbe Übung doppelt an (Speichern je Arbeit fehlte), behoben mit Test; (2) 🔴 Speichern
der Position löschte das Ende der Vorbereitung, behoben mit Test (R1); (3) 🟡 gleichzeitige Abgleiche, behoben (Anspruch per
`ExecuteUpdateAsync`, ohne roten Test); (4) 🟡 Rückwirken ungetestet, Test ergänzt (R2); (5) 🟡 Migrationstest ohne Abhängige,
ergänzt. **Damit stand der Zähler bei 5; die übrigen vier 🟡** (Kind verkleinert sein Bündel → B-940, Ausführ-Recht umgangen →
B-941, Meldung des Fenster-Editors verschwindet → B-942, Zusicherung ohne Biss → B-943) **hätten ihn überschritten – nach Freigabe
3/11 endet der gesamte Lauf hier.** Sie sind als `Defekt` auf `idee` abgelegt, nicht behoben. 🟢 gesammelt in B-944 (ohne Zählung).

## Runde 134 — Abnahme

- **Vater — nicht abgenommen:** „Ich plane die Arbeit mit 12 Tagen Vorbereitung und sehe ‚etwa 2 Karten je Tag‘; ohne Plan im
  Fach sagt die Liste, warum nichts entsteht.“ (E2E gegangen.) Offen: das Kind kann den Stoff über Schlagwörter verkleinern
  (B-940), und zurückgezogenes Material kann über ein Schlagwort doch Pflicht werden (B-941) – beides untergräbt genau die
  Erzwingung, die er will.
- **Sohn — nicht abgenommen (maschinell gegangen):** nach dem Login steht die Vorbereitung als Knoten auf dem Pfad, die Karte
  nennt die Arbeit. Ob die Last sich fair anfühlt, sieht nur ein Mensch.
- **Creator — Regressionszeuge:** kein Creator-Pfad im Diff außer dem unveränderten Schlagwort-Endpunkt (Fund B-940 betrifft ihn).
- **Stand:** B-752, B-758, B-757 bleiben `in-arbeit` mit `wartet_auf` (Nutzer: Lauf am Fehlerzähler beendet).

## Retrospektive Sprint 134

**Nachschau:** entfällt als eigene Handlung – der vorige Sprint (133) ist in dieser Nacht abgenommen und am selben Tag nicht neu
zu besehen; B-863 ist in der Retro 133 nachgeschaut.

**Was die eigenen Tore durchgelassen haben:** Der 🔴 entstand an einer Annahme, die B-659 still gemacht hatte („Taktung ≠ None und
`DutyUntil` gesetzt gibt es nicht“) – B-752 hat genau diesen Zustand erzeugt. Die pm-loop-Regel „Changing a shared helper … is decided
per caller“ hätte es gefangen, hätte ich `DutyUntil` als geteilten Zustand gelesen und alle Schreiber gegrept
(`PlanPositionsController.cs:440`). Ich habe nur die Leser gegrept.

**Vorschlag (nicht gelandet, Freigabe 3):** (DC) Ein Wächter-Test, der für jede Spalte mit dokumentierter Invariante „nur zusammen mit
X“ (hier `DutyUntil` ↔ `Cadence`) alle Schreibstellen per Reflexion/Grep pinnt – eine neue Schreibstelle macht ihn rot. Billiger:
pm-loop Step 4 um „ein neuer Zustand einer bestehenden Spalte: jeden **Schreiber** greppen, nicht nur die Leser“ ergänzen.

**Bewusst ungedeckt:** gleichzeitige Logins (B-944 Punkt 9) – die Absicherung steht, ein Test, der sie rot zeigt, nicht.

## Halt und offene Entscheidungen

- **Halt:** Fehlerzähler Sprint 134 hätte 5 überschritten (Freigabe 3, 11). Kein Sprint 135.
- **Für den Nutzer:** (1) B-940/B-941 vor der Abnahme von B-752 – beide sind Produktfragen am Rand (darf das Kind Schlagwörter seiner
  Klassenarbeit lösen? zählt Material ohne Freigabe?), die Vorschläge stehen in den Stories. (2) Konfiguration `ClassTestPrep:MaxCardsPerDay`
  = 10 ist nicht an echten Bündeln gemessen. (3) Migration `ClassTestPrepDuty` an einer Kopie der echten DB proben, bevor der
  Heimnetz-Server sie einspielt. (4) Retro-Vorschläge (DB) und (DC).

## Offene Roadmap

Siehe `docs/backlog/`: B-940–B-943 (Defekte aus Sprint 134), B-944 (Restpunkte); B-752/B-758/B-757 warten auf sie.
