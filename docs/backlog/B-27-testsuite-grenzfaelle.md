---
tags: [typ/story, status/idee, bereich/doku]
aliases: [Testsuite-Sensitivität, Grenzfälle]
status: idee
prio: P2
art: Aufräumen
quelle: docs/testplan.md
unverifiziert: true
---

# B-27 · Testsuite: die Grenzfall-Lücke schließen

Gemessen per Defektinjektion (2026-07-30): Konformität 60 %, Sensitivität 57 % — rund 40 % der eingebauten
Fehler blieben unbemerkt, **trotz** 268/268 abgedeckter Endpunkte. Die Fehlerklasse hat einen Namen: „Regel
getestet, Grenzfall offen" — die teuerste Lücke war eine Bestehensgrenze, die nie *genau auf* der Schwelle
geprüft wurde.

**Nicht neu erheben** — der Befund steht in [testplan.md](../testplan.md). Offen ist das Abarbeiten.

## Verlauf

- **2026-07-30** — geerntet (Befund liegt vor, Abarbeitung offen).
