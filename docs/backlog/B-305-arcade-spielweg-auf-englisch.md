---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [i18n Teil b2, Spielweg der Arcade auf Englisch, Üben und Test auf Englisch]
status: abgenommen
prio: P3
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-295 (geteilt, Lauf F)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: "Mensch: die neuen englischen Texte (play.*, practice.*, test.*, problem.*) in src/i18n/en.ts gegenlesen (AK 6)"
---

# B-305 · Der Spielweg der Sohn-Arcade auf Englisch: Üben, Test und die Übungskomponenten

Zweiter Teil der geteilten [B-295](B-295-arcade-korpus-auf-englisch.md): `SohnPractice`, `SohnTest` und die von ihnen
eingebundenen geteilten Komponenten (`LetterBoxes`, `LetterTiles`, `ListRule`, `RevealAlternatives`, `AudioButton`,
`ClozePrompt`, `Passage`, `BirkenbihlDecoding`, `Mascot`, `CardImage` …), `GERMAN_PROBLEM_TEXT` in `lib/api.ts`,
soweit die Arcade ihn zeigt, und die Spielweg-Anteile von `lib/labels.ts`.

## User Story

Als **Kind, das Englisch lernt**, möchte ich auch beim Üben und im Test Englisch lesen – dort verbringe ich die
meiste Zeit.

## Ist-Stand am Code · Entscheidungen

Wie B-85 (Entscheidungen 1–6) und [B-304](B-304-arcade-rahmen-auf-englisch.md) (geteilte Helfer bleiben für das
Vater-Web deutsch). Zusätzlich: viele dieser Komponenten zeigt auch das Vater-Web (Übungs-Vorschau/Testmodus) – sie
bekommen ihre Texte darum als Schlüssel nur dort, wo die Arcade sie rendert, oder als Props vom Aufrufer; welcher
der beiden Wege je Komponente, entscheidet der Bau (Handwerk, keine Produktfrage). Übungstyp-Anzeigenamen bleiben
deutsch (B-85, Entscheidung 4) – eine bekannte, in der UI benannte Lücke.

## Akzeptanzkriterien

1. Jeder UI-Chrome-Text von Üben, Test und den eingebundenen Komponenten liegt auf Deutsch und Englisch vor; der
   Paritäts-Test bleibt grün.
2. Nutzerdaten (Vokabeln, Aufgabentexte, Übungstitel) bleiben unübersetzt.
3. Die Vorschau im Vater-Web bleibt deutsch, auch wenn `pugling.lang = en` gespeichert ist.
4. Übungstyp-Anzeigenamen: bekannte Lücke, in der UI benannt (B-85 AK 7).
5. Alle Arcade-Specs bleiben grün (Deutsch gepinnt); `sprache.spec.ts` spielt zusätzlich eine Übungsrunde auf Englisch.
6. Die englischen Texte liest am Ende ein Mensch gegen.

## Schätzung

**M** (obere Hälfte): zwei große Bildschirme (~730 Zeilen) plus rund zehn geteilte Komponenten mit doppelter
Verwendung. Angriffsplan: erst die Bildschirme, dann je Komponente Schlüssel oder Prop; nach jeder Datei Vitest.

## Verlauf

- **2026-10-27** — angelegt beim Teilen von [B-295](B-295-arcade-korpus-auf-englisch.md) in Lauf F
  ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)); Entscheidungen aus B-85/B-295 übernommen.
- **2026-10-27** — gebaut in Lauf G, Teil 2 ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)). `SohnPractice`,
  `SohnTest` (samt `SelfAssessAnswer`, `TestResult`, Stufennamen) auf Schlüssel. Für die acht geteilten Bausteine fiel
  „Schlüssel oder Prop“ (Handwerk, Ist-Stand der Story) auf einen Weg für alle: `lib/chromeText.tsx` mit `ArcadeScope`
  an der `SohnApp`-Wurzel und `useChromeT()` – innerhalb die Arcade-Sprache, außerhalb (Vater-Vorschau) fest Deutsch.
  `GERMAN_PROBLEM_TEXT`, soweit die Arcade ihn zeigt: `useErrorMessage()` plus optionaler Formatierer an `useAction`;
  dabei [B-314](B-314-fehlermeldungen-der-arcade-bleiben-deutsch.md) als Doppel zusammengelegt. **AK 4:** die Arcade
  zeigt nirgends Übungstyp-Namen (Suche über `src/sohn/`) – in diesem Umfang keine Fläche, also auch kein Hinweis.
  Deutsch ändert sich sichtbar nur in „1 Karte“ statt „1 Karten“ am Rundenende.
- **2026-10-27** — Belege. `arcadeSpielweg.test.tsx` (**10 Fälle**): rote Probe gegen den Ausgangsstand nicht messbar
  (neue Module), stattdessen **18 Vitest-Mutanten** (Bausteine, Fehlermeldungen, `useAction`, Klausur), am Endstand je mindestens
  **1 failed**; Kontrolle **10 passed**. Zwei Lücken dabei
  gefunden und geschlossen: „Sprachweiche weg“ blieb zuerst **10/10 grün** (Fall mit einem nur von `errorMessage`
  gekannten Code ergänzt → **1 failed**); in `sprache.spec.ts` lief ein Zweig „Next →“ nie (Mutante blieb **1 passed**)
  – Zweig gestrichen, Weg fest auf Umdrehen → „Knew it!“, Mutante darauf → **1 failed**.
- **2026-10-27** — Review (`frontend-reviewer`): kein Blocker, drei 🟡 (zählen): die deutschen `problem.*`-Texte
  wurden nie gelesen (jetzt läuft der deutsche Weg für Arcade-Codes auch über die Ressource); die Hälfte der Bausteine
  ohne Sprach-Fall (jetzt alle acht, je ein Text innen englisch/außen deutsch, sieben Mutanten je **1 failed**, „Alternative
  durch `t`“ **2 failed**); AK 2 auf dem Spielweg nur als Kommentar (Schlüssel als Nutzerdatum im Baustein-Fall, Spec-
  Kommentar ehrlich gefasst). 🟢 ohne Zählung: zwei englische Formulierungen, ein falscher Begründungs-Kommentar.
  **Zähler: 3.**
- **2026-10-27** — Rollengang: der Sohn-Gang ist der neue Fall in `sprache.spec.ts` im echten Browser gegen frisch
  gestartete Server (Vater legt eine Position an, der Sohn spielt auf Englisch eine Karte: „Card 1 / N“, „Task“,
  „Flip 🔄“, „Knew it!“, danach „Card 2“ oder „ROUND DONE!“). Der Vater ist mit AK 3 belegt (Bausteine außerhalb des
  Scopes deutsch, Vitest); Creator Regressionszeuge. Chrome-Extension nicht genutzt. **`abgenommen`**, `wartet_auf`
  das menschliche Gegenlesen (AK 6).
