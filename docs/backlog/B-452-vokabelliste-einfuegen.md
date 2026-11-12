---
tags: [typ/story, status/abgenommen, bereich/katalog, lerntechnik/vokabeln, rolle/creator, rolle/supervisor]
aliases: [Vokabelliste einfügen, Sammeleingabe]
status: abgenommen
nachgeschaut: 2026-11-12
prio: P1
art: Wunsch
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-10-fertigstellung.md
grund: ""
ersetzt_durch: []
---

# B-452 · Eine Vokabelliste einfügen statt 150 Zeilen einzeln tippen

Der Vater bekommt die Wörter als Liste (Buch, Lehrer, Tabelle). Die Vokabelseite kennt nur „+ Zeile" je Paar
(`frontend/src/vater/VaterVocab.tsx`, gespeichert wird schon als Batch über `api.createVocabularyBatch`). Für
vier Wochen Stoff sind das 150 Zeilen von Hand – und danach dieselben 150 noch einmal in die Übung wählen.
Gebraucht wird: Liste einfügen (ein Paar je Zeile, Trenner Tab, Semikolon oder „ – "), Vorschau, speichern,
und aus dem Ergebnis in einem Schritt eine Vokabelübung in einer Unit anlegen.

## User Story

Als Vater möchte ich die Vokabelliste aus dem Buch in einem Stück einfügen und daraus eine Übung machen, damit
vier Wochen Stoff eine Sache von Minuten sind und nicht von 150 Zeilen.

## Ist-Stand am Code

- Store-Seite: zeilenweise Eingabe, gespeichert als Batch (`VaterVocab.tsx`, `submit`).
- Übung anlegen: `VocabRefPicker` wählt Store-Wörter per Häkchen oder legt **ein** Wort an („+ anlegen & wählen“,
  `VaterExerciseCreate.tsx`, `quickAdd`).
- Server: `POST creator/vocabulary/batch` liefert je Paar `id` und `key` und kennt `reuseExisting`
  (`VocabularyStoreController.cs:522-551`) – die Fähigkeit ist da, nur keine Oberfläche nutzt sie für eine Liste.

## Die echte Lücke

Reine Oberfläche: kein Feld nimmt eine Liste an. Der Vertrag ändert sich nicht.

## Entscheidungen

1. **Die Liste gehört in das Übungs-Formular, nicht auf die Store-Seite.** Dort ist der Schritt „anlegen **und**
   wählen“ einer; auf der Store-Seite müsste der Vater die 150 Wörter danach noch einmal anhaken. Kosten: wer
   nur den Store füllen will, tippt dort weiter zeilenweise.
2. **Trenner: Tabulator, Semikolon, „ – “ / „ - “ mit Leerzeichen, „ = “. Kein Komma.** „Abfall, Müll“ ist eine
   Übersetzung, französische Wendungen tragen Kommas. Kosten: eine reine Komma-Liste (CSV ohne Semikolon) wird
   als „ohne Trenner“ gemeldet statt gelesen.
3. **Weitere Spalten sind „Auch richtig“; Zeilen ohne Trenner werden übersprungen und genannt**, nicht still verworfen.
4. **`reuseExisting: true`**, in Scheiben zu 50 Paaren: zweimal einfügen wählt dieselben Einträge. Kosten: bei
   150 Paaren drei Anfragen nacheinander.
5. **Kein Datei-Upload, kein Foto** (B-251/B-375 bleiben eigene Wünsche).

## Akzeptanzkriterien

1. Im Formular „Neue Übung“ (Typ Vokabel) öffnet „Liste einfügen“ ein Textfeld; die erkannten Paare und die
   übersprungenen Zeilen stehen vor dem Anlegen da.
2. „N anlegen & wählen“ legt die Paare im gewählten Sprachpaar an (oder verwendet vorhandene) und wählt sie.
3. Ein wiederholtes Paar wird einmal gewählt; ein Komma in der Übersetzung und ein Bindestrich im Wort bleiben.
4. Ein Paar, das der Server ablehnt, wird mit Wort genannt; die übrigen sind gewählt.

## Schätzung

**Größe: S**, nur Frontend. Testweg: `vocabList.test.ts` (Parser), `e2e/vokabelliste-einfuegen.spec.ts` (Weg).

## Verlauf

- **2026-11-10** — angelegt aus Rollengang 0 des Fertigstellungs-Laufs (Befund Z2).
- **2026-11-10** — entschieden (PM unter Vollmacht des Nutzers) und gebaut: `lib/vocabList.ts`, `VocabRefPicker`, Feldhilfe `vocabListPaste`.
- **2026-11-10** — `frontend-reviewer`: keine Blocker; eingearbeitet: Vorschau zeigt die ersten zwei Paare (eine am falschen Trenner gelesene Liste sah als Zahl richtig aus), der Knopf nennt das Sprachpaar, ein Scheibenfehler behält und wählt das bis dahin Angelegte, der Fokus kehrt zum Umschalter zurück, Fortschritt am Knopf. Belege: `vocabList.test.ts` (Trenner, Rangfolge, Komma, Bindestrich, Dubletten/Homonym), `e2e/vokabelliste-einfuegen.spec.ts`. Nicht belegt: AK 4 (vom Server abgelehntes Paar) – der Parser liefert nie ein leeres Wort, der Fall ist über die Oberfläche nicht erreichbar.
- **2026-11-10** — abgenommen. Vitest 741/741, E2E 134 passed (`vokabelliste-einfuegen` und `ziel-vier-wochen` fahren den Weg – der Rollengang des Vaters); `frontend-reviewer` gelaufen. Commit `595b429`.
- **2026-11-12** — Nachschau 2026-11-12 (Sprint 31): `vocabList.ts` gegen Entscheidung 2 (Trenner, kein Komma) und AK 3 (Dubletten, Komma, Bindestrich); Rückabbildung `slice[r.index]` und `reuseExisting` in `addList` geprüft – ohne Befund.
