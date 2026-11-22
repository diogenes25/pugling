# PM-Sitzung: Nachtlauf 2026-11-22-c – Aufbau-Stufe im Katalog

**Datum:** 2026-11-22 (Shell-Datum beim Start, 21:08 WEST)  ·  **Moderation:** PM (Opus)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die Aufbau-Stufen-Kette aus B-536 anfangen (B-700 → B-701 → B-704), ihre Entscheidungen hat der Nutzer
am 2026-11-19 bestätigt.

Auftrag: „Starte einen Nachtlauf" – die Freigaben 1–10 aus [nachtlauf.md](nachtlauf.md) gelten.

- **Branch:** `main` im Haupt-Checkout. Keine andere Sitzung lief beim Start (`list_sessions`: keine läuft).
- **Fremder Stand im Arbeitsbaum, nicht angefasst und nicht mitcommittet:**
  `docs/backlog/B-626-…md`, `docs/backlog/B-784-…md`, `docs/backlog/INDEX.md`, `docs/openapi/v1.json`
  (nur Zeilenenden), `scripts/friedrich-testvorbereitung-2026-11-22.ps1`,
  `scripts/theodor-part-c-aufholen-2026-11-19.ps1`.
- **Heimnetz (:5200, echte DB) unberührt.** Der nächste Start spielt die Migration `AufbauStufe` ein (zwei nullable
  Spalten an `Exercises`, Fremdschlüssel; SQLite baut die Tabelle dafür neu).

## Runde 1 — Auswahl

Ausgewählt nach dem Stand der letzten beiden Läufe: Lauf A hielt vor B-731, weil B-730s Entscheidungen 4–8 unbestätigt
sind; alles, was auf B-730 aufbaut, bleibt darum liegen. Die B-536-Kette hängt nicht daran, und ihre
Entscheidungen (B-700, B-701, B-704) sind bestätigt. Die Reibung ist beobachtet, nicht ausgedacht: Friedrich scheiterte am
2026-11-13 an zu schwerem Grammatik-Stoff (B-536, Quelle B-535).

- **Creator:** „Ich kann eine leichtere Übung neben die schwere legen, aber nirgends sagen, dass sie ihre Vorstufe
  ist." → B-700.
- **Sohn:** „Ich bekomme immer gleich die schwere Übung." → B-701 (Spielen der Stufe), B-704 (Anzeige).
- **Vater:** „Ich will nur das Ziel wählen und die Stufen laufen von selbst." → B-701, B-704.

## PM-Synthese & Priorisierung (→ Entwickler)

| Prio | Story | Größe | Wo | Inhalt | Ergebnis der Nachprüfung |
|---|---|---|---|---|---|
| P1 | [B-700](backlog/B-700-aufbau-stufe-am-katalog.md) | M | backend + Migration | Vorstufe und Ziel-Übung am Katalog | baubar |
| P1 | [B-701](backlog/B-701-position-spielt-die-aufbau-stufe.md) | L | backend + Migration | Position spielt die Stufe, Aufstieg | **angehalten**: Entscheidung 4 braucht ein Schema, das die Schätzung nicht kennt |
| P1 | [B-703](backlog/B-703-ki-creator-erzeugt-vorstufen.md) | M | agent | KI-Creator erzeugt Vorstufen | **angehalten**: Auswahlregel von `--existing` offen |
| P1 | [B-704](backlog/B-704-aufbau-weg-in-der-oberflaeche.md) | M | frontend | Aufbau-Weg in der Oberfläche | wartet fachlich auf B-701 |

**Roter Faden:** Der Lernweg zum Stoff wird Teil des Katalogs statt Wissen im Kopf des Vaters.

## Sprint 1 — Ziel & Umfang

**Sprint-Ziel:** Aus Sicht des Creators lässt sich im Katalog festhalten, welche Übung eine Vorstufe welcher Ziel-Übung
ist, und der Katalog verweigert jeden Weg, den die Position später nicht spielen könnte (andere Unit, Kette, Stufe
außerhalb 1–2, Selbstverweis, verwaistes Ziel). Widerlegbar durch einen solchen Weg, der gespeichert wird.

**Umfang:** B-700.

**Entwickler-Brief:** Quelle der Wahrheit ist `ExerciseControllerBase` (beide Schreibpfade, eine Prüfung
`BuildPathProblemAsync` vor der ersten Zuweisung). Fremdschlüssel `NoAction` statt `Restrict`: SQLite prüft RESTRICT je
Zeile, und das Löschen einer Unit kaskadiert Ziel und Vorstufe in einer Anweisung. Den Schutz vor verwaisten Wegen hält
der Löschpfad (409; nach dem Review mit eigenem Code `exercise_is_build_target`). Migration `AufbauStufe` hängt an `DutyUntilCadenceUntil`. Testweg:
`AufbauStufeKatalogTests`, `AufbauStufeMigrationTests`, Schema-Tore.

