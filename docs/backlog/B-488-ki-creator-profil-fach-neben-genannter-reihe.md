---
tags: [typ/story, status/abgenommen, bereich/ki, rolle/creator]
aliases: [werkunabhängiges Profil neben fremder Reihe, Fach des Profils gegen Fach der Reihe]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: "backend"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 37)
unverifiziert: false
entgangen_bei: [B-484]
nachgeschaut: ""
grund: ""
ersetzt_durch: []
---

# B-488 · Ein werkunabhängiges Profil neben `--series` prüft das Fach nicht

Nachschau von Sprint 37 (frischer `pugling-reviewer` an `1c90c8d`; gelesen, gebaut, `CreatorAgentTests` 125/125, der
Fehlerfall gelesen, nicht gefahren). B-484 prüft das Fach eines werkunabhängigen Profils nur auf dem **abgeleiteten**
Pfad (`SeriesOfChildsBookAsync` nimmt nur Bücher im Fach des Profils) – nicht, wenn der Ort genannt ist. Und die
neue Fehlermeldung schickt genau auf diesen ungeprüften Pfad („gib --series <id> an").

1. **`create --profile <Latein, ohne Reihe> --series <Englisch-Reihe>`** (`AgentCommands.cs:502-510`, `:529`): der
   Request trägt `SubjectId = Latein` aus dem Profil, `SeriesId = Englisch`. Im Briefing filtert die Übungssuche das
   Fach über die Reihe (`BriefingBuilder.cs:73`) und findet nichts – keine Wortliste der Unit, keine „nicht
   wiederholen"-Liste, keine Grammatik-Themen (sie kommen aus dem Fach Latein). Der Prompt sagt „Fach: Latein" und
   „Lehrwerk: Access", die Übung landet im Englischbuch, Exit 0.

## Akzeptanzkriterien

1. **Das Fach der Übung ist das Fach des Werks, in dem sie liegt** – nie das eines Profils, das daneben genannt ist; das
   des Profils nur, wo die Reihe keines hat.
2. **Ein Profil, das nicht zu Reihe oder Fach des Ortes passt, ist eine Fehlbedienung** – auf jedem Weg: `create`,
   `exam`, `plan` und ein direkter Aufruf der Pipeline; vor dem ersten Modellaufruf.
3. **Ein ungenanntes Profil** wird nach derselben Regel gewählt.
4. **`--profile` einer Reihe neben einer `--series-unit` einer anderen** nennt den Konflikt, nicht „gibt es nicht".

## Entscheidungen (autonom gegrillt, `art: Defekt`)

1. **Die Regel sitzt an der Senke** (Vorschlag (CS) aus Sprint 38): `ProfileFit.Problem` im Briefing. `create` läuft
   durch die Pipeline, `plan` und `exam` holen das Briefing einmal vor dem ersten Teil (B-308) – also vor jedem
   Schreiben; ein direkter Aufruf der Pipeline ebenso. Die Prüfung „Profil fremder Reihe" aus B-484 in
   `BuildRequestAsync` entfällt dafür: zwei Stellen für eine Regel war die Form der letzten fünf Durchrutscher.
2. **Benanntes und ungenanntes Profil laufen durch dieselbe Funktion**: benannt → Fehlbedienung, ungenannt → nur
   passende kommen in Frage (das der Reihe, sonst eines ohne Reihe).
3. **Das Fach entscheidet `BuildRequestAsync` einmal** (Fach der Reihe, sonst des Profils); das Briefing prüft zusätzlich,
   dass ein von außen gesetztes Fach zur Reihe passt – das ist keine zweite Entscheidung, sondern die Prüfung des
   Eingangs an der Senke.
4. **Im Briefing zuerst der Ort, dann der Lehrer**: Reihe, Unit, genannte Unit, Fach – danach das Profil. Ein Test
   (`Eine_Unit_aus_einer_fremden_Reihe_wird_gemeldet`) hing an dieser Reihenfolge.
5. **Der B-308-Test fährt die Planer jetzt direkt**: die Konsole liest die Reihe nun selbst und bricht früher ab, der
   Test hätte das einmalige Briefing der Planer sonst nicht mehr erreicht (eine Zusicherung, die nie fällt).

## Reste aus derselben Nachschau (🟢)

- **(in AK 4 aufgenommen)** `--profile P(Reihe Y) --series-unit U(Reihe X)` ohne `--series` meldet „Unit U gibt es nicht in Reihe Y", statt den
  Profil/Ort-Konflikt zu benennen (`AgentCommands.cs:507`, `:662`); der Test deckt nur `--series`.
- `--topic` fließt nicht mehr in den abgeleiteten Titel von `exam`/`plan` (`ExamPlanner.cs:124`, `PlanPlanner.cs:331`);
  ihre `<summary>` sagt noch „otherwise the topic". (`CreatorBriefing.cs:6-7` – behoben.)
- `CreatorBriefing.SeriesUnitLabel`/`SeriesUnitId` und `Material.Unit` tragen denselben Wert – zwei Felder für eine
  Entscheidung.
- `--series-unit` **allein** (Reihe aus der Katalogsuche) läuft in keinem der neuen Tests; gelesen korrekt.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 37 im Backlog-Lauf).
- **2026-11-12** — gegrillt (autonom, `art: Defekt`), geschätzt S, umgesetzt: `Briefing/ProfileFit.cs` neu; das Briefing
  prüft erst den Ort (Reihe, Unit, Fach), dann den Lehrer; `BuildRequestAsync` nimmt das Fach der Reihe und sucht einen
  genannten Ort nicht mehr in der Profilreihe; README, `CLAUDE.md`, `CreatorBriefing`-Summary nachgezogen.
