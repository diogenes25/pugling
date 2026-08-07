---
tags: [typ/story, status/abgenommen, bereich/frontend]
aliases: [Kommentar gegen die Bedingung]
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Code-Review 2026-08-05 der Commits 4469662…b20600f (Befund 6)
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-08-07"
---

# B-112 · Ein Kommentar begründet das Gegenteil der Bedingung unter ihm

Im Wortpaar-Block des Übungs-Bearbeiten-Dialogs steht ein Kommentar, der genau die Prüfung ausschließt,
die die Zeile darunter durchführt. Beides ist einzeln richtig gedacht — zusammen gelesen widerspricht es
sich, und wer die Regel hier nachschlägt, lernt sie falsch.

## User Story

Als Entwickler möchte ich, dass der Kommentar über einer Bedingung dieselbe Bedingung begründet, damit ich
beim nächsten Lesen nicht die halbe Regel übernehme.

## Ist-Stand am Code

- `frontend/src/vater/ExerciseEditModal.tsx:353-355`:

  ```tsx
  {/* Auf `loading` prüfen, nicht auf „noch keine Daten": nach einem Fehler bleibt `data` null, und
      der Spinner stünde neben der Fehlermeldung für immer. */}
  {items.loading && items.data === null ? <div className="loading">Lade Wortpaare…</div> : items.data && (
  ```

  Der Kommentar sagt „**nicht** auf ‚noch keine Daten' prüfen"; die Zeile darunter prüft `items.data === null`,
  also genau das. Beide Teile der Bedingung sind nötig, der Kommentar nennt nur einen und schließt den
  anderen ausdrücklich aus.

- Die Bedingung selbst ist **richtig** und ist die dokumentierte Projektregel: `frontend/CLAUDE.md`
  („Wiederkehrende Falle bei Listen mit aufklappbaren Zeilen") schreibt genau
  `loading && data === null` vor — `useAsync` behält `data` über ein `reload`, setzt aber `loading` neu,
  und `{loading ? … : rows}` hängt darum bei jeder Änderung alle Zeilen aus.
- Es ist also kein Verhaltensfehler: der Code tut das Richtige, nur seine Begründung ist halb.

## Die echte Lücke

Der Kommentar erklärt, warum `loading` **mitgeprüft** werden muss (Fehlerfall: `data` bleibt null, der
Spinner bliebe ewig), und formuliert daraus fälschlich ein Verbot des zweiten Teils. Die zweite Hälfte der
Begründung fehlt: `data === null` verhindert, dass ein `reload` die schon sichtbare Tabelle austauscht.

Das ist genau die Sorte Kommentar, gegen die die Konvention im Root steht — er soll das *Warum* tragen,
lesbar für Mensch **und** Modell ohne Vorwissen. Hier trägt er ein halbes Warum und ein falsches Verbot.
Und der `//`-Kommentar ist überdies deutsch, während die Konvention seit B-08 für Code-Doku ausnahmslos
Englisch verlangt.

## Offene Punkte

1. ~~Auch die übrigen Fundstellen desselben Muster-Kommentars mitnehmen?~~ → entschieden, siehe 2.

## Entscheidungen

1. **Der Kommentar wird umgeschrieben, nicht gelöscht.** Die Stelle ist eine bekannte Falle mit eigenem
   Absatz in `frontend/CLAUDE.md`; ein Verweis darauf plus die *beiden* Gründe ist mehr wert als ein
   stiller Zweizeiler. **Kosten:** zwei Zeilen Kommentar statt zwei Zeilen Kommentar — keine.
2. **Nur diese Fundstelle**, und sie wird auf Englisch geschrieben. Ein Rundumschlag über alle Stellen mit
   `loading && data === null` wäre eine eigene Story (und `wo: doku`-Arbeit); B-08 hat die Backend-Doku
   übersetzt, das Frontend nicht. **Kosten:** die deutschen `//`-Kommentare des Frontends bleiben
   insgesamt ungetastet — die Inkonsistenz wird hier nicht gelöst, nur nicht vergrößert.

## Akzeptanzkriterien

1. Der Kommentar an `ExerciseEditModal.tsx:353` benennt **beide** Gründe (Spinner verschwindet im
   Fehlerfall; die sichtbare Tabelle bleibt über ein `reload` stehen) und widerspricht der Bedingung nicht.
2. Er verweist auf die Projektregel in `frontend/CLAUDE.md`, statt sie nachzuerzählen.
3. Kein Verhalten ändert sich — `npm test` und `npm run build` bleiben so grün wie vorher.

## Schätzung

**Größe: XS** — ein Kommentar. Der Wert liegt darin, dass die nächste Lesestelle die Regel ganz mitnimmt.

**Risiken:** keine. `art: Aufräumen` — kein Verhalten ändert sich, also ist „alles so grün wie vorher"
die vollständige Abnahme.

**Angriffsplan:** Kommentar ersetzen. Kein Backend-Anteil, keine Teständerung.

**Testweg:** `npm run build` (Typecheck) und `npm test` unverändert grün; kein neuer Test — ein Test auf
einen Kommentar wäre Unsinn, und die Bedingung selbst ist schon durch das Verhalten der Seite gedeckt.

## Verlauf

- **2026-08-05** — angelegt aus dem Code-Review der autonomen Bau-Runde (Befund 6), direkt mit belegtem
  Ist-Stand.
- **2026-08-05** — im Autonomen Modus ausformuliert, gegrillt und geschätzt (`art: Aufräumen`, damit
  autonom grillbar). Bei der Recherche bestätigt: die Bedingung ist die dokumentierte Regel aus
  `frontend/CLAUDE.md`, der Code also richtig — die Story schrumpft damit von „Bedingung prüfen" auf
  „Begründung reparieren". **Bewusst nicht in den Sprint aufgenommen** (siehe
  `docs/pm-sitzung-2026-08-05.md`): sie dient dem Sprint-Ziel nicht.
