---
tags: [typ/story, status/geschaetzt, bereich/frontend, rolle/student]
aliases: [i18n Teil b2, Spielweg der Arcade auf Englisch, Üben und Test auf Englisch]
status: geschaetzt
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
wartet_auf: ""
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
