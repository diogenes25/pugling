---
tags: [typ/story, status/abgenommen, bereich/werkzeug, rolle/creator]
aliases: [laden.py Abbruch, Selbsttest nur für neue Übungen, grammar alternatives]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: ".claude/skills/lehrwerk-aufbereiten"
migration: nein
vertragsbruch: nein
quelle: Lauf des Skills lehrwerk-aufbereiten an Découvertes Série bleue 2 (2026-11-11)
unverifiziert: false
entgangen_bei: []
nachgeschaut: ""
grund: ""
ersetzt_durch: []
---

# B-475 · Bricht `laden.py` mitten im Anlegen ab, bleiben die schon angelegten Übungen für immer ungetestet

Beobachtet gegen die Wegwerf-DB: Übung 23 von 28 scheiterte mit `400 unknown_field`
(`config.tasks[2].alternatives`). Die 22 Übungen davor standen da schon auf dem Server, der Selbsttest läuft aber
erst am Ende. Der nächste Lauf meldete sie als „schon da“ und testete nur die 12 neuen – der Lauf sah grün aus,
obwohl 22 Übungen nie durch den Selbsttest gegangen waren. Bemerkt nur, weil die Zahl der Testzeilen nicht zur
Zahl der Übungen passte; behoben durch eine frische DB. Gegen die echte Instanz gäbe es diesen Ausweg nicht.

Die Ursache des Abbruchs ist ein zweiter, kleinerer Fund: `--pruefen` kennt die Felder der Typen nicht. Eine
`grammar`-Aufgabe hat nur `prompt`, `answer`, `ruleHint` (`GrammarTask` in `ExerciseConfigs.cs`), also keine
`alternatives` – anders als Lückentext und Übersetzung. `FORMAT.md` sagt das nicht, und die Prüfung fängt es
nicht; der Fehler kommt erst vom Server, mitten im Lauf.

Offen und nicht gemessen: ob der Grammatik-Vergleich einen typografischen Apostroph (`qu’`) wie den geraden
(`qu'`) wertet. Ohne `alternatives` lässt sich das in der Übung nicht absichern.

Naheliegende Abhilfe: (1) der Selbsttest läuft über **alle** Übungen des Ordners, nicht nur über die neuen;
(2) `--pruefen` lehnt unbekannte Felder je Typ ab (Feldlisten aus dem Vertrag); (3) ein Satz in `FORMAT.md`.

## Akzeptanzkriterien

1. **Ein zweiter Lauf prüft auch, was schon da war.** Der Selbsttest läuft über jede Übung des Ordners – neue,
   ersetzte und die, die als „schon da" stehen bleiben. Ein Lauf, der nach einem Abbruch wiederholt wird, kann
   also nicht grün aussehen, während Übungen ungeprüft auf dem Server liegen.
2. **`--pruefen` lehnt ein Feld ab, das der Typ nicht kennt**, mit Übung, Pfad und den erlaubten Feldern
   (`uebungen.json #3 (…): config.tasks[2].alternatives – grammar kennt prompt, answer, ruleHint`). Groß/Klein
   zählt nicht, wie beim Server. Die Abkürzungen `markup`/`distractors` des Lückentexts bleiben erlaubt.
3. **Die Feldliste in `laden.py` kann nicht vom Vertrag abdriften:** ein xUnit-Test liest sie aus der Datei und
   vergleicht sie je Route mit den Config-Typen in `Pugling.Contracts`.
4. **`FORMAT.md` sagt es:** Regelaufgaben (`grammar`) haben keine `alternatives`; Varianten gehen dort nur über
   den Antwortvergleich.

## Entscheidungen (autonom, `art: Defekt`)

1. **Selbsttest über alle statt Buchführung über Abbrüche.** Der Testmodus ist nebenwirkungsfrei, ein Lauf
   kostet je Übung zwei Aufrufe; eine Datei „was wurde schon getestet" wäre Zustand außerhalb der API.
   Kosten: ein Lauf über ein großes Buch dauert ein paar Sekunden länger.
2. **Die Feldliste wird gespiegelt, nicht zur Laufzeit aus Swagger geholt** – `--pruefen` läuft ohne Server.
   Gegen Drift hält der Test aus AK 3; er liest die Zuordnung Route → Config-Typ per Reflection aus den
   Controllern und gleicht `ROUTES` mit `FIELDS` ab. Geprüft wird jede Liste von Objekten in jeder Tiefe
   (`tasks[]`, `gaps[]`, `sentences[].decoding[]`). Die Abkürzungen `markup`/`distractors` gelten nur in genau
   dieser Schreibweise, weil `build_config` nur sie liest. `_self` lehnt der Lader ab, obwohl der Server es
   hinnähme – ein Autor schreibt es nie, nur wer eine Server-Antwort abschreibt.
