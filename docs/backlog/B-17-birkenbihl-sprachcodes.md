---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/katalog, lerntechnik/vokabeln, rolle/creator]
aliases: [Sprachcode-Normalisierung]
status: abgenommen
prio: P3
art: Frage
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: memory/birkenbihl-vokabel-dekodierung.md
wartet_auf: ""
nachgeschaut: "2026-10-26"
---

# B-17 · Sprachcode-Normalisierung bei der Vokabel-Dekodierung

Die Dekodierung vergleicht Sprachcodes exakt; `gb` und `en` gelten als verschieden. Die Recherche zeigt:
das ist keine allgemeine Normalisierungslücke, sondern **eine einzelne freie Eingabe**, die aus der Reihe
tanzt — die Antwort ist ein kleiner, gezielter Fix, keine Verwerfung.

## User Story

Als Creator möchte ich beim Anlegen einer Birkenbihl-Übung dieselbe geschlossene Sprachliste sehen wie
überall sonst im Vater-Web, damit meine Eingabe automatisch zum Vokabelspeicher passt, statt an einem
exakten String-Vergleich lautlos vorbeizulaufen.

## Ist-Stand am Code

- **Der Vergleich ist tatsächlich exakt und bewusst so** —
  `BirkenbihlDecodingService.cs:25` (Klassenkommentar): "The comparison of language codes is exact: the
  supervisor must use the same codes as in the vocabulary store." Die Query
  `BirkenbihlDecodingService.cs:50` vergleicht `v.SourceLanguage == learningLang && v.TargetLanguage ==
  nativeLang` ohne jede Normalisierung (nur das Wort selbst wird `ToLower()`-verglichen, Zeile 51/60).
  `BirkenbihlDecodingServiceTests.cs:26-42` testet genau das als **dokumentierte, gewollte
  Fail-closed-Garantie** ("EN" ≠ "en" → kein Treffer) — dieser Test war zuvor ungetestet, ist aber keine
  Lücke, sondern eine Absicherung einer bewussten Entscheidung.
- **Der Vokabelspeicher selbst kann gar keine uneinheitlichen Codes erzeugen**: `VaterVocab.tsx:68-69`
  und `:372-375` sowie `ClozeTexts.tsx:208-217` und `exerciseConfig.tsx:404-423` (Übungstyp `Translation`)
  füllen `sourceLanguage`/`targetLanguage` ausschließlich über ein `<select>` aus der geschlossenen Liste
  `LANGUAGES` (`frontend/src/lib/languages.ts:12-17`: `de`, `en`, `fr`, `la`). Der Kommentar dort ist
  explizit: "Bewusst kein Freitext: die Vater-UI bietet nur diese Codes an."
  `exerciseConfig.tsx:420` begründet das für `Translation` sogar wörtlich: "Jedes Satzpaar landet im
  Vokabel-Store … deshalb sind die Sprachcodes Pflicht."
- **Genau eine Stelle bricht diese Konvention**: das Birkenbihl-Formular im selben File,
  `exerciseConfig.tsx:479-480`. `LearningLang`/`NativeLang` kommen dort aus einem freien `<input
  placeholder="Englisch">` bzw. `<input placeholder="Deutsch">` — Platzhalter, die sogar den deutschen
  Sprach**namen** nahelegen, nicht den Code. `LANGUAGES` ist in derselben Datei bereits importiert
  (Zeile 5) und für `Translation` in Gebrauch, für `Birkenbihl` aber ungenutzt.
- **Die Store-Verankerung verschärft die Folge**: Setzt ein Creator am Birkenbihl-Wort einen freien Gloss,
  legt `ExerciseControllers.cs:556-558` über `store.GetOrCreateAsync(config.LearningLang, …,
  config.NativeLang, …)` bei Bedarf einen **neuen** Store-Eintrag unter dem abweichenden Code an — ein
  Tippfehler im Formular pflanzt sich damit dauerhaft in den Vokabelspeicher fort, statt nur einmal eine
  leere Dekodierung zu liefern.
