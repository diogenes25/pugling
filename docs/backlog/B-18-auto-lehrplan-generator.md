---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/katalog, rolle/supervisor]
aliases: [Auto-Lehrplan-Generator]
status: geschaetzt
prio: P2
art: Wunsch
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: memory/uebungs-metadaten.md
grund: ""
ersetzt_durch: []
---

# B-18 · Lehrplan automatisch aus gefilterten Übungen bauen

Die Übungs-Metadaten (Klassenstufe, Schulart, Quelle, Art) und ein Filter-Endpunkt sind das behauptete
Fundament; die Recherche zeigt, dass darauf schon ein funktionierender Generator steht (`/vater/wizard`) –
die echte Lücke liegt in drei schmalen Stellen, nicht in einem fehlenden Feature.

## User Story

Als **Vater**, der einen Lehrplan für ein Kind aufsetzt, möchte ich Übungen nach Fach, Klassenstufe,
Schulart, Kategorie, Übungstyp **und** Quelle vorfiltern und die Treffer in einem Schritt zu einem
vollständigen Lehrplan mit Positionen machen, damit ich nicht jede passende Übung einzeln aus dem
gesamten Katalog heraussuchen muss.

## Ist-Stand am Code

- Der Filter-Endpunkt existiert bereits genau mit dieser Zweckbeschreibung im Code:
  `ExerciseCatalogController.Search` (`backend/Pugling.Api/Controllers/Creator/ExerciseCatalogController.cs:12-16`)
  trägt das Doc-Kommentar „the pre-filtering as a basis for the (future) automatic study plan
  generation" – die Übung dokumentiert selbst, dass sie als Fundament gedacht ist. Er filtert
  UND-verknüpft nach `subjectId`, `chapterId`, `grade`, `schoolType`, `categoryId`, `type`, `search`
  (Zeilen 44-101). **`source` ist kein Query-Parameter** – das Feld wird nur zurückgegeben
  (`ExerciseSummary.Source`, `Pugling.Contracts/Creator/ExerciseCatalogDtos.cs:15`), nicht gefiltert.
- Ein Generator, der aus einem Filter direkt einen `StudyPlan` mit Positionen baut, existiert bereits als
  UI-Feature: `frontend/src/vater/VaterWizard.tsx` (Kopf-Kommentar Zeile 16-22) führt den Vater durch
  Kind → Fach/Klasse/Schulart+Suchtext → **Übungsauswahl aus dem gefilterten Katalog** → Feinschliff →
  Abschluss. Die Suche läuft über `api.searchExercises` mit `subjectId`, `grade`, `schoolType`, `search`
  (`VaterWizard.tsx:107-115`); ein Knopf **„Alle wählen"** übernimmt in einem Klick alle geladenen Treffer
  (`VaterWizard.tsx:145`, Knopf Zeile 313).
- Die Fein-Einstellungen der Positionen (Bestehensgrenze, Punkte, Malus, Stufe, getippter Test) leitet der
  Assistent **automatisch** aus einem kurzen Fragenkatalog ab (Ziel × Intensität,
  `VaterWizard.tsx:24-42,127-140`) – auch das ist bereits Automatik, keine Handarbeit je Position.
- Server-seitig entsteht der Plan **nicht** in einem Rutsch: `StudyPlansController.Create` legt nur den
  leeren Container an (`backend/Pugling.Api/Controllers/Supervisor/StudyPlansController.cs:74-105`),
  `PlanPositionsController.Create` nimmt genau **eine** `ExerciseId` je Aufruf entgegen
  (`backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:106-167`). Der Abschluss des
  Assistenten (`frontend/src/vater/wizardFinish.ts:68-115`) ruft `addPosition` darum **sequenziell in
  einer Schleife**, mit Wiederaufnahme-Fortschritt (`WizardProgress`) statt Transaktion.
