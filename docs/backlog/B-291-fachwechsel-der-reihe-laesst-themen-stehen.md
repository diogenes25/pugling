---
tags: [typ/story, status/idee, bereich/katalog, rolle/creator]
aliases: [Fachwechsel einer Reihe lässt fremde Themen an Übungen und Units stehen]
status: idee
prio: P3
art: Frage
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf E Teil 4 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-291 · Wechselt eine Reihe ihr Fach, bleiben die Grammatik-Themen des alten Fachs an Übungen und Units stehen

Die Fachgrenze aus [B-155](B-155-grammatik-themen-als-tags.md) gilt **beim Schreiben** eines Trägers: eine Übung
oder Unit nimmt nur Themen des Fachs ihrer Reihe an. Ein `PATCH` der Reihe, der `SubjectId` ändert oder leert
(`TextbookSeriesController`), lässt jede bestehende Themen-Zuordnung des alten Fachs stehen – und der nächste PUT
einer solchen Übung mit ihren eigenen Ids wird von `GrammarTopicsProblemAsync` abgewiesen: der Creator muss die
Themen entfernen, um überhaupt etwas speichern zu können.

Das ist dieselbe Drift, die `CategoryId` beim Fachwechsel einer Reihe heute schon hat (dort ebenfalls nicht
behandelt). Zwei Wege, beide eine Produktentscheidung: (a) der Fachwechsel wird mit 409 verweigert, solange ein
Träger der Reihe Themen trägt; (b) der Fachwechsel löst die Zuordnungen (und Kategorien) als bekannte Folge, mit
Hinweis im Löschdialog/Formular. Bis dahin ist es eine bekannte Grenze von B-155, dort im Verlauf benannt.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf E Teil 4 (`1c1fe62`) ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  🟡-Fund des frischen Prüfers; als `Frage` abgelegt (Entscheidung zwischen 409 und Lösen), nicht als Entgleiten.
