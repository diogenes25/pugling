---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-09-01, Die Oberflaeche verschweigt nichts mehr]
---

# PM-Sitzung 2026-09-01 — Nachtlauf: die Oberfläche verschweigt weder Erfolg noch Fehler

## Auftrag und Freigaben

Unbeaufsichtigter Backlog-Lauf nach `pm-loop` und [docs/backlog/README.md](backlog/README.md).
**Alle acht Freigaben** aus [docs/nachtlauf.md](nachtlauf.md) gelten, vom Nutzer für diesen Lauf erteilt.

**Abweichung zu Freigabe 6, vor dem Start festgestellt:** In dieser Sitzung ist **keine Browser-Verbindung**
verfügbar. Der Rollengang läuft also durchgehend über den dokumentierten Ersatz — bevorzugt eine eigene
Playwright-E2E-Spec (die *ist* der Rollengang, nur wiederholbar), sonst Integrationstest oder Live-Probe.
Jede Story benennt ihren Ersatz im `## Verlauf`.

## Ausgangslage (gemessen, nicht geschätzt)

- Index: **61 offen**, 112 abgenommen, 12 verworfen.
- `geschaetzt`: **27** — zwei davon blockiert (B-07 wartet auf einen Handgriff an der Azure-Instanz,
  B-31 auf ein echtes Handy).
- `ausformuliert`: **15**, ausnahmslos `Defekt`/`Aufräumen` → nach Freigabe 1 vollständig autonom grillbar.
- `idee`: **19**, davon 11 `Defekt`/`Aufräumen` (baubar nach Refinement), 8 `Wunsch`/`Frage` (**gesperrt**).
- `in-arbeit`: **0** — kein Rest aus einem früheren Lauf.
- Nachschau beim Start: **107 von 112**, Arbeitsvorrat 5 (B-165, B-172, B-180, B-182, B-45).
  Davon sind **B-172 und B-45 die Arbeit des vorigen Sprints** — sie sind damit die Pflicht-Nachschau
  der ersten Retrospektive dieses Laufs.

## Was dieser Lauf nicht anfassen darf

- **B-170** (P1, `ausformuliert`) — der verbliebene Kern ist genau die Schema-Entscheidung
  (`ExerciseCategory.CreatedByAdultId` ja/nein), die der Nutzer am 2026-08-14 ausdrücklich auf den Tag
  gelegt hat. `wartet_auf` benennt das. Der nachts baubare Teil ist in B-178 abgeflossen und **abgenommen**.
- **8 `Wunsch`/`Frage` unterhalb `geschaetzt`** — Freigabe 1/8 sperrt sie. Die gebrauchten Entscheidungen
  stehen unten unter „Offene Punkte für den Morgen".

## Die Messung, die den roten Faden trägt

Vor dem Sprint-Zuschnitt gemessen (`useAsync`-Aufrufstellen im gesamten Frontend, je Datei geprüft, ob der
zugehörige `.error` irgendwo in derselben Datei gerendert wird):

**64 von 105 rendern ihren Fehler — 41 nicht.**

Das beantwortet den offenen Punkt 2 von [B-174](backlog/B-174-arten-liste-verschweigt-ihren-fehler.md)
(„nur hier oder als Muster?") und den offenen Punkt 2 von
[B-162](backlog/B-162-assistent-nennt-den-leeren-katalog-als-ursache.md) mit einer Zahl statt einer
Vermutung: Es **ist** ein Muster, und es ist mit 41 Stellen zu breit für einen Sprint. Der Sprint nimmt
darum die Stellen, an denen das Schweigen **tragend** ist (der Hauptinhalt einer Fläche), und legt den
Rest als eigene Story mit der gemessenen Zahl ab.
