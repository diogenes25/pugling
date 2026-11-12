---
tags: [typ/story, status/abgenommen, bereich/ki, rolle/creator]
aliases: [Briefing wählt das Profil nach Punkten, werkunabhängiges Profil landet im ersten Regal]
status: abgenommen
prio: P2
art: Defekt
groesse: M
wo: "backend"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 35)
unverifiziert: false
entgangen_bei: [B-481]
nachgeschaut: 2026-11-12
grund: ""
ersetzt_durch: []
---

# B-484 · Der KI-Creator wählt Profil und Reihe an zwei Stellen

Nachschau von Sprint 35 (frischer `pugling-reviewer` an `d941ca1`; gelesen und gegrept, Suite 1343/1343, kein
eigener Lauf der Fälle). B-481 hat die **Unit** an eine Stelle gezogen – `create`, `exam`, `plan` und `briefing`
laufen alle über `BuildRequestAsync`. Das **Profil** und die **Reihe ohne Profilreihe** werden aber weiter an
zweiter Stelle mit eigener Regel bestimmt. Dieselbe Klasse wie B-473 und B-481: zwei Leser, zwei Regeln.

1. **Das Briefing wählt das Profil nach Punkten** (`Briefing/BriefingBuilder.cs:238-241`): bei `--child` mit
   `--series` oder `--series-unit` überspringt `BuildRequestAsync` die Profilwahl (`AgentCommands.cs:429`), und
   `ResolveProfileAsync` nimmt `matches[0]` ohne Blick auf die Reihe. Ein Profil auf einer anderen Reihe Y, das über
   Fach/Klasse/Schulart mehr Punkte hat, liefert dann Stoff aus Y für eine Übung, die in Reihe X entsteht; mit
   `--unit` bricht der Lauf mit „Unit … gehört nicht zur Reihe Y" ab (`BriefingBuilder.cs:266-270`). Die Tests legen
   das Profil immer auf die genannte Reihe.
2. **`--child --profile <Profil ohne Reihe>` landet im ersten Regal des Katalogs** (`AgentCommands.cs:426-434`,
   `:549-566`): ohne Profilreihe nimmt `ResolveLocationAsync` die erste Reihe mit Units, während das Briefing den
   Stoff aus dem Buch des Kindes holt. Werkunabhängige Profile gibt es ausdrücklich (`profiles` zeigt sie so an).

## Akzeptanzkriterien

1. **Ein Ort, ein Profil:** mit `--child` und genanntem Ort (`--series`, `--series-unit`) spricht das Profil auf
   dieser Reihe – auch wenn ein Lehrer eines anderen Werks mehr Punkte hat; gibt es auf der Reihe keines, arbeitet der
   Agent ohne Profil, statt einen fremden zu borgen. Der Stoff im Prompt ist der der Unit, in der die Übung entsteht.
2. **Ein werkunabhängiges Profil rät kein Regal:** mit `--child` die Reihe seines Buchs im Fach des Profils (an seiner
   Unit); kein Kind, kein Buch im Fach oder mehrere → Fehlbedienung mit dem Ausweg `--series`.
3. **Zwei Units für eine Übung** (`--unit` ≠ `--series-unit`) sind eine Fehlbedienung.

## Entscheidungen (autonom gegrillt, `art: Defekt`)

1. **Der Stoff kommt aus der Unit, in der die Übung entsteht** – das Briefing liest Reihe und Unit aus dem Auftrag,
   nicht noch einmal aus Profil oder Kinderbuch (`ResolveMaterialAsync` entfällt). Die Klasse seit B-473 waren zwei
   Stellen, die über dieselbe Größe entscheiden; eine davon wegzunehmen schließt sie, eine dritte Regel nicht.
2. **Das ungenannte Profil wählt weiter das Briefing**, nicht `BuildRequestAsync`: `exam` und `plan` und direkte
   Aufrufe der Pipeline gehen alle durch das Briefing, also ist dort die eine Stelle. Regel: das bestpunktende Profil
   *auf der Zielreihe*, sonst eines ohne Reihe, nie eines auf einer anderen (Review-Fund 1).
3. **Ohne Kind braucht ein werkunabhängiges Profil `--series`** – auch die allgemeine Übung bekam bisher das erste
   Regal des Katalogs.
