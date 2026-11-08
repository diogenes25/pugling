---
tags: [typ/story, status/geschaetzt, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [Artikel-Toleranz, Stadt statt die Stadt]
status: geschaetzt
prio: P2
art: Wunsch
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08-rollengang.md#feedback-sohn
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-421 · Antwort-Toleranz: „Stadt" für „die Stadt" zählt als richtig

Im Rollengang vom 2026-11-08 tippte der Sohn zu „la ville" die Antwort „Stadt". Erwartet war „die Stadt" –
gewertet wurde **falsch**. Für ein Kind ist das Unfairness in Reinform („ich hab's doch gewusst!"), und
genau das Gefühl, das die Kernidee (Pflicht mit Konsequenz) nicht verträgt: der Malus trifft dann nicht
Nichtwissen, sondern einen fehlenden Artikel.

## User Story

Als *Sohn* möchte ich, dass eine Antwort ohne Artikel als richtig zählt, wenn das Wort stimmt, damit Punkte und
Malus mein Wissen messen und nicht meine Tipp-Disziplin – und als *Vater* möchte ich den Artikel trotzdem
verlangen können, wenn die Übung genau ihn trainiert.

## Ist-Stand am Code

- `AnswerGrader.cs:14-15`: `Matches(given, expected)` ist Gleichheit nach `StageMechanics.Normalize`
  (`StageMechanics.cs:31-32`: Trim, `ToLowerInvariant`, Leerzeichen zusammenziehen). Kein Artikel, kein
  Tippfehler wird toleriert.
- Der Review prüft gegen alle akzeptierten Antworten eines Items (`PositionPracticeController.cs:325`,
  `item.AcceptedAnswers.Any(a => grader.Matches(…))`) – die „Auch richtig"-Varianten des Creators (B-71)
  sind der einzige Toleranzweg, und sie müssen je Wort gepflegt werden.
- Die Übungs-Config kennt `sourceLang`/`targetLang` (`fr`/`de` im Rollengang, `GET …/vocabulary/8`), also ist
  die Sprache der erwarteten Antwort bekannt.
- Gemessen: „Stadt" → falsch (Lösung: die Stadt); „Laden" → falsch (Lösung: das Geschäft – hier zu Recht).

## Die echte Lücke

Ein Vergleich, der Groß-/Kleinschreibung verzeiht, aber den Artikel nicht – obwohl die Lösung ihn ohnehin
**zeigt** (Reveal nach der Antwort). Die Toleranz gehört in den **einen** Grader, nicht in tausend
„Auch richtig"-Einträge.

## Offene Punkte

1. ~~Artikel verzeihen?~~ → Entscheidung 1.
2. ~~Welche Sprachen?~~ → Entscheidung 2.
3. ~~Tippfehler (Levenshtein) mit verzeihen?~~ → **zurückgestellt**, eigene Frage
   [B-422](B-422-beinahe-treffer-tippfehler.md): das ist eine andere Fairness-Frage mit anderem Risiko.
4. ~~Was, wenn die Übung den Artikel trainiert?~~ → Entscheidung 3.

## Entscheidungen

Getroffen vom PM in der Fable-Sitzung vom 2026-11-08 unter dem Auftrag des Nutzers, das Backlog
nachtlauffähig zu machen ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)). Vetorecht beim Nutzer.

1. **Ein führender Artikel der Zielsprache ist optional.** Beginnt die erwartete Antwort mit einem Artikel
   als eigenem Wort und bleibt danach mindestens ein Wort, zählt auch die Antwort ohne diesen Artikel als
   richtig. Umgekehrt nicht (Antwort mit Artikel, Lösung ohne – dann hat das Kind etwas dazuerfunden).
   Begründung: der Artikel steht im Reveal und wird beim Lernen trotzdem gesehen; der Malus darf nicht an ihm
   hängen. Kosten: ein Artikel-Vokabular je Sprache im Grader und eine Signatur mit Sprache.
