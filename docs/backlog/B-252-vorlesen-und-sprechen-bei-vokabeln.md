---
tags: [typ/story, status/geschaetzt, bereich/frontend, bereich/vokabeln, rolle/student]
aliases: [Vokabel vorlesen, Spracheingabe]
status: geschaetzt
prio: P3
art: Wunsch
groesse: S
wo: beides
migration: nein
vertragsbruch: ja
quelle: docs/knowunity-vergleich-plan.md#wo-knowunity-weiter-ist
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-252 · Vokabeln vorlesen lassen und optional per Stimme beantworten

Knowunity hat Sprach-Wiederholung im Plan und einen Sprachmodus. Bei uns gibt es Ton nur, wo ein Creator
Audio hochlädt (Übungstyp Hörverstehen); eine Vokabelkarte kann ihre Aussprache nicht hören lassen. Die
Idee: ein Lautsprecher-Knopf an der Vokabelkarte liest das Wort über die Browser-Sprachausgabe
(`speechSynthesis`) in der Zielsprache vor – reine Frontend-Arbeit, keine Medien, kein Vertrag. Als
zweiter Schritt optional eine Spracheingabe als Antwortweg. Vorher den Klang-Vorbehalt aus
[B-31](B-31-geraete-vorbehalt-klang.md) prüfen. Anti-Cheat-Frage: Vorlesen darf auf der Abfrageseite
nur das Gefragte sprechen, nie die Lösung – sonst ist es ein Lösungskanal am Server vorbei.

## User Story

Als *Sohn* (Friedrich, LRS) möchte ich ein französisches Wort auf der Karte antippen und hören können, auch wenn
niemand eine Aufnahme hochgeladen hat, damit ich die Aussprache lerne und nicht nur das Schriftbild rate. Als *Vater*
möchte ich, dass das Vorlesen nie verrät, was gefragt ist.

## Ist-Stand am Code

1. **Die Aufnahme kommt nur in der Hörstufe auf die Karte.** `VocabularyExerciseType.StageFacets` gibt die
   `AudioUrl` nur für `TestStage.Audio` heraus (`backend/Pugling.Api/Exercises/VocabularyExerciseType.cs:118-122`),
   dort ersetzt sie den Text (`:140`, angewandt in `PositionPlayService.CardFacets`). In der Arcade erscheint der
   Abspielknopf nur bei `karte.audioUrl` (`frontend/src/sohn/Lektion.tsx:93`). Auf allen anderen Stufen hat eine
   Vokabelkarte also gar keinen Ton – mit oder ohne Aufnahme.
2. **Woher die Aufnahme stammt:** `PronunciationAudioUrl` der Vokabel wird zur `ContentItem.AudioUrl`
   (`ExerciseContentResolver.cs:102`); in der umgekehrten Richtung (Deutsch → Fremdsprache) wird sie gelöscht
   (`ExerciseContentProvider.cs:104`, `Reversed = true`), weil dort das Fremdwort die Lösung ist.
3. **Die Karte kennt ihre Sprache nicht.** `PracticeCard` (`backend/Pugling.Contracts/Student/PracticeDtos.cs:81-86`)
   trägt kein Sprachfeld; die Sprache steht nur am Store-Eintrag (`Vocabulary.SourceLanguage` für `Word`,
   `Models/VocabEntities.cs:17-20`) und wird im Resolver gelesen (`ExerciseContentResolver.cs:98`), aber nicht
   weitergegeben. Ohne Sprache liest der Browser „la gare“ mit der deutschen Standardstimme – falsche Aussprache ist
   schlimmer als keine.
4. **`speechSynthesis` kommt im Frontend nicht vor**; der Abspielknopf ist `frontend/src/components/AudioButton.tsx`.

## Die echte Lücke

Der Rahmen ist entschieden (Vorlesen nur ohne Aufnahme, nie in der Hörstufe, keine Spracheingabe), aber der Client
kann ihn nicht allein umsetzen: Er weiß weder, in welcher Sprache das Wort steht, noch ob es eine Aufnahme gibt
(außerhalb der Hörstufe kommt auch eine vorhandene Aufnahme nicht mit, Ist-Stand 1), noch ob der Text auf der Karte
das Fremdwort oder die deutsche Seite ist. Alle drei Fragen beantwortet nur der Server.