- **Kein Live-Konsument umgeht das Formular**: Der KI-Creator-Agent (`backend/Pugling.Agent.Creator/
  Drafting/`) hat keine Birkenbihl-Strategie (nur `VocabularyStrategy`, `GrammarStrategy`,
  `TranslationStrategy`, `ClozeStrategy`); der geseedete Datensatz nutzt saubere Codes (`Seed.cs:1052-1053`,
  `"en"`/`"de"`). Das einzige heute genutzte Einfallstor für einen Mismatch ist die Vater-UI selbst.

## Die echte Lücke

Nicht „Sprachcodes brauchen Normalisierung" (das wäre eine Backend-Änderung gegen eine bewusste,
getestete Fail-closed-Garantie) — sondern: **ein einzelnes Eingabefeld hält sich nicht an die im Rest der
App längst etablierte Regel** „Sprachcode kommt aus der geschlossenen Liste, nie aus Freitext". Damit ist
das Ausgangs-„Ungeprüft" beantwortet: gemischte Codes kommen im echten Datenstand nicht vor, weil bislang
niemand zufällig auf den korrekten zweistelligen Code getippt hat *und* der Rest der App das ohnehin
verhindert — aber das Formular lädt aktiv dazu ein, es beim nächsten Mal falsch zu machen.

## Entscheidungen

1. **Kein Backend-Fix, keine Normalisierung/Fuzzy-Matching am Sprachcode.** Der exakte Vergleich in
   `BirkenbihlDecodingService` ist eine bewusste, jetzt getestete Fail-closed-Garantie
   (`BirkenbihlDecodingServiceTests.cs`); sie anzurühren wäre eine Verhaltensänderung ohne Auftrag und
   ginge über die Frage hinaus, die diese Story stellt. Kosten: keine — es ändert sich nichts am Code.
2. **Frontend-Fix: Birkenbihl bekommt dieselben zwei `<select>`-Felder wie `Translation`/`ClozeTexts`/
   `VaterVocab`**, statt der zwei freien `<input>` in `exerciseConfig.tsx:479-480`. `LANGUAGES` ist im File
   bereits importiert und für `Translation` im Gebrauch — reine Wiederverwendung, kein neuer Import, kein
   Vertragsfeld ändert Typ (`LearningLang`/`NativeLang` bleiben `string`). Begründung: schließt das einzige
   heute bestehende Einfallstor, an der Stelle, an der es auch entstanden ist. Kosten: gering (eine
   Formular-Stelle), aber deckelt die Sprachauswahl der Birkenbihl-Übung auf die vier geführten Sprachen
   (`de`/`en`/`fr`/`la`) — deckungsgleich mit der Einschränkung, die `Translation` und der Store schon
   haben, also kein neuer Verlust an Ausdruckskraft.
3. **Kein Daten-Fix/keine Migration für Bestandsdaten.** Der Seed trägt bereits saubere Codes
   (`Seed.cs:1052-1053`), und kein anderer Erzeuger existiert. Sollte in einer echten Installation doch
   eine Birkenbihl-Übung mit einem freien Sprachnamen stehen, zeigt sich das beim nächsten Satz-Dekodieren
   sofort als leere Dekodierung — sichtbar, nicht still. Kosten: ein (bisher hypothetischer) Alt-Fall
   müsste von Hand nachkorrigiert werden, statt automatisch geheilt zu sein.

## Akzeptanzkriterien

1. Im Birkenbihl-Formular (`exerciseConfig.tsx`, `type === "Birkenbihl"`) ersetzen zwei `<select>` aus
   `LANGUAGES` die bisherigen freien `<input>` für Lern- und Muttersprache — analog zu `Translation`
   (Zeilen 404-423).
2. Eine neu angelegte Birkenbihl-Übung kann keinen Sprachcode mehr tragen, der nicht in `LANGUAGES` steht;
   `learningLang`/`nativeLang` im gespeicherten Config-JSON sind einer der vier Codes.
3. Ein neuer Komponententest (oder eine Ergänzung von `e2e/uebungstypen.spec.ts`, das Birkenbihl bisher
   **nicht** anlegt — geprüft: kein `createExercise(page, "Birkenbihl", …)` im Spec) belegt die beiden
   `<select>` und ihre Optionen für `type === "Birkenbihl"`.
