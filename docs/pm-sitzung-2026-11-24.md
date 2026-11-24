# PM-Sitzung: Vorbereitungs-Pflicht dicht machen

**Datum:** 2026-11-24  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die vier Defekte aus Sprint 134 (B-940–B-943) schließen, damit die Vorbereitungs-Pflicht (B-752/B-757/B-758)
abgenommen werden kann.

## Runde 1 — Vorstellung & Feedback

Kein frischer Rollengang zu Beginn: Der Nutzer hat im Dialog entschieden, den Sprint auf die offenen Defekte zu legen,
statt zuerst neu zu sichten. Die Rollen-Stimmen stammen aus der Abnahme-Runde 134
([pm-sitzung-2026-11-23-i.md](pm-sitzung-2026-11-23-i.md)) und wurden dort an der laufenden App erhoben. Anmerkungen unter
`docs/anmerkungen/` sind alle erledigt (Stand 2026-08-14), es gibt keinen neuen Befund.

### Feedback Vater (aus Runde 134)

- Gefällt: Er sieht „etwa N Karten je Tag“ und den Grund, wenn keine Pflicht entsteht.
- Stört: Das Kind kann den Stoff über Schlagwörter verkleinern (B-940). Zurückgezogenes Material kann über ein Schlagwort
  doch Pflicht werden (B-941).
- Top-3: B-940, B-941, eine Meldung nach dem Speichern (B-942).

### Feedback Creator (aus Runde 134)

- Stört: Zurückziehen soll halten, auch über ein Schlagwort (B-941).

### Feedback Sohn (aus Runde 134)

- Die Vorbereitung steht als Knoten auf dem Pfad, die Karte nennt die Arbeit. Ob sich die Last fair anfühlt, kann nur ein
  Mensch beurteilen.

## PM-Synthese & Priorisierung (→ Entwickler)

| Prio | Story | Größe | Wo | Warum jetzt |
| --- | --- | --- | --- | --- |
| P2 | B-940 Kind verkleinert sein Bündel | S | backend | untergräbt die Erzwingung, also den Kern |
| P2 | B-941 Vorbereitung umgeht das Ausführ-Recht | S | beides | Zurückziehen des Creators hält nicht |
| P3 | B-942 Meldung des Fenster-Editors verschwindet | XS | frontend | der Vater sieht keinen Erfolg |
| P3 | B-943 Zusicherung ohne Biss | XS | frontend | ein Test, der nicht fallen kann |

**Roter Faden:** Die Vorbereitungs-Pflicht fragt genau den Stoff ab, den ein Erwachsener freigegeben hat, und nur diesen.

**Entscheidungen im Dialog (Nutzer, 2026-11-24):**

1. B-940: Sperre, solange das Schlagwort an einer geplanten Arbeit hängt (ohne Migration).
2. B-941: Es zählen nur ausführbare Übungen (öffentlich ausführbar oder mit Freigabe für einen Betreuer des Kindes). Die
   Vater-Sicht nennt die ausgelassenen.
3. Umfang: ein Sprint über B-940–B-943, danach Abnahme von B-752/B-757/B-758 und Halt.

**Refinement:** B-940–B-943 von `idee` auf `geschaetzt` gebracht; die Ist-Stände sind am Code belegt (siehe die Stories).
Neu gefunden: auch `DELETE creator/tags/{id}` (das ganze Schlagwort) verkleinert das Bündel, weil die Verknüpfung zur
Arbeit per Cascade fällt. Das ist in B-940 aufgenommen.

## Sprint 135 — Ziel & Umfang

**Sprint-Ziel:** Der Vater plant eine Klassenarbeit über Schlagwörter, und ihr Stoff kann weder vom Kind verkleinert werden
noch zurückgezogenes Material enthalten. Wenn etwas ausgelassen wird, sieht der Vater es, und sein Speichern des Fensters
quittiert die App sichtbar.
**Umfang:** B-940, B-941, B-942, B-943.
**Entwickler-Brief:**

- Senke für „was wird Pflicht“: `ClassTestPrepService.NewCandidatesAsync`. Start und Zählung lesen beide dort, darum steht
  der Ausführ-Filter dort und nicht im Bündel-Helfer: Die Übungs-Ansicht und die Bereitschaft des Kindes sollen das Bündel
  weiter vollständig sehen.
