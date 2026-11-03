---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/test, rolle/supervisor]
aliases: [.vater-main overflow-x ungetestet]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Sprint 10 in Lauf B (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-03"
entgangen_bei: [B-68]
wartet_auf: ""
---

# B-276 · Die Überlauf-Regel von `.vater-main` trägt kein Test mehr

Die Kernregel aus [B-68](B-68-vater-web-responsive.md), `.vater-main { min-width: 0; overflow-x: auto }`
(`frontend/src/index.css`), lässt eine breite Tabelle in `.vater-main` scrollen statt das Dokument verbreitern (AK 2,
AK 5 von B-68). Die roten Proben von B-68 („Netz allein“) wurden gemessen, als das ⓘ-Popover noch `absolute` war und
über den Rand ragte. Seit [B-263](B-263-info-popover-steht-am-rechten-rand-ueber.md) Entscheidung 2 steht es fest
zum Fenster und trägt nichts mehr zum Überlauf bei. Mit dem Seed läuft keine der 22 Routen über. **Ein Rückbau der
Regel bliebe darum grün**, wenn die Überlegung der Nachschau stimmt. Gemessen ist das noch nicht.

Dazu kommt ein veralteter Kommentar an `.vater-main`: Er sagt, absolut positionierte Kinder („das ⓘ-Popover“) würden
abgeschnitten und `InfoHint` halte sich im sichtbaren Streifen. Beides gilt seit dem festen Popover nicht mehr.

Vorschlag: zuerst die Mutante „Regel weg“ gegen `responsive-vater.spec.ts` messen. Bleibt sie grün, braucht es eine
Route oder Testdaten mit einer wirklich breiten `.table` ohne eigenen Rahmen und die Zusicherung
`main.scrollWidth > main.clientWidth` bei einem Dokument, das nicht breiter ist als das Fenster. Offen und ebenfalls
zu messen: ob `.vater-main` in manchen Lagen auch senkrecht scrollt (`feldhilfe-lage.spec.ts` sichert sich gegen
beides ab).

## User Story

Als **Vater** am Tablet möchte ich, dass eine breite Tabelle in ihrem Seitenbereich seitlich scrollt statt unerreichbar
abgeschnitten zu werden – und dass ein Test das festhält, damit ein Rückbau der Regel auffällt.

## Ist-Stand am Code (Lauf P, Stand `bffb583`)

- `frontend/src/index.css:322`: `.vater-main { … min-width: 0; overflow-x: auto; … }`, Kommentar `:319-321` begründet
  `min-width: 0` („sonst wanderte der Überlauf eine Ebene höher") und spricht vom absoluten ⓘ-Popover.
- `index.css:33`: `html { overflow-x: hidden }` – ohne die Regel wäre eine breite Tabelle abgeschnitten, nicht scrollbar.
- **Gemessen:** Mutante „beide Deklarationen weg" gegen `responsive-vater.spec.ts` und `feldhilfe-lage.spec.ts`:
  **7 passed** – die Vermutung der Idee stimmt.

## Die echte Lücke

Kein Fall trägt die Regel. Dazu, beim Messen gefunden: **`min-width: 0` trägt nichts** – Mutante „ohne `min-width: 0`"
grün, „ohne `overflow-x: auto`" rot. Ein Flex-Kind, dessen Überlauf nicht `visible` ist, hat ohnehin die automatische
Mindestgröße 0; der Kommentar behauptete das Gegenteil.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Eine breite Tabelle als Probe in der echten Seite** (per `evaluate` in `.vater-main` gehängt) statt neuer Testdaten.
   Die Regel ist eine Eigenschaft des Containers, nicht des Inhalts. *Kosten:* die Probe ist kein Bildschirm, den es gibt.
2. **`min-width: 0` wird gestrichen**, der Kommentar berichtigt (auch das veraltete Popover). *Kosten:* gemessen nur in
   Chromium; Firefox und Safari folgen derselben Regel der Spezifikation (automatische Mindestgröße nur bei `visible`).
3. **Senkrechtes Scrollen von `.vater-main`** wird mitgemessen, nicht zugesichert (offener Punkt der Idee): gemessen
   `false` auf `/vater` bei 768 × 900.

## Akzeptanzkriterien

1. Ohne `overflow-x: auto` an `.vater-main` fällt ein Test.
2. Der Kommentar an `.vater-main` beschreibt, was gilt.

## Schätzung

`XS`, frontend. Testweg: neuer Fall in `responsive-vater.spec.ts`, Mutanten gegen ihn.

## Verlauf

- **2026-10-26** — angelegt aus der Nachschau zu Sprint 10 (frischer Prüfer, read-only) in Lauf B
  ([Protokoll](../pm-sitzung-2026-10-26-tag.md)).
- **2026-11-03** — Lauf P, Teil 2: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt, gebaut. Messung vor dem Bau:
  „Regel weg" **7 passed** (Lücke bestätigt). Neuer Fall gegen die Mutanten: „beide weg" **1 failed**, „ohne
  `overflow-x: auto`" **1 failed** (an „scrollt seitlich"), „ohne `min-width: 0`" **1 passed** → gestrichen (Entscheidung 2).
  Mit der vereinfachten Regel `responsive-vater.spec.ts` **2 passed**.
- **2026-11-03** — Review `frontend-reviewer`: sauber für B-276 – entfällt `overflow-x: auto`, fallen `mainBreite`, `verschoben` und `dokument`; Streichen von `min-width: 0` nach Flexbox §4.5 browserübergreifend unbedenklich (die automatische Mindestgröße gilt nur für Flex-Kinder, die keine Scroll-Container sind); alles synchron in einem `evaluate`, ohne Seed-Abhängigkeit.
- **2026-11-03** — Abnahme Lauf P, Teil 2: Vitest **620/620**, `tsc -b` sauber, E2E-Gesamtlauf **107 passed** (`exit=0`, nach dem letzten Code-Edit gestartet). Rollengang: Vater maschinell – die neue Probe fährt die echte Seite bei 768 px im Browser, die 22 Routen der Spec bleiben ohne seitliches Scrollen; Creator und Sohn Regressionszeugen. **Abgenommen**, Commit siehe Protokoll ([Lauf P](../pm-sitzung-2026-11-03-nacht.md)).
- **2026-11-03** — Nachschau in Lauf Q, Teil 1 (frischer `frontend-reviewer`, `git show a25a1a6 -- frontend`, read-only): **Entgleitung** → [B-363](B-363-begruendung-zu-min-width-falsch-mainbreite-traegt-nichts.md) (🟡: `.app-vater` ist eine Flex-Spalte, die Begründung zu `min-width: 0` ist falsch, `mainBreite` trägt nichts). Geprüft: `verschoben` hält AK 1 gegen die Mutante „ohne `overflow-x`".
