---
tags: [typ/protokoll, bereich/pm]
---

# PM-Sitzung: Nachtlauf G – Notenstufe je Fach

**Datum:** 2026-11-23  ·  **Moderation:** PM (Nachtlauf, unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die Notenstufe aus B-682 bauen: Notenschlüssel, Berechnung, Lese-Endpunkte, Anzeige (B-740 bis B-743).

Auftrag: „Starte 3 Nachtläufe“ – dieser ist Lauf G, gelesen als der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den
Freigaben 1–11, in einem eigenen Worktree (parallel laufen H und I). Ausgangsstand `a782b4b2`, sauber. Paket vom
Auftraggeber fest vorgegeben: B-740, B-741, B-742, B-743 in dieser Reihenfolge. Story-Ids dieses Laufs nur B-900 bis
B-919, Sprints 127 bis 129. API auf :5211, Vite auf :5184; :5200 (Heimnetz, echte `pugling.db`) bleibt unberührt.

## Runde 1 — Auswahl

Kein neuer Befund unter `docs/anmerkungen/` (jüngster vom 2026-08-02). Alle vier Stories stehen auf `geschaetzt`,
`art: Wunsch` – baubar nach Freigabe 8. Ihre Entscheidungen tragen die Marke „stellvertretend im Vorbereitungslauf
2026-11-20 (Sonnet) gefällt, unbestätigt“; nach dem [Nachtrag des Vorbereitungslaufs](pm-sitzung-2026-11-19-vorbereitungslauf.md)
(„Auf Anweisung ‚entscheide‘ …“, B-682) wurden sie auf Anweisung des Nutzers so entschieden und `wartet_auf` blieb leer.
Ich baue sie darum, halte die Marke aber im Abschlussbericht fest: die Bestätigung durch den Nutzer steht aus.

| Sprint | Stories | Thema |
| --- | --- | --- |
| 127 | B-740, B-741, B-742, B-743 | Notenstufe je Kind und Vokabel-Fach, vom Schlüssel bis zur Anzeige |

## Sprint 127 — Ziel & Umfang

**Sprint-Ziel:** Der Sohn sieht auf der Fachkarte eines Vokabel-Fachs seinen Notenstand (Stufe 1–3 mit Namen, darunter nur
den Zielsatz „noch N Wörter sicher“), der Vater dieselbe Zahl samt Bestwert im Kind-Hub – berechnet nur aus serverseitig
gewerteten Wörtern des aktiven Plans, höchstens eine Stufe Abstieg je Woche, ohne Münzbuchung.
**Umfang:** B-740 (S), B-741 (M, Migration), B-742 (S), B-743 (M, Frontend).
**Entwickler-Brief:** Quelle der Wahrheit ist `GradeKey` (die eine Umrechnung Anteil → Note, Tabelle `Grading:Thresholds`)
und `ChildSubjectLevel` (je Kind und Fach, gespeichert). Das Maß kommt aus `ChildLearnProgressService` (neue interne
Methode über die Übungen aktiver Pläne, keine Kopie der Rechnung). Bewertet wird einmal je Lerntag, träge: an der
Kind-Anmeldung und am Lesezugriff. Senkung höchstens eine Stufe je sieben Tage, Bestwert sinkt nie, kein
`ChildPointsEntry`. „Wörter bis zur nächsten Stufe“ ist die Umkehr aus `GradeKey` (eine Stelle). Migration
`NotenstufeChildSubjectLevel`. Testweg: `PercentToGradeTests`, `ChildSubjectLevelTests`, `NotenstufeMigrationTests`,
`SubjectLevelEndpointTests`, Vitest `notenstufe.test.ts`, E2E `notenstufe.spec.ts`.

## Iteration 127 — umgesetzt

*(wird fortgeschrieben)*
