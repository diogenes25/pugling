---
tags: [typ/story, status/abgenommen, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [up / down, Schrägstrich mit Leerzeichen]
status: abgenommen
prio: P1
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-25.md#runde-2--vorstellung--feedback
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-626]
nachgeschaut: ""
---

# B-993 · Ein Leerzeichen am Schrägstrich kostet die Karte

Beobachtet vom Vater am Gerät (Rückmeldung 8 des Testjungen, 2026-11-25 08:25, „zu streng“): „turn sth. up / down“ für
„(to) turn sth. up/down“ ist falsch. An der Kopie der echten DB nachgestellt (Vorschau, Übung 150): „turn sth. up/down“
richtig, „turn sth. up / down“, „turn sth. up/ down“ und „to turn sth. up / down“ falsch, alle mit „falsch geschrieben“.

Umgekehrt gilt dasselbe: seit B-626 AK 7 steht Vokabel 122 als „towards the station / towards Mr Bell“ im Store – wer den
ganzen Eintrag ohne Leerzeichen tippt, verliert die Karte. Betroffen sind 15 Store-Wörter mit Schrägstrich ohne und 88 mit
Leerzeichen (Kopie vom 2026-11-25).

## User Story

Als Lernender möchte ich, dass „up / down“ und „up/down“ gleich zählen, weil ich das Wort weiß und das Leerzeichen keine
Vokabel ist.

## Ist-Stand am Code

- `StageMechanics.Normalize` (`backend/Pugling.Api/Services/Shared/StageMechanics.cs:39`–`:41`) faltet Groß/klein,
  Leerraum, Apostroph und Schlusszeichen, nicht den Leerraum am `/`. Alle Textvergleiche laufen dort durch (`AnswerGrader`,
  Ablenker-Dedupe und Aufzählung in `BuiltInExerciseTypes.cs:327`, `:585`, `:693`–`:701`).
- `AnswerVariants.IsSegmentSeparator` (`AnswerVariants.cs:102`) teilt den **rohen** Eintrag am gespaceten `/` in Bedeutungen –
  vor `Normalize`, bleibt also unberührt.
- Die Regel „wann sind zwei Antworten gleich“ hat drei Leser über eine Fallliste (`docs/fallliste-antwortvergleich.json`,
  B-493): Server, `frontend/src/lib/answerNormalize.ts` (Lernkarte, Vater-Web) und `graded()` im Lehrwerk-Lader.

## Entscheidungen

Defekt, vom PM allein entschieden (README → „Der Backlog-Lauf“).

1. **Die Regel sitzt in `Normalize`** (der Senke), nicht in `AnswerVariants`: Leerraum direkt am `/` fällt weg. So gilt sie
   für jeden Typ und beide Richtungen (Eingabe und Lösung), und die Fallliste hält die drei Leser gleich. *Kosten:* zwei
   Ablenker „a / b“ und „a/b“ sind künftig einer – das ist richtig, sie sahen für das Kind gleich aus.
2. **Nur der Schrägstrich.** Leerraum am Komma oder Bindestrich bleibt, wie er ist – nicht beobachtet, und „well-known“ vs.
   „well - known“ ist kein Fall aus den Wortlisten.

## Akzeptanzkriterien

1. Im Üben, im Abschlusstest und in der Vorschau zählt „turn sth. up / down“, „turn sth. up/ down“ und „turn up / down“ für
   „(to) turn sth. up/down“ als richtig.
2. „towards the station/towards Mr Bell“ zählt für „towards the station / towards Mr Bell“; jede Hälfte allein weiterhin.
3. Die Fallliste trägt die Fälle; Server, Vater-Web und Lehrwerk-Lader folgen ihr.

## Schätzung

**S**, `wo: beides` (Server und `answerNormalize.ts`, dazu der Python-Lader), keine Migration, kein Vertragsbruch.
Risiko: ein Aufrufer, der die Ausgabe von `Normalize` anzeigt statt vergleicht – gegrept, keiner.

### Angriffsplan

1. Fallliste um die Fälle erweitern (rot an allen drei Lesern).
2. `Normalize`, `normalizeAnswer`, `graded()` nachziehen.
3. Pfad-Test: Üben und Abschlusstest an einer Vokabel mit „up/down“.

### Testweg

`FalllisteAntwortvergleichTests`, `answerNormalize.test.ts`, `test_laden.py`, ein Integrationstest je Pfad; Rollengang an der
Kopie (Vorschau der Übung 150).

## Verlauf

