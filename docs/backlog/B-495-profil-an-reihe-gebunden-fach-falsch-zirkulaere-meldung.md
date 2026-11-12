---
tags: [typ/story, status/abgenommen, bereich/werkzeug, rolle/creator]
aliases: [ProfileFit zirkuläre Meldung, Profil an Reihe mit fremdem Fach]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: "backend"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Nachschau Sprint 39 in der Retro von Sprint 40)
unverifiziert: false
entgangen_bei: [B-488]
grund: ""
ersetzt_durch: []
---

# B-495 · Ein an eine Reihe gebundenes Profil mit fremdem Fach schickt den Creator im Kreis

Nachschau von Sprint 39 (frischer `pugling-reviewer` an `ed43705`, Commit per `git archive` exportiert, dort gebaut und
getestet: 1385/1385; die Fehlerfolge gelesen, nicht gefahren).

Der Server nimmt ein Profil an, dessen `SeriesId` und `SubjectId` auseinanderlaufen
(`CreatorProfilesController.ReferenceProblemAsync` prüft nur, dass beide existieren; das Fach einer Reihe lässt sich
später per PATCH ändern). Profil P mit Reihe Y (Englisch) und Fach Latein:

1. `create --profile P` in Reihe Y → Fachprüfung in `ProfileFit` (`backend/Pugling.Agent.Creator/Briefing/ProfileFit.cs:14-21`)
   rät „wähle ein Profil für Englisch oder **eine Reihe in Latein**".
2. `--series L` (eine Latein-Reihe) → Reihenprüfung: „gehört zur Reihe Y … lass --series/--series-unit weg".
3. Zurück zu 1. Dasselbe mit nur `--child`: `BestProfileForChildAsync` wählt P über `series_match`.

Die Meldung nennt einen Ausweg, der an einer zweiten Sperre endet (Meldungsregel 2026-11-07). Vor `ed43705` lief dieses
Profil (mit falschem Fach), jetzt ist es gar nicht mehr nutzbar.

## Reste aus derselben Nachschau (🟢)

- Ein Profil mit Freitext-Fach (`SubjectId == null`, `SubjectName = "Latein"`, B-142) passiert `ProfileFit` immer; der
  Prompt nennt dann „Fach: Latein" (`CreatorBriefing.cs:101`) neben „Fach: Englisch" (`:109`).
- Ein direkter Pipeline-Aufruf mit einer Reihe ohne Fach prüft nicht, dass das Fach der Anfrage das des Profils ist
  (`BriefingBuilder.cs:57`); die Konsole erzeugt so eine Anfrage nicht (`AgentCommands.cs:531`).
- Veraltete Summary an `ResolveProfileAsync` (`BriefingBuilder.cs:230-235`); `ProfileFit.cs:16` gibt einen CLI-Tipp auch
  direkten Aufrufern.
- Der B-308-Test sagt „rot, wenn die Prüfung fehlt" – für die Plan-Hälfte allein nicht messbar (in B-488 offen benannt);
  ein Satz in der Summary fehlt.

## Akzeptanzkriterien

