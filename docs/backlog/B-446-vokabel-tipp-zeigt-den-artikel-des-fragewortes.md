---
tags: [typ/story, status/idee, bereich/training, rolle/student]
aliases: [Tipp zeigt den Artikel des Frageworts]
status: idee
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

