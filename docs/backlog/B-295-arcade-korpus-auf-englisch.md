---
tags: [typ/story, status/geschaetzt, bereich/frontend, rolle/student]
aliases: [i18n Teil b, Rest der Sohn-Arcade auf Englisch]
status: geschaetzt
prio: P3
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-85 (geteilt, NP-11 Schritt 0)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-295 · Der restliche Chrome-Korpus der Sohn-Arcade auf Englisch

Teil b der geteilten [B-85](B-85-i18n-infrastruktur-sohn-arcade-englisch.md). Die Infrastruktur, der Umschalter und der
Startbildschirm kommen aus [B-294](B-294-i18n-infrastruktur-und-startbildschirm.md); hier folgt der Rest des
Korpus: `SohnLogin`, `SohnPractice`, `SohnTest`, `SohnProgress`, `SohnShop`, `SohnSkins`, `SohnKonto`,
`SohnInterests` (ohne die Intro-Karte), die Auszeichnungs-Panels in `GamificationPanels`, die von der Arcade
eingebundenen geteilten Komponenten (`LetterBoxes`, `LetterTiles`, `ListRule`, `RevealAlternatives`, `AudioButton`,
`Mascot`, `CardImage` …), `GERMAN_PROBLEM_TEXT` in `lib/api.ts` und die Arcade-Anteile von `lib/labels.ts`.

## User Story

Als **Kind, das Englisch lernt**, möchte ich, dass die ganze Arcade auf Englisch spricht, nicht nur der Start –
sonst ist die Immersion nach dem ersten Klick vorbei.

## Ist-Stand am Code · Entscheidungen

Wie B-85 (Entscheidungen 1–6 gelten; Übungstyp-Anzeigenamen bleiben deutsch bis zur Frontend-Tabelle mit `type`
als Schlüssel aus der Nachprüfung vom 2026-10-23). Die Infrastruktur steht seit B-294: Namespace `sohn` in
`src/i18n/de.ts`/`en.ts`, Parität per `i18n.test.ts`, Deutsch in den Specs gepinnt.

## Akzeptanzkriterien

1. Jeder UI-Chrome-Text der oben genannten Dateien liegt auf Deutsch und Englisch vor; der Paritäts-Test bleibt grün.
2. Nutzerdaten (Missions-/Auszeichnungstitel, Shop-Artikel, Plan-/Übungstitel, Vokabeln) bleiben unübersetzt.
3. Alle Arcade-Specs bleiben grün (Deutsch gepinnt); `sprache.spec.ts` prüft zusätzlich einen zweiten Bildschirm
   (z. B. Shop) auf Englisch.
4. Übungstyp-Anzeigenamen: bekannte Lücke, in der UI benannt (B-85 AK 7).
5. Die englischen Texte liest am Ende ein Mensch gegen.

## Schätzung

**M** (obere Hälfte): ~12 Dateien mit vielen Literalen, je Zeile echte Übersetzungsqualität; kein Backend, keine
Migration. Angriffsplan: Datei für Datei extrahieren (Schlüssel nach Bildschirm gruppiert), nach jeder Datei Vitest
und die betroffene Spec; Testweg: Paritäts-Test, Arcade-Specs, erweiterte `sprache.spec.ts`.

## Verlauf

- **2026-10-27** — angelegt beim Teilen von B-85 in Lauf E, Teil 8 ([Protokoll](../pm-sitzung-2026-10-27-nacht.md),
  NP-11 Schritt 0); Entscheidungen und Schätzung aus B-85 übernommen, Umfang auf den Rest des Korpus gesetzt.
- **2026-10-27** — Hinweise aus dem Bau von B-294: `SohnLogin` liegt **außerhalb** der `SohnShell`, braucht also den
  `<html lang>`-Effekt und den Chip selbst oder einen gemeinsamen Rahmen; der E2E-Helfer `sohnLogin` klickt „▶ LOS“
  (Deutsch gepinnt, aber der Text wandert); `SavedNotice` und `BadgesGallery` in `GamificationPanels` tragen noch
  deutsche Literale. Eine Tabelle deutscher Literale neben den Ressourcen (wie `PERIOD_LABEL` in B-294) sieht der
  Paritäts-Test nicht – darum der Vorschlag (M) im Protokoll (Literal-Wächter über `src/sohn/**`). Schlüssel sind seit
  B-294 typisiert (`src/i18n/i18next.d.ts`): ein neuer Schlüssel gehört zuerst nach `de.ts`.
- **2026-10-27** — Commit `1ee9bf0` (Lauf E, Teil 8; nicht gepusht).
