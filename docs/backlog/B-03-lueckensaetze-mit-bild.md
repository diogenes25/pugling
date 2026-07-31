---
tags: [typ/story, status/geschaetzt, bereich/medien, bereich/training, lerntechnik/vokabeln, lerntechnik/lueckentext, rolle/student]
aliases: [Lückensätze mit Bild, Idee 1]
status: geschaetzt
prio: P3
art: Wunsch
groesse: M
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/backlog-vokabellernen.md#runde-1--idee-1-lückensätze-mit-bild-als-vokabel-vertiefung
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
