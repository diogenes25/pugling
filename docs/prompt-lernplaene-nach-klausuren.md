---
tags: [typ/tutorial, bereich/lehrplan]
---

# Prompt: Lernpläne nach anstehenden Klausuren ausrichten

Dieser Prompt richtet die Lernpläne für Theodor und Friedrich an ihren nächsten Klausuren aus. Er ist für eine
**frische Sitzung** geschrieben: Alles, was sie wissen muss, steht hier oder ist verlinkt. Den Text unter
„Prompt“ als erste Nachricht in die neue Sitzung kopieren.

Stand der Fakten: **2026-11-18**. Ältere Fakten gelten nur, bis die Sitzung sie gegen WebUntis und die DB
geprüft hat.

## Prompt

```text
Richte die Lernpläne von Theodor (Child 1) und Friedrich (Child 3) in Pugling an ihren nächsten Klausuren aus.
Ziel: Jeder Prüfungsstoff ist rechtzeitig tägliche Pflicht, alles andere Kür, und die Tageslast bleibt
spielbar. Arbeite die Schritte unten der Reihe nach ab und halte an den mit HALT markierten Stellen an.

Lies zuerst:
- docs/prompt-lernplaene-nach-klausuren.md (diese Datei: Ausgangsstand, Regeln, offene Fragen)
- die Memory-Notizen familie-theodor-friedrich, plaene-13-bis-16, dosis-zaehlt-karten,
  friedrich-vokabeltest-2026-11-24, friedrich-vokabeltest-format, theodor-vokabeltest-format,
  friedrich-motivation-belohnungen, heimnetz-echte-db-lebt, testphase-echte-kinder
- als Vorlagen die drei Skripte vom 18.11.: scripts/latein-l4-l6-2026-11-18.ps1,
  scripts/friedrich-termine-2026-11-18.ps1, scripts/franzoesisch-u2-u8-2026-11-18.ps1

Schritt 1 – Schulstand holen. Starte scripts/webuntis-abruf.ps1 per run_in_terminal (Friedrich mit den
Vorgaben, Theodor mit -Server avs.webuntis.com -School avs). Die Zugänge tippe ich selbst, du gibst nie
Zugangsdaten ein. Werte die JSON-Dateien aus %TEMP%\pugling-webuntis aus: Prüfungen (exams), Hausaufgaben der
letzten 4 Wochen und der kommenden Tage, Fächer laut Stundenplan. Die AVS gibt meinem Elternkonto keine
Prüfungen frei (403); Theodors Termine kommen dann von mir (siehe offene Fragen).

Schritt 2 – Pugling-Stand lesen, nur an einer Kopie: backend/Pugling.Api/pugling.db (plus -wal) in den
Scratchpad kopieren und dort per sqlite lesen: aktive StudyPlans je Kind, PlanPositions (Cadence,
NewItemsPerDay, GoalThreshold, PenaltyCoins), Klassenarbeiten mit zugeordneten Übungen, Textbooks
(CurrentUnit) und die Übungen je SeriesUnit. Lernstand je Position (ItemProgress/PositionItemProgress), um zu
sehen, was ein Kind schon kann.

Schritt 3 – Abgleich. Je Kind und Prüfung: Datum, Stoff (aus Prüfungstext, Hausaufgaben, Buchseiten), welche
Übungen in Pugling diesen Stoff tragen, was fehlt (Übungen, Fotos, Wortlisten), ob eine Klassenarbeit
eingetragen ist. Stoff, den es in Pugling nicht gibt, nicht erfinden: als Lücke melden.

HALT 1 – Lege mir eine Tabelle je Kind vor: Prüfung · Datum · Stoff · Pugling-Übungen · Lücke. Darunter die
offenen Fragen, die nur ich beantworten kann, je mit deiner Empfehlung. Warte auf meine Antworten.

Schritt 4 – Planvorschlag nach den Regeln in der Datei (Abschnitt „Regeln für die Pläne“): je Kind ein
Wochenfahrplan bis zur letzten Klausur vor Weihnachten – was ist ab wann tägliche Pflicht, was Kür, was
Wochenpflicht als Generalprobe, wann wird nach einer Klausur zurückgebaut. Mit Tageslast je Kind (Pflichten
und neue Wörter pro Tag).

HALT 2 – Vorschlag vorlegen, auf mein Ja warten.

Schritt 5 – Umsetzen wie die Vorlagen: ein Einmal-Skript je Änderungspaket unter scripts/ (UTF-8 mit BOM,
#requires -Version 7, PIN per Read-Host -MaskInput, Sicherung der DB vor dem ersten Schreiben, Schutz gegen
zweimaliges Ausführen, nur über die API, nichts löschen). Erst an einer DB-Kopie testen (Konto 1 dort auf
PinHash '0000' setzen, API mit ASPNETCORE_ENVIRONMENT=Development auf einem freien Port, z. B. 5281), Ergebnis
zeigen, dann per run_in_terminal gegen http://127.0.0.1:5200 starten; die PIN tippe ich. Danach den Stand an
einer frischen Kopie prüfen, nicht nur die Skriptausgabe lesen.

Schritt 6 – Abschluss: Memory-Notiz plaene-13-bis-16 (oder eine neue) nachführen, Termine für spätere
Umstellungen (z. B. „nach dem 24.11. zurückbauen“) als offene Punkte nennen. Skripte nicht ohne mein Ja
committen.
```

## Ausgangsstand am 2026-11-18

### Theodor (Child 1, Klasse 7, Auguste-Viktoria-Schule Flensburg)