- Senke für die Sperre: ein Helfer in `TagsController`, den beide Schreibwege des Kindes durchlaufen.
- Guards: Erwachsene bleiben frei; ein Grant eines fremden Erwachsenen reicht nicht.
- Keine Migration. Additives Vertragsfeld `PrepNotExecutableCount`.
- Reihenfolge: B-940 → B-941 (Backend, Vertrag) → Contract erzeugen → B-941 Zelle, B-942, B-943.

## Iteration 135 — umgesetzt

**Backend (API-First):**

- B-940: `ApiErrors.TagLockedByClassTest` (409 `tag_locked_by_class_test`) und `TagsController.ClassTestLockAsync` als die
  eine Sperre, durch die beide Schreibwege des Kindes laufen (`UntagExercise`, `Delete`). Die Meldung nennt die Arbeit und
  nur den Schritt, der die Sperre aufhebt.
- B-941: `ClassTestPrepService.ExecutableFor(childId)` (öffentlich ausführbar **oder** Grant eines Betreuers des Kindes)
  steht in der Senke `NewCandidatesAsync`; Start und „etwa N Karten“ lesen beide dort. `NotExecutableCountAsync` speist
  das additive Vertragsfeld `KlassenarbeitResponse.PrepNotExecutableCount` (0, wenn keine Pflicht läuft).

**Frontend:** `VorbereitungsZelle` nennt „N Übung(en) ohne Ausführ-Recht ausgelassen“ (B-941); der `FensterEditor` hängt
nur noch an der Id der Arbeit, sodass die Meldung das Neuladen übersteht (B-942); der Zellen-Test für geschriebene und
abgesagte Arbeiten trägt jetzt eine Zahl und erwartet `–` (B-943).

**Tests:** `VorbereitungsBuendelSperreTests` (neu, 8 Fälle), zwei Fälle in `VorbereitungsPflichtTests`, zwei Vitest-Fälle
in `VaterClassTests.test.tsx` (dazu B-943 umgeschrieben), E2E `klassenarbeit-stoff-dicht.spec.ts` als Rollengang.

**Mutanten-Probe** (`.claude/scripts/mutant-probe.py`, Debug, nach der letzten Logik-Änderung; danach kam nur noch das Datumsformat der
Sperr-Meldung samt `using`, wodurch die Zeilen in `TagsController.cs` um eine nach unten rutschen):

| Mutante | Stelle | erwartet | gemessen | Anmerkung |
|---|---|---|---|---|
| B940-untag | backend/Pugling.Api/Controllers/Creator/TagsController.cs:221 | red | red |  |
| B940-delete | backend/Pugling.Api/Controllers/Creator/TagsController.cs:140 | red | red |  |
| B940-status | backend/Pugling.Api/Controllers/Creator/TagsController.cs:57 | red | red |  |
| B940-adult | backend/Pugling.Api/Controllers/Creator/TagsController.cs:55 | red | red |  |
| B941-public | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:126 | red | red |  |
| B941-grant | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:127 | red | red |  |
| B941-anygrant | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:127 | red | red |  |
| B941-sink | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:156 | red | red |  |
| B941-count | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:144 | red | red |  |
| B941-cell | frontend/src/vater/VaterClassTests.tsx:191 | red | red |  |
| B941-cell0 | frontend/src/vater/VaterClassTests.tsx:191 | red | red |  |
| B942-key | frontend/src/vater/VaterClassTests.tsx:276 | red | red |  |
| B943-status | frontend/src/vater/VaterClassTests.tsx:182 | red | red |  |
| R1-claim | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:189 | red | red |  |
| R3-inplan | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:144 | red | red |  |
| R3-day | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:137 | red | red |  |
| R3-status | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:137 | red | red |  |

Grundlinie nach dem Zurückspielen (beide Läufe): green. Die R-Zeilen sind ein eigener Lauf über die Review-Korrekturen.

**Verifikation:** Backend-Suite 2309/2309 grün (Release, nach der letzten Code-Änderung; der Lauf davor fand zwei Tore rot – den
409-Code ohne deutschen Text und ein Datumsformat in der Sperr-Meldung, das der Tag-Wächter als Tagesbildung liest; beide behoben).
Frontend `vitest` 1426/1426, `tsc -b` grün. `dotnet format --verify-no-changes`: kein Fund in den Dateien dieses Sprints (die
übrigen `WHITESPACE`-Funde sind das bekannte CRLF/LF-Rauschen in `PositionPlayService.cs`). Commit `1b92cd02`.