4. `BirkenbihlDecodingService`/`BirkenbihlDecodingServiceTests.cs` bleiben unverändert — die
   Fail-closed-Garantie ist explizit **kein** Teil dieser Reparatur.

## Schätzung

**Größe: XS** — zwei `<input>` durch zwei `<select>` mit bereits importierter, bereits im selben File
verwendeter Liste ersetzen (Muster liegt zwei Übungstypen weiter oben im selben File vor).

- **`wo: frontend`**, **`migration: nein`** (kein Schema betroffen), **`vertragsbruch: nein`** (Typ von
  `LearningLang`/`NativeLang` bleibt `string`, nur die UI-Steuerung ändert sich).
- **Risiken:** keine erkennbaren — die Änderung engt nur die Eingabe ein, sie schafft kein neues
  Verhalten. Einziger Kontrollpunkt: bestehende, von Hand mit falschem Freitext angelegte
  Birkenbihl-Übungen (falls es sie in einer laufenden Installation geben sollte) laden im Editor einen
  Wert, der nicht in `LANGUAGES` steht — das `<select>` müsste diesen Fall nicht abstürzen lassen, sondern
  ihn (wie ein `<select>` mit fehlendem `<option>` es ohnehin tut) einfach als leere Auswahl zeigen, bis
  neu gewählt wird.
- **Angriffsplan:** rein frontend, ein Edit in `exerciseConfig.tsx`; kein Backend-Schritt nötig.
- **Testweg:** `e2e/uebungstypen.spec.ts` legt Birkenbihl bisher gar nicht an (kein `createExercise(page,
  "Birkenbihl", …)` im Spec) — dieser Fix bekommt darum einen **neuen** gezielten Test, der die beiden
  `<select>` und ihre `LANGUAGES`-Optionen für `type === "Birkenbihl"` prüft (Komponententest analog zum
  bestehenden Muster für `Translation`, oder eine Ergänzung des E2E-Specs).

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-08-03** — ausformuliert: Recherche zeigt, das Ausgangs-„Ungeprüft" ist beantwortbar. Kein
  allgemeines Normalisierungsproblem, sondern ein einzelnes Formularfeld (`exerciseConfig.tsx:479-480`),
  das entgegen der sonst durchgehaltenen Konvention (`LANGUAGES`-Picklist) Freitext zulässt — belegt gegen
  `BirkenbihlDecodingService.cs`, `BirkenbihlDecodingServiceTests.cs`, `languages.ts`, `VaterVocab.tsx`,
  `ClozeTexts.tsx` und `exerciseConfig.tsx` (autonom geprüft, Nutzerauftrag 2026-08-04).
- **2026-08-03** — gegrillt: drei Entscheidungen getroffen (kein Backend-Fix/keine Normalisierung, da
  bewusste getestete Fail-closed-Garantie; Frontend-Fix auf dieselbe Picklist wie `Translation`; kein
  Daten-Fix, da kein anderer Erzeuger existiert und der Seed sauber ist) (autonom getroffen, Nutzerauftrag
  2026-08-04).