- **Englisch**, Plan 13 „Englisch: Unit 1 – Lernkarte und Test-Nacharbeit“, Buch Access G9 Band 3 (Reihe 5).
  Tägliche Pflicht: Unit 1 Part A (Pos. 99) und Part B (100); wöchentlich die Nacharbeit zum Test vom 9.11.
  (101). **Part C (102) und Text (103) sind nur Kür.** Die Schule hat „Vocab Unit 1, part C (the rest!)“ zum
  23.11. aufgegeben. In Pugling steht Klassenarbeit 1 „Vokabeltest Unit 1“ am **20.11.** (Part A–C und Text) –
  Datum ist nicht bestätigt. Für Unit 2–5 gibt es keine Übungen und keine Vokabelfotos.
- **Latein**, Plan 14 „Latein: Lektion 4–6 – Wörter mit Lernkarte“, Buch VIVA 1 (Reihe 7), Stand Lektion 5.
  Tägliche Pflicht: Wörter L5 (Pos. 193) und L4 (194); L1–3, L6 und die Grammatik sind Kür. Die Schule hat
  L5 bis 17.11. gelernt, L6 kommt als Nächstes. Wortlisten bis L6 liegen in der App; der Lernwortschatz bis L17
  ist abfotografiert (Anhang ab S. 118, Seitendateien unter raw/latin/viva-1-pugling/md).
- **Mathe**: kein Plan, Schule arbeitet mit Bettermarks. Klassenarbeit 2 „Mathe-Arbeit Einmaleins“ (3.11.,
  Note 4,5) ist vermutlich Testdatenrest.
- **Prüfungstermine** der AVS sind über WebUntis nicht sichtbar.

### Friedrich (Child 3, Klasse 9, Altes Gymnasium Flensburg)

- **Französisch**, Plan 15 „Französisch: Unité 1 – Wörter mit Lernkarte“, Buch Découvertes Série bleue 2
  (Reihe 6), Stand Unité 5. Tägliche Pflicht nur noch die Generalprobe für den **Petit contrôle am 24.11.**:
  Unité 5 Découvertes (Pos. 165, täglich; 167, wöchentlich) und Atelier A (166; 168), Stoff S. 187 bis
  *dire*. Kür: Unité 1–4, Unité 5 Atelier B (202), Grammatik. **Klassenarbeit („Contrôle“) am 8.12.**, Thema
  laut WebUntis offen; zugeordnet sind Unité 5 Découvertes, Atelier A, Atelier B und die U5-Grammatik.
  Wortlisten für alle Unités 1–8 liegen in der App.
- **Englisch**, Plan 16 „Englisch: Unit 1 Australia – Wörter mit Lernkarte“, Buch Access G9 Band 5 (Reihe 8).
  Tägliche Pflicht nur Part A (133); Part B/C und Grammatik Kür. **Klassenarbeit am 4.12.**, Thema offen
  (Unit 1 angenommen). Für Unit 2–4 gibt es je 7 Übungen im Katalog.
- **Mathe**: Klassenarbeit am **16.12.**, ohne Übungen; Hausaufgaben zuletzt Lambacher Schweizer 9, S. 73–82.
  Abfotografiert sind nur S. 102–133.
- Ohne Fach in Pugling: Geschichte (2.12.), Chemie (7.12.), Ästhetik (23.11.).

## Regeln für die Pläne

- **Ein aktiver Plan je Kind und Fach.** Bestehende Pläne umbauen statt neue anlegen: Fortschritt hängt an der
  Position, ein neuer Plan verliert ihn.
- **Dosis zählt Karten:** `newItemsPerDay = 4` heißt bei „beide Richtungen“ zwei neue Wörter am Tag.
- **Tageslast begrenzen:** höchstens etwa vier tägliche Pflichten je Kind über alle Fächer. Pläne mit sieben
  oder acht Tagespflichten wurden kaum gespielt.
- **Prüfungsstoff wird tägliche Pflicht,** spätestens zwei Wochen vor der Klausur. In der letzten Woche davor
  eine Wochenpflicht als Generalprobe ohne Malus (Muster: Pos. 167/168).
- **Nach der Klausur zurückbauen:** Generalprobe auf Kür, den nächsten Stoff zur Pflicht.
- **Pflicht** wie in den Vorlagen: Stufe 4, Ziel 80 %, getippter Test, Leitner, 20 Münzen, 2 Münzen Malus.
  **Kür:** gleiche Lernkarte, kein Ziel, kein Malus. Lückentexte als Nacharbeit (`remediation`).
- **Klassenarbeiten** mit Fach und Datum eintragen und die Übungen des Stoffs zuordnen.
- Beim Laden von Buchdaten: `buch.json` → `kind` ohne `klassenarbeit`. Sonst benennt `laden.py` die erste
  geplante Klassenarbeit des Fachs um.

## Offene Fragen an den Vater

1. Theodor: Gilt der Vokabeltest am 20.11.? Welche Klassenarbeiten hat er bis Weihnachten (Englisch, Latein,
   Mathe), mit Datum und Stoff?
2. Friedrich: Welcher Stoff kommt in Englisch am 4.12. (Unit 1 oder auch Unit 2) und in Französisch am 8.12.
   (nur Unité 5 oder auch Unité 6)?
3. Mathe Friedrich: Fotos von Lambacher Schweizer 9, S. 70–90, damit Übungen entstehen können?
4. Kleinere Fotos: Découvertes S. 180 neu (unscharf); Access Band 3 Vokabelteil ab Unit 2 für Theodor.