4. Vier bestehende Tests legten Ablage (frische Unit) und Stoff (Unit des Profils) auseinander, wie es die Konsole nie
   tut; sie legen die Übung jetzt in die Unit des Profils, ihre Zusicherungen bleiben unverändert.
5. **Werk und Unit sind eigener Teil jedes Briefings** (`MaterialFacts`), nicht des Profils (`ProfileFacts` ist nur
   noch der Lehrer): auch ohne Profil stehen Lehrwerk und Unit-Stoff im Prompt, und die Quelle der Übung ist immer das
   Werk der Ablage. Das freie Kinderbuch (`ChildFacts.PrimaryTextbook`) entfällt als zweite Buchquelle (Review-Fund 2);
   ein Test, der genau sie festhielt (`Das_Briefing_traegt_…`), prüft jetzt das Gegenteil.
6. **`--profile` einer anderen Reihe neben `--series`** ist eine Fehlbedienung (Review-Fund 3).

## Reste aus derselben Nachschau (🟢)

- `exam` und `plan` sind nur mit `--series-unit` getestet; die neue Unit-Regel läuft dort nie. Bei `plan` hängen
  die Buch-Wortlisten an der Unit.
- `--profile <ohne Reihe> --unit U` ohne Kind meldet „Unit gehört nicht zur Reihe …" statt „--unit setzt eine
  Lehrwerk-Reihe voraus" (`BriefingBuilder.cs:257-259`); verschwindet mit Punkt 2.
- Zwei Profile auf **derselben** Reihe entscheiden still nach Punkten (`AgentCommands.cs:512`) – so gewollt
  („zwei *Bücher*"), nur festgehalten.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 35 im Backlog-Lauf).
- **2026-11-12** — ausformuliert, gegrillt (autonom, `art: Defekt`) und geschätzt (M) im Sprint 37; in Arbeit.
  Vorab nach jeder *Entscheidung* über Reihe, Unit und Profil gesucht (geschärfter Vorschlag CR): `BuildRequestAsync`
  (Reihe, Unit, Profil mit Kind ohne Ort), `ResolveLocationAsync` (erstes Regal), `BriefingBuilder.ResolveProfileAsync`
  (Profil nach Punkten), `BriefingBuilder.ResolveMaterialAsync` (Reihe aus Profil oder Kinderbuch, Unit erneut),
  `ChildFacts.PrimaryTextbook` (Prompt-Text und – wie das Review zeigte – auch die Quelle der Übung). `fill` nennt beides selbst.
- **2026-11-12** — Fälle und Belege (`CreatorAgentTests`, Aufbau `TwoTeachersAsync` mit Zusicherung, dass der fremde
  Lehrer mehr Punkte hat):
  - AK 1, `--series` / `--series-unit` → `Mit_genanntem_Ort_spricht_der_Lehrer_dieses_Buchs` (Theory): Persona des
    Buchlehrers im System-Prompt, „Unit 2 – Buchstoff" im Prompt, nichts vom Fremdwerk, Übung in Unit 2.
  - AK 1, kein Profil auf der Reihe → `Ohne_Lehrer_dieses_Buchs_borgt_der_Agent_keinen_fremden`.
  - AK 2, Buch im Fach (dazu ein Buch in anderem Fach) → `Werkunabhaengiges_Profil_legt_im_Buch_des_Kindes_ab`.
  - AK 2, ohne Kind / ohne Buch / zwei Bücher → `Werkunabhaengiges_Profil_ohne_eindeutiges_Buch_ist_eine_Fehlbedienung`.
  - AK 3 → `Zwei_verschiedene_Units_sind_eine_Fehlbedienung`; im Briefing → weiter `Eine_Unit_aus_einer_fremden_Reihe_…`.
  - 🟢 der Nachschau, `plan` mit `--child --series` → `Ein_Plan_mit_Reihe_fuellt_die_Unit_des_Kindes`.
  - Ungetestet (🟢, gelesen): zwei Bücher des Kindes auf *derselben* Reihe im Fach zählen als eines (`Distinct`).
- **2026-11-12** — `pugling-reviewer`: kein 🔴, vier 🟡, alle behoben – **Funde 1–4 des Sprints 37 (zählen)**:
  (1) ein werkunabhängiges Profil fiel heraus, und mit ihm der Unit-Stoff, der nur über `ProfileFacts` in den Prompt
  kam; (2) ohne Profil kamen Buchzeile und **Quelle der Übung** aus `PrimaryTextbook` – der zweiten Buchregel;
  (3) `--profile` auf Reihe Y neben `--series X` ging durch; (4) der Test „ohne Buchlehrer" ließ die Mutante „auch
  Profile ohne Reihe" überleben. 🟢 offen: `SeriesOfChildsBookAsync` filtert auf `b.SubjectId` (ein katalogisiertes
  Buch ohne Fach fiele heraus); `ProfileFacts`-freie Prompts zeigen keine Klassenstufe des Lehrers.
