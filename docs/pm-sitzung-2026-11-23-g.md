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

**Backend (API-First):** `GradeKey` (die eine Umrechnung Anteil → ganze Note, `decimal`, Umkehr `MinPartForGrade`),
`GradingOptions` mit Tabelle `Grading:Thresholds` = 92/81/67/50/30 (beim Start geprüft), Entität `ChildSubjectLevel` und
Migration `20261123220126_NotenstufeChildSubjectLevel` (nur neue Tabelle), Maß
`ChildLearnProgressService.ActiveSubjectMeasuresAsync` (Übungen aktiver Pläne, gruppiert nach dem Fach des Plans),
`SubjectLevelService` (Bewertung einmal je Lerntag, an Kind-Anmeldung und Lesezugriff; Anstieg sofort, Senkung eine Stufe
je sieben Tage, Bestwert sinkt nie, keine Buchung), Endpunkte `student/me/subject-levels` und
`supervisor/children/{childId}/subject-levels`, DTO `SubjectLevelResponse`, Client-Einzeiler. Kein Vertragsbruch.

**Frontend:** Textregel `sohn/notenstufe.ts` (Stand-Text nur Stufe 1–3, darunter nur Zielsatz), Stufenzeile auf der
Fachkarte (eigener Abruf, Fehler still), Abschnitt „Notenstufe“ im Reiter „Schule“ des Kind-Hubs, i18n de/en.

**Verifikation:** Suite 2211/2211 (Release; Ausgangsstand 2175). Neue Klassen `PercentToGradeTests`,
`ChildSubjectLevelTests`, `NotenstufeMigrationTests`, `SubjectLevelEndpointTests`. Vitest 128 Dateien / 1396 Tests,
`npm run build` grün. E2E auf :5211/:5184 (Wegwerf-DB): `notenstufe` neu, Zeugen `belohnung-ueber-faecher`,
`bestaetigung-ueberlebt`, `pfad` – 6 passed. Mutanten-Probe nach der letzten Änderung: 20 von 21 rot am benannten Test,
E1 (Vater-Zahl, E2E) im Schlusslauf „unklar“, im Lauf davor (00:38, Datei seither unverändert) rot. Tabelle im Verlauf von
B-741. Ein rein zeilenweise äquivalenter Fall (M7/M7b, zwei redundante Wächter für „Fach ohne Wortschatz“) ist dort benannt.

**Review:** `pugling-reviewer` und `frontend-reviewer` (Agenten, nur gelesen). Keine 🔴. Fehlerzähler Sprint 127: **3**
(Freigabe 10/11): (1) eigener Fund der Mutanten-Probe M12 – kein Test stieg um genau eine Stufe; (2) roter Wächter
`Unpaginierte_Array_GETs_Sind_Gepinnt` (31 → 33, bewusste Zeile); (3) 🟡 Frontend-Review: Stufe nach dem Fach der Reihe,
Karte nach dem Fach des Plans – Maß jetzt nach dem Plan-Fach. Ohne Zählung: 🟡 Backend-Doku-Satz (nur Doku), 🟢 doppelte
Rollup-Rechnung je Lesen, Startprüfung der Tabelle, Kommentar am Verlierer-Pfad, AK-2-Fälle als Absichtserklärung,
englische Stufennamen; ein falscher eigener Kommentar (`double` und 23/25, nachgerechnet: kein Fall).

## Runde 127 — Abnahme

- **Sohn — abgenommen (maschinell), mit benanntem Rest:** „Auf meiner Englisch-Karte steht, wie viele Wörter mir bis zur
  nächsten Stufe fehlen, und keine schlechte Note.“ Gegangen über `e2e/notenstufe.spec.ts` (frisches Kind, Stufe 6, Mathe
  ohne Zeile). Nicht im Browser gesehen: Stufe 1–3 (braucht Box 5, also Wochen; die Texte trägt der Vitest). Offen für den
  Menschen: ob der Zielsatz am Handy freundlich wirkt. B-743 bleibt `in-arbeit` wegen der AK-2-Frage (unten).
- **Vater — abgenommen (maschinell):** „Im Kind-Hub unter Schule sehe ich je Vokabel-Fach die Zahl, den Bestwert und die
  Wörter bis zur nächsten Stufe – nur für dieses Kind.“ Über dieselbe E2E; Besitz über `OwnershipMatrixTests` und
  `SubjectLevelEndpointTests`.
