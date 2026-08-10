---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/a11y, rolle/creator]
aliases: [Live-Region wird ein- und ausgehängt, aria-live meldet nicht zuverlässig]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: frontend-reviewer zur Abnahme von
  [B-126](B-126-ableitung-behauptet-falsche-herkunft.md) (2026-08-07), Befund 7 — dort nicht mitgenommen,
  weil B-126s Ziel ohne ihn erfüllt ist und A11y-Arbeit mit dem `accessibility`-Skill gehört
grund: ""
ersetzt_durch: []
entgangen_bei: [B-67]
wartet_auf: ""
nachgeschaut: 2026-08-10
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

## Entscheidungen

Autonom gegrillt im Nachtlauf am 2026-08-09 (Freigabe 1: `art: Defekt`), Protokoll
[pm-sitzung-2026-08-09.md](../pm-sitzung-2026-08-09.md).

1. **Dauerhaft rendern, Inhalt tauschen — als eigene Komponente `DerivedHint`, nicht als drei Kopien.**
   *Begründung*: Das Muster ist im Repo bereits entschieden und begründet — `StatusBanner.tsx:9-12` sagt
   wörtlich, warum die Region auch im Leerfall im DOM steht („viele Screenreader sagen nur an, was in eine
   *bereits vorhandene* Region hineinwächst"). Eine Komponente statt dreier Kopien aus demselben Grund,
   aus dem `StatusBanner` eine ist: die Attribute sind schnell getippt, die Leer-Regel ist schnell
   vergessen. *Kosten*: drei zusätzliche, leere `<span>` im DOM — **und die sind nicht gratis.**
   Nachtrag vom selben Tag, gefunden vom `frontend-reviewer`: `.field` ist ein Flex-Container mit
   `gap: 6px`, und ein `gap` gilt **zwischen den Items**, unabhängig davon, ob eines leer ist. Die leere
   Region ließ jedes der drei Felder um 6 px wachsen, und weil `form-grid` mit `alignItems: "end"`
   ausrichtet, verschob sie die Bedienelemente gegenüber den Nachbarfeldern derselben Zeile. Meine
   ursprüngliche Formulierung hier („nehmen also keinen Platz — AK 3 ist damit erfüllt, nicht bloß
   behauptet") war **gemessen falsch**: sie beschrieb die Höhe des `<span>`, nicht die des Feldes.
   Behoben mit `marginTop: active ? 0 : -6`, begründet am Code. Die Regel gehört in den
   [B-134](B-134-bedingte-live-regionen.md)-Sweep: „Bedingung in die Region" allein genügt nicht, der
   Gap-Kontext muss mit.
2. **Es ist eine Regel, kein Einzelfall — die Verallgemeinerung wird eine eigene Story.** Beim Bauen
   gemessen (`grep` über `frontend/src` nach `aria-live`/`role="status"`, jede Fundstelle einzeln
   klassifiziert): **zwölf weitere** Live-Regionen stehen hinter einer Bedingung, in neun Dateien —
   `VaterExerciseCreate.tsx:270,272`, `VaterMedia.tsx:250`, `VaterPlanCreate.tsx:81`,
   `VaterWizard.tsx:397`, `SohnPractice.tsx:360`, `SohnShop.tsx:178`, `VaterVocab.tsx:465,489`,
   `VaterLogin.tsx:45`, `exerciseConfig.tsx:619`, `ListControls.tsx:61`. *Begründung*: B-132s Ziel
   (die drei Fachlehrer-Hinweise) ist ohne sie
   erfüllt, und ein Sweep über acht Dateien plus ein Wächter ist eine andere Arbeit als dieser Fix —
   `docs/backlog/README.md` verlangt dafür genau eine eigene Story. Sie ist als
   [B-134](B-134-bedingte-live-regionen.md) angelegt. *Kosten*: der Fehler bleibt an zehn Stellen
   bestehen, bis B-134 läuft — sichtbar notiert statt still.
3. **Der Vitest prüft Identität, nicht Anzahl.** *Begründung*: „drei Hinweise erscheinen" prüft der
   bestehende erste Fall schon (`:31`) und war die ganze Zeit grün — er kann den Defekt gar nicht sehen.
   Die Zusicherung ist, dass **dieselben Knoten** vorher schon dastanden. *Kosten*: der Test kennt die
   Zahl 4 (drei Hinweise + `StatusBanner`) und muss angefasst werden, wenn dem Formular eine Region
   zuwächst — das ist ein akzeptabler Preis für eine Zusicherung, die sonst nicht formulierbar ist.
4. **Ob ein Screenreader es *ansagt*, bleibt ein benannter menschlicher Check.** *Begründung*: Wie
   [B-31](B-31-geraete-vorbehalt-klang.md) — ein Vitest kann belegen, dass die Region durchgehend im DOM
   steht, nicht dass NVDA/VoiceOver sie vorliest. *Kosten*: die Story schließt über Step 6s dritten
   Ausgang („delivered, pending human check"), nicht mit einem vollen Sign-off.

## Akzeptanzkriterien

1. Die Live-Region der drei Hinweise steht durchgehend im DOM; nur ihr Textinhalt wechselt.
2. Ein Vitest-Fall belegt das (Region vorhanden, wenn kein Hinweis aktiv ist).
3. Die sichtbare Darstellung ändert sich nicht — kein zusätzlicher Leerraum, wenn kein Hinweis ansteht.

## Schätzung

**XS** (`wo: frontend`, `migration: nein`, `vertragsbruch: nein`) — eine Datei, eine kleine Komponente,
drei ersetzte Zeilen, ein Testfall. Vergleichbar mit dem XS-Anker (zwei Sätze in `fieldHelp.ts` plus der
Test, der sie prüft, B-02).

**Testweg:** `frontend/src/vater/VaterFachlehrer.test.tsx` — neuer Fall „hält die Hinweis-Regionen
dauerhaft im DOM". Kein E2E: der Defekt ist im DOM sichtbar, nicht im Weg durch die App; ein Playwright-
Lauf wäre teurer und würde dieselbe Zusicherung schwächer treffen.

## Verlauf

- **2026-08-07** — angelegt aus dem `frontend-reviewer`-Befund zur B-126-Abnahme, am Code nachgeprüft
  (`VaterFachlehrer.tsx:259,297,305`). **Bewusst nicht in B-126 mitgenommen:** dessen Akzeptanzkriterien
  sind ohne diesen Punkt erfüllt, der Fehler ist älter als die Story, und eine A11y-Korrektur nebenbei zu
  erledigen ist genau der Reflex, den `docs/backlog/README.md` als „mitschlucken" beschreibt.
  `entgangen_bei: [B-67]`: das Muster ist in jener Story entstanden und war `abgenommen`.
- **2026-08-09** — Nachtlauf, Sprint 1: autonom gegrillt (vier Entscheidungen), geschätzt (**XS**,
  `frontend`) und gebaut. **Rote Probe mit Zahl:** der neue Vitest-Fall erwartete 4 Live-Regionen und
  maß **1** — nur der `StatusBanner` war da, die drei Hinweis-Regionen existierten nicht. Nach dem Fix
  (Komponente `DerivedHint`, Bedingung *in* der Region statt um sie herum, `VaterFachlehrer.tsx:128-144`
  und `:271`/`:309`/`:317`) **4/4 grün** in der Datei, **174/174** in der gesamten Frontend-Suite
  (25 Dateien), `tsc -b` ohne Befund. Beim Bauen gefunden und als [B-134](B-134-bedingte-live-regionen.md)
  abgespalten: dasselbe Muster steht an **zwölf** weiteren Stellen in neun Dateien (jede Fundstelle
  einzeln klassifiziert, nicht hochgerechnet).
  Reviewer und Rollengang laufen am Sprint-Ende (Step 5/6), darum noch `in-arbeit`.
- **2026-08-09** — `frontend-reviewer` (Sprint 1, Step 5): **AK 3 war nicht erfüllt**. Die dauerhafte
  Region kostete 6 px je Feld (`.field` hat `gap: 6px`, ein `gap` gilt zwischen Items unabhängig von
  deren Höhe) und verschob über `alignItems: "end"` drei Bedienelemente gegen ihre Nachbarn. Behoben mit
  `marginTop: active ? 0 : -6`; Entscheidung 1 trägt die Korrektur samt der Messung, die meine
  ursprüngliche Behauptung widerlegt. Suite danach 177/177 grün. Zwei weitere Reviewer-Vorschläge
  (Test ohne die feste Zahl 4 verankern; Feldname per `.sr-only` in die Ansage ziehen plus
  `aria-describedby`) sind **nicht** umgesetzt — sie gehören zu B-134 bzw. brauchen den
  `accessibility`-Skill, und der Lauf ist an dieser Stelle beendet worden.
- **2026-08-10** — `frontend-reviewer`, Re-Review: **kein Korrektheitsfund**. Der Reviewer hat die
  Layout-Rücknahme in Chromium bei fünf Breiten gemessen: „nach dem Fix, untätig" ist in **jeder** Zahl
  identisch mit „vor dem Fix" (`fieldHeight`, `labelTop`, `ctlTop`, `ctlBottom`, Gitterhöhe) — AK 3 ist
  jetzt belegt und nicht mehr behauptet. Ein 🟢 umgesetzt: die `6` stand als Kopie im TSX, ohne
  Verbindung zu `.field { gap: 6px }` — wer den `gap` ändert, hätte den Versatz still zurückbekommen.
  Sie liegt jetzt als `.field > .live-slot` in `index.css`, direkt unter dem `gap`, den sie ausgleicht.
  **Bekannt und unverändert offen** (kein Befund dieses Diffs): der *aktive* Hinweis bricht in der
  181px-Spalte auf zwei Zeilen und schiebt die Nachbarfelder um 40 px — wer das loswerden will, müsste
  den Platz reservieren statt ihn zurückzunehmen. Gehört zu B-134.
- **2026-08-10** — **abgenommen, mit einem benannten menschlichen Check.** Commit `1905034`.
  Verifikation: **4/4** eigene Fälle, Frontend-Suite **177/177**, `tsc -b` sauber, E2E **29/29** als
  Rollengang, `frontend-reviewer` zweimal — der zweite Lauf hat die Layout-Neutralität bei fünf Breiten
  **gemessen**. Was kein Automat entscheiden kann und darum offen bleibt (Step 6, dritter Ausgang): ob
  NVDA/VoiceOver die drei Hinweise wirklich vorliest. Der Check für den Menschen: Fachlehrer-Formular
  öffnen, eine Reihe wählen, hinhören.
- **2026-08-10** — nachgeschaut (Nachtlauf, Retro des Folge-Sprints). Geprüft wurde genau die Eigenschaft,
  deren Fehlen der Defekt war: die Live-Region steht in `VaterFachlehrer.tsx:141` **dauerhaft** im DOM —
  `active` schaltet nur eine CSS-Klasse, nicht die Existenz des Elements. Ein bedingtes Rendern wäre der
  Rückfall gewesen. Kein durchgekommener Defekt.
