---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/student]
aliases: [Bonusrunde Sonderzeichen falsches Kästchen]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: "Nachtlauf 2026-11-18, Nachschau von B-661 (96a3491d, frontend-reviewer) – docs/pm-sitzung-2026-11-18.md, Runde 5"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-661]
---

# B-671 · In der Bonusrunde schreibt die Sonderzeichen-Leiste ins Kästchen der vorigen Karte

Aus der Nachschau von B-661, am Code gelesen, nicht im Browser nachgestellt: zwei Kästchen-Karten nacheinander, auf der
ersten Kästchen 5 gefüllt und abgegeben ⇒ das erste „é" der nächsten Karte landet in Kästchen 5 statt 1.

## User Story

Als Schüler möchte ich in der Bonusrunde nach dem Weiter in der ersten Lücke der neuen Karte schreiben – mit der
Tastatur wie mit der Sonderzeichen-Leiste –, damit ein Akzent nicht in einem Kästchen der vorigen Karte landet.

## Ist-Stand am Code

1. **Gemerktes Kästchen:** `LetterBoxes` hält `zuletzt` (`frontend/src/components/LetterBoxes.tsx:35`), setzt es bei
   jedem Fokus (`:116`) und nutzt es für ein Leisten-Zeichen, solange es kleiner als die Länge und nicht fest ist
   (`:83-84`); sonst das erste leere. Zurückgesetzt wird es nirgends.
2. **Die Kästchen überleben den Kartenwechsel:** `Antwortflaeche` hängt sie bewusst nicht neu ein, ein `key` von
   außen risse den Fokus weg (`frontend/src/sohn/Lektion.tsx:144-146`); die Kästchen tragen Index-Schlüssel
   (`LetterBoxes.tsx:102`).
3. **Üben und Test fokussieren bei jedem Kartenwechsel das erste Bedienbare der Antwortzone** – damit auch Kästchen 1
   und `zuletzt = 0` (`frontend/src/sohn/SohnPractice.tsx:196-203`, B-531; `frontend/src/sohn/SohnTest.tsx:89-102`,
   B-541).
4. **Die Bonusrunde tut das nicht:** `SohnBonus.tsx` ruft `.focus()` nur für die Fehlermeldung auf (`:181`); der
   Kartenwechsel (`answer`, `:222-229`) leert den Wert und rückt den Zeiger vor, mehr nicht. Die Fläche bekommt
   dieselbe Karten-Id wie im Test (`kartenId={card.itemIndex}`, `:319`).
5. **Test:** der vorhandene Fall deckt nur den Wechsel auf eine *kürzere* Karte (`LetterBoxes.test.tsx:105-112`). Ein
   Fall mit gleich langer Folgekarte: nicht gefunden.

## Die echte Lücke

Schmaler und breiter als die Notiz: nicht das gemerkte Kästchen ist die Ursache, sondern dass die Bonusrunde die
Fokusregel von Üben und Test nicht kennt. Wird die Karte per Enter abgegeben, **bleibt der Fokus im Kästchen 5 der
alten Karte stehen** – die Kästchen werden nicht ausgetauscht –, und dann landet nicht nur das Leisten-Zeichen dort,
sondern auch jede getippte Taste (`setChar(i)` über `onChange`, `LetterBoxes.tsx:60-70`). Wird per „Weiter"-Knopf
abgegeben, steht der Fokus auf dem Knopf und nur das gemerkte Kästchen irrt. Beides am Code gelesen; im Browser nicht
nachgestellt.

## Offene Punkte

1. **Wo wird repariert?** (a) Nur in `LetterBoxes` das gemerkte Kästchen zurücksetzen (Vorschlag der Notiz);
   (b) in `SohnBonusRun` dieselbe Fokusregel wie Üben/Test; (c) beides. **Empfehlung (c):** (a) allein heilt den
   Enter-Weg nicht (der Fokus bleibt im Kästchen 5, `onFocus` setzt `zuletzt` nie neu), (b) allein lässt den
   Knopf-Weg offen, wenn der Fokus nicht an die Kästchen geht.
2. **Wann setzt `LetterBoxes` zurück?** Bei jedem Leeren des Werts wäre falsch – wer Kästchen 3 antippt und dann ein
   Zeichen wählt, will Kästchen 3. **Empfehlung:** beim Verlassen der Kästchen (`onBlur` auf der Gruppe, Ziel außerhalb).
   Die Leiste nimmt dem Feld den Fokus nicht (`LetterBoxes.test.tsx:118-123`), löst also keinen Verlust aus.
3. **Bonus-Runde: Fokus nach Laden?** Üben setzt ihn nach dem ersten Aufbau bewusst nicht (B-443).
   **Empfehlung:** auch hier nur nach einer Handlung des Kindes, nicht beim Laden.

## Akzeptanzkriterien (Entwurf)

1. In der Bonusrunde steht nach der Abgabe einer Kästchen-Karte per Enter der Fokus im ersten Kästchen der nächsten
   Karte. Rot macht es: ein Bonus-Weg ohne Fokus beim Kartenwechsel (heute so) – im E2E, nicht per Vitest (Fläche mit
   Server).
2. Ein Leisten-Zeichen nach dem Kartenwechsel landet im ersten leeren Kästchen, auch bei gleich langer Folgekarte.
   Rot macht es: das gemerkte Kästchen bleibt über Blur und Kartenwechsel erhalten (heute so) – `LetterBoxes.test.tsx`,
   Fokus Kästchen 5, Blur, Leiste, Erwartung Kästchen 1.
3. Wer ein Kästchen antippt und dann ein Zeichen der Leiste wählt, setzt es dort ein (kein Blur dazwischen). Rot macht
   es: ein Zurücksetzen bei jedem Leeren des Werts statt beim Verlassen; der Fall steht schon (`:95-101`).
4. Üben und Test verhalten sich unverändert: ihre Fokus-Fälle bleiben grün.

## Verlauf

- **2026-11-18** — angelegt aus der Nachschau von B-661 (`frontend-reviewer`), `entgangen_bei: [B-661]`.
- **2026-11-19** — ausformuliert (Vorbereitungslauf): am Code gelesen (nicht im Browser nachgestellt); die Ursache
  liegt in der Bonusrunde (kein Fokus beim Kartenwechsel), und der Enter-Weg betrifft auch normale Tasten.
