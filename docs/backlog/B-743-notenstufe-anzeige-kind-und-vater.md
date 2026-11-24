---
tags: [typ/story, status/in-arbeit, bereich/gamification, bereich/frontend, rolle/student, rolle/supervisor]
aliases: [Notenstufe anzeigen, Stand-Text]
status: in-arbeit
prio: P2
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-682
grund: ""
ersetzt_durch: []
wartet_auf: "Nutzer-Entscheidung AK 2: Stufenwort nach Abstieg auf 2/3 (a: wie gebaut, b: weglassen unter Bestwert)"
---

# B-743 · Die Notenstufe zeigen: dem Kind mit Zielsatz, dem Vater als Zahl

Die Oberfläche zu [B-742](B-742-notenstufe-lesen-student-und-supervisor.md): auf der Fachkarte des Kindes steht für die Stufen 1 bis 3
„Einser-/Zweier-/Dreier-Stand", für 4 bis 6 nur das nächste Ziel („noch N Wörter sicher"). Der Vater sieht die Zahl je Fach im
Kind-Hub. Abgespalten aus [B-682](B-682-notenstufe-je-fach-statt-liga.md); die Textregel für den Abstieg aus
[B-683](B-683-notenstufe-an-der-echten-klausur-eichen.md) (Entscheidung 3) ist hier mit umgesetzt, weil es dieselbe Anzeige ist.

*Entscheidungen stellvertretend im Vorbereitungslauf 2026-11-20 (Sonnet) gefällt, unbestätigt; Herkunft B-682 und B-683.*

## User Story

