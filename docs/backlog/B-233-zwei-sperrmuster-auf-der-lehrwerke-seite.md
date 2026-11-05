---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [Sperrregel Lehrwerke uneinheitlich, SeriesForm UnitForm Pflichtfeld]
status: abgenommen
prio: P3
art: Frage
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-17-nacht.md (Nachschau zu Sprint 2, Nuancen 4 und 5)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-233 · Eine Seite, zwei Sperrmuster: Soll „Pflichtfeld leer" überall gleich aussehen?

Das Sprint-Ziel von Sprint 2 (2026-10-17) lautete: „ein Knopf, der nichts tun kann, ist gesperrt und sagt am
Feld, warum." Das gilt auf `frontend/src/vater/VaterLehrwerke.tsx` für „Reihe anlegen", „Verlag anlegen" und
den OK-Knopf im Verlags-Vokabular (`PublisherAdmin.tsx`) – alle drei mit `disabled` bei leerem Namen.
**Nicht** dafür gelten die Speichern-Knöpfe von `SeriesForm` und `UnitForm`: sie bleiben bei leerem Pflichtfeld
klickbar und melden lokal („Der Name der Reihe fehlt."), ihre Labels tragen kein „(Pflicht)". Das ist B-212s
Entscheidung („Eingabeprüfung verlässt die Zeile nie") – und genau dadurch bleibt der Zustand „zwei Meldungen,
zwei Plätze" erreichbar: rotes „Der Name der Reihe fehlt." unter Reihe A neben grünem „Unit hinzugefügt." am
Seitenplatz. Die Abnahme-Behauptung „nach jeder Handlung genau EINE Rückmeldung an EINEM Platz" war darum zu
weit.

Dazu ein zweiter Riss derselben Seite: `PublisherAdmin.tsx` formuliert als Regel „`aria-disabled` für die
fachliche Sperre, echtes `disabled` nur für ‚läuft gerade' (B-160)"; B-225 setzt für die fachliche Sperre
„Name leer" echtes `disabled`. Barrierefrei vertretbar, weil der Grund am Label steht – aber die Regel ist
seitdem nicht mehr seitenweit, und `frontend/CLAUDE.md` kennt nur `disabled={busy}` als Konvention.

Drei mögliche Antworten, alle Produktentscheidung: (a) `SeriesForm`/`UnitForm` ziehen nach (Sperre + „(Pflicht)",
lokale Eingabeprüfung entfällt), (b) B-212 bleibt und die Behauptung wird enger formuliert, (c) eine
Hausregel in `frontend/CLAUDE.md`, welches Sperrmuster für Pflichtfelder gilt. Darum eine `Frage`.

## User Story

Als **Creator** möchte ich auf der Lehrwerke-Seite ein einziges Sperrmuster für Pflichtfelder sehen: ein Knopf, der nichts
tun kann, ist gesperrt, und das Label sagt warum.

## Ist-Stand am Code (vor dem Bau, Stand `37e6f6e`)

- `frontend/src/vater/VaterLehrwerke.tsx:447` (`SeriesForm`) und `:740` (`UnitForm`): Speichern blieb bei leerem Pflichtfeld klickbar und
  meldete lokal „Der Name der Reihe fehlt." bzw. „Die Bezeichnung fehlt."; die Labels trugen kein „(Pflicht)".
- „Reihe anlegen" (`:991`) und die Verlags-Knöpfe sperrten schon (B-225).

## Entscheidungen

1. **(Nutzer, 2026-10-23)** Antwort (a) als Hausregel: Sperre, „(Pflicht)" am Label, keine lokale Meldung; Regel in
   `frontend/CLAUDE.md`.
2. **(Nutzer, über B-231)** Die Sperre ist echtes `disabled`, nicht `aria-disabled`.
3. **(Im Auftrag des Nutzers, beim Bau)** Ein stiller Riegel bleibt im `submit`, und zwar **vor** `onMeldung(null)`: ein Versuch,
   der nicht stattfindet, räumt den Platz der Seite nicht.
4. **(Im Auftrag des Nutzers, beim Bau)** Nur die Lehrwerke-Seite; die übrigen zehn Formulare mit demselben Muster →
   [B-380](B-380-pflichtfeld-sperre-in-den-uebrigen-formularen.md).

## Akzeptanzkriterien

1. `SeriesForm` und `UnitForm` sperren Speichern bei leerem (auch nur aus Leerzeichen bestehendem) Pflichtfeld; das Label
   trägt „(Pflicht)".
2. Ein leeres Absenden meldet nichts und räumt den Meldungsplatz nicht.
3. Die Hausregel steht in `frontend/CLAUDE.md`, ohne dass die Datei wächst.

## Schätzung

`S`, `wo: frontend`. Testweg: Vitest je Formular mit Mutanten; die vier E2E mit exaktem Label ziehen nach,
`bestaetigung-ueberlebt.spec.ts` Schritt 2 fährt B-197 jetzt über „Nichts geändert.".

## Verlauf

- **2026-10-18** — angelegt aus der Nachschau zu Sprint 2 des Nachtlaufs 2026-10-17 (Nuancen 4 und 5 des
  `frontend-reviewer`). Kein Defekt: beide Zustände sind entschieden, nur nicht miteinander.
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde G-2 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): Antwort **(a)** als **Hausregel**: ein Speichern-Knopf ist bei leerem Pflichtfeld **gesperrt**, das Label trägt „(Pflicht)“, und die lokale Eingabeprüfung („Der Name der Reihe fehlt.“) entfällt. `SeriesForm`/`UnitForm` ziehen nach; die Regel gehört beim Bau in `frontend/CLAUDE.md`. Offen bleibt nur Handwerk: welche weiteren Formulare abweichen (Sweep) und ob die Sperre `disabled` oder `aria-disabled` ist (hängt an B-231).
- **2026-10-23** — **Freigabe des Nutzers (Autonomer Modus, Opt-in je Vorhaben, README):** ein Nachtlauf darf diese Story auf Grundlage der getroffenen Entscheidung selbst ausformulieren, schätzen und bauen (Paket NP-14); trifft er dabei auf eine Produktfrage, die die Entscheidung nicht hergibt, hält er an und notiert sie. Rollengang und Reviewer bleiben Pflicht. Erteilt in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)).
- **2026-11-05** — ausformuliert, geschätzt und gebaut in Sprint 1 der Sitzung 2026-11-05 ([Protokoll](../pm-sitzung-2026-11-05.md)). AK 1 → `SeriesForm.test.tsx:143` und `VaterLehrwerke.test.tsx:243`, Mutanten „`|| !form.name.trim()` bzw. `|| !form.label.trim()` am Knopf weg" → je rot (1 von 34); „(Pflicht)" trägt die exakte Label-Abfrage derselben Fälle. AK 2 → `SeriesForm.test.tsx:152` und `VaterLehrwerke.test.tsx:254`, Mutanten „Riegel hinter `onMeldung(null)`" → je rot (1 von 34). AK 3: `frontend/CLAUDE.md` 10347 → 10333 B (Platzhalter-Absatz verdichtet, Begründung liegt in `useAsync.ts`). E2E: vier Specs auf das neue Label nachgezogen, `bestaetigung-ueberlebt.spec.ts` Schritt 2 fährt B-197 über „Nichts geändert.".
- **2026-11-05** — **abgenommen.** Vitest 648/648, E2E 110/110 (darunter die vier nachgezogenen Creator-Specs = Rollengang). Review `frontend-reviewer`: B-233-Fälle je mit Mutant bestätigt; 🟢 4 (Hausregel-Satz vermischte B-231 und B-233) behoben, ohne Zählung. Commit: siehe nächster Eintrag.
- **2026-11-05** — Commit `c6876b4`.
