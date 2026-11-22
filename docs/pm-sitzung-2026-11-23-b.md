# PM-Sitzung: Nachtlauf B – Flackern, Fokus-Effekt, Pflicht wird Kür (Worktree)

**Datum:** 2026-11-22/23 (Shell-Datum beim Start: 2026-11-22)  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die zwei offenen Test-Defekte aus Lauf B vom 2026-11-22 schließen, danach B-659 bauen.

Paralleler Lauf zu Nachtlauf A auf `main`. Die PO-Rolle hat der Nutzer übertragen: stellvertretend entscheiden und
protokollieren, statt anzuhalten.

- **Branch:** `claude/nachtlauf-b-dcf748`, Worktree `.claude/worktrees/nachtlauf-b-dcf748`.
- **Basis:** `ec0c705a`. Der Worktree stand beim Start auf `560bd2f0` und wurde per `--ff-only` vorgezogen.
- **Bereich:** B-787, B-788, B-659, danach frontend-/testlastige Stories. B-791 und B-670 gehören Lauf A, ebenso
  backend-fachliche Defekte (außer B-659).
- **Nummern:** Lauf B vergibt nur B-820..B-839.
- **Ports:** E2E auf :5207/:5179. :5200 ist die echte DB (Heimnetz) und blieb unberührt.
- **`frontend/node_modules`:** als Junction auf den Haupt-Checkout (Playwright 1.65.1).
- Nichts gepusht, nichts nach `main` gemergt.

## Runde 1 — Einrichtung und Auswahl

- **Rollen:** B-787 und B-788 sind Testcode plus eine Zeile im Dep-Array. Kunde ist der Entwickler; die drei
  Kundenrollen sind Regressionszeugen.
- **Befund vorab:** Drei volle Vitest-Läufe in Dateien geschrieben: 1327/1327, 1327/1327, 1327/1327. Das Flackern trat
  nicht von selbst auf, darum wurde es mit einer eingeschleusten Pause hergestellt (siehe B-787).

## PM-Synthese & Priorisierung (→ Entwickler)

| Prio | Story | Größe | Wo | Inhalt |
|---|---|---|---|---|
| P3 | [B-787](backlog/B-787-sohnprogress-vitest-flackert-unter-last.md) | XS | frontend (Vitest) | `findByText(/Plan 5/)` trifft Knopf und Zusammenfassung |
| P3 | [B-788](backlog/B-788-fokus-effekt-abhaengigkeiten-ungetragen.md) | XS | frontend | Dep-Array des Fokus-Effekts, Kommentar verspricht ein Rot |
| P3 | [B-659](backlog/B-659-pflicht-wird-kuer-bricht-die-serie.md) | L | backend + Migration | Pflicht → Kür bricht die Serie rückwirkend |

**Roter Faden Sprint B1:** Jede Zusicherung im Sohn-Bereich sagt, was sie trägt – und trägt es.

## Sprint B1 — Ziel & Umfang

**Sprint-Ziel:** Aus Sicht des Entwicklers fällt `SohnProgress.test.tsx` nicht mehr durch die Zeitplanung, und kein
Kommentar am Fokus-Effekt verspricht ein Rot, das keine Mutante liefert. Widerlegbar durch eine Pause nach dem Rendern,
die die Datei rot macht, oder eine Mutante, deren Ausgang dem Kommentar widerspricht.

**Umfang:** B-787, B-788 (beide `idee` → im Lauf autonom gegrillt, Defekt).

**Entwickler-Brief:** Erst die Meldung einfangen, dann reparieren. Kein Produktverhalten ändert sich. Proben über
`mutant-probe.py`, Treffer der Pause über die Meldung zugesichert (Retro-Lehre aus Lauf B).

## Iteration B1 — umgesetzt

### B-787 (`7b501ed4`)

