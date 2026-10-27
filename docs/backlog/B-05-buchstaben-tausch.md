---
tags: [typ/story, status/abgenommen, bereich/training, bereich/frontend, lerntechnik/vokabeln, rolle/student]
aliases: [Buchstaben-Tausch, Anagramm, Idee 2]
status: abgenommen
prio: P5
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/backlog-vokabellernen.md#runde-1--idee-2-buchstaben-tausch-eingabe-anagramm
wartet_auf: ""
---

# B-05 · Buchstaben-Tausch-Eingabe (Anagramm)

## User Story

Als Sohn möchte ich eine Vokabel auch lernen, indem ich durcheinandergewürfelte Buchstaben in die richtige
Reihenfolge bringe, als Abwechslung zum Tippen und Auswählen.

## Ist-Stand am Code · Entscheidungen

→ Grill-Protokoll vom 2026-07-30, **Idee 2**, Entscheidungen 5–7:
[backlog-vokabellernen.md](../backlog-vokabellernen.md#runde-1--idee-2-buchstaben-tausch-eingabe-anagramm).

Kern, belegt: Die Übungsschleife ist bewusst **stage-agnostisch** (branched auf mitgelieferte Felder, nicht
auf einen Stufen-Enum), `components/LetterBoxes.tsx` ist reine Einzelfeld-Eingabe ohne Anagramm, und es gibt
**keine Drag-&-Drop-Library** im Projekt. Die echte Lücke: auf getippten Stufen kennt das Kind die Lösung
nicht (`Reveal == null`), der Server muss die **gemischten Buchstaben** mitschicken — ein neues Facet-Feld,
kein neuer Mechanismus. `StageMechanics.Normalize` trimmt und kleinschreibt, Groß-/Kleinschreibung der
Kacheln ist grading-irrelevant.

## Akzeptanzkriterien

1. Neuer `TestStage`-Wert (z. B. `LetterScramble`) im Vokabel-Typ, im Creator wählbar.
2. `StageFacets` liefert die gemischten Buchstaben, gemischt aus einem **je Sitzung eingefrorenen Seed**.
3. Nicht geeignete Items (mehrwortig, > ~12 Zeichen) werden **für sich** als `LetterBoxes` ausgespielt.
4. Neue Komponente rendert die Kacheln; das zusammengesetzte Wort geht wie gehabt als `GivenAnswer` —
   **kein** Änderungsbedarf am Grading.
5. Gleichwertige Tastatur-/Screenreader-Bedienung neben dem Ziehen.
6. Die Stufe zählt als „getippt" für `RequireTypedTest`.

## Schätzung

**Größe: M** — Server-Teil klein und musterkonform; Frontend mittel wegen **neuer Library** und **zweiter
Bedienart**, die dieselbe Logik nochmal abbilden muss.

- **`vertragsbruch: nein`** — `StageFacets` bekommt ein Feld *additiv*; kein bestehendes fällt weg.
- **Einzige Story mit neuer Abhängigkeit** (Drag & Drop). Darum zuletzt in der Reihe.
- **Nebenbefund aus dem Grillen, hier mit erledigt:** `LetterBoxes` hat heute **keine Obergrenze und keine
  Leerzeichen-Behandlung** — ein Leerzeichen wäre ein leer zu lassendes Kästchen mitten im Wort. Das
  Mehrwort-Problem ist im Projekt ungelöst, nur bisher nicht aufgefallen.
- **Testweg:** Server-Test auf Facet + Sitzungs-Seed (zweimal `/next` liefert dieselbe Anordnung, neue
  Sitzung eine andere); Frontend-E2E auf beide Bedienarten.

## Verlauf

- **2026-07-30** — geerntet aus dem Grill-Protokoll vom selben Tag, Stufe `geschaetzt` übernommen.
- **2026-08-07** — Autonomer Modus (Opt-in je Vorhaben, README → „Autonomer Modus") vom Nutzer im Dialog
  ausdrücklich freigegeben: ein Nachtlauf darf diese Story trotz `art: Wunsch` ohne weitere Rückfrage bauen
  (Rollengang/Reviewer bleiben Pflicht wie bei jeder Abnahme). Bleibt P5 — einzige Story mit neuer
  Abhängigkeit (Drag & Drop), zuletzt in der Reihe.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): gilt, leicht unterschätzt. Der Nebenbefund Leerzeichen ist durch B-66 erledigt (`StageMechanics.LetterBoxPattern`). Zusätzlich nachzuziehen: `StageOptions` (`VocabularyExerciseType.cs:93`), `StageMechanics.IsTyped`, die Frontend-Liste `VOCAB_FORMS` (`exerciseConfig.tsx:106`), drei Renderstellen (`SohnPractice`, `SohnTest`, `ExercisePreviewModal`). `StageFacets` ist ein Tupel an der Schnittstelle – ein neues Feld ändert die Signatur überall; den Misch-Seed aus `PracticeSession.Id`/`TestAttempt.Id` ableiten, dann bleibt `migration: nein`. **Offen für den Nachtlauf:** die Story wählt keine Drag-&-Drop-Bibliothek – ein Lauf baut die reine Klick-/Tastatur-Variante ohne neue Abhängigkeit oder hält an.
- **2026-10-26** — gebaut in Lauf D ([Protokoll](../pm-sitzung-2026-10-26-tag.md), NP-7) unter Freigabe 8. **Ohne Ziehen**, wie die Sichtung vorgibt: Bedienung per Klick und per Tastatur (ein Buchstabe wählt die erste freie passende Kachel, Backspace nimmt zurück, Enter prüft), keine neue Abhängigkeit. AK 5 („neben dem Ziehen“) gilt damit als zwei gleichwertige Bedienarten ohne Ziehen. Backend: `TestStage.LetterScramble = 7` (getippt), `IExerciseType.LetterTileSource`, `StageMechanics.IsScramblable` (2–12 Buchstaben/Ziffern) und `ScrambleLetters` (seed-stabiles Fisher-Yates, nie schon die Lösung). `CardFacets` mischt mit `(Sitzungs- bzw. Versuchs-Id × 397) ^ Index`, die Vorschau mit der Übungs-Id. `LetterTiles` kommt additiv an `PracticeCard`, `TestItem` und `PreviewItem`. Ungeeignete Antworten laufen als Buchstabenkästchen (AK 3). Keine Migration. Frontend: `LetterTiles` in Übung, Klausur und Vorschau, „Buchstaben ordnen“ in `VOCAB_FORMS`. **Gemessen auf dem Weg:** Die E2E-Spec fand, dass schnell getippte Buchstaben verloren gingen (von „kaufen“ kam „kauf“ an). Eine gelegte, fokussierte Kachel wurde `disabled`, und der Browser gab den Fokus an `body` ab. Jetzt ist sie `aria-disabled`. Rote Probe: mit `disabled` → **1 failed** („Gelegt: k a u f“). Ein zusätzlicher Ref gegen veralteten Zustand blieb als Mutante grün und ist wieder entfernt. Rote Proben Backend: Seed ignoriert → 1 failed (neue Sitzung), Rückfall für Mehrwort weg → 1 failed, Stufe nicht getippt → 4 failed. Vitest `LetterTiles`: Tastatur ignoriert gelegte Kacheln / kein Neustart beim Leeren / Region nur mit Wort → je 1 failed.
- **2026-10-26** — Reviews in Lauf D ([Protokoll](../pm-sitzung-2026-10-26-tag.md)). Zähler: **6, der Lauf endet hier.** Behoben (gezählt 1–5): Die Kacheln verrieten über die Großschreibung den ersten Buchstaben (jetzt klein, „schon die Lösung“ vergleicht klein); der Item-Index fehlte im Seed (alle gleich langen Wörter einer Sitzung hatten dieselbe Anordnung); beim Rückfall fehlte die Längen-Zusicherung; die Klausur zeigte „Stufe 7“; Enter auf „Neu anfangen“/„⌫ Zurück“ schickte das halbe Wort ab. Je mit Probe → 1 bzw. 3 failed. **Offen, nicht behoben (Fund 6 beendet den Lauf):** (a) **Nach jeder Karte geht der Tastaturfokus an `body`**: `key={card.itemIndex}` baut den Block neu, die fokussierte Kachel verschwindet. Vorschlag des Reviewers: `key` weglassen und bei einem Wechsel von `tiles` zurücksetzen, oder beim Mount fokussieren; E2E mit zwei Karten nur per Tastatur und Enter. (b) Testlücken: Klick auf eine gelegte Kachel, „Neu anfangen“ ohne Test, Klausur und Vorschau rendern Stufe 7 in keinem Test, die Spec prüft nicht, dass richtig gewertet wurde. (c) Strg/Cmd/Alt werden nicht ausgeschlossen (Strg+R legt eine Kachel). (d) Die Feldhilfe `defaultStage` nennt die neue Form nicht. (e) Der Plan-Assistent bietet Stufe 6 und 7 nicht an, uneinheitliche Bezeichnung „Ordnen“/„Buchstaben ordnen“. Zurückgestellt vor dem Ende: [B-283](B-283-buchstaben-ordnen-testluecken.md). Vorbestehend, neu als Story: [B-284](B-284-doppeltes-enter-ueberspringt-karte.md). Stand grün: Backend **948/948 `exit=0`**, Vitest **450/450**, Playwright voll **96 passed** (vor den Review-Behebungen), `buchstaben-ordnen.spec` danach **1 passed**. **Nicht abgenommen**, Stufe `in-arbeit`.
- **2026-10-27** — Nacharbeit in Lauf E ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)) mit neuem Zähler 0 (Entscheidung des Nutzers vom Morgen). **(a) Fokus:** `LetterTiles` setzt sich bei neuen Kacheln selbst zurück (Zustand im Render angepasst, kein `key` mehr in `SohnPractice`/`SohnTest`) und holt den Fokus auf die erste Kachel, wenn er vorher im Block stand oder auf `body` lag; stand er außerhalb (Maus auf „Prüfen“), bleibt er dort. Vitest, vier neue Fälle, Abgrenzung je Fall: Fokus im Block vs. außerhalb (Mutante „nie fokussieren“ → **1 failed**, Mutante „immer fokussieren“ → **1 failed**), Neustart aus den Kacheln bei stehendem Wert (Mutante Reset entfernt → **1 failed**). E2E `buchstaben-ordnen.spec.ts` jetzt mit vier Karten: zweite Tastatur-Karte ohne erneutes Fokussieren, Kästchen-Karte per Enter; gegen die alte Komponente **1 failed** („toBeFocused: Expected focused, Received inactive“), danach **1 passed**. **(c) Modifier:** Strg/Alt/Cmd werden ignoriert (Mutante Wächter entfernt → **1 failed**). **(d)** Feldhilfe `defaultStage` nennt „gemischte Buchstaben ordnen“. **(e)** Plan-Assistent bietet „6 · Auswahl“ und „7 · Ordnen“; die Kurznamen (Assistent, Klausur-`STAGE_LABEL`) bleiben eine Familie, die Langnamen stehen in `VOCAB_FORMS` – bewusst zwei Register, kein Widerspruch. **(b)** Testlücken in B-283 geschlossen (siehe dort), die Spec prüft jetzt je Antwort die Server-Wertung (`wasCorrect`). Vitest `LetterTiles` **10/10**, Backend-Filter **13/13 `exit=0`**.
- **2026-10-27** — Reviews in Lauf E ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)), nacheinander je Bereich. `pugling-reviewer` (nur Testdateien): zwei Kommentar-Funde, ohne Zählung. `frontend-reviewer`: **🟡 (gezählt, 1):** Der im Kommentar beschriebene Maus-Weg „Fokus bleibt auf ‚Prüfen‘“ entsteht in der App nicht – der Knopf wird mit dem Absenden gesperrt und gibt den Fokus ab. Gemessen mit der auf `orderStrategy: Serial` umgestellten Spec, die nach **jedem** der drei Übergänge (Kästchen→Kacheln, Enter→Kacheln, Klick auf „Prüfen“→Kacheln) `toBeFocused` auf der ersten Kachel zusichert: **1 passed** – der Fokus landet auf der Kachel, die Kommentare in Komponente, Test und Spec sind berichtigt. 🟢 ohne Zählung: Reset-Vertrag (`tiles` referenzstabil) im JSDoc benannt; die dritte Handkopie der Stufenliste im Assistenten liest jetzt `VOCAB_FORMS`; Fokus-Sprung im Vorschau-Dialog beim Stufenwechsel nur notiert (Altlast); Ansage der neuen Aufgabe nach dem Kartenwechsel als [B-286](B-286-kartenwechsel-sagt-die-aufgabe-nicht-an.md) abgelegt (außerhalb des Diffs). **Rollengang:** Sohn über die E2E-Spec (vier Karten, drei Bedienarten, Server-Wertung je Antwort), Vater (Assistent, Feldhilfe) über `assistent.spec`/`feldhilfe.spec` **6 passed** als Regressionszeugen; kein Browser verbunden. Stand: Backend **949/949 `exit=0`**, Vitest **454/454**, Playwright voll **96 passed** (vor der Spec-Umstellung), danach `buchstaben-ordnen.spec` **1 passed**. `abgenommen`; Commit siehe Protokoll.
