---
tags: [typ/story, status/idee, bereich/backend, rolle/student]
aliases: [Vokabel-Tag gibt Übersetzungen preis, TaggedVocabularyDto trägt die Lösung,
  Kind liest jede Übersetzung des Stores]
status: idee
prio: P1
art: Defekt
quelle: B-80 (Schätzung, Befund außerhalb des Schnitts)
unverifiziert: true
---

# B-81 · Über die Vokabel-Tags kann ein Kind jede Übersetzung des Stores lesen

Dieselbe Bauart wie [B-80](B-80-tags-geben-fremde-konfiguration-preis.md), aber ein anderer Endpunkt und ein
anderes DTO — von den dortigen Entscheidungen also **nicht** miterledigt:

1. `POST tags/{tagId}/vocabulary` prüft von den Vokabel-Ids **nur die Existenz**
   ([TagsController.cs:209-211](../../backend/Pugling.Api/Controllers/Creator/TagsController.cs)) — kein
   Eigentum, keine Zuweisung. Der Controller trägt klassenweit nur `[Authorize]` (`:20`, bewusst), ein Kind
   darf also taggen.
2. `GET tags/{tagId}/vocabulary` (`:241-257`) antwortet mit
   `TaggedVocabularyDto(Id, Key, Word, Translation)`
   ([TagDtos.cs:21](../../backend/Pugling.Contracts/Creator/TagDtos.cs)) — **Wort und Übersetzung**.

Damit kann ein Kind zu jeder Store-Vokabel das Paar lesen. Das ist wörtlich die Antwort, die der Spielpfad
auf getippten Stufen zurückhält (`CardFacets` gibt dort kein `reveal`) — also dieselbe ausgehebelte
Zusicherung wie in B-80, nur mit der Vokabel statt der Konfiguration.

**Nicht durch B-80 gedeckt:** dessen **E1** entfernt `Config` aus `ExerciseBrief`, ein anderer Typ; dessen
**E2** beschränkt den Übungs-Schreibpfad, einen anderen Endpunkt. Der symmetrische Schnitt wäre hier: für ein
Student-Token nur Vokabeln zulassen, die in einer ihm zugewiesenen Übung vorkommen — und/oder
`Translation` aus dem Lese-DTO nehmen. Was von beidem, ist die Entscheidung, nicht der Befund.

## Zu prüfen beim Ausformulieren

- **Erst nachspielen** mit einem Kind-Token (B-80 hat das Rezept: Wegwerf-DB auf `:5280`, `auth/child`).
- Ob `Translation` an `TaggedVocabularyDto` überhaupt einen Verbraucher hat — bei B-80 hatte das
  entsprechende Feld **keinen**, und das machte die Reparatur dort billig.
- Ob weitere Endpunkte Wort/Übersetzung an ein Student-Token ausgeben (der Vokabel-Store selbst ist
  Creator-gegatet — bei B-80 waren alle Katalog-Wege `403`, das ist hier zu wiederholen).
- Ob der Lernstand-Weg betroffen ist: `ChildVocabularyProgressController` liegt unter
  `api/v1/student/…` und ist damit ausdrücklich für das Kind gedacht — was gibt er über das Wort hinaus
  heraus? (Vermutung: er ist in Ordnung, weil das Kind seinen eigenen Stand lesen darf. **Nicht geprüft.**)
- Ob die Kind-skopierten Vokabel-Tags (B-24-Bereich, `VocabularyTag`) daran etwas ändern.

## Verlauf

- **2026-08-03** — angelegt beim Schätzen von B-80, als der Befund derselben Bauart am Nachbar-Endpunkt
  auffiel. Der Ist-Stand ist **am Code** belegt (`:209-211` prüft nur Existenz, `TagDtos.cs:21` trägt
  `Translation`), aber **nicht am laufenden System nachgespielt** — darum `unverifiziert: true`.
  `prio: P1` in Analogie zu B-80 vorgeschlagen (dieselbe Anti-Cheat-Zusicherung, ohne Zutun des Vaters
  ausnutzbar) — nicht vom Nutzer bestätigt. Bewusst **nicht** in B-80 eingefaltet: dessen Stufe `gegrillt`
  ist abgeschlossen und seine Akzeptanzkriterien sind final; eine sechste Entscheidung hätte sie wieder
  aufgemacht (dieselbe Handhabung wie B-76 → [B-79](B-79-position-stufe-unvalidiert.md)).
