---
tags: [typ/story, status/idee, bereich/frontend, bereich/test, rolle/supervisor]
aliases: [.vater-main overflow-x ungetestet]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Sprint 10 in Lauf B (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
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

## Verlauf

- **2026-10-26** — angelegt aus der Nachschau zu Sprint 10 (frischer Prüfer, read-only) in Lauf B
  ([Protokoll](../pm-sitzung-2026-10-26-tag.md)).
