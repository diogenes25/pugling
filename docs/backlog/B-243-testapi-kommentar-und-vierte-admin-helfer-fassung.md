---
tags: [typ/story, status/abgenommen, bereich/tests]
aliases: [AdminAdultAsync-Summary falsche Richtung, RegisterAdminFatherAsync in RemarkTests]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-23-nacht.md (Nachschau zu Sprint 3, zweiter Lauf)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-179]
wartet_auf: ""
---

# B-243 · B-179 hat den Admin-Helfer geteilt – die Summary behauptet die Gegenrichtung des gemessenen Effekts, und eine vierte Fassung blieb stehen

Nachschau zu Sprint 3 des ersten Laufs vom 2026-10-23 (Commit `98c8431`), frischer Agent, von mir am Code
gegengelesen. B-179 ist in seinen drei Akzeptanzkriterien erfüllt; beide Funde liegen daneben.

## User Story

Als **Entwickler, der den vierten Admin-Test schreibt**, möchte ich, dass der geteilte Helfer die einzige
Fassung ist und seine Summary den Effekt richtig herum beschreibt, damit ich weder eine fünfte Kopie anlege
noch aus der Begründung das Falsche lerne.

## Ist-Stand am Code

1. **Ein Satz, der durch B-179 falsch wurde** – bereits korrigiert in diesem Sprint, hier als Beleg:
   `backend/Pugling.Api.Tests/TestApi.cs:63-65` (`AdminAdultAsync`-Summary) sagte: „flagging the seeded
   adult would … turn every later ‚foreign creator' check of the same class **green** from the wrong end".
   Gemessen (alte Summary in `FachEigentumTests`, Diff von `98c8431`): „Flagging adult 1 makes
   `Geseedete_Arten_Sind_Fuer_Jeden_Gesperrt` **fail**" – der Fall (`FachEigentumTests.cs:305-320`) erwartet
   403, mit Admin-Flag greift das Break-Glass, die Antwort wird 200 und der Test **rot**. Die Summary
   behauptete die Gegenrichtung. Fehlerklasse B-112: ein Satz beim Zusammenziehen umformuliert, den Effekt
   nicht nachgelesen.
2. **Eine vierte private Fassung existiert weiter:** `backend/Pugling.Api.Tests/RemarkTests.cs:659-668`
   `RegisterAdminFatherAsync` setzt `father.IsAdmin = true` per EF (async, ohne Login; der Aufrufer `:764-765`
   meldet danach selbst an). B-179 nannte drei Fassungen; der Grep dahinter (`AdminCreatorAsync|AdminAsync|
   MakeAdmin`) traf diesen Namen nicht. Die eigene Summary (`:650-657`: geteilte Factory, Break-Glass umgeht
   RWX) ist dort wertvoll und bliebe.

## Die echte Lücke

Dieselbe wie bei B-166/B-241: ein Grep, der nicht alles fasst, gilt als Inventar – hier über drei Namen,
die der Autor kannte, statt über das Merkmal (`IsAdmin = true`). Und ein Kommentar, den kein Build liest.

## Akzeptanzkriterien

1. `TestApi.cs:63-65` beschreibt den gemessenen Effekt (403-Fälle werden **rot**) – in `98c8431`s Nachfolger
   bereits so, nachgelesen.
2. `RemarkTests.RegisterAdminFatherAsync` nutzt `TestApi.MakeAdmin` (oder ist durch `RegisterFatherAsync` +
   `TestApi.MakeAdmin` ersetzt); `rg "IsAdmin = true" backend/Pugling.Api.Tests` trifft nur noch `TestApi.cs`.
3. Suite grün, Zahl im Verlauf.

## Offene Punkte

1. Helfer behalten (mit `MakeAdmin` darin) oder ganz auflösen? *Empfehlung: behalten* – die Summary erklärt,
   warum dieser Test einen Admin braucht; das ist lokal richtig.

## Entscheidungen

Gegrillt im zweiten Nachtlauf 2026-10-23 (Freigabe 1, `art: Defekt`).

1. **Helfer behalten, Rumpf auf `TestApi.MakeAdmin` umstellen** (Punkt 1): `RegisterAdminFatherAsync` bleibt als
   Name und mit seiner Summary (sie erklärt, warum *dieser* Test einen Admin braucht – Break-Glass umgeht RWX,
   geteilte Factory), ruft aber `RegisterFatherAsync` + `TestApi.MakeAdmin` statt eigenem EF-Zugriff.
   Begründung: B-179s Lehre war „der Merksatz steht einmal", nicht „jeder Aufrufer ruft TestApi direkt".
   **Kosten:** eine Indirektion mehr als nötig; dafür bleibt der lokale Grund lesbar.
