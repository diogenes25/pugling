---
tags: [typ/story, status/ausformuliert, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [Akzent-Toleranz, Akzentfehler]
status: ausformuliert
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
- **Friedrich tippt Französisch.** Seit [B-597](B-597-getippt-wird-immer-die-zielsprache.md) wird getippt immer
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

1. **Wird B-524/2 („falsch geschrieben ist falsch“) revidiert?** Empfehlung: **ja, eingeschränkt** – im Üben
   wird gelockert, im Abschlusstest (Schulformat) bleibt es binär, damit der Test weiter misst, was die Schule
   misst. Kosten: Üben und Test werten verschieden; `AnswerGrader` hält die Regel heute bewusst gleich.
2. **Was zählt milder: nur Akzente oder jeder Beinahe-Treffer?** Empfehlung: **nur Diakritika** (é/è/ê/e,
   à/a, ç/c, ï/i, ô/o, ü/u, ä/a …) – sonst nichts. Ein falscher Buchstabe ist bei LRS genau die Schwäche, die
   geübt werden soll (Notiz zu B-524); ein Akzent ist auf dem Gerät oft eine Tastenfrage. Kosten: „Strase“ statt
   „Straße“ bleibt ein ganzer Fehler.
3. **Wie milder?** Optionen: (a) zählt als richtig, mit Hinweis „Achte auf den Akzent“; (b) zählt halb: Kasten
   bleibt stehen (kein Auf-, kein Abstieg), keine Münzen; (c) Teilpunkte. Empfehlung: **(b)** – das Kind wird
   nicht zurückgeworfen, steigt aber auch nicht auf, bevor die Schreibweise sitzt. Kosten: ein dritter Ausgang
   im Review neben richtig/falsch; Combo, Truhe und Pflicht-Zählung müssen ihn kennen.
4. **Global, je Position oder je Kind?** Empfehlung: **je Position, Default an** – wie `ForgivingDemotion`, der
   Vater kann es für eine Rechtschreib-Position abschalten. Kosten: ein Feld an der Position (Migration) und im
   Vater-Web.
5. **Welche Sprachen?** Empfehlung: **jede Sprache**, die Faltung ist zeichenbasiert. Kosten: Deutsche Umlaute
   fallen mit darunter („Mädchen“/„Madchen“) – für Englisch→Deutsch in Theodors Plänen selten getippt, seit
   B-597 gar nicht.
6. **Was sieht der Vater?** Empfehlung: im Lernstand bzw. Positions-Report eine eigene Kategorie „Akzent“
   neben „falsch geschrieben“, damit er sieht, wo die Schreibweise noch fehlt. Kosten: ein Zähler im Report.
7. **Plan-übergreifender Lernstand je Wort.** Empfehlung: **bleibt hart** wie bei B-219 (ehrliche Messung).
   Kosten: Position und Wort-Lernstand können auseinanderlaufen.

## Akzeptanzkriterien (Entwurf)

1. Im Üben einer Position mit eingeschalteter Akzent-Milde zählt „ecole“ für „école“ weder als richtig noch als
   falsch im Sinne von Punkt 3; das Blatt zeigt „Achte auf den Akzent: école“.
2. „ecolle“ für „école“ (Buchstabe falsch) bleibt falsch (und weiter „falsch geschrieben“).
3. Im Abschlusstest ändert sich nichts (Integrationstest: dieselbe Antwort ist falsch).
4. Ausgeschaltet an der Position, verhält sich das Üben wie heute.
5. Der Vater sieht die Akzent-Fälle getrennt von den übrigen Schreibfehlern.

## Verlauf

- **2026-11-17** — angelegt aus dem Chat mit dem Vater; Konflikt mit B-422/4 benannt.
- **2026-11-17** — ausformuliert: Ist-Stand gegen den Code (`Normalize` ohne Akzent-Faltung, `IsMisspelled` nur
  Anzeige, `ForgivingDemotion` als vorhandene Milde), Konflikt mit B-524/2 (Entscheidung des Vaters) benannt,
  sieben offene Punkte mit Empfehlung.
