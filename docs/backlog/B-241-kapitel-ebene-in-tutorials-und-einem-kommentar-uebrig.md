---
tags: [typ/story, status/abgenommen, bereich/doku]
aliases: [B-166 Rest, Kapitel in Tutorials, Cascade-Kommentar AdultLifecycleTests]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: doku
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-23-nacht.md (Nachschau zu Sprint 2)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-166]
wartet_auf: ""
---

# B-241 · B-166 hat die Kapitel-Ebene aus der Wissenskarte entfernt – in drei Tutorials und einem Plan steht sie noch, und ein Kommentar wurde dabei falsch

B-166s AK 2 („Fach → Kapitel → Übung steht in keiner lebenden Doku mehr") war nach dem Reviewer-Fund (elf
Kommentare, `head -8`) ein zweites Mal behauptet und ein zweites Mal nicht erfüllt: der Grep lief über
`chapters/|chapterId`, nicht über das deutsche Wort in den Tutorials, die die User Story ausdrücklich nennt.

## User Story

Als **Leser der Tutorials** (Supervisor, Student, Einstiegsseite) möchte ich dieselbe Katalog-Hierarchie
lesen wie in der Wissenskarte, die B-166 gerade berichtigt hat – Fach → Reihe/Unit → Übung –, damit nicht
drei Dokumente des Repos drei verschiedene Produkte beschreiben.

## Ist-Stand am Code

**Ein Satz, der durch B-166 falsch wurde** – in Sprint 3 bereits korrigiert, hier als Beleg:
`backend/Pugling.Api.Tests/AdultLifecycleTests.cs:14-15` sagte nach dem Umschreiben „deleting the adult takes
their children, subjects and exercises with it". Wahr ist `SetNull` für `Subject.OwnerAdultId`
(`PuglingDbContext.cs:471-472`), `Exercise.AuthorAdultId` (`:512-516`), `TextbookSeries.OwnerAdultId`
(`:245-246`); `AdultsController.Delete` (`:118-152`) nimmt nur allein betreute Kinder und das Konto mit – so
heißt auch der Test (`Loeschen_Nimmt_Das_Allein_Betreute_Kind_Und_Das_Konto_Mit`). Das Wort „chapters" wurde
ersetzt, die falsche Prämisse (Katalog-Cascade) übernommen.

**Lebende Doku, die die Ebene noch nennt:**

| Datei:Zeile | Wortlaut | Richtig wäre |
| --- | --- | --- |
| `docs/tutorial-supervisor.md:340-341` | „Etappe auf einem Kapitel … ein Kapitel-Ziel" | Unit (Löschen einer Unit antwortet `exercise_in_use`, `SeriesUnitsController.cs:146-151`) |
| `docs/tutorial-student.md:31` | „Katalog (Fach → Kapitel → Übung `exerciseId 1`)" | Fach → Reihe/Unit → Übung |
| `docs/tutorial-student.md:320` | „Hierarchisch (Fach→Kapitel→Übung→Item)" | Fach→Unit→Übung→Item (`…/series-units/…`) |
| `docs/tutorial.md:20` | Creator-Zeile „Fächer, Kapitel, typisierte Übungen" | wie `rollen-doku.md:18` nach B-166 |
| `docs/vater-informationsarchitektur-plan.md:121-125` (und `:89/:94/:111/:112/:151`) | „Fach und Kapitel", Filter „Fach/Kapitel", „Neues Kapitel" | halb editierter Absatz – Zeile 120 wurde geändert, der Rest nicht; entweder ganz nachziehen oder als Plan-Historie kennzeichnen |
| `docs/uebergabe-vokabeln.md:59` | „Kapitel-Liste mit Verwendung + Löschen" | Unit-Liste (UI-Beschreibung; `:87-91` sind Tag-Beispiele und bleiben) |

**Nits aus derselben Nachschau:** `frontend/CLAUDE.md:82` nennt `?seriesId=&seriesUnitId=`, der Code reicht
auch `subjectId` durch (`createExerciseHref`, `VaterLehrwerke.tsx:100-106`); `docs/endpunkt-beziehungen.md:418`
verlinkt `Services/ChildLearnProgressService.cs`, die Datei liegt unter `Services/Student/` (tot seit
`57c724c`). `DraftRules.cs:51` und `CreatorBriefing.cs:133` sagen „Kapitel" in deutschen Laufzeit-Strings des
KI-Creators – gehört zu **B-237**.

## Die echte Lücke

Dieselbe wie in B-166s eigenem Review: ein Grep, der nicht alles fasst, gilt als Sweep. Zweimal im selben
Sprint (`head -8`, dann nur die englischen Routen-Wörter). Und einmal die Fehlerklasse B-112: ein Wort
ersetzt, die Behauptung drumherum nicht gelesen.

## Akzeptanzkriterien

