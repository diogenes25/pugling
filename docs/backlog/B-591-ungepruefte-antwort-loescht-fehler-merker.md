---
tags: [typ/story, status/in-arbeit, bereich/training, rolle/student]
aliases: [LastMissed ungeprüft, Selbstauskunft im gewerteten Pfad]
status: in-arbeit
prio: P3
art: Frage
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-219 (Fix-Prüfung im Backlog-Lauf 2026-11-16)
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-591 · Löscht eine ungeprüfte „gewusst"-Antwort den Fehler-Merker der milden Abstufung?

[B-219](B-219-verzeihende-abstufung-je-position.md) stuft bei gesetzter Option nach dem ersten Fehler nur eine Box
zurück, nach zwei Fehlern in Folge hart. Der Merker `PositionItemProgress.LastMissed` folgt laut Entscheidung 11 nur
geprüften Antworten; der Selbstauskunfts-Pfad (`climbs`) lässt ihn seit dem Fix unverändert.

Offen bleibt der gewertete Pfad (`PositionPracticeController`, Aufruf von `ApplyReview` ~538): Ist die Tagesstufe
selbst nicht getippt und `RequireTypedTest` aus, wertet er ein ungeprüftes `WasKnown` – und löscht damit den Merker.
Folge ab jeder Box: 4 → Fehler 3 → „gewusst" 4 → Fehler 3 statt 1. Frage an den Vater: soll der Merker dort auch
stehen bleiben, oder ist eine ungeprüfte Stufe bewusst gleichwertig?

## User Story

Als **Vater** möchte ich, dass nur eine vom Server geprüfte Antwort den Fehler-Merker der verzeihenden Abstufung
löscht, damit ein ungeprüftes „gewusst“ meines Sohnes nicht den nächsten getippten Fehler wieder mild macht.

## Ist-Stand am Code

Gemessen am 2026-11-16 (Stand `35a6ce8e`):

- **„Geprüft“ heißt getippte Stufe**: `PositionPracticeController.cs:410` `typed = type.IsTypedStage(stage)` auf der
  serverseitig erzwungenen Kartenstufe (`:405-409`, nicht vom Client wählbar). Getippt sind beim Vokabeltyp
  `LetterBoxes`/`FreeText`/`Audio`/`MultipleChoice`/`LetterScramble` (`Services/Shared/StageMechanics.cs:17-19`);
  `SelfAssess` nicht. Die Wertung: `typed ? grader.Matches(…) : dto.WasKnown` (`:427-429`) – ungeprüft entscheidet die
  Behauptung des Clients.
- **Gewerteter Pfad nimmt ungeprüfte Antworten an**: `scored = … (typed || !pos.RequireTypedTest) …` (`:465`); bei
  einer ungetippten Tagesstufe (Kartenstufe = Tagesstufe, sonst wäre es die Stufenleiter `climbs`, `:130-131`) und
  `RequireTypedTest` aus wertet er also `WasKnown` und ruft `ApplyReview` (`:538`).
- **Senke**: `PositionPlayService.ApplyReview` (`Services/Shared/PositionPlayService.cs:494-513`) setzte
  `LastMissed = false` bei richtig, `true` bei falsch – ohne zu wissen, ob geprüft wurde.
- **Selbstauskunfts-Pfad** (`climbs`, `PositionPracticeController.cs:563-573`) sicherte `LastMissed` vor dem Aufruf
  und schrieb ihn danach zurück (B-219 Entscheidung 11) – dieselbe Regel, beim Aufrufer wiederholt.
- **Merker**: `PositionItemProgress.LastMissed` (`Models/PlanPositionEntities.cs:232`).

## Die echte Lücke

Die Regel „der Merker folgt nur geprüften Antworten“ stand an einem Aufrufer statt in der Senke; der zweite Aufrufer
(gewerteter Pfad mit ungetippter Tagesstufe) löschte ihn. Folge: 4 → Fehler 3 → ungeprüft „gewusst“ 4 → Fehler 3
statt 1.

