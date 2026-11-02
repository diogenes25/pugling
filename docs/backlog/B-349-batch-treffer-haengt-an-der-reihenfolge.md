---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [FindManyAsync filtert den ganzen Local-Cache]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf L, Teil 2 (docs/pm-sitzung-2026-10-30-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-11-02"
---

# B-349 · Im Vokabel-Batch hängt ein Treffer davon ab, was frühere Einträge desselben Batches geladen haben

`VocabularyStoreService.FindManyAsync` lädt Kandidaten (Schlüssel passt oder Wort gleicht unter NOCASE), filtert danach
aber den ganzen `db.Vocabularies.Local` – nicht nur die eben geladenen Zeilen. Im `CreateBatch`
(`VocabularyStoreController.cs`) bedient ein Kontext alle Einträge; was ein früherer Eintrag geladen hat (eigene
`FindAsync`-Kandidaten, `BaseForm` über `LoadGraphAsync`), zählt darum für spätere mit.

Repro aus dem Code (Nachschau-Prüfer): Eintrag E als `hus`/`Haus` angelegt, per `PATCH` zu `Hõuse`. Batch
[`hus`/`Haus`, `house`/`Haus`]: Eintrag 1 lädt E über den Schlüssel (kein Treffer, E faltet jetzt zu `house`), Eintrag 2
meldet `existing` → E, obwohl E für ihn weder Schlüssel- noch Wortkandidat ist (NOCASE faltet õ nicht). Allein geschickt
gibt Eintrag 2 `201`. Das Ergebnis hängt an der Reihenfolge, und zwar in der großzügigeren Richtung. Ein einzelnes
`Create` ist exakt.

Vorschlag: `Local` auf die Zeilen dieses Aufrufs (geladene Kandidaten plus ungespeichert hinzugefügte) einschränken –
oder die Wortseite ohnehin weiter falten (dann fällt der Unterschied mit [B-347](B-347-store-speichert-wort-ungetrimmt.md)
zusammen). Erst messen.

## User Story

Als **Creator**, der Vokabeln im Batch anlegt, möchte ich für eine Zeile dieselbe Antwort bekommen, egal was davor im
Batch stand, damit ein wiederholter oder umsortierter Batch dasselbe Ergebnis liefert.

## Ist-Stand (Lauf M, am Code belegt, Stand `d0afbc0`)

- `FindManyAsync` (`VocabularyStoreService.cs:82-87`) filterte den ganzen `db.Vocabularies.Local`, nicht nur die eben
  geladenen Kandidaten; `CreateBatch` (`VocabularyStoreController.cs:497-520`) bedient alle Zeilen mit einem Kontext.
- **Gemessen:** Eintrag `hus`/`Haus`, per `PATCH` zu „Hõuse". Batch [`hus`, `house`] → Zeile 2 `Existing`; `house` allein
  → `Created`.

## Die echte Lücke

Ein Vergleich über „was gerade getrackt ist" macht das Ergebnis vom Verlauf der Anfrage abhängig.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Mit [B-347](B-347-store-speichert-wort-ungetrimmt.md) gelöst:** die Kandidaten sind je Aufruf vollständig bestimmt
   (alle gespeicherten Einträge des Sprachpaars plus die ungespeicherten `Added`), aus dem Tracker zählt sonst nichts.
   Die großzügigere Antwort wird die Regel: „Hõuse" → „Haus" sagt „house" → „Haus". *Kosten:* keine über B-347 hinaus.

## Akzeptanzkriterien

1. `house`/`Haus` bekommt allein und nach einer Zeile, die den Eintrag geladen hat, dieselbe Antwort (`Existing`, der
   gepatchte Eintrag).

## Schätzung

`XS` über B-347 hinaus, Backend, keine Migration, kein Vertragsbruch. Testweg: `VocabularyStoreTests`.

## Verlauf

- **2026-10-30** — angelegt aus der Nachschau zu Lauf L, Teil 2 (`pugling-reviewer` über `d2be0a3`, 🟢). Aus Code gelesen.
- **2026-11-02** — Lauf M, Teil 1: ausformuliert, autonom gegrillt (Freigabe 1), mit B-347 gebaut. Rote Probe **1 failed**
  (erwartet 1): `ReuseExisting_ImBatch_HaengtNichtAnDerReihenfolge`, „allein" `Created` statt `Existing` (die erste
  Zusicherung „nach dem Laden" hielt schon vorher, die zweite fiel). AK 1 → dieser Test; Mutante „Kandidaten nur über die
  NOCASE-Wortspalte" → rot. Die Mutante „ganzer Tracker statt `Added`" ist im neuen Aufbau **nicht beobachtbar**
  (gespeicherte Treffer gewinnen vor jedem Tracker-Eintrag, getrackte Einträge sind dieselben Zeilen) – das ist der
  Beleg, dass die Abhängigkeit weg ist, nicht eine Lücke.
- **2026-11-02** — Review `pugling-reviewer` (Teil 1, 1041/1041 am Review-Stand): kein 🔴. 🟡 die Klassen-Doku von `VocabKey` nannte den Schlüssel noch „den Begriff von dasselbe Wort" → berichtigt (Doku, ohne Zählung); 🟡 der B-349-Test behauptete „rot, wenn der ganze Tracker zählt" – im neuen Aufbau nicht erzeugbar → Beschriftung auf den Fall, der ihn wirklich rot macht (Kandidaten wieder über Schlüssel/NOCASE), und den Service-Satz entsprechend gekürzt (nur Kommentare, ohne Zählung). 🟢 behoben ohne Zählung: veraltete Testkommentare („key"/„NOCASE-Kandidat") in drei Testdateien, SQL-`trim()` faltet nur Leerzeichen (Kommentar präzisiert), Theorie-Zeile „`_` gegen Leerraum-Folge" ergänzt und im Vertragstext genannt. 🟢 bewusst stehen gelassen: ein Scan des Sprachpaars je Batch-Zeile (Entscheidung 1 von B-347, Kosten benannt). **Zähler Teil 1: 0.**
- **2026-11-02** — Abnahme Lauf M, Teil 1: volle Suite **1045/1045**, Build ohne Warnung. E2E **102 passed** (`exit=0`, nach dem letzten Produktcode-Edit gestartet). Rollengang: Creator maschinell über die neuen Integrationstests gegen die In-Process-API; Vater und Sohn Regressionszeugen (ihre Specs im E2E-Lauf grün, kein Pfad von ihnen im Diff). **Abgenommen**, Commit siehe Protokoll ([Lauf M](../pm-sitzung-2026-11-02-nacht.md)).
- **2026-11-02** — Nachschau in Lauf M, Teil 2 (frischer `pugling-reviewer`): **sauber**. Geprüft: beide Hälften des Batch-Tests nachgezeichnet (Zeile 1 legt `hus…_2` an, Zeile 2 findet E); Behauptung „Tracker ohne Einfluss" bestätigt – Projektion `AsNoTracking`, Nachladen derselben Zeilen, `CreateBatch` speichert je Zeile, der Reuse-Zweig ändert keine Identitätsfelder.
