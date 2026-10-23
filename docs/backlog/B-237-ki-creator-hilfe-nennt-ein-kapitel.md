---
tags: [typ/story, status/abgenommen, bereich/ki-creator]
aliases: [Agent.Creator --chapter, Zielkapitel in der Konsolenhilfe]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachtlauf 2026-10-23, Sprint 2 (Sweep zu B-166)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-237 · Die Hilfe des KI-Creators bietet `--chapter <id>` an – ein Kapitel gibt es seit B-106 nicht mehr

## User Story

Als **Creator an der Konsole** möchte ich, dass `pugling-creator help` genau die Optionen nennt, die der
Agent versteht – mit dem Standard, der wirklich gilt –, damit ich die Unit, in die eine Übung soll, nicht
raten oder im Quelltext nachlesen muss.

## Ist-Stand am Code

Gelesen im zweiten Nachtlauf 2026-10-23 (`backend/Pugling.Agent.Creator/AgentCommands.cs`):

| Stelle | Befund |
| --- | --- |
| `:459-460` Hilfe | `--chapter <id>  Zielkapitel (Standard: erstes Kapitel des Fachs; allein angegeben bestimmt es auch das Fach)` – **wird nirgends geparst**: kein `command.Value("chapter")` in der Datei. Toter Hilfetext, kein Alias. |
| `:458` Hilfe | `--subject <id>  Zielfach (Standard: Fach des Profils, sonst erstes Fach mit Kapitel)` – `--subject` liest nur `profiles` (`:89`, Filter der Treffer); `create`/`briefing` leiten das Fach aus der Reihe ab (`ResolveSubjectIdAsync`). Der genannte Standard gilt für keinen Befehl. |
| `:366-367` Code | **`--series` und `--series-unit`** sind die Optionen, die den Ort bestimmen (`ResolveLocationAsync`, Aufrufer `:319`; Standard: Reihe des Profils, sonst erste Reihe mit Unit) – **in der Hilfe fehlen beide.** |
| `:325` Code | `--unit <id>` ist die Unit des Kindes für `exam`/`plan` (`UnitId`) – in der Hilfe korrekt beschrieben. |
| `README.md:146` | Tabellenzeile `--subject`/`--chapter` · „Ort im Katalog" · „Fach des Profils, sonst erstes Fach mit Kapitel" – dieselbe Unwahrheit. `README.md:50` „Fach/Kapitel + vorhandene Übungen". |
| `Briefing/CreatorBriefing.cs:133` | Prompt-Überschrift „Bereits vorhandene Übungen in diesem Kapitel" – die Liste kommt aus der Unit. |
| `Drafting/DraftRules.cs:51` | Regelverstoß „Der Titel '…' existiert im Kapitel bereits" – geprüft wird gegen die Unit. |
| `Pugling.Api.Tests/CreatorAgentTests.cs:53` | Test-Helfer `FreshChapterAsync` legt Fach → Reihe → Unit an (33 Aufrufer); Test `:441` heißt `Ein_unbekanntes_Kapitel_…` und prüft eine unbekannte **Unit** (`--series-unit 999999`). |

Nicht betroffen: `backend/Pugling.Agent.Creator/CLAUDE.md` und der Skill `ki-creator` (gegrept, 0 Treffer);
`AgentCommands.cs:359` sagt ausdrücklich „not a chapter" und bleibt.

## Die echte Lücke

Die Hilfe ist ein Dokument, das beim **Ausführen** nicht auffällt – `--chapter 5` wird still ignoriert
(`CommandLine` meldet unbekannte Optionen nicht), und die Übung landet in der ersten Reihe mit Unit. Das ist
derselbe „stille Griff ins falsche Regal", den `ResolveLocationAsync` für `--series-unit` ohne `--series`
ausdrücklich abgestellt hat – nur dass die Hilfe den Nutzer selbst dorthin schickt.

## Akzeptanzkriterien

1. `pugling-creator help` nennt `--chapter` nicht mehr und nennt `--series <id>` und `--series-unit <id>` mit
   dem Standard, der in `ResolveLocationAsync` gilt; die `--subject`-Zeile sagt, dass sie nur `profiles`
   filtert. Rote Zahl: `help | grep -c -- "--chapter"` 1 → 0, `grep -c -- "--series-unit"` 0 → 1.
