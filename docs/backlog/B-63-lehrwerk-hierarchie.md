---
tags: [typ/story, status/ausformuliert, bereich/katalog, bereich/frontend, rolle/creator, rolle/supervisor]
aliases: [Lehrwerk-Hierarchie, Verlag-Reihe-Band-Unit]
status: ausformuliert
prio: P2
art: Wunsch
quelle: remark #2, #3, #4, #5, #6, #7 (+ #10 zweite Hälfte)
---

# B-63 · Das Lehrwerk ist eine Ebene aus Freitext, gebraucht wird eine Hierarchie mit Listen

Sechs Anmerkungen aus **einer** Testsitzung an derselben Seite (`/vater/lehrwerke`) sagen dasselbe aus
verschiedenen Richtungen. Die Häufung ist das Signal: das Lehrwerk trägt heute eine flache Ebene mit
Freitextfeldern, erwartet wird eine Hierarchie mit kontrollierten Listen.

## User Story

Als Vater möchte ich ein Lehrwerk als **Verlag → Fach → Reihe → Band → Units** anlegen und dabei aus
gepflegten Listen wählen statt zu tippen, damit dieselbe Reihe wiederverwendbar bleibt, statt in fünf
Schreibweisen zu zerfallen.

## Ist-Stand am Code

**Struktur** — Band und Unit liegen **bewusst** auf einer Ebene:

> „Band und Unit liegen bewusst auf **einer** Ebene (`Grade` = Band): ‚Access 8, Unit 3' ist eine Zeile,
> kein zweistufiger Baum."
> — [CurriculumEntities.cs:44-47](../../backend/Pugling.Api/Models/CurriculumEntities.cs), dieselbe Aussage
> in [backend/Pugling.Api/CLAUDE.md](../../backend/Pugling.Api/CLAUDE.md) → „Unterrichtsmaterial &
> Creator-Profile".

- `TextbookSeries.Publisher` ist ein `string?` (`CurriculumEntities.cs:22`) — der Verlag ist **keine Ebene**.
- `TextbookSeries.SubjectId` ist nullable (`:26`) — Fach → Reihe ist eine optionale Verknüpfung, keine
  Hierarchie.
- `SeriesUnit.Grade` trägt den Band (`:57`), `SeriesUnit.OrderIndex` die Reihenfolge darin (`:59`).

**Freitext, wo Listen erwartet werden:**

- `SeriesUnit.Topics`, `.Grammar`, `.VocabularyNotes` sind je ein `string?`
  (`CurriculumEntities.cs:63,65,67`); im UI je ein einzeiliges Feld bzw. `textarea`
  ([VaterLehrwerke.tsx:273-292](../../frontend/src/vater/VaterLehrwerke.tsx)).
- Eine **Grammatik-Entität existiert nicht** — „Grammar" im Backend ist ausschließlich der Übungstyp
  ([BuiltInExerciseTypes.cs:52](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs),
  [ExerciseConfigs.cs:112](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs),
  [ExerciseControllers.cs:343](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs)).
- `TextbookSeries.SourceLanguage`/`.TargetLanguage` sind `string?` (`CurriculumEntities.cs:31,33`), im UI
  Freitext ([VaterLehrwerke.tsx:375,379](../../frontend/src/vater/VaterLehrwerke.tsx)).

**Formular und Liste:**

- Reihenfolge im Anlege-Formular: Reihe, Verlag, Fach, Schulart, Lernsprache, Muttersprache, Notiz
  ([VaterLehrwerke.tsx:352-384](../../frontend/src/vater/VaterLehrwerke.tsx)). Fach (`:361`) und Schulart
  (`:368`) sind bereits Pulldowns.
- Übersichtstabelle: Reihe · Fach · Schulart · Units (`:56`) — **kein Band**; der steht nur in der
  aufgeklappten Unit-Tabelle (`:152` Kopf, `:158` Wert).
- Suche kennt `search` (Name/Verlag), `subjectId` und `mineOnly`
  ([TextbookSeriesController.cs:48-50](../../backend/Pugling.Api/Controllers/Creator/TextbookSeriesController.cs)) —
  weder Schulart noch Verlag noch Band sind filterbar.

**Was mit hängt:**

- `Textbook.SeriesId`/`CurrentUnitId` am Kind zeigt auf **Reihe + Unit**
  ([AdminEntities.cs:168-175](../../backend/Pugling.Api/Models/AdminEntities.cs)).
- `CreatorProfile.SeriesId` (`CurriculumEntities.cs:98`) und das Matching-Gewicht „Reihe 8"
  (`CreatorProfileService`, siehe [backend/Pugling.Api/CLAUDE.md](../../backend/Pugling.Api/CLAUDE.md)).
- Eine `TextbookSeries → SeriesUnit`-Kette ist genau zweistufig (`CurriculumEntities.cs:41,51`) — weitere
  Bücher derselben Reihe (Lernbuch, Übungsbuch) kennt das Modell nicht.

## Die echte Lücke

