---
tags: [typ/story, status/idee, bereich/doku]
aliases: [wiki/08 deutsche summaries, Erweitern-Anleitung gegen die Englisch-Regel]
status: idee
prio: P3
art: Aufräumen
groesse: ""
wo: doku
migration: ""
vertragsbruch: ""
quelle: Reviewer zu B-166 (Nachtlauf 2026-10-23, Sprint 2)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-239 · Die Erweitern-Anleitung im Wiki zeigt deutsche `/// <summary>` – entgegen der Regel „Code-Doku auf Englisch"

`wiki/08-erweitern.md:41` sagt „mit deutschen `/// <summary>`", und die Code-Beispiele der Anleitung (`:45`,
`:66`, `:126`) tragen deutsche XML-Docs. Die Root-`CLAUDE.md` verlangt seit dem 2026-08-01 Englisch ohne
Ausnahme (239 Dateien umgestellt, `docs/translate.md`). Wer das Beispiel kopiert, liefert deutsche Swagger-Texte
– genau das, was die Umstellung beendet hat. Gefunden vom Reviewer, als B-166 in derselben Anleitung die
`Check`-Signatur berichtigte.

## Offene Punkte

1. Reicht es, die Beispiele zu übersetzen und den Satz in `:41` umzudrehen – oder nennt die Anleitung die
   Sprachregel noch an weiteren Stellen (`wiki/` greppen nach „deutsch")?
2. Gilt dasselbe für `wiki/09-llm-kochbuch.md` (Prompts, die Code erzeugen sollen)?

## Verlauf

- **2026-10-23** — angelegt aus dem Reviewer-Nit zu B-166 (Nachtlauf, Sprint 2). **Bewusst nicht in B-166
  mitgenommen:** dort ging es um tote Routen; die Sprache der Beispiele ist eine andere Regel und ein
  anderer Grep.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht und reicht weiter – neben `wiki/08-erweitern.md:14,41,44,69,124,154` tragen **aktive Anweisungen** die alte Regel: `.claude/commands/neuer-uebungstyp.md:61,73`, die Beschreibung von `.claude/agents/pugling-reviewer.md:3` („deutsche Docs“), `README.md:111`. `wiki/09-llm-kochbuch.md` ist sauber. Historische Notizen stehen lassen. XS–S, autonom.