- **2026-08-03** — geschätzt: Größe XS, `wo: frontend`, keine Migration, kein Vertragsbruch — reine
  Wiederverwendung des `LANGUAGES`-Picklist-Musters aus derselben Datei (autonom getroffen, Nutzerauftrag
  2026-08-04).
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): gilt (Eingabefelder jetzt `exerciseConfig.tsx:577-578`, Seed `Seed.cs:1107`). `exerciseConfig.test.ts:71-80` prüft schon einen Birkenbihl-Rundlauf – der neue Test hängt sich dort an. Nachtlauffähig; Vorbehalt: die Entscheidungen hat am 2026-08-03 ein Agent getroffen, nicht der Nutzer im Dialog.
- **2026-10-23** — **Vom Nutzer bestätigt** in der Grill-Runde G-0 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): die am 2026-08-03 autonom getroffenen Entscheidungen gelten als seine. Die Story ist ohne Vorbehalt nachtlauffähig (Paket NP-2).
- **2026-10-23** — gebaut im Nachtlauf (dritter Lauf, Sprint 7 / NP-2), unter Freigabe 8 (Entscheidungen vom Nutzer bestätigt, G-0). `exerciseConfig.tsx`: `LanguageSelect` aus `LANGUAGES` für Lern- und Muttersprache, mit „– bitte wählen –" nur bei unbekanntem Alt-Code (sonst zeigte die Auswahl „Deutsch" und speicherte den Altwert); `emptyExtra("Birkenbihl")` belegt en/de vor. Neuer Test `BirkenbihlSprachen.test.tsx` (4 Fälle). Rote Proben (Mutanten-Skript): freie Eingabe (= Ausgangszustand) 2/4 rot, falsches Feld 1/4, kein Platzhalter 1/4, keine Vorbelegung 2/4. Vitest **406/406**, `tsc -b` sauber. `BirkenbihlDecodingService` unberührt (AK 4).
- **2026-10-23** — `frontend-reviewer`: kein Blocker. **Offen, nicht behoben (Lauf an Freigabe 3 beendet):** (a) `contentProblem` (`exerciseConfig.tsx:779`) prüft die Sprachen nicht – eine bearbeitete Alt-Übung mit `""`/„Englisch" lässt sich trotz „bitte wählen" speichern, und eine eigene Glosse legt dann Store-Einträge unter dem falschen Code an; (b) Fall 2 des Tests prüft nur „Muttersprache", ein Fehler „Lernsprache schreibt nach `nativeLang`" bliebe grün. Notiz ohne Story: `Translation`/`ClozeTexts` bauen ihre Auswahl weiter selbst und zeigen einen unbekannten Code als erste Sprache – `LanguageSelect` wäre das Bauteil dafür. Kein Rollengang, keine Abnahme.
- **2026-10-26** — **Entscheidung des Nutzers** (Morgen-Entscheidung 2026-10-26, Dialog, [Protokoll](../pm-sitzung-2026-10-26-nacht.md#morgen-entscheidung-2026-10-26)): Sprint 7 wird **behalten** (kein Revert von `93f227a`). Die offenen Review-Funde werden als Nacharbeit mit **neuem Zähler (0)** abgearbeitet, danach folgt NP-3. `wartet_auf` geleert.
- **2026-10-26** — Nacharbeit ([Protokoll](../pm-sitzung-2026-10-26-tag.md), Lauf C): (a) `contentProblem` lehnt bei Birkenbihl Codes außerhalb von `LANGUAGES` ab, zuerst vor dem Satz. Laut Reviewer ist das heute ein Wächter, denn der Bearbeiten-Dialog hat für Birkenbihl keinen Inhalts-Editor. Der Fund vom 2026-10-23 beschrieb also einen Weg, den es nicht gibt, der Kommentar ist berichtigt. (b) Beide Auswahlen werden einzeln geprüft. Proben: Prüfung weg → 1 failed; Lernsprache schreibt `nativeLang` → 1 failed; Reihenfolge getauscht → 1 failed (Review-Fund 3). Rollengang ohne Browser (Extension nicht verbunden) als E2E bzw. Integrationstest: **Creator** fährt `uebungstypen.spec.ts` (jede Übungsart anlegen und im Testmodus ausprobieren, der Aufsatz als Ansicht ohne Bewertung) und die Agent-Tests (`CreatorAgentTests.Foerdermodus_*`); **Vater** und **Sohn** als Regressionszeugen, volle Suiten grün. Verifikation: Solution-Build 0 Warnungen, Backend **920/920 `exit=0`**, Vitest **444/444**, Playwright voll nach dem letzten Code-Edit **95 passed**. Zähler der Nacharbeit am Ende **4**. **Abgenommen**; Commit: `14fa1f7`.
- **2026-10-26** — **Nachschau** (frischer Prüfer, `git archive` von `14fa1f7`, [Protokoll](../pm-sitzung-2026-10-26-tag.md), Lauf C): kein Entgleiten. Geprüft: `contentProblem :784-787`, die Wächter-Aussage (`CONTENT_EDITABLE`, `ExerciseEditModal.tsx:180`), alle drei Mutanten je 1 failed wie behauptet.