- **Creator — Regressionszeuge:** kein Pfad des Creators im Diff; Suite grün, Katalog-Anlage in allen neuen Tests über die
  Creator-Endpunkte.
- **Offen für einen Menschen:** Migration `NotenstufeChildSubjectLevel` beim nächsten Heimnetz-Start (nur neue Tabelle;
  Startskript sichert vorher). Danach bewertet die erste Kind-Anmeldung des Tages die Stufen der echten Kinder.

## Retrospektive

**Nachschau:** B-863 (Sprint 125, Nachtlauf E, im Basis-Stand ohne `nachgeschaut`). Prüfpunkt: die sieben in der Story
genannten Testdateien nach deutschen Funktionswörtern in `//`/`///` gegrept – 0 Treffer; Umlaut-Treffer im übrigen
Testprojekt nur in zitierten Beispielwörtern. Kein Fund, `nachgeschaut: 2026-11-24`. (B-880 trug sein `nachgeschaut`
schon.) Index nach dem Lauf: Nachgeschaut 559 von 593 abgenommenen.

**Was die eigenen Tore durchgelassen haben:** Die Zuordnung „Stufe ↔ Fachkarte“ lief über zwei verschiedene Fach-Begriffe
(Reihen-Fach im Maß, Plan-Fach in der Tagessicht, B-614). Kein Test und keine Mutante sah das, weil alle Test-Pläne ohne
`SubjectId` angelegt waren und damit beide Begriffe zusammenfielen – dieselbe Familie wie im Nachtlauf-Profil („eine
Bedingung, die zwei Situationen zusammenzieht“). Gefunden hat es der `frontend-reviewer` durch Lesen der Quelle des
Kartennamens.

**Vorschlag (nur vorgeschlagen, Freigabe 3) (DB):** Ein Leitsatz für Testdaten in `Pugling.Api.Tests/CLAUDE.md`: „Wer eine
Größe nach Fach gruppiert, legt im Test Plan-Fach und Reihen-Fach **verschieden** an.“ Besser als Prosa wäre ein Helfer
`TestApi.SeedPlan(…, subjectId:)` ohne Vorgabewert, damit kein neuer Test das Plan-Fach still weglässt; Kosten: rund 60
Aufrufer von Direkt-Seeds. Entscheidung beim Nutzer.

**Bewusst ungedeckt:** das Rennen zweier Erst-Anlagen am eindeutigen Index (nicht deterministisch herstellbar); ein
Fachname, den zwei Fächer teilen (Karte und Stufe teilen ihn ohnehin).

## Offene Punkte für den Nutzer

1. **B-743 AK 2 (angehalten, Freigabe 8):** Darf eine auf 2 oder 3 *gesunkene* Stufe ihren Namen tragen? Gebaut ist (a):
   ja, „Zweier-Stand“ wie bei jedem Stand 2; nie „abgestiegen“, unter 3 nie ein Stufenwort. Alternative (b): Stufenwort
   weglassen, solange `level > bestLevel`. B-743 trägt `wartet_auf`.
2. **B-742 Entscheidung 3 gegen B-741-Risiko „keine Senkung ohne Lernen“:** Auch der Lesezugriff des Vaters bewertet. Ein
   Kind, das drei Wochen fehlt, kann darum sinken, wenn der Vater wöchentlich nachsieht und der Stoff gewachsen ist.
   Gebaut wie Entscheidung 3; soll der Vater-Lesepfad nur lesen?
3. **Notenschlüssel 92/81/67/50/30** (B-740, Vorschlag aus den Risiken) bestätigen.
4. Die Entscheidungen von B-740 bis B-743 tragen weiter die Marke „stellvertretend …, unbestätigt“ (Vorbereitungslauf
   2026-11-20, auf Anweisung des Nutzers entschieden, ohne `wartet_auf`).

## Konkreter Änderungsstand

Commits `1835a938` (B-740/B-741), `4aae8d36` (B-742/B-743 und Review-Fixes), danach dieses Protokoll. Neue Migration:
`20261123220126_NotenstufeChildSubjectLevel` (Vorgänger `20261123165222_PracticeSessionBuildLevel`;
`NotenstufeMigrationTests.Before` nennt ihn – beim Neuordnen der Kette nachziehen). Story-Ids B-900 ff. nicht vergeben.
