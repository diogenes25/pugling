---
tags: [typ/story, status/abgenommen, lerntechnik/vokabeln, rolle/student]
aliases: [Scope New und Lernkarte, offene Lernkarte verschwindet]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: "PM-Sitzung 2026-11-18, Nachschau von B-643 (e10cdd34)"
entgangen_bei: [B-643]
grund: ""
ersetzt_durch: []
---

# B-653 · Bei „nur neue Inhalte“ kommt ein nicht eingeprägtes Wort nie wieder als Lernkarte

Gefunden in der Nachschau von [B-643](B-643-lernkarte-leichter-schalten-und-runden-bis-alles-erinnert.md): bei
`ItemScope.New` lässt der Filter nur nie oder heute eingeführte Inhalte durch, der Zustand „eingeführt, aber nicht
eingeprägt“ (B-643) fällt ab dem Folgetag heraus.

**Warum P3:** die Plan-Skripte der Söhne setzen keinen Scope (Standard `All`); der Fall braucht eine Position, die der
Vater ausdrücklich auf „nur neue“ stellt.

## User Story

Als Schüler möchte ich ein Wort, das ich bei der Einführung nicht aus dem Kopf schreiben konnte, am nächsten Tag wieder als
Lernkarte bekommen, auch wenn der Vater die Position auf „nur neue Inhalte“ gestellt hat, damit ein halb gelerntes Wort
nicht still aus meinem Plan fällt.

## Ist-Stand am Code