## Offene Punkte

1. ~~Spracheingabe?~~ → nein, Entscheidung vom 2026-11-05 (1).
2. ~~Woran erkennt man „keine Aufnahme“?~~ → leere `PronunciationAudioUrl`, Entscheidung vom 2026-11-05 (2); am Server
   gelesen, Entscheidung 1.
3. ~~Woher kennt der Client die Sprache?~~ → Entscheidung 1.
4. ~~Was wird gesprochen?~~ → Entscheidung 2.
5. ~~Automatisch oder auf Antippen?~~ → Entscheidung 3.
6. ~~Was, wenn das Gerät keine passende Stimme hat?~~ → Entscheidung 4.
7. ~~Test und Bonus-Training auch?~~ → Entscheidung 5.

## Entscheidungen

Übernommen (nicht umgestoßen): die Entscheidungen des Nutzers vom 2026-10-23 und die im Auftrag des Nutzers vom
2026-11-05 – Vorlesen nur als Rückfall ohne Aufnahme, nie in der Hörstufe, keine Spracheingabe, „keine Aufnahme“ =
leere `PronunciationAudioUrl`, Klang-Urteil beim Gerätecheck (B-31).

1. **Der Server sagt, ob und in welcher Sprache vorgelesen werden darf: `PracticeCard.SpeechLang` (`string?`).**
   Gesetzt genau dann, wenn (a) die Karte eine Vokabel in Leserichtung ist (nicht `Reversed`), (b) die Vokabel
   **keine** `PronunciationAudioUrl` hat, (c) die Stufe nicht die Hörstufe ist (`AudioReplacesPrompt` falsch) und (d)
   `Vocabulary.SourceLanguage` nicht leer ist; Wert = diese Sprache (z. B. `fr`). Stakeholder-Entscheidung im Auftrag
   des Nutzers. Begründung: Nur der Server kennt Sprache, Richtung und Aufnahme (Ist-Stand 3); die Regel steht damit
   an **einer** Stelle neben den übrigen Anti-Cheat-Facetten. Das stößt die Entscheidung „kein neues Vertragsfeld“
   vom 2026-11-05 nicht um – die galt dem Signal „keine Aufnahme“, und das bleibt die leere `PronunciationAudioUrl`,
   nur am Server gelesen. Kosten: ein additives Vertragsfeld (`contract.ts` neu erzeugen), die Story ist nicht mehr
   „reine Frontend-Arbeit“.
2. **Gesprochen wird ausschließlich der Text, der schon auf der Karte steht (`prompt`)** – in Leserichtung das
   Fremdwort selbst. Nie `reveal`, nie eine Antwort. Stakeholder-Entscheidung im Auftrag des Nutzers. Begründung: So
   kann das Vorlesen nichts verraten, was das Kind nicht schon liest; in der Gegenrichtung (Deutsch gefragt) gibt es
   bewusst keinen Knopf. Kosten: In der Richtung Deutsch → Fremdsprache bleibt die Karte stumm.
3. **Nur auf Antippen, nie automatisch.** Stakeholder-Entscheidung im Auftrag des Nutzers. Begründung: Eine plötzlich
   sprechende Computerstimme im Klassenraum oder abends ist eine Störung; die Aufnahme in der Hörstufe spielt an,
   weil sie dort die Aufgabe *ist* – das Vorlesen ist eine Hilfe. Kosten: Ein Tipp mehr.
4. **Kein Knopf ohne passende Stimme.** Der Knopf erscheint nur, wenn `window.speechSynthesis` existiert und
   `getVoices()` eine Stimme liefert, deren `lang` mit `SpeechLang` beginnt (Stimmen laden asynchron, darum auch auf
   `voiceschanged` hören). Stakeholder-Entscheidung im Auftrag des Nutzers. Begründung: Eine deutsche Stimme, die
   Französisch liest, lehrt falsche Aussprache (Ist-Stand 3). Kosten: Auf manchen Geräten bleibt der Knopf weg; ob die
   Stimme *gut* klingt, prüft der Gerätecheck B-31, nicht diese Story.
