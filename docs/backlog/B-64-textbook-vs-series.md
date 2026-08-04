---
tags: [typ/story, status/geschaetzt, bereich/katalog, rolle/supervisor, rolle/creator]
aliases: [Textbook vs. TextbookSeries, Lehrwerk zweimal]
status: geschaetzt
prio: P3
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: remark #9
---

# B-64 · Das Lehrwerk gibt es zweimal: einmal als Freitext am Kind, einmal katalogisiert

## User Story

Als Vater möchte ich das Buch meines Kindes **einmal** pflegen und dabei auf den geteilten Katalog zeigen,
damit ich nicht denselben Titel zweimal in zwei Schreibweisen führe.

## Ist-Stand am Code

Die Anmerkung vermutete eine Dopplung zwischen **Fach** und **Lehrwerk**. Die Prüfung zeigt: dort ist keine
— aber eine Ebene weiter schon.

**Fach ≠ Lehrwerk** (keine Dopplung):

- `Subject → Chapter → Exercise` trägt die **Übungen**: `Exercise.ChapterId` ist nicht nullable
  ([LearnEntities.cs:54](../../backend/Pugling.Api/Models/LearnEntities.cs)).
- `TextbookSeries → SeriesUnit` trägt den **Stoff** (`Topics`/`Grammar`/`VocabularyNotes`,
  [CurriculumEntities.cs:62-67](../../backend/Pugling.Api/Models/CurriculumEntities.cs)) und **keine einzige
  Übung** — an einer Unit hängt kein Inhalt, nur eine Notiz.
- Verbunden sind beide über `TextbookSeries.SubjectId` (`CurriculumEntities.cs:26`).

Ein Vater kann über ein Lehrwerk also *keinen* Katalog erstellen — das war die Annahme der Anmerkung.

**Die echte Dopplung:**

- `Textbook` — das Buch **am Kind**: `Title`, `SubjectName`/`SubjectId`, `Grade`, `Publisher`, `Isbn`,
  `CurrentChapter`, alles Freitext
  ([AdminEntities.cs:146-165](../../backend/Pugling.Api/Models/AdminEntities.cs)).
- `TextbookSeries` — dieselben Angaben, katalogisiert und geteilt: `Name`, `Slug`, `Publisher`,
  `SubjectName`/`SubjectId` ([CurriculumEntities.cs:16-27](../../backend/Pugling.Api/Models/CurriculumEntities.cs)).
- Verbunden sind sie über `Textbook.SeriesId`/`CurrentUnitId`
  ([AdminEntities.cs:171,178](../../backend/Pugling.Api/Models/AdminEntities.cs)), **optional** — Titel und
  `CurrentChapter` bleiben ausdrücklich Rückfallebene für unkatalogisierte Werke (Zeilenangabe gegen den
  heutigen Code nachgezählt und leicht korrigiert gegenüber der Erstfassung, die 168-175 nannte).

**Was der Nutzer im Lehrwerk vermisst,** existiert im Katalog: `ExerciseCategory` je Fach
([LearnEntities.cs:25-32](../../backend/Pugling.Api/Models/LearnEntities.cs)), verwaltet unter
`/vater/katalog` ([VaterKatalog.tsx:22](../../frontend/src/vater/VaterKatalog.tsx)) — eine kontrollierte
Liste, also genau das Muster, das [B-63](B-63-lehrwerk-hierarchie.md) für Themen und Grammatik fordert.

**Das aktuelle Formular** ([ChildMaterialSection.tsx](../../frontend/src/vater/ChildMaterialSection.tsx),
`TextbookForm`) bestätigt die Lücke unverändert: Titel, Fach, Klasse, Verlag, Reihe und Unit stehen als
**gleichrangige** Felder eines einzigen `form-grid` nebeneinander (Zeilen 189–245) — die Reihen-Auswahl trägt
keinen Vorrang, „– nicht katalogisiert –" ist nur die erste von mehreren Optionen im selben Pulldown. Verlag
(`form.publisher`) und Klasse (`form.grade`) werden unabhängig von einer gewählten Reihe immer als Freitext
gespeichert (`TextbooksController.cs:72,112,114`) — die vermutete Dopplung ist im Code **live**, nicht
theoretisch.

