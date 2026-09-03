---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/student]
aliases: [Bestätigung reist durch die Arcade, SavedNotice ohne Räumen]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-09-03 zu B-50 (Nachtlauf)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-50]
wartet_auf: ""
---

# B-209 · „Gespeichert" bleibt über jedem folgenden Bildschirm der Arcade stehen

## User Story

Als **Sohn** möchte ich, dass eine Bestätigung zu dem Bildschirm gehört, auf dem ich etwas gespeichert
habe — und nicht über Shop, Weg und Konto weiter mitreist, als hätte ich dort gerade etwas getan.

## Ist-Stand am Code

`frontend/src/components/SavedNotice.tsx:23-33`: Der Effekt setzt `message`, wenn `location.state.saved`
ein String ist, und **kehrt sonst früh zurück, ohne zu räumen**. Der Baustein sitzt in der Shell **über**
den `<Routes>` (`SohnApp.tsx:114`), bleibt also die ganze Sitzung montiert.

Der Weg, auf dem es auftritt: Kind speichert seine Interessen → Sprung nach `/sohn` (Banner richtig) →
tippt auf „Shop", „Weg" oder „Konto". Die Meldung „Gespeichert – deine Übungen passen sich an." steht
danach über **jedem** Bildschirm bis zum Abmelden.

**Dieselbe Fehlerfamilie, die dieses Repo schon dreimal bezahlt hat:** eine Bedingung zieht zwei
Situationen zusammen — „diese Navigation bringt keine Meldung mit" und „es ist nichts zu tun".

**Warum es keiner Prüfung auffiel:** `frontend/src/components/` enthält **keine** `SavedNotice.test.tsx`,
und `e2e/interessen.spec.ts` prüft die Meldung nur auf `/sohn` selbst — also genau dort, wo sie richtig
ist.

## Offene Punkte

1. Räumen beim Navigieren ohne `saved`, oder die Meldung nach einer Frist verfallen lassen? *Empfehlung:
   räumen* — eine Frist wäre eine zweite Zeitachse neben der Navigation und im Test nur mit Warten zu
   fassen.
2. Gehört der Fall an den Baustein oder in einen E2E? *Empfehlung: beides* — der Baustein trägt die Regel
   (heute ohne jeden Test), der E2E den Weg, auf dem sie auffiel.

## Verlauf

- **2026-09-03** — aus der **Nachschau** zu B-50 (Nachtlauf). Im Abnahme-Diff `827b220` entstanden, darum
  `entgangen_bei: [B-50]`. Der Ist-Stand ist am Code nachgesehen, nicht vermutet.
