---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/frontend, rolle/student, rolle/supervisor, rolle/creator]
aliases: [Rückmeldung Frontend, Feedback-Knopf Sohn-App]
status: geschaetzt
prio: P1
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-623
grund: ""
ersetzt_durch: []
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

## Akzeptanzkriterien

1. (B-623 AK1) An jeder Karte im Üben gibt es einen Knopf „Rückmeldung“; er öffnet die acht Gründe zur
   Mehrfachauswahl und ein optionales Textfeld; Absenden ohne Grund und ohne Text ist gesperrt.
2. (B-623 AK4) Im Test gibt es den Knopf nur auf dem Ergebnisblatt, je Karte neben der Lösung.
3. (B-623 AK9) Nach dem Absenden „Danke, ist angekommen“, die Karte bleibt stehen; ein erledigter Fall erscheint
   dem Kind einmal als Hinweis.
4. (B-623 AK6) Das Vater-Web hat eine Seite „Rückmeldungen“, filterbar nach Kind und Status, mit Übung, Karte,
   Antwort und Freitext; *erledigt* setzen; Sprung zur Übung.
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
