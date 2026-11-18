---
tags: [typ/story, status/abgenommen, bereich/klassenarbeit, rolle/supervisor]
aliases: [Abschlussbonus-Feld, Nacharbeit-Nachschliff]
status: abgenommen
prio: P3
art: Aufräumen
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "PM-Sitzung 2026-11-18, Review Sprint 86 (frontend-reviewer, pugling-reviewer)"
grund: ""
ersetzt_durch: []
---

# B-652 · Das Abschlussbonus-Feld und die beendete Nacharbeit glätten

Vier kleine Reste aus dem Review von Sprint 86 ([B-636](B-636-pflicht-bis-erledigt-mit-abschlussbonus.md),
[B-637](B-637-nacharbeit-vater-sicht-je-aufgabe.md)). Bewusst nicht im Sprint: das Sprint-Ziel hält ohne sie, und
keiner ändert eine Buchung.

## Beobachtung

1. Wechselt der Vater von „täglich, bis erledigt“ auf einen anderen Rhythmus, verschwindet das Bonusfeld, der alte
   Wert wird aber weiter gespeichert (`frontend/src/vater/PlanPositions.tsx`, `withRemediation`). Er zahlt nur bei
   „bis erledigt“ – harmlos, aber unsichtbar gespeichert.
2. Das Emoji in „Abschlussbonus 🪙“ ist Teil des zugänglichen Namens; ein Screenreader liest „Münze“ mit.
3. Ein Bonus über 1000 scheitert erst am Server mit englischem `validation_error`-Text; das Formular hat keine
   eigene Grenze am Speichern-Knopf.
4. Nacharbeiten, die vor B-636 fertig wurden und `Daily` bleiben, zeigen `completedOn: null`, obwohl jede Aufgabe ein
   `doneAt` hat (keine Rückfüllung in der Migration). Nur Anzeige; das Umstellen auf „bis erledigt“ stempelt richtig.

## User Story

Als *Vater* möchte ich, dass das Positionsformular speichert, was ich sehe, mir Fehler auf Deutsch sagt und mit einem
Screenreader sauber benannt ist.

## Ist-Stand am Code

Gelesen am 2026-11-18 auf `6b961cb5`. (1) `settingsToDto` schickte `completionBonus` bei jedem Rhythmus
(`frontend/src/vater/PlanPositions.tsx:363` vor dem Fix), das Feld ist nur bei „bis erledigt“ sichtbar (`:480`). (2) Das
Label war `Abschlussbonus 🪙` (`:482`), der E2E griff es als `"Abschlussbonus 🪙"`. (3) Beide Speicherwege prüften vorab
nur `timeSlotProblem` (`:704`, `:927`); der Server begrenzt auf 0–1000 (`PlanPositionsController.MaxCompletionBonus`) und
antwortet englisch. (4) `completedOn` fehlt bei Nacharbeiten, die vor B-636 als Tagesziel fertig wurden – nur Anzeige.

## Offene Punkte

1. ~~Verborgenen Bonus speichern?~~ → Entscheidung 1.
2. ~~Emoji im zugänglichen Namen?~~ → Entscheidung 2.
3. ~~Grenze im Formular?~~ → Entscheidung 3.
4. ~~Rückfüllung von `completedOn`?~~ → Entscheidung 4.

## Entscheidungen

Autonom gegrillt im Nachtlauf 2026-11-18 (`art: Aufräumen`).

1. **Gespeichert wird, was zu sehen ist:** außerhalb von „bis erledigt“ schickt die Senke `settingsToDto` 0. *Begründung:*
   ein unsichtbar gespeicherter Wert käme beim Zurückwechseln wieder. *Kosten:* ein alter Bonus geht beim Wechsel verloren
   – gewollt.
2. **Das 🪙 ist `aria-hidden`**, der Feldname heißt „Abschlussbonus“. *Begründung:* der Name sagt die Größe, die Münze ist
   Schmuck. *Kosten:* der E2E-Selektor zieht nach.
3. **Eine Prüfstelle für beide Speicherwege** (`positionSettingsProblem`: Bonus ganzzahlig 0–1000 bei „bis erledigt“, dann
   Zeitfenster) mit deutscher Meldung. *Begründung:* Regel an der Senke statt je Handler. *Kosten:* im
   Hinzufügen-Formular hält die Browser-Prüfung (`min`/`max`) 1001 schon vorher auf – dieselbe Asymmetrie wie beim
   Zeitfenster-Faktor, bewusst so gelassen (Review 🟢).