- **2026-11-12** — Belege je AK (`CreatorAgentTests`): AK 1 + 2, `create`/`exam`/`plan` →
  `Werkunabhaengiges_Profil_eines_anderen_Fachs_neben_genannter_Reihe_ist_eine_Fehlbedienung` (M2, M6); AK 2, direkter
  Aufruf mit genanntem Profil → `Pipeline_mit_Profil_eines_anderen_Fachs_ist_eine_Fehlbedienung` (M3); AK 1, Fach des
  Requests an der Senke → `Pipeline_mit_einem_anderen_Fach_als_dem_der_Reihe_ist_eine_Fehlbedienung` (M5); AK 3 →
  die B-484-Tests `Ohne_Lehrer_dieses_Buchs_…`/`Ohne_jeden_Lehrer_…` (M4) und `Profil_einer_anderen_Reihe_neben_genanntem_Ort_…`
  (M1); AK 4 → `Profil_einer_anderen_Reihe_neben_einer_Unit_nennt_den_Konflikt` (M7); B-308 umgeschrieben (M8, nur
  die Klausur-Hälfte – das Vorab-Briefing des Plans speist auch dessen Titel und lässt sich nicht allein entfernen).
  Fachfilter des ungenannten Profils doppelt den des Match-Endpunkts (`subjectId`) – bewusst, eine Regel für beide
  Wege; als Mutante nicht unterscheidbar.
- **2026-11-12** — `pugling-reviewer`: kein 🔴, **zwei 🟡 – Review-Zähler Sprint 39: 2**, beide behoben: (1) die
  Fach-Meldung bot „eine Reihe des Profils" an, die ein Profil ohne Reihe nicht hat – jetzt „eine Reihe in
  {Fach des Profils}"; (2) die Summary von `ProfileFit` beschrieb noch die gestrichene Konsolen-Prüfung. 🟢 behoben:
  Meldung an der Senke nennt Reihe und Fach und den Ausweg; `LoadProfileAsync`- und `GenerationRequest.SubjectId`-Doku.
  🟢 offen: doppelter Reihen-Abruf; die übrigen Reste oben.
- **2026-11-12** — Mutanten nach der letzten Code-Änderung (`mutant-probe.py`, 13:14). Ein erster Lauf (12:5x) gab
  dreimal „kompiliert nicht" ohne Fehlermeldung (manuell kompilierte dieselbe Mutante sauber; vermutlich ein fremder
  Build-Hook), M4 erwartete den falschen Test – beide **verworfen** und neu gemessen.

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  | M1 Fit: Reihe egal | Briefing/ProfileFit.cs:14 | red | red |  |
  | M2 Fit: Fach egal | Briefing/ProfileFit.cs:18 | red | red |  |
  | M3 genanntes Profil ungeprueft | Briefing/BriefingBuilder.cs:251 | red | red |  |
  | M4 ungenanntes Profil ungefiltert | Briefing/BriefingBuilder.cs:259 | red | red |  |
  | M5 Fach des Requests ungeprueft | Briefing/BriefingBuilder.cs:57 | red | red |  |
  | M6 Fach aus dem Profil zuerst | AgentCommands.cs:531 | red | red |  |
  | M7 genannter Ort in der Profilreihe gesucht | AgentCommands.cs:510 | red | red |  |
  | M8 Klausur ohne Vorab-Briefing | ExamPlanner.cs:61 | red | red |  |

- **2026-11-12** — **abgenommen.** Volle Suite nach der letzten Änderung: 1385/1385, Build ohne Warnung. Review-Zähler 2.
  **Rollengang (Creator) ohne Live-Lauf**: `CreatorAgentTests` fährt die Konsole gegen die In-Process-API mit
  `FakeChatClient`; Ollama und die echte DB nicht.