**Stand von [B-63](B-63-lehrwerk-hierarchie.md):** B-63 ist selbst erst `geschaetzt`, noch nicht gebaut —
`TextbookSeries.Publisher` ist heute (nachgeprüft, `CurriculumEntities.cs:22`) weiterhin ein einfacher
`string?`, keine `PublisherId`-FK. B-63s Entscheidung 2 plant genau diesen Wechsel; ihre Entscheidung 10
weist selbst darauf hin, dass das **zwei** Kandidatenfelder für „doppelte Wahrheit" hinterlässt (Verlag
*und* Band), nicht nur eines. Diese Story rechnet unten mit dem **geplanten** Zielzustand von B-63
(Publisher als eigene Entität), nicht mit dem heutigen Freitext-Feld — siehe Entscheidung 4.

**Korrektur gegenüber der Erstfassung:** Der Satz „`Textbook.Grade` trägt bereits den Band, den B-63 im
Katalog vermisst" war ungenau. Nachgeprüft gegen B-63s eigenen Ist-Stand und ihre Entscheidung 1: die
Band-Ebene existiert im Katalog **bereits heute** als `SeriesUnit.Grade`
([CurriculumEntities.cs:57](../../backend/Pugling.Api/Models/CurriculumEntities.cs)) und wird durch B-63
**nicht** neu eingeführt — B-63 hebt die Ein-Ebenen-Entscheidung Band+Unit ausdrücklich nicht auf. Was B-63
am Band ändert, ist allein die **Sichtbarkeit/Filterbarkeit** in der Übersicht (B-63 Entscheidung 7), nicht
die Existenz des Feldes. Für diese Story heißt das: die Dopplung bei Band (`Textbook.Grade` vs.
`SeriesUnit.Grade` der gewählten `CurrentUnit`) besteht **schon heute**, unabhängig von B-63 — B-63 verändert
nur die Verlags-Dopplung (Freitext → Entität).

## Die echte Lücke

Die Rückfallebene ist kein Versehen, sondern gewollt: ein unkatalogisiertes Buch soll eintragbar bleiben.
Die Lücke ist, dass **nichts das Katalogisieren nahelegt** — beide Wege stehen gleichberechtigt
nebeneinander, und der billigere (tippen) gewinnt. Das kostet genau da, wo der Katalog seinen Wert hat: das
Fachlehrer-Matching zählt „Reihe" mit Gewicht 8, greift aber nur bei gesetzter `SeriesId`
(`CreatorProfileService.cs:18,72`, nachgeprüft).

## Offene Punkte

1. ~~Aufheben oder überbrücken?~~ → siehe Entscheidung 1.
2. ~~Soll das Anlegen eines `Textbook` die Reihe *erzeugen* können?~~ → siehe Entscheidung 2.
3. ~~Was passiert mit `Textbook.Grade`/`Publisher`, wenn eine Reihe gewählt ist?~~ → siehe Entscheidung 3.
4. ~~Reihenfolge zu B-63: danach oder parallel?~~ → siehe Entscheidung 4.

## Entscheidungen

1. **Aufheben oder überbrücken? Überbrücken — mit einer echten Weiche, nicht einem gleichrangigen
   Pulldown-Eintrag.** `Textbook` bleibt bestehen (Freitext-Fallback ist Produktentscheidung, kein Versehen),
   aber `TextbookForm` bekommt eine Reihenfolge mit Vorrang: zuerst „Reihe wählen" (inkl. Inline-Anlage,
   Entscheidung 2), Freitext ist eine ausdrücklich benannte, sichtbare Ausweichoption dahinter statt einer
   von sechs gleichwertigen Zeilen im selben `form-grid`. Begründung: „gleichberechtigt nebeneinander, der
   billigere gewinnt" (Die echte Lücke) ist ein UI-Problem, kein Datenmodell-Problem — das Modell erlaubt die
   Rückfallebene schon korrekt (`SeriesId`/`CurrentUnitId` optional). Aufheben hieße, unkatalogisierte Bücher
   zu verbieten, was Akzeptanzkriterium 3 widerspräche und im Katalog-Bau von B-63 keine Stütze findet (dort
   bleibt derselbe Freitext-Fallback-Gedanke für `Textbook` unangetastet, siehe B-63 Entscheidung 10). Kosten:
   Umbau von `ChildMaterialSection.tsx`/`TextbookForm` (Layout, kein neues Feld) — reines Frontend, kein
   Vertragsbruch.
