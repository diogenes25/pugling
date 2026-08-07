---
tags: [typ/story, status/verworfen, bereich/medien, bereich/training, lerntechnik/vokabeln, lerntechnik/lueckentext, rolle/student]
aliases: [Lückensätze mit Bild, Idee 1]
status: verworfen
prio: P3
art: Wunsch
groesse: M
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/backlog-vokabellernen.md#runde-1--idee-1-lückensätze-mit-bild-als-vokabel-vertiefung
grund: >
  Seit B-76 (Commit 1125ee6) ist TranslationWordBank eine getippte Stufe — die einzige ungetippte
  Cloze-Stufe ist damit weg, und die Anti-Cheat-Regel "Bild nur auf ungetippten Stufen" lässt keine
  bebilderbare Stufe mehr übrig. Im Grillen vom 2026-08-07 entschieden: nicht durch eine neue eigene
  Bild-Stufe erzwingen und die Anti-Cheat-Regel nicht lockern (ein Bild neben einer Lückentext-Vokabel
  zeigt praktisch immer die gesuchte Vokabel selbst — die Regel zu lockern höhlte aus, was sie
  verhindern soll). Aufwand/Risiko einer neuen Stufe steht bei P3 in keinem Verhältnis zum Nutzen.
---

# B-03 · Lückensätze mit Bild als Vokabel-Vertiefung

## User Story

Als Vater möchte ich Lückensätze anlegen, die eine gelernte Vokabel in einem Beispielsatz zeigen
(„The ___ banana"), damit mein Sohn das Wort im Kontext statt isoliert lernt — wo sinnvoll mit einem
passenden Bild.

## Ist-Stand am Code · Entscheidungen

→ Grill-Protokoll vom 2026-07-30, **Idee 1**, Entscheidungen 1–4:
[backlog-vokabellernen.md](../backlog-vokabellernen.md#runde-1--idee-1-lückensätze-mit-bild-als-vokabel-vertiefung).

Kern, belegt: Der Cloze-Typ und die Bild↔Vokabel-Verknüpfung existieren vollständig; **das Frontend ist
fertig** (`sohn/SohnPractice.tsx:199` rendert `card.imageUrl` typ- und stufenagnostisch). Die echte Lücke
ist, dass der `MediaSelector` **keine Vokabel-Batches** kann: sein einziger Batch-Einstieg verlangt eine
`ExerciseItem.Id` (`MediaSelector.cs:55-91`), und Cloze-Lücken haben kein `ExerciseItem` — sie leben in der
`ConfigJson`.

## Akzeptanzkriterien

1. Ein Cloze-Gap mit `VocabKey` bekommt `ImageUrl`/`ImageAlt` aus dem `MediaSelector`, sofern für die
   Vokabel ein Bild hinterlegt ist und die Stufe **nicht getippt** ist.
2. Kein Treffer ⇒ kein Bild (kein Notnagel).
3. Cloze-Position wie jede andere einplanbar — keine neue UI.
4. Bildkonstanz über `ChildMediaPick`, Träger ist die **Vokabel**.
5. Der Abschlusstest liefert und zeigt **kein** Bild; `TestItem` trägt die Felder nicht mehr (→ B-01).

## Schätzung

**Größe: M** — Frontend ≈ 0, dafür ein neuer vokabel-basierter Batch-Pfad im `MediaSelector` samt
Freeze-Verhalten.

- **Reihenfolge:** nach **B-01**, das den Test-Pfad ohnehin anfasst — sonst wird derselbe Code zweimal
  angerührt.
- **Risiko:** die item-zentrierte Genauigkeits-Kaskade („hat das Item eigene Bilder, zählt ausschließlich
  diese Menge") muss vokabel-zentriert nachgebildet werden, sonst ändert sich die Bildwahl der
  Karteikarten mit.
- **Nebenbedingung:** In der Praxis erscheint das Bild auf **genau einer** Stufe, weil `WordBank` zwar im
  `ClozeStage`-Enum steht, aber nicht in den `StageOptions` (`BuiltInExerciseTypes.cs:131-135`).
- **Testweg:** Integrationstest, der eine Cloze-Position auf `TranslationWordBank` ausspielt und Bild bei
  hinterlegtem Motiv / kein Bild ohne erwartet; dazu B-06 mitnehmen, sonst sieht der Vater es nie.

## Verlauf

- **2026-07-30** — geerntet aus dem Grill-Protokoll vom selben Tag, Stufe `geschaetzt` übernommen.
- **2026-08-02** — **die Schätzung trägt nicht mehr.** [B-76](B-76-lueckentext-karte-ohne-luecke.md)
  (Entscheidung E6, Commit `1125ee6`) hat `TranslationWordBank` zu einer **getippten** Stufe gemacht, damit
  die Wortbank überhaupt ankommt. Damit ist **keine** Cloze-Stufe mehr untypisiert — und die Anti-Cheat-Regel
  „Bild nur auf nicht-getippten Stufen" lässt für den Lückentext keine bebilderbare Stufe übrig. Die
  „Nebenbedingung" oben (Bild auf genau einer Stufe) ist damit gegenstandslos, die Story in dieser Form
  unbaubar. Vor dem Bauen neu zu entscheiden: entweder eine eigene Bild-Stufe für den Lückentext, oder die
  Regel für Lückensätze begründet lockern (das Motiv zeigt dort den *Satz*, nicht die einzelne Lösung) —
  oder verwerfen. Gefunden vom `pugling-reviewer` beim Review zu B-76, nicht vom Nutzer entschieden.
- **2026-08-07** — **verworfen** (Grillen im Dialog, siehe `grund`): weder neue Bild-Stufe noch gelockerte
  Anti-Cheat-Regel — der Nutzen bei P3 rechtfertigt beides nicht. [B-06](B-06-cloze-preview-bild.md) hing an
  derselben Entscheidung und ist im selben Zug verworfen.
