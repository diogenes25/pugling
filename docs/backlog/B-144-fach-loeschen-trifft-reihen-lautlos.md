---
tags: [typ/story, status/gegrillt, bereich/beides, bereich/katalog, rolle/creator]
aliases: [Fach löschen loescht Kind-Daten, Cascade auf KeyResult, SubjectInUse]
status: gegrillt
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-137
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-144 · Ein Fach zu löschen löscht Meilensteine und Stundenpläne des Kindes

Abgespalten von [B-137](B-137-freitext-fach-unerreichbar.md) (dessen Punkt 2). Das ist die **Ursache**
des Zustands, den [B-143](B-143-formular-kennt-zustaende-des-modells-nicht.md) nicht anzeigen kann.

## User Story

Als **Creator** möchte ich, dass mir das Löschen eines Fachs nicht die Ziele und Stundenpläne eines
Kindes mitnimmt — und dass ich bei den harmloseren Folgen wenigstens vorher erfahre, welche Zuordnungen
ich zerreiße.

## Ist-Stand am Code

> **Beim Grillen am 2026-08-10 nachgemessen — und die ursprüngliche Fassung dieses Abschnitts war zu
> klein.** Sie nannte nur `TextbookSeries` und beschrieb die Folge als verwaisten Anzeigenamen. Tatsächlich
> zeigen **acht** Beziehungen auf `Subject`, und drei davon **löschen Zeilen**.

`Controllers/Creator/SubjectsController.cs:79-86` — `Delete` prüft **nichts**. Was daran hängt
(`Data/PuglingDbContext.cs`, jede Zeile einzeln nachgesehen):

