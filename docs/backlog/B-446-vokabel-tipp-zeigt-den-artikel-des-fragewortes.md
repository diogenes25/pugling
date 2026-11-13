---
tags: [typ/story, status/abgenommen, bereich/training, rolle/student]
aliases: [Tipp zeigt den Artikel des Frageworts]
status: abgenommen
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-09-nacht.md
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-446 · Der Vokabel-Tipp zeigt den Artikel des Frageworts, nicht einen Hinweis auf die Lösung

Teil 3 von B-429 – aber kein Seed-Fehler. Der abgeleitete Tipp einer Vokabelkarte ist `r.Hint ?? v.Noun?.Article`
(`backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs:100`): der Artikel des **Wortes** (der Vorderseite). In
Richtung `front-to-back` ist das Wort die Frage – „la ville" bekommt den Tipp „la", Lösung „die Stadt". Erst beim Tauschen
(`WithDirection`) fällt der Tipp weg. Der Tipp wiederholt also genau in der häufigsten Richtung die Frage.

Zu entscheiden beim Ausformulieren (Produktfrage, darum nicht im Lauf gebaut): welcher Hinweis soll erscheinen – der
Artikel der **Lösung** (falls sie ein Nomen der Zielsprache ist), der Anfangsbuchstabe, oder in dieser Richtung gar keiner?
Und gilt das nur für den abgeleiteten Tipp oder auch für einen vom Creator gesetzten `ExerciseItem.Hint`?

## Verlauf

- **2026-11-09** — abgeteilt aus [B-429](B-429-seed-familie-widerspricht-dem-katalog.md) beim Grillen in Lauf AG (Sprint 27). Abgeteilt, weil die Ursache im Resolver liegt, nicht im Seed, und die Reparatur eine Produktentscheidung braucht.
- **2026-11-13** — Datenstand geprüft (Tageslauf, Kopie der echten DB, sieben Einträge mit Artikel): uneinheitlich. Beim Seed
  „house → Haus" trägt `Noun.Article` „das", den Artikel der **Lösung** – dort ist der Tipp nützlich. Bei den
  französischen Einträgen („la ville → die Stadt", „l'école", „le magasin") trägt er den Artikel des **Fragewortes**, das
  ihn schon enthält – dort wiederholt der Tipp die Frage. Vor jeder Code-Änderung zu entscheiden: welche Seite beschreibt
  `NounInfo`? Empfehlung des PM: die Seite des Wortes (`Word`) – vorher prüfen, wie Editor und Lader das Feld füllen; dann gehört der abgeleitete
  Tipp in Richtung `front-to-back` weg, und der Seed „house/das" wird berichtigt. Bleibt im Dialog.
- **2026-11-13** — Tageslauf (Sprint 65), **stellvertretend entschieden**: der Vertrag gilt – `NounInfo.Article` ist „Definite
  article in the target language" (`VocabBaseTypes.cs:50`), der Editor schlägt „der/die/das" vor; der Seed „house/das" ist
  richtig, die französischen Einträge sind die Abweichung. (Die frühere PM-Empfehlung „Seite des Wortes" widersprach dem
  Vertrag und ist damit verworfen.) Gebaut, ohne Migration: an der Senke (`ExerciseContentResolver.ArticleHint`) fällt ein
  abgeleiteter Tipp weg, wenn das gefragte Wort schon mit ihm beginnt – das deckt die vorhandenen Zeilen der echten DB; der
  französische Seed schreibt künftig den deutschen Artikel. Ein vom Creator gesetzter `ExerciseItem.Hint` bleibt unberührt.
  - Zielsprachen-Artikel bleibt Tipp → `VokabelTippTests.Artikel_Der_Zielsprache_Bleibt_Tipp`, V4 (abgeleiteten Tipp streichen) → rot.
  - Artikel des Frageworts kein Tipp → `…Fragewort_Traegt_Ist_Kein_Tipp`, V1 (Wache entfernt) → rot.
  - elidiert „l'" → `…Elidierter_Artikel…`, V2 (Apostroph-Zweig aus) → rot.
  - Wortgrenze „le"/„lecteur" → `…Ohne_Grenze_Bleibt_Tipp`, V3 (Grenze immer wahr) → rot.
  - Seed „la ville" trägt „die" → `…Franzoesischer_Seed…`, V5 (wieder `w.Article`) → rot.
  - `pugling-reviewer` (frisch): kein 🔴. 🟡 Seed leitete den Artikel aus dem ersten Wort der Übersetzung ab (ein Nomen
    ohne deutschen Artikel hätte die ganze Lösung als Tipp gezeigt) → behoben: die Liste trägt den deutschen Artikel
    ausdrücklich, V5 neu gemessen → rot. 🟢 zwei ungetestete Zweige → `…Typografisch_Elidierter…` (W1 → rot),
    `…Wort_Gleich_Artikel…` (W2 → rot); 🟢 Interpolation entfernt. 🟢 Datenstand: Kopie der echten DB geprüft – alle sechs
    französischen Zeilen beginnen mit ihrem Artikel, die Wache deckt sie; keine Datenkorrektur nötig.
  - Suite 1474/1474. Rollengang Sohn: der Tipp erscheint auf der Tippen-Stufe über dieselbe Senke wie die Vorschau
    (`PositionPlayService.CardFacets`), maschinell über die Vorschau-API belegt; Vater/Creator Regressionszeugen (Suite).
    **abgenommen** (stellvertretend entschieden, Veto beim Nutzer).