- **Meldung:** Mit 50 ms Pause nach `renderProgress()` auf `ec0c705a`: 2 failed / 7 passed,
  `TestingLibraryElementError: Found multiple elements with the text: /Plan 5/` bzw. `/Plan 99/`. Die Planwahl
  (Knopf „Plan 5“) und die Zusammenfassung („Plan 5 · …“) tragen denselben Titel.
- **Die Vermutung der Idee** (`setPlanId` ohne `waitFor`) ist damit widerlegt.
- **Reparatur:** Die Suche zielt auf die Zusammenfassung (`/Plan 5 ·/`). Mit derselben Pause in acht Fällen 9/9 grün.
- **Mutanten:** „spielbar“ und „gemerkt“ in `useChosenPlan` gestrichen, beide rot am richtigen Fall.

### B-788 (`d7b7546b`)

- **Gemessen:** Der Effekt lief im Fall AK 3 viermal. Die neue Frage, `cursor` und `busy = false` kommen in **einem**
  Render; `busy` wechselt allein nur auf `true` (und findet dort bewusst kein Ziel).
- **Entscheidung:** `cursor` raus (nicht gelesen), `action.busy` bleibt (gelesen, Sicherung gegen ein künftiges `await`
  nach `setItem`), beide Kommentare richtiggestellt.
- **Mutanten:** busy gestrichen → grün (erwartet grün), cursor zurück → grün (erwartet grün), Absicht ohne Ziel verbraucht
  → rot an AK 3 (erwartet rot).

### Verifikation

- Vitest voll: 1327/1327. `tsc -b`: sauber, nach `node scripts/gen-contract.mjs` (im frischen Worktree fehlte
  `src/lib/contract.ts`; ohne ihn meldet `tsc` 3 Fehler in fremden Dateien).
- Review `frontend-reviewer`: keine 🔴/🟡. Zwei 🟢 zum Kommentar eingearbeitet (ohne Zählung). Zähler Sprint B1: 0.

## Runde 2 — Re-Review / Abnahme Sprint B1

- **Sohn:** Maschinengang mit E2E `full-flow`, `test-durchgefallen-ueben`, `erfolge-woche-frist` und `karten-umdrehen` auf
  :5207/:5179 gegen eine Wegwerf-DB, nach der letzten Frontend-Änderung gestartet: 4 passed. Klausur und Erfolge laufen
  wie vorher. **Abgenommen.**
- **Vater, Creator:** Regressionszeugen. Kein Pfad im Diff, Vitest voll grün.
- Hinweis: Der Server des Gangs baute schon die ungecommittete Entity/Migration von B-659 mit (Arbeitsbaum). Für den
  Sohn-Pfad ohne Wirkung.

## Retrospektive Sprint B1

**Nachschau:** Sprint B1 von Lauf B 2026-11-22 (B-602 `f79b8fed`, B-605 `04c72b89`, B-603 `20ca1ce2`, B-790 `761af289`)
durch `frontend-reviewer`, je Story mit benanntem Prüfpunkt im `## Verlauf`. Kein Befund, `nachgeschaut: 2026-11-22`
an allen vier gesetzt. Index: Nachgeschaut 536 von 568. Sprint 114 von Lauf A (B-789) gehört Lauf A.

**Was die eigenen Tore durchgelassen haben:** B-787 hat seine Meldung verloren, weil der volle Lauf durch `grep` ging.
Die Ursache lag damit eine Nacht lang falsch (Vermutung `setPlanId`).

**Vorschlag (nicht gelandet, Freigabe 3):** Vitest schreibt bei jedem vollen Lauf zusätzlich einen JSON-Bericht in eine
Datei (`reporters: ['default', ['json', { outputFile }]]` in der Vitest-Konfiguration). Dann trägt jedes seltene Rot seine
Meldung, egal wie die Ausgabe gefiltert wurde. Kosten: eine gitignorte Datei je Lauf. Das wäre ein Werkzeug, kein Tor.

**Bewusst ungedeckt:** Die Fehlerklasse „Titel neben gleichnamigem Knopf“. Die Suche des Reviewers fand keine zweite
Stelle; ein Tor dafür wäre nicht treffsicher.
