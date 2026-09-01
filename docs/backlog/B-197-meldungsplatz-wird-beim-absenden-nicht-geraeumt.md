---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/creator, rolle/supervisor]
aliases: [gruenes Gespeichert ueber rotem Fehler, vierter setMeldung(null) fehlt]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-09-02 zu Sprint 1 des Nachtlaufs
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-173, B-151]
wartet_auf: ""
---

# B-197 · Der Meldungsplatz wird beim Absenden nicht geräumt — und zeigt dann zwei Wahrheiten

Die Reparatur von Sprint 1 hat an dieser Stelle **eine neue Instanz genau des Defekts erzeugt, gegen den
sie gebaut wurde.**

## User Story

Als **Creator** möchte ich nicht gleichzeitig „Gespeichert." und eine Fehlermeldung sehen, damit ich weiß,
ob mein letzter Klick geglückt ist.

## Ist-Stand am Code

Geräumt wird an genau **drei** Stellen, und alle drei hängen am **Öffnen** eines Formulars:
`ChildMaterialSection.tsx:93`, `VaterFachlehrer.tsx:93`, `VaterLehrwerke.tsx:196` (`onOeffnen`).

Es fehlt das Räumen beim **Absenden** — und das trifft ausgerechnet die eine Fläche, auf der das Formular
offen bleibt:

- `VaterLehrwerke.tsx:415` setzt bei Erfolg den Seitenplatz, `SeriesForm` bleibt danach offen. Ein
  **zweites** Absenden derselben Sitzung, das scheitert (`action.fail("Der Name der Reihe fehlt.")` auf
  `:390`, oder ein Server-Fehler), zeigt den roten Banner am Formular — während das grüne „Gespeichert."
  am Seitenplatz (`:210`) stehen bleibt. **Zwei widersprechende Meldungen in einem Blickfeld.**
- Kleinere Ausprägungen derselben Lücke: `ChildMaterialSection.tsx:117` und `VaterFachlehrer.tsx:124`
  (Anlegen räumt nicht), `VaterLehrwerke.tsx:195` (die Unit-Ziehharmonika räumt nicht).

## Die echte Lücke

„Der Erfolg reist nach oben, Fehler bleiben lokal" ist **zustandsfrei** formuliert, der Seitenplatz ist
aber **zustandsbehaftet**. Solange nichts ihn räumt, überlebt eine Meldung die Handlung, die sie meinte.

## Offene Punkte

1. Beim Absenden räumen, oder den Platz nach einer Weile verfallen lassen? Empfehlung: **beim Absenden**
   — B-151s Entscheidung 2 hat einen Timer bereits verworfen, mit Kosten und Begründung; das gilt weiter.
2. Der billigste Schnitt: `onOeffnen`/`onMessage` zu **einem** `onMeldung(m: ActionMessage | null)`
   verallgemeinern und am Anfang von `submit()` mit `null` rufen. Zu bestätigen beim Bauen.

## Verlauf

- 2026-09-02 · Aufgenommen aus der **Nachschau** zu Sprint 1 (zweiter Nachtlauf). `entgangen_bei` nennt
  beide Stories, weil der Platz aus B-173 stammt und B-151 ihn auf zwei weitere Flächen gebracht hat.
