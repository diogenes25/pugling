---
tags: [typ/story, status/in-arbeit, bereich/training, bereich/frontend, rolle/student, rolle/supervisor, rolle/creator]
aliases: [Rückmeldung Frontend, Feedback-Knopf Sohn-App]
status: in-arbeit
prio: P1
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-623
grund: ""
ersetzt_durch: []
wartet_auf: "Review-Funde Sprint 88 einarbeiten (Nachtlauf-Halt, Zähler 8)"
---

# B-629 · Der Rückmelde-Knopf in der Sohn-App und die Meldungen im Vater-Web (Frontend)

Teil 2 von 3 aus [B-623](B-623-feedback-knopf-an-der-karte.md), beim Schätzen am 2026-11-17 geteilt. Hängt an
der API aus [B-628](B-628-rueckmeldung-backend.md) – erst bauen, wenn B-628 abgenommen ist (API-First).

## User Story

Als **Lernender** möchte ich an der Karte auf „Rückmeldung“ tippen, Gründe antippen und optional etwas
schreiben; als **Supervisor** möchte ich die Meldungen meiner Kinder in einer Liste sehen und erledigen; als
**Ersteller** möchte ich an meiner Übung sehen, was gemeldet wurde.

## Ist-Stand am Code

Siehe [B-623 → Ist-Stand am Code](B-623-feedback-knopf-an-der-karte.md#ist-stand-am-code). Ergänzt beim Schätzen
(2026-11-17):

- Karte im Üben: `frontend/src/sohn/SohnPractice.tsx:518` (`<Lernkarte …>`), Bewertung `:250`.
- Ergebnisblatt des Tests: `TestResult` in `frontend/src/sohn/SohnTest.tsx:337`.
- Vater-Web-Routen: `frontend/src/vater/VaterApp.tsx:142` (Übungen), `:153` (Kind); die Anmerkungen-Seite
  hängt nur im Entwicklungsbuild (`:163`).
- Vorbild für einen Hinweis an der Übung im Creator-Editor: `ExerciseRatingPanel`
  (`frontend/src/vater/VaterExercises.tsx:9`, `:440`).

## Entscheidungen

Übernommen aus [B-623](B-623-feedback-knopf-an-der-karte.md#entscheidungen). Diesen Teil binden **E4** (die acht
Knöpfe mit ihren Texten für das Kind), **E5** (im Test nur auf dem Ergebnisblatt), **E7** (Vater-Liste und
Hinweis im Creator-Editor ohne Freitext und Kind) und **E8** („Danke, ist angekommen“, Hinweis nach *erledigt*).

Dazu eine eigene Entscheidung dieser Story:

1. **„Sprung zur Übung“ ist der Link zum Plan, in dem das Kind die Übung spielt** (Entscheidung des Vaters,
   2026-11-19, auf den Review-Fund 🟡7). *Begründung:* Einen Tiefenlink auf eine einzelne Übung gibt es in
   `/vater/exercises` nicht; der Plan zeigt die Position mit ihrer Übung und dem Report. *Kosten:* zum Korrigieren
   der Karte geht der Vater von dort noch einen Schritt in die Übungsverwaltung.

## Akzeptanzkriterien

1. (B-623 AK1) An jeder Karte im Üben gibt es einen Knopf „Rückmeldung“; er öffnet die acht Gründe zur
   Mehrfachauswahl und ein optionales Textfeld; Absenden ohne Grund und ohne Text ist gesperrt.
2. (B-623 AK4) Im Test gibt es den Knopf nur auf dem Ergebnisblatt, je Karte neben der Lösung.
3. (B-623 AK9) Nach dem Absenden „Danke, ist angekommen“, die Karte bleibt stehen; ein erledigter Fall erscheint
   dem Kind einmal als Hinweis.
4. (B-623 AK6) Das Vater-Web hat eine Seite „Rückmeldungen“, filterbar nach Kind und Status, mit Übung, Karte,
   Antwort und Freitext; *erledigt* setzen; Sprung zur Übung (als Link zum Plan, Entscheidung 1).
5. (B-623 AK7) Im Creator-Editor steht an der Übung die Zählung der Gründe, ohne Freitext und Kind.
6. Jede Mutation läuft über `useAction` + `StatusBanner`, Knöpfe tragen `disabled={busy}`
   ([frontend/CLAUDE.md](../../frontend/CLAUDE.md)); der Dialog ist per Tastatur bedienbar.

## Schätzung

**Größe: M** — wie der Batch-Pfad im `MediaSelector`: drei Oberflächen, aber jede klein und an vorhandenen
Mustern (`ExerciseRatingPanel`, Vater-Listen mit `ListControls`).

- **`migration: nein`**, **`vertragsbruch: nein`** — nur Verbraucher der API aus B-628.

### Risiken

1. **Der Knopf darf das Spiel nicht stören.** In der Arcade zählt jede Sekunde (Heartbeat, Combo); der Dialog
   darf weder einen Tastendruck als Antwort durchreichen noch den Cursor bewegen.
2. **Doppelte Abgabe** beim Doppeltippen – Sperre im Primitiv (`useAction`), siehe Notiz „Effekt-Doppellauf“.
3. **Rollengang.** Der Weg Sohn meldet → Vater erledigt → Sohn sieht Hinweis braucht beide Logins in einer E2E.

### Angriffsplan

1. Rückmelde-Dialog als Komponente (Vitest), eingehängt in `Lernkarte`/`SohnPractice` und `TestResult`.
2. Vater-Seite „Rückmeldungen“ mit Route und Eintrag in der Navigation.
3. Zählung im Creator-Editor neben `ExerciseRatingPanel`.
4. Hinweis nach *erledigt* auf der Sohn-Startseite.

### Testweg

- Vitest je Komponente (Sperre ohne Grund/Text, Mehrfachauswahl, Knopf im Test nur auf dem Ergebnisblatt).
- Neue E2E `frontend/e2e/rueckmeldung.spec.ts`: Sohn meldet im Üben → Vater sieht und erledigt → Sohn sieht den
  Hinweis. Sie ist zugleich der Rollengang.
- Reviewer: `frontend-reviewer`.

## Verlauf

- **2026-11-17** — angelegt beim Schätzen von B-623 (Teil 2 von 3: Frontend), geschätzt M.
- **2026-11-18** — Nachtlauf, Sprint 88: gebaut. `frontend/src/sohn/KartenMeldung.tsx` (Knopf, acht Gründe mit `aria-pressed`, Text, Sperre ohne Grund/Text, Dank; `MeldungsHinweis` auf der Startseite), eingehängt in `SohnPractice` (je Karte) und `TestResult` (je Zeile, nur mit Plan/Position); Vater-Seite `/vater/rueckmeldungen` (Navigation „Betreuen“); `ExerciseFeedbackPanel` hinter „💬 Rückmeldungen“ an eigenen Übungen; i18n `feedback.*` de/en, `problem.feedback_during_test`. Vitest `KartenMeldung.test.tsx` 5/5; E2E `frontend/e2e/rueckmeldung.spec.ts` **1 passed** (Sohn meldet → Vater erledigt → Ersteller zählt → Sohn sieht den Hinweis; Ausweichports 5211/5181).
- **2026-11-18** — Live-Region-Tor (`src/liveRegions.test.ts`) rot: Dank und Hinweis entstanden mit ihrem Text → Regionen dauerhaft, `MeldungsHinweis` in `REGION_BAUSTEINE` (zählt).
- **2026-11-18** — Review `frontend-reviewer`, **offen** (Lauf beendet, bevor eingearbeitet): 🟡1 Fokus fällt beim Öffnen/Abbrechen/Absenden auf `body`, `aria-expanded` fest `false`; 🟡3 `GRUENDE` nicht erschöpfend typisiert, `api.feedbackReasons` ungenutzt; 🟡4 Filterwechsel zeigt alte Zeilen mit aktiven Knöpfen (`list.stale`); 🟡5 E2E „Hinweis nur einmal“ kann nach `reload` nicht fallen (`waitForResponse` fehlt); 🟡6 keine Zusicherung „kein Knopf während des Tests“; 🟡7 AK 4 „Sprung zur Übung“ ist ein Link zum Plan (es gibt keinen Tiefenlink auf eine Übung) – Entscheidung nötig; 🟢 Vater-Labels Kurzform statt „dieselben Worte“, Tastatur-Fall-Kommentar, Knopf an `isOwn` statt Schreibrecht, `ListenAusfall` ohne `schluessel`, `children.error` stumm, CSS-Klassen ohne Regeln.
- **2026-11-18** — **Halt des Nachtlaufs (Freigabe 3):** der Zähler des Sprints stand nach dem Frontend-Review bei **8** (Backend 2, Live-Region-Tor 1, Frontend-Review 5) – mehr als fünf. Code liegt ungemergt auf `refs/heads/auto/sprint-88-rueckmeldung`; nicht abgenommen, kein Rollengang-Urteil.
- **2026-11-19** — 🟡7 entschieden vom Vater: „Link zum Plan reicht“ → Entscheidung 1, AK 4 präzisiert; kein Code nötig.
  Offen bleiben die übrigen fünf 🟡 (1, 3, 4, 5, 6).
