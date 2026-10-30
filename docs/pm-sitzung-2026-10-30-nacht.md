---
tags: [typ/protokoll, bereich/pm]
aliases: [Nachtlauf 2026-10-30, Lauf L]
---

# PM-Sitzung: Nachtlauf 2026-10-30 – Lauf L

**Datum:** 2026-10-30  ·  **Moderation:** PM (unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die baubaren `Defekt`-Stories nach Themen in Sprints fahren, beginnend mit den Kandidaten aus
[Ende Lauf K](pm-sitzung-2026-10-29-nacht.md#ende-lauf-k).

Auftrag: „starte nachtlauf" – der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–10. Ausgangslage:
HEAD `1195c26`, Baum sauber, alles seit Lauf J ungepusht. Keine Paket-Vorgabe. Index zu Beginn: „Nachgeschaut 254 von
258" – offen sind B-335, B-286, B-337, B-338 (Lauf K, Teil 2); die Nachschau des ersten Sprints nimmt sie.

**Seit Lauf K von außen dazugekommen:** `3190f9e` „Review-Funde Lauf K behoben" – ein Review außerhalb der Schleife fand
im Store-Abgleich (B-277/B-337) und an den Schlagwort-Labels (B-141) weitere Lücken, die der Nutzer behoben hat. Das ist
Retro-Eingang (siehe Teil 1, Retrospektive). Der aus B-337 zurückgestellte Punkt „`GetOrCreateAsync` folgt dem
gespeicherten Schlüssel" ist damit erledigt (`VocabularyStoreService.GetOrCreateManyAsync` sucht über `FindManyAsync`
nach Inhalt).

**Nicht angefasst, weil gesperrt:** B-329 (Vertragsbruch ohne Freigabe), B-328 (`Frage`), B-310 (Statuscode-Änderung,
`vertragsbruch` ungeklärt).

## Lauf L, Teil 1 — Der KI-Creator repariert, was der Server ablehnen würde (B-336)

**Sprint-Ziel:** *Der Creator, der Vokabelübungen vom KI-Creator erzeugen lässt, bekommt für zwei Schreibweisen desselben
Wortes eine Reparaturrunde statt eines abgebrochenen Laufs.*

**Umfang:** [B-336](backlog/B-336-ki-creator-dublettenregel-schwaecher-als-store-schluessel.md) (P3, `Defekt`, unter
Freigabe 1 ausformuliert, gegrillt, geschätzt: S). Ein Sprint aus einer Story ist zulässig; die übrigen Kandidaten
(B-280, B-275, B-262, B-266) sind Testlücken im Vater-Web und bilden Teil 2.

**Entwickler-Brief:** Ein Begriff von „dasselbe Wort" für Agent und Server. `VocabKey` zieht unverändert nach
`Pugling.Contracts/Common` (Namespace `Pugling.Contracts`, die API sieht ihn per `<Using>`), `DraftRules.NoDuplicates`
bekommt eine optionale Faltung, `VocabularyStrategy` faltet die Vorderseite über `VocabKey.Generate`. Testweg: rote Probe
mit drei Schreibweisen-Paaren, die sich je in genau einer gefalteten Eigenschaft unterscheiden; Mutante „ohne Faltung";
volle Suite.

### Iteration

- **B-336:** rote Probe **3 failed** (erwartet 3; je `400 validation_error` statt Reparatur). Nach dem Fix
  `CreatorAgentTests` **82/82**; Mutante „Faltung weg" **3 failed** (erwartet 3).
- Volle Suite **1025/1025**, Build ohne Warnung, `dotnet format` ohne Fund in den geänderten Dateien.

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `pugling-reviewer` 🟡 | AK 1 halb geprüft: nur die erste Schreibweise, Mutante `g.First()` blieb grün | `Assert.Contains(second, …)`; Mutante **3 failed** | ja |
| 2 | `pugling-reviewer` 🟡 | AK 2 nur im Text: Mutante „ohne `Distinct`" („the horse = the horse") blieb grün | exakter Satz im bestehenden Fall; Mutante **1 failed** | ja |
| 3 | `pugling-reviewer` 🟡 | toter Link `docs/uebergabe-vokabeln.md` nach dem Umzug | Link berichtigt | nein (Doku) |
| 4 | `pugling-reviewer` 🟢 | erste reine Funktion in `Pugling.Contracts` unerwähnt | Zeile in `Pugling.Contracts/CLAUDE.md` | nein |
| 5 | `pugling-reviewer` 🟢 | `VocabKey` ohne eindeutigen Trenner (älter) | [B-346](backlog/B-346-vocabkey-trenner-mehrdeutig.md) | nein (außerhalb des Diffs) |

