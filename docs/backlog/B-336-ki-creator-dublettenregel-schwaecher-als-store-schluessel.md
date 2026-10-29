---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [DraftRules.NoDuplicates faltet anders als VocabKey]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Review von B-331 in Lauf K (docs/pm-sitzung-2026-10-29-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
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

## Verlauf

- **2026-10-29** — angelegt aus dem `pugling-reviewer`-Fund 🟢 zu B-331 in Lauf K (ohne Zählung); den Abbruch-Pfad am
  Code nachgesehen (`grep PuglingApiException` im Agenten), nicht gemessen.