- **2026-11-25** — angelegt aus Rückmeldung 8 (Vater am Gerät), an der Kopie nachgestellt; als Defekt allein entschieden und
  geschätzt (S); in Arbeit.
- **2026-11-25** — gebaut: Regel in `StageMechanics.Normalize` (Leerraum am `/` fällt nach dem Zusammenziehen weg), Spiegel
  `normalizeAnswer` (Vater-Web/Lernkarte) und `graded()` (Lehrwerk-Lader), fünf Fälle in der Fallliste (rot an allen drei
  Lesern vor dem Bau: xUnit 3, Vitest 3, unittest 3). Entgangen bei B-626: dessen AK 7 schrieb Vokabel 122 auf „ / “ um.
- **2026-11-25** — Belege je AK-Fall (Ausgangsstand ohne Regel: genau die sechs Wahr-Fälle von `SchraegstrichPfadTests` rot,
  die vier Absichtserklärungen grün, gemessen per `git stash` der Senke):
  - AK 1, Üben, beidseitig → `SchraegstrichPfadTests.cs` `Ueben_…("turn sth. up / down")`, „turn up / down“; M1, M3 rot
  - AK 1, Üben, einseitig → `Ueben_…("turn up/ down")`; M2 rot
  - AK 1, Abschlusstest → `Abschlusstest_…("turn sth. up / down")`; M1 rot
  - AK 1, Vorschau → am Rollengang (Übung 15, Index 11); die Vorschau teilt `AnswerGrader` mit dem Test
  - AK 2, ganzer Eintrag ohne Leerzeichen → `Vorschau_…`, `Ueben_…("towards the station/towards Mr Bell")`; M1 rot
  - AK 2, Hälfte bleibt → `Vorschau_…("towards Mr Bell")`, Absichtserklärung (rot nur, wenn die Regel vor die Bedeutungsteilung wandert)
  - Grenze, Schrägstrich ist kein Leerzeichen → „… up down“ false (Pfade) und Fallliste; M7 rot
  - Grenze, nur am Schrägstrich → Fallliste „ice cream“/„icecream“; M4 rot (die Pfad-Kontrolle fängt M4 nicht – Kommentar berichtigt)
  - AK 3, Vater-Web → `answerNormalize.test.ts`; M5 rot · Lehrwerk-Lader → `test_laden.py`; M6 rot

  Mutanten-Probe (`mutant-probe.py`, nach der letzten Code-Änderung an der Senke):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | M1 Regel weg (Server) | StageMechanics.cs:44 | red | red |
  | M2 nur links | StageMechanics.cs:44 | red | red |
  | M3 nur rechts | StageMechanics.cs:44 | red | red |
  | M4 jedes Leerzeichen weg | StageMechanics.cs:44 | red | red (2. Lauf, `fails` auf den Fallliste-Fall) |
  | M5 Vater-Web | answerNormalize.ts:11 | red | red |
  | M6 Lehrwerk-Lader | laden.py:119 | red | red |
  | M7 Schrägstrich wie Leerzeichen | StageMechanics.cs:44 | red | red |

  Grundlinie nach dem Zurückspielen grün. .NET-Suite 2412/2412 (Release, Ausgangsstand 2398; vor dem 🟢-Fall „turn up / down“, der im Test-Tor mitläuft), Vitest 502/502 (Lernkarte + Vater-Normalisierung), `test_laden.py` OK.
- **2026-11-25** — Review `pugling-reviewer`: kein 🔴/🟡. Alle Aufrufer von `Normalize` vergleichen oder deduplizieren nur;
  kein Vergleichspfad an `Normalize` vorbei. 🟢 übernommen: grüne Ausgangsfälle als Absichtserklärung beschriftet,
  „turn up / down“ ergänzt. 🟢 bewusst nicht: `Pugling.Agent.Creator/Drafting/ClozeMarkup.cs:91` findet „up/down“ nicht in
  einem Text mit „up / down“ – Erstellen, nicht Werten.
- **2026-11-25** — Rollengang an einer frischen Kopie der echten DB (`kopie-server.sh` nach der letzten Änderung, :5201):
  Testjunge spielt Position 220, Karte „etwas lauter/leiser stellen“, tippt „turn sth. up / down“ → richtig. Vater, Vorschau
  an Theodors Übung 15 (Index 11): „turn sth. up / down“ und „turn up / down“ richtig, „turn sth. up down“ falsch.
- **2026-11-25** — abgenommen in `dcdd8c32`: Sohn und Vater an der Kopie (Rollengang oben), Creator als Regressionszeuge.