2. **Drei Sprachen, eine Tabelle.** de: der/die/das/ein/eine; en: the/a/an; fr: le/la/les/l'/un/une/des (mit
   `l'` als Präfix ohne Leerzeichen). Latein hat keine Artikel. Die Tabelle steht in `StageMechanics` neben
   `Normalize`. Kosten: die Liste ist Produktwissen im Code – klein, englisch kommentiert.
3. **Opt-out je Übung: `articleRequired`.** Ein Feld in der Vokabel-Config (JSON, `VocabularyConfig`), Default
   `false`; der Creator setzt es, wenn die Übung Artikel trainiert (Wortart „Artikel", Genus-Übungen). Kein
   Schema, kein Vertragsbruch (additives Config-Feld; der Editor bekommt eine Checkbox). Kosten: ein Feld im
   Hin- und Rückweg des Editors (`exerciseConfig.tsx`).
4. **Gilt für Üben und Test gleichermaßen.** Beide gehen durch denselben `AnswerGrader`; zwei Maßstäbe
   wären ein zweiter Selbstbetrugsweg. Kosten: keine.

## Akzeptanzkriterien

1. Erwartet „die Stadt", gegeben „Stadt" → richtig; gegeben „Die Stadt" → richtig; gegeben „der Stadt" →
   falsch (anderer Artikel ist kein weggelassener).
2. Erwartet „the house", gegeben „house" → richtig; erwartet „l'école", gegeben „école" → richtig.
3. Erwartet „Stadt" (ohne Artikel), gegeben „die Stadt" → falsch.
4. Erwartet „die" (nur Artikel) → kein Weglassen möglich, nur exakt.
5. `articleRequired: true` an der Übung → Fall 1 wird wieder falsch.
6. Derselbe Fall im Test (`PositionTestsController`) wird gleich gewertet wie im Üben.
7. Der Editor zeigt die Checkbox „Artikel muss mitgetippt werden" und schreibt/liest das Feld.

## Schätzung

**Größe: S** — Grader-Erweiterung mit Sprache + Tabelle, Config-Feld, Checkbox im Editor, Unit- und
Integrationstests. Kein Schema, kein Vertragsbruch.

**Risiken:** (1) Der Grader wird an mehreren Stellen aufgerufen (Üben, Test, Lückentext-Gaps `:21-23`,
Vorschau `preview/check`) – die Sprache muss überall mitkommen; für Gaps gilt die Toleranz **nicht** (ein
Lückentext fragt oft genau den Artikel). (2) Birkenbihl/Übersetzung haben eigene Vergleiche – nicht anfassen.
(3) `Normalize` ist in `AnswerPattern`/Buchstabenkästchen eingebaut (B-66): die Kästchen zeigen weiter die volle
Lösung mit Artikel; nur der Freitext-Vergleich wird toleranter.

**Angriffsplan (Backend zuerst):** (1) `StageMechanics.ArticlesOf(lang)` + `AnswerGrader.Matches(given,
expected, lang, articleRequired)`; (2) Aufrufer in Üben/Test/Vorschau mit `targetLang` aus der Config versorgen;
(3) `VocabularyConfig.ArticleRequired`; (4) Unit-Tests `AnswerGraderTests` (neu) für AK 1–5, Integrationstest in
`PositionPracticeFlowTests` für AK 6; (5) Editor-Checkbox + `exerciseConfig.test.ts` Rundlauf; (6) Doku
`wiki/05-punkte-und-bonus.md` Satz zur Toleranz.

**Testweg:** `AnswerGraderTests` (Unit, neu), `PositionPracticeFlowTests` (Integration), `exerciseConfig.test.ts`
(Vitest); Rollengang Sohn über `erstbegegnung.spec.ts` (B-419) mit einer Antwort ohne Artikel.

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang Sohn ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)),
  Ist-Stand belegt, Entscheidungen vom PM unter dem Auftrag „nachtlauffähig machen" (Vetorecht beim Nutzer),
  geschätzt. Tippfehler-Toleranz bewusst abgetrennt (B-422).