**Umfang erweitert (begründet):** B-704 wurde geteilt (B-840 Editor, B-841 Rest). B-840 kam in diesen Sprint, weil der
Übungseditor einen Vollersatz-PUT schickt: Ohne die zwei Felder hätte jedes Speichern im Vater-Web eine Vorstufe still
gelöscht (B-700 AK 3). Das Sprint-Ziel war ohne B-840 also nicht haltbar.

## Iteration 1 — umgesetzt

- **Backend (API-First):** `Exercise.BuildLevel`/`BuildTargetExerciseId` mit Selbst-Fremdschlüssel (`NoAction`),
  `BuildPathProblemAsync` in beiden Schreibpfaden, Lösch-Sperre mit eigenem Code `exercise_is_build_target`, Felder in
  `ExercisePayload`/`ExerciseResponse`/`ExerciseSummary`/`ExerciseDetail` (additiv), Migration `AufbauStufe`.
- **Frontend:** Abschnitt „Aufbau-Weg" (`BuildPathField.tsx`) im Übungseditor; `payloadFrom` reicht beide Felder mit;
  deutscher Text für `exercise_is_build_target`.
- **Verifikation:** `dotnet test Pugling.sln -c Release` 2089/2089; Vitest 1356/1356; `tsc -b` sauber; Mutanten in drei
  Läufen 23/23 rot am benannten Fall (Tabellen im `## Verlauf` von B-700/B-840); E2E 3 passed.
- **Reviews:** `pugling-reviewer` 2 × 🟡 (beide behoben: eigener Code für die Lösch-Sperre, Migrationstest mit
  Kind-Tabellen); `frontend-reviewer` kein 🔴/🟡, zwei 🟢 billig behoben. **Zähler Sprint 1: 2 von 5.**
- **Commit:** `e36a5c79`.

## Runde 2 — Re-Review / Abnahme

- **Creator:** abgenommen, im Maschinengang `frontend/e2e/aufbau-weg-editor.spec.ts` gegen frisch gestarteten Server
  (:5207/:5179) nach dem letzten Edit. Abgelaufen wurde: Ziel wählen (nur Übungen der eigenen Unit, nicht die Übung selbst),
  Stufe 2 speichern, neu öffnen und beide Werte sehen, ohne Änderung speichern (der Weg bleibt), an der Ziel-Übung den Satz
  „ist Ziel von einer Vorstufe" statt einer Auswahl sehen. Offen: Den Weg sieht das Kind erst mit B-701.
- **Vater:** Regressionszeuge. Kein Pfad der Supervisor-Ebene im Diff; Suite grün; `einkleidung.spec.ts` und
  `grammatik-themen.spec.ts` (Editor- und Planbau-Wege) grün.
- **Sohn:** Regressionszeuge. Kein Pfad der Student-Ebene im Diff; Suite grün.

## Retrospektive

**Nachschau:** Die Vorsprints B-730 (Lauf A, `3c0122fa`) und B-660 (Lauf B, `199fc39f`) wurden je von einem Reviewer
nur lesend nachgesehen; die Prüfpunkte stehen im `## Verlauf` der Stories. Gefunden: sechs 🟢-Entgleitungen, keine 🔴 und
keine 🟡, als eigene `Defekt`-Stories angelegt: B-842, B-843, B-844 (`entgangen_bei: B-730`) und B-845, B-846, B-847
(`entgangen_bei: B-660`). `nachgeschaut: 2026-11-22` ist an beiden gesetzt. Index: **544 von 575**.

**Was die eigenen Tore dieses Sprints durchgelassen haben** (beide Funde vom `pugling-reviewer`, nicht von Test oder Tor):

1. **Ein wiederverwendeter Fehler-Code mit fester deutscher Übersetzung.** `exercise_in_use` wird im Frontend durch einen
   Satz ersetzt, der auf „Verwendung" verweist. Die neue Sperre hätte dorthin gezeigt, wo sie nie erscheint. Das ist die
   B-413-Falle zum zweiten Mal. Meine Tests prüften Status und Code, nicht die Übersetzung, die der Vater liest.
2. **Ein Migrationstest ohne die Kinder der neu gebauten Tabelle.** Das Vorbild (`StudyPlanSubjectMigrationTests`) hatte
   die Regel, ich habe sie nicht übertragen.

