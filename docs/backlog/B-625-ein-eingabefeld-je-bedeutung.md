---
tags: [typ/story, status/ausformuliert, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [Eingabefeld je Bedeutung, mehrere Übersetzungen getrennt eingeben]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17 (Erfahrung aus der Testphase)"
grund: ""
ersetzt_durch: []
---

# B-625 · Ein Eintrag mit mehreren Formen oder Bedeutungen bekommt je Teil ein eigenes Eingabefeld

Hat ein Begriff mehrere Übersetzungen, muss der Lernende heute nach Erfahrung des Vaters alle in **einem**
Feld eingeben, samt Komma: zu *run* also „rennen, laufen". Gewünscht ist: so viele Eingabefelder, wie es
Übersetzungen gibt – „rennen" und „laufen" getrennt.

## User Story

Als **Lernender** möchte ich bei einem Eintrag mit mehreren Formen oder Bedeutungen jede in ein eigenes Feld
tippen, statt den ganzen Eintrag samt Schrägstrichen und Kommas in eine Kästchenreihe zu zwängen, damit ich
das Wort lerne und nicht die Zeichensetzung der Wortliste.

## Ist-Stand am Code

Recherchiert am 2026-11-17 gegen `11fed646`.

- **Freitext lässt schon eine einzelne Bedeutung gelten.** [B-462](B-462-mehrdeutige-uebersetzung-verlangt-alle-bedeutungen.md):
  `AnswerVariants.Of` zerlegt einen Eintrag an `;`, ` / ` und `,` außerhalb von Klammern
  (`backend/Pugling.Api/Services/Shared/AnswerVariants.cs:5`–`:12`, `:30`–`:53`), eingeschaltet für jedes
  Vokabel-Atom in beide Richtungen (`backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs:133`–`:135`).
  Wer im Freitext nur „quel“ tippt, hat recht.
- **Die Buchstabenkästchen verlangen den ganzen Eintrag.** Die Maske entsteht Zeichen für Zeichen aus der ganzen
  Lösung, jedes Nicht-Buchstaben-Zeichen ist fest (`backend/Pugling.Api/Services/Shared/StageMechanics.cs:96`–`:97`,
  `LetterBoxPattern`). Aus „quel / quelle / quels / quelles“ wird eine Reihe mit allen vier Formen, die Trenner
  stehen fest; das Kind muss jede Form tippen. Die Karte trägt **eine** Maske (`AnswerPattern`,
  `backend/Pugling.Contracts/Student/PracticeDtos.cs:67`, `:100`), die Sohn-App rendert **eine** Reihe
  (`frontend/src/sohn/Lektion.tsx:173`, `frontend/src/components/LetterBoxes.tsx:4`–`:19`). Das ist die Stelle,
  die der Vater meint – B-462 hat sie bewusst ausgenommen („Nicht Teil dieser Story: Buchstabenkästchen“).
- **Buchstabensalat** gibt es nur für ein einzelnes Wort bis 12 Zeichen (`StageMechanics.cs:99`–`:108`) – ein
  mehrteiliger Eintrag wird nie Salat, sondern Kästchen.
- **Getippt wird die Fremdsprache** ([B-597](B-597-tippstufen-fragen-die-zielsprache-ab.md)). Mehrteilig auf der
  *getippten* Seite ist also die Wort-Seite des Speichers, nicht die deutsche. Das Beispiel des Vaters
  („run“ → „rennen, laufen“) tritt in dieser Richtung heute nicht mehr auf; die deutsche Seite ist Frage.
- **Gemessen** an einer Kopie der Sicherung `pugling.db.bak-auto-20261117-124225` (nur lesend): 265 Vokabeln
  hängen an Übungen, davon **22 mit mehrteiliger Wort-Seite** und 93 mit mehrteiliger deutscher Seite. Die 22
  sind vor allem Friedrichs Französisch – Formen eines Worts statt Synonyme:
  „quel / quelle / quels / quelles“, „un mono / une mono“, „connaître qn / qc“, „sévère / sévère“ (männlich =
  weiblich), „la 5e(A) / la cinquième (A)“. An „quel, quelle, quels, quelles“ scheiterte Friedrich achtmal in
  Folge ([B-536](B-536-aufbau-uebungen-statt-versagen.md)).

## Die echte Lücke

Nur die Kästchen-Stufe (und mit ihr der Test auf dieser Stufe) presst einen mehrteiligen Eintrag in eine Reihe.
Es fehlen: eine Zerlegung der **getippten** Seite in Teile für die Anzeige (B-462 zerlegt nur zum Vergleichen),
eine Karte mit **mehreren** Masken, eine Bewertung je Feld und eine Antwort, die mehrere Teile trägt. Offen ist
vor allem die Produktfrage, ob dann **alle** Teile verlangt werden – im Freitext genügt heute einer.

## Offene Punkte

1. **Alle Teile oder einer?** Empfehlung: **alle** – je Feld ein Teil. Bei den gemessenen Fällen sind es
   Formen eines Worts (quel/quelle/…, un/une), und genau die soll das Kind schreiben können. Kosten: Die Karte
   wird schwerer als heute im Freitext; B-462/1 („eine genügt“) gilt dann nur noch für den Freitext.
2. **Welche Stufen?** Empfehlung: **Kästchen und Freitext** gleich (je Teil ein Feld), dazu Diktat. Sonst
   verlangt die eine Stufe alle Formen und die nächste nur eine. Kosten: Freitext wird für mehrteilige Einträge
   strenger als heute.
3. **Reihenfolge.** Empfehlung: **beliebig** – jedes Feld darf jeden noch nicht getroffenen Teil treffen.
   Kosten: Bei Formen (quel → quelle) verliert die Reihenfolge ihre Ordnung; die Felder tragen keine Beschriftung.
4. **Gleiche Teile.** „sévère / sévère“ ist männlich/weiblich gleich. Empfehlung: **gleiche Teile zu einem
   Feld zusammenfassen**. Kosten: keine sichtbaren.
5. **Teilweise richtig.** Drei von vier Formen richtig – was zählt? Empfehlung: Die Karte ist nur richtig, wenn
   **alle** Felder stimmen; das Blatt zeigt je Feld richtig/falsch. Kosten: hart bei vier Formen; eine
   Abstufung (halb wie in [B-624](B-624-akzentfehler-nicht-ganzer-fehler.md)) wäre denkbar.
6. **Was ist ein Teil?** Empfehlung: **dieselben Trenner wie B-462** (` / `, `;`, `,` außerhalb von Klammern,
   nicht in Sätzen); Klammerteile bleiben im Feld und dürfen fehlen. Kosten: „connaître qn / qc“ wird zwei
   Felder „connaître qn“ und „qc“ – die Heuristik trennt dort falsch und braucht einen Sonderfall oder die
   Pflege des Eintrags.
7. **Code oder Datenpflege?** Empfehlung: **Code**, wie B-462/1 (der Import zerlegt nicht). Alternative wäre,
   die 22 Einträge im Speicher in einzelne Vokabeln zu teilen. Kosten: die Heuristik aus Punkt 6.
8. **Im Abschlusstest auch?** Empfehlung: **ja**, nach der Regel von B-597/5 (überall gleich). Kosten: Tests
   mit solchen Einträgen werden schwerer.

## Akzeptanzkriterien (Entwurf)

1. Eine Karte zu „quel / quelle / quels / quelles“ zeigt auf der Kästchenstufe vier Kästchenreihen, je eine
   Form, ohne feste Trenner zwischen ihnen.
2. Die Antwort trägt die Teile einzeln; die Karte ist richtig, wenn jeder Teil genau einmal getroffen ist,
   Reihenfolge beliebig.
3. „sévère / sévère“ zeigt ein Feld.
4. Ein einteiliger Eintrag verhält sich wie heute (ein Feld, eine Maske).
5. Im Freitext und im Abschlusstest gilt dieselbe Zerlegung.
6. Das Blatt zeigt je Feld, ob es stimmt.

## Verlauf

- **2026-11-17** — angelegt aus dem Chat mit dem Vater.
- **2026-11-17** — ausformuliert: Ursache sind die Buchstabenkästchen (eine Maske über den ganzen Eintrag);
  an der DB-Kopie 22 mehrteilige Einträge auf der getippten Seite gemessen, fast alle französische Formen;
  acht offene Punkte mit Empfehlung.