2. **Ja, das Anlegen eines `Textbook` kann die Reihe erzeugen.** Der Slug macht das idempotent
   (`CurriculumEntities.cs:19-20`), `api.createTextbookSeries` existiert bereits und wird schon aus dem
   Vater-Web heraus gegen die Creator-Route aufgerufen (`ChildMaterialSection.tsx:33`, `VaterLehrwerke.tsx:330`)
   — kein neues Rechte-Loch, denn jeder Account mit Supervisor-Rolle trägt laut `AccountService.cs:46-47`
   immer auch die Creator-Rolle. Begründung: sonst muss der Vater die Seite wechseln (`/vater/lehrwerke`),
   und genau dann tippt er wieder Freitext statt zu katalogisieren. Kosten: ein neues, bewusst **minimales**
   Inline-Formular (nur Name, optional das am `Textbook`-Formular schon gewählte Fach vorausgefüllt) statt
   des vollen `VaterLehrwerke.tsx`-Formulars (Verlag/Schulart/Sprachen/Notiz) — wer mehr braucht, pflegt es
   unter „Lehrwerke" nach. Kein neuer Endpunkt, kein Vertragsbruch.
3. **Doppelte Wahrheit bei Verlag *und* Band: ableiten statt speichern, nicht duplizieren — Anzeige, keine
   Löschung.** Bei gesetzter `SeriesId` zeigt die API den Verlag aus dem Katalog, bei gesetzter
   `CurrentUnitId` den Band aus der Unit, statt den gespeicherten Freitextwert unverändert weiterzugeben —
   serverseitig in `TextbooksController.Project` berechnet (`Series.Publisher`/`.PublisherId`-Auflösung nach
   B-63, `CurrentUnit.Grade`). Der gespeicherte Freitextwert wird dabei **nicht gelöscht**: er bleibt stille
   Rückfallebene, falls der Vater die Verknüpfung später wieder löst (`ClearSeries`/`ClearUnit`) — sonst wäre
   das Entkoppeln ein Datenverlust, den nichts in den Akzeptanzkriterien fordert. `SeriesUnit.Grade` kann
   `null` sein (Unit ohne Band); dann bleibt der gespeicherte `Textbook.Grade` die einzige Angabe und wird
   nicht überschrieben — sonst ginge eine vorhandene Information ohne Ersatz verloren. Begründung: löst
   Akzeptanzkriterium 4 minimal-invasiv, ohne neues Contract-Feld — Frontend und jeder andere Konsument
   erkennt „katalogisiert" bereits über `seriesId`/`currentUnitId` im bestehenden `TextbookResponse`. Kosten:
   eine erweiterte `Project`-Projektion (LINQ, kein Schema), Frontend zeigt Verlag/Band bei bestehender
   Verknüpfung als Text mit Katalog-Hinweis statt als editierbares Feld.
4. **Reihenfolge zu B-63: danach — jetzt aus zwei Gründen statt einem.** B-63 ist noch nicht gebaut
   (`TextbookSeries.Publisher` ist heute noch `string?`, nachgeprüft). Würde B-64 vorher gebaut, müsste die
   Verlags-Ableitung aus Entscheidung 3 zunächst gegen den Freitext-String geschrieben und nach B-63 auf
   `PublisherId` umgeschrieben werden — doppelte Arbeit an derselben Stelle. Der zweite Grund kam erst durch
   diese Grill-Runde dazu: die Band-Ableitung (`SeriesUnit.Grade`) hängt **nicht** an B-63 (siehe Korrektur
   oben) und wäre für sich genommen unabhängig baubar — aber weil Entscheidung 3 Verlag und Band im selben
   Zug (dieselbe `Project`-Projektion, dasselbe Formular) löst, lohnt sich eine Aufspaltung nicht. Kosten:
   B-64 wartet auf B-63s Umsetzung (nicht nur auf ihre Schätzung).
