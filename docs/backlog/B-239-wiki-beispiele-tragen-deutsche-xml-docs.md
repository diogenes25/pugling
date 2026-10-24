---
tags: [typ/story, status/abgenommen, bereich/doku]
aliases: [wiki/08 deutsche summaries, Erweitern-Anleitung gegen die Englisch-Regel]
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
wo: doku
migration: nein
vertragsbruch: nein
quelle: Reviewer zu B-166 (Nachtlauf 2026-10-23, Sprint 2)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: 2026-10-23
---

# B-239 · Die Erweitern-Anleitung im Wiki zeigt deutsche `/// <summary>` – entgegen der Regel „Code-Doku auf Englisch"

`wiki/08-erweitern.md:41` sagt „mit deutschen `/// <summary>`", und die Code-Beispiele der Anleitung (`:45`,
`:66`, `:126`) tragen deutsche XML-Docs. Die Root-`CLAUDE.md` verlangt seit dem 2026-08-01 Englisch ohne
Ausnahme (239 Dateien umgestellt, `docs/translate.md`). Wer das Beispiel kopiert, liefert deutsche Swagger-Texte
– genau das, was die Umstellung beendet hat. Gefunden vom Reviewer, als B-166 in derselben Anleitung die
`Check`-Signatur berichtigte.

## User Story

Als **Entwickler, der einen neuen Übungstyp nach der Anleitung baut**, möchte ich Beispiele und Checklisten, die der
geltenden Regel „Code-Doku auf Englisch" folgen, damit ich keine deutschen Swagger-Texte einschleppe.

## Ist-Stand am Code

Vor dem Bau: `wiki/08-erweitern.md:14,41,154` („deutsche `/// <summary>`"), deutsche Kommentare in den C#-Beispielen
(`:44,:69,:74-75,:87-88,:124,:132`), `.claude/commands/neuer-uebungstyp.md:21,61,73`, `.claude/agents/pugling-reviewer.md:3`,
`README.md:111`.

## Akzeptanzkriterien

1. Keine aktive Anleitung verlangt mehr deutsche Code-Doku; die Beispiele tragen englische Kommentare.
2. Datierte Berichte bleiben unverändert und sind als solche benannt.

## Offene Punkte

1. Reicht es, die Beispiele zu übersetzen und den Satz in `:41` umzudrehen – oder nennt die Anleitung die
   Sprachregel noch an weiteren Stellen (`wiki/` greppen nach „deutsch")?
2. Gilt dasselbe für `wiki/09-llm-kochbuch.md` (Prompts, die Code erzeugen sollen)?

## Entscheidungen

Autonom gegrillt (`art: Aufräumen`, Freigabe 1).

1. **Aktive Anweisungen und Beispiele umstellen, datierte Notizen nicht.** Umgestellt: `wiki/08-erweitern.md:14,41,154`
   und die Code-Beispiele (`///` an `:44,:69,:124`, `//` an `:74-75,:87-88,:132`), `.claude/commands/neuer-uebungstyp.md:21,61,73`,
   `.claude/agents/pugling-reviewer.md:3`, `README.md:111`. Stehen gelassen: `docs/architektur-resumee.md:53` (Überholt-Banner),
   `docs/lernziele-objectives-plan.md:177`, `docs/code-review.md:64` (datierte Berichte) und ein `//` in einem `jsonc`-Beispiel
   (`wiki/05-punkte-und-bonus.md:252`, kein C#). *Kosten:* drei Volltexttreffer, die keine Anweisung sind.
2. **`wiki/09-llm-kochbuch.md`** ist sauber (Sichtung bestätigt).

## Schätzung

- **groesse:** XS · **wo:** doku · **migration:** nein · **vertragsbruch:** nein. **Testweg:** Grep nach der Regel, Markdown-Lint.

## Verlauf

- **2026-10-23** — angelegt aus dem Reviewer-Nit zu B-166 (Nachtlauf, Sprint 2). **Bewusst nicht in B-166
  mitgenommen:** dort ging es um tote Routen; die Sprache der Beispiele ist eine andere Regel und ein
  anderer Grep.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht und reicht weiter – neben `wiki/08-erweitern.md:14,41,44,69,124,154` tragen **aktive Anweisungen** die alte Regel: `.claude/commands/neuer-uebungstyp.md:61,73`, die Beschreibung von `.claude/agents/pugling-reviewer.md:3` („deutsche Docs“), `README.md:111`. `wiki/09-llm-kochbuch.md` ist sauber. Historische Notizen stehen lassen. XS–S, autonom.
- **2026-10-23** — vierter Nachtlauf, Sprint 9 (NP-12, [Protokoll](../pm-sitzung-2026-10-23-nacht.md)): ausformuliert, autonom gegrillt, gebaut. Sweep (Auflage 5) über `wiki/ .claude/ README.md` nach „deutsche … `/// <summary>`/XML-Docs/Docs": erwartet 7 (Sichtung) → gemessen **8 vorher** (zusätzlich `neuer-uebungstyp.md:21`) / **0 nachher**; deutsche Kommentare in den C#-Beispielen von `wiki/08`: **8 Zeilen / 0**. Reviewer (`pugling-reviewer`): Übersetzungen sinntreu, keine weitere aktive Anweisung; zwei Doku-Nits (datierter Bericht, `jsonc`) bewusst belassen. Rollengang: kein UI-Pfad, Ersatz benannt – Sweep und Lint. Abgenommen; Commit: `90387d5`.
- **2026-10-23** — Nachschau zu Sprint 9 (fünfter Nachtlauf, [Protokoll](../pm-sitzung-2026-10-24-nacht.md)), frischer Prüfer: Sweep „8 vorher / 0 nachher" an den acht Stellen nachgerechnet (`wiki/08:14,41,154`, `neuer-uebungstyp.md:21,61,73`, `pugling-reviewer.md:3`, `README.md:111`), die acht übersetzten Beispielzeilen einzeln gezählt. Kein Befund. Hinweise ohne Zählwert, vorbestehend: `docs/medien-bilder.md:89-215` (Plan, keine Anleitung) trägt deutsche `///`; die Prüfliste in `pugling-reviewer.md:36` prüft die Sprache nicht, obwohl die Beschreibung sie jetzt nennt.