- **2026-11-12** — Belege nach den Review-Fixes: neu `Ohne_Lehrer_dieses_Buchs_spricht_ein_werkunabhaengiger_statt_eines_fremden`,
  `Ohne_jeden_Lehrer_bleibt_der_Stoff_der_Unit_und_die_Quelle_des_Werks` (Quelle exakt; „kein Profil" über die
  Pipeline ohne Kind, weil die geteilte DB fachneutrale Profile ohne Reihe hält – gefunden, weil M15 erst grün
  blieb), `Profil_einer_anderen_Reihe_neben_genanntem_Ort_ist_eine_Fehlbedienung`; `Mit_genanntem_Ort_…` prüft das
  Thema der Unit statt ihres Labels (das Label steht ohnehin im Prompt – M8 fiel erst am falschen Ort).
- **2026-11-12** — Mutanten nach der letzten Code-Änderung (`mutant-probe.py`, `e63dbba + Arbeitsbaum`, 09:31; M8 und
  M15 nach der Test-Schärfung erneut 09:35). Ein erster Lauf um 09:04 ist **verworfen**: ein Test-Tor einer parallelen
  Sitzung sperrte die Build-Ausgabe, sechs Zeilen „kompiliert nicht" und eine ungültige Grundlinie.

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  | M1 Profil ohne Reihe -> erstes Regal | AgentCommands.cs:439 | red | red |  |
  | M2 genannter Ort zaehlt nicht | AgentCommands.cs:439 | red | red |  |
  | M3 mit Kind trotzdem Fehlbedienung | AgentCommands.cs:440 | red | red |  |
  | M4 Buch jedes Fachs | AgentCommands.cs:526 | red | red |  |
  | M5 zwei Buecher -> erstes | AgentCommands.cs:537 | red | red |  |
  | M6 --unit gegen --series-unit egal | AgentCommands.cs:453 | red | red |  |
  | M7 Briefing: fremde Unit egal | BriefingBuilder.cs:67 | red | red |  |
  | M8 Briefing: Stoff aus erster Unit | BriefingBuilder.cs:103 | red | red | (Nachlauf 09:35) |
  | M9 Briefing: Profil nach Punkten | BriefingBuilder.cs:249 | red | red |  |
  | M11 Briefing: kein werkunabhaengiger | BriefingBuilder.cs:250 | red | red |  |
  | M12 Briefing: jeder als Rueckfall | BriefingBuilder.cs:250 | red | red |  |
  | M13 Profil fremder Reihe erlaubt | AgentCommands.cs:447 | red | red |  |
  | M14 Quelle nur Reihe | CreatorBriefing.cs:87 | red | red |  |
  | M15 Stoff nur mit Profil | CreatorBriefing.cs:115 | red | red | (Nachlauf 09:35) |
  | M10 plan ohne Unit des Kindes | AgentCommands.cs:463 | red | red |  |

- **2026-11-12** — **abgenommen.** Volle Suite nach der letzten Änderung: 1354/1354, Build ohne Warnung. Review-Zähler
  Sprint 37: 4 von 5. **Rollengang (Creator) ohne Live-Lauf**: `CreatorAgentTests` fährt die Konsole mit
  `FakeChatClient` gegen die In-Process-API; Ollama und die echte DB nicht.
- **2026-11-12** — Commit der Abnahme: `1c90c8d`.
- **2026-11-12** — Nachschau (Sprint 38): ein werkunabhängiges `--profile` neben `--series` prüft das Fach
  nicht – die Regel gilt nur auf dem abgeleiteten Pfad → [B-488](B-488-ki-creator-profil-fach-neben-genannter-reihe.md).
