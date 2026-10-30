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