5. **Kern ist die Übungskarte (`PracticeCard`), Test und Bonus-Training sind zurückgestellt.** Stakeholder-Entscheidung
   im Auftrag des Nutzers. Begründung: klein schneiden – das Üben ist der Ort, an dem neue Wörter kennengelernt
   werden; Test und Bonus haben eigene Karten-DTOs (`TestItem`, `ExtraCard`). Kosten: Im Test und im Bonus bleibt die
   Karte stumm.

**Zurückgestellt** (keine eigene Story, beim nächsten Anlass wieder aufnehmen):

- Vorlesen auch auf `TestItem` und `ExtraCard` (Entscheidung 5).
- Vorhandene Aufnahme auch außerhalb der Hörstufe anbieten (Ist-Stand 1) – eine Anti-Cheat-Frage am
  `StageFacets`, nicht Teil des Rückfalls.
- Vorlesen der Auflösung nach dem Aufdecken (Kennenlernkarte, Rückseite) in der Gegenrichtung.
- Spracheingabe (2026-11-05: nein).

## Akzeptanzkriterien

1. Backend: Übungskarte einer Vokabel `fr → de` ohne Aufnahme auf einer getippten Stufe ⇒ `speechLang: "fr"`. Rot,
   wenn das Feld nie gesetzt wird.
2. Dieselbe Vokabel **mit** `PronunciationAudioUrl` ⇒ `speechLang: null` (auch auf Stufen ohne Audio). Rot, wenn die
   Regel die `AudioUrl` der Karte statt der Aufnahme der Vokabel liest (die ist außerhalb der Hörstufe immer leer).
3. Richtung `de → fr` (umgekehrt) ⇒ `speechLang: null`. Rot, wenn `WithDirection` das Feld nicht löscht.
4. Hörstufe (`TestStage.Audio`) ohne Aufnahme ⇒ `speechLang: null`, obwohl der Text dort als Ersatz auf der Karte
   steht. Rot, wenn die Hörstufe nicht ausgenommen ist.
5. Vitest `Lektion.test.tsx`: mit `speechLang` und einer gemockten `speechSynthesis` samt Stimme `fr-FR` erscheint ein
   Knopf „Vorlesen“; Antippen ruft `speak` mit einer Äußerung, deren `text` der `prompt` und deren `lang` `fr` ist.
   Rot, wenn `reveal` gesprochen wird oder `lang` fehlt.
6. Vitest: ohne passende Stimme (nur `de-DE`), ohne `speechSynthesis` oder ohne `speechLang` ⇒ kein Knopf; beim
   Erscheinen der Karte wird nie `speak` gerufen (Entscheidung 3). Rot, wenn der Knopf ohne Stimmprüfung erscheint.
7. Der Knopf hat einen sichtbaren Text als zugänglichen Namen (wie `AudioButton`, WCAG 2.5.3) und steht in `de.ts`
   und `en.ts`.

## Schätzung

**Größe: S** – ein Feld am Inhalt und an der Karte, eine Regel, ein kleiner Knopf. Vergleich: kleiner als B-421
(Artikel-Regel je Sprache), etwa wie ein Facetten-Feld (`CueBox`, B-524) ohne dessen Bewertungsanteil.

- **`migration: nein`** – `SourceLanguage` und `PronunciationAudioUrl` liegen schon in der Tabelle.
- **`vertragsbruch: ja`** – `PracticeCard` bekommt `SpeechLang` (additiv, optional am Ende der Parameterliste);
  `Contracts` und `contract.ts` ändern sich, kein Client bricht.

**Risiken**

- `ContentItem` trägt die Sprache bisher nicht; das Feld muss in `WithDirection` (`ExerciseContentProvider.cs:104`)
  gelöscht werden wie `AudioUrl`, sonst spricht die Gegenrichtung (AK 3).
- `getVoices()` ist beim ersten Aufruf oft leer (Chrome) – ohne `voiceschanged` bliebe der Knopf auf manchen Geräten
  dauerhaft weg.
- Lösungsfeld-Regel (`ConventionGuardTests`): `SpeechLang` ist kein Lösungsfeld, der Name trifft keine der vier
  verbotenen Bezeichnungen.

**Angriffsplan (Backend zuerst)**

1. `ContentItem.SpeechLang` im `ExerciseContentResolver` setzen (Sprache der Vokabel, nur ohne Aufnahme), in
   `WithDirection` löschen.