- **2026-08-05** — gebaut (Nachtlauf 2, Sprint 1 „Rollen-/Bezeichner-Konsistenz"): Kommentar in
  `ExerciseEditModal.tsx:353` auf Englisch umgeschrieben, benennt beide Gründe und verweist auf
  `frontend/CLAUDE.md`. `npm run build` clean, `npm test -- --run` → **153/153 grün** (unverändert).
- **2026-08-05** — `frontend-reviewer` fand einen **echten Defekt im eigenen Increment**: der erste
  Kommentar-Entwurf hatte die beiden Begründungshälften vertauscht — er schrieb „`loading` allein ließe
  den Spinner ewig neben der Fehlermeldung stehen", tatsächlich ist es umgekehrt: `useAsync.ts:36` setzt
  `loading` im `finally`-Block **immer** auf `false`, auch nach einem Fehler — „Spinner für immer" kann
  also nur passieren, wenn **`data === null`** allein geprüft wird (der Fehlerpfad füllt `data` nie).
  `loading` allein wäre umgekehrt das Reload-Problem: `data` bleibt über einen `reload` erhalten, `loading`
  wird neu gesetzt, also verdeckte „nur `loading`" die schon sichtbare Tabelle während des Nachladens.
  Nachgerechnet gegen `useAsync.ts:25-39` — der Befund ist korrekt. Kommentar entsprechend korrigiert,
  `npm run build` erneut clean. **Diese Entgleitung ist der Grund, warum der Nachtlauf nach diesem Sprint
  endet** (Freigabe 3, `docs/nachtlauf.md`): ein Review-Fund im eigenen Increment vor der Abnahme.
- **2026-08-07** — Nachschau (Nachtlauf): geprüft, ob der Kommentar in `ExerciseEditModal.tsx` weiterhin
  beide Gründe (Fehlerfall + Reload) korrekt nennt — hält (`ExerciseEditModal.tsx:353-356`). Kein Fund.
