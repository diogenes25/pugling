---
tags: [typ/story, status/abgenommen, bereich/training, rolle/student, rolle/supervisor, lerntechnik/vokabeln]
aliases: [Akzent-Toleranz, Akzentfehler]
status: abgenommen
prio: P2
art: Wunsch
groesse: L
wo: beides
migration: ja
vertragsbruch: nein
quelle: "Chat mit dem Vater, 2026-11-17 (Erfahrung aus der Testphase mit Friedrich)"
grund: ""
ersetzt_durch: []
nachgeschaut: 2026-11-25
---

# B-624 · Ein falscher Akzent im Französischen zählt nicht als ganzer Fehler

Wer im Französischen einen Akzent, einen Strich oder einen Buchstaben falsch setzt, bekommt heute einen
kompletten Fehler – mit Kasten zurück und gegebenenfalls Malus. Der Vater hält das für zu streng und
wünscht eine mildere Wertung.

**Achtung, das kehrt eine frühere Entscheidung um:** [B-422](B-422-beinahe-treffer-tippfehler.md),
Entscheidung 4, hat am 2026-11-13 (Stakeholder-Entscheidung im Auftrag des Nutzers) ausdrücklich **keine**
Normalisierung für Akzente beschlossen, weil die Schule „é" für „e" als Rechtschreibfehler zählt; ein
Beinahe-Treffer wird nur als „falsch geschrieben" gekennzeichnet und bleibt falsch. Dahinter steht
[B-524](B-524-vokabeltest-wie-in-der-schule.md)/2 („falsch geschrieben ist falsch"). Die Grill-Runde muss
also entscheiden, ob und wo das jetzt anders gilt – Kandidaten: nur Akzente vs. jeder Beinahe-Treffer,
nur im Üben vs. auch im Test, halber Fehler (wie der LRS-Ausgleich −½ in Friedrichs Schule) vs. richtig
mit Hinweis, global vs. Option je Position/Kind.

## User Story

Als **Lernender** möchte ich, dass ein Wort, das ich weiß und nur an einem Akzent oder einem Buchstaben falsch
schreibe, nicht wie ein ungewusstes Wort zählt, damit ich nicht für Kleinigkeiten zurückgeworfen werde und die
Lust verliere. Als **Supervisor** möchte ich trotzdem sehen, dass die Schreibweise noch nicht sitzt.

## Ist-Stand am Code

Recherchiert am 2026-11-17 gegen `5938fc12`.

- **Eine Regel für Üben, Test und Vorschau.** `AnswerGrader` ist der einzige Vergleich
  (`backend/Pugling.Api/Services/Shared/AnswerGrader.cs:3`–`:16`); er normalisiert über
  `StageMechanics.Normalize` (`backend/Pugling.Api/Services/Shared/StageMechanics.cs:40`).
- **`Normalize` faltet keine Akzente.** Es verzeiht Groß/klein, Leerraum, die Schreibweise des Apostrophs und das
  Satzzeichen am Ende (`StageMechanics.cs:31`–`:57`) – „é“ und „e“ bleiben verschieden. Dazu verzeiht die
  Artikelregel einen fehlenden führenden Artikel, auch französisch (`StageMechanics.cs:66`).
- **„Falsch geschrieben“ wird erkannt, aber nur angezeigt.** `AnswerGrader.IsMisspelled`
  (`AnswerGrader.cs:35`–`:47`): Damerau-Levenshtein ≤ 1 bis fünf Buchstaben, ≤ 2 darüber, und kein anderes Atom
  der Übung ist näher dran. Ein reiner Akzentfehler hat Abstand 1 und fällt damit immer darunter – aber genauso
  ein falscher oder fehlender Buchstabe. Aufrufer: Üben (`backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:489`,
  `:706`), Test (`backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:431`), Vorschau
  (`backend/Pugling.Api/Services/Creator/ExercisePreviewService.cs:96`). Die Wertung bleibt falsch
  (`PositionPracticeController.cs:486`–`:489`, Kommentar „named, not forgiven“).
- **Eine mildere Abstufung gibt es schon – je Position, für jeden Fehler.** `PlanPosition.ForgivingDemotion`
  (B-219, `backend/Pugling.Api/Models/PlanPositionEntities.cs:125`–`:129`): ein einzelner Fehler senkt um einen
  Kasten statt auf Kasten 1; der plan-übergreifende Lernstand je Wort bleibt hart.
- **Friedrich tippt Französisch.** Seit [B-597](B-597-tippstufen-fragen-die-zielsprache-ab.md) wird getippt immer
  die Zielsprache; in Plan 8 sind das französische Wörter mit Akzenten.
- **Zwei frühere Entscheidungen stehen dagegen.** [B-524](B-524-vokabeltest-wie-in-der-schule.md)/2 (Grill-Runde
  **mit dem Vater**, 2026-11-13): „Die Wertung bleibt binär … wertet falsch geschrieben als falsch“ – gegen die
  damalige Empfehlung von Teilpunkten; das −½ auf Friedrichs Blatt ist sein LRS-Ausgleich und wird bewusst nicht
  nachgebildet. [B-422](B-422-beinahe-treffer-tippfehler.md)/4 (Stakeholder im Auftrag): keine eigene
  Normalisierung für Akzente, weil die Schule „é“ für „e“ als Fehler zählt.

## Die echte Lücke

Technisch fehlt wenig – der Beinahe-Treffer wird schon erkannt, und eine mildere Kasten-Regel gibt es. Die Lücke
ist eine **Produktentscheidung**: Der Vater erlebt die binäre Wertung jetzt als zu streng und muss B-524/2 bewusst
neu entscheiden. Danach fehlen (a) die Abgrenzung, *was* milder zählt (nur Akzente oder jeder Beinahe-Treffer),
(b) *wie* milder (richtig mit Hinweis, Kasten halten, halber Punkt), (c) *wo* (Üben, Test) und (d) *für wen*
(global, je Position, je Kind).

## Offene Punkte

Alle in der Grill-Runde vom 2026-11-17 entschieden – siehe Entscheidungen.

1. ~~**Wird B-524/2 revidiert?**~~ → Entscheidung 1.
2. ~~**Nur Akzente oder jeder Beinahe-Treffer?**~~ → Entscheidung 2.
3. ~~**Wie milder?**~~ → Entscheidung 3.
4. ~~**Global, je Position oder je Kind?**~~ → Entscheidung 4.
5. ~~**Welche Sprachen?**~~ → Entscheidung 5.
6. ~~**Was sieht der Vater?**~~ → Entscheidung 6.
7. ~~**Plan-übergreifender Lernstand je Wort.**~~ → Entscheidung 7.

## Entscheidungen

Grill-Runde mit dem Vater, 2026-11-17. Alle sieben nach Empfehlung.

1. **B-524/2 wird für das Üben revidiert, der Abschlusstest bleibt binär.** Im Üben soll das Kind nicht für
   Kleinigkeiten zurückgeworfen werden; der Test misst weiter, was die Schule misst, und hält damit den Grund
   der Entscheidung vom 2026-11-13 (ehrlich zeigen, wo die Rechtschreibung fehlt). *Kosten:* Üben und Test werten
   verschieden, `AnswerGrader` trennt beide erstmals (`AnswerGrader.cs:15` sagt heute das Gegenteil); B-422/4
   gilt für den Test weiter.
2. **Milder zählen nur Diakritika.** Antwort und Lösung sind nach dem Entfernen der Zeichen auf und unter dem
   Buchstaben gleich (é/è/ê→e, à→a, ç→c, ï→i, ô→o, ü/ä/ö→u/a/o …), vorher nicht. Ein falscher, fehlender oder
   vertauschter Buchstabe bleibt ein ganzer Fehler. Begründung: Ein Buchstabe ist bei LRS die Schwäche, die
   geübt werden soll; ein Akzent ist oft eine Tastenfrage. *Kosten:* „ecolle“ und „Strase“ bleiben falsch (mit
   Markierung „falsch geschrieben“).
3. **Ein Akzentfehler zählt halb.** Die Karte bleibt im Kasten (kein Auf-, kein Abstieg) und kommt zum nächsten
   fälligen Termin wieder; keine Münzen; die Combo reißt nicht; als Pflicht-Karte zählt sie als gespielt. Das
   Blatt zeigt „Fast! Achte auf den Akzent: *Lösung*“. Begründung: nicht zurückwerfen, aber auch nicht
   aufsteigen lassen, bevor die Schreibweise sitzt – sonst überrascht der strenge Test. *Kosten:* ein dritter
   Ausgang neben richtig/falsch im Review; Combo, Tagestruhe und Pflicht-Zählung lernen ihn.
4. **Je Position einstellbar, standardmäßig an** – auch für die bestehenden Positionen (die Migration setzt
   den Haken). Der Vater schaltet es für eine Position ab, die gezielt Akzente übt. *Kosten:* ein Feld an
   `PlanPosition` (Migration, Bestand → an), ein Schalter im Vater-Web, Vertragsfeld an den Positions-DTOs;
   Friedrichs Plan 8 wertet ab dem Einspielen milder.
5. **Für jede Sprache**, weil die Regel an Zeichen hängt. *Kosten:* deutsche Umlaute fallen mit darunter;
   seit B-597 wird kaum Deutsch getippt.
6. **Eigene Kategorie „Akzent“** im Lernstand bzw. Positions-Report neben „falsch geschrieben“ und „falsch“.
   Begründung: Der Vater sieht, ob Wörter oder nur Akzente fehlen. *Kosten:* ein Zähler im Report.
7. **Der plan-übergreifende Lernstand je Wort bleibt hart** (Akzentfehler = falsch), wie bei B-219.
   Begründung: ehrliche Messung, ob das Wort samt Schreibweise sitzt. *Kosten:* Position und Wort-Lernstand
   können auseinanderlaufen.

## Akzeptanzkriterien

1. Im Üben einer Position mit eingeschalteter Milde liefert „ecole“ für „école“ den Ausgang *halb*: Kasten
   unverändert, keine Münzen, Combo unverändert, Karte als gespielt gezählt; das Blatt zeigt
   „Fast! Achte auf den Akzent: école“.
2. „ecolle“ für „école“ bleibt falsch und „falsch geschrieben“ (Kasten zurück wie heute bzw. nach
   `ForgivingDemotion`).
3. Im Abschlusstest ist „ecole“ für „école“ falsch, wie heute (Integrationstest).
4. Mit ausgeschaltetem Haken verhält sich das Üben wie heute (Integrationstest).
5. Nach der Migration tragen alle bestehenden Positionen den Haken *an*; neue Positionen ebenso.
6. Ein Akzentfehler in einer anderen Sprache (Umlaut, spanisches ñ) wird gleich behandelt.
7. Der Positions-Report bzw. Lernstand weist Akzent-Fälle als eigene Kategorie aus.
8. Der Lernstand je Wort (`ItemProgressService`) wertet den Akzentfehler weiter als falsch.

## Schätzung

**Größe: L** — [B-219](B-219-verzeihende-abstufung-je-position.md) (Positions-Schalter + andere Kasten-Regel, M)
plus ein **dritter Review-Ausgang**, der durch Combo, Punkte, Kasten, Lernstand und Report läuft, plus zwei
Spalten mit Bestandsdaten. Kein XL: kein neuer Endpunkt, und jede Stelle hat ein Vorbild im Repo.

- **`migration: ja`** — zwei Spalten, angehängt an die jüngste Migration der Kette (heute
  `20261117061523_StudyPlanSeriesUnit`):
  - `PlanPosition`: Schalter für die Akzent-Milde. Bestand **an** (E4) – per `migrationBuilder.Sql("UPDATE …")`,
    **kein** DB-Default (Tor G9).
  - `ReviewEvent` (`backend/Pugling.Api/Models/StudyPlanEntities.cs:146`–`:153`): Kennzeichen „halb“, Bestand
    aus. Grund: Die Combo wird aus `WasCorrect` der gespeicherten Reviews gezählt
    (`backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:526`–`:530`); ein „halb“ als
    `WasCorrect = false` risse sie, als `true` zählte es in `MetricsService.cs:30` als Treffer.
- **`vertragsbruch: nein`** — nur additive Felder: an `ReviewOutcome`
  (`backend/Pugling.Contracts/Student/PracticeDtos.cs:146`, neben `Misspelled`), an `PositionResponse` und den
  Create/Update-DTOs der Position (Vorbild `ForgivingDemotion`, `backend/Pugling.Contracts/Supervisor/StudyPlanDtos.cs:144`,
  `:206`), am Positions-Report. Update ist nullable ohne Clear-Schalter (wie `ForgivingDemotion`).

### Risiken

1. **Die Combo.** „Reißt nicht“ heißt: Ein halbes Review wird beim Rückwärtszählen übersprungen, nicht als
   Treffer gezählt (sonst stiege der Combo-Bonus durch Akzentfehler). Test: richtig, halb, richtig ⇒ Combo 2.
2. **Pflicht und Tagestruhe.** Wo die Pflicht „gespielte Karten“ zählt, ist nicht über `ReviewEvent` gelöst
   (kein Treffer in `PositionProgressService`); beim Bauen nachsehen, ob sie am Kasten, an `LastReviewedAt` oder
   an Treffern hängt. Hängt sie an Treffern, braucht E3 („zählt als gespielt“) dort einen eigenen Zweig.
3. **Kasten bleibt, aber Fälligkeit?** `ApplyReview` (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:557`–`:579`)
   kennt nur richtig/falsch. „Kommt zum nächsten fälligen Termin wieder“ heißt: Kasten unverändert, neuer
   Termin nach dem Intervall des **aktuellen** Kastens – sonst bliebe die Karte sofort fällig und würde in
   derselben Runde erneut gezogen.
4. **Die Faltung selbst.** Diakritika über Unicode-Zerlegung (`NormalizationForm.FormD`, Zeichen der Klasse
   `NonSpacingMark` entfernen) – aber **ß** zerlegt sich nicht und bleibt (E2: Buchstabe), **œ/æ** ebenso.
   Gilt nur, wenn die Antwort **ohne** Faltung falsch und **mit** Faltung richtig ist, also nach dem vollen
   `Matches` (Artikel, Bedeutungen, Alternativen).
5. **Test bleibt binär.** `AnswerGrader` wird erstmals zwischen Üben und Test getrennt; der Test-Pfad
   (`PositionTestsController.cs:431`) darf die neue Methode nicht rufen.
6. **Echte Kinder.** Die Migration schaltet Friedrichs und Theodors Positionen sofort milder (E4) – das ist
   gewollt, gehört aber in den Rollengang als bewusster Blick auf eine echte Position (Kopie der DB).

### Angriffsplan

1. `AnswerGrader.IsAccentSlip(given, item)` + Unit-Tests der Faltung (é/ç/ü/ñ ja, ß/œ/fehlender Buchstabe nein).
2. Migration (zwei Spalten, Bestand per `UPDATE`), Modell, Vertrag (additiv), Positions-CRUD nach dem Muster
   `ForgivingDemotion` (`backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:47`, `:206`, `:324`).
3. Review-Pfad: dritter Ausgang in `PositionPracticeController.Review` – kein `ApplyReview`-Abstieg, keine
   Münzen, Combo übersprungen, `ItemProgressService.RecordAsync` mit `false` (E7), `ReviewEvent` halb.
4. Positions-Report: Kategorie „Akzent“ (`backend/Pugling.Api/Services/Supervisor/PositionReportService.cs:41`–`:44`).
5. Frontend: Blatt „Fast! Achte auf den Akzent“ in `frontend/src/sohn/SohnPractice.tsx` (neben der
   `Misspelled`-Anzeige), Schalter im Positions-Formular des Vater-Webs, Spalte im Report.

### Testweg

- Integrationstests in `Pugling.Api.Tests` nach dem Vorbild der B-219-Tests: je AK ein Fall; AK1 misst Kasten,
  Wallet, Combo und Pflicht vorher/nachher; AK3 über den echten Positions-Test; AK5 als Migrations-Test.
- `SchemaGuardTests` (G9: kein DB-Default), `PatchSemanticsTests` laufen im Test-Tor mit.
- Vitest für die Blatt-Anzeige und den Schalter; E2E: Erweiterung von `frontend/e2e/artikel-pflicht.spec.ts`
  oder eine neue `akzent-milde.spec.ts` (Sohn tippt „ecole“ ⇒ „Fast!“, Kasten unverändert) – zugleich Rollengang.
- Reviewer: `pugling-reviewer` und `frontend-reviewer`.

## Verlauf

- **2026-11-17** — angelegt aus dem Chat mit dem Vater; Konflikt mit B-422/4 benannt.
- **2026-11-17** — ausformuliert: Ist-Stand gegen den Code (`Normalize` ohne Akzent-Faltung, `IsMisspelled` nur
  Anzeige, `ForgivingDemotion` als vorhandene Milde), Konflikt mit B-524/2 (Entscheidung des Vaters) benannt,
  sieben offene Punkte mit Empfehlung.
- **2026-11-17** — gegrillt mit dem Vater: sieben Entscheidungen, alle nach Empfehlung; B-524/2 gilt nur noch
  für den Abschlusstest.
- **2026-11-17** — geschätzt: L, beides, Migration ja (Schalter an der Position mit Bestand an, „halb“ am
  `ReviewEvent` wegen der Combo), kein Vertragsbruch (nur additive Felder).
- **2026-11-25** — Schätzung gegen den Code geprüft (Sprint 164): trägt. Risiko 2 erledigt – die Pflicht hängt am
  Rundencursor bzw. am Test, nicht an Treffern. Neu: „falsch geschrieben“ wird nirgends gespeichert, darum ein drittes
  Kennzeichen an `ItemReviewEvent` für den Report. AK 2 „und ‚falsch geschrieben‘“ gilt für „ecolle“/„école“ nicht (Abstand 2,
  Schwelle aus B-422 bei fünf Buchstaben 1) – unverändertes Verhalten; geprüft wird, was AK 2 trägt.
- **2026-11-25** — gebaut: `AnswerGrader.IsAccentSlip` (Faltung FormD ohne `NonSpacingMark`, über dieselben Schreibweisen wie
  `IsMisspelled`); Migration `AccentLeniency` (Positions-Schalter, Bestand per `UPDATE` an; `AccentSlip` an `ReviewEvent` und
  `ItemReviewEvent`); Vertrag additiv (`PositionResponse`/`Create`/`UpdatePositionDto.AccentLenient`,
  `ReviewOutcome.AccentSlip`, `ItemReport.AccentSlips`, `MixedReviewOutcome.AccentSlip`); dritter Zweig im Üben
  (`PositionPlayService.ApplyAccentSlip`: Kasten hält, Termin nach dem Intervall des Kastens; Combo überspringt den Fehler);
  `ItemProgressService.RecordAsync` mit Pflichtparameter `accentSlip`. Frontend: halbes Blatt „Fast! Achte auf den Akzent:“
  (gold, Stelle markiert), Schalter „Akzentfehler halb werten“ (Standard an) mit Erklärung, Report-Spalte „Akzent“, Hinweis
  „Akzent streng“ in der Positionsliste, Mischrunden-Zeile „nur der Akzent – zählt halb“. `scripts/plan-kopieren.ps1` nimmt
  das Feld mit.
- **2026-11-25** — Reviews (`pugling-reviewer`, `frontend-reviewer`): kein 🔴. 🟡 übernommen: (1) die Mischrunde schreibt den
  Kasten der Position und warf einen Akzentfehler auf Kasten 1 – folgt jetzt dem Schalter der Position (sonst hinge der
  Kasten am Bildschirm); (2) `plan-kopieren.ps1` verlor das Feld; (3) die Verdrahtung in `SohnPractice` war ungetestet;
  (4) eine leere Zusicherung („keine Münzen“ auf einem roten Blatt) entfernt. 🟢 übernommen: Report zählt nur gewertete
  Akzentfehler, „ou“/„où“ dokumentiert, Listen-Hinweis, Spaltenerklärung. 🟢 bewusst nicht: Report nur für Vokabeln (Lücken
  ohne `ItemId`), Verlauf je Wort ohne Kennzeichen, Maskottchen-Geste auf dem halben Blatt (Blick am Gerät). Bewusst streng
  bleiben Abschlusstest, Bonus-Training, Turmspiel und Lernkarte (bewegen keinen Positions-Kasten). **Fehlerzähler: 2** (zwei 🟡
  im Increment: Mischrunde, Kopier-Skript).
- **2026-11-25** — Belege je AK-Fall (`AkzentMildeTests.cs`, `AkzentMildePfadTests.cs`, `MischrundeWertungTests.cs`, Vitest):
  - AK 1 Kasten/Termin/Münzen → `Akzentfehler_Haelt_Den_Kasten_Und_Zahlt_Nichts`; M1, M8, M9, M16 rot
  - AK 1 Combo → `Akzentfehler_Reisst_Die_Combo_Nicht` (1, 1, 2); M6, M7 rot
  - AK 1 Blatt → `Lektion.test.tsx` „halbe Blatt“; F2, F3 rot · Verdrahtung → `SohnPractice.test.tsx`; F1, F7, F8 rot
  - AK 1 „als gespielt gezählt“ → der Rundencursor rückt bei jeder Antwort vor (unbedingter Code, nicht eigens getestet)
  - AK 2 Buchstabe falsch → `Buchstabenfehler_Bleibt_Ein_Ganzer_Fehler`, Unit-Fälle inkl. ß/œ; M2, M3 rot
  - AK 3 Test bleibt binär → `Abschlusstest_Wertet_Den_Akzentfehler_Als_Falsch`, Absichtserklärung (grün auch vor B-624)
  - AK 4 Haken aus → `Ohne_Haken_…`; M5 rot
  - AK 5 neue Positionen, PATCH → `Neue_Position_Ist_Mild_Und_Abschaltbar`; M13, M14b rot · Vater-Web → `PlanPositions.test.ts`; F4–F6 rot
  - AK 5 Bestand → `Migration_Schaltet_Bestehende_Positionen_Mild`; M12 rot
  - AK 6 andere Sprachen → Unit-Fälle Umlaut, ñ; M1 rot
  - AK 7 Report → `Report_Zaehlt_Akzentfehler_Je_Wort` (gewertet 1, Treffer 0, ungewertet nicht doppelt); M10, M11, M20 rot
  - AK 8 Lernstand je Wort hart → `Planuebergreifender_Lernstand_…`; M15 rot
  - Review-Fund Mischrunde → `Akzentfehler_Haelt_Den_Kasten_Nur_Auf_Milder_Position`; M17, M18, M19 rot · Zeile → F9 rot

  Mutanten-Probe (`mutant-probe.py`), zwei Läufe: 11:40 (vor den Review-Fixes) und 11:56 (nach der letzten Code-Änderung):
  M1–M3, M5–M13, M15, M16, F1–F6 rot (M14 kompilierte nicht, als M14b neu gemessen); M14b, M17–M20, F7–F9 rot. Grundlinie
  nach dem Zurückspielen beide Male grün. M10 maß die Zeile vor ihrer Änderung zu `accentSlip && scored`; die neue Form trägt M20.
- **2026-11-25** — Suite nach der letzten Code-Änderung: .NET 2440/2440 (Release, Ausgangsstand 2413), Vitest 1468/1468,
  `tsc -b` sauber. Rollengang an einer frischen Kopie der echten DB (`kopie-server.sh`, :5201): der Start spielte
  `AccentLeniency` ein (Migrationsprobe – alle 212 Positionen mild, `foreign_key_check` leer). Friedrich, Plan 15, Position 166,
  eine Runde: „une autoroute“ richtig (Kasten 5, Combo 1); „le peage“ → halb, Kasten 4 bleibt, 0 Münzen, Combo 1; „le depart“ →
  halb, Kasten 1 bleibt, morgen fällig statt heute; „une pausse“ → ganzer Fehler, Kasten 3 → 1, Combo 0. Vater-Report: „Akzent“
  1 bei péage und départ, 0 bei autoroute und pause. Abschlusstest derselben Position: „le peage“ falsch und „falsch
  geschrieben“. Offen für einen Menschen: das halbe Blatt am Handy (Gold statt Rot, Markierung, Maskottchen).
- **2026-11-25** — abgenommen in `b7538326`: der Vater hat das halbe Blatt am Gerät gesehen („passt so“); Sohn damit abgenommen, Vater abgenommen (Report), Creator Regressionszeuge.