## Offene Punkte

1. ~~Löscht ein ungeprüftes „gewusst“ im gewerteten Pfad den Merker?~~ → Entscheidung 1 (Nutzer).
2. ~~Woran erkennt der Server „geprüft“?~~ → Entscheidung 2.
3. ~~Setzt ein ungeprüftes „nicht gewusst“ im gewerteten Pfad den Merker?~~ → Entscheidung 3.
4. ~~Wo sitzt die Regel?~~ → Entscheidung 4.
5. ~~Was geschieht mit Merkern, die vor dem Fix schon ungeprüft gelöscht wurden?~~ → Entscheidung 5.

## Entscheidungen

1. **Ein ungeprüftes „gewusst“ lässt `LastMissed` stehen, auch im gewerteten Pfad.** **Entscheidung des Nutzers**
   (2026-11-16, siehe Verlauf). Box und Punkte der ungeprüften Antwort bleiben wie heute.
2. **„Geprüft“ = getippte Kartenstufe (`type.IsTypedStage(stage)`).** Stakeholder-Entscheidung im Auftrag des Nutzers
   (2026-11-16). Begründung: dieselbe Größe entscheidet schon, ob der `AnswerGrader` wertet oder `WasKnown` gilt
   (`PositionPracticeController.cs:427-429`); eine zweite Definition wäre eine zweite Wahrheit. `MultipleChoice` zählt
   als geprüft (der Server vergleicht die Wahl). Mengen-Modus setzt `typed` ohnehin voraus. Kosten: keine.
3. **Auch ein ungeprüftes „nicht gewusst“ im gewerteten Pfad setzt den Merker nicht.** Stakeholder-Entscheidung im
   Auftrag des Nutzers (2026-11-16), deckungsgleich mit B-219 Entscheidung 11 für `climbs` (beide Richtungen). Die Box
   fällt weiter wie heute (gelesen wird der Merker, nur nicht geschrieben). Begründung: eine Regel für beide Pfade
   statt einer Ausnahme je Richtung. Kosten: auf einer selbstbewerteten Tagesstufe mit Milde fallen wiederholte
   ehrliche „nicht gewusst“ je eine Box statt beim zweiten auf 1 – vertretbar, weil dort ohnehin das Wort des Kindes
   gilt und ein „gewusst“ jederzeit möglich wäre.
4. **Die Regel sitzt in der Senke**: `ApplyReview(…, bool verified, …)` schreibt `LastMissed` nur bei `verified`. Der
   gewertete Pfad reicht `typed`, `climbs` reicht `false` und verliert sein Sichern/Zurückschreiben des Merkers
   (`ReviewCount` sichert er weiter, das ist eine andere Regel). Stakeholder-Entscheidung im Auftrag des Nutzers
   (2026-11-16). Begründung: ein künftiger dritter Aufrufer muss die Frage beantworten, statt sie zu vergessen
   (Pflichtparameter ohne Vorgabe). Kosten: keine Vertragsänderung, keine Migration.
5. **Bestandszeilen werden nicht nachgebessert.** Stakeholder-Entscheidung im Auftrag des Nutzers (2026-11-16). Ein
   vor dem Fix ungeprüft gelöschter Merker ist aus keiner Spalte rekonstruierbar (die `ReviewEvent`s hängen an der
   Sitzung, nicht am Item; B-219 Ist-Stand). Kosten: höchstens eine zusätzliche milde Abstufung je betroffenem Wort.

## Akzeptanzkriterien

1. **Ungeprüft „gewusst“ im gewerteten Pfad**: Box 4 → getippter Fehler (3) → ungeprüftes „gewusst“ auf
   selbstbewerteter Tagesstufe (4, gewertet) → Merker steht → getippter Fehler → **Box 1**. **Rot**, wenn der
   gewertete Pfad den Merker bei ungeprüfter Antwort löscht.
