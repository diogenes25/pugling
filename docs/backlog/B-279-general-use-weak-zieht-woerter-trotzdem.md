---
tags: [typ/story, status/abgenommen, bereich/ki-agent, rolle/creator]
aliases: [--general --use-weak zieht die schwachen Wörter trotzdem heran]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: pugling-reviewer zu B-21 (dritter Nachtlauf, docs/pm-sitzung-2026-10-23-nacht.md), angelegt in Lauf C (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-10-28"
---

# B-279 · `--child X --general --use-weak` zieht die schwachen Wörter weiter in den Pflicht-Wortschatz

`--general` lässt beim KI-Creator das Kind als Zuschnitt fallen: Die Übung soll allgemein sein, nicht für dieses
Kind. `BriefingBuilder.BuildAsync` übernimmt die schwachen Wörter des Kindes aber weiter in `RequiredWords`
(`backend/Pugling.Agent.Creator/Briefing/BriefingBuilder.cs`, Vorrang „ausdrücklich > schwache Wörter des Kindes“),
sobald `--use-weak` gesetzt und ein Kind angegeben ist. Eine „allgemeine“ Übung trägt dann den Förderwortschatz
eines bestimmten Kindes. Das Verhalten ist älter als [B-21](B-21-ki-creator-foerdermodus.md), der Reviewer hat es dort
benannt. Aus Code gelesen, nicht gemessen.

Zu entscheiden: Bricht die Kombination ab wie `--use-weak` ohne `--child`, oder wird `--use-weak` unter `--general`
still ignoriert? Abbrechen ist die Linie von B-21 AK 4.

## User Story

Als Creator, der eine **allgemeine** Übung aus dem Stoff eines Kindes erzeugt (`--child X --general`), möchte ich nicht
versehentlich den Förderwortschatz genau dieses Kindes im gemeinsamen Katalog haben.

## Ist-Stand am Code

`BriefingBuilder.BuildAsync` (`Briefing/BriefingBuilder.cs:49-64` vor dem Fix) lud mit `--use-weak` die schwachen Wörter
des Kindes und übernahm sie in `RequiredWords`, auch wenn `request.General` das Kind als Zielgruppe fallen ließ.

## Akzeptanzkriterien

1. `--general` zusammen mit `--use-weak` ist eine Fehlbedienung, deren Meldung `--general` nennt – für jedes Verb.

## Entscheidungen

1. Abbrechen statt still ignorieren – die Linie von B-21 AK 4 (`--use-weak` ohne Kind bricht ab). Kosten: eine
   Kombination, die vorher „funktionierte“, ist jetzt ein Fehler; sie tat nie, was ihr Name versprach.
2. Die Prüfung sitzt im `BriefingBuilder`, nicht in `AgentCommands`: so gilt sie auch für `plan`/`exam` und den
   direkten Pipeline-Aufruf.

## Schätzung

XS, `backend` (Agent), keine Migration, kein Vertragsbruch. Testweg: Integrationstest über `pipeline.BriefAsync`.

## Verlauf

- **2026-10-26** — angelegt in Lauf C ([Protokoll](../pm-sitzung-2026-10-26-tag.md)) aus dem Review-Hinweis zu B-21
  vom 2026-10-23 („eigene Story wert“).
- **2026-10-28** — in Lauf H, Teil 1 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)) gegrillt (Freigabe 1): **abbrechen**, die Linie von B-21 AK 4 – still
  ignorieren hieße, dass einer der beiden Schalter lügt. Die Prüfung sitzt am Anfang von `BriefingBuilder.BuildAsync`,
  damit sie für alle Verben (auch `plan`/`exam` über `DeriveTitleAsync`, vor den Teilen) und den direkten Pipeline-Aufruf
  gilt. **Gemessen:** `Foerdermodus_mit_general_bricht_mit_Meldung_ab` → **1 failed** („No exception was thrown“).
  README des Agenten: `--use-weak` „verträgt kein `--general`“. Suite **1000/1000 `exit=0`**. Rollengang: Tests sind
  der Weg. **`abgenommen`.**
- **2026-10-28** — Commit `c195ef2` (Lauf H, Teil 1).
- **2026-10-28** — nachgeschaut (Lauf H, Teil 2, frischer `pugling-reviewer` am Commit `c195ef2`, Dateien im Stand des Commits): Wächter vor jedem API-Aufruf (`BriefingBuilder.cs:25-28`), alle
  Verben laufen über `BuildAsync` (`exam`/`plan` über `DeriveTitleAsync` vor jedem Seiteneffekt); Mutanten „Wächter weg“,
  „nur ohne Kind“, „still ignorieren“ je rot hergeleitet. Kein Entgleiten; 🟢: ohne passendes Profil fehlt unter
  `--child X --general` die Lehrbuch-Zeile, obwohl ein Kommentar das Gegenteil sagt (schon vorher).