5. **Bestätigung: keine Überlappung mit B-63 — eigene Recherche deckt sich mit B-63 Entscheidung 10.** B-63
   baut die **innere Struktur** des geteilten Katalogs um (Verlag als Entität, Themen als Liste, Buchtyp,
   Filter/Aggregation); diese Story klärt die **Brücke** zwischen dem Freitext-Feld am Kind (`Textbook`) und
   der katalogisierten Reihe (`TextbookSeries`) — unterschiedliche Dateien (`CurriculumEntities.cs` vs.
   `AdminEntities.cs`/`TextbooksController.cs`/`ChildMaterialSection.tsx`), unterschiedliche Endpunkte
   (`creator/textbook-series` vs. `supervisor/children/{}/textbooks`), unterschiedliche Nutzerfrage („wie
   pflege ich den Katalog" vs. „welchen Weg nehme ich beim Anlegen des Kind-Buchs"). Kein Duplikat. Einzige
   reale Kopplung: die Reihenfolge (Entscheidung 4) und der Umstand, dass B-63s Feldtypwechsel den
   Beleg-Text dieser Story (Entscheidung 3) direkt betrifft — genau das, was B-63 Entscheidung 10 selbst
   vorausgesagt hat. Kosten: keine, reine Bestätigung.

## Akzeptanzkriterien

1. Beim Hinterlegen des Buchs am Kind ist die katalogisierte Reihe der Vorschlagsweg: sie steht im Formular
   zuerst und mit sichtbarem Vorrang, Freitext ist eine ausdrücklich benannte Ausweichoption dahinter — kein
   gleichrangiges Nebeneinander mehr im selben Feldraster.
2. Eine noch nicht katalogisierte Reihe lässt sich direkt aus diesem Formular heraus anlegen (idempotent über
   den Slug, minimaler Umfang: Name + optional Fach).
3. Ein unkatalogisiertes Werk bleibt vollständig eintragbar — kein Zwang zum Katalog.
4. Bei gesetzter `SeriesId` zeigt die Oberfläche für den Verlag den Katalogwert, bei gesetzter
   `CurrentUnitId` für den Band den Katalogwert — nicht mehr den separat editierbaren Freitextwert. Fehlt der
   Katalogwert (z. B. Unit ohne Band), bleibt der gespeicherte Freitextwert sichtbar.
5. Löst der Vater die Verknüpfung (`ClearSeries`/`ClearUnit`) wieder, geht der zuletzt bekannte
   Freitextwert für Verlag/Band **nicht** verloren, sondern taucht wieder auf — kein stiller Datenverlust
   beim Entkoppeln.

## Schätzung

