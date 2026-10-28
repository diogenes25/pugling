---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-10-28, Lauf H]
---

# PM-Sitzung: Nachtlauf 2026-10-28 – Lauf H

**Datum:** 2026-10-28  ·  **Moderation:** PM (unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die baubaren `Defekt`-Stories nach Themen in Sprints fahren, beginnend mit dem Kandidaten aus Lauf G (B-311).

Auftrag: „Starte einen Nachtlauf" – der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–10.
Ausgangslage: HEAD `6b6eb34`, Baum sauber, 90 Commits ungepusht. Keine Paket-Vorgabe; Auswahl nach Thema, erster
Kandidat laut [Ende Lauf G](pm-sitzung-2026-10-27-nacht.md#ende-lauf-g) B-311.

**Ungeprüfte abgenommene Stories zu Beginn** (Index „Nachgeschaut 221 von 226"): B-305, B-315, B-316 (Lauf G, Teil 2)
und B-306, B-307 (Lauf F, Teil 4) – die Nachschau des ersten Sprints nimmt alle fünf.

## Lauf H, Teil 1 — Der Agent findet, was es gibt, und sagt, was er tut (B-311, B-312, B-279, B-264), Zähler 1

**Sprint-Ziel:** *Der Creator, der den KI-Creator mit einer gültigen Id aufruft, wird nie als Tippfehler abgewiesen,
mit einer falschen Id nie belogen – und eine allgemeine Übung ist für niemanden zugeschnitten und doch in die Interessen
eingekleidet, die er vorgibt.* Umfang: [B-311](backlog/B-311-agent-reihensuche-liest-nur-50-reihen.md) (P2),
[B-312](backlog/B-312-agent-profiles-unbekanntes-fach-meldet-kein-profil.md),
[B-279](backlog/B-279-general-use-weak-zieht-woerter-trotzdem.md),
[B-264](backlog/B-264-interessen-ohne-kind-kleiden-nicht-ein.md) – alle `Defekt` auf `idee`, unter Freigabe 1 selbst
ausformuliert, gegrillt und geschätzt (Refinement: Ist-Stand je Story am Code belegt, siehe die Abschnitte in den
Story-Dateien). Draußen: [B-313](backlog/B-313-agent-unbekannter-typ-je-teil-statt-fehlbedienung.md) (`Frage`, gesperrt)
und B-310 (Vertragsfrage, beim Nutzer).

**Entwickler-Brief:** Alles im Agenten, kein Server-Code. Quelle der Wahrheit bleibt die API: eine genannte Id wird beim
Server erfragt (`GET series/{id}`, `GET subjects/{id}`), nicht in einer geblätterten Liste gesucht; 404 → Fehlbedienung
(Exit 2) mit der Id, die Linie von B-301/B-307/B-308. Die Briefing-Regeln (B-279, B-264) sitzen im `BriefingBuilder`,
damit sie für jedes Verb gelten. Testweg: Integrationstests in `CreatorAgentTests` mit `FakeChatClient`, rote Probe vor
jedem Fix.

### Iteration

- Sechs neue Fälle gegen den Ausgangsstand: **6 failed** an der erwarteten Stelle (Reihen-Fall an der ersten Zeile,
  `AgentCommands.cs:473`). Danach `CreatorAgentTests` **75/75**.
- Vor dem Review selbst gefunden: mit Seitengröße 200 bliebe die Mutante „nur eine Seite“ grün (die Test-DB hat weniger
  als 200 Reihen) → Seitengröße 50; Mutante danach **1 failed**. Weitere Mutanten (B-264: „neutral immer“, „neu
  immer“, „Beschreibung alt“) je **1 failed**, Kontrolle **6 passed**.

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `pugling-reviewer` 🟡 | `--child X --general --interests` ungetestet; Mutante `request.ChildId is null` blieb grün | Prompt-Zusicherung im bestehenden Fall; Mutante **1 failed** | ja |
| – | `pugling-reviewer` 🟢 | Reihen-Fall prüft nicht, *welche* Unit gewählt wurde | zurückgestellt: `Console.Out` abzugreifen ist prozessweit und verträgt sich nicht mit parallelen Testklassen | nein |
| – | `pugling-reviewer` 🟢 | Wortlaut „Kein Profil für Fach …“ ungetestet | als Konsolenausgabe benannt | nein |
| – | `pugling-reviewer` 🟢 | `\n` in `AppendLine`, veralteter Kommentar, `create`-Ausgabe ohne Einkleidung, Formatierungsrauschen | alle vier behoben | nein |

**Zähler: 1.** Suite **1000/1000** (`exit=0`), `dotnet format` an den geänderten Dateien sauber. Rollengang: der Agent
hat keine Oberfläche, die Integrationstests fahren seine Verben gegen die echte API (in-process) – das ist der Weg;
Vater und Sohn Regressionszeugen (Suite grün, kein Server- oder Frontend-Pfad im Diff). **B-311, B-312, B-279, B-264
`abgenommen`**, dazu **B-318** aus der Nachschau (siehe unten).

### Retrospektive

**Nachschau:** fünf bis dahin unbeobachtete Stories, je von einem frischen Reviewer ohne Testlauf. Lauf F, Teil 4
(`d8e4a0a`, B-306/B-307, `pugling-reviewer`): jede Strategie auf `null`-Dereferenzierung nach bestandener Prüfung, jedes
Verb mit unbekannter Reihe, Mutante je Fall hergeleitet. **Entgangen:** [B-318](backlog/B-318-null-element-ohne-fall-in-drei-strategien.md)
(kein Fall für `null` in Vokabel/Übersetzung/Grammatik; im Sprint behoben, drei Mutanten je **1 failed**) und der
`--series`-ohne-Unit-Pfad, identisch mit B-311 (dort `entgangen_bei` ergänzt). `Assert.Equal(0, chat.Calls)` in drei
Fällen als Absichtserklärung beschriftet. Lauf G, Teil 2 (`43b5eef`, B-305/B-315/B-316, `frontend-reviewer`): Spielweg
auf deutsche Literale gelesen, Vater-Vorschau auf Scope, Plural-Zeilen nachgerechnet. **Entgangen:**
[B-319](backlog/B-319-zwei-zusicherungen-aus-b-305-tragen-nicht.md) (zwei Zusicherungen aus B-305 können nicht fallen) und
[B-320](backlog/B-320-klausur-fehler-erreicht-den-bildschirm-nicht.md) (P2, älter: ein Fehler beim Beantworten in der
Klausur erreicht nie den Bildschirm). Beide Increments stammen aus **früheren** Läufen – Limit 2 greift nicht; beide
Stories gehen in Teil 2. `nachgeschaut` an B-305, B-306, B-307, B-315, B-316. Index danach: **Nachgeschaut 226 von 231**
(die fünf offenen sind die dieses Sprints).

**Was die eigenen Tore durchgelassen haben:** dieselbe Familie wie in Lauf F und G – eine Regel an einer Stelle belegt,
ihr Geschwister nicht: B-306 testete den Lückentext, nicht die drei anderen Strategien; B-264 hier zuerst den kindlosen
Pfad, nicht `--general` mit Kind. Neu: ein **`.or(…)` in einer E2E-Zusicherung** (B-319) ist derselbe Zweig wie das
`if (await … .isVisible())` aus Vorschlag (T) – er lief nie, und (T) in der vorgeschlagenen Fassung hätte ihn nicht
gefangen.

**Vorschlag, nicht gelandet (Freigabe 3):** **(U)** Vorschlag (T) auf `.or(` innerhalb eines `expect` in
`frontend/e2e/*.ts` ausdehnen. Bestand gemessen: drei Stellen; zwei sind ehrliche Wartepunkte auf „leer *oder* erste
Zeile“ (`shop-verlauf.spec.ts:54`, `shop-verlauf-wiederholen.spec.ts:56`) und bräuchten eine begründete Ausnahme, die
dritte ist B-319.