2. **Die drei `using`s prüfen, nicht raten:** `Microsoft.EntityFrameworkCore`, `Microsoft.Extensions.DependencyInjection`,
   `Pugling.Api.Data` werden in `RemarkTests.cs` nur vom Helfer benutzt (`:662-666`, drei Treffer, alle im
   Helfer). Nach dem Umbau sind sie tot – IDE0005 ist in `.editorconfig:39` eine Warnung und
   `TreatWarningsAsErrors` gilt repo-weit, der Build würde rot. Sie fliegen mit. **Kosten:** keine.
3. **Rote Zahl ist ein Grep, kein Test** (Aufräumen-artiger Teil): `rg -c "IsAdmin = true" backend/Pugling.Api.Tests`
   zählt vorher **zwei** Dateien (`TestApi.cs`, `RemarkTests.cs`), nachher **eine**. Der Beleg, dass das
   Verhalten gleich bleibt, ist die Suite (`Zeigt_Fremde_Anmerkungen_Nur_Per_Break_Glass`-Fall in RemarkTests
   grün vorher wie nachher). Begründung: ein Umbau ohne Verhaltensänderung hat keine Zusicherung, die rot werden
   kann – das ist die Lage aus B-179 Entscheidung 4, hier ausdrücklich so benannt. **Kosten:** kein Tor gegen
   die fünfte Kopie; das Merkmal (`IsAdmin = true`) ist jetzt aber als Grep benannt, der Nachschau-Prüfer kann es
   zählen.
4. **Punkt 1 der Story (Summary-Richtung) ist in `7c33765` erledigt** – AK 1 wird beim Abnehmen nachgelesen,
   nicht erinnert.

## Schätzung

**Größe XS** (Anker B-179 selbst: ein Helfer, eine Datei). `wo: backend`, keine Migration, kein Vertrag.

**Angriffsplan:** Grep-Zahl vorher → Helfer-Rumpf auf `RegisterFatherAsync` + `TestApi.MakeAdmin` → tote `using`s
raus → Build (Hook) → Grep-Zahl nachher → Suite → Reviewer → Commit.

**Risiko:** `RegisterFatherAsync` könnte in `RemarkTests` anders registrieren als `TestApi.AdminAdultAsync`
(Name, PIN) – darum der eigene Helfer bleibt, nur sein Flag-Schritt wird geteilt.

**Testweg:** Suite (879 → 879), Grep 2 → 1.

## Verlauf

- **2026-10-23** — **abgenommen** (zweiter Nachtlauf, Sprint 5). `pugling-reviewer`: Verhalten identisch
  (`TestApi.MakeAdmin` synchron, Flag vor dem Login in `FatherWithoutGlobalReadAsync`), die drei entfernten
  `using`s nirgends sonst gebraucht (Grep auf EF-/DI-Symbole → 0; `.Select` aus `ImplicitUsings`), Summary des
  Helfers weiter wahr; **kein Fund**. Nachschau-Nit aus Sprint 4 übernommen: die Summary von `AdminAdultAsync`
  sagt jetzt „every later case … in which the seeded adult himself is expected to get 403" (betroffen sind
  nur Fälle, in denen Adult 1 selbst 403 erwartet – `FachEigentumTests.cs:305-320/:623-637`). **Rollengang:**
  Entwickler-Sicht, kein UI; Vater/Sohn Regressionszeugen (Suite). Commit: siehe Nachtrag.
- **2026-10-23** — **gebaut** (zweiter Nachtlauf, Sprint 5). **Zahl vorher/nachher:** `rg -c "IsAdmin = true"
  backend/Pugling.Api.Tests/*.cs` → vorher **2 Dateien** (`TestApi.cs`, `RemarkTests.cs`), nachher **1**
  (`TestApi.cs`). `RegisterAdminFatherAsync` ruft `RegisterFatherAsync` + `TestApi.MakeAdmin`; die drei
  `using`s (`EntityFrameworkCore`, `DependencyInjection`, `Pugling.Api.Data`) waren danach tot (Grep über die
  Datei: 0 Restnutzung) und sind weg. AK 1 nachgelesen: `TestApi.cs:63-65` sagt „would see the break-glass 200
  instead and go red" (seit `7c33765`). Suite **879/879** – ein Umbau ohne Verhaltensänderung, die
  Suite ist der Beleg (Entscheidung 3).

- **2026-10-23** — gegrillt und geschätzt im zweiten Nachtlauf (Sprint 5, Freigabe 1).

- **2026-10-23** — angelegt aus der **Nachschau** zu Sprint 3 des ersten Laufs (Commit `98c8431`),
  `entgangen_bei: B-179`. Punkt 1 ist im selben Sprint (Sprint 4, zweiter Lauf) korrigiert – ein Satz von
  heute; Punkt 2 bleibt offen, weil er ein Test-Umbau mit eigener roter Zahl ist und der Sprint sich nicht
  erweitert (README: ein Fund beim Bauen wird eine eigene Story).
