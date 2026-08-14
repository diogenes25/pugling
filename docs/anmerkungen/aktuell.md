# Anmerkungen – Export

Stand: 2026-08-14 21:03 UTC · 2 Einträge · Filter: alle

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

## #2 · ohne Einordnung · offen

- **Erfasst:** 2026-08-14 21:00 UTC von Konto 1 (Supervisor)
- **Wo:** `/vater/lehrwerke` (vater)

Bei Unit kann man BAnd und Buchtyp festlegen, diese Information gehört aber zu lehrwerk. Zu Unit
