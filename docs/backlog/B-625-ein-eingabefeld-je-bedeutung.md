---
tags: [typ/story, status/gegrillt, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [Eingabefeld je Bedeutung, mehrere Übersetzungen getrennt eingeben]
status: gegrillt
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

Alle in der Grill-Runde vom 2026-11-17 entschieden – siehe Entscheidungen.

1. ~~**Alle Teile oder einer?**~~ → Entscheidung 1.
2. ~~**Welche Stufen?**~~ → Entscheidung 2.
3. ~~**Reihenfolge.**~~ → Entscheidung 4.
4. ~~**Gleiche Teile.**~~ → Entscheidung 5.
5. ~~**Teilweise richtig.**~~ → Entscheidung 3 (abweichend vom Entwurf: halb statt ganz oder gar nicht).
6. ~~**Was ist ein Teil?**~~ → Entscheidung 6.
7. ~~**Code oder Datenpflege?**~~ → Entscheidung 7.
8. ~~**Im Abschlusstest auch?**~~ → Entscheidung 2.

## Entscheidungen

Grill-Runde mit dem Vater, 2026-11-17. Alle nach Empfehlung.

1. **Alle Teile werden verlangt, je Feld einer.** Die mehrteiligen Einträge auf der getippten Seite sind fast
   alle Formen eines Worts; wer „quelles“ nicht schreiben kann, kann das Wort nicht. *Kosten:* Diese Karten
   werden schwerer als heute im Freitext; B-462/1 („eine genügt“) gilt für zerlegbare Einträge auf
   Tipp-Stufen nicht mehr.
2. **Überall, wo getippt wird:** Kästchen, Freitext und Diktat, in Üben, Abschlusstest, Bonus-Training und
   Vorschau – die Regel von B-597/5. *Kosten:* Freitext wird für diese Einträge strenger; vier Oberflächen
   (Lektion, Test, Bonus, Vorschau) brauchen mehrere Felder.
3. **Teilweise richtig zählt halb** – mit dem Ausgang aus [B-624](B-624-akzentfehler-nicht-ganzer-fehler.md)
   (Kasten bleibt, keine Münzen, Combo hält), wenn **mindestens die Hälfte** der Felder stimmt; darunter falsch.
   Das Blatt zeigt je Feld ✔/✘. Im Abschlusstest bleibt es binär (B-624/1). *Kosten:* **B-625 hängt an B-624**,
   das zuerst gebaut wird; eine Schwelle (Hälfte) als Produktregel im Code.
4. **Reihenfolge beliebig:** Jedes Feld darf jeden noch nicht getroffenen Teil treffen. *Kosten:* Die Felder
   tragen keine Beschriftung.
5. **Gleiche Teile werden ein Feld** („sévère / sévère“). *Kosten:* keine sichtbaren.
6. **Ein Teil ist, was die B-462-Regel trennt** (` / `, `;`, `,` außerhalb von Klammern, nicht in Sätzen);
   Klammerteile bleiben im Feld und dürfen fehlen. **Ausnahme:** Ein Teil, das nur aus Platzhalter-Kürzeln
   besteht (qn, qc, sb, sth, jdn, jdm, etw …), wird nicht abgetrennt – „connaître qn / qc“ bleibt **ein** Feld.
   *Kosten:* eine Kürzel-Liste im Code neben der Abkürzungsliste von B-462.
7. **Gelöst im Code, nicht im Speicher.** Die Einträge bleiben, wie das Buch sie druckt. *Kosten:* Die Zerlegung
   bleibt eine Heuristik; was sie als Teil erkennt, steht nirgends in den Daten.

## Akzeptanzkriterien

1. Eine Karte zu „quel / quelle / quels / quelles“ zeigt auf der Kästchenstufe vier Kästchenreihen, je eine
   Form, ohne feste Trenner zwischen ihnen; im Freitext und im Diktat vier Textfelder.
2. Die Antwort trägt die Teile einzeln; die Karte ist richtig, wenn jeder Teil genau einmal getroffen ist,
   Reihenfolge beliebig.
3. Drei von vier richtig ⇒ Ausgang *halb* (B-624); einer von vier ⇒ falsch; im Abschlusstest ⇒ falsch.
4. „sévère / sévère“ zeigt ein Feld; „connaître qn / qc“ zeigt ein Feld.
5. Ein einteiliger Eintrag verhält sich wie heute (ein Feld, eine Maske).
6. Üben, Abschlusstest, Bonus-Training und Vorschau zerlegen gleich (Integrationstest je Pfad).
7. Das Blatt zeigt je Feld, ob es stimmt.

## Verlauf

- **2026-11-17** — angelegt aus dem Chat mit dem Vater.
- **2026-11-17** — ausformuliert: Ursache sind die Buchstabenkästchen (eine Maske über den ganzen Eintrag);
  an der DB-Kopie 22 mehrteilige Einträge auf der getippten Seite gemessen, fast alle französische Formen;
  acht offene Punkte mit Empfehlung.
- **2026-11-17** — gegrillt mit dem Vater: sieben Entscheidungen, alle nach Empfehlung; teilweise richtig
  zählt halb wie in B-624, B-625 hängt damit an B-624.
