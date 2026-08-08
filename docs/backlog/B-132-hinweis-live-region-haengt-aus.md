---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/a11y, rolle/creator]
aliases: [Live-Region wird ein- und ausgehängt, aria-live meldet nicht zuverlässig]
status: ausformuliert
prio: P3
art: Defekt
quelle: frontend-reviewer zur Abnahme von
  [B-126](B-126-ableitung-behauptet-falsche-herkunft.md) (2026-08-07), Befund 7 — dort nicht mitgenommen,
  weil B-126s Ziel ohne ihn erfüllt ist und A11y-Arbeit mit dem `accessibility`-Skill gehört
grund: ""
ersetzt_durch: []
entgangen_bei: [B-67]
wartet_auf: ""
---

# B-132 · Der Hinweis „aus dem Lehrwerk übernommen" wird angesagt, indem seine Live-Region entsteht

Die drei Hinweise im Fachlehrer-Formular tragen `role="status"` und `aria-live="polite"` — sie werden aber
**mitsamt ihrer Region** ein- und ausgehängt. Eine Live-Region, die im selben Moment erst entsteht, in dem
sie ihren Inhalt bekommt, meldet ein Screenreader unzuverlässig bis gar nicht: die Region muss vorher da
sein, damit die Änderung ihres Inhalts als Änderung wahrgenommen wird (WCAG 2.2 SC 4.1.3).

## User Story

Als **Creator, der mit einem Screenreader arbeitet**, möchte ich erfahren, dass Fach und Sprachen sich
gerade aus dem gewählten Lehrwerk gefüllt haben — sonst ändern sich drei Felder, ohne dass ich es merke.

## Ist-Stand am Code

`frontend/src/vater/VaterFachlehrer.tsx:259`, `:297`, `:305` — dreimal dasselbe Muster:

```tsx
{isDerived("subjectId") && <span className="muted" role="status" aria-live="polite" …>aus dem Lehrwerk übernommen</span>}
```

Die Bedingung steht **vor** dem Element, nicht in ihm: ist sie falsch, existiert die Region nicht.

Der Befund ist aus [B-67](B-67-fachlehrer-aus-lehrwerk.md) geerbt, wird aber seit
[B-126](B-126-ableitung-behauptet-falsche-herkunft.md) **häufiger** ausgelöst: vorher erschien der Hinweis
im Wesentlichen einmal beim Wählen der Reihe, jetzt verschwindet und erscheint er auch beim Wechsel und
beim Öffnen eines gespeicherten Profils.

Zum Vergleich, wie das Repo es sonst hält: `StatusBanner` rendert seine Region **dauerhaft** und tauscht
nur den Inhalt — im selben Formular sichtbar an `VaterFachlehrer.tsx` (der Banner unter dem Absenden-Knopf).

## Die echte Lücke

Nicht „die Attribute fehlen" — sie sind gesetzt, und jemand hat dabei an Barrierefreiheit gedacht. Die
Lücke ist, dass die Attribute allein nichts bewirken, wenn die Region nicht überlebt: die Ansage hängt an
einer Bedingung, die die Region selbst erzeugt.

Reichweite: begrenzt und real. Es trifft nur die Screenreader-Nutzung, und der Hinweis ist eine
Zusatzinformation, keine Fehlermeldung — aber es ist genau die Art Zusicherung, die man für erfüllt hält,
weil die Attribute im Code stehen.

## Offene Punkte

1. **Region dauerhaft rendern oder Ansage anders lösen?** Empfehlung: dauerhaft rendern, Inhalt tauschen
   (Muster `StatusBanner`) — der billigste Weg, der die bestehende Optik unangetastet lässt (leerer Text
   nimmt keinen Platz). Alternative wäre eine einzige gemeinsame Region für alle drei Felder; das
   verlöre aber die Zuordnung „welches Feld".
2. **Gilt dasselbe Muster anderswo?** Nicht erhoben. Vor dem Bau einmal alle `aria-live`/`role="status"`
   im Frontend durchsehen, die hinter einer Bedingung stehen — wenn es mehrere sind, ist das eher eine
   Regel (und ein Kandidat für einen Wächter) als ein Einzelfall.
3. **Mit welchem Werkzeug prüfen?** Empfehlung: der `accessibility`-Skill, nicht nach Gefühl. Ein
   Vitest-Fall kann belegen, dass die Region durchgehend im DOM steht; ob ein Screenreader sie *ansagt*,
   kann er nicht belegen — das bleibt ein benannter menschlicher Check, verwandt mit
   [B-31](B-31-geraete-vorbehalt-klang.md).

## Akzeptanzkriterien

> Entwurf, siehe Offene Punkte.

1. Die Live-Region der drei Hinweise steht durchgehend im DOM; nur ihr Textinhalt wechselt.
2. Ein Vitest-Fall belegt das (Region vorhanden, wenn kein Hinweis aktiv ist).
3. Die sichtbare Darstellung ändert sich nicht — kein zusätzlicher Leerraum, wenn kein Hinweis ansteht.

## Verlauf

- **2026-08-07** — angelegt aus dem `frontend-reviewer`-Befund zur B-126-Abnahme, am Code nachgeprüft
  (`VaterFachlehrer.tsx:259,297,305`). **Bewusst nicht in B-126 mitgenommen:** dessen Akzeptanzkriterien
  sind ohne diesen Punkt erfüllt, der Fehler ist älter als die Story, und eine A11y-Korrektur nebenbei zu
  erledigen ist genau der Reflex, den `docs/backlog/README.md` als „mitschlucken" beschreibt.
  `entgangen_bei: [B-67]`: das Muster ist in jener Story entstanden und war `abgenommen`.