- Der Client kennt `categoryId`/`type` bereits als Suchparameter
  (`frontend/src/lib/uiTypes.ts:67-82`, durchgereicht in `frontend/src/lib/api.ts:384-397`) – **der
  Wizard nutzt sie nur nicht**: Schritt 3 exponiert weder Kategorie- noch Typ-Auswahl. Eine
  Kategorienliste je Fach existiert bereits (`api.categories(subjectId)`, `frontend/src/lib/api.ts:319-320`).
- „Alle wählen" übernimmt nur die **geladene erste Seite** (`PagingExtensions.DefaultTake = 100`,
  `backend/Pugling.Api/Controllers/PagingExtensions.cs:9`); `TruncationHint` zeigt zwar „X von Y", aber es
  gibt keinen Weg, mehr als die erste Seite zu übernehmen.

## Die echte Lücke

Nicht „der Generator fehlt", sondern: **er ist schon da, aber unvollständig verdrahtet.** Drei konkrete,
unabhängige Lücken bleiben:

1. Der Wizard filtert nicht nach Kategorie/Übungstyp, obwohl Server **und** Client-Bibliothek das schon
   können – reine Verdrahtungslücke im Formular.
2. „Quelle" ist entgegen der Prämisse der Idee (und der Memory-Notiz) **serverseitig gar nicht
   filterbar** – nur ein zurückgegebenes Feld, kein Query-Parameter.
3. „Alle wählen" ist bei mehr als 100 Treffern **kein** „alle passenden übernehmen", sondern „die ersten
   100 übernehmen" – für einen Anspruch namens „Generator" eine stille Kappung.

Ein neuer serverseitiger Bulk-Endpunkt für Positionen ist **nicht** die Lücke: Der bestehende
Sequenz-Mechanismus (`wizardFinish.ts`) funktioniert inklusive Fehlerbehandlung und Wiederaufnahme; er
wurde genau dafür gebaut (siehe [B-53](B-53-wizard-doppelklick.md)).

## Offene Punkte

1. ~~Wie verhält sich ein neuer Generator zum vorhandenen Lehrplan-Assistenten – ersetzt er ihn oder füllt
   er ihn vor?~~ → siehe Entscheidung 1.
2. ~~Läuft der Generator serverseitig oder im externen KI-Agenten (Backend soll bewusst kein LLM tragen)?~~
   → siehe Entscheidung 2.
3. ~~Sollen Kategorie- und Typ-Filter, die der Server schon anbietet, im Wizard nachgezogen werden?~~ →
   siehe Entscheidung 3.
4. ~~Soll „Quelle" zu einem echten Server-Filter werden, wie es die Idee/Memory-Notiz unterstellt?~~ →
   siehe Entscheidung 4.
5. ~~Wie geht „Alle wählen" mit mehr als 100 Treffern um?~~ → siehe Entscheidung 5.
6. ~~Braucht es einen serverseitigen Bulk-Create-Endpunkt für Positionen, um „automatisch" zu rechtfertigen?~~
   → siehe Entscheidung 6.

## Entscheidungen

1. **Der Generator ist der Wizard, kein Parallelbau.** `VaterWizard` erfüllt bereits Filter → Auswahl →
   Plan+Positionen in einem Zug; B-18 erweitert genau diese Stelle, statt eine zweite Oberfläche oder einen
   zweiten Endpunkt zu bauen. Begründung: eine zweite Ablage desselben Ablaufs (Kind→Plan→Positionen)
   würde sofort auseinanderlaufen (zwei Stellen für dieselbe Doppelklick-Sperre, zwei Stellen für
   Fein-Einstellungen). Kosten: keine – es ist eine Erweiterung, kein zusätzliches Bauteil.
2. **Kein LLM, keine Agenten-Beteiligung.** Der bestehende Mechanismus ist vollständig deterministisch
   (SQL-Filter + Fragenkatalog-Ableitung); das entspricht der Vorgabe „kein LLM im Backend" direkt, ohne
   dass etwas entschieden werden müsste. Begründung: die Frage der Idee war spekulativ und durch den
   Ist-Stand bereits beantwortet. Kosten: keine.
