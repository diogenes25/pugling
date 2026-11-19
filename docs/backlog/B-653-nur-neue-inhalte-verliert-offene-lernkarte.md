---
tags: [typ/story, status/gegrillt, lerntechnik/vokabeln, rolle/student]
aliases: [Scope New und Lernkarte, offene Lernkarte verschwindet]
status: gegrillt
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
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

## Verlauf

- **2026-11-18** — angelegt (Quelle: Nachschau von B-643, `HEAD` 7c642bb9).
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): am Code bestätigt; Zeilen des Filters wanderten von
  `:614-616` nach `:621-625`; Lücke geschärft (nur Inhalte mit `NeedsImprint`, Dosis-Zählung hängt hinter dem Filter,
  der Tagestest ignoriert den Scope). Kein Test für den Fall vorhanden.
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet), stellvertretend und unbestätigt: vier Entscheidungen (`New` lässt
  offene Lernkarten-Inhalte mit demselben Prädikat wie `OpenLearnCards` durch, Dosis zählt sie, Typ und Inhalt reicht
  `DueItemIndicesAsync` durch, Defekt bleibt B-643 zugeordnet); Tagestest-Scope ausdrücklich zurückgestellt;
  Akzeptanzkriterien final. Verfeinert B-435 Entscheidung 1, ändert sie nicht.
