---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/creator]
aliases: [PublisherAdmin StatusBanner, zweiter Meldungsplatz Lehrwerke-Seite]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-10-17-nacht.md (Nachschau zu Sprint 2026-09-05)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-212]
wartet_auf: ""
---

# B-224 · Der Verlags-Baustein trägt auf der Lehrwerke-Seite einen zweiten Meldungsplatz

B-212 versprach in seiner User Story, dass auf der **Lehrwerke-Seite** nie gleichzeitig eine Fehler- und
eine Erfolgsmeldung stehen. Gebaut – und so im Ist-Stand und in `VaterLehrwerke.tsx:401` auch beschrieben –
wurde das für die **Datei**. Dieselbe Seite rendert aber `PublisherAdmin` mit eigenem Platz.

## User Story

Als **Creator** möchte ich auf der Lehrwerke-Seite auch nach einer Verlags-Aktion genau **eine**
Rückmeldung lesen, die zu dem gehört, was ich zuletzt getan habe – statt einer roten Meldung am Verlag
unter einer grünen Meldung an der Reihe.

## Ist-Stand am Code

Nachgesehen am 2026-10-17 (Nachschau, Reviewer-Agent; Zeilen von mir gegengelesen).

- `frontend/src/vater/PublisherAdmin.tsx:80`: `<StatusBanner message={action.message} />` – eigener Platz
  für Server-Erfolg („Gespeichert." `:56`, „Verlag gelöscht." `:67`) **und** Server-Fehler (409 bei fremd
  genutztem Verlag).
- `frontend/src/vater/VaterLehrwerke.tsx:230`: der Baustein wird mit `onChanged` eingehängt; `onChanged`
  räumt den Seitenplatz nicht, und kein Aufrufer kann den Platz in `PublisherAdmin` räumen – `nachObenMelden`
  ist dateiprivat in `VaterLehrwerke.tsx`.
- Weg zum Widerspruch: Verlags-Vokabular öffnen → Verlag löschen scheitert (rot dort) → Reihe löschen
  gelingt (grün im Seitenplatz). Oder zwei Grüns: „Reihe X gelöscht." oben, „Gespeichert." unten.
- Mildernd: `PublisherAdmin` ist standardmäßig zugeklappt (`:22`). Von keiner Spec gefahren; nicht im
  Ist-Stand von B-212, darum dort nicht gezählt (B-212 zählt „drei"/„vier" `action.message`-Banner – die
  Zahl ist in der Story selbst uneinheitlich).
- Verwandt, außerhalb des B-212-Diffs: `VaterLehrwerke.tsx:692` – „Bearbeiten" am `UnitPanel` räumt den
  Seitenplatz nicht, anders als `bearbeitenUmschalten` `:323` und `onToggle` `:209` (Regel aus B-197,
  „Formular öffnen räumt"). Kein Widerspruch, nur eine veraltete Bestätigung bleibt stehen.

## Die echte Lücke

Der Schnitt aus B-212 („alles vom Server reist nach oben") endet an der Dateigrenze, die Seite nicht.

## Offene Punkte

1. `PublisherAdmin` ein `onMeldung` geben und `nachObenMelden` nach `src/lib/` ziehen – oder B-212
   ausdrücklich auf die Datei einschränken und diesen Baustein so lassen? *Empfehlung: nach oben melden* –
   die User Story von B-212 spricht von der Seite, und der Weg zum Widerspruch ist real, wenn auch hinter
   einem zugeklappten Bereich.
2. Den `UnitPanel`-„Bearbeiten" (`:692`) mitnehmen? *Empfehlung: ja, eine Zeile, dieselbe Regel.*

## Verlauf

- **2026-10-17** — angelegt aus der **Nachschau** zu Sprint 2026-09-05 (Nachtlauf, Protokoll
  `docs/pm-sitzung-2026-10-17-nacht.md`), `entgangen_bei: B-212`. Direkt `ausformuliert`: Der Ist-Stand ist
  aus der Nachschau mit `Datei:Zeile` belegt.
