---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [Sperrregel Lehrwerke uneinheitlich, SeriesForm UnitForm Pflichtfeld]
status: idee
prio: P3
art: Frage
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-10-17-nacht.md (Nachschau zu Sprint 2, Nuancen 4 und 5)
unverifiziert: true
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

## Verlauf

- **2026-10-18** — angelegt aus der Nachschau zu Sprint 2 des Nachtlaufs 2026-10-17 (Nuancen 4 und 5 des
  `frontend-reviewer`). Kein Defekt: beide Zustände sind entschieden, nur nicht miteinander.