1. Die sechs Stellen der Tabelle oben sind umgeschrieben (oder, für den Plan, als Historie gekennzeichnet –
   Entscheidung in Punkt 1), die beiden Nits (`frontend/CLAUDE.md:82`, `endpunkt-beziehungen.md:418`) mit.
2. Der Grep aus Punkt 2 (Routen **und** das Wort „Kapitel", mit Ausschlussliste) steht mit seiner Liste im
   `## Verlauf` und ergibt 0 – nicht „gegrept", sondern das Kommando und die Ausnahmen.
3. `AdultLifecycleTests.cs:14-15` sagt `SetNull` (bereits in `98c8431` korrigiert) – nachgelesen, nicht erinnert.

## Offene Punkte

1. `docs/vater-informationsarchitektur-plan.md`: Plan-Historie oder lebende Beschreibung? Die Datei
   beschreibt die Umstellung von 2026-08 in der Vergangenheitsform; `frontend/CLAUDE.md` verlinkt sie als
   Quelle der Informationsarchitektur. *Empfehlung: die Begriffe nachziehen, die Erzählung lassen.*
2. Beleg: der Grep aus B-166 AK 1 **plus** `Kapitel` über `docs/*.md wiki/*.md` mit der Ausschlussliste
   (Freitext-Kapitel, Unit-Labels, Zeitzeugen) → 0.

## Entscheidungen

Gegrillt im zweiten Nachtlauf 2026-10-23 (Freigabe 1, `art: Defekt`).

1. **IA-Plan: Begriffe nachziehen, Erzählung lassen** (Punkt 1). Die Datei beschreibt die Etappen von 2026-08
   in der Vergangenheitsform und wird von `frontend/CLAUDE.md` als Quelle der Informationsarchitektur
   verlinkt – also lebende Doku mit historischem Ton. „Kapitel" wird dort durch „Unit" ersetzt, wo heute die
   Unit steht (Filter auf `/vater/exercises`: Fach → Reihe → Unit, gelesen in `VaterExercises.tsx:116-137`),
   und eine Zeile am Kopf sagt, dass die Begriffe nach B-106 nachgezogen sind. **Kosten:** die Erzählung
   nennt eine Ebene, die es zur Zeit der Etappe so noch nicht hieß – die Kopfzeile trägt das.
2. **Die Liste wächst um eine siebte Stelle:** `docs/tutorial-supervisor.md:477` („Nach Katalog" (Fach →
   Kapitel → Übung → Wort)) stand nicht in der Tabelle – gefunden beim Grep über das deutsche Wort, der in
   diesem Sprint zuerst lief. Begründung: AK 2 verlangt genau diesen Grep; die Tabelle war selbst ein
   abgeschnittener Sweep. **Kosten:** eine Zeile mehr.