**Vorgeschlagener Mechanismus (Freigabe 3: nur vorgeschlagen, nicht gelandet):** Ein Tor, das je `ApiErrors`-Code mit
fester Übersetzung in `frontend/src/lib/api.ts` die Aufrufstellen (`ProblemWithCode(ApiErrors.X, …)`) in einer gepinnten
Liste führt, nach dem Muster von G2. Eine neue Aufrufstelle eines übersetzten Codes erzwingt dann eine bewusste Zeile mit
der Antwort auf die Frage „zeigt die Fläche, auf die der Text verweist, diese Sperre?". Heute merkt das nur ein Reviewer.
Kosten: eine Liste mehr, die bei jedem neuen Sperrpfad mitwächst. Ungedeckt bliebe, ob der Text inhaltlich passt; das Tor
erzwingt nur die Frage.

**Bewusst ungedeckt:** das Rennen zwischen der Prüfung des Wegs und dem Schreiben (endet als 500, nicht als stiller
Verlust). Die Kosten von B-700 Entscheidung 4 (ein fremder Creator kann mir das Löschen sperren) sind eine
Produktfrage und stehen bei B-700.

## Halt nach Sprint 1 (begründet)

Kein formaler Halt-Grund der Freigaben (kein Tor gelandet, Zähler 2 von 5). Gehalten wird aus zwei Gründen:

1. **Eine zweite Sitzung arbeitet im selben Checkout** („Puckling natives Gameplay-Frontend", seit etwa 21:26: neue
   Dateien `docs/backlog/B-822-…md`, `docs/turm-spiel-plan.md`). Ihr Stop-Hook ließ das Test-Tor auf meinem halbfertigen
   Stand laufen und sperrte meine Testläufe. Die Regel aus früheren Läufen gilt: kein zweiter schreibender Lauf neben
   einer aktiven Sitzung.
2. **Das Thema ist ohne Entscheidung des Nutzers erschöpft.** B-701 braucht die Antworten aus seiner Nachprüfung (Schema
   des stufengebundenen Lernstands, Verhalten beim Aufstieg, Teilen), B-703 die Auswahlregel von `--existing`. B-841
   wartet auf B-701.

## Offene Roadmap (priorisiert)  ·  Konkreter Änderungsstand (für Review)

**Zu entscheiden (Tagesordnung fürs Grillen):**

- **B-701:** (a) Stufe als Schlüsselteil von `PositionItemProgress` oder Verwerfen beim Aufstieg; (b) laufende Sitzung,
  Lernkarten-Stand und Tagesdosis beim Aufstieg; (c) Ziel- oder Stufen-Übung in Übersicht und Bericht; (d) Teilen.
- **B-703:** Typ-Rangfolge für `build --existing`; zweiter Lauf zur selben Ziel-Übung: abbrechen oder ergänzen.
- **B-700 (zum Umstoßen):** eigener Code `exercise_is_build_target` statt `exercise_in_use` (AK 4); die Kosten von
  Entscheidung 4.
- **Retro-Vorschlag oben:** das Tor für übersetzte Fehler-Codes landen oder verwerfen.

**Neu angelegt:** B-840 (abgenommen), B-841 (geschätzt, wartet auf B-701), B-842…B-847 (Defekte aus der Nachschau, `idee`).
Nummernbereich dieses Laufs: B-840…B-859.

| Commit | Inhalt |
|---|---|
| `e36a5c79` | Sprint 1: Aufbau-Stufe am Katalog und im Übungseditor (B-700, B-840) |
| (folgt) | Backlog und Protokoll: B-700/B-840 abgenommen, B-704 geteilt, B-701/B-703 Nachprüfung, Nachschau B-730/B-660 |

- **Heimnetz:** Der nächste Start spielt die Migration `AufbauStufe` in die echte `pugling.db` ein (zwei nullable
  Spalten; SQLite baut `Exercises` dafür neu; der Migrationstest belegt, dass Items, Lernstand, Grants und Positionen das
  überstehen). Das Startskript legt vorher eine Sicherung an. Wie bei `ShopStockPerChild` meldet EF, dass der Neubau
  nicht in einer Transaktion läuft.
- **Nicht committet, weil fremd:** B-626, B-784, B-822, `docs/turm-spiel-plan.md`, zwei Skripte unter `scripts/`,
  und `docs/backlog/INDEX.md` (neu erzeugt, verweist auch auf die fremden, ungetrackten B-784/B-822).
- Nichts gepusht.
