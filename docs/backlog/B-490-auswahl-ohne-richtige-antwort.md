---
tags: [typ/story, status/abgenommen, bereich/lernen, rolle/student]
aliases: [choices ohne answer, Lese- und Hörverstehen unlösbar]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: "backend"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 38)
unverifiziert: false
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-490 · Eine Auswahlfrage, deren Optionen die Antwort nicht enthalten, ist unlösbar

Gefunden bei der Nachschau von Sprint 38 (frischer `pugling-reviewer`, gelesen, nicht gefahren) – dieselbe Klasse wie
B-487, aber ein anderer Typ: `AnswerChecking.ChoicesOf` (`BuiltInExerciseTypes.cs:472`) gibt die `choices` einer Lese-
oder Hörverstehensfrage unverändert weiter. Fehlt die `answer` darunter, kann das Kind die Frage nicht lösen. Nur das
Vater-Web warnt (`exerciseConfig.tsx:149`, `choicesMissAnswer`); Lader (`FIELDS["reading"]` trägt `choices`) und API
lassen es durch. Ob ergänzt (wie B-487) oder abgelehnt wird, ist beim Grillen zu entscheiden.

## Akzeptanzkriterien

1. **Jede Lese- und Hörverstehensfrage mit Optionen ist lösbar:** fehlt die Antwort unter den Optionen, kommt sie dazu –
   in Üben, Klausur und Testmodus (alle über `CardFacets`).
2. **Die Reihenfolge des Autors bleibt**, die ergänzte Antwort steht dahinter.
3. **Eine Option, die die Bewertung annimmt** (andere Groß/Klein-Schreibung, Schlusszeichen), genügt – dann bleibt die
   Liste wie geschrieben.
4. **Ununterscheidbare Optionen** sind ein Knopf (dieselbe Bereinigung wie die Wortbank, B-491).

## Entscheidungen (autonom gegrillt, `art: Defekt`, Nachtlauf 2026-11-12)

1. **Ergänzen statt ablehnen**, wie B-487 für die Wortbank: eine Frage ohne lösbare Option will niemand, und die Senke
   (`AnswerChecking.ChoicesOf`) erreicht jeden Schreibweg (Editor, Lader, API), auch Altbestand ohne Migration. Eine
   Ablehnung beim Schreiben (400) hätte Altbestand nicht geheilt und wäre ein Vertragsbruch.
2. **Hinten anfügen**, nicht einsortieren: die Reihenfolge des Autors trägt Bedeutung (Zeitleiste, „keine davon" am
   Ende) und wird darum nie umsortiert. Die Stelle verrät nur, *dass* ergänzt wurde – das Kind sieht das nicht, und der
   Vater bekommt im Editor weiter die Warnung `choicesMissAnswer`. **Kosten:** in einer Übung, deren Autor bei *jeder*
   Frage die Antwort vergaß, steht sie immer zuletzt; das ist Fehlbedienung, die das Vater-Web anzeigt.
3. **„Passt" entscheidet der `AnswerGrader`** – dieselbe Prüfung, die die Wahl des Kindes bewertet.
4. **Die Kopie der echten DB** (2026-11-12 14:55): keine Verstehensfrage mit Optionen ohne die Antwort – für die Söhne
   ändert sich nichts.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 38 im Backlog-Lauf).
- **2026-11-12** — Nachtlauf, Sprint 40: autonom gegrillt (`art: Defekt`), geschätzt XS, umgesetzt in
  `AnswerChecking.ChoicesOf` (geteilte Bereinigung `Tiles` mit B-491).
- **2026-11-12** — Rote Probe vor dem Fix (`ExerciseChoicesTests`, Release): erwartet 3 rot, gemessen 3 rot
  (`Moeglichkeiten_ohne_Loesung_bekommen_sie` reading/listening: `["Leeds","Hull"]` ohne `York`;
  `Ununterscheidbare_Moeglichkeiten_kommen_einmal`: `york` als dritter Knopf). Ein erster Lauf des Hörverstehens-Falls
  fiel am falschen Ort (400 beim Anlegen, `text` statt `audioUrl`) – Test korrigiert, dann rot aus dem richtigen Grund.
  `Moeglichkeit_in_anderer_Schreibweise_genuegt` ist vorher **grün** (die Liste ging unverändert durch) und trägt nur
  die Mutante A2.
- **2026-11-12** — Belege je AK: AK 1 → `Moeglichkeiten_ohne_Loesung_bekommen_sie` (A1), Klausur/Testmodus über die
  geteilte Projektion (`Klausur_LiefertDieAntwortmoeglichkeitenEbenfalls`, `Testmodus_ZeigtDieAntwortmoeglichkeiten`);
  AK 2 → derselbe Test (Reihenfolge `Leeds, Hull, York`); AK 3 → `Moeglichkeit_in_anderer_Schreibweise_genuegt` (A2);
  AK 4 → `Ununterscheidbare_Moeglichkeiten_kommen_einmal` (T2).
- **2026-11-12** — `pugling-reviewer`: kein 🔴; **ein 🟡** – die Warnung im Vater-Editor (`exerciseConfig.tsx`,
  `choicesMissAnswer`) behauptete noch „kann nicht richtig antworten"; jetzt sagt sie, dass die Antwort als letzter Knopf
  dazukommt (Doku-Kommentar nennt die Schreibweisen-Falle „York"/„in York"). **Review-Zähler Sprint 40: 1.** 🟢 behoben
  ohne Zählung: behaltene Einträge werden getrimmt (Test `Wortbank_laesst_leere_Eintraege_weg` um `" go "` erweitert,
  Mutante T4), Lader-Kommentar. 🟢 offen, bewusst: die ergänzte Antwort steht immer zuletzt (Entscheidung 2 in B-490);
  eine leere Antwort bleibt unlösbar (nicht neu); Editor- und Server-Normalisierung unterscheiden sich bei NBSP (älter).
  Der Reviewer bestätigt die Senke: Üben, Klausur, Testmodus und „anderes Bild" gehen über `CardFacets`; die Arcade filtert
  nicht nach; der Lader legt Kacheln nach derselben Regel zusammen.
- **2026-11-12** — Mutanten nach der letzten Code-Änderung (`mutant-probe.py`, 15:16, **Debug**: fremde Testläufe
  sperrten zweimal die Release-DLL, beide Läufe verworfen – 14:59 und 15:03, Grundlinie danach „kompiliert nicht"):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | C1 Bank roh | BuiltInExerciseTypes.cs:235 | red | red |
  | C2 leere Bank zaehlt | BuiltInExerciseTypes.cs:236 | red | red |
  | T1 Leere bleiben | BuiltInExerciseTypes.cs:467 | red | red |
  | T2 Dubletten bleiben | BuiltInExerciseTypes.cs:467 | red | red |
  | T3 nur Gross/Klein | BuiltInExerciseTypes.cs:467 | red | red |
  | T4 nicht getrimmt | BuiltInExerciseTypes.cs:467 | red | red |
  | A1 keine Ergaenzung | BuiltInExerciseTypes.cs:494 | red | red |
  | A2 exakter Vergleich | BuiltInExerciseTypes.cs:494 | red | red |

- **2026-11-12** — **abgenommen.** Volle Suite nach der letzten Änderung 1396/1396 (Release), `tsc -b` sauber,
  `exerciseConfig.test.ts` 22/22. **Rollengang (Sohn) als Maschinengang:** die Tests spielen Wortbank- und Auswahlkarten
  über die Karten-Route des Kindes gegen die In-Process-API; kein Browser. Vater: die geänderte Warnzeile ist nur
  gelesen, nicht im Browser angesehen.
