---
tags: [typ/story, status/idee]
aliases: []
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17 (Mathe in die App; Material raw/Math/lambacher-schweizer-9-sh-pugling/md)"
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-638 · Formeln in Aufgabentext und Auswahl anzeigen

Das Mathe-Lehrwerk (Lambacher Schweizer 9, Kapitel IV) liegt durchgehend in LaTeX vor (`$x^2 - 6x + 8 = 0$`), das Frontend
kann Formeln aber nicht setzen – in `frontend/package.json` steht weder KaTeX noch MathJax. Ohne das ist kein
Mathe-Inhalt lesbar, auch nicht mit den vorhandenen Typen Zuordnen und Lückentext (Merkwissen: pq-Formel, Satz von
Vieta, Diskriminanten-Fälle; Karten-Aufgaben Gleichung ↔ p/q ↔ Term ↔ Lösung). Gedacht: `$…$` in Aufgabentext,
Auswahloptionen und Auflösung wird als Formel gesetzt, im Sohn- und im Vater-Web. Erste Stufe des Mathe-Vorhabens;
B-640 bis B-642 hängen daran.

## Verlauf

- **2026-11-17** — angelegt (Quelle: Konzept „Mathe in die App" aus dem Chat, Lambacher Schweizer 9, Kapitel IV).