- **Der Filter:** `ScopeMatch` lässt `New` durch, wenn `prog?.IntroducedAt is null || prog.IntroducedAt == day`
  (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:621-625`; Kommentar `:615-620`). Er bekommt nur den
  Fortschritt, nicht den Typ und nicht das Inhalts-Atom.
- **Einziger Aufrufer:** `DueItemIndicesAsync` (`PositionPlayService.cs:286`); auch der Lern-Pfad (`PositionPracticeController.cs:129`)
  und der Tagesstatus (`PositionProgressService.cs:328`) laufen darüber. Eine zweite Auswahl der Lernkarten: nicht gefunden.
- **Der Zustand, den der Filter nicht kennt:** `PositionItemProgress.ImprintedAt` (`PlanPositionEntities.cs:269`), gesetzt
  durch eine aus dem Kopf richtig gewusste Lernkarte (`PositionPracticeController.cs:890`) bzw. im Test (`PositionTestsController.cs:334`).
  Ob ein Inhalt überhaupt eine Lernkarte braucht, sagt `NeedsImprint(type, item)` (`PositionPlayService.cs:176-177`) – nicht
  jeder Typ und nicht jede Karte (die Erkennungskarte eines „both“-Worts nicht), `ImprintedAt` ist dort `null` und bleibt es.
  Der Zustand „offen“ ist dasselbe wie in `OpenLearnCards` (`:349-354`, `introduced vor heute && (ImprintedAt null oder heute)
  && NeedsImprint`).
- **Der Widerspruch zu B-643 steht in der Dosis:** `OpenLearnCards` zählt nur Wörter, die den Filter schon passiert haben
  (`due.Select(...)`, `:292-296`); der Kommentar `:287-291` nennt das ausdrücklich („a word the scope … keeps out takes no
  place“). Der Test-Anteil (`DailyTestPortion`, `:389`) ignoriert den Scope und fragt solche Wörter kalt – siehe unten.
- **Der Test deckt die Lücke nicht ab:** `UmfangNeuTests` setzt jedes Wort sofort mit `wasKnown: true` ein
  (`UmfangNeuTests.cs:46-51`, Kommentar „a word only seen would come back as a learn card“) – dort ist es eingeprägt; der Fall
  „eingeführt, nicht eingeprägt, Folgetag, Scope New“ ist nicht gefunden. `UmfangNeuTests.cs:76-83` hält fest, dass
  eingeprägte Wörter am Folgetag draußen bleiben.

## Die echte Lücke

Das Szenario der Idee stimmt am Code: Wort am Tag 1 eingeführt, nicht eingeprägt → Tag 2: `IntroducedAt != day`, der
Filter wirft es hinaus; es kommt weder als Lernkarte noch sonst. Ob es im **Test** auftaucht, ist ein zweiter Befund: der
Tagestest (`DailyTestPortion`) nimmt es (`:389-408`, eingeführt und fällig) ohne Scope – nicht nachgestellt, nur gelesen.
Heißt: das Kind wird am Folgetag das Wort abgefragt, bekommt es aber nie wieder gezeigt.
Schmaler als „New verliert die Lernkarte“: die Korrektur betrifft nur Inhalte, die eine Lernkarte brauchen
(`NeedsImprint`), und nur bis zum Einprägen.

## Offene Punkte

1. ~~**Wie weit gilt „neu“ jetzt?**~~ → Entscheidung 1.
2. ~~**Zählt ein solches Wort auf die Tagesdosis?**~~ → Entscheidung 2.
3. ~~**Wer reicht Typ und Atom in den Filter?**~~ → Entscheidung 3.
4. ~~**Ist es ein Defekt von B-643 oder von B-435?**~~ → Entscheidung 4.
5. **Fragt der Tagestest ein offenes Wort weiterhin kalt?** Zurückgestellt: Der Test-Anteil (`DailyTestPortion`,
   `PositionPlayService.cs:389-408`) ignoriert den Scope und ist Anti-Cheat-Teil von B-04. Er gehört nicht zur Story
   (Kartenwahl in der Lernrunde) und wird hier nicht angefasst; fällt am Kind auf, dass es ein Wort abgefragt, aber nie
   gezeigt bekommt, ist das eine eigene Story.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

Keine Malus- oder Münzregel ist neu; die Änderung bringt `ItemScope.New` mit dem Zustand „eingeführt, nicht eingeprägt“
(B-643) in Einklang, den die Dosis schon kennt.

1. **`New` lässt zusätzlich einen offenen Lernkarten-Inhalt durch, mit genau dem Prädikat von `OpenLearnCards`:** eingeführt
   vor dem Tag, der Inhalt braucht eine Lernkarte (`NeedsImprint`) und `ImprintedAt` ist `null` **oder** der Tag
   (`PositionPlayService.cs:352-353`). Das Prädikat steht danach an **einer** Stelle als statische Hilfsmethode, die
   `OpenLearnCards` und `ScopeMatch` gemeinsam nutzen. *Begründung:* Zwei Größen werden gleichgesetzt – „das Wort ist im
   Umfang“ und „das Wort zählt auf die Dosis“. Belege: die Dosis zählt es an `OpenLearnCards` (`:349-354`), der Umfang
   schließt es heute über `ScopeMatch` aus (`:621-625`); ein gemeinsames Prädikat macht ein Auseinanderlaufen unmöglich.
   Gegenfundstelle: der Tagestest rechnet bewusst ohne Scope (`:389-408`), die Gleichsetzung gilt also nur für die
   Lernrunde. Das Wort bleibt im Umfang, **bis es eingeprägt ist** – nicht nur am Folgetag. *Kosten:* Ein Kind, das ein
   Wort nie einprägt, bekommt es täglich wieder und es belegt täglich einen Platz der Dosis (dasselbe wie unter `All`,
   B-643 Entscheidung 2). `Old` und `All` ändern sich nicht. *Verworfene Alternative:* nur `ImprintedAt is null` prüfen
   (kürzer): dann fiele ein am selben Tag eingeprägtes Wort aus dem Umfang und aus der Dosis-Zählung, und eine zweite
   Runde brächte neue Wörter über die Dosis – sofern es nach dem Einprägen noch in `due` liegt (hängt an `DueOn`, nicht
   nachgestellt; der Bau misst es, die Gleichheit mit `:352-353` bleibt der Grund). *Ausgeschlossener Fall (gegen die User
   Story gerechnet):* Ein eingeführtes Wort **ohne** Lernkarte (Typ ohne `IntroductionStage`, Erkennungskarte eines
   „both“-Worts) bleibt am Folgetag draußen, wie B-435 Entscheidung 1 es für „neu“ festgelegt hat – dort gibt es keinen
   halb gelernten Zustand, der still aus dem Plan fiele. Das Wort einer Position, deren `Exercise` nicht geladen ist
   (kein Typ), fällt auf das heutige Verhalten zurück, wie `OpenLearnCards` (`:351`).
2. **Das Wort zählt auf die Tagesdosis, wie bei B-643 Entscheidung 2:** bei Dosis 6 und 2 offenen kommen 4 neue.
   *Begründung:* Mit Entscheidung 1 liegt das Wort im Filterergebnis, hinter dem `OpenLearnCards` zählt
   (`PositionPlayService.cs:294-296`) – die Zählung ändert sich nicht, sie wirkt jetzt auch unter `New`. Heute lässt `New`
   das Wort draußen und die Dosis gibt 6 neue her (Dosis-Leck). *Kosten:* keine zusätzlichen; die Reihenfolge Filter, dann
   Zählung bleibt (Rot-Bedingung in Kriterium 4).
3. **Typ und Inhalt kommen aus `DueItemIndicesAsync` in `ScopeMatch`, nicht per Nachschlagen.** `ScopeMatch` bekommt den
   Typ und die Inhaltsliste durchgereicht (beide liegen dort schon vor: `items` in `:276`, der Typ wie in
   `OpenLearnCards`), bleibt statisch und ohne `db`. *Begründung:* Eine zweite Auswahl der Lernkarten gibt es nicht
   (nicht gefunden: `DueItemIndicesAsync` ist der einzige Aufrufer von `ScopeMatch`, `:286`; die Aufrufer von
   `DueItemIndicesAsync` sind `PositionPracticeController.cs:129` und `PositionProgressService.cs:328`) – eine Änderung
   an dieser Stelle wirkt auf Lernrunde **und** Tagesstatus. *Kosten:* `ScopeMatch` bekommt zwei Parameter; der Tagesstatus
   (`dueCount`) zählt das offene Wort mit, wie die Runde es spielt. *Verworfene Alternative:* der Controller filtert nach
   – dann sähe der Tagesstatus das Wort nicht und beide zeigten verschiedene Zahlen.
4. **Der Defekt bleibt Defekt von B-643** (`entgangen_bei: [B-643]`); B-643 AK 5 („Nicht eingeprägte Wörter kommen am
   nächsten Tag wieder als Lernkarte“) ist die Soll-Zeile. *Begründung:* B-643 hat den Zustand eingeführt und `New` nicht
   mitgeführt. *Kosten:* keine.

**Überlappung:** [B-435](B-435-umfang-nur-neue-fragt-nach-der-einfuehrung-nie.md) Entscheidung 1 (abgenommen) nannte als
Kosten, wer am Einführungstag nicht mehr abfrage, werde unter „nur neue“ später nicht mehr gefragt. Das bleibt für
Inhalte ohne Lernkarte so; für Inhalte mit Lernkarte wird es durch B-643 überholt, weil „abgefragt“ jetzt „eingeprägt“
heißt. B-435 wird nicht umgeschrieben, die Verfeinerung steht hier. [B-643](B-643-lernkarte-leichter-schalten-und-runden-bis-alles-erinnert.md)
(abgenommen) wird erfüllt, nicht geändert. Keine offene Story überschneidet sich.

## Akzeptanzkriterien

Jedes Kriterium nennt die Änderung, die es rot macht. Testklasse `UmfangNeuTests` (der Aufbau `IntroduceAsync` mit
`wasKnown: true` bekommt eine Schwester, die ein Wort nur einführt und nicht einprägt) bzw. `LernkarteTests` für die Dosis.

1. Position mit `Scope = New`: ein Wort, das gestern eingeführt und nicht eingeprägt wurde, steht heute als Lernkarte in
   der Runde. Rot, wenn `ScopeMatch` wie heute nur `IntroducedAt == day` oder nie eingeführt zulässt.
2. Dasselbe Wort steht auch am übernächsten Tag noch in der Runde, solange es nicht eingeprägt ist, und verschwindet am Tag
   nach dem Einprägen. Rot, wenn der Filter nur den Folgetag zulässt (`IntroducedAt == day - 1`), und rot, wenn er jedes
   eingeführte Wort durchlässt (`UmfangNeuTests`, Fall `Neu_Mit_Leitner_Laesst_Am_Folgetag_Nur_Die_Nie_Eingefuehrten_Im_Umfang`
   bleibt dafür grün).
3. Ein eingeführtes Wort ohne Lernkarte (Typ ohne `NeedsImprint`, Erkennungskarte eines „both“-Worts) bleibt am Folgetag
   draußen. Rot, wenn die Erweiterung nur `ImprintedAt is null` prüft und `NeedsImprint` weglässt (dann bliebe es für immer
   drin, weil `ImprintedAt` dort `null` bleibt).
4. Die offenen Wörter zählen auf die Tagesdosis: bei Dosis 6 und 2 offenen kommen 4 neue. Rot, wenn die Zählung vor dem
   Filter statt dahinter steht oder entfällt (heute unter `New`: 6 neue). Dazu eine zweite Runde am selben Tag, nachdem
   ein offenes Wort in der ersten eingeprägt wurde: sie bringt keine neuen über die Dosis; rot, wenn das Prädikat
   `ImprintedAt == day` auslässt – sofern das Wort danach noch fällig ist (der Bau misst es und streicht den Satz sonst).
5. `Scope = Old` und `All` ändern sich nicht (`UmfangNeuTests`, vorhanden). **Absichtserklärung:** `All` lässt ohnehin
   jedes Wort durch und `Old` jedes eingeführte; eine auf alle Scopes ausgedehnte Erweiterung wäre in der Runde nicht zu
   sehen. Rot wird nur der Fall, dass jemand `Old` auf „nur nicht eingeprägt“ verengt – dann fehlte dort ein eingeprägtes
   Wort in der Runde (der vorhandene `Old`-Fall in `UmfangNeuTests`).
6. Der Tagesstatus (`dueCount`, `PositionProgressService.cs:328`) zählt das offene Wort mit der Runde mit. Rot, wenn der
   Filter außerhalb von `DueItemIndicesAsync` sitzt.

## Schätzung

**Größe: S** — eine Datei (`PositionPlayService.cs`): ein gemeinsames Prädikat herausziehen, `ScopeMatch` um zwei
Parameter erweitern, ein Aufrufer. Der Aufwand liegt in den Tests, nicht im Code; kleiner als der Batch-Pfad im
`MediaSelector` (M), weil kein neuer Pfad entsteht.

- **`migration: nein`** — `PositionItemProgress.IntroducedAt` und `ImprintedAt` gibt es (`PlanPositionEntities.cs:269`);
  gelesen, nicht geschrieben.
- **`vertragsbruch: nein`** — kein Vertragstyp berührt; nur der Inhalt von `dueCount` im Tagesstatus ändert sich
  (`PositionProgressService.cs:328`), nicht seine Form.
- **`wo: backend`** — das Frontend zeigt, was die Runde liefert.

### Risiken

- **Zwei Aufrufer, eine Änderung:** `DueItemIndicesAsync` speist Lernrunde (`PositionPracticeController.cs:129`) und
  Tagesstatus (`PositionProgressService.cs:328`); beide ändern sich zugleich (Entscheidung 3, AK 6).
- **Dosis-Reihenfolge:** `OpenLearnCards` zählt hinter dem Filter (`PositionPlayService.cs:294-296`); gelangt das offene Wort
  jetzt durch den Filter, zählt es doppelt, wenn das Prädikat nicht an einer Stelle steht (AK 4).
- **Ungemessen:** die zweite Runde am selben Tag (AK 4, zweiter Satz) hängt an `DueOn` nach dem Einprägen; die Story
  verlangt, den Satz zu streichen, wenn der Bau zeigt, dass das Wort danach nicht mehr fällig ist.
- **Tagestest bleibt unberührt** (Offener Punkt 5): `DailyTestPortion` fragt solche Wörter weiter kalt; wer das ändern
  will, eröffnet eine eigene Story.
- **Ohne `Exercise`/Typ** (nicht geladen) fällt der Filter auf das heutige Verhalten zurück (Entscheidung 1); ein Fall
  für den Bau, damit das nicht still den Filter öffnet.

### Angriffsplan

1. Prädikat „offene Lernkarte“ aus `OpenLearnCards` (`:352-353`) als statische Hilfsmethode herausziehen; `OpenLearnCards`
   ruft sie auf – Verhalten unverändert, vorhandene Tests grün.
2. `ScopeMatch` (`:621`) bekommt Typ und Inhaltsliste; `New` lässt zusätzlich das offene Wort durch;
   `DueItemIndicesAsync` (`:286`) reicht beides durch.
3. Tests (siehe Testweg), je Kriterium vorher rot gemessen.
4. `dotnet build Pugling.sln` und `dotnet format Pugling.sln`.

### Testweg

- `UmfangNeuTests` (`backend/Pugling.Api.Tests/UmfangNeuTests.cs`): Schwester zum Aufbau `IntroduceAsync` mit
  `wasKnown: true` (`:46-51`), die ein Wort nur einführt; AK 1, 2, 3 und 5. Rot heute: AK 1 und AK 2 (der Filter wirft
  das Wort am Folgetag hinaus); AK 3 ist im Ausgangszustand grün und trägt erst gegen die Erweiterung, die
  `NeedsImprint` weglässt (rote Probe über diesen Mutanten, nicht über den Ausgangszustand); AK 5 ist als
  Absichtserklärung beschriftet.
- `LernkarteTests` (`backend/Pugling.Api.Tests/LernkarteTests.cs`): AK 4, Dosis 6 mit 2 offenen ergibt 4 neue – heute
  6 neue (Dosis-Leck), also vorher rot.
- AK 6 (Tagesstatus, `dueCount`): ein Test, der das offene Wort im Tagesstatus mitzählt. Im Test-Projekt kommt `dueCount`
  nirgends vor (Grep ohne Fund); der Test gehört vermutlich nach `KindTagessichtTests`, die Heimat ist im Bau zu klären.
- Vor der Abnahme: `pugling-reviewer`; Rollengang als Sohn an einer Position mit „nur neue“ und einem am Vortag nicht
  eingeprägten Wort.

## Verlauf

- **2026-11-18** — angelegt (Quelle: Nachschau von B-643, `HEAD` 7c642bb9).
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): am Code bestätigt; Zeilen des Filters wanderten von
  `:614-616` nach `:621-625`; Lücke geschärft (nur Inhalte mit `NeedsImprint`, Dosis-Zählung hängt hinter dem Filter,
  der Tagestest ignoriert den Scope). Kein Test für den Fall vorhanden.
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet), stellvertretend und unbestätigt: vier Entscheidungen (`New` lässt
  offene Lernkarten-Inhalte mit demselben Prädikat wie `OpenLearnCards` durch, Dosis zählt sie, Typ und Inhalt reicht
  `DueItemIndicesAsync` durch, Defekt bleibt B-643 zugeordnet); Tagestest-Scope ausdrücklich zurückgestellt;
  Akzeptanzkriterien final. Verfeinert B-435 Entscheidung 1, ändert sie nicht.
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet): S, backend, keine Migration, kein Vertragsbruch; Filter,
  Prädikat und beide Aufrufer am Code nachgelesen (`PositionPlayService.cs:286`, `:352-353`, `:621`); Testort von AK 6
  offen.
- **2026-11-21** — in-arbeit (Nachtlauf, Sprint 106). Gebaut: `IsOpenLearnCard` als gemeinsames Prädikat von
  `OpenLearnCards` und `ScopeMatch` (`PositionPlayService.cs:358-361`, `:633-634`), `DueItemIndicesAsync` reicht Typ und
  Inhalte durch (`:286-287`). Entscheidung 1–3 ohne Abweichung; ohne Typ bleibt die Tagesregel.
  - **Rote Proben vor dem Fix** (Debug, 5 von 9 rot): AK 1 erwartet `[0, 2]`, gemessen `[2]`; AK 6 erwartet `dueCount` 2,
    gemessen 1; AK 4 erwartet Karten 0..5, gemessen 2..7; AK 4 zweite Runde: Wörter ≥ 6 kamen; AK 3 (Fall in
    `LernkarteTests`) rot nur am `Contains(1)` – sein Unterscheidungswert liegt in Q3/Q3b unten.
  - AK 1, Folgetag → `UmfangNeuTests.Neu_Bringt_Ein_Nicht_Eingepraegtes_Wort_Als_Lernkarte_Zurueck_Bis_Es_Sitzt`, Q1 → rot.
  - AK 2, übernächster Tag → derselbe Fall, Q2 (nur Folgetag) → rot; AK 2, jedes eingeführte Wort → Q6 → rot.
  - AK 3, Erkennungskarte bleibt draußen → `LernkarteTests.Neu_Laesst_Eine_Erkennungskarte_Ohne_Lernkarte_Am_Folgetag_Draussen`,
    Q3 (Prädikat ohne `NeedsImprint`) und Q3b (Umfang nur `ImprintedAt is null`) → rot.
  - AK 4, Dosis → `LernkarteTests.Neu_Offene_Lernkarten_Zaehlen_Auf_Die_Tagesdosis`, Q4 (Zählung entfällt) → rot; zweiter
    Satz gemessen statt gestrichen: das Wort bleibt nach dem Einprägen am selben Tag fällig →
    `Neu_Zweite_Runde_Am_Selben_Tag_Bringt_Keine_Neuen_Ueber_Die_Dosis`, Q5 (ohne `ImprintedAt == day`) → rot.
  - AK 5, `Old`/`All` → vorhandene Fälle in `UmfangNeuTests` und `LernkarteTests`, Absichtserklärung wie in der Story.
  - AK 6, Tagesstatus → `UmfangNeuTests.Neu_Tagesstatus_Zaehlt_Die_Offene_Lernkarte_Mit`, Q1 → rot. Die Form „Filter
    außerhalb von `DueItemIndicesAsync`“ ist nicht als Mutante gemessen (kein Einzeilen-Eingriff); der Fall fällt mit jeder
    Form, die den Tagesstatus ohne die Regel lässt.

| Mutante | Stelle | erwartet | gemessen | Anmerkung |
|---|---|---|---|---|
| Q1 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:635 | red | red |  |
| Q2 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:361 | red | red |  |
| Q3 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:362 | red | red |  |
| Q3b | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:635 | red | red |  |
| Q4 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:297 | red | red |  |
| Q5 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:361 | red | red |  |
| Q6 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:635 | red | red |  |

Gemessen nach der letzten Änderung (Review-Nacharbeit) an `7da612ee + Arbeitsbaum geändert`, 2026-11-21 21:34, mit `fails` je Zeile (fällt am benannten Fall).
- **2026-11-21** — Review (`pugling-reviewer`): kein 🔴/🟡; drei 🟢 übernommen (ohne Zählung): doppelter Tageshelfer in `UmfangNeuTests` zusammengelegt, Klassen-Doku um die Lernkarten-Ausnahme ergänzt, `OpenLearnCards`-Doku korrigiert (der Testanteil liest keine offenen Karten – einziger Aufrufer `PositionPlayService.cs:297`). Mutanten danach neu gemessen (Tabelle oben). Volle Suite (Release) 1990/1990.
- **2026-11-21** — abgenommen (Sprint 106). Rollengang: Sohn über die API im Integrationsfall (In-Process-Server aus dem Stand nach der letzten Änderung): Wort am Folgetag und übernächsten Tag als Lernkarte, Dosis 2 offen + 4 neu, Tagesstatus 2. Keine UI-Änderung, darum keine eigene Spec; `full-flow` E2E 1 passed (Server frisch auf 5201/5174). Vater und Creator: Regressionszeugen, keine ihrer Flächen im Diff. Commit `f72b9638`.