3. **Ausschlussliste des Sweeps, vorab festgelegt:** `docs/backlog/*` und `docs/pm-sitzung-*` (Zeitzeugen),
   `docs/api-design-bewertung.md`, `docs/codequalitaet-gates-plan.md`, `docs/db-struktur-umbau-plan.md`
   (datierte Bewertungen/Pläne des Vor-B-106-Schemas, erzählen den Umbau), `docs/tutorial-creator.md`
   (Unit-Labels „Kapitel 1 – Die Zelle" und die Sätze, die sagen, dass die Ebene weg ist),
   `docs/uebergabe-vokabeln.md:87-91` (Tag-Beispiele „Kapitel 5"). Alles andere mit „Kapitel" ist ein Treffer.
   **Kosten:** die Liste ist eine Behauptung über neun Dateien – sie steht im Verlauf, damit sie prüfbar ist.
4. **Die zwei Nits kommen mit** (AK 1): `frontend/CLAUDE.md:82` nennt `?subjectId=` nicht, der Code reicht es
   durch (`createExerciseHref`, `VaterLehrwerke.tsx:100`); `endpunkt-beziehungen.md:418` verlinkt die Datei
   am alten Ort. `frontend/CLAUDE.md` ist residenter Kontext – das Budget-Tor läuft nach der Änderung
   (`context-budget.sh`), und ein Überlauf wird im selben Schritt bezahlt. **Kosten:** +11 Byte resident.

## Schätzung

**Größe XS** (Anker B-02): sieben Doku-Zeilen, zwei Nits, ein Grep. `wo: doku`, keine Migration, kein Vertrag.

**Angriffsplan:** Grep mit Ausschlussliste (Zahl vorher) → sieben Stellen umschreiben → Nits → Grep erneut (0)
→ `AdultLifecycleTests.cs:14-15` nachlesen → Budget-Tor → Markdown-Lint → Reviewer → Commit.

**Risiko:** Ein Grep über „Kapitel" findet nicht „chapter" in englischen Kommentaren oder Routen – dafür
steht der Routen-Grep aus B-166 AK 1 daneben (`chapters/|chapterId|CurrentChapter`), und `CurrentChapter` im
`CreateTextbookDto` ist ein lebendes Vertragsfeld, kein Treffer.

**Testweg:** zwei Greps mit benannter Ausschlussliste → 0; markdownlint über `docs/`.

## Verlauf

- **2026-10-23** — **abgenommen** (zweiter Nachtlauf, Sprint 4). Reviewer (allgemeiner Agent): jede geänderte
  Zeile gegen den Code gelesen (`SeriesUnitsController.Delete :146-151` + `AnyBlockingAsync :132-134` für die
  409-Aussage; `ChildLearnProgressController :51/:66/:82` für `series-units`; `VaterExercises.tsx:116-137`);
  **ein Fund mit Doku-Wirkung, Fehlerklasse B-112 in eigener Sache:** die Wortersetzung Kapitel→Unit machte
  im IA-Plan vier Sätze falsch (`:93` Anlegen im Katalog ist heute Fach+Art, Reihe/Unit unter
  `/vater/lehrwerke`; `:99`/`:124` Link geht auf Lehrwerke; `:155` E2E legt die Unit unter Lehrwerke an) und
  `:121` nannte „Fach & Unit", es sind Fach (Filter), Reihe & Unit – alle fünf umformuliert, historische
  Sätze mit „(heute: …)" ergänzt. Nits übernommen (`DraftRules.cs:51`; Byte-Zahl unten).
  **Rollengang im Browser (Vater, Freigabe 6):** Login Vater 1 → `/vater/exercises` zeigt die Filter
  **Fach · Reihe (– alle –) · Unit (– alle –)**, `/vater/exercises/neu` zeigt **Fach (filtert nur die Reihen) ·
  Reihe · Unit** und den Satz „Fehlt eine Reihe oder Unit? … unter 📕 Lehrwerke" – genau das, was der Plan
  jetzt behauptet. Commit `7c33765` (Protokoll `docs/pm-sitzung-2026-10-23-nacht.md`, „Sprint 4").
- **2026-10-23** — **gebaut** (zweiter Nachtlauf, Sprint 4). **Sweep vorher:** `grep -n "Kapitel"` über
  `docs/*.md wiki/*.md docs/REST/*.md docs/REST/*.http frontend/CLAUDE.md backend/CLAUDE.md
  backend/Pugling.Api/CLAUDE.md backend/Pugling.Agent.Creator/CLAUDE.md CLAUDE.md`, Ausschlussliste
  `docs/pm-sitzung-*`, `docs/api-design-bewertung.md`, `docs/codequalitaet-gates-plan.md`,
  `docs/db-struktur-umbau-plan.md`, `docs/tutorial-creator.md`, `docs/uebergabe-vokabeln.md:87-91` →
  **16 Treffer** (sieben Stellen + neun Zeilen im IA-Plan). **Nachher: 1 Treffer** – die neue Kopfnotiz des
  IA-Plans selbst (`:9`, nennt das Wort, um es abzulösen) und die Kopfzeile von `Creator.http:11` („Kapitel gibt
  es nicht mehr"), beide gewollt. **Routen-Sweep** `chapters/|chapterId|/chapters` über dieselben Pfade plus
  `docs/REST/`: Treffer nur in `docs/tutorial-creator.md:43` (sagt, dass die Route weg ist),
  `docs/anmerkungen/*` (Zeitzeugen, nachträglich in die Ausschlussliste) und **`docs/REST/Creator/
  VocabularyExecises.http` (neun Requests) → B-242**. AK 3: `AdultLifecycleTests.cs:14-15` nachgelesen, sagt
  `SetNull`. `frontend/CLAUDE.md`: 10.223 → **10.208 Byte** (die +11 Byte des Query-Tripels sind durch eine
  doppelte Klammer zwei Zeilen tiefer bezahlt, netto −15); die Datei liegt **weiter +1.208 Byte über dem
  9.000-Byte-Budget** – das ist Altschuld vor diesem Sprint, hier nur gemeldet, nicht bezahlt.
  markdownlint-cli2 über 348 Dateien: 0 Funde.

- **2026-10-23** — gegrillt und geschätzt im zweiten Nachtlauf desselben Tages (Freigabe 1); beim Grillen eine siebte Stelle gefunden (Entscheidung 2).

- **2026-10-23** — angelegt aus der **Nachschau** zu Sprint 2 des Nachtlaufs (Commit `95b0624`),
  `entgangen_bei: B-166`. Der falsche Cascade-Kommentar ist im selben Lauf (Sprint 3) korrigiert – ein Satz,
  den ich selbst am selben Tag falsch geschrieben hatte; die sechs Doku-Stellen bleiben hier offen, weil sie
  eine eigene Grep-Runde mit Ausschlussliste brauchen und der Lauf nach Sprint 3 endet (Limit 2).