Nicht „eine Ebene fehlt", sondern: **der geteilte Katalog ist auf Wiederverwendung ausgelegt, seine Felder
sind es nicht.** Der Slug macht die Reihe idempotent (`CurriculumEntities.cs:19-20`), aber Verlag, Sprachen,
Themen und Grammatik sind Freitext — zwei Väter, die dieselbe Reihe beschreiben, erzeugen zwei
Beschreibungen, und nichts daran ist verknüpfbar.

Bemerkenswert: **den Band gibt es schon** — auf der Kind-Seite (`Textbook.Grade`,
[AdminEntities.cs:161](../../backend/Pugling.Api/Models/AdminEntities.cs)), nur nicht im geteilten Katalog.

Die Reihenfolge ergibt sich damit von selbst: **#7 ist der Träger.** #6 wird erst danach eindeutig (heute
kann eine Reihe mehrere Bände tragen, eine Spalte müsste aggregieren), #2 braucht die Verlag-Ebene, und
die Anmerkungen #3, #4 und #5 sind derselbe Schnitt eine Ebene tiefer. Einzeln gebaut wären das drei
Migrationen an derselben Tabelle.

## Offene Punkte

1. **Wird die dokumentierte Entscheidung wirklich umgekehrt?** Die eine Ebene ist ausdrücklich begründet
   (`CurriculumEntities.cs:44-47`). Der Nutzer hat sie in Kenntnis der Oberfläche in Frage gestellt.
   *Empfehlung: ja* — der Grund für „eine Ebene" war Einfachheit, nicht Fachlichkeit, und die Reibung ist
   jetzt sechsfach belegt. Kosten: `Textbook`- und `CreatorProfile`-Verträge ziehen nach.
2. **Verlag als eigene Tabelle oder als kontrollierte Liste?** *Empfehlung: eigene Entität mit Slug*, Muster
   `TextbookSeries`/`InterestTag` — nur dann können abhängige Pulldowns („nur Verlage, die Englisch
   anbieten") überhaupt entstehen.
3. **Grammatik-Themen: geteilte Taxonomie oder pro Reihe?** *Empfehlung: geteilt und slug-idempotent* wie
   `VocabTag` — der Nutzer nennt ausdrücklich die sprachübergreifende Wiederverwendung als Ziel.
4. **Themen der Unit: Array von Freitext oder eigene Entität?** *Empfehlung: Freitext-Array* — Themen sind
   buchspezifisch („Reading a Tube map"), Grammatik nicht.
5. **Sprachen: feste Liste oder ISO-Codes?** Berührt [B-17](B-17-birkenbihl-sprachcodes.md)
   (`gb` vs. `en`) — beides zusammen entscheiden.
6. **Weitere Buchtypen derselben Reihe** (Lernbuch, Übungsbuch) — eigene Ebene unter dem Band oder ein
   Typ-Feld am Band? *Empfehlung: Typ-Feld*, eine Ebene weniger.
7. **Zweite Hälfte von Anmerkung #10:** sollen Klassenstufe und Schulart vom `Exercise` ans `Chapter`
   wandern? Heute sind sie **nicht** redundant, weil `Chapter` nur `Name`+`OrderIndex` trägt
   ([LearnEntities.cs:35-44](../../backend/Pugling.Api/Models/LearnEntities.cs)) und `Subject` nur den Namen
   (`:8-18`). *Empfehlung: zurückstellen* — es ist ein eigener Umbau am Übungs-Katalog, nicht am Lehrwerk.
8. **Ist das eine Karte statt einer Grill-Runde?** Sieben offene Punkte, die voneinander abhängen
   (2 entscheidet über 3, 1 über alles). *Empfehlung: erst Punkt 1 grillen* — fällt er negativ aus, sind
   die übrigen sechs gegenstandslos.

## Akzeptanzkriterien

> Entwurf — final erst nach dem Grillen, Punkt 1 entscheidet über den Zuschnitt.

1. Ein Lehrwerk lässt sich als Verlag → Fach → Reihe → Band → Unit anlegen; jede Ebene ist wiederverwendbar.
2. Verlag, Reihe und die Sprachen sind Auswahlfelder; die Auswahl baut aufeinander auf und kennt „default".
3. Themen und Grammatik einer Unit sind Listen; Grammatik-Werte sind über eine stabile Id
   reihen**übergreifend** wiederverwendbar.
4. Die Lehrwerk-Übersicht zeigt den Band.
5. Die Suche filtert mindestens nach Fach, Schulart, Verlag und Band.
6. `Textbook` am Kind und `CreatorProfile` bleiben funktionsfähig; das Fachlehrer-Matching liefert für
   denselben Datenstand dieselbe Empfehlung.

## Verlauf

- **2026-08-02** — angelegt aus den Anmerkungen #2, #3, #4, #5, #6 und #7 (#10 als Punkt 7); Ist-Stand am
  Code belegt, Befund:
  [befund-2026-08-02.md](../anmerkungen/befund-2026-08-02.md#a--lehrwerk-modell--der-größte-block-2-3-4-5-6-7).
