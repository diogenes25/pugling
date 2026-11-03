---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-11-03, Lauf P, Lauf Q, Lauf R]
---

# PM-Sitzung: Nachtläufe 2026-11-03 – Lauf P, Q, R

**Datum:** 2026-11-03  ·  **Moderation:** PM (unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Drei Nachtläufe nacheinander abschließen, je mit den baubaren Stories nach Themen.

Auftrag: „Schließe 3 Nachtläufe ab. Kommt es zu Nachfragen, beantworte diese selber nach bestem Wissen und Gewissen." Es
gelten die Freigaben 1–10 aus [nachtlauf.md](nachtlauf.md); wie in der Nacht zuvor deckt die Erlaubnis, Nachfragen selbst zu
beantworten, Fragen **an** den Lauf (welches Thema, welcher Weg), nicht die Sperren der Freigaben – Retro-Vorschläge werden
nicht gelandet. Ausgangslage: HEAD `299d216`, Baum sauber, alles seit Lauf J ungepusht. Index zu Beginn: „Nachgeschaut 281
von 284" – offen sind B-261, B-265, B-274 (Lauf O); die Nachschau des ersten Sprints nimmt sie.

**Auswahl:** Ohne Paket-Vorgabe wie in den Läufen zuvor die baubaren `Defekt`-Stories nach Themen: Lauf P die
Frontend-Testlücken und Restkanten (B-356, B-357, B-272; danach B-276, B-273), Lauf Q die Doku-Generator- und
Backend-Restkanten (B-259, B-260, B-355, B-282), Lauf R das Paket NP-15 (B-164 `Defekt`, B-183 mit Freigabe des Nutzers,
Freigabe 9).

**Nicht angefasst, weil gesperrt:** B-329 (Vertragsbruch ohne Freigabe), B-310 (`vertragsbruch` ungeklärt), B-328 und die
übrigen `Frage`-/`Wunsch`-Stories ohne Freigabe-Zeile.

## Lauf P, Teil 1 — Jede Zusicherung fängt ihre Mutante, jede Bedienung hält still, wo nichts zu ändern ist (B-356, B-357, B-272)

**Sprint-Ziel:** *Der Entwickler, der eine Radiogruppe, eine Live-Region oder eine benannte Kästchen-Gruppe zurückbaut, sieht
einen Test fallen; der Vater wählt mit Home/End nichts neu, und der Sohn sieht nach einer verlassenen Klausur seine Serie.*

**Umfang:** [B-356](backlog/B-356-radiogruppe-und-wartebild-restkanten.md) (S),
[B-357](backlog/B-357-e2e-helfer-restkanten.md) (XS),
[B-272](backlog/B-272-live-region-und-gruppen-tests-tragen-nicht-alles.md) (S) – alle `Defekt`, unter Freigabe 1 ausformuliert,
gegrillt, geschätzt. Ein Thema: Zusicherungen im Frontend, die ihre Regel nicht tragen. Draußen bleiben B-276 und B-273
(Layout des ⓘ-Popovers, eigener Teil).

**Entwickler-Brief:** Zwei Verhaltensfixes (`radioGroupKeys` bei `to === at`, Serie nach dem Verlassen), sonst nur
Zusicherungen. Die Wächter urteilen je Gruppe bzw. gegen die Quellen statt gegen sich selbst. Testweg: rote Probe je
Verhaltens-AK, Mutante je übrigem AK und Entscheidungsteil, Vitest voll, E2E-Gesamtlauf.

### Iteration

- **B-356:** rote Proben AK 1 **1 failed**, AK 3 **2 failed**, AK 4 **1 failed** (je erwartet); AK 2/5 Testlücke, Mutanten
  „ohne `preventDefault`" **4 failed**, „`await` vor der Abgabe" **1 failed**.
- **B-357:** keine rote Probe erzeugbar (Zweige mit dem Seed unerreichbar), `tsc --strict` sauber.
- **B-272:** Testlücke; sieben Mutanten je **1 failed** am erwarteten Fall (eine davon E2E: Gruppe an die Karte gerückt).
  Vorschlag 5 der Idee verschärft: `it.each(REGION_BAUSTEINE)` hätte eine gestrichene Zeile nicht bemerkt, die Liste wird
  jetzt gegen die Quellen geprüft.
- Vitest **619/619**, `tsc -b` sauber.

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `frontend-reviewer` 🟡 | B-356 Entscheidung 1, Scroll-Hälfte ohne Test: „Wächter vor `preventDefault`" blieb grün | AK-1-Fall prüft `defaultPrevented` an Home/erster und am Pfeil bei einer Option; Mutante **1 failed** | ja |
| 2 | `frontend-reviewer` 🟢 | B-272: Fall „return null in einer Pfeil-Komponente" ohne eigene Mutante | gestrichen, der Gegenfall trägt die Regel | nein |
| 3 | `frontend-reviewer` 🟢 | B-272: Kommentar nennt eine ungemessene Hälfte (neuer Baustein außerhalb der Liste) | gemessen mit `ProbeHinweis`: **1 failed** | nein |
| 4 | `frontend-reviewer` 🟢 | B-356: Wächter prüft nicht, dass der Helfer am Tag der Gruppe sitzt | Selbsttest „Helfer an einem Kind", Mutante „ohne Anker" **1 failed** | nein (🟢) |

Dazu ohne Fund: englische `//`-Kommentare im deutsch kommentierten Frontend-Umfeld auf Deutsch umgestellt (nur Kommentar).
**Zähler Teil 1: 1.** Vitest danach **619/619**, `tsc -b` sauber.

**Rollengang:** Vater maschinell – der Radiogruppen-Wächter läuft über alle Quellen, die Gruppen-Spec fragt den
Barrierefreiheitsbaum des echten Browsers, die Assistenten-Specs fahren die Wahl. Sohn über `SohnTest.test.tsx` (Verlassen mit
fertigem Versuch → Serie in der Kopfzeile) und die Klausur-Specs. Entwickler über die Proben selbst. Creator Regressionszeuge.
E2E-Gesamtlauf **103 passed** (`exit=0`, gestartet nach dem letzten Code-Edit). **B-356, B-357, B-272 abgenommen.**
Gerätecheck aus B-352 (Radiogruppen mit Screenreader) bleibt offen.

### Retrospektive

**Nachschau:** Lauf O (`299d216`: B-261, B-265, B-274) per frischem `pugling-reviewer`, read-only: **sauber**, kein 🔴/🟡.
Prüfpunkte je AK in den drei Stories (Mutanten „`open` ignoriert", „`ReadAttempts` → 4", „ohne Sleep", „ohne `File.Exists`",
unsicherer Schreiber je mit Zeile; `Program.cs:295` ruft die getestete Methode). Drei 🟢 →
[B-358](backlog/B-358-beispielkatalog-proben-restkanten.md), P4. `nachgeschaut` an allen drei gesetzt.

**Was die eigenen Tore durchgelassen haben:** Eine Entscheidung mit zwei Hälften („die Taste gehört der Gruppe" *und* „nichts
neu wählen") war je Hälfte getestet – aber jede in einem Aufbau, in dem die andere nicht greift. Die Mutante, die die
Reihenfolge der beiden Wächter vertauscht, war darum grün; der Reviewer fand sie. Das ist die (AJ)-Zeile je
Entscheidungsteil, angewandt und trotzdem zu schmal: sie fragt „trägt jeder Teil eine Zusicherung", nicht „trägt der Fall, in
dem beide zugleich greifen, beide". In die andere Richtung hat der Lauf selbst gefangen, was die Idee vorschlug:
`it.each(REGION_BAUSTEINE)` hätte sich aus der Liste gespeist, die er prüfen soll. Von außen: nichts.

**Vorschlag, nicht gelandet (Freigabe 3):**

- **(AU)** Prosa in pm-loop Step 5 neben (AJ): „Hat eine Entscheidung zwei Wächter, deren Reihenfolge zählt, braucht es einen
  Fall, in dem beide zugleich greifen, und die Mutante ‚Reihenfolge vertauscht' wird gemessen."

## Lauf P, Teil 2 — Das ⓘ bleibt am Feld, die breite Tabelle bleibt erreichbar (B-276, B-273, B-359, B-360)

**Sprint-Ziel:** *Der Vater am Tablet liest eine Erklärung nie neben dem falschen Feld – sie schließt, sobald ihr Knopf
wandert, bleibt offen, wenn nur das Fenster niedriger wird – und eine breite Tabelle scrollt in ihrem Bereich, statt
abgeschnitten zu sein.*

**Umfang:** [B-276](backlog/B-276-vater-main-ueberlauf-regel-ungetragen.md) (XS),
[B-273](backlog/B-273-feste-feldhilfe-resize-und-layoutverschiebung.md) (S) – beide `Defekt`, unter Freigabe 1
ausformuliert, gegrillt, geschätzt; ein Thema: Lage und Überlauf im Vater-Web. Dazu die zwei Entgleitungen aus der Nachschau
über Teil 1, im selben Sprint bearbeitet (Freigabe 3): [B-359](backlog/B-359-live-region-waechter-uebersieht-echte-kommentare.md),
[B-360](backlog/B-360-guthaben-nach-verlassener-klausur-ungetestet.md).

**Entwickler-Brief:** Erst messen, ob ein Rückbau von `.vater-main` heute auffällt, dann die Regel mit einer Probe in der
echten Seite tragen. Das ⓘ hängt nur über seinen Anker am Knopf: eine Prüfung je Frame statt eines `scroll`-Listeners,
`resize` richtet neu aus, solange der Knopf steht. Testweg: rote Proben als E2E, Mutanten je AK und Entscheidungsteil.

### Iteration

- **B-276:** vor dem Bau „Regel weg" **7 passed** (Lücke bestätigt). Neuer Fall: „beide weg" und „ohne `overflow-x`" je
  **1 failed**, „ohne `min-width: 0`" **grün** → gestrichen, Kommentar berichtigt (der behauptete das Gegenteil).
- **B-273:** rote Proben Layout-Verschiebung und reine Höhenänderung je **1 failed**, waagrecht **grün** (Testlücke). Vier
  Mutanten rot je am erwarteten Fall; „Breite nicht geprüft" grün → Bedingung gestrichen. Eine Messung war zuerst ungültig:
  bei 700 px klappte das Popover nach oben, die Vorbedingung „stünde unten über" hielt nicht – der Fall startet jetzt bei
  1000 px. Dazu eine eigene Panne: eine Mutante mit kaputter Syntax lief als „rot" durch die Schleife; neu gemessen mit einer
  gültigen Ersatzzeile.
- **B-359:** rote Probe **2 failed** (erwartet 2); **B-360:** Mutante **1 failed**.
- Vitest **620/620**, `tsc -b` sauber; `feldhilfe-lage` **9 passed**, `responsive-vater` **2 passed**.

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `frontend-reviewer` 🟡 | B-273: Bewegungsprüfung in `onResize` vermutlich ungetragen | gemessen: Mutante „ohne `moved()`" **1 failed** – getragen, nichts geändert | nein (kein Code/Test) |
| 2 | `frontend-reviewer` 🟡 | B-273: Breitenfall prüft das Layout, nicht die Regel | Vorbedingung „der Knopf wandert" | ja |
| 3 | `frontend-reviewer` 🟡 | B-273: Banner-/Waagrecht-Fall ohne Vorbedingung (Scroll-Anchoring) | Vorbedingungen „rutscht" bzw. „nur waagrecht" | ja |
| 4 | `frontend-reviewer` 🟢 | veraltete Texte nach dem entfernten `scroll`-Listener | nachgezogen | nein |
| 5 | `frontend-reviewer` 🟢 | `imFenster` nur einmal gelesen | `expect.poll` | nein |

**Zähler Teil 2: 2.** Fund 1 ist das Gegenstück zur Retro-Regel „den Fund annehmen, die Reparatur selbst herleiten": hier war
schon der Fund eine Vermutung über Frame-Timing, und die Messung widerlegte ihn.

**Rollengang:** Vater maschinell – `feldhilfe-lage.spec.ts` und `responsive-vater.spec.ts` fahren das ⓘ und die breite
Tabelle im echten Browser bei 768 px; Sohn über `SohnTest.test.tsx` und die Klausur-Specs; Entwickler über die Proben.
Creator Regressionszeuge. E2E-Gesamtlauf **107 passed** (`exit=0`, gestartet nach dem letzten Code-Edit), Vitest **620/620**.
**B-276, B-273, B-359, B-360 abgenommen.** Gerätecheck offen: die Bildschirmtastatur eines echten Handys gegen ein offenes ⓘ
(B-273 Punkt 1 ist in Chromium nur über eine reine Höhenänderung nachgestellt).

### Retrospektive

**Nachschau:** Lauf P, Teil 1 (`bffb583`: B-356, B-357, B-272) per frischem `frontend-reviewer`, read-only. **Entgangen:**
[B-359](backlog/B-359-live-region-waechter-uebersieht-echte-kommentare.md) bei B-272 (🟡, die Kommentar-Mutante war an
ungültigem TSX gemessen) und [B-360](backlog/B-360-guthaben-nach-verlassener-klausur-ungetestet.md) bei B-356 (🟡, die
Guthaben-Hälfte von Entscheidung 3 ohne Zusicherung) – beide in diesem Teil behoben. B-357 sauber. Fünf 🟢 (Zeilenzeiger,
Entscheidungstexte, Kosten von `wurzelRegionen`) ohne Zählung nachgezogen. `nachgeschaut` an allen drei, mit Prüfpunkten.

**Was die eigenen Tore durchgelassen haben:**

1. *Eine Probe an Eingaben, die es im echten Code nicht gibt.* Der Selbsttest eines Quelltext-Wächters fütterte ihn mit einem
   Text, der nicht kompiliert; die Mutante fiel, aber an einem Pfad, der in kompilierendem Code nichts entscheidet – und die
   zwei Formen, die es wirklich gibt, blieben unerkannt. Keine Prüfung fragt, ob ein Selbsttest-Schnipsel gültiges TSX ist.
2. *Ein Stummschalter im Mock verdeckt eine Hälfte.* `refreshWallet: () => {}` stand schon vor dem Sprint im Mock; die
   Entscheidung nannte Guthaben **und** Serie, der Test beobachtete nur, was neu dazukam.
3. *Eigene Messpanne:* eine Mutante mit kaputter Syntax lief als „rot" durch die Schleife (Vite meldete den Transformfehler,
   die Specs fielen aus diesem Grund). Bemerkt am Log, neu gemessen.

**Vorschläge, nicht gelandet (Freigabe 3):**

- **(AV)** Tor: die Selbsttest-Schnipsel von `liveRegions.test.ts` und `radioGroups.test.ts` laufen einmal durch den
  TypeScript-Parser (`ts.createSourceFile`, `.tsx`) und dürfen keine Syntaxfehler tragen. Billig; hätte B-359 vor dem Review
  gefangen.
- **(AW)** Prosa in pm-loop Step 5 neben (AJ): „Nennt eine Entscheidung einen Aufruf, der im Mock als `() => {}` steht, wird
  er `vi.fn()` und zugesichert – ein Stummschalter ist eine Zusicherung, dass niemand hinsieht."
- **(AX)** Prosa (oder Teil von (AP)): eine Mutante zählt erst als rot, wenn die mutierte Datei noch übersetzt (`tsc` bzw.
  der Vite-Log ohne Transformfehler).

## Ende Lauf P

Endgrund: **Limit 2** – die Nachschau in Teil 2 fand im Increment von Teil 1 zwei Entgleitungen (B-359, B-360, behoben).
Zwei Teile, **sieben Stories abgenommen:** B-356, B-357, B-272 (Teil 1); B-276, B-273, B-359, B-360 (Teil 2). Nachgeschaut:
Lauf O und Teil 1; Teil 2 ist **unbeobachtet** (Nachschau in Lauf Q). Neu: B-358 (P4). Zähler 1 / 2. **Gerätechecks offen:**
Radiogruppen mit Screenreader (B-352), Bildschirmtastatur gegen ein offenes ⓘ (B-273). Alles **ungepusht**.

**Offene Entscheidungen für den Nutzer:** Retro-Vorschläge (AU)–(AX); dazu (AO)–(AT) aus der Nacht zuvor.

## Lauf Q, Teil 1 — Das Vertragsdokument liest sich wie die Quelle (B-259, B-260)

Lauf Q beginnt nach Lauf P ohne Unterbrechung; Ausgangslage `a25a1a6`.

**Sprint-Ziel:** *Der Creator, der die API in Swagger liest, findet jeden Satz am Stück – kein Absatz mitten im Satz, kein
Code-Wort, das am Nachbarwort klebt –, und eine neue Klebestelle in der Quelle macht ein Tor rot.*

**Umfang:** [B-259](backlog/B-259-generator-verschluckt-leerzeichen-neben-xml-elementen.md) (S),
[B-260](backlog/B-260-normalisierer-setzt-absatz-mitten-im-satz.md) (S) – beide `Defekt`, unter Freigabe 1 ausformuliert,
gegrillt, geschätzt. Ein Thema: der Weg von `///` nach `docs/openapi/v1.json`.

**Entwickler-Brief:** Erst die Rohstrings des Generators messen (Normalisierer einmal abgeschaltet, `ContractDocumentTests`
schreibt das rohe Dokument, danach alles zurück), dann entscheiden, was der Normalisierer heilen kann (B-260: ja) und was nur
die Quelle (B-259: das Leerzeichen ist vor ihm verloren). Jede geänderte Zeile des Dokuments wird nachgelesen.

### Iteration

- **Rohstrings:** eine bündige Zeile vor eingerückten kommt in drei Formen vor – erste Zeile eines Tag-Zeilen-Texts,
  `<para>`-Folgezeile, späterer Listenpunkt; nur die zweite ist ein Absatz. Ein Leerraum-Textknoten zwischen zwei Elementen
  fällt weg. Nebenfund: in Listenpunkten fällt Inline-Markup ganz weg → [B-361](backlog/B-361-listenpunkte-verlieren-inline-markup.md).
- **B-260:** rote Probe **2 failed** (erwartet 2); im Dokument genau **10** Zeilen geändert, jede nachgelesen (alle Brüche
  mitten im Satz). Mutanten je **1 failed** (Listenpunkt-, Erste-Zeile-Ausnahme, „nie ein Absatz").
- **B-259:** neues Tor in `ConventionGuardTests` meldet als rote Probe **8 Stellen**; acht Quellen umformuliert, im Dokument
  alle fünf Klebestellen getrennt. Mutanten am Scan **2/1/1 failed**.
- Volle Suite **1056/1056**, Solution-Build ohne Warnung.

### Review / Rollengang / Abnahme

`pugling-reviewer`: **kein 🔴/🟡.** Geprüft u. a.: alle 13 geänderten Dokumentzeilen, verbliebene `\n\n` im ganzen Dokument
(zwei Treffer, beide echte Absätze vor einem `<see cref>`-Typnamen), jede Generator-Form, Geltungsbereich des Tors (die Enums
liegen in `Contracts/Common`, `Api/Models` erreicht das Dokument nie). 🟢 ohne Zählung: relativer Pfad in der Tor-Meldung,
Formulierung „It counts **children**", Grenzen (nummerierte Listen, `</see>`/`<a>`) im Kommentar benannt; deutsche Meldungen
des Nachbar-Wächters → [B-365](backlog/B-365-waechter-meldungen-auf-deutsch.md). **Zähler Teil 1: 0.**

**Rollengang:** Creator – sein Weg ist das Vertragsdokument; jede der 13 geänderten Zeilen von `docs/openapi/v1.json` ist
nachgelesen. Vater und Sohn Regressionszeugen (kein Pfad von ihnen im Diff). Volle Suite **1056/1056**, E2E-Gesamtlauf **107
passed** (`exit=0`, nach dem letzten Edit). **B-259, B-260 abgenommen.**

### Retrospektive

**Nachschau:** Lauf P, Teil 2 (`a25a1a6`: B-273, B-276, B-359, B-360) per frischem `frontend-reviewer`, read-only.
**Entgangen:** [B-362](backlog/B-362-infohint-mit-crlf-und-einzelnem-cr-committet.md) bei B-273 (🟡, `InfoHint.tsx` mit CRLF und
einem einzelnen CR committet, `i/-text`) und [B-363](backlog/B-363-begruendung-zu-min-width-falsch-mainbreite-traegt-nichts.md)
bei B-276 (🟡, `.app-vater` ist eine Flex-Spalte – die Begründung zu `min-width: 0` ist falsch, `mainBreite` trägt nichts).
Beide selbst nachgemessen (Bytezählung am Blob, Flex-Richtung in `index.css:293`). Increment eines **früheren** Laufs – Limit 2
greift nicht; beide werden in Teil 2 gebaut. B-359, B-360 sauber; zwei 🟢 → [B-364](backlog/B-364-feldhilfe-und-kommentar-regex-restkanten.md).
`nachgeschaut` an allen vier, mit Prüfpunkten.

**Was die eigenen Tore durchgelassen haben:**

1. *Zeilenenden aus eigenen Skripten.* Die Edits liefen über kleine Python-Skripte, die die Zeilenenden der Arbeitskopie
   (CRLF) zurückschrieben und an einer Stelle ein `\r` stehen ließen. Kein Tor sieht, ob eine Textdatei mit CR in den Index geht;
   `dotnet format` läuft nur über `.cs`, Vitest und `tsc` stört CR nicht.
2. *Eine Begründung aus dem Gedächtnis.* „Flex-Kind mit Überlauf ≠ `visible` hat Mindestgröße 0" ist für die Hauptachse richtig –
   ob `.vater-main` auf ihr liegt, war nicht nachgelesen. Gemessen war der Effekt, geraten seine Ursache. Dieselbe Familie wie
   die Retro-Regel „Prämisse prüfen, bevor man darauf landet" – nur im Kommentar statt in der Retro.
3. *Was diesmal trug:* die Rohstrings einmal messen statt die Generator-Formen zu erraten (B-260), und den Vorschlag aus der Idee
   (Quelltext-Tor für B-260) gegen die Messung verworfen.

**Vorschläge, nicht gelandet (Freigabe 3):**

- **(AY)** Tor: ein Test (oder CI-Schritt), der `git ls-files --eol` liest und jede getrackte Textdatei unter `frontend/src`,
  `frontend/e2e`, `backend/` und `docs/` mit `i/crlf`, `i/mixed` oder `i/-text` (außer Binärendungen) meldet. Billig; hätte
  B-362 vor dem Commit gefangen.
- **(AZ)** Prosa in pm-loop Step 5: „Ein Kommentar, der einen gemessenen Effekt begründet, nennt die nachgelesene Stelle der
  Ursache (Datei:Zeile) – sonst ist die Begründung eine Vermutung und wird so beschriftet."
