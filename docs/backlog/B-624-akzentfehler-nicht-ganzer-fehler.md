---
tags: [typ/story, status/gegrillt, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [Akzent-Toleranz, Akzentfehler]
status: gegrillt
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17 (Erfahrung aus der Testphase mit Friedrich)"
grund: ""
ersetzt_durch: []
---

# B-624 · Ein falscher Akzent im Französischen zählt nicht als ganzer Fehler

Wer im Französischen einen Akzent, einen Strich oder einen Buchstaben falsch setzt, bekommt heute einen
kompletten Fehler – mit Kasten zurück und gegebenenfalls Malus. Der Vater hält das für zu streng und
wünscht eine mildere Wertung.

**Achtung, das kehrt eine frühere Entscheidung um:** [B-422](B-422-beinahe-treffer-tippfehler.md),
Entscheidung 4, hat am 2026-11-13 (Stakeholder-Entscheidung im Auftrag des Nutzers) ausdrücklich **keine**
Normalisierung für Akzente beschlossen, weil die Schule „é" für „e" als Rechtschreibfehler zählt; ein
Beinahe-Treffer wird nur als „falsch geschrieben" gekennzeichnet und bleibt falsch. Dahinter steht
[B-524](B-524-vokabeltest-wie-in-der-schule.md)/2 („falsch geschrieben ist falsch"). Die Grill-Runde muss
also entscheiden, ob und wo das jetzt anders gilt – Kandidaten: nur Akzente vs. jeder Beinahe-Treffer,
nur im Üben vs. auch im Test, halber Fehler (wie der LRS-Ausgleich −½ in Friedrichs Schule) vs. richtig
mit Hinweis, global vs. Option je Position/Kind.

## User Story

Als **Lernender** möchte ich, dass ein Wort, das ich weiß und nur an einem Akzent oder einem Buchstaben falsch
schreibe, nicht wie ein ungewusstes Wort zählt, damit ich nicht für Kleinigkeiten zurückgeworfen werde und die
Lust verliere. Als **Supervisor** möchte ich trotzdem sehen, dass die Schreibweise noch nicht sitzt.

## Ist-Stand am Code

Recherchiert am 2026-11-17 gegen `5938fc12`.

- **Eine Regel für Üben, Test und Vorschau.** `AnswerGrader` ist der einzige Vergleich
  (`backend/Pugling.Api/Services/Shared/AnswerGrader.cs:3`–`:16`); er normalisiert über
  `StageMechanics.Normalize` (`backend/Pugling.Api/Services/Shared/StageMechanics.cs:40`).
- **`Normalize` faltet keine Akzente.** Es verzeiht Groß/klein, Leerraum, die Schreibweise des Apostrophs und das
  Satzzeichen am Ende (`StageMechanics.cs:31`–`:57`) – „é“ und „e“ bleiben verschieden. Dazu verzeiht die
  Artikelregel einen fehlenden führenden Artikel, auch französisch (`StageMechanics.cs:66`).
- **„Falsch geschrieben“ wird erkannt, aber nur angezeigt.** `AnswerGrader.IsMisspelled`
  (`AnswerGrader.cs:35`–`:47`): Damerau-Levenshtein ≤ 1 bis fünf Buchstaben, ≤ 2 darüber, und kein anderes Atom
  der Übung ist näher dran. Ein reiner Akzentfehler hat Abstand 1 und fällt damit immer darunter – aber genauso
  ein falscher oder fehlender Buchstabe. Aufrufer: Üben (`backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:489`,
  `:706`), Test (`backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:431`), Vorschau
  (`backend/Pugling.Api/Services/Creator/ExercisePreviewService.cs:96`). Die Wertung bleibt falsch
  (`PositionPracticeController.cs:486`–`:489`, Kommentar „named, not forgiven“).
- **Eine mildere Abstufung gibt es schon – je Position, für jeden Fehler.** `PlanPosition.ForgivingDemotion`
  (B-219, `backend/Pugling.Api/Models/PlanPositionEntities.cs:125`–`:129`): ein einzelner Fehler senkt um einen
  Kasten statt auf Kasten 1; der plan-übergreifende Lernstand je Wort bleibt hart.
- **Friedrich tippt Französisch.** Seit [B-597](B-597-tippstufen-fragen-die-zielsprache-ab.md) wird getippt immer
  die Zielsprache; in Plan 8 sind das französische Wörter mit Akzenten.
- **Zwei frühere Entscheidungen stehen dagegen.** [B-524](B-524-vokabeltest-wie-in-der-schule.md)/2 (Grill-Runde
  **mit dem Vater**, 2026-11-13): „Die Wertung bleibt binär … wertet falsch geschrieben als falsch“ – gegen die
  damalige Empfehlung von Teilpunkten; das −½ auf Friedrichs Blatt ist sein LRS-Ausgleich und wird bewusst nicht
  nachgebildet. [B-422](B-422-beinahe-treffer-tippfehler.md)/4 (Stakeholder im Auftrag): keine eigene
  Normalisierung für Akzente, weil die Schule „é“ für „e“ als Fehler zählt.

## Die echte Lücke

Technisch fehlt wenig – der Beinahe-Treffer wird schon erkannt, und eine mildere Kasten-Regel gibt es. Die Lücke
ist eine **Produktentscheidung**: Der Vater erlebt die binäre Wertung jetzt als zu streng und muss B-524/2 bewusst
neu entscheiden. Danach fehlen (a) die Abgrenzung, *was* milder zählt (nur Akzente oder jeder Beinahe-Treffer),
(b) *wie* milder (richtig mit Hinweis, Kasten halten, halber Punkt), (c) *wo* (Üben, Test) und (d) *für wen*
(global, je Position, je Kind).

## Offene Punkte

Alle in der Grill-Runde vom 2026-11-17 entschieden – siehe Entscheidungen.

1. ~~**Wird B-524/2 revidiert?**~~ → Entscheidung 1.
2. ~~**Nur Akzente oder jeder Beinahe-Treffer?**~~ → Entscheidung 2.
3. ~~**Wie milder?**~~ → Entscheidung 3.
4. ~~**Global, je Position oder je Kind?**~~ → Entscheidung 4.
5. ~~**Welche Sprachen?**~~ → Entscheidung 5.
6. ~~**Was sieht der Vater?**~~ → Entscheidung 6.
7. ~~**Plan-übergreifender Lernstand je Wort.**~~ → Entscheidung 7.

## Entscheidungen

Grill-Runde mit dem Vater, 2026-11-17. Alle sieben nach Empfehlung.

1. **B-524/2 wird für das Üben revidiert, der Abschlusstest bleibt binär.** Im Üben soll das Kind nicht für
   Kleinigkeiten zurückgeworfen werden; der Test misst weiter, was die Schule misst, und hält damit den Grund
   der Entscheidung vom 2026-11-13 (ehrlich zeigen, wo die Rechtschreibung fehlt). *Kosten:* Üben und Test werten
   verschieden, `AnswerGrader` trennt beide erstmals (`AnswerGrader.cs:15` sagt heute das Gegenteil); B-422/4
   gilt für den Test weiter.
2. **Milder zählen nur Diakritika.** Antwort und Lösung sind nach dem Entfernen der Zeichen auf und unter dem
   Buchstaben gleich (é/è/ê→e, à→a, ç→c, ï→i, ô→o, ü/ä/ö→u/a/o …), vorher nicht. Ein falscher, fehlender oder
   vertauschter Buchstabe bleibt ein ganzer Fehler. Begründung: Ein Buchstabe ist bei LRS die Schwäche, die
   geübt werden soll; ein Akzent ist oft eine Tastenfrage. *Kosten:* „ecolle“ und „Strase“ bleiben falsch (mit
   Markierung „falsch geschrieben“).
3. **Ein Akzentfehler zählt halb.** Die Karte bleibt im Kasten (kein Auf-, kein Abstieg) und kommt zum nächsten
   fälligen Termin wieder; keine Münzen; die Combo reißt nicht; als Pflicht-Karte zählt sie als gespielt. Das
   Blatt zeigt „Fast! Achte auf den Akzent: *Lösung*“. Begründung: nicht zurückwerfen, aber auch nicht
   aufsteigen lassen, bevor die Schreibweise sitzt – sonst überrascht der strenge Test. *Kosten:* ein dritter
   Ausgang neben richtig/falsch im Review; Combo, Tagestruhe und Pflicht-Zählung lernen ihn.
4. **Je Position einstellbar, standardmäßig an** – auch für die bestehenden Positionen (die Migration setzt
   den Haken). Der Vater schaltet es für eine Position ab, die gezielt Akzente übt. *Kosten:* ein Feld an
   `PlanPosition` (Migration, Bestand → an), ein Schalter im Vater-Web, Vertragsfeld an den Positions-DTOs;
   Friedrichs Plan 8 wertet ab dem Einspielen milder.
5. **Für jede Sprache**, weil die Regel an Zeichen hängt. *Kosten:* deutsche Umlaute fallen mit darunter;
   seit B-597 wird kaum Deutsch getippt.
6. **Eigene Kategorie „Akzent“** im Lernstand bzw. Positions-Report neben „falsch geschrieben“ und „falsch“.
   Begründung: Der Vater sieht, ob Wörter oder nur Akzente fehlen. *Kosten:* ein Zähler im Report.
7. **Der plan-übergreifende Lernstand je Wort bleibt hart** (Akzentfehler = falsch), wie bei B-219.
   Begründung: ehrliche Messung, ob das Wort samt Schreibweise sitzt. *Kosten:* Position und Wort-Lernstand
   können auseinanderlaufen.

## Akzeptanzkriterien

1. Im Üben einer Position mit eingeschalteter Milde liefert „ecole“ für „école“ den Ausgang *halb*: Kasten
   unverändert, keine Münzen, Combo unverändert, Karte als gespielt gezählt; das Blatt zeigt
   „Fast! Achte auf den Akzent: école“.
2. „ecolle“ für „école“ bleibt falsch und „falsch geschrieben“ (Kasten zurück wie heute bzw. nach
   `ForgivingDemotion`).
3. Im Abschlusstest ist „ecole“ für „école“ falsch, wie heute (Integrationstest).
4. Mit ausgeschaltetem Haken verhält sich das Üben wie heute (Integrationstest).
5. Nach der Migration tragen alle bestehenden Positionen den Haken *an*; neue Positionen ebenso.
6. Ein Akzentfehler in einer anderen Sprache (Umlaut, spanisches ñ) wird gleich behandelt.
7. Der Positions-Report bzw. Lernstand weist Akzent-Fälle als eigene Kategorie aus.
8. Der Lernstand je Wort (`ItemProgressService`) wertet den Akzentfehler weiter als falsch.

## Verlauf

- **2026-11-17** — angelegt aus dem Chat mit dem Vater; Konflikt mit B-422/4 benannt.
- **2026-11-17** — ausformuliert: Ist-Stand gegen den Code (`Normalize` ohne Akzent-Faltung, `IsMisspelled` nur
  Anzeige, `ForgivingDemotion` als vorhandene Milde), Konflikt mit B-524/2 (Entscheidung des Vaters) benannt,
  sieben offene Punkte mit Empfehlung.
- **2026-11-17** — gegrillt mit dem Vater: sieben Entscheidungen, alle nach Empfehlung; B-524/2 gilt nur noch
  für den Abschlusstest.