3. **Kategorie- und Typ-Filter werden im Wizard-Schritt „Übungen" ergänzt.** Begründung: Server und
   Client-Bibliothek unterstützen `categoryId`/`type` bereits (`ExerciseSearchParams`), eine
   Kategorienliste je Fach existiert (`api.categories`); es fehlt nur die Formularsteuerung. Ohne diese
   Ergänzung bleibt ein Vater bei großen Fächern auf Freitextsuche angewiesen, obwohl der Server die
   passende Struktur längst hat. Kosten: zwei neue `<select>`-Felder samt State
   (`categoryId`, `type` – Typen aus dem bestehenden Typ-Manifest `api.exerciseTypes()`), keine
   Vertrags- oder Backend-Änderung.
4. **`source` wird ein additiver Query-Parameter am bestehenden Such-Endpunkt.** Begründung: Die Idee (und
   die Memory-Notiz „Übungs-Metadaten") behauptet Klassenstufe/Schulart/**Quelle**/Art als Fundament –
   für Quelle stimmt das heute nicht, und das ist eine irreführende Lücke, kein Rand-Detail. Ein optionaler
   Substring-Filter auf `Exercise.Source` (analog zum bestehenden `search`-Muster,
   `ExerciseCatalogController.cs:80-85`) schließt sie. Kosten: ein neuer optionaler Parameter im
   Controller + `ExerciseSearchParams`/`api.searchExercises` im Frontend, ein Testfall in
   `ExerciseMetadataTests` – additiv, kein Vertragsbruch (bestehende Aufrufe ändern sich nicht).
5. **„Alle wählen" fragt bei Bedarf bis zur Server-Obergrenze (500) nach, statt bei 100 zu kappen; echte
   Vollständigkeit über 500 hinaus bleibt bewusst zurückgestellt.** Begründung: `take` bis 500 ist bereits
   serverseitig erlaubt (`PagingExtensions`); ein zweiter Klick/eine zweite Anfrage mit `take=500` löst die
   auffälligste stille Kappung, ohne ein neues Paging-UI zu bauen. Über 500 Treffer je Fach/Klasse/Schulart
   in einem Filter sind ein Rand-Fall, den `TruncationHint` weiterhin sichtbar macht – ein vollständiges
   Cursor-Paging im Wizard wäre eine eigene, größere Story. Kosten: eine bedingte zweite Anfrage mit
   höherem `take`, ausgelöst vom „Alle wählen"-Knopf, wenn `total > shown`.
6. **Kein neuer Bulk-Create-Endpunkt für Positionen.** Begründung: `wizardFinish.ts` deckt Fehlerfall und
   Wiederaufnahme bereits ab (B-53); ein Bulk-Endpunkt wäre zusätzliche API-Fläche ohne echten
   Zusatznutzen bei den hier relevanten Größenordnungen (bis zu einigen hundert sequenziellen
   `POST`-Aufrufen einmalig beim Plan-Anlegen). Kosten: keine – bestehender Code bleibt unangetastet.

## Akzeptanzkriterien

1. Schritt „Übungen" des Lehrplan-Assistenten bietet zusätzlich zu Fach/Klasse/Schulart/Suchtext eine
   optionale Kategorie- und eine optionale Übungstyp-Auswahl; beide AND-verknüpft wie beim Server, beide
   leer lassbar („alle").
2. `GET creator/exercises` akzeptiert einen optionalen `source`-Parameter (Substring, wie `search`) und
   filtert danach; ohne den Parameter ändert sich das bisherige Verhalten nicht.
3. Der Assistent bietet ein optionales Quelle-Suchfeld, das den neuen Parameter nutzt.
4. Zeigt der Filter mehr Treffer als geladen (`total > shown`), übernimmt „Alle wählen" bis zu 500 Treffer
   (statt der bisherigen 100); bleiben darüber hinaus Treffer offen, macht `TruncationHint` das weiterhin
   sichtbar.
5. Der bestehende Anlegeweg (ein Kind, ein Plan, eine Position je gewählter Übung, Wiederaufnahme nach
   Fehler) bleibt unverändert – kein neuer Endpunkt, keine neue Schleifenlogik.
6. `ExerciseMetadataTests` deckt den neuen `source`-Filter mit einem Treffer- und einem Nichttreffer-Fall
   ab.

## Schätzung

**Größe: S** – ein additiver Backend-Parameter (kein Schema, kein Vertragsbruch) plus drei
Formular-Ergänzungen an einer bestehenden, bereits arbeitenden Oberfläche; vergleichbar mit dem
S-Anker `childId` aus dem Test-Pfad ziehen (B-01), eher am oberen Rand von S wegen der drei
unabhängigen Teilstellen.

- **`wo`:** beides (kleiner Backend-Teil zuerst, dann die Wizard-Erweiterung).
- **`migration`:** nein – `Exercise.Source` existiert bereits als Spalte, es wird nur ein zusätzlicher
  Filter darauf gebaut.
- **`vertragsbruch`:** nein – ein neuer optionaler Query-Parameter an einem bestehenden `GET`-Endpunkt ist
  additiv; bestehende Aufrufe (Client, Frontend, Agenten) bleiben unverändert gültig.
- **Risiken:** Ein Fach ohne Kategorien zeigt ein leeres Dropdown – muss wie die übrigen optionalen Filter
  "alle" als Vorgabe tragen, sonst wirkt das Feld kaputt. Ein `take=500`-Request lädt spürbar mehr Zeilen
  auf einen Schlag – akzeptabel, weil er nur beim expliziten „Alle wählen" bei großer Trefferzahl feuert,
  nicht bei jedem Tastendruck. Parallelität mit [B-58](B-58-assistent-e2e.md) (derselbe Screen bekommt dort
  seinen ersten E2E-Durchstich): keine inhaltliche Kollision, aber Merge-Reihenfolge beachten, falls beide
  Stories zeitgleich in Arbeit sind.
- **Angriffsplan:** Erst Backend (`source`-Parameter in `ExerciseCatalogController.Search` +
  `ExerciseMetadataTests`-Fall), danach Frontend (`ExerciseSearchParams`/`api.searchExercises` um `source`
  ergänzen, Wizard-Schritt „Übungen" um Kategorie/Typ/Quelle-Felder erweitern, „Alle wählen" auf
  bedarfsweises `take=500` umstellen).
- **Testweg:** Backend über `ExerciseMetadataTests` (neuer `source`-Fall, analog zum bestehenden
  `search`-Fall). Frontend über einen Komponententest der neuen Wizard-Filterfelder (React Testing
  Library, analog zu bestehenden Formular-Tests) – der volle Wizard-E2E-Durchstich ist bewusst nicht Teil
  dieser Story, sondern Gegenstand von [B-58](B-58-assistent-e2e.md). Vor der Abnahme zusätzlich ein
  gezielter `/smoke-test`-Durchlauf des Assistenten mit gesetztem `source`-Filter.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-08-03** — ausformuliert: Ist-Stand gegen den echten Code belegt
  (`ExerciseCatalogController.Search`, `VaterWizard.tsx`, `wizardFinish.ts`, `PlanPositionsController`);
  die Recherche zeigt, dass der Generator als `/vater/wizard` bereits existiert und die Lücke schmaler ist
  als die Idee unterstellte.
- **2026-08-03** — gegrillt: alle offenen Punkte autonom in nummerierte Entscheidungen überführt (autonom
  getroffen, Nutzerauftrag 2026-08-04).
- **2026-08-03** — geschätzt: Größe S, `wo: beides`, `migration: nein`, `vertragsbruch: nein`,
  Angriffsplan und Testweg festgelegt (autonom getroffen, Nutzerauftrag 2026-08-04).