**Review:** `pugling-reviewer` – 🟡 1 (Bündel nur aus zurückgezogenem Material fror die Arbeit ein; behoben, R1 rot), 🟢 2 (Zählung
an die Grenzen von `CardsPerDayAsync` angeglichen, R3 rot; das Verlängern laufender Positionen über eine spätere Arbeit bleibt
bewusst ungefiltert, im Code begründet). `frontend-reviewer` – keine 🔴/🟡; der Locator hängt nicht mehr am Standard 7, die übrigen
🟢 in B-944. **Fehlerzähler Sprint 135:** 2 (der Review-🟡 und der eigene Fehlbeleg B941-public, den die Probe fand).

## Runde 135 — Re-Review / Abnahme

Server für den Gang: von Playwright nach der letzten Änderung gestartet (Ports 5231/5204, Wegwerf-DB; `:5200` ist der
Heimnetz-Server mit der echten DB und blieb unberührt). E2E `klassenarbeit-stoff-dicht`, `klassenarbeit-vorbereitung`,
`klassenarbeit-countdown`, `pfad`: 4 passed.

- **Vater – abgenommen (maschinell gegangen):** „Ich plane die Arbeit über ein Schlagwort mit fremdem, zurückgezogenem Material, und
  die Liste sagt mir ‚1 Übung ohne Ausführ-Recht ausgelassen‘. Ich speichere das Fenster und sehe ‚Vorbereitung: 11 Tage.‘ Das Kind
  kann das Schlagwort nicht lösen.“ Die Gesamtabnahme der Vorbereitungs-Pflicht (B-752/757/758) steht noch aus, siehe unten.
- **Creator – abgenommen (Regressionszeuge mit eigenem Anliegen):** Sein Zurückziehen hält jetzt auch über ein Schlagwort (E2E Zeile 90,
  `VorbereitungsPflichtTests`). Kein Creator-Pfad im Diff außer dem Fehlercode am Schlagwort-Endpunkt.
- **Sohn – geliefert, Prüfung durch einen Menschen offen:** Auf seinem Pfad steht nur die erlaubte Übung (E2E); seine eigenen Specs
  `pfad` und `klassenarbeit-countdown` sind grün. Ob sich die Last fair anfühlt, kann nur ein Mensch beurteilen – das stand schon in
  Runde 134 und bleibt.

**Stand:** B-940, B-941, B-942, B-943 `abgenommen`. **B-752/B-757/B-758 bleiben `in-arbeit`:** Die Mutanten-Probe über ihre
AK-Fälle ist in Lauf I nie gelaufen, und die Fixes aus `6162599c` sind ungeprobt. Das ist eine Eintrittsbedingung (pm-loop Step 5)
und gehört nicht in diesen Sprint. Dazu fehlt die Probe der Migration `ClassTestPrepDuty` an einer Kopie der echten DB. Beides steht
in `wartet_auf`. Nebenbei gefunden: Die E2E-Spec `klassenarbeit-vorbereitung` war seit `6162599c` rot (sie erwartete 2 statt 3
Karten je Tag). Am Stand ohne Sprint 135 gemessen; die Spec ist angepasst.

## Retrospektive

**Nachschau:** Sprint 133 (B-750, B-751, Commit `65f4d2a7`) gelesen – Controller, Bündel-Helfer, Arcade-Karte. Befund in B-751:
„x von y **Wörtern** sicher“ zählt `ExerciseItem`s, ein Wort in zwei Übungen des Bündels also doppelt →
[B-945](backlog/B-945-bereitschaft-zaehlt-woerter-doppelt.md) (`Defekt`, `entgangen_bei: B-751`, unverifiziert an echten Bündeln).
B-750 ohne Befund. `nachgeschaut: 2026-11-24` an beiden. Index: Nachgeschaut 563 von 603 abgenommenen.

**Was die eigenen Tore durchgelassen haben:**

1. Ich habe den Ausführ-Filter an die Senke `NewCandidatesAsync` gesetzt. Die Leer-Prüfung vor dem Anspruch in `ReconcileAsync`
   las dieselbe Quelle aber ungefiltert, und so fror eine Arbeit ohne erlaubte Übung für immer ein. Gefunden hat das der
   `pugling-reviewer`, kein Tor. Die Regel „shared helper → per caller“ in pm-loop Step 4 deckte den Fall dem Wortlaut nach
   nicht: Es änderte sich kein Helfer, es kam ein Filter dazu.
