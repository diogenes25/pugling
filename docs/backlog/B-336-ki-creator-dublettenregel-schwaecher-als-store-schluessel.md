---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [DraftRules.NoDuplicates faltet anders als VocabKey]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: Review von B-331 in Lauf K (docs/pm-sitzung-2026-10-29-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-10-30"
---

# B-336 · Die Dubletten-Regel des KI-Creators ist schwächer als der Store-Schlüssel

`DraftRules.NoDuplicates` (`Pugling.Agent.Creator/Drafting/DraftRules.cs:70-82`) gruppiert nur nach der Vorderseite,
ohne Groß/Klein. Der Server-Schlüssel (`VocabKey.Slug`) faltet zusätzlich Diakritika, ß→ss und Apostrophe. Ein Entwurf mit
„Café"/„Cafe" (gleiche Rückseite) besteht die Regel des Agenten und bekommt seit
[B-331](B-331-doppeltes-wort-im-uebungs-payload-500.md) beim Anlegen `400 validation_error` (vorher `500`). Der Agent fängt
beim Anlegen keinen `400` (`AgentCommands.cs` fängt nur `404`), der Lauf bricht also ab statt in eine Reparaturrunde zu
gehen – wie vorher beim `500`, also keine Regression, aber dieselbe Lücke.

Zu klären beim Ausformulieren: die Regel an `VocabKey.Generate` angleichen (Agent referenziert `Pugling.Api` nicht – den
Slug teilen oder nachbauen?) oder einen `400` beim Anlegen als Reparaturgrund behandeln.

## User Story

Als **Creator**, der Vokabelübungen vom KI-Creator erzeugen lässt, möchte ich, dass ein Entwurf mit zwei Schreibweisen
desselben Wortes („the café"/„the cafe") in die Reparaturrunde geht, statt den Lauf mit einem `400` des Servers zu
beenden.

## Ist-Stand (Lauf L, am Code belegt)

- `DraftRules.NoDuplicates` (`Pugling.Agent.Creator/Drafting/DraftRules.cs:70-82`, Stand vor dem Fix) gruppiert getrimmt
  und `OrdinalIgnoreCase`. `VocabularyStrategy.Validate` ruft sie nur mit der Vorderseite (`:44`).
- Der Server lehnt in `VocabularyController.RepeatedWordAsync` (`ExerciseControllers.cs:77-91`) ein zweites Item ab,
  dessen `VocabKey.Generate(src, front, tgt, back)` schon vorkam bzw. das auf dieselbe Store-Id auflöst. Der Schlüssel
  faltet Groß/Klein, Diakritika, ß→ss, Apostroph→Leerzeichen und doppelte Leerzeichen (`"__"→"_"`).
- `VocabKey` lag in `Pugling.Api/Data`; der Agent referenziert `Pugling.Api` nicht, nur `Pugling.Contracts` und
  `Pugling.Client`.
- **Gemessen** (rote Probe unten): Für „the café"/„the cafe", „rock'n'roll"/„rock n roll" und „the  stable"/„the stable"
  (je gleiche Rückseite) endet `pipeline.CreateAsync` mit `PuglingApiException: 400 validation_error: Entry 1 repeats a
  word …` – keine Reparaturrunde.

## Die echte Lücke

Agent und Server haben zwei Begriffe von „dasselbe Wort", und der des Agenten ist der schwächere. Was der Server ablehnt,
lässt die Regel durch – und ein `400` beim Anlegen ist für den Agenten kein Reparaturgrund.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Die Regel faltet wie der Server; der Schlüssel zieht dafür nach `Pugling.Contracts`.** `VocabKey` (unverändert)
   liegt jetzt in `Pugling.Contracts/Common`, Namespace `Pugling.Contracts`; die API nutzt ihn über den projektweiten
   `<Using>`. Nicht nachbauen: zwei Kopien einer Faltung driften, und genau so ist der Defekt entstanden. *Kosten:*
   das Blatt-Projekt trägt die erste reine Funktion neben den DTOs; begründet damit, dass der Schlüssel als
   `Vocabulary.Key` im Vertrag sichtbar ist und „dasselbe Wort" eine Vertragsregel ist.
2. **Weiter nur die Vorderseite vergleichen**, gefaltet über `VocabKey.Generate(src, front, tgt, "")`. Der Server
   verlangt zusätzlich die gleiche Rückseite; die Regel des Agenten bleibt damit strenger als der Server (wie bisher
   bei Groß/Klein) und deckt ihn ab. *Kosten:* „the café → das Café" neben „the cafe → der Kaffee" wird weiter als
   Dublette abgelehnt – das war schon für „The cafe"/„the cafe" so und ist für eine Übung aus einer Unit gewollt.
3. **Kein Fang des `400` beim Anlegen** (die zweite Option der Idee). Ein `400` heißt sonst „der Agent hat etwas
   geschickt, das seine eigenen Regeln hätten finden müssen" – das gehört in die Regel, nicht in einen breiten Fang,
   der auch echte Vertragsfehler zur Reparaturrunde machen würde. *Kosten:* jede weitere Server-Regel braucht wieder
   ihr Gegenstück in `DraftRules`.

## Akzeptanzkriterien

1. Ein Vokabel-Entwurf mit zwei Vorderseiten, die sich nur in Akzent, Apostroph oder doppeltem Leerzeichen
   unterscheiden, löst eine Reparaturrunde aus; die Meldung nennt beide Schreibweisen; der reparierte Entwurf wird
   veröffentlicht.
2. Der bisherige Fall (zweimal „the horse") meldet unverändert „Vokabeln kommen doppelt vor: the horse."

## Schätzung

`S`, Backend (Agent + Contracts), keine Migration, kein Vertragsbruch (eine Klasse zieht um, öffentliche API
unverändert). Angriffsplan: rote Probe → `VocabKey` verschieben → Faltung in `NoDuplicates` → Mutante. Testweg:
`CreatorAgentTests` (Agent gegen die In-Process-API).

## Verlauf

- **2026-10-29** — angelegt aus dem `pugling-reviewer`-Fund 🟢 zu B-331 in Lauf K (ohne Zählung); den Abbruch-Pfad am
  Code nachgesehen (`grep PuglingApiException` im Agenten), nicht gemessen.

- **2026-10-30** — Lauf L, Teil 1: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt, gebaut. Rote Probe: neue
  Theorie `Eine_Dublette_nach_dem_Store_Schluessel_geht_in_die_Reparatur` **3 failed** (erwartet 3), je
  `PuglingApiException: 400 validation_error`. Nach dem Fix `CreatorAgentTests` **82/82**. Mutante „Faltung weg"
  (`fold: null`) → **3 failed** (erwartet 3). AK 2 hält der bestehende Fall
  `Ein_fehlerhafter_Entwurf_geht_mit_den_Verstoessen_zurueck_ans_Modell` – das stimmte nicht, siehe Review.
- **2026-10-30** — Review `pugling-reviewer`: 🟡 AK 1 halb geprüft (nur die erste Schreibweise; Mutante `g.First()`
  blieb grün) → `Assert.Contains(second, …)`, Mutante jetzt **3 failed**; 🟡 AK 2 stand nur im Text (der bestehende Fall
  prüfte `Contains("doppelt vor")`, Mutante „ohne `Distinct`" → „the horse = the horse" blieb grün) → exakter Satz,
  Mutante jetzt **1 failed** (beide zählen, Zähler 2); toter Link in `docs/uebergabe-vokabeln.md` berichtigt (ohne
  Zählung); 🟢 eine Zeile in `Pugling.Contracts/CLAUDE.md` zur reinen Funktion (ohne Zählung); 🟢 mehrdeutiger Trenner im
  `VocabKey` (älter) → [B-346](B-346-vocabkey-trenner-mehrdeutig.md). Superset-Frage beantwortet: außer B-346 fängt die
  Regel alles, was der Server ablehnt.
- **2026-10-30** — Abnahme Lauf L, Teil 1: volle Suite **1025/1025**, `CreatorAgentTests` **82/82**, Build ohne Warnung,
  `dotnet format` ohne Fund in den geänderten Dateien. E2E **100 passed** (`exit=0`, nach dem Fix gestartet; danach nur
  Testzusicherungen und Doku geändert, der Produktcode ist derselbe). Rollengang: Creator maschinell über den Agenten
  gegen die In-Process-API (die neue Theorie ist sein Weg: Entwurf → Reparatur → veröffentlicht); kein echtes Ollama-Modell
  gefahren. Vater und Sohn Regressionszeugen: ihre Specs im E2E-Lauf grün, kein Pfad von ihnen im Diff außer dem
  verschobenen `VocabKey` (Inhalt unverändert). **Abgenommen**, Commit siehe Protokoll ([Lauf L](../pm-sitzung-2026-10-30-nacht.md)).
- **2026-10-30** — Nachschau in Lauf L, Teil 2 (frischer `pugling-reviewer`, `git archive 0cb732b`, Build und Suite **1025/1025** am Snapshot): **sauber** (nur 🟢). Geprüft: keine Referenz mehr auf den alten Ort (backend, docs, wiki, `.http`, `.claude`, frontend); alle Aufrufer von `NoDuplicates`/`DraftRules`/`VocabKey` im Agenten (Exam-/PlanPlanner, UnitNotesFiller rufen beides nicht); Server-Dublettenprüfung und Peek gegen die Faltung des Agenten (Superset bis auf B-346); je Theorie-Zeile eine eigene Mutante in `VocabKey` gemessen, jede fällt genau an ihrer Zeile. 🟢 behoben in Teil 3: Zusage „names every spelling" trug kein Fall (Mutante `Distinct(OrdinalIgnoreCase)` grün) → Zeile „The horse"/„the horse", Mutante **1 failed**; `Pugling.Contracts/CLAUDE.md` nennt den `VocabRefJsonConverter`. 🟢 offen, ohne Story: Translation-/Grammar-Dubletten prüft kein Test (älter, nur Meldungstext).