2. `README.md:146` und `:50` sagen dasselbe wie die Hilfe.
3. Die zwei deutschen Laufzeit-Strings sagen „Unit" statt „Kapitel" (`CreatorBriefing.cs:133`, `DraftRules.cs:51`).
4. `FreshChapterAsync` heißt `FreshUnitAsync`, der Test `:441` `Eine_unbekannte_Unit_…`;
   `rg -i "chapter|kapitel" backend/Pugling.Agent.Creator backend/Pugling.Api.Tests/CreatorAgentTests.cs`
   trifft nur noch `AgentCommands.cs:359` („not a chapter") und `CurrentChapter` in `CreatorBriefing.cs:69/:99`
   (lebendes Vertragsfeld des Lehrbuchs, `ProfileDtos.cs:21`).
5. Suite grün (Zahl im Verlauf).

## Offene Punkte

1. Was macht `--chapter` heute? **Beantwortet im Ist-Stand:** nichts – es wird nicht geparst.
2. Dieselbe Formulierung in `CLAUDE.md`/Skill? **Beantwortet:** nein (gegrept).

## Entscheidungen

Gegrillt im zweiten Nachtlauf 2026-10-23 (Freigabe 1, `art: Defekt`).

1. **`--chapter` wird gestrichen, nicht als Alias auf `--series-unit` behalten.** Begründung: es war nie
   geparst – ein Alias wäre eine *neue* Fähigkeit (Produktentscheidung, gesperrt), das Streichen einer toten
   Hilfezeile ist die Herstellung der Wahrheit. **Kosten:** wer `--chapter` aus Gewohnheit tippt, bekommt
   weiter keine Fehlermeldung (`CommandLine` kennt keine Unbekannt-Prüfung) – das ist ein eigener Befund, der
   hier **nicht** mitgebaut wird (siehe Verlauf).
2. **Die Hilfe bekommt `--series`/`--series-unit` mit dem echten Standard** („Reihe des Profils, sonst erste
   Reihe mit Unit; `--series-unit` allein bestimmt die Reihe"), die `--subject`-Zeile wird auf `profiles`
   eingegrenzt. Begründung: AK 1 – die Hilfe soll das sagen, was `ResolveLocationAsync` tut, Wort für Wort
   nachgelesen. **Kosten:** zwei Zeilen Hilfetext, eine README-Zeile.
3. **Strings auf „Unit"** (AK 3): deutsche Produkt-/Diagnose-Strings bleiben deutsch (CLAUDE.md-Regel), nur
   das falsche Wort geht. **Kosten:** keine; kein Test prüft den Wortlaut (gegrept: `existiert im` 0 Treffer in
   Tests).
4. **Test-Namen und Helfer umbenennen** (AK 4): `FreshChapterAsync` → `FreshUnitAsync` (33 Stellen, `sed`),
   Testname und Fixture-Name „Kapitel-Wahl" → „Unit-Wahl". Begründung: Code-Namen sind Code-Doku; ein Helfer,
   der „Chapter" heißt und eine Unit anlegt, ist die Fehlerklasse dieser Nacht im Kleinen. **Kosten:** ein
   größerer, mechanischer Diff in einer Testdatei.
5. **Rote Probe ist die Hilfe selbst** (AK 1), gemessen über `dotnet run -- help` vor und nach der Änderung;
   für AK 3/4 ist die Suite der Beleg (Umbenennung ohne Verhalten).

## Schätzung

**Größe S** (eine Hilfe, ein README, zwei Strings, eine Testdatei mit 33 mechanischen Stellen). `wo: backend`,
keine Migration, kein Vertrag.

**Angriffsplan:** Hilfe per `dotnet run -- help` zählen (rot) → `AgentCommands.cs:456-460` umschreiben →
README `:50`/`:146` → zwei Strings → `sed` über `CreatorAgentTests.cs` → Build (Hook) → Hilfe erneut zählen →
Suite → Reviewer → Commit.

**Risiko:** der Hilfetext ist ein Raw-String-Literal mit Spaltenausrichtung – eine Zeile zu lang verschiebt
die Optik, nicht die Wahrheit.

**Testweg:** Hilfe-Zählung 1/0 → 0/1, Grep aus AK 4, Suite.

## Verlauf

- **2026-10-23** — **abgenommen** (zweiter Nachtlauf, Sprint 5). `pugling-reviewer`: `--series`/`--series-unit`
  gelten für alle vier Verben (`BuildRequestAsync :319`, Aufrufer briefing/create/exam/plan), `--subject` liest
  nur `profiles` (`:89`), `--chapter` wurde nie geparst; **zwei Funde mit Codewirkung** (Zähler 2 von 5), beide
  Fehlerklasse B-112 in der Story, die sie benennt: die neue Hilfe sagte „das Fach kommt aus der Reihe" – der
  Code nimmt `profile?.SubjectId ?? ResolveSubjectIdAsync` (`:320`), also „aus dem Profil, sonst aus der Reihe";
  und „`--series-unit` allein bestimmt die Reihe" gilt nur ohne Profil-Reihe (`:366/:372/:389-392`) – beides
  umformuliert, README `:146` mit. Ein Fund am Story-Text: AK 4 nannte `CurrentChapter` nicht als Ausnahme
  (jetzt drin). Hilfe nach den Korrekturen erneut gezählt: `--chapter` 0, `--series-unit` 1.
  **Rollengang Creator (Konsole):** `pugling-creator help` gelesen – die Zeilen 22–27 sagen, was
  `ResolveLocationAsync` tut; Vater/Sohn Regressionszeugen (Suite). Commit `4954bd2` (Protokoll `docs/pm-sitzung-2026-10-23-nacht.md`, „Sprint 5").
- **2026-10-23** — **gebaut** (zweiter Nachtlauf, Sprint 5). **Rote Probe (erwartet/gemessen):** `dotnet run
  --project backend/Pugling.Agent.Creator -- help`, vorher `grep -c -- "--chapter"` = **1** (erwartet 0),
  `grep -c -- "--series-unit"` = **0** (erwartet 1); nachher **0 / 1**, und `--subject` steht als
  „nur profiles". Hilfe gegen `ResolveLocationAsync` (`:363-395`) Wort für Wort: Reihe des Profils, sonst
  erste Reihe mit Unit; erste Unit der Reihe; `--series-unit` allein bestimmt die Reihe. README `:50`/`:146`
  nachgezogen (zwei Zeilen statt einer), Strings `CreatorBriefing.cs:133`/`DraftRules.cs:51` auf „Unit",
  `FreshChapterAsync` → `FreshUnitAsync` (33 Stellen), Test `Eine_unbekannte_Unit_…`, Fixture „Unit-Wahl",
  Kommentar `CreatorAgentTests.cs:325`. Grep AK 4 über `Pugling.Agent.Creator` + `CreatorAgentTests.cs`:
  Rest = `AgentCommands.cs:359` („not a chapter", gewollt) und `CurrentChapter` (lebendes Vertragsfeld des
  Lehrbuchs, `CreatorBriefing.cs:69/:99`) – beides Ausschluss. Suite **879/879**.

- **2026-10-23** — ausformuliert, gegrillt und geschätzt im zweiten Nachtlauf (Sprint 5, Freigabe 1): `AgentCommands.cs` gelesen – `--chapter` wird nicht geparst, `--series`/`--series-unit` fehlen in der Hilfe; `unverifiziert` damit aufgehoben. **Nebenfund, nicht mitgebaut:** `CommandLine` meldet unbekannte Optionen nicht – ein Tippfehler (`--serie`) läuft still in den Standard; das ist eine eigene Story, wenn der Nutzer sie will.
- **2026-10-23** — angelegt im Nachtlauf (Sprint 2) aus dem B-166-Sweep über `chapter` in Backend-Kommentaren.
  **Bewusst nicht in B-166 mitgenommen:** dort geht es um Doku-Routen; hier hängt ein Verhalten des
  Konsolenagenten dran, das erst zu lesen ist.