2. Ein eigener Test wollte die `ExecutePublic`-Klausel prüfen, aber seine öffentliche Übung trug zugleich den Grant des Vaters.
   Damit war die Klausel nie allein wahr. Gefunden hat das die Mutanten-Probe (grün statt rot), also ein Tor – kein neuer
   Mechanismus nötig.
3. Zwei Konventions-Tore schlugen erst im vollen Lauf an (deutscher Text für 409, Datumsformat). Das ist richtig so; dafür gibt
   es sie.

**Der eine Mechanismus (gelandet):** pm-loop Step 4: Die Regel „Changing a shared helper … is decided per caller“ nennt jetzt
auch „a filter at a sink“ und verlangt, jeden anderen Leser der gefilterten Quelle zu greppen, Wächter eingeschlossen. Prosa im
treibenden Skill, kein neues Tor – darum kein Halt. Die Regel ist in den bestehenden Satz gefaltet (+161 B). Die Skill-Datei lag
schon vor diesem Sprint rund 1,7 KB über ihrem Budget (`context-budget.sh`). Eine Regel zu streichen, um Platz zu schaffen,
überlasse ich dem Nutzer.

**Bewusst ungedeckt:** Das Verlängern laufender Positionen über eine spätere Arbeit prüft kein Ausführ-Recht (es ist keine neue
Zuweisung; im Code begründet). Gleichzeitige Logins bleiben ohne deterministischen Test (B-944).

## Runde 136 — Mutanten-Probe B-752/B-757/B-758

**Auftrag des Nutzers (2026-11-24):** die fehlende Probe laufen lassen. Grundlage war die in Lauf I vorbereitete Liste (28 Mutanten);
jede Zeile wurde an der heutigen Stelle über ihren genauen Text neu verortet. Dazu kamen fünf Mutanten für die Korrekturen aus
`6162599c` (F1–F5) und vier für die Vater-Oberfläche (U1–U4). Gemessen in Debug, die Grundlinie war vor und nach jedem Lauf grün.

| Mutante | Stelle | erwartet | gemessen | Anmerkung |
|---|---|---|---|---|
| I-P1 Fensterbeginn ignoriert | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:59 | red | red |  |
| I-P4 Plan anderen Fachs | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:65 | red | red |  |
| I-P5 inaktiver Plan zaehlt | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:65 | red | red |  |
| I-P6 Dosis abgerundet | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:98 | red | red |  |
| I-P7 Deckel fehlt | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:98 | red | red |  |
| I-P8 Dublette im Plan | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:157 | red | red |  |
| I-P9 leere Uebung | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:161 | red | red |  |
| I-P13 Malus je Uebung voll | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:240 | red | red |  |
| I-P14 Ziel-Belohnung | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:241 | red | red |  |
| I-P15 Dosis ohne Leitner | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:238 | red | red |  |
| I-P17 eigene Arbeit deckt nicht | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:274 | red | red |  |
| I-P18 frueheste Arbeit gewinnt | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:284 | red | red |  |
| I-P20 Wiederbeginn rueckwirkend | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:286 | red | red |  |
| I-P21 PATCH gleicht nicht ab | backend/Pugling.Api/Controllers/Supervisor/KlassenarbeitenController.cs:210 | red | red |  |
| I-P22 Loeschen schliesst nicht | backend/Pugling.Api/Controllers/Supervisor/KlassenarbeitenController.cs:231 | red | red |  |
| I-P23 Login gleicht nicht ab | backend/Pugling.Api/Controllers/AuthController.cs:88 | red | red |  |
| I-P24 Kauf gleicht nicht ab | backend/Pugling.Api/Controllers/Student/MeController.cs:297 | red | red |  |
| I-P26 negatives Fenster erlaubt | backend/Pugling.Api/Controllers/Supervisor/KlassenarbeitenController.cs:62 | red | red |  |
| I-P27 Karten bei Fenster 0 | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:107 | red | red |  |
| I-P28 Grund ohne Fach fehlt | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:75 | red | red |  |
| I-P29 freigegebene Position | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:261 | red | red |  |
| I-P30 Fenster beim Anlegen ignoriert | backend/Pugling.Api/Controllers/Supervisor/KlassenarbeitenController.cs:146 | red | red |  |
| F1-leer-friert | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:189 | red | red |  |
| F2-anlegen | backend/Pugling.Api/Controllers/Supervisor/KlassenarbeitenController.cs:153 | red | red |  |
| F3-zuweisen | backend/Pugling.Api/Controllers/Supervisor/KlassenarbeitenController.cs:255 | red | red |  |
| F5-spanne | backend/Pugling.Api/Services/Shared/PositionProgressService.cs:237 | red | red |  |
| U1-anlegen | frontend/src/vater/VaterClassTests.tsx:79 | red | red |  |
| U2-aus | frontend/src/vater/VaterClassTests.tsx:184 | red | red |  |
| U3-grund | frontend/src/vater/VaterClassTests.tsx:185 | red | red |  |
| U4-standard | frontend/src/vater/VaterClassTests.tsx:210 | red | red |  |
| I-P12b Ende am Termin (Align) | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:284 | red | red |  |
| I-P16b Abrechnung ohne Obergrenze | backend/Pugling.Api/Services/Shared/PositionProgressService.cs:857 | red | red |  |
| I-P19b nichts schliessen (`_ = progress;`) | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:280 | red | red |  |
| I-P25b Zuruecksetzen wirkungslos | backend/Pugling.Api/Controllers/Supervisor/KlassenarbeitenController.cs:200 | red | red |  |
| F4-tag (mit neuem Test) | backend/Pugling.Api/Controllers/Supervisor/KlassenarbeitenController.cs:290 | red | red |  |
| I-P10b Bündel nicht eingefroren (zwei Zeilen, von Hand) | backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:176+188 | red | red | Schleifenfilter und Anspruch zugleich entfernt |

