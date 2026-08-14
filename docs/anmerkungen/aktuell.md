# Anmerkungen – Export

Stand: 2026-08-14 21:23 UTC · 2 Einträge · Filter: alle

> Erzeugt von `GET api/v1/remarks/export`. **Nicht von Hand bearbeiten** – die Quelle ist
> die Datenbank. Status und Antworten ändert der Skill `anmerkungen` über die API.

## #1 · Ui · erledigt

- **Erfasst:** 2026-08-14 20:48 UTC von Konto 1 (Supervisor)
- **Wo:** `/vater/lehrwerke` (vater)

Die Suche der Lehrwerke soll auch nach Fach selektiert werden

**Antwort** (claude-code, 2026-08-14 20:52 UTC):

Backend und Client koennen es schon, nur der Screen bietet es nicht an. Backend: TextbookSeriesController.List nimmt [FromQuery] int? subjectId (:63) und filtert damit (:85). Client: api.textbookSeries reicht subjectId durch (api.ts:754 Signatur, :759 Query-Param). Die Luecke sitzt allein in VaterLehrwerke.tsx: :57-68 haelt kein subjectId-State und uebergibt es nicht an den Aufruf, die Filterzeile :87-104 hat Verlag/Schulart/Klassenstufe, aber kein Fach. Die Faecherliste ist im selben Screen bereits geladen (:69, api.subjects()) und die Ergebnistabelle zeigt Fach sogar als Spalte (:110) - man sieht das Fach also, kann aber nicht danach filtern. Zusatzbefund: der Freitext deckt das Fach bewusst NICHT ab (Controller :80-83 sucht nur Name, Slug und Verlagsname), Eintippen des Fachnamens hilft also auch nicht; der Platzhalter sagt das mit Suche in Name oder Verlag korrekt. Aufwand: ein State, ein select, eine Zeile im Aufruf - reine Frontend-Ergaenzung ohne Vertragsaenderung.

**Verlauf** (1):

> **claude-code** · 2026-08-14 21:03 UTC, Konto 1
>
> Gebaut. VaterLehrwerke.tsx: Fach-Select in die Filterzeile (vor dem Verlag, weil das die Facette ist, nach der gesucht wird), subjectId-State, und die Abbildung Formular->Abfrage als exportierte reine Funktion seriesFilterQuery herausgezogen. Die Faecherliste war im Screen schon geladen, es kam keine Anfrage dazu. Drei Faelle in VaterLehrwerke.test.tsx, beide Richtungen rot geprobt: ohne die Durchreichung faellt der Durchreich-Fall, mit Number() ohne Leer-Pruefung faellt der Leer-Fall (subjectId=0 statt weglassen). Warum der Guard noetig ist, ist an der laufenden Instanz gemessen: subjectId=0 liefert 0 Treffer, die Liste waere also grundlos leer. Filter selbst verifiziert: Fach 1 Englisch -> 2 Reihen, Fach 2 Mathe -> 1, ohne Filter -> 5. 283/283 Vitest gruen, npm run build gruen. Der Freitext deckt das Fach weiterhin nicht ab - das blieb absichtlich, der Platzhalter sagt es.

## #2 · Content · erledigt

- **Erfasst:** 2026-08-14 21:00 UTC von Konto 1 (Supervisor)
- **Wo:** `/vater/lehrwerke` (vater)

Bei Unit kann man BAnd und Buchtyp festlegen, diese Information gehört aber zu lehrwerk. Zu Unit

**Antwort** (claude-code, 2026-08-14 21:23 UTC):

Kein Defekt, sondern eine bewusst dokumentierte Entscheidung - aber es ist die ZWEITE Meldung derselben Sache, und das aendert die Beweislage. Am Code bestaetigt: TextbookSeries traegt kein Grade und kein BookType (CurriculumEntities.cs:29-57), SeriesUnit traegt beide (:73 Grade, :79 BookType). Eine Band-Ebene gibt es also nicht, und genau das ist gewollt: B-63 hat die Frage gegrillt und die Ein-Ebenen-Entscheidung ausdruecklich NICHT aufgehoben (Punkt 1 der Entscheidungen) - Grade IST der Band, Access 8 Unit 3 ist eine Zeile. Damalige Begruendung: keine der sechs Anmerkungen nannte einen Bedarf an bandeigenen Metadaten, der Schmerz sei Sichtbarkeit und Filterbarkeit gewesen; aufgehoben werden sollte die Entscheidung nur dort, wo sie WIEDERVERWENDUNG verhindert. Vorgeschichte: die archivierte Anmerkung #7 vom 2026-08-02 sagte woertlich Band muss aus Unit raus und in Lehrwerk ziehen, ging als quelle in B-63 ein, und B-63 steht auf abgenommen. Neu gegenueber damals sind zwei Dinge: der Buchtyp (#7 nannte nur den Band) und dass das Argument auf Eigentum und Wiederverwendung zielt - also auf die Ausnahme, die B-63 selbst formuliert hat. KORREKTUR meiner ersten Fassung dieser Antwort: dort stand, die Buchidentitaet werde bei zwoelf Units zwoelfmal getippt. Das war zu stark. VaterLehrwerke.tsx submit behaelt Band und Buchtyp nach dem Anlegen bereits absichtlich (es setzt nur Bezeichnung, Themen, Grammatik und Wortschatz zurueck), aufeinanderfolgende Units erben sie also schon. Die echte Luecke war enger: das ERSTE Formular nach dem Aufklappen einer Reihe startete leer und beim Regelfall Lehrbuch, obwohl die Reihe ihre Baende laengst kennt. Genau das ist jetzt behoben (siehe Verlauf). Die Modellfrage selbst bleibt offen und unveraendert entschieden - eine echte Volume-Ebene waere Migration, Vertragstyp und UI-Ebene, und B-63s Nein dazu steht. Hinweis: der Text der Anmerkung bricht nach Zu Unit mitten im Satz ab, die zweite Haelfte fehlt.

**Verlauf** (1):

> **claude-code** · 2026-08-14 21:23 UTC, Konto 1
>
> Gebaut - die billige Haelfte, ohne Schemaeingriff. Neue Funktion newUnitDefaults(units) in VaterLehrwerke.tsx belegt eine neue Unit mit Band und Buchtyp der LETZTEN Unit der Reihe vor; die letzte ist die richtige Vorlage, weil der Server nach Band sortiert (SeriesUnitsController.cs:42). Zwei Fallstricke dabei gefunden und behandelt: (1) Die Fallunterscheidung haengt am Vorhandensein von unit, nicht an seinen Werten - sonst erbte eine BESTEHENDE Unit ohne Band beim blossen Oeffnen den Band der Nachbarin, ein stiller Datenwechsel. Rot geprobt: mit der wertbasierten Variante faellt der Fall (erwartet leer, gemessen 8). (2) Das Anlege-Formular haengt jetzt an units.data !== null, weil useState seine Vorbelegung nur beim Einhaengen liest - stand es schon waehrend des Ladens da, startete es leer und die nachgelieferte Vorbelegung kam nie an. Vier Faelle in VaterLehrwerke.test.tsx, 287/287 Vitest gruen, npm run build gruen. NICHT gebaut: die Band-Ebene im Modell. B-63s Ein-Ebenen-Entscheidung steht unveraendert.
