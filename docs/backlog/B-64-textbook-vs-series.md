---
tags: [typ/story, status/ausformuliert, bereich/katalog, rolle/supervisor, rolle/creator]
aliases: [Textbook vs. TextbookSeries, Lehrwerk zweimal]
status: ausformuliert
prio: P3
art: Wunsch
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
- Verbunden sind sie über `Textbook.SeriesId`/`CurrentUnitId` (`AdminEntities.cs:168-175`), **optional** —
  Titel und `CurrentChapter` bleiben ausdrücklich Rückfallebene für unkatalogisierte Werke.

**Was der Nutzer im Lehrwerk vermisst,** existiert im Katalog: `ExerciseCategory` je Fach
([LearnEntities.cs:25-32](../../backend/Pugling.Api/Models/LearnEntities.cs)), verwaltet unter
`/vater/katalog` ([VaterKatalog.tsx:22](../../frontend/src/vater/VaterKatalog.tsx)) — eine kontrollierte
Liste, also genau das Muster, das [B-63](B-63-lehrwerk-hierarchie.md) für Themen und Grammatik fordert.

## Die echte Lücke

Die Rückfallebene ist kein Versehen, sondern gewollt: ein unkatalogisiertes Buch soll eintragbar bleiben.
Die Lücke ist, dass **nichts das Katalogisieren nahelegt** — beide Wege stehen gleichberechtigt
nebeneinander, und der billigere (tippen) gewinnt. Das kostet genau da, wo der Katalog seinen Wert hat: das
Fachlehrer-Matching zählt „Reihe" mit Gewicht 8, greift aber nur bei gesetzter `SeriesId`.

Auffällig: `Textbook.Grade` trägt bereits den **Band**, den [B-63](B-63-lehrwerk-hierarchie.md) im Katalog
vermisst.

## Offene Punkte

1. **Aufheben oder überbrücken?** *Empfehlung: überbrücken* — `Textbook` bleibt, aber das Formular führt
   zuerst in den Katalog („Reihe wählen") und bietet Freitext nur als bewusste Ausweichoption an.
   Aufheben hieße, unkatalogisierte Bücher zu verbieten.
2. **Soll das Anlegen eines `Textbook` die Reihe *erzeugen* können?** Der Slug macht das idempotent
   (`CurriculumEntities.cs:19-20`), das Muster gibt es schon. *Empfehlung: ja* — sonst muss der Vater die
   Seite wechseln, und genau dann tippt er.
3. **Was passiert mit `Textbook.Grade`/`Publisher`, wenn eine Reihe gewählt ist?** Doppelte Wahrheit.
   *Empfehlung: bei gesetzter `SeriesId` aus dem Katalog anzeigen statt speichern* — hängt an
   [B-63](B-63-lehrwerk-hierarchie.md) Punkt 1 (ob es dort eine Band-Ebene gibt).
4. **Reihenfolge zu B-63:** danach oder parallel? *Empfehlung: danach* — Punkt 3 hat sonst keine Antwort.

## Akzeptanzkriterien

1. Beim Hinterlegen des Buchs am Kind ist die katalogisierte Reihe der Vorschlagsweg, Freitext die Ausnahme.
2. Eine noch nicht katalogisierte Reihe lässt sich aus diesem Formular heraus anlegen (idempotent über den
   Slug).
3. Ein unkatalogisiertes Werk bleibt eintragbar — kein Zwang zum Katalog.
4. Bei gesetzter `SeriesId` gibt es für Verlag und Band **eine** Wahrheit, nicht zwei.

## Verlauf

- **2026-08-02** — angelegt aus Anmerkung #9; Ist-Stand am Code belegt, Befund:
  [befund-2026-08-02.md](../anmerkungen/befund-2026-08-02.md#b--die-zweite-wahrheit-über-das-lehrwerk-9).
