---
tags: [typ/protokoll, bereich/backlog]
---

# PM-Sitzung: Backlog-Lauf 2026-11-16 als PO und Stakeholder

## Auftrag und Ausgangswert

Backlog `docs/backlog/` als PO abarbeiten, Grillen als Stakeholder (auch `Wunsch`/`Frage`, Entscheidungen markiert
als „Stakeholder-Entscheidung im Auftrag des Nutzers"), höchstens 2 Sprints à höchstens 4 Stories, Ziel ≥ 20 % von N
eingearbeitet, hartes Kontextbudget (Abgleich, Refinement, Bau und Reviews in Subagents). Keine parallele Sitzung
(beim Nutzer erfragt). Worktree `.claude/worktrees/backlog-lauf-1116`, Branch `auto/backlog-lauf-2026-11-16`,
`main` (`73de5380`) vor dem Zählen gemergt: **N = 22**.

## Abgleich gegen den Code

Ein Subagent hat alle 22 offenen Stories gegen den Code geprüft. Ergebnis in Kürze: B-09, B-249, B-536, B-539 zu groß
(teilen), B-539/B-580 tragen Nutzer-Entscheidungen aus B-537 und hängen aneinander, B-558 sinnvoll erst nach B-539,
B-31 wartet nur auf das Gegenhören am Handy. Klein und wertvoll: B-410, B-219, B-588.

Stakeholder-Entscheidungen im Auftrag des Nutzers (2026-11-16):

- **B-376 verworfen** – ein Sammelalbum für Gems trägt nichts zur Pflicht-Kernidee bei und überschneidet sich mit
  den Skins und B-221.
- **B-582 verworfen** – setzt die ungebaute Probeklausur B-249 voraus; wird mit B-249 neu angelegt.

## Refinement

- **B-588** `idee` → `geschaetzt` (S): Freitext `BookPages` ≤ 20 Zeichen an der Übung, additiv im Creator-Vertrag,
  PUT-Semantik wie `Source`; der Sohn sieht die Seite nur über `PositionStatus`, nie in Test-, Klausur- oder
  Practice-DTOs. Neun Stakeholder-Entscheidungen in der Story.
- Daraus zwei Folge-Stories: **B-589** (`Defekt`, P2: `laden.py --ersetzen` schickt das PUT ohne `bookPages` und
  löscht eine gepflegte Seite) und **B-590** (P3: „schon durchgenommen" und Lehrwerk-Bezug in Vater-Plan/Lernstand).

## Sprint 81 — Ziel & Umfang

**Ziel (Sicht Vater):** „Ich kann die Strenge der Rückstufung je Position fair einstellen, ohne dass der Sohn sie
unterlaufen kann, und mein Sohn findet zu jeder Übung die Seite im Buch."

Umfang: B-410 (Tor, nur Tests), B-219 (verzeihende Abstufung je Position), B-588 (Reihe · Unit · Seite an der Übung).

## Iteration 81 — umgesetzt

| Story | Kern | Belege |
| --- | --- | --- |
| B-410 | `NullElementGuardTests`: reflexives Tor über alle 128 Body-Parameter / 90 Listen, Mindestzahlen 100/75 | 5 neue Fälle, 8/8 Mutanten rot |
| B-219 | `PlanPosition.ForgivingDemotion` + `PositionItemProgress.LastMissed`, Regel in `PositionPlayService.ApplyReview`, Häkchen „Ausrutscher verzeihen" im Positions-Editor | Migration `20261116065113_ForgivingDemotion`, 14/14 Mutanten rot, Vitest 66/66 |
| B-219 Fix | Selbstauskunfts-Pfad (`climbs`) lässt `LastMissed` stehen (Review-Fund, Entscheidung 11) | 2 neue Tests, Mutant rot |
| B-588 | `Exercise.BookPages`, Editor beidseitig, Pille „Reihe · Unit · S. x" am Positions-Blatt | Migration `20261116071258_ExerciseBookPages`, 15/15 Mutanten rot |

Qualitätsschwelle: volle Suite 1715/1715 (exit 0), Vitest 1121/1121, `tsc -b` und `dotnet format --verify-no-changes`
sauber. Migrationsprobe gegen eine **Kopie** der echten `pugling.db`: sieben ausstehende Migrationen laufen durch,
Zeilenzahlen unverändert (39 Positionen, 115 Item-Fortschritte, 64 Übungen), neue Spalten „hart" bzw. leer.

Reviews (`pugling-reviewer`, `frontend-reviewer`): ein Korrektheitsfund im Increment (B-219: ungeprüftes „gewusst"
über die Selbstauskunft löschte den Fehler-Merker – der Sohn hätte die harte Rückstufung umgehen können), im Sprint
behoben. Nicht blockierend und offen: aria-describedby fürs ausgegraute Häkchen, Klick→Payload nur indirekt getestet,
Anlegeformular ohne Seitenfeld, doppelte Normalisierung in `BookPagesProblem`. Die Invarianten-Prüfung des Fixes fand
einen weiteren Pfad (gewerteter Pfad mit nicht getippter Tagesstufe) – als Produktfrage **B-591** angelegt.

## Runde 81 — Abnahme

Maschineller Rollengang `frontend/e2e/sprint-81-rollengang.spec.ts` gegen echte UI und frisch gestarteten Server
(Ausweichports 5201/5174): **2 passed**, zweimal hintereinander. Dazu die sechs betroffenen Specs (Pfad, Feldhilfe,
Positions-Editor, Leitner aus, Lehrwerk-Weg, Full-Flow): **8 passed**.

- **Vater — abgenommen:** setzt „Ausrutscher verzeihen" an einer Leitner-Position, sieht nach Neuladen „· verzeihend";
  ohne Leitner ist das Häkchen deaktiviert. Den Selbstbetrugs-Pfad deckt der Integrationstest, nicht der Gang.
- **Creator — abgenommen:** pflegt „42–43" im Übungs-Editor, der Wert steht nach Neuladen im Feld.
- **Sohn — abgenommen:** sieht am Blatt „<Reihe> · <Unit> · S. 42–43", an der Übung ohne Seite keine Pille.
- Nicht maschinell belegt: wie sich die Pille am Handy anfühlt (Liste für den Menschen, Punkt 6).

## Retrospektive 81

- **Nachschau:** die sechs im Lauf vom 2026-11-15 abgenommenen Stories ohne `nachgeschaut` (B-252, B-39, B-47, B-538,
  B-583, B-584) geprüft – sauber, `nachgeschaut: 2026-11-16` gesetzt.
- **Durchrutscher aus Sprint 79:** die volle Playwright-Suite (159/160) fand `vater-von-null.spec.ts:460` rot – der
  mit B-39 eingeführte Link „Lernstand von <Kind>" macht den Teilstring-Treffer auf den Kindnamen mehrdeutig. Die
  Nachschau hatte das Frontend nur gestreift. Spec auf `exact: true` korrigiert, danach 1 passed; Vermerk in B-39.
- **Was rutschte:** der Bau von B-219 schrieb einen neuen Zustand (`LastMissed`) in einem geteilten Helfer
  (`ApplyReview`), ohne jeden Aufrufer darauf zu prüfen, ob dessen Antwort geprüft ist. Das Review fand es, kein Tor.
- **Was es jetzt deckt:** eine Regel in Schritt 4 des `pm-loop`-Skills – wer in einem geteilten Helfer neuen Zustand
  schreibt, listet jeden Aufrufer und entscheidet je Aufrufer, ob dessen Eingabe vertrauenswürdig ist (geprüft vs.
  Selbstauskunft). Kein Tor: die Unterscheidung „geprüft" ist fachlich und nicht mechanisch erkennbar.
- **Eigener Fehler beim Abschluss:** die Aufräum-Verschiebung eines Testkommentars (per `awk`, ohne anschließendes
  `tsc`) zerbrach `ExerciseEditModal.test.ts`; CI fing es (Typecheck, Artefakt-Smoke). Behoben im Nachzug, `tsc -b`
  0 und Vitest grün. Lehre: nach jedem Frontend-Edit `tsc -b`, auch bei „nur einem Kommentar".
- **Halt:** B-410 ist ein neues Tor und das Review fand einen Defekt im Increment – beide Halt-Bedingungen des
  Backlog-Laufs greifen; Sprint 2 entfällt.

## Abschluss

### Zähltabelle

| Größe | Wert |
| --- | --- |
| N (offen zu Beginn, nach Merge von `main`) | 22 |
| eingearbeitet | 5 = 23 % von N (Ziel ≥ 20 % erreicht) |
| abgenommen | 3 (B-410, B-219, B-588) |
| verworfen | 2 (B-376, B-582) |
| neu angelegt | 3 (B-589, B-590, B-591) |
| **offen am Ende (Index)** | **20** |

### Liste für den Menschen

1. **B-589 zuerst** (`Defekt`, P2): bis es gebaut ist, löscht jeder `laden.py --ersetzen`-Lauf eine von Hand gepflegte
   Buchseite. Bis dahin Seiten erst nach dem letzten Import pflegen.
2. **B-591 entscheiden** (Frage): soll eine ungeprüfte „gewusst"-Antwort auf einer nicht getippten Tagesstufe den
   Fehler-Merker der milden Abstufung löschen dürfen?
3. **Neues Tor B-410** ansehen (`NullElementGuardTests`) – der Lauf hält deshalb hier an.
4. Stakeholder-Entscheidungen zum Umstoßen: B-376/B-582 verworfen; B-219 Entscheidungen 9–11; B-588 Entscheidungen 1–9
   (u. a. die Seite ist vor einem Positionstest sichtbar, nicht in der Klausur).
5. Weiter offen aus dem letzten Lauf: B-539/B-580 (P1, L) vor dem Bau teilen; B-31 am Handy gegenhören.
6. Am echten Gerät anschauen: Häkchen „Ausrutscher verzeihen" im Positions-Editor und die Buchseiten-Pille beim Sohn.