**Größe: M** — kein neues Schema, kein neuer Endpunkt, aber drei reale Arbeitspakete über beide Schichten:
eine abgeleitete Anzeige-Logik im Backend (mit Sonderfall „Band fehlt"), eine Formular-Restrukturierung im
Frontend (Vorrang + Inline-Anlage) und die zugehörigen Tests — vergleichbar mit dem vokabel-basierten
Batch-Pfad im `MediaSelector` (B-03), deutlich mehr als eine lokalisierte Ein-Punkt-Änderung wie B-01.

- **`wo: beides`** — Backend zuerst (Ableitungs-Logik in `TextbooksController.Project`), danach das
  Formular; Reihenfolge-Abhängigkeit zu B-63 kommt **davor** (siehe Angriffsplan).
- **`migration: nein`** — keine neue Spalte, keine neue Tabelle; `Textbook.Grade`/`.Publisher` bleiben
  unverändert im Schema, nur ihre Anzeige wird bei bestehender Katalog-Verknüpfung überlagert.
- **`vertragsbruch: nein`** — `TextbookResponse` behält Typ und Feldnamen; „katalogisiert oder nicht" liest
  das Frontend weiterhin aus den bestehenden Feldern `seriesId`/`currentUnitId`. Kein additives Feld nötig.
- **Risiken:**
  - **Reihenfolge-Abhängigkeit zu B-63 ist real** (Entscheidung 4): B-64 darf erst nach B-63s Umsetzung
    beginnen, sonst fällt die Verlags-Ableitung doppelt an.
  - `SeriesUnit.Grade` kann `null` sein — die Ableitung darf den vorhandenen `Textbook.Grade` dann nicht
    überschreiben (Entscheidung 3); ohne Testfall dafür ist das ein stiller Informationsverlust.
  - Inline-Anlage ruft die Creator-Route aus einem Supervisor-Bildschirm auf — nach heutigem Rollenmodell
    unproblematisch (Entscheidung 2), aber bei künftigen Rollenänderungen (z. B. einem reinen
    Supervisor-Konto ohne Creator-Rolle) gegenprüfen.
  - Kein bestehender E2E-Test deckt `ChildMaterialSection`/`TextbookForm` ab (recherchiert: kein Treffer
    unter `frontend/e2e/`) — die Formular-Restrukturierung hat also keinen Regressionsschutz von außen, bis
    ein neuer Test steht (siehe Testweg).
- **Angriffsplan** (Backend zuerst, nach B-63):
  1. Voraussetzung: B-63 ist gebaut (`TextbookSeries.PublisherId` existiert).
  2. `TextbooksController.Project`: Verlag ableiten (`SeriesId` gesetzt → Katalog-Verlag, sonst
     `book.Publisher`), Band ableiten (`CurrentUnitId` gesetzt **und** `Grade` dort nicht `null` → Katalog-
     Band, sonst `book.Grade`).
  3. Backend-Tests in `StudentProfileTests.cs` erweitern (siehe Testweg).
  4. Frontend: `TextbookForm` umbauen — Reihe zuerst mit Inline-Anlage („+ Neue Reihe", minimales Formular),
     Freitext-Ausweiche danach; Verlag-/Band-Feld wird bei bestehender Verknüpfung als Text mit
     Katalog-Hinweis dargestellt statt als Eingabefeld.
  5. Neuer oder erweiterter E2E-Fall für den Rundgang Reihe wählen/anlegen → Buch verknüpfen →
     Verlag/Band-Anzeige prüfen → Entkoppeln → Freitextwert taucht wieder auf.
  6. `/smoke-test` vor dem Commit.
- **Testweg:**
  - Backend: `StudentProfileTests.Vater_KannLehrbuch_Anlegen_Lesen_Aendern_Loeschen` (bestehend) um Fälle
    mit gesetzter `SeriesId`/`CurrentUnitId` erweitern (Katalogwert überlagert Freitext), plus einen neuen
    Fall „Unit ohne Band" (Freitext-Rückfall) und einen Fall „Entkoppeln erhält den Freitextwert"
    (Akzeptanzkriterium 5) — alle in derselben Datei, gleiches Muster.
  - Frontend/E2E: kein bestehender Spec betroffen; ein neuer Fall gehört in `e2e/lehrwerke.spec.ts` (das
    B-63 ohnehin anfasst) oder einen eigenen `textbook-anlegen.spec.ts`; `/smoke-test` vor dem Commit für
    den End-to-End-Rundgang.

## Verlauf

- **2026-08-02** — angelegt aus Anmerkung #9; Ist-Stand am Code belegt, Befund:
  [befund-2026-08-02.md](../anmerkungen/befund-2026-08-02.md#b--die-zweite-wahrheit-über-das-lehrwerk-9).
- **2026-08-04** — gegrillt: alle vier offenen Punkte in fünf nummerierte Entscheidungen überführt
  (überbrücken statt aufheben mit echter Formular-Weiche, Inline-Erzeugung der Reihe minimal statt vollem
  Formular, Verlag- und Band-Ableitung mit Anzeige statt Löschung inkl. Null-Band-Fallback, Reihenfolge nach
  B-63 aus zwei Gründen statt einem, Überlappung mit B-63 anhand des heutigen Codes bestätigt statt
  angenommen); jeder `Datei:Zeile`-Beleg gegen den heutigen Code nachgeprüft, dabei eine ungenaue
  Zeilenangabe korrigiert und eine unzutreffende Aussage zur Band-Ebene aus der Erstfassung richtiggestellt
  (autonom getroffen, Nutzerauftrag).
- **2026-08-04** — geschätzt: Größe M, `wo: beides`, `migration: nein`, `vertragsbruch: nein`; Risiken,
  Angriffsplan (Backend zuerst, mit realer Reihenfolge-Abhängigkeit zu B-63) und Testweg ergänzt (autonom
  getroffen, Nutzerauftrag).