4. **Keine Rückfüllung.** *Begründung:* reine Anzeige an Altbestand; das Umstellen auf „bis erledigt“ stempelt richtig
   (B-636), und der Report zeigt je Aufgabe „erledigt am“. *Kosten:* „geschafft am“ fehlt an alten Tagesziel-Nacharbeiten.

## Akzeptanzkriterien

1. `settingsToDto` mit Tagesziel und Bonus 40 ⇒ 0. *Rot, wenn der rohe Wert reist.*
2. Bonus 1001, −1 und 2,5 bei „bis erledigt“ ⇒ Meldung; 1000 ⇒ keine; Tagesziel mit 5000 ⇒ keine. *Rot ohne Ober-,
   Unter-, Ganzzahl-Grenze bzw. ohne Rhythmus-Bedingung.*
3. `positionSettingsProblem` meldet weiter ein halbes Zeitfenster. *Rot, wenn es nicht weitergibt.*
4. Der E2E findet das Feld unter dem genauen Namen „Abschlussbonus“. *Rot, wenn das 🪙 wieder im Namen steht.*

## Schätzung

**Größe: S** — drei kleine Eingriffe an einem Formular plus E2E-Selektor (wie B-01). `migration: nein`,
`vertragsbruch: nein`, `wo: frontend`. Risiko: ein zweiter Speicherweg an der Senke vorbei – der Assistent schickt keinen
Bonus (`wizardFinish.ts`), geprüft im Review. Testweg: `PlanPositions.test.ts`, E2E `nacharbeit-bis-erledigt.spec.ts`
(Rollengang Vater), `frontend-reviewer`.

## Verlauf

- **2026-11-18** — angelegt aus dem Review von Sprint 86.
- **2026-11-18** — Nachtlauf: ausformuliert, autonom gegrillt (Entscheidungen 1–4), geschätzt S/frontend; in Arbeit.
- **2026-11-18** — Review `frontend-reviewer`: 🟡 Kommazahl erreicht beim Bearbeiten den Server (englische Meldung) →
  Ganzzahl-Prüfung plus Fall `2.5` (zählt, Zähler 1); 🟢 Browser-Prüfung im Hinzufügen-Formular → bewusst so
  (Entscheidung 3, ohne Zählung); 🟢 zwei verrutschte Kommentare → behoben (ohne Zählung).
- **2026-11-18** — AK 1 → `frontend/src/vater/PlanPositions.test.ts:511`, F2 (`PlanPositions.tsx:377`) → rot · AK 2 → `:517`, F3 (Ganzzahl), F4 (Obergrenze, beide `:241`), F6 (Rhythmus-Bedingung, `:240`) → rot · AK 3 → `:528`, F5 (`:243`) → rot · AK 4 → `frontend/e2e/nacharbeit-bis-erledigt.spec.ts:63`, E1 (`aria-hidden` entfernt, `:496`) → rot. Gemessen an `6b961cb5 + Arbeitsbaum geändert`, 2026-11-18 22:37 (Skriptkopie mit Schreib-Wiederholung, siehe Retro).
- **2026-11-18** — Erster E2E-Lauf rot: `getByLabel("Abschlussbonus", exact)` liest den Label-Text samt 🪙; der Selektor fragt jetzt den zugänglichen Namen (`getByRole("spinbutton")`) – das ist, was AK 4 meint.
- **2026-11-18** — Verifikation nach der letzten Bearbeitung: `dotnet test Pugling.sln -c Release` **1882/1882**, Vitest **1249/1249**, `tsc -b` sauber, `dotnet format --verify-no-changes` ohne Fund außer Zeilenende-Artefakten an nicht berührten Zeilen; E2E `nacharbeit-bis-erledigt`, `nacharbeit`, `full-flow` **3 passed** (Ausweichports 5211/5181, Server von Playwright nach der letzten Bearbeitung gestartet).
- **2026-11-18** — Rollengang Vater: `nacharbeit-bis-erledigt.spec.ts` stellt „bis erledigt“ mit Bonus 30 ein – passed.
- **2026-11-18** — abgenommen: `f5b04d26` (Sprint 93, Nachtlauf).