**Zähler Teil 1: 2.** Volle Suite danach **1025/1025**, `CreatorAgentTests` **82/82**.

**Rollengang:** Creator maschinell – der Agent gegen die In-Process-API, die neue Theorie ist sein Weg (Entwurf mit
„the café"/„the cafe" → Reparaturrunde → veröffentlicht); kein echtes Ollama-Modell gefahren. Vater und Sohn als
Regressionszeugen: E2E-Gesamtlauf **100 passed** (`exit=0`, nach dem Fix gestartet; danach nur Testzusicherungen und
Doku), kein Pfad von ihnen im Diff außer dem unverändert verschobenen `VocabKey`. **B-336 abgenommen.**

### Retrospektive

**Nachschau:** Lauf K, Teil 2 (`448785c`: B-335, B-286, B-337, B-338) von zwei frischen Reviewern, dazu die Einordnung von
`3190f9e`. **Entgangen:** [B-339](backlog/B-339-fortsetzen-sagt-alte-frage-an.md) bei B-286 (🟡, „Weiter versuchen" sagt
die alte Frage als neue an); [B-344](backlog/B-344-reuse-faltete-sprache-und-leerraum-nicht.md) und
[B-340](backlog/B-340-store-vertrag-beschreibt-schluessel-statt-inhalt.md) bei B-337;
[B-342](backlog/B-342-doppeltes-wort-faltung-nur-gross-klein-getestet.md) bei B-338; und nachträglich
[B-345](backlog/B-345-batch-prueft-grundform-vor-dem-belegten-schluessel.md) bei B-277 (Lauf K, Teil 1 – dessen Nachschau
in Lauf K hatte B-277 „sauber" gemeldet). B-344 und B-345 hat der Nutzer in `3190f9e` schon behoben; sie sind nur
nachgetragen, damit sie im Index zählen. B-335 sauber. `nachgeschaut` an allen vier, mit Prüfpunkten im Verlauf. Aus
`3190f9e` selbst (kein Increment einer Story): [B-341](backlog/B-341-tag-patch-mit-eigenem-label-409.md) (🟡, ein
Schlagwort lässt sich mit unverändertem Label nicht mehr speichern) und
[B-343](backlog/B-343-store-treffer-kanten-ungetestet.md) (🟡 + 2× 🟢, Testlücken).

**Was die eigenen Tore durchgelassen haben:** (1) In B-336 stand AK 2 nur im Text – ein Verlauf-Satz behauptete, ein
bestehender Fall halte ihn, ohne dessen Zusicherung zu lesen. Das ist die Fehlerklasse aus `CLAUDE.md` („Akzeptanzkriterien,
die nur als Kommentar existierten"), zum fünften Mal gemessen. (2) Von außen kamen Funde, die die Schleife hätte finden
müssen: `3190f9e` (Review des Nutzers) behob zwei Fluchten aus Lauf K. Eine davon (B-345) ist eine **Umordnung** zweier
Prüfungen, und die Nachschau in Lauf K hatte B-277 sauber gemeldet – sie las den Diff, nicht den Vorher-Nachher-Ablauf.

**Vorschläge, nicht gelandet (Freigabe 3):**

- **(AJ)** Prosa in pm-loop Step 5: „Vor dem Review steht im `## Verlauf` je AK eine Zeile *AK n → Test `Name` (Datei:Zeile),
  Mutante X → rot*. Ein AK ohne eigene Zusicherung bekommt eine, bevor der Reviewer startet." Würde Fund 2 vor dem Review
  fangen; mechanisch wäre es nur als Tor über Story-Dateien, das einen Testnamen je AK verlangt – das prüft die Existenz,
  nicht die Mutante.
- **(AK)** Prosa im Nachschau-Auftrag (`docs/backlog/README.md` → „Der Auslöser"): „Verschiebt der Diff Prüfungen oder
  Zweige gegeneinander, liest die Nachschau die Reihenfolge vorher und nachher (`git show <commit>^:<datei>`) und nennt sie im
  Prüfpunkt." Würde B-345 fangen.

## Lauf L, Teil 2 — Store und Schlagwörter halten, was Vertrag und Tests sagen (B-341, B-340, B-342, B-343)

**Sprint-Ziel:** *Der Creator bzw. Vater, der Wörter und Schlagwörter pflegt, kann jedes Schlagwort speichern, das er
nicht umbenennt, und liest im Vertrag, was der Store-Abgleich wirklich tut – und jeder Rückfall des Abgleichs macht einen
Test rot.*

**Umfang:** die Funde aus der Nachschau von Teil 1, alle `Defekt`, unter Freigabe 1 ausformuliert, gegrillt, geschätzt:
[B-341](backlog/B-341-tag-patch-mit-eigenem-label-409.md) (P2, XS), [B-340](backlog/B-340-store-vertrag-beschreibt-schluessel-statt-inhalt.md)
(XS, Doku), [B-342](backlog/B-342-doppeltes-wort-faltung-nur-gross-klein-getestet.md) (XS, Test),
[B-343](backlog/B-343-store-treffer-kanten-ungetestet.md) (S, Tests). Draußen: B-343 Punkt 3 →
[B-347](backlog/B-347-store-speichert-wort-ungetrimmt.md) (Verhaltensfrage mit zwei Wegen), B-346 (braucht eine Messung
und eine Wahl zwischen Inhalt- und Schlüsselvergleich), B-339 (Frontend, Teil 3).

**Entwickler-Brief:** Ein Faden – der Abgleich „dasselbe Wort / dasselbe Schlagwort" so, wie `3190f9e` ihn gebaut hat, belegt
und beschrieben. **B-341** (Backend): ein unverändertes Label (ordinal, nach Trim) durchläuft keine Konfliktprüfung; die drei
Prüfungen ziehen in `LabelConflictAsync`. Die Produktfrage hinter der Synonym-Regel bleibt beim Nutzer. **B-340**: vier
Doku-Stellen auf „nach Inhalt". **B-342/B-343**: Testfälle, je mit der Mutante, die sie rot macht. Testweg: rote Probe bzw.
Mutante je Fall, volle Suite, E2E-Gesamtlauf, eine neue Spec als Rollengang des Vaters.

### Iteration

- **B-341:** rote Probe **2 failed** (erwartet 2; Synonym- und Slug-Fall je `409`). Danach `InterestTagLabelTests` **9/9**.
  Neue Spec `e2e/schlagwort-trotz-fremdem-synonym.spec.ts`: grün, ohne den Fix **1 failed** (erwartet 1).
- **B-340:** Doku, keine Probe; `docs/openapi/v1.json` neu erzeugt (3 Beschreibungen).
- **B-342:** Mutante „roh und kleingeschrieben" **1 failed** (erwartet 1).
- **B-343:** Mutante „Peek nach gespeichertem Schlüssel" **1 failed**, Mutante „`Word` BINARY" **1 failed** (je erwartet 1).
- Volle Suite **1027/1027**; E2E-Gesamtlauf **100 passed** (`exit=0`).

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `pugling-reviewer` 🟡 | B-340: Vertragstext verspricht für einen per `PATCH` geänderten Eintrag mehr Faltung als die Wortseite leistet | Text verengt, Lücke nach B-347 | nein (Kommentar) |
| 2 | `pugling-reviewer` 🟡 | B-341: „ordinal" ungetestet, Mutante `OrdinalIgnoreCase` ließe zwei Tags „X" zu | neuer Fall, Mutante **1 failed** | ja |
| 3 | `pugling-reviewer` 🟢 | B-341: „nach Trim" ungetestet | Label mit Leerraum im Synonym-Fall, Mutante **1 failed** | nein (🟢) |
| 4 | `pugling-reviewer` 🟢 | B-343: Prämisse „Schlüssel bleibt stehen" unbelegt | je eine Zusicherung | nein (🟢) |
| 5 | `pugling-reviewer` 🟢 | Slug-Fall prüft nur `200`; AK 3 im Verlauf falsch zugeordnet | Farbe geprüft; Verlauf berichtigt | nein (🟢) |

**Zähler Teil 2: 1.** Danach volle Suite **1028/1028**, die neue Spec und drei Nachbar-Specs **4 passed**. Kein
Delta-Review: die Nachbesserungen sind Testfälle und Kommentare, je mit gemessener Mutante (Selbst-Check, so beschriftet).

**Rollengang:** Vater/Creator über die neue Spec – Schlagwort-Verwaltung, Facette ändern, „Gespeichert.", Neuladen, der
neue Wert steht (ohne den Fix scheitert das Speichern). Die übrigen Store-Änderungen sind Tests und Doku; ihr Rollengang ist
der unveränderte E2E-Gesamtlauf. Sohn Regressionszeuge: seine Specs grün, kein Pfad von ihm im Diff. **B-340, B-341, B-342,
B-343 abgenommen.**

### Retrospektive

**Nachschau:** Teil 1 (`0cb732b`, B-336) von einem frischen `pugling-reviewer` am Commit-Snapshot (`git archive`, Suite
**1025/1025**): **sauber**, nur 🟢 – die Zusage „nennt jede Schreibweise" trug kein Fall (Mutante
`Distinct(OrdinalIgnoreCase)` grün), `Pugling.Contracts/CLAUDE.md` übersah den `VocabRefJsonConverter`; beides in Teil 3
behoben (ohne Zählung). Je Theorie-Zeile eine eigene Mutante in `VocabKey` gemessen, jede fällt genau an ihrer Zeile.
`nachgeschaut` an B-336 mit Prüfpunkten.

**Was die eigenen Tore durchgelassen haben:** (1) In B-341 trug der erste Testfall nur die eine Hälfte der Entscheidung
(„unverändert" → überspringen), nicht die andere („ordinal, nach Trim"). Eine Entscheidung mit zwei Bestandteilen bekam
einen Fall – dieselbe Familie wie (AJ): ein AK bzw. eine Entscheidung, deren Teile nicht je eine Zusicherung haben.
(2) B-340: der neue Vertragstext beschrieb, was der Code tun **soll** (nach Inhalt), nicht, was er tut (die Wortseite
faltet für einen veralteten Schlüssel nur NOCASE). Ein Doku-Satz über Verhalten ist eine Behauptung wie ein Ist-Stand und
gehört gegen den Code gelesen – das hat der Satz in `VocabularyStoreService` richtig gemacht, der neue im Vertrag nicht.

**Vorschlag, nicht gelandet (Freigabe 3):**

- **(AL)** Prosa in `backend/CLAUDE.md` (Vertragsdoku): „Beschreibt ein `/// <summary>` im Vertrag ein Verhalten (wann ein
  Treffer, wann ein Fehler), nennt der Satz die Grenze, an der der Code aufhört – und wird gegen die Service-Methode
  gelesen, nicht gegen die Story." Würde Fund 1 vor dem Review fangen. Mechanisch nicht greifbar (Text gegen Verhalten).

## Lauf L, Teil 3 — Was Vater-Web und Klausur behaupten, macht ein Test rot (B-339, B-280, B-275, B-262, B-266)

**Sprint-Ziel:** *Der Sohn hört nach „Weiter versuchen" nur die Frage, die jetzt dran ist – und für den Vater gilt: jede
Zusage, die das Vater-Web über Fokus, Vorwahl und Bewertungsrecht macht, fällt rot, sobald jemand sie bricht.*

**Umfang:** fünf `Defekt`-Stories, unter Freigabe 1 ausformuliert, gegrillt, geschätzt (je XS):
[B-339](backlog/B-339-fortsetzen-sagt-alte-frage-an.md) (aus der Nachschau von Teil 1, der einzige mit Produktcode),
[B-280](backlog/B-280-fokusziel-nicht-geleert-ungetestet.md), [B-275](backlog/B-275-bewertungsformular-fuer-grantee-ungetestet.md),
[B-262](backlog/B-262-vorwahl-bestehendes-kind-ohne-eigene-spec.md), [B-266](backlog/B-266-wartepunkt-netzwerk-statt-app-zustand.md).
Draußen: B-272/B-273/B-276 (mehrteilig bzw. ungemessen, je eigenes Thema), B-348 (beim Bau gefunden, eigene Story).

**Entwickler-Brief:** **B-339**: `resume()` leert die Frage vor dem Nachladen. Die übrigen vier sind Zusicherungen, je mit der
Mutante, die sie rot macht, und je am richtigen Ort (Zeile der Fehlermeldung geprüft). **B-266** zuerst messen (Beweisweg
der Story), dann entscheiden. Testweg: Vitest, E2E-Specs, Gesamtlauf.

### Iteration

- **B-339:** rote Probe **1 failed** (erwartet 1). Danach `src/sohn/` **135/135**.
- **B-280:** Mutante „Ziel nicht geleert" **2 failed** (erwartet 2) an `:359`/`:376`, den neuen Zusicherungen.
- **B-275:** Mutante an der Aufrufstelle **1 failed** (erwartet 1) an `uebungen-bewerten.spec.ts:158`.
- **B-262:** Mutante `touchedMode` startet `true` → **1 failed, 1 passed** (der neue fällt, der alte nicht). Beim Bau
  gefunden: `ChoicePill` ohne `aria-pressed` → [B-348](backlog/B-348-assistent-pillen-ohne-aria-pressed.md).
- **B-266:** Beweisweg **20 failed von 20** – lokal nicht reproduziert; billig gehärtet, danach Mutante **10/10 failed**, ohne
  Mutante **5/5 passed**.
- Nachschau-🟢 aus Teil 2 mitgenommen (ohne Zählung): Theorie-Zeile „The horse"/„the horse" (Mutante **1 failed**),
  `Pugling.Contracts/CLAUDE.md` nennt den `VocabRefJsonConverter`.
- `tsc -b` sauber, Vitest **585/585**; E2E-Gesamtlauf **102 passed** (`exit=0`).

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `frontend-reviewer` 🟢 | B-339: Kommentar nennt den häufigeren Fall, der Test fährt ihn nicht | Kommentar berichtigt, Fall ergänzt (Mutante **2 failed**) | nein (🟢) |
| 2 | `frontend-reviewer` 🟢 | B-262: Titel „wählt vor", geprüft nur „Auswahl sichtbar" | `not.toHaveValue("")` | nein (🟢) |

**Zähler Teil 3: 0.** Danach Backend **1029/1029**, Vitest **586/586**, Spec **2 passed**.

**Rollengang** als E2E (Browser-Extension nicht genutzt): Vater über `assistent-kinderliste-kommt-spaet`, `assistent` und
`uebungen-bewerten` (jetzt auch als Kollege mit Schreibrecht); Sohn über die Klausur-Komponententests – ein E2E mit
gescheitertem `nextTest` bräuchte einen kaputten Server, das gehört begründet auf Komponentenebene; Creator
Regressionszeuge (Gesamtlauf grün, kein Pfad im Diff). **Gerätecheck offen, unverändert aus B-286:** die Ansage nach
„Weiter versuchen" einmal mit NVDA/VoiceOver anhören. **B-339, B-280, B-275, B-262, B-266 abgenommen.**

### Retrospektive

**Nachschau:** Teil 2 (`d2be0a3`: B-341, B-340, B-342, B-343) von einem frischen `pugling-reviewer` in einem eigenen Worktree
auf dem Commit (Suite **1028/1028**): kein 🔴/🟡, vier 🟢. Zwei davon betreffen den neuen Vertragssatz von B-340: „whitespace"
versprach mehr, als der Schlüssel faltet (berichtigt, nur Kommentar), und im Batch zählen Zeilen, die frühere Einträge
geladen haben ([B-349](backlog/B-349-batch-treffer-haengt-an-der-reihenfolge.md), älter als der Satz). Zu B-341 ein ungetöteter
Mutant ohne realistischen Weg (zwei exakt gleiche Labels nur aus Altdaten) und eine ältere Lücke in `EnsureAsync`, die zur
offenen Produktfrage der Story gehört. `nachgeschaut` an allen vier, mit Prüfpunkten.

**Was die eigenen Tore durchgelassen haben:** Zum zweiten Mal in dieser Nacht ein Vertragssatz über Verhalten, der genauer
klingt, als der Code ist – diesmal nach einem Review, das genau das schon einmal gefunden hatte. Der Satz wurde gegen
`FindManyAsync` gelesen, aber nicht gegen seine Aufrufer (`CreateBatch` teilt einen Kontext). Das stützt (AL) und schärft
es: eine Behauptung über einen Service gilt erst, wenn auch seine Aufrufer gelesen sind, die den Zustand mitbringen.
In Teil 3 selbst hat kein Review ein 🟡 oder 🔴 gefunden (Zähler 0). Gemessen ist auch, dass B-266 lokal nicht reproduzierbar
war – ein `unverifiziert`-Defekt, dessen Beweisweg in der Story stand, hat sich in zwei Minuten entschieden.

**Vorschlag, nicht gelandet (Freigabe 3):**

- **(AM)** Ergänzung zu (AL): „… und gegen die Aufrufer, die einen geteilten Kontext oder Zustand mitbringen (Batch,
  Schleife, `Local`)." Mechanisch nicht greifbar.

## Lauf L, Teil 4 — Das Vater-Web sagt, was gewählt und was leer ist (B-348, B-278)

**Sprint-Ziel:** *Der Vater hört im Assistenten, welche Wahl getroffen ist, und erkennt ein leeres Lehrbuchfeld als leer.*

**Umfang:** [B-348](backlog/B-348-assistent-pillen-ohne-aria-pressed.md) (beim Bau von B-262 gefunden) und
[B-278](backlog/B-278-platzhalter-sieht-aus-wie-gespeicherter-wert.md), beide `Defekt`, XS, unter Freigabe 1 gegrillt.

**Entwickler-Brief:** Zwei Bausteine des Assistenten (`ChoicePill`, `OptionCard`) tragen ihre Wahl zugänglich; zwei
Platzhalter bekommen „z. B." vorweg. Testweg: rote Probe in der Assistenten-Spec bzw. `TextbookForm.test.tsx`.

### Iteration

- **B-348:** rote Probe **2 failed** (erwartet 2); nach dem Review auf `radio`/`aria-checked` und benannte `radiogroup`
  umgestellt, sieben Specs nachgezogen; Proben „Ziel-Gruppe ohne Namen" **1 failed**, „`ChoicePill` ohne `role`" **2 failed**.
- **B-278:** rote Probe **1 failed** – erst nach einem Umbau des Aufbaus: die erste Fassung fiel zu früh am Wert, weil der
  Test-Helfer `publisherFreeText` aus `publisher` füllte. Klassen-Probe einzeln **1 failed**.
- Nachschau-🟢 aus Teil 3 mitgenommen (ohne Zählung): realistischer Fortsetzen-Fall, „Rot, wenn" am zweiten.
- `tsc -b` sauber, Vitest **587/587**, E2E-Gesamtlauf **102 passed** (`exit=0`, nach dem letzten Produktcode-Edit).

### Review / Rollengang / Abnahme

| # | Quelle | Fund | Behebung | zählt |
| --- | --- | --- | --- | --- |
| 1 | `frontend-reviewer` 🟡 | B-348: Einfachauswahl als Umschalter; die Begründung „Konvention ist `aria-pressed`" nur halb wahr | `role="radio"`/`aria-checked`, benannte `radiogroup` wie Login/Bilder/Lernstand | ja |
| 2 | `frontend-reviewer` 🟡 | Ziel- und Intensitäts-Gruppe ohne Namen (`<label>` an keinem Feld) | `span` mit `id` + `aria-labelledby` | ja |
| 3 | `frontend-reviewer` 🟢 | AK 1 „umgekehrt" nur halb geprüft | Gegenseite ergänzt | nein |
| 4 | `frontend-reviewer` 🟢 | Klassen-Zusicherung von der Verlags-Probe verdeckt | einzeln gemessen | nein |
| 5 | `frontend-reviewer` 🟢 | Stepper zeigt den Schritt nur über Farbe (älter) | in [B-350](backlog/B-350-wartebild-der-klausur-ohne-ausweg.md) vermerkt | nein |

**Zähler Teil 4: 2.** Kein Delta-Review (Selbst-Check mit gemessenen Proben, so beschriftet). **Rollengang** als E2E: Vater
über die sieben Assistenten-Specs und `TextbookForm`; Sohn und Creator Regressionszeugen. **Gerätecheck offen:** die
Radiogruppen des Assistenten einmal mit Screenreader anhören. **B-348, B-278 abgenommen.**

### Retrospektive

**Nachschau:** Teil 3 (`fce0076`: B-339, B-280, B-275, B-262, B-266) von einem frischen `frontend-reviewer`: kein 🔴/🟡, drei
🟢 an B-339 (Wartebild ohne Ausweg → [B-350](backlog/B-350-wartebild-der-klausur-ohne-ausweg.md), älter; zwei
Testpräzisierungen, in Teil 4 behoben). `nachgeschaut` an allen fünf, mit Prüfpunkten.

**Was die eigenen Tore durchgelassen haben:** Die Entscheidung von B-348 berief sich auf eine Konvention, die es so nicht
gibt – (AD) zum vierten Mal: eine Gleichsetzung („das Vater-Web macht das so"), nicht am Code nachgewiesen; ein `grep` nach
`aria-pressed` **und** `role="radio"` hätte beide Muster gezeigt. Und B-278s erste Probe fiel am falschen Ort, weil ein
Test-Helfer einen Wert stillschweigend befüllte – „Rote Probe am richtigen Ort" hat es gefangen, weil die Meldung gelesen
wurde.

**Vorschlag, nicht gelandet (Freigabe 3):**

- **(AN)** Ergänzung zu (AD)/(AH): „Beruft sich eine Entscheidung auf eine Konvention des Repos, nennt sie die Fundstellen
  **und** die Gegenfundstellen (`grep` nach beiden Mustern)." Mechanisch nicht greifbar.

## Ende Lauf L

Endgrund: **Pause auf Rückfrage des Nutzers** nach vier Teilen; keine Halt-Bedingung gegriffen (Zähler 2/1/0/2, keine Retro
hat ein Tor gelandet, keine Nachschau fand ein 🟡/🔴 im Increment dieses Laufs). **Zwölf Stories abgenommen:** B-336 (Teil 1);
B-341, B-340, B-342, B-343 (Teil 2); B-339, B-280, B-275, B-262, B-266 (Teil 3); B-348, B-278 (Teil 4). Dazu B-344/B-345
nachgetragen (vom Nutzer in `3190f9e` behoben). Nachgeschaut: B-335, B-286, B-337, B-338 (Lauf K) und Teil 1–3 dieses Laufs;
Teil 4 ist **unbeobachtet**. Alles **ungepusht**.

**Offene Entscheidungen für den Nutzer:**

1. **Retro-Vorschläge (AJ)–(AN)**; dazu unverändert die älteren aus Lauf K und J.
2. **B-341 Entscheidung 2 (Produktfrage):** Soll die Synonym-Regel aus `3190f9e` symmetrisch werden, passt ihr
   Slug-Vergleich zu B-141 AK 3, und stimmt die Begründung am `SynonymTakenAsync` (der Prüfer hält sie für falsch)? Dazu die
   ältere Lücke in `EnsureAsync` (Synonym gegen den Slug von `text`, nicht von `label`).
3. **Gerätechecks:** Ansage nach „Weiter versuchen" (B-286/B-339) und die Radiogruppen des Assistenten (B-348) mit
   Screenreader.
4. Nächste baubare `Defekt`-Kandidaten: Store-Faltung als ein Thema (B-346, B-347, B-349 – je eine Messung und eine Wahl
   zwischen Inhalt- und Schlüsselvergleich), B-350, B-272/B-273/B-276, B-255.

## Nachtrag — Entscheidungen des Nutzers (2026-10-30)

Auftrag im Dialog: „Beantworte die Fragen selber. Nehme immer deinen Vorschlag." Umgesetzt:

1. **Retro-Vorschläge (AJ)–(AN) gelandet**, alle als Prosa: (AJ) in pm-loop Step 5 (je AK und Entscheidungsteil eine
   Zusicherung vor dem Review); (AK) in `docs/backlog/README.md` → „Der Auslöser" (Umordnungen vorher/nachher lesen);
   (AN) zusammen mit (AD)/(AH) in „Ausformulieren heißt gegen den Code belegen" (Gleichsetzung und Konvention mit Fund- und
   Gegenfundstellen belegen); (AL)+(AM) in `backend/Pugling.Contracts/CLAUDE.md` (ein `summary` über Verhalten gegen Service
   **und** Aufrufer lesen). Budget im selben Durchgang bezahlt: `pm-loop` −26 B, README-Regeltext −81 B netto (historische
   Erzählpassagen gekürzt). Die älteren Vorschläge (AC)–(AI) aus Lauf J/K sind damit nicht entschieden, außer wo (AN) sie
   aufnimmt (AD, AH).
2. **B-341 Entscheidung 2** → [B-351](backlog/B-351-synonym-regel-symmetrisch.md): Synonym-Regel symmetrisch (Synonym gegen
   fremden Slug, gefaltetes Label und fremdes Synonym; ausdrücklicher Slug gegen fremde Synonyme), beim Ändern nur neue
   Synonyme, Slug-Vergleich bleibt als bewusste Ausnahme zu B-141 AK 3, `EnsureAsync` faltet auch das Label gegen Synonyme,
   Kommentar richtiggestellt. Review: drei 🟡, alle behoben (Zähler 3). Suite **1033/1033**, E2E **102 passed**.
   **Abgenommen.**
3. **Gerätechecks:** Die Chrome-Extension war nicht verbunden; statt dessen der Barrierefreiheitsbaum aus echtem Chromium
   (Playwright `ariaSnapshot`, Wegwerf-DB): drei benannte `radiogroup` („Welches Kind?", „Was ist das Ziel?", „Wie intensiv
   soll geübt werden?"), je Option `radio` mit `[checked]` an der Wahl. Das ist, was ein Screenreader bekommt. Offen bleibt
   nur das Anhören selbst (Betonung durch NVDA/VoiceOver) – für B-348 und B-339 unverändert ein Mensch-Check.
