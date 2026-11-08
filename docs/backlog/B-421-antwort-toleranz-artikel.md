---
tags: [typ/story, status/abgenommen, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [Artikel-Toleranz, Stadt statt die Stadt]
status: abgenommen
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
- **2026-11-08** — **Freigabe des Nutzers (Autonomer Modus, Opt-in):** die sieben vorgelegten Entscheidungen der Fable-Sitzung hat der Nutzer im Dialog bestätigt („Ja, so fahren“, Antwort auf die Frage nach den Entscheidungen für die erste Nacht, [Protokoll](../pm-sitzung-2026-11-08-rollengang.md)). Ein Nachtlauf darf diese Story auf Grundlage der `## Entscheidungen` bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
- **2026-11-08** — Lauf AE, Sprint 23 ([Protokoll](../pm-sitzung-2026-11-08-nacht.md)), Commit `481c973`. Gebaut nach
  Angriffsplan; die Toleranz reist als Eigenschaft des Inhalts-Atoms (`ContentItem.ArticleOptionalIn`), und alle fünf
  Bewertungsstellen gehen über `AnswerGrader.Matches(given, item)`. **Auslegung:** „Zielsprache" in Entscheidung 1 =
  Sprache der erwarteten Antwort (bei „hinten → vorne" die Ausgangssprache) – vom `pugling-reviewer` bestätigt.
- **2026-11-08** — Rote Probe: Grader-Signatur mit Sprache zuerst ohne Toleranz, `AnswerGraderTests` **11 rot erwartet,
  11 gemessen** (der Fall „Die Stadt" war schon vorher richtig, Kontrollfall).
- **2026-11-08** — Fälle → Tests: AK 1 (`Stadt`/`Die Stadt` richtig, `der Stadt` falsch) und AK 2 (`house`, `école`) →
  `AnswerGraderTests.Weggelassener_Artikel_Zaehlt` / `Anderer_Oder_Hinzugefuegter_Artikel_Bleibt_Falsch` und
  `ArtikelToleranzTests.Ueben_Wertet_Stadt_Fuer_Die_Stadt_Als_Richtig`; AK 3 (Lösung ohne Artikel, Antwort mit) → derselbe
  Theory-Fall; AK 4 (nur Artikel) → `Nur_Artikel_Gilt_Nur_Exakt`, **als Absichtserklärung beschriftet** (keine Änderung macht
  ihn rot); AK 5 → `ArticleRequired_Verlangt_Den_Artikel_Wieder` und `Ohne_Sprache_Bleibt_Der_Vergleich_Exakt`; AK 6 →
  `Test_Gesammelt_…`, `Test_Einzeln_…`, `Vorschau_Wertet_Wie_Der_Test`; Richtung → `Hinten_Nach_Vorne_…`,
  `Beide_Richtungen_…`; Auswahl → `Auswahl_Bietet_Das_Wort_Ohne_Artikel_Nicht_Als_Falsche_Option_An`; AK 7 →
  `exerciseConfig.test.ts` (Anlegen), `ArtikelPflichtBearbeiten.test.ts` (Dialog: Vorbelegung, Speichern) und E2E
  `artikel-pflicht.spec.ts` (anhaken → anlegen → im Dialog angehakt → abhaken → speichern → neu laden → abgehakt).
- **2026-11-08** — Mutanten (`mutant-probe.py`, alle rot am benannten Fall, Grundlinie grün): G1 keine Toleranz, G2 jedes
  erste Wort fällt weg, G3 Regionssuffix, G4 Elision, G5 Alternativen ohne Toleranz, G6 `articleRequired` ignoriert, G7
  Antwortsprache ohne Richtung, G8 Sprache nie am Atom; H1/H2 Dialog liest/schreibt nicht, H3 Anlegen schreibt nicht,
  H4/H5 Haken ohne Wirkung (E2E), H6 ohne Erklärung (E2E), H7 Elision mit Leerzeichen, H8 Auswahl, H9 „beide" ohne Tausch.
  Ein leerer Integrationsfall („hinten → vorne verzeiht keinen deutschen Artikel" – in jeder Variante grün) wurde vor der
  Messung gestrichen.
- **2026-11-08** — Review: `frontend-reviewer` 🔴 der Haken stand im Typ-Editor, den die App für Vokabeln nie zeigt; 🔴 die
  Tests bewachten genau diesen Zweig; 🟡 Erklärung gehört nach `fieldHelp.ts` → alle behoben (Zähler 3). `pugling-reviewer`:
  keine 🔴; 🟡 „a few" → „few" → [B-437](B-437-artikel-toleranz-nur-bei-nomen.md) (Frage, nicht entschieden); 🟢 Elision mit
  Leerzeichen, zweite richtige Auswahl, Richtung „beide" → behoben.
- **2026-11-08** — Verifikation: Backend 1169/1169, Vitest 686/686, `tsc -b` sauber, Playwright **121 passed**. Rollengang:
  Sohn als E2E (`erstbegegnung.spec.ts` tippt „Stadt" für „die Stadt" und rückt weiter), Creator als E2E
  (`artikel-pflicht.spec.ts`); Vater als Regressionszeuge (volle Suite grün, keine Vater-Fläche im Diff). → `abgenommen`.