1. **Ein an die Reihe gebundenes Profil mit fremdem Fach** nennt das Profil als Ursache und bietet keinen Weg an, der an
   der Reihenprüfung endet („eine Reihe in …").
2. **Ein Profil mit Freitext-Fach** (ohne `SubjectId`, B-142) eines anderen Fachs ist eine Fehlbedienung.
3. **In einer Reihe ohne Fach** wird das Fach des Profils gegen das Fach der Anfrage geprüft.
4. Die Summary an `ResolveProfileAsync` beschreibt, was die Methode tut.

## Entscheidungen (autonom gegrillt, `art: Defekt`, Nachtlauf 2026-11-12)

1. **`ProfileFit` vergleicht mit dem Fach der Anfrage**, nicht mit dem der Reihe: das Briefing hält das Fach der Anfrage
   schon gleich dem der Reihe, wo diese eins hat – so deckt *eine* Prüfung beide Fälle (Senke statt zweiter Regel).
2. **Zwei Meldungen nach der Bindung des Profils:** gebunden → „wähle ein anderes Profil oder korrigiere Fach oder Reihe
   dieses Profils (Vater-Web, Fachlehrer)"; ungebunden → wie bisher „ein Profil für X oder eine Reihe in Y" (dort führt
   der zweite Weg nicht in die Reihenprüfung).
3. **Kein neues Server-Tor** für widersprüchliche Profile (Reihe und Fach auseinander): das Fach einer Reihe lässt sich
   später ändern, der Widerspruch entsteht also auch ohne Profil-Schreiben; die Senke ist das Briefing.
4. **Freitext-Fach:** verglichen mit dem Fachnamen, getrimmt und ohne Groß/Klein; ein Profil ohne beides passt zu jedem
   Fach (wie bisher).
5. **Nicht angefasst:** der CLI-Tipp „lass --series weg" für direkte Pipeline-Aufrufer (kosmetisch); der B-308-Test.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 39, Retro Sprint 40 im Nachtlauf).
- **2026-11-12** — Nachtlauf, Sprint 42: autonom gegrillt (`art: Defekt`), geschätzt S, umgesetzt (`ProfileFit` mit Fach
  der Anfrage, `TeachesAnother`, Summary von `ResolveProfileAsync`).
- **2026-11-12** — Rote Probe vor dem Fix (`CreatorAgentTests`, Debug): erwartet 3 rot, gemessen 3 rot („eine Reihe in"
  gefunden; zweimal keine Ausnahme). Dabei gefunden: `Contains("gebunden")` war nur grün, weil der Profilname der
  Testdaten „gebunden" enthielt – umbenannt, damit die Zusicherung fallen kann.
- **2026-11-12** — Belege: AK 1 → `Profil_auf_einer_Reihe_mit_fremdem_Fach_nennt_das_Profil_als_Ursache` (P1); AK 2 →
  `Profil_mit_freiem_Fachnamen_eines_anderen_Fachs_ist_eine_Fehlbedienung` (P2); AK 3 →
  `Profil_eines_anderen_Fachs_in_einer_Reihe_ohne_Fach_ist_eine_Fehlbedienung` (P3); AK 4 gelesen. `CreatorAgentTests`
  133/133.
- **2026-11-12** — `pugling-reviewer`: kein 🔴/🟡 – **Review-Zähler Sprint 42: 0.** Bestätigt: alle Wege zum Profil
  (benannt, per Kind im Briefing, `BestProfileForChildAsync`, Klausur- und Planplaner) enden in `ProfileFit`; die Auswege
  beider Meldungen bestehen beide Prüfungen; die Fläche „Vater-Web, Fachlehrer" ändert Fach und Reihe
  (`profilePatch.ts`). 🟢 behoben ohne Zählung: die gebundene Meldung rät zuerst zum Fach („…oder seine Reihe, wenn es zu
  einem anderen Werk gehört"), eine Vorbedingung im Test vor den Aufruf gezogen, der zweite B-455-Fall als
  Absichtserklärung beschriftet. 🟢 zurückgestellt → [B-498](B-498-reste-review-sprint-42.md). Die vier roten Tests im
  Lauf des Reviewers waren die roten Proben von Sprint 43 vor ihrem Fix, nicht dieser Diff.
- **2026-11-12** — Kopie der echten DB: zwei Profile, beide mit Fach-Id passend zu ihrer Reihe (1→5, 4→6), kein
  Freitext-Profil – für die Söhne ändert sich nichts.
- **2026-11-12** — Mutanten nach der letzten Änderung (`mutant-probe.py`, Debug):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | P1 keine Gebunden-Meldung | ProfileFit.cs:24 | red | red |
  | P2 Freitext-Fach passt immer | ProfileFit.cs:41 | red | red |
  | P3 nur gegen Fach der Reihe | ProfileFit.cs:21 | red | red |
  | P4 Typprüfung nach Entdoppeln | CreatorProfilesController.cs:233 | red | red |

- **2026-11-12** — **abgenommen.** Volle Suite 1405/1405 (Debug, mit Sprint 43 im Arbeitsbaum), `CreatorAgentTests` +
  `CreatorProfile*` 148/148 nach der letzten Änderung. **Rollengang (Creator) als Maschinengang:** die Tests fahren
  Konsole und Pipeline gegen die In-Process-API mit `FakeChatClient`; Ollama nicht gefahren.