2. `CardFacets`/`BuildCard`: `SpeechLang` nur, wenn `!type.AudioReplacesPrompt(stage)`; Feld an `PracticeCard`.
3. Integrationstest(s) AK 1–4 (`PositionPracticeFlowTests` oder eigene Klasse), `contract.ts` neu erzeugen.
4. Frontend: kleiner Knopf `SpeakButton` neben `AudioButton` (gleiche Optik), in `Aufgabe` (`Lektion.tsx:93`) nur
   für die Übungskarte; `LektionsKarte` um `speechLang` erweitern (optional, Test/Bonus liefern es nicht).
5. Vitest AK 5–7.

**Testweg**

- Integrationstests AK 1–4.
- Vitest `Lektion.test.tsx` AK 5–7 (jsdom kennt `speechSynthesis` nicht – Mock über `vi.stubGlobal`).
- E2E: keiner – Playwright-Chromium hat je nach Plattform keine Stimmen, der Knopf wäre dort zu Recht unsichtbar.
  Das Klang-Urteil gehört zum Gerätecheck B-31 (argumentierte Ausnahme vom Rollengang; der Bau-Sprint nennt sie im
  `## Verlauf`).

## Verlauf

- **2026-10-23** — angelegt (Quelle: Knowunity-Vergleich, Etappe 5 des Übernahmeplans).
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): **die Grundannahme stimmt nicht** – Vokabeln haben schon Aussprache-Audio (`PronunciationAudioUrl`, `ExerciseContentResolver.cs:100`; `AudioButton` an der Karte, `SohnPractice.tsx:295`) und eine Hörstufe, in der der Server bewusst keinen Text schickt (`AudioButton.tsx:8-10`). Browser-TTS bräuchte den Text am Client und wäre in der Hörstufe ein Lösungskanal. Übrig bleibt: TTS als Rückfall für Vokabeln **ohne** Aufnahme und nie in der Hörstufe – das ist die Frage beim Ausformulieren.
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde G-0 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): Browser-Sprachausgabe **nur als Rückfall** für Vokabeln **ohne** Aufnahme und **nie** in der Hörstufe (sonst Lösungskanal am Server vorbei). Beim Ausformulieren gilt das als Rahmen; offen bleiben die Spracheingabe als zweiter Schritt und wie der Server ‚hat keine Aufnahme‘ meldet.
- **2026-11-05** — **Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05: „Wenn es etwas zu entscheiden gibt, übernimm meine Position und entscheide nach bestem Wissen und Gewissen“; [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): (1) keine Spracheingabe (die Browser-Spracherkennung schickt den Ton an einen fremden Dienst); (2) „keine Aufnahme“ = leere `PronunciationAudioUrl`, kein neues Vertragsfeld; Vorlesen nie in der Hörstufe. Das Klang-Urteil bleibt beim Gerätecheck (B-31). **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt im Dialog am 2026-11-05 mit derselben Delegation):** ein Lauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
- **2026-11-14** — Abgleich Backlog-Lauf: Ist-Stand bestätigt, Fundstellen gewandert: `AudioButton` jetzt `frontend/src/components/AudioButton.tsx` (Hörstufe ohne Text `:9-10`), an der Karte `frontend/src/sohn/Lektion.tsx:93` (nur bei `audioUrl`); `AudioUrl` aus `PronunciationAudioUrl` in `ExerciseContentResolver.cs:102`. `speechSynthesis` kommt im Frontend nicht vor.
- **2026-11-14** — Refinement Backlog-Lauf: ausformuliert, gegrillt und geschätzt (S). Beim Belegen gefunden: die Karte kennt ihre Sprache nicht, und außerhalb der Hörstufe kommt auch eine vorhandene Aufnahme nicht mit – „reine Frontend-Arbeit“ trägt nicht. Fünf Stakeholder-Entscheidungen im Auftrag des Nutzers (Server setzt `PracticeCard.SpeechLang`, gesprochen wird nur der `prompt`, nur auf Antippen, kein Knopf ohne passende Stimme, Kern nur Übungskarte); die früheren Entscheidungen bleiben unangetastet. Vertragsbruch ja (additives Feld).