| Beziehung | Verhalten | Folge |
| --- | --- | --- |
| `TextbookSeries`, `Textbook`, `CreatorProfile`, `StudyPlan`, `Klassenarbeit` | `SetNull` | Zuordnung weg, Zeile bleibt |
| `ExerciseCategory` (`:473`) | **Cascade** | Kategorien werden gelöscht — der zweite Sprung ist harmlos: `Exercise.CategoryId` ist `SetNull` (`:481`, *„does NOT delete the exercise"*) |
| **`KeyResult`** (`:653`) | **Cascade** | **Meilensteine von Kind-Zielen werden gelöscht.** `KeyResult.SubjectId` ist *pflichtig* (`ObjectiveEntities.cs:59`), und an erreichten Etappen hängt eine Auszahlung (`ObjectiveReward`) |
| **`TimetableEntry`** (`:722`) | **Cascade** | Stundenplan-Einträge des Kindes werden gelöscht |

**Die Methodendoku untertreibt ihre eigene Reichweite.** Sie sagt, das Löschen betreffe „textbook series
(SetNull) and exercises pointing at one of its categories" — von `KeyResult` und `TimetableEntry` steht
dort nichts.

`frontend/src/vater/CatalogAdmin.tsx:86-88` bietet das Löschen an mit dem Text *„Lehrwerk-Reihen und
Übungen behalten ihren Inhalt, verlieren aber die Zuordnung zu diesem Fach."* — nicht falsch, aber
unvollständig, und über die drei Cascades schweigt er.

Zum Vergleich: Das Repo kennt die Nutzungssperre bereits, fünfmal — `DeleteBehavior.Restrict` im Modell
**plus** Vorprüfung im Controller mit eigenem Code (`SeriesUnitsController.cs:149`,
`TextbookSeriesController.cs:271`, je `ApiErrors.ExerciseInUse`). Beim Fach gibt es sie nicht.

Verwandt, aber **nicht dasselbe**: [B-127](B-127-verlag-loeschen-trifft-fremde.md) fragt, ob das Löschen
eines *Verlags* fremde Konten treffen darf. Dort ist die Frage die Reichweite über Eigentumsgrenzen
hinweg; hier ist sie, was der Löschende überhaupt zerstört.

## Die echte Lücke

Nicht „ein verwaister Fachname entsteht lautlos" — das ist die **harmloseste** der Wirkungen, und sie hat
die Story zu klein gerahmt. Die Lücke ist, dass **ein Katalog-Handgriff Kind-Daten löscht**: Meilensteine,
an denen eine Auszahlung hing, und einen von Hand getippten Stundenplan. Ohne Warnung, ohne Weg zurück,
und ohne dass die Doku der Methode es erwähnt.

`SetNull` bleibt dabei vertretbar und gewollt. `Cascade` auf Kind-Daten ist es nicht.

## Offene Punkte

1. ~~**Warnen oder verweigern?**~~ → Entscheidungen 1 und 2: **beides**, je nach Löschverhalten.
2. ~~**Woher kommt die Zahl?**~~ → Entscheidungen 4 und 5: **keine Zahl**.
3. ~~**Gilt dasselbe für `CreatorProfile` und `Textbook`?**~~ → erhoben: beide sind `SetNull`
   (`PuglingDbContext.cs:198,272`), gehören also in die Warn-Gruppe, nicht in die sperrende. Die Frage
   war zu eng gestellt — sie übersah die drei `Cascade`-Beziehungen, um die es eigentlich geht.

## Entscheidungen

1. **Zwei Entscheidungen statt einer, entlang des Löschverhaltens.** Für `SetNull` bleibt es bei
   „warnen, nicht verweigern": Der Verlust ist eine Zuordnung, sichtbar und in einem Klick reparierbar,
   und ein blockiertes Löschen wäre schlimmer — es gäbe keinen Weg mehr, ein Fach loszuwerden, ohne
   vorher jede Reihe umzuhängen. Für `Cascade` reicht das nicht: Eine gelöschte Etappe ist kein
   Anzeigewert, sondern ein Stück Lernstand, an dem eine Auszahlung hing, und es gibt keinen Weg zurück.
   **Kosten:** Die Regel ist nicht mehr in einem Satz erklärbar; wer das Löschen anfasst, muss beide
   Hälften kennen.
2. **Die Linie läuft an „gehört die Zeile einem Kind?" — nicht an einer Liste von Namen.** Sperrend sind
   damit `KeyResult` und `TimetableEntry`; `ExerciseCategory` sperrt nicht, weil sie katalog-intern ist,
   eine Kategorie ohne ihr Fach bedeutungslos wäre und ihr eigener zweiter Sprung bereits `SetNull` ist —
   es geht nichts verloren außer einer Beschriftung. Begründung für die Regelform: Sie sagt dem Nächsten,
   was mit einer **neunten** Beziehung zu tun ist; eine Namensliste veraltet still. Und die Unterscheidung
   ist im Projekt ohnehin die tragende — der Katalog ist geteilt, Kind-Daten gehören dem Kind.
   **Kosten:** Ein Fach, das je in einem Kind-Ziel oder Stundenplan vorkam, lässt sich erst löschen, wenn
   diese Zeilen weg sind — bei langer Historie faktisch „nie". Aufräumen wird unbequemer.
3. **Das volle Muster: `Restrict` im Modell *plus* Vorprüfung im Controller *plus* ein neuer Code
   `SubjectInUse`** (Namensmuster `exercise_in_use`, `vocabulary_in_use`). Begründung, drei Teile: es ist
   die etablierte Form dieses Repos (fünf `Restrict`, jedes mit Vorprüfung); die Vorprüfung allein wäre
   das Einzige zwischen den Meilensteinen eines Kindes und ihrer Löschung, und
   [B-145](B-145-fach-umbenennen-laesst-namen-stehen.md) hat gerade gezeigt, dass Schreibwege am
   Controller vorbei existieren (`Seed.cs`); und `Restrict` allein gäbe einen `500` mit halb
   gespeichertem Zustand statt eines lesbaren `409` — genau darum schreibt `backend/CLAUDE.md` beides vor.
   **Kosten:** `migration: ja`. Die Kette wird neu gefaltet (Länge bleibt 1, `SchemaGuardTests` hält das),
   der Snapshot-Diff muss genau die beabsichtigten Zeilen zeigen. Die Story wächst dadurch von `S` auf `M`.
4. **Keine Zahl in der Warnung — stattdessen wird der Bestätigungstext vollständig.** Er nennt künftig
   alle fünf `SetNull`-Betroffenen statt nur „Lehrwerk-Reihen und Übungen". Begründung: Die Zahl hätte
   eine eigene Verwendungs-Route gekostet *und* die Bestätigung asynchron gemacht — `confirmAction`
   (`frontend/src/lib/ui.ts`) ist ein synchroner `window.confirm`. Das ist ein Umbau an der Löschstelle
   für eine Information, die die Entscheidung des Vaters nicht ändert. **Kosten:** Er erfährt *was*
   betroffen ist, nicht *wie viel*. Wer das Ausmaß wissen will, muss selbst nachsehen.
5. **Auch der `409` nennt, was sperrt, ohne Zahl** — „wird in Zielen oder Stundenplänen eines Kindes
   verwendet", nicht „in 3 Meilensteinen". Begründung: dieselbe Linie wie Entscheidung 4, und die Zahl
   hilft nicht — löschbar wird das Fach dadurch nicht. Nebeneffekt: Die Vorprüfung bleibt ein `AnyAsync`,
   das abbricht, sobald es eine Zeile findet, statt zweier `CountAsync`. **Kosten:** Wer wissen will,
   *welche* Ziele betroffen sind, sieht auf der Kind-Seite nach — ein Schritt mehr.

## Akzeptanzkriterien

1. Ein Fach, auf das ein `KeyResult` oder ein `TimetableEntry` zeigt, lässt sich **nicht** löschen:
   `409 subject_in_use`, und das `detail` nennt die Art der Verwendung ohne Zahl.
2. Ein Fach ohne solche Verweise lässt sich löschen wie bisher — auch wenn Lehrwerk-Reihen, Lehrbücher,
   Fachlehrer-Profile, Lehrpläne oder Klassenarbeiten darauf zeigen.
3. Das Löschverhalten von `KeyResult.SubjectId` und `TimetableEntry.SubjectId` steht im Modell auf
   `Restrict`; `ExerciseCategory` bleibt `Cascade`.
4. Die Migrationskette bleibt bei Länge 1 (`SchemaGuardTests`), und der Snapshot-Diff zeigt genau die
   beiden beabsichtigten Änderungen.
5. Der Bestätigungstext in `CatalogAdmin.tsx` nennt alle fünf Zuordnungen, die verloren gehen — und
   behauptet nicht mehr, das Löschen sei folgenlos.
6. Die Methodendoku von `SubjectsController.Delete` sagt ihre tatsächliche Reichweite.
7. Je ein Integrationstest, vorher rot: gesperrt bei `KeyResult`, gesperrt bei `TimetableEntry`,
   löschbar bei einer bloßen `TextbookSeries`-Zuordnung.

## Verlauf

- **2026-08-10** — abgespalten von [B-137](B-137-freitext-fach-unerreichbar.md) im Nachtlauf (Sprint 2),
  weil B-137 faktisch XL war. Als eigene Story und nicht als Teil von
  [B-143](B-143-formular-kennt-zustaende-des-modells-nicht.md), obwohl sie die Ursache desselben Zustands
  ist: B-143 ist reine Oberfläche, diese hier braucht einen Endpunkt. Und B-143 bleibt nötig, selbst wenn
  diese Story nie gebaut wird — verwaiste Namen gibt es im Bestand bereits.
- **2026-08-10** — **gegrillt** im Dialog mit dem Nutzer (fünf Entscheidungen). Die Runde hat die Story
  **vergrößert statt verkleinert**: Die Messung fand acht Beziehungen auf `Subject` statt einer, drei davon
  `Cascade` — darunter `KeyResult` (Meilensteine mit Auszahlung, `SubjectId` pflichtig) und
  `TimetableEntry`. Titel, Aliasse, Ist-Stand und „echte Lücke" sind entsprechend korrigiert; der Begriff
  „verwaister Fachname" beschrieb die harmloseste Wirkung und hatte die Story zu klein gerahmt.
  Aus `S` ohne Migration wird damit voraussichtlich `M` mit `migration: ja` — zu bestätigen beim Schätzen.