**Sechs Zeilen trugen im ersten Lauf keinen Beleg; so wurden sie aufgelöst:**

1. **P12** (`DutyUntil` am Termin in `StartAsync`) blieb grün, weil der Mutant gleichwertig ist: `AlignSpansAsync` setzt das Ende
   im selben Abgleich neu. Die Zeile, die AK 2 trägt, ist die `until`-Zeile in Align → P12b rot.
2. **P16 und P19** kompilierten nicht (`if (false)` ist unerreichbarer Code; ohne den Aufruf war `progress` ungelesen) →
   umformuliert als P16b und P19b, beide rot.
3. **P25** fiel, aber im Testfall `Clear_Schalter_Leert_Und_Gewinnt`, nicht im erwarteten Namen. Neu gemessen mit dem richtigen
   Namen (P25b), rot.
4. **P10:** Das Einfrieren hat zwei unabhängige Wächter – den Schleifenfilter auf den geladenen Stand und den DB-Anspruch, der
   gleichzeitige Logins abfängt. Kein einzeiliger Mutant macht AK 3 rot. Die Zeile `k.PrepStartedOn = today` fiel an anderer
   Stelle: Sie speist das `started` von Align im Speicher. Darum von Hand mit beiden Zeilen gemessen: AK 3 rot, nach
   `git checkout` grün.
5. **F4 war eine echte Lücke:** Kein Test prüfte, dass das Verknüpfen eines Schlagworts im Fenster sofort abgleicht. Neuer Test
   `Schlagwort_Verknuepfen_Im_Fenster_Legt_Die_Pflicht_Ohne_Login_An` → F4 rot.

**Bewusst ungemessen:** die Transaktion um Anspruch und Positionen (`6162599c`) und gleichzeitige Logins. Beides lässt sich nicht
deterministisch auslösen (B-944).

## Sprint 137 — Ziel & Umfang

**Sprint-Ziel:** Die Klassenarbeits-Zahlen sagen, was gespielt wird – das Kind sieht so viele Wörter, wie die Arbeit hat, und der
Vater sieht nach dem Start dieselben „etwa N Karten je Tag“ wie vorher.
**Umfang:** [B-945](backlog/B-945-bereitschaft-zaehlt-woerter-doppelt.md) (Nennerregel mit dem Nutzer entschieden: verschiedene
Wörter, sicher = Schnitt über die Bündel-Zeilen ≥ `WeakBelowPercent`, ungeübt zählt 0),
[B-946](backlog/B-946-karten-je-tag-nach-dem-start-zeigen-den-deckel.md) (aus B-944 Punkt 1 herausgelöst).
**Entwickler-Brief:** Senke je Zahl – die Bereitschaft im Endpunkt `ChildClassTestsController` faltet nach `VocabularyId`; die
Vater-Liste liest `PositionPlayService.PoolSize`, die Regel, nach der das Kind die Runde spielt. Keine Migration, kein Vertragsbruch
(`TotalItems`/`SecureItems` bleiben, Doku sagt „Wörter“).

