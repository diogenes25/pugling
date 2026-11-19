---
tags: [typ/story, status/ausformuliert, lerntechnik/vokabeln, rolle/student]
aliases: [Scope New und Lernkarte, offene Lernkarte verschwindet]
status: ausformuliert
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

1. **Wie weit gilt „neu“ jetzt?** Empfehlung: `New` = nie eingeführt, heute eingeführt **oder** eingeführt, aber noch nicht
   eingeprägt, wo der Inhalt eine Lernkarte braucht. `Old` bleibt „irgendwann eingeführt“ (dort fällt das Wort nicht heraus).
2. **Zählt ein solches Wort auf die Tagesdosis?** B-643 Entscheidung 2 sagt ja („nimmt einen Platz der neuen Wörter“).
   Empfehlung: ja – dann muss die Zählung in `OpenLearnCards` nach dem erweiterten Filter dieselbe bleiben (heute wird
   hinter dem Filter gezählt).
3. **Wer reicht Typ und Atom in den Filter?** Empfehlung: `ScopeMatch` bekommt eine Delegate/Lambda, ob das Atom offen ist
   (die Typen und die Atome liegen in `DueItemIndicesAsync` schon vor), statt dass `ScopeMatch` selbst nachschlägt.
4. **Ist es ein Defekt von B-643 oder von B-435?** Empfehlung: Defekt belassen; B-643 AK 5 ist die Soll-Zeile.

## Entscheidungen

Keine – eine Defekt-Story geht erst mit `gegrillt` in die Entscheidungen.

## Akzeptanzkriterien

Entwurf; je Kriterium die Änderung, die es rot macht.

1. Position mit `Scope = New`: ein Wort, das gestern eingeführt und nicht eingeprägt wurde, steht heute als Lernkarte in
   der Runde. Rot, wenn `ScopeMatch` wie heute nur `IntroducedAt == day` zulässt.
2. Dasselbe Wort verschwindet aus dem Umfang, sobald es eingeprägt ist (am Folgetag nicht mehr in der Runde). Rot, wenn
   der Filter jedes eingeführte Wort durchlässt (`UmfangNeuTests`, AK 2 bleibt dafür grün).
3. Ein eingeführtes Wort ohne Lernkarte (Typ ohne `NeedsImprint`, Erkennungskarte eines „both“-Worts) bleibt am Folgetag
   draußen. Rot, wenn die Erweiterung nur `ImprintedAt is null` prüft (dann bliebe es für immer drin).
4. Die offenen Wörter zählen auf die Tagesdosis: bei Dosis 6 und 2 offenen kommen 4 neue. Rot, wenn die Zählung vor dem
   Filter statt dahinter steht oder entfällt.
5. `Scope = Old` und `All` ändern sich nicht (`UmfangNeuTests`, vorhanden). Rot, wenn die Erweiterung auf alle Scopes
   wirkt.

## Verlauf

- **2026-11-18** — angelegt (Quelle: Nachschau von B-643, `HEAD` 7c642bb9).
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): am Code bestätigt; Zeilen des Filters wanderten von
  `:614-616` nach `:621-625`; Lücke geschärft (nur Inhalte mit `NeedsImprint`, Dosis-Zählung hängt hinter dem Filter,
  der Tagestest ignoriert den Scope). Kein Test für den Fall vorhanden.