Als *Schüler* möchte ich je Fach sehen, wie weit ich bin und was mir bis zur nächsten Stufe fehlt, ohne dass mir ein schlechter
Stand als Etikett angehängt wird; als *Vater* möchte ich die Zahl sehen.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`; Pfade relativ zu `frontend/src/`. Die Dateien `sohn/*` tragen fremde, ungecommittete Arbeit
(LearningDad); beim Bau vorher klären.

- Fachkarte des Kindes: `FachKarte` in `sohn/SohnHome.tsx:158` (Fachmotiv `sohn/fachMotiv.ts`); Pfadansicht `sohn/pfad.ts`.
- Kind-Hub des Vaters: `vater/VaterKind.tsx` (Bereichsliste mit „Klassenarbeiten", Zeile `:146`).
- Typen kommen aus `lib/contract.ts` über `lib/types.ts` (Muster `export type X = S["X"]`, `types.ts:493`); ein Aufruf für
  Stufen fehlt in `lib/api.ts` (nicht gefunden).
- Schreib-Primitive: `useAction` und `StatusBanner` für Mutationen; hier nur Lesen, also ein Ladezustand und ein Fehlerzustand
  (`frontend/CLAUDE.md`).
- Textregeln „Einser-Stand", „Zielsatz": nicht gefunden.

## Entscheidungen

1. **Am Kind heißen nur die Stufen 1 bis 3 „Einser-", „Zweier-" und „Dreier-Stand"; darunter steht ausschließlich das nächste Ziel
   („noch N Wörter sicher bis zur nächsten Stufe")** (B-682 Entscheidung 3). Kosten: Das Kind ahnt die Stufe unter 3 nur aus dem
   Zielsatz; der Vater sieht die Zahl. Schließt aus: ein „Fünfer-Schüler" am Kind.
2. **Beim Absteigen steht dieselbe Anzeige: neue Stufe und Weg zurück, nie das Wort „abgestiegen" oder eine schlechtere
   Notenbezeichnung** (B-683 Entscheidung 3). Kosten: Das Kind sieht den Rückschritt nur an der Zahl. Schließt aus: einen Tadel im
   Wortlaut.
3. **Kein Fach ohne Maß zeigt eine Stufe** (B-682 Entscheidung 1). Kosten: Mathe und Grammatik bleiben ohne Anzeige.
4. **Nie im Vergleich mit anderen Kindern** (B-682 Entscheidung 6). Kosten: keine Stufe in der Familienübersicht.

## Akzeptanzkriterien

1. Für Stufe 1, 2 und 3 steht der Stand-Text, für 4, 5 und 6 nur der Zielsatz. *Rot, wenn* ein Text für Stufe 4 bis 6 „Vierer",
   „Fünfer" oder „Sechser" enthält.
2. Der Text trägt weder „abgestiegen" noch eine Notenbezeichnung der neuen Stufe, wenn die Stufe zwischen zwei Abrufen sinkt und
   nennt den nächsten Schritt. *Rot, wenn* eines der Wörter erscheint (Vitest über die reine Text-Funktion, Zustand vorher und
   nachher übergeben).
3. Ein Fach, das der Server nicht liefert, hat keine Stufenzeile auf der Fachkarte. *Rot, wenn* die Karte eine leere oder
   erfundene Stufe zeigt.
4. Der Vater sieht je Fach die Zahl und den Bestwert im Kind-Hub. *Rot, wenn* die Zahl dort fehlt oder nur Stand-Text trägt.
5. Ein Ladefehler beim Lesen der Stufen blockiert die Fachkarte nicht (die Karte erscheint ohne Stufenzeile). *Rot, wenn* ein
   Fehler der Stufen-Abfrage die ganze Heimseite leert.
6. Keine Seite zeigt Stufen zweier Kinder nebeneinander. *Absichtserklärung.* *Rot, wenn* eine Vergleichsansicht entsteht.

## Schätzung

**Größe: M**, wie der Batch-Pfad im `MediaSelector` (B-03): zwei Anzeigeorte, eine reine Textfunktion, ein Aufruf, drei Teststufen.

- **`wo: frontend`**: die Endpunkte kommen aus B-742; diese Story fasst kein C# an.
- **`migration: nein`**.
- **`vertragsbruch: nein`**: nur neue Typen aus der erzeugten `contract.ts`.

### Risiken

- **Die Textregel ist eine Produktregel, die im Code leicht rutscht.** Zielsatz und Stand-Text gehören in eine reine, getestete
  Funktion (Muster `fachMotiv.ts` mit `fachMotiv.test.ts`), nicht in JSX, sonst bricht die Stufe-4-bis-6-Regel unbemerkt.
- **Ohne Stufenzeile sieht eine Fachkarte anders aus als mit.** Layout am Handy prüfen; das Sohn-Web hat eine Dark-Arcade-Richtung
  (frontend-Normen).
- **Fremde Arbeit in `sohn/*`:** LearningDad liegt ungecommittet in genau diesen Dateien; erst mergen oder abstimmen.
- **Sinnliches Urteil** („fühlt es sich freundlich an") bleibt beim Nutzer und ist ein benannter Rest, kein Gate.

### Angriffsplan

1. `api.subjectLevels()` (Kind) und `api.childSubjectLevels(childId)` (Vater) in `lib/api.ts`, Typ in `lib/types.ts`.
2. Reine Funktion `notenstufeText(level, wordsToNext)` in `sohn/notenstufe.ts`.
3. Stufenzeile in `FachKarte`; Vater-Zeile im Kind-Hub.
4. Vitest, Playwright, `npm run build`, Lint.

### Testweg

- Vitest `sohn/notenstufe.test.ts` (neu, AK 1, 2): je Stufe 1 bis 6 ein Fall, dazu Absteigen; je Fall die Änderung benennen, die ihn
  rot macht (nie ein Fall, der den Ausgangszustand prüft).
- Vitest `sohn/SohnHome.test.tsx` (vorhanden, AK 3, 5) und `vater/VaterKind.test.tsx` (vorhanden, AK 4).
- E2E-Spec `frontend/e2e/notenstufe.spec.ts` (neu): Sohn mit Seed-Vokabeln sieht Zielsatz, Vater sieht Zahl; fährt als Rollengang
  für B-741 bis B-743 mit.
- Vor der Abnahme: Agent `frontend-reviewer`.

## Verlauf

- **2026-11-20** — angelegt aus [B-682](B-682-notenstufe-je-fach-statt-liga.md) (geteilt) und B-683 (Textregel);
  `geschaetzt`: M, frontend, keine Migration, kein Vertragsbruch (Vorbereitungslauf, Sonnet).
- **2026-11-24** — `in-arbeit` und gebaut (Nachtlauf G, Sprint 127): reine Textfunktion `sohn/notenstufe.ts`
  (`notenstufeText(t, level, wordsToNext)`, Stand-Text nur für 1–3 über eine feste Schlüsselliste, darunter nur Zielsatz;
  `0` → „nächste Stufe geschafft – sie zählt ab morgen“), Stufenzeile in `FachKarte` (`SohnHome.tsx`, eigener
  `useAsync`, dessen Fehler still bleibt), `vater/NotenstufenSection.tsx` im Reiter „Schule“ des Kind-Hubs (Zahl, Bestwert,
  Wörter bis zur nächsten Stufe; `ListenAusfall`), `api.mySubjectLevels`/`api.childSubjectLevels`, Typ `SubjectLevel`,
  i18n de/en, CSS `.fach-stufe`. Die Sohn-Dateien trugen in diesem Worktree keine fremde Arbeit (Stand `a782b4b2` sauber).
- **2026-11-24** — AK → Test: AK 1 → `sohn/notenstufe.test.ts` (F1: Grenze bei 4 statt 3 → rot) · Zielsatz/0 → dieselbe
  Datei (F2 → rot) · AK 2 → dieselbe Datei, **als Absichtserklärung beschriftet** (die Funktion kennt die vorige Stufe
  nicht) · AK 3 → `sohn/fachStufe.test.tsx` (F3: erste Stufe an jede Karte → rot) · AK 4 → `e2e/notenstufe.spec.ts`
  (E1: Zahl leer → rot) · AK 5 → `sohn/fachStufe.test.tsx` (F4: Stufenfehler leert die Seite → rot) · AK 6
  Absichtserklärung (keine Vergleichsansicht gebaut). Tabelle: B-741. Vitest 128 Dateien / 1396 Tests grün, `npm run
  build` grün, E2E `notenstufe` + Zeugen `belohnung-ueber-faecher`, `bestaetigung-ueberlebt`, `pfad` 6 passed.
- **2026-11-24** — Review `frontend-reviewer` (Agent, nur gelesen): keine 🔴. 🟡 Zuordnung über den Fachnamen (Karte =
  Fach des Plans, Stufe = Fach der Reihe) – serverseitig behoben, siehe B-741. 🟡 **AK 2 gegen die Umsetzung**: wörtlich
  verbietet AK 2 „eine Notenbezeichnung der neuen Stufe“ nach einem Abstieg; gebaut ist Entscheidung 1 (Stufe 2/3 tragen
  ihren Namen immer) mit Entscheidung 2 gelesen als „keine *schlechtere* Bezeichnung als Dreier“. Ein Abstieg 1 → 2 zeigt
  also „Zweier-Stand“. Das geben die `## Entscheidungen` nicht eindeutig her → **angehalten** (Freigabe 8), nicht selbst
  entschieden. Varianten für den Nutzer: (a) AK 2 auf „nie ‚abgestiegen‘, unter 3 kein Stufenwort“ umschreiben (gebaut);
  (b) die Funktion nimmt `bestLevel` und lässt das Stufenwort weg, solange `level > bestLevel`. 🟢 behoben ohne Zählung:
  AK-2-Fälle als Absichtserklärung beschriftet (ungenutzte Spalte „vorher“ entfernt), englische Stufennamen „Grade-N
  standing“. 🟢 offen: AK 4 nur per E2E, kein Vitest.
- **2026-11-24** — Rollengang maschinell (`e2e/notenstufe.spec.ts`): Sohn liest auf Stufe 6 „noch 1 Wort sicher bis zur
  nächsten Stufe“, kein Stufenwort auf der Seite, Mathe ohne Zeile; Vater liest im Kind-Hub 6 / 6 / „noch 1 Wort“. Nicht
  gelaufen: ein Kind auf Stufe 1–3 im Browser (braucht Box 5, also Wochen) – die Texte trägt der Vitest. Offen für einen
  Menschen: das sinnliche Urteil („fühlt sich der Zielsatz freundlich an“, Zeile am Handy). Code in Commit `4aae8d36`;
  bleibt `in-arbeit` mit `wartet_auf` bis zur Entscheidung über AK 2.
