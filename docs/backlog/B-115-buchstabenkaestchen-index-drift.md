---
tags: [typ/story, status/in-arbeit, bereich/frontend, rolle/student]
aliases: [LetterBoxes Index-Drift]
status: in-arbeit
prio: P2
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Code-Review 2026-08-05 der Commits 4469662…b20600f (Befund 2)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-66]
wartet_auf: frontend-reviewer
---

# B-115 · Übersprang das Kind ein Buchstabenkästchen, rutschten alle folgenden Zeichen

B-66 hat die Trennzeichen-Maske eingeführt: Leer- und Satzzeichen stehen fest und sind nicht tippbar. Der
gemeldete Wert wurde dabei aber mit `join("")` über die Rohwerte zusammengesetzt — eine noch leere tippbare
Stelle trug nichts bei, ein festes Maskenzeichen aber immer. Wer eine Stelle übersprang, verschob damit
alle folgenden Zeichen.

## User Story

Als Sohn möchte ich, dass jeder Buchstabe in dem Kästchen landet, in das ich ihn getippt habe — auch wenn
ich eines überspringe.

## Ist-Stand am Code

Zum Zeitpunkt des Funds (Stand `b20600f`, B-66 war `abgenommen`):

- `LetterBoxes.compose` setzte den Wert per `next.join("")` zusammen. Für eine Maske wie `_____ ___` und
  ein einzelnes getipptes Zeichen an Stelle 0 entstand `"g "` statt `"g    ␣..."` — der Wert war kürzer als
  `length`.
- Damit bezeichnete `value[i]` nicht mehr das Kästchen `i`: das Kästchen unter dem Cursor zeigte plötzlich
  das Maskenzeichen der übernächsten Stelle, und wegen `maxLength={1}` sah es dabei sogar besetzt aus.

## Die echte Lücke

B-66s Maske war richtig, ihr **Rückweg** nicht: die Komponente konnte den eigenen gemeldeten Wert nicht
mehr stellengetreu zurücklesen. Ein Test hätte das gefangen, aber der bestehende prüfte nur den einfachen
Fall ohne übersprungene Stelle — und der Rollengang, der es sofort gezeigt hätte, fand nicht statt. B-66
lief zudem als einzige der vierzehn Stories jener Runde **ohne jeden Reviewer**.

## Entscheidungen

1. **Der gemeldete Wert ist immer genau `length` Zeichen lang und stellengetreu**; eine leere tippbare
   Stelle trägt ein Leerzeichen. Gefahrlos, weil `StageMechanics.LetterBoxPattern` jedes Zeichen, das kein
   Buchstabe und keine Ziffer ist, für fest erklärt — an einer tippbaren Stelle kann ein Leerzeichen also
   nie zur Lösung gehören — und `StageMechanics.Normalize` serverseitig ohnehin trimmt und Folgeleerzeichen
   zusammenzieht. **Kosten:** beim Zurücklesen muss ein Leerzeichen an tippbarer Stelle ausdrücklich wieder
   als „leer" gezeigt werden, sonst stünde es als sichtbares Zeichen im Kästchen.

## Akzeptanzkriterien

1. Das feste Maskenzeichen bleibt an seiner Stelle im gemeldeten Wert.
2. Ein Kästchen hinter einer übersprungenen Stelle schreibt an seinen eigenen Index.
3. Mit dem gemeldeten Wert neu gerendert bleibt eine leere tippbare Stelle leer und beschreibbar.

## Schätzung

**Größe: XS**, `wo: frontend`, keine Migration, kein Vertragsbruch. Testweg:
`frontend/src/components/LetterBoxes.test.tsx` — ein Regressionsfall mit zweiteiliger Maske (`_____ ___`).

## Verlauf

- **2026-08-05** — gefunden im Code-Review der autonomen Bau-Runde (Befund 2) und **sofort behoben**,
  Commit `ec3ba19`. Neuer Regressionstest mit zweiteiliger Maske: festes Leerzeichen bleibt an Index 5,
  das zweite Kästchen bleibt beschreibbar, das 7. Kästchen schreibt an Stelle 6. **149/149** Frontend.
- **2026-08-05** — **als eigene Story nachgetragen** für die Messung (README → „Die eine Zahl über die
  Wirkung"); vorher stand die Entgleitung nur im `## Verlauf` von B-66. `entgangen_bei: [B-66]`.
- **2026-08-05** — bleibt auf `in-arbeit`: `frontend-reviewer` ist dreimal an einem serverseitigen `529`
  gescheitert. Alles andere ist belegt.
