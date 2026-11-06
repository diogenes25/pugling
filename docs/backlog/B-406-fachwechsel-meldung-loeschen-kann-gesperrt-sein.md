---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [Ausweg Löschen kann gesperrt sein]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-aa
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-05"
entgangen_bei: [B-403, B-405]
---

# B-406 · Der Ausweg „Übung löschen" in der Fachwechsel-Meldung kann selbst gesperrt sein

Der deutsche Satz zu `series_subject_in_use` (B-403, `frontend/src/lib/api.ts`) sagt „eine Übung mit einer Art dieses Fachs kannst du nur
löschen" und „Ganz entfernen lässt sich das Fach nur, solange die Reihe keine Übungen trägt". Eine Übung, die in einem Lehrplan steckt,
lässt sich aber nicht löschen (`409 exercise_in_use`, `ExerciseControllerBase.cs` um `:507`) – der Ausweg führt in die zweite Sperre.

Dazu zwei Beleg-Mängel aus derselben Nachschau:
- Die AK-Tabelle von [B-405](B-405-fach-leeren-meldung-verspricht-wechsel.md) zitiert die Zusicherung `once no exercise or unit carries`;
  seit dem Delta-Review heißt sie `once no unit carries grammar topics and no exercise`.
- `ReiheFachwechselTests` sichert Anzahlen mit `Assert.Contains("1 exercise(s) …")` zu – das passt auch auf „11 exercise(s)". Eine Mutante,
  die über alle Reihen zählt, wird darum nur rot, solange die geteilte Test-DB nicht zufällig eine passende Zahl liefert.

## Entscheidungen

1. **(Defekt, im Lauf gegrillt; nach Review geändert)** Kein Zwischenschritt: beide Sätze sagen, dass eine Übung nur löschbar ist, solange
   nichts sie benutzt; der deutsche verweist für die eigenen Stellen auf den Knopf „Verwendung". Ein Ausweg „aus dem Plan herausnehmen"
   wäre bei gespielten Positionen und fremden Plänen falsch (→ [B-407](B-407-gespielte-uebung-sperrt-fachwechsel-dauerhaft.md)).
2. **Vorschlag (BX) angewandt:** jeder genannte Ausweg wird als Integrationsfall **gegangen** – gesperrt → Ausweg → entsperrt.
3. Die Anzahl-Zusicherung prüft `StartsWith` statt `Contains` (die Meldung beginnt mit der Zahl).

## Akzeptanzkriterien

| Fall | Beleg | Rote Probe / Mutante |
|---|---|---|
| deutscher Satz nennt den Lehrplan | `errorMessage.test.ts` (`in einem Lehrplan`) | gegen den vorigen Satz gemessen: rot |
| englischer Satz nennt den Lehrplan | `ReiheFachwechselTests.Fach_Leeren_Mit_Uebungen…` (`study plan`) | gegen den vorigen Satz gemessen: rot (`Not found: "study plan"`) |
| Ausweg „Themen herausnehmen" (Unit-PATCH, Übungs-PUT) hebt die Sperre | `Ausweg_Themen_Herausnehmen_Entsperrt_Den_Wechsel` | **Gang**, vorher grün; rot unter Themen-/Unit-Mutante (gemessen) |
| Ausweg „Löschen" erst nach Herausnehmen aus dem Plan | `Ausweg_Loeschen_Geht_Bei_Einer_Verplanten_Uebung…` | **Gang**, vorher grün; rot unter Clear-/Reihenfilter-Mutante (gemessen) |
| Anzahl exakt | `AssertBlockedAsync` (`StartsWith`) | Reihenfilter-Übungen-Mutante: rot in vier Fällen (gemessen) |
| B-405-Tabelle zitiert die heutige Zusicherung | – | Text |

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf AA, Sprint 16 (Retro Sprint 17; frischer `frontend-reviewer`, `git show 9f395d2`,
  🟡/🟡/🟢). Increment **dieses** Laufs: **Limit 2**, Lauf AA endet nach Sprint 17. **Gegengerechnet und verworfen:** der Fund, die rote
  Probe in B-403 („expected … to contain 'Grammatik-Thema'") könne nicht stimmen, weil der alte Satz „Grammatik-Themen" enthielt –
  „Grammatik-Them**en**" enthält „Grammatik-Them**a**" nicht; die Ausgabe ist gemessen und steht so im Lauf.
- **2026-11-05** — gebaut in Lauf AB, Sprint 18. Rote Probe je Satz gemessen (Tabelle); die zwei Gänge liefen schon vorher grün – sie
  belegen, dass die Auswege die Sperre wirklich heben, nicht einen Defekt. Mutanten nach dem letzten Test-Edit erneut gemessen. Volle Suite
  1119/1119, Vitest 9/9, `tsc` sauber.
- **2026-11-05** — Review `pugling-reviewer`: 🔴 „aus dem Plan herausnehmen" ist bei gespielten Positionen (`position_has_data`) und fremden
  Plänen unmöglich; 🟡 auch Klassenarbeit und Ziel sperren das Löschen. Beide Sätze ohne Zwischenschritt neu, Zusicherungen gegen den
  vorigen Satz gemessen: rot (Vitest „nirgends mehr benutzt", Integrationsfall „only while nothing uses it"). **Zähler 2 von 5.** Die
  dauerhafte Sperre selbst → B-407 (Frage). Delta-Review: 🟡 „sagt dir die Meldung beim Löschen" stimmt im deutschen UI nicht (dessen Text
  nennt nur die Arten) → Verweis auf den Knopf „Verwendung" (`VaterExercises.tsx:348`), Zusicherung gegen den vorigen Satz gemessen: rot.
  **Zähler 3 von 5.** 🟢 englisch „names" → „counts". Kein weiteres Delta-Review; Selbst-Check (so beschriftet): Knopf „Verwendung" gibt es
  in der Übungsliste, er zeigt die eigenen Stellen, fremde laut `exercise_in_use`-Text nicht.
- **2026-11-05** — **abgenommen.** Rollengang Creator: `e2e/reihe-fachwechsel-gesperrt.spec.ts` 1 passed nach dem letzten Edit; volle
  Playwright-Suite 117 passed nach dem vorletzten (seither nur die zwei Meldungstexte); Integrationsfälle gehen beide Auswege. Volle Suite
  1119/1119. Vater und Sohn Regressionszeugen. Commit: siehe nächster Eintrag.
- **2026-11-05** — Commit `98e684d`.
- **2026-11-05** — **Nachschau** (Lauf AB, Retro Sprint 19; frischer `pugling-reviewer`, `git show 98e684d`, `ReiheFachwechselTests` 9/9):
  geprüft AK-Zeilen, beide Sätze gegen `CountBlockingAsync`/`Explain` und gegen „Verwendung", die zwei Gänge, Summaries. **Entgangen (🟡):**
  „Verwendung" zeigt keine Ziel-Etappen; zwei AK-Zeilen und ein Summary veraltet → [B-409](B-409-verwendung-zeigt-keine-ziel-etappen.md).
