---
tags: [typ/story, status/idee, bereich/training, rolle/student]
aliases: [Nachschau-Reste Sprint 31, scrollWidth kann nicht fallen]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "beides"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 31)
unverifiziert: true
entgangen_bei: [B-466, B-457]
grund: ""
ersetzt_durch: []
---

# B-479 · Nachschau-Reste Sprint 31: eine Zusicherung, die nicht fallen kann, und Grenzfälle der Schlusszeichen

Gefunden in der Nachschau von Sprint 31 (frischer `frontend-reviewer`, gelesen, nicht nachgestellt):

1. **🟡 Zusicherung ohne Wirkung** (`frontend/e2e/lueckentext-am-handy.spec.ts`, zweiter Test): „die Seite scrollt nicht
   seitlich" misst `document.documentElement.scrollWidth`. `.sohn-body` hat `overflow-y: auto` (`index.css:126`), damit
   auch horizontal `auto` – ein zu breiter Kopf scrollt *in* `.sohn-body`, nicht die Seite; dazu `html { overflow-x:
   hidden }`. Die Zeile bleibt auch ohne Fix grün; AK 2 trägt nur die Rechtskante des Knopfs. Reparatur: den echten
   Scroller messen (`.sohn-body` `scrollWidth <= clientWidth`) und mit entferntem `play-head` rot messen – oder die Zeile
   streichen.
2. **🟢 `...` und `…` sind verschiedene Antworten**: eine Lücke, deren ganze Lösung `…` ist, bleibt wörtlich; iOS macht
   aus `...` selbst `…` – das Gerät entscheidet, wie beim Apostroph (B-440).
3. **🟢 Schlusszeichen vor Anführungszeichen oder Klammer** (`"Stop!"`, `(Hello.)`, `« Oui. »`) wird nicht entfernt.
   Kein Fall im Katalog bekannt.
4. **🟢 `¿`/`¡` am Anfang** bleiben Pflicht, während das Schluss-`?` verziehen ist. Kein Spanisch im Katalog.
5. **🟢 Der Kopf der Übungsrunde** (`SohnPractice`, Klasse `play-head`) hat keine E2E bei 375 px; heute läuft er nicht
   über, ein vierter Pill bliebe unbemerkt.
6. **🟢 Rückfall-Zweig von `ClozePrompt`** ohne `pre-line` (umnummerierter Altbestand) – in B-466 bewusst offen gelassen.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 31 im Backlog-Lauf).
