---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/a11y, rolle/supervisor]
aliases: [Fokus fällt auf body nach „Reihe anlegen & wählen“]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Review Sprint 11 zu B-64 (sechster Nachtlauf, docs/pm-sitzung-2026-10-26-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-10-26"
---

# B-268 · Nach „Reihe anlegen & wählen“ fällt der Fokus auf `body`

Das Inline-Anlegen einer Reihe im Lehrbuch-Formular ([B-64](B-64-textbook-vs-series.md),
`frontend/src/vater/ChildMaterialSection.tsx`, `reiheAnlegen`) schließt nach Erfolg sein Panel – samt dem gerade
fokussierten Knopf. Der Fokus landet auf `body`; eine Tastaturnutzerin verliert ihren Platz. Nach „+ Neue Reihe“
springt der Fokus außerdem nicht ins Namensfeld.

Vorschlag (vom `frontend-reviewer`): `autoFocus` am Namensfeld, nach Erfolg den Fokus auf `#${id}-series`; Test als
Komponententest (`document.activeElement`).

**Nicht in diesem Lauf behoben:** Der Reviewer stufte es als Hinweis (🟢) ein. Eine Behebung hätte den Fund-Zähler
von Sprint 11 über die Grenze von fünf gehoben (Freigabe 3). Das ist hier offen benannt, die Entscheidung liegt
beim Nutzer.

## Entscheidungen

1. **Wie vom Reviewer vorgeschlagen** (Nutzer, 2026-10-26): Beim Öffnen des Anlege-Streifens geht der Fokus ins
   Namensfeld, nach dem Anlegen auf die Reihen-Auswahl. Mitgenommen, weil es dieselbe Ursache hat: Nach „Abbrechen“
   geht er zurück auf „+ Neue Reihe“. Gesetzt wird über einen Effekt nach dem Rendern, nicht über `autoFocus`. Das
   Ziel existiert erst nach dem Rendern, und `autoFocus` griffe auch beim ersten Erscheinen der Seite.

## Akzeptanzkriterien

1. „+ Neue Reihe“ setzt den Fokus ins Namensfeld.
2. Nach „Reihe anlegen & wählen“ steht der Fokus auf der Reihen-Auswahl, nicht auf `body`.
3. Nach „Abbrechen“ steht der Fokus auf „+ Neue Reihe“.

## Schätzung

**XS**, `wo: frontend`, keine Migration, kein Vertragsbruch. **Testweg:** ein Komponententest auf
`document.activeElement` je Schritt.

## Verlauf

- **2026-10-26** — angelegt aus dem `frontend-reviewer` zu B-64 im sechsten Nachtlauf (Fund 4).
- **2026-10-26** — **Entscheidung des Nutzers** (Morgen-Entscheidung 2026-10-26, Dialog, [Protokoll](../pm-sitzung-2026-10-26-nacht.md#morgen-entscheidung-2026-10-26)): Das Zurückstellen unter dem alten Zähler war richtig. Die Story wird jetzt mit der **Nachschau zu Sprint 11** (zu B-64) mitgenommen, umgesetzt wie vom Reviewer vorgeschlagen (`autoFocus` am Namensfeld, nach Erfolg Fokus auf `#${id}-series`, Komponententest auf `document.activeElement`). **Freigabe des Nutzers (Autonomer Modus, Opt-in):** ein Nachtlauf darf sie selbst ausformulieren, schätzen und bauen.
- **2026-10-26** — ausformuliert und geschätzt (XS) unter Freigabe 9, gebaut ([Protokoll](../pm-sitzung-2026-10-26-tag.md), Lauf B, Sprint *Lehrbuch-Formular*). `ChildMaterialSection.tsx`: Zustand `fokusZiel` plus Effekt, Ref am Knopf „+ Neue Reihe“. Komponententest in `TextbookForm.test.tsx` (vor dem Anlegen ist festgestellt, dass der Fokus nicht schon auf der Auswahl steht). Rote Proben, je ohne einen der drei Sprünge: Namensfeld → rot an `:333`, Knopf → rot an `:336`, Reihe → rot an `:344` (je **1 failed**). Grün: Vitest **441/441**. Nebenbefund: Die `ECONNREFUSED`-Meldungen in `TextbookForm.test.tsx` gab es schon vorher (10 an HEAD), kein Fehler.
- **2026-10-26** — Review ([Protokoll](../pm-sitzung-2026-10-26-tag.md), Lauf B, Sprint *Lehrbuch-Formular*): `frontend-reviewer` Fund 2 (gezählt 3): Das Zurücksetzen des Fokusziels prüfte kein Test. Kein echter Fehler, wie ich zuerst meinte: Der Zustand wurde nach jedem Sprung auf `null` gesetzt. Umgebaut auf einen Ref mit Effekt ohne Abhängigkeiten, der das bauartbedingt ausschließt, plus der Schritt „öffnen → per Knopf schließen → öffnen“. Fund 3 (gezählt 4): Der Fehlerweg war ungetestet. Delta-Review (gezählt 5): **Im echten Browser gibt der während des Abrufs gesperrte Knopf den Fokus an `body` ab, jsdom nicht.** Gemessen mit der neuen Spec `reihe-anlegen-scheitert-fokus.spec.ts`: vor dem Fix `activeElement.id` **leer (body)**, danach `tbnew-neue-reihe`. Behoben: Im Fehlerzweig geht der Fokus direkt ins Namensfeld. Rote Proben: Ref nicht geleert → rot an `:366`; Fokus vor dem Erfolgs-Check → **1 failed**; Fokuszeile im Fehlerzweig weg → **1 failed** (Vitest). Rollengang ohne Browser (Extension nicht verbunden) als E2E: `lehrbuch-katalog-zuerst.spec.ts` (Vater: Katalog zuerst, Reihe inline anlegen, Entkoppeln bringt den Freitext zurück), `reihe-anlegen-scheitert-fokus.spec.ts` (Fehlerweg per Tastatur). Sohn und Creator als Regressionszeugen: volle Suiten grün. Verifikation: Solution-Build 0 Warnungen, Backend **918/918 `exit=0`**, Vitest **443/443**, Playwright voll nach dem letzten Code-Edit **95 passed**. Zähler des Sprints am Ende **5** (Grenze erreicht, nicht überschritten), darum kein weiteres Delta-Review. **Abgenommen**; Commit: `4a25d47`.
- **2026-10-26** — **Nachschau** (frischer Prüfer, `git archive` von `4a25d47`, [Protokoll](../pm-sitzung-2026-10-26-tag.md), Lauf C): AK 1–3 halten, die Mutanten „kein name/knopf/reihe“ und „Fehlerzweig ohne Fokus“ sind rot. **Entgleiten:** „Ref nicht geleert“ bleibt grün (18/18). Meine Probe „rot an `:366`“ galt nur vor Fund 5 → [B-280](B-280-fokusziel-nicht-geleert-ungetestet.md). **Doku-Befund:** Die Zeilenangabe des Reihen-Sprungs lautet im Commit `:352`, nicht `:344` (gemessen an einem Zwischenstand).