3. **Die Apostroph-Frage ist am Code beantwortet, nicht offen:** Regelaufgaben vergleichen über
   `StageMechanics.Normalize`, das typografische Apostrophe (’ ‘ ʼ ´ und den Gravis) auf den geraden faltet. Ein Integrationstest misst es (`qu’` gegen `qu'`).

## Verlauf

- **2026-11-11** — angelegt (Lauf an Découvertes Série bleue 2).
- **2026-11-12** — gegen den Code belegt (`GrammarTask` ohne `Alternatives`, `ExerciseConfigs.cs:130`; Selbsttest nur über `touched`, `laden.py:345`), autonom gegrillt und auf S geschätzt (Backlog-Lauf, Sprint 34).
- **2026-11-12** — umgesetzt (Sprint 34). Fälle und Belege:
  - AK 1, stehen gebliebene Übung → Lauf 2 gegen den Wegwerf-Server (Port 5280), „über alle 3 Übungen", davon zwei
    „schon da"; Gegenprobe ohne `touched.append((old, …))` → „über alle 0". Von Hand gemessen: `laden.py` hat
    keinen automatischen Test, nur das Tor über seine Feldliste.
  - AK 1, Wortliste aus der TSV → im selben Lauf mitgeprüft (6/6).
  - AK 2, unbekanntes Feld oben/in `tasks[]`/in `decoding[]`, `Instruction` groß geschrieben erlaubt, `Markup` groß
    geschrieben abgelehnt → `unknown_fields` direkt aufgerufen; Gegenproben ohne die Prüfung in `check()` (kaputter
    Ordner besteht) und mit Groß/Klein-freien Abkürzungen (`Markup` rutscht durch) – von Hand.
  - AK 3 → `LadenFieldListTests.Feldliste_Des_Laders_Deckt_Sich_Mit_Dem_Vertrag`.
  - Entscheidung 3 → `Regelaufgabe_Wertet_Typografischen_Apostroph_Wie_Den_Geraden` (`qu’il`; `qu'il` ist die
    Kontrolle, die nur den Aufbau zeigt).
- **2026-11-12** — `pugling-reviewer`: kein 🔴, drei 🟡, alle behoben – **Funde 1–3 des Sprints 34 (zählen)**:
  (1) `Markup` groß geschrieben bestand die Prüfung, aber `build_config` liest nur `markup` – der Server hätte mitten
  im Lauf abgelehnt; (2) `decoding[]` der Birkenbihl-Sätze ungeprüft, obwohl das Format den Typ anbietet;
  (3) die Zuordnung Route → Typ im Test war von Hand gepflegt, `ROUTES` nicht abgeglichen. 🟢 tote Ausnahmeliste
  entfernt, `_self` im Kommentar begründet.
- **2026-11-12** — Mutanten nach der letzten Änderung (`0d00efb + Arbeitsbaum`, 06:54):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | Regelaufgabe mit `alternatives` | laden.py:40 | red | red |
  | Liste ohne `ordered` | laden.py:45 | red | red |
  | Abkürzung als Vertragsfeld | laden.py:39 | red | red |
  | `decoding` ohne `gloss` | laden.py:48 | red | red |
  | Route ohne Feldliste | laden.py:28 | red | red |
  | `’` nicht gefaltet | StageMechanics.cs:53 | red | red |

  Grundlinie nach dem Zurückspielen grün. Nicht als Mutante gemessen: ein Controller, der einen anderen Typ bindet
  (kompiliert nicht ohne Umbau) – die Reflection liest den Basistyp, gelesen statt gemessen.
- **2026-11-12** — **abgenommen.** Volle Suite nach der letzten Änderung: Backend 1338/1338. Review-Zähler Sprint 34: 3 von 5. **Rollengang (Creator) als echter Lauf:** `laden.py` zweimal gegen einen Wegwerf-Server auf Port 5280 mit frischer DB (nicht die echte, nicht 5200) – zweiter Lauf prüft die stehen gebliebenen Übungen mit; ein Ordner mit `alternatives` an einer Regelaufgabe stoppt vor dem ersten Aufruf. Commit folgt in derselben Zeile des Protokolls.