2. **Ungeprüft „nicht gewusst“ im gewerteten Pfad**: Box 4 → 3, Merker bleibt `false`. **Rot**, wenn er ihn setzt.
3. **Gegenfall geprüft**: ein getippter Treffer löscht den Merker weiter (B-219 AK 2). **Rot**, wenn `ApplyReview` ihn
   nie löscht.
4. **Selbstauskunft unverändert**: die beiden B-219-Tests zu Entscheidung 11 bleiben grün, jetzt über die Senke.

## Schätzung

**XS** (zwei Zeilen in der Senke, zwei Aufrufer, zwei Tests). `wo: backend`, `migration: nein`, `vertragsbruch: nein`.

- **Risiko**: der Box-Pfad darf sich nicht ändern – nur die Schreibzeilen des Merkers sind bedingt, die Box-Formel
  liest ihn wie bisher.
- **Angriffsplan**: Parameter `verified` an `ApplyReview`; Aufrufer `typed` bzw. `false`; Sicherung in `climbs`
  entfernen; Tests in `VerzeihendeAbstufungTests`.
- **Testweg**: Integrationstests `VerzeihendeAbstufungTests` (Tagesstufe per DB umgestellt, ersetzbare Uhr), volle
  Suite, Mutanten per `.claude/scripts/mutant-probe.py`.

## Verlauf

- **2026-11-16** — angelegt aus der Invarianten-Prüfung des B-219-Fixes; nicht gebaut, weil es eine Produktfrage ist.
- **2026-11-16** — **Nutzer-Entscheidung:** „nein, ungeprüft löscht den Merker nicht". Eine ungeprüfte „gewusst"-Antwort lässt `LastMissed` stehen, auch im gewerteten Pfad.
- **2026-11-16** — ausformuliert (Ist-Stand gegen `35a6ce8e`), gegrillt (Entscheidung 1 des Nutzers, 2–5 als Stakeholder-Entscheidungen im Auftrag des Nutzers), geschätzt (**XS**, `backend`, keine Migration, kein Vertragsbruch) und gebaut (Stufe `in-arbeit`): `ApplyReview(…, bool verified, …)` schreibt `LastMissed` nur bei `verified` (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:494-516`); gewerteter Pfad reicht `typed` (`Controllers/Student/PositionPracticeController.cs:538`), `climbs` reicht `false` und sichert den Merker nicht mehr selbst (`:563-569`). Volle Suite 1717/1717 grün (exit 0), `dotnet format Pugling.sln --verify-no-changes` sauber.
- **2026-11-16** — Nachweis je AK-Fall (`.claude/scripts/mutant-probe.py`, Debug, gemessen an `35a6ce8e` + Arbeitsbaum nach dem letzten Code-Edit, Grundlinie nach dem Zurückspielen grün):
  - AK 1, ungeprüft „gewusst“ im gewerteten Pfad → `VerzeihendeAbstufungTests.Mild_Ungepruefte_Wertung_Gewusst_Loescht_Den_Fehler_Merker_Nicht` (`backend/Pugling.Api.Tests/VerzeihendeAbstufungTests.cs:129`), M1 `verified: true` am gewerteten Aufrufer (Stand vor B-591) → rot; M2 Löschen ohne `if (verified)` → rot.
  - AK 2, ungeprüft „nicht gewusst“ im gewerteten Pfad → `Mild_Ungepruefte_Wertung_Nicht_Gewusst_Setzt_Den_Fehler_Merker_Nicht` (`:148`), M1 → rot; M3 Setzen ohne `if (verified)` → rot.
  - AK 3, Gegenfall geprüfter Treffer löscht → `Mild_Treffer_Unterbricht_Die_Folge` (`:36`), M4 Löschzeile entfernt → rot (erster M4-Versuch `if (false)` kompilierte nicht, ungemessen; neu gemessen mit Kommentarzeile).
  - AK 4, Selbstauskunft über die Senke → `Mild_Selbstauskunft_Gewusst_…` (`:99`) und `Mild_Selbstauskunft_Nicht_Gewusst_…` (`:115`), M5 `verified: true` am `climbs`-Aufrufer → rot.