## Iteration 137 — umgesetzt

Commit `262bd5ae`. Volle Suite 2315/2315 grün (Release), Build ohne Warnung. Mutanten-Probe nach dem letzten Edit: B945-1…5 und
B946-1/2 alle rot (Tabellen in den Stories). `pugling-reviewer`: B-945 ohne Fund; B-946 ein 🟡 (eigene Rechnung statt `PoolSize`,
Deckel-Fall ungetestet), beides eingearbeitet und neu gemessen.

## Runde 137 — Re-Review / Abnahme

Server frisch gestartet auf Ausweichports (5201/5174, Release; `:5200` unberührt). `klassenarbeit-countdown` und
`klassenarbeit-vorbereitung`: 2 passed. Die Specs fahren die Anzeige; Überlappungs- und Deckelfall tragen die Integrationstests.

- **Sohn – abgenommen (maschinell):** Die Karte zeigt „x von y Wörtern sicher“; ein Wort in zwei Übungen zählt einmal.
- **Vater – abgenommen (maschinell):** Die Karten je Tag springen nach dem Start nicht mehr auf den Deckel.
- **Creator – Regressionszeuge:** kein Creator-Pfad im Diff, Suite grün.

## Retrospektive (Sprint 137)

**Nachschau:** Sprint 135 (B-940…B-943, `1b92cd02`) gelesen. Befund an B-941:
[B-947](backlog/B-947-ausgelassen-hinweis-verschwindet-nach-dem-start.md) – der Hinweis „N Übungen ohne Ausführ-Recht ausgelassen“
zählt gegen den Live-Stand, das Bündel ist nach dem Start aber eingefroren; der geratene Ausweg (Freigabe holen) wirkt danach nicht.
B-940, B-942, B-943 ohne Befund; dazu B-757 nachgesehen (→ B-946). `nachgeschaut: 2026-11-24` an allen. Index: Nachgeschaut
568 von 607.

**Was durchgelassen wurde:** Die Regel zu Fehlermeldungen mit Ausweg (pm-loop Step 5) deckte Hinweise/Zähler nicht, deren Grund
einen Ausweg nahelegt, und keine Reihenfolge „einmaliger Übergang, dann Ausweg“.

**Der eine Mechanismus (gelandet):** pm-loop Step 5 – ein Hinweis oder Zähler, dessen Grund einen Ausweg nahelegt, gilt als
Ratschlag; ein Test nimmt den Ausweg **nach jedem einmaligen Übergang** (Start, Einfrieren) und prüft die Wirkung, nicht das
Verschwinden des Hinweises. Prosa im treibenden Skill, kein Tor – darum kein Halt.

**Bewusst ungedeckt:** B-947 selbst braucht eine Produktentscheidung (nachziehen oder ehrlich zählen) und bleibt `idee`.
Nachgetragen in Sitzung 2 vom 2026-11-24: Die erste Sitzung endete nach dem Code-Commit, bevor Protokoll und Story-Dateien
committet waren.

## Offene Roadmap (priorisiert)

Die Liste, die gilt, ist `docs/backlog/`. Begründung der Reihenfolge:

1. **B-752:** Die Migration `ClassTestPrepDuty` an einer Kopie der echten DB proben (AK 8, Nutzer), bevor der Heimnetz-Server sie
   einspielt. Danach bleibt nur der Blick am Gerät, ob die Last fair wirkt. B-757 und B-758 sind seit Runde 136 abgenommen.
2. **B-947** (P2, Dialog): Nach dem Start rät der Hinweis einen Ausweg, der nichts bewirkt. B-945/B-946 sind abgenommen.
3. **B-944** Restpunkte, darunter die nicht deterministisch messbare Nebenläufigkeit.

## Konkreter Änderungsstand (für Review)

`1b92cd02` – Code, Tests, Vertrag, generierte Doku, E2E. Danach ein Doku-Commit: Stories, dieses Protokoll, pm-loop-Regel,
B-945, Nachschau.

Runde 136: ein Test (`Schlagwort_Verknuepfen_Im_Fenster…`), Belege und Abnahme von B-757/B-758 – eigener Commit.
