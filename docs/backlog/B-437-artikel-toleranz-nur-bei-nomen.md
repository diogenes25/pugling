---
tags: [typ/story, status/abgenommen, bereich/training, rolle/student, rolle/creator, lerntechnik/vokabeln]
aliases: [Artikel-Toleranz bei Nicht-Nomen, a few ist nicht few]
status: abgenommen
prio: P3
art: Wunsch
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08-nacht.md
grund: ""
ersetzt_durch: []
wartet_auf: ""
nachgeschaut: 2026-11-14
---

# B-437 · Die Artikel-Toleranz greift auch dort, wo das erste Wort kein Artikel eines Nomens ist

Seit B-421 darf ein führender Artikel der Antwortsprache fehlen. Die Regel prüft nur das **erste Wort** gegen die
Artikel-Tabelle (`StageMechanics.WithoutLeadingArticle`), nicht die Wortart. Bei Nicht-Nomen ändert das die Bedeutung:
„a few" (einige) nimmt „few" (wenige) an, „a little" nimmt „little", „un peu" nimmt „peu"; französisch „des" ist auch
Teilungsartikel.

Vom `pugling-reviewer` in Lauf AE (Sprint 23) als 🟡 gemeldet. Entscheidung 1 von B-421 deckt den Wortlaut („Artikel als
eigenes Wort"), darum kein Code-Defekt.

## User Story

Als **Vater** möchte ich, dass „few“ für „a few“ als falsch zählt, **damit** mein Sohn den Bedeutungsunterschied lernt
und nicht für das Gegenteil des gefragten Wortes belohnt wird – während „Stadt“ für „die Stadt“ weiter richtig bleibt
(B-421).

## Ist-Stand am Code

Gelesen am 2026-11-13 (Branch `auto/backlog-lauf-2026-11-13`, Stand `b0653edb`):

- Die Toleranz wird je Karte gesetzt: `backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs:115`
  (`ArticleOptionalIn = articleRequired || answerLanguage.Length == 0 ? null : answerLanguage`). Einzige Prüfung ist
  der Schalter `articleRequired` der Übung; die Wortart spielt keine Rolle.
- Die Store-Vokabel `v` ist an dieser Stelle geladen (`:93-103`, `byId.TryGetValue`), wird aber nach der Richtungswahl
  (`:105-118`) nur noch über das Sprachpaar `languages[itemId]` erreicht – die Wortart müsste mitgeführt werden.
- Die Wortart steht an der Vokabel: `backend/Pugling.Api/Models/VocabEntities.cs:36` (`PartOfSpeech`), Enum
  `backend/Pugling.Contracts/Common/VocabBaseTypes.cs:8-33` mit `Noun`, `Adjective`, `Adverb`, `Pronoun`, `Numeral`,
  `Phrase` (Mehrwort-Ausdruck), `Other` (Standard, „nicht bestimmt“).
- Die Artikel-Tabelle: `backend/Pugling.Api/Services/Shared/StageMechanics.cs:61-66` (de/en/fr, fr mit „des“);
  `WithoutLeadingArticle` `:74-88` schneidet das erste Wort ab, wenn es in der Tabelle steht.
- Leser der Toleranz: `AnswerGrader.Matches` (`backend/Pugling.Api/Services/Shared/AnswerGrader.cs:17-18`, `:26-33`),
  `AnswerGrader.Spellings` für das Erkennen von Schreibfehlern (`:65-73`), `StageMechanics.cs:172`. Alle lesen
  `ContentItem.ArticleOptionalIn`; eine Änderung an der Quelle wirkt in Üben, Test, Vorschau und Mengen-Zuordnung gleich.
- Tests zu B-421: `backend/Pugling.Api.Tests/ArtikelToleranzTests.cs` (u. a. `Ueben_Wertet_Stadt_Fuer_Die_Stadt_Als_Richtig`,
  `:45`). Ein Fall mit einer Nicht-Nomen-Vokabel fehlt.
- Inline-Atome (ohne Store-Vokabel) bekommen keine Toleranz: `ArticleOptionalIn` wird nur in `ExerciseContentResolver`
  gesetzt (`ExerciseContentProvider.cs:44` ist der Standard `null`).

## Die echte Lücke

Die Regel weiß nicht, ob das erste Wort ein Artikel **vor einem Nomen** ist. Die Information liegt an der Vokabel bereit
und wird nicht genutzt.

## Offene Punkte

1. ~~So eng (nur Nomen) – oder bewusst so weit, weil die Fälle selten sind und das Reveal den Artikel zeigt?~~ →
   Entscheidung 1.
2. ~~Was gilt für Vokabeln ohne Wortart (`Other`)?~~ → Entscheidung 2.
3. ~~Widerspricht das B-421, Entscheidung 1 (vom Nutzer bestätigt)?~~ → Entscheidung 3.

## Entscheidungen

Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

1. **Eng: die Toleranz gilt nur für Store-Vokabeln mit Wortart `Noun` oder ohne bestimmte Wortart (`Other`).** Der
   günstige Weg des Reviewers. *Begründung:* „few“ statt „a few“ ist ein Bedeutungsfehler, kein Tippversäumnis; der
   Sohn darf nicht für das Gegenteil belohnt werden, und der Vater muss sich auf „richtig“ verlassen können.
   *Kosten:* die Wortart wird neben dem Sprachpaar mitgeführt (`ExerciseContentResolver.cs:91-118`); eine Bedingung mehr.
2. **`Other` bleibt tolerant.** *Begründung:* `Other` ist der Standard beim Anlegen; viele Nomen im Bestand tragen keine
   Wortart, und B-421 soll für sie nicht still zurückgenommen werden. *Kosten:* eine untypisierte „a few“-Vokabel
   bleibt tolerant – bekannte Grenze; behoben wird sie am Inhalt (Wortart `Phrase`/`Pronoun` setzen), nicht im Code.
3. **Kein Widerspruch zu B-421/1:** dessen Begründung („der Malus darf nicht am Artikel hängen“) setzt einen Artikel
   voraus; das „a“ in „a few“ ist Teil eines Mengenworts. Die Entscheidung wird ausgelegt, nicht umgestoßen; der
   Wortlaut „Artikel als eigenes Wort“ bleibt für Nomen unverändert. *Kosten:* keine.

## Akzeptanzkriterien

1. Store-Vokabel „a few“ (en, Wortart `Pronoun` oder `Phrase`): im Üben wird „few“ als **falsch** gewertet, „a few“ als
   richtig. *Rot, wenn* die Wortart-Bedingung an `ExerciseContentResolver.cs:115` fehlt.
2. Store-Vokabel „die Stadt“ (de, Wortart `Noun`): „Stadt“ bleibt richtig (bestehender Fall in `ArtikelToleranzTests`).
   *Rot, wenn* die Bedingung `Noun` ausschließt.
3. Store-Vokabel mit Wortart `Other` und führendem Artikel: das Weglassen bleibt richtig. *Rot, wenn* die Bedingung nur
   `Noun` zulässt.
4. Der gesammelte Test wertet AK 1 wie das Üben (eine Quelle, `AnswerGrader.Matches`). *Rot, wenn* die Toleranz an
   einem Leser statt an der Quelle eingeschränkt wird.

## Schätzung

**Größe: XS** – eine Bedingung und ein mitgeführtes Feld in `ExerciseContentResolver`, drei bis vier Fälle in
`ArtikelToleranzTests`. `wo: backend`, `migration: nein` (die Wortart ist schon eine Spalte), `vertragsbruch: nein`.

**Risiken:** Die Wirkung im echten Bestand hängt daran, wie die Lehrwerk-Vokabeln getypt sind; sind „a few“ & Co. dort
`Other`, ändert sich für die Söhne zunächst nichts (Entscheidung 2). Beim Bau prüfen, ob das Lehrwerk-Laden
(`lehrwerk-aufbereiten`) Mehrwort-Ausdrücke als `Phrase` anlegt, und das Ergebnis im Verlauf vermerken.

**Angriffsplan:** (1) Fälle AK 1, 3, 4 in `backend/Pugling.Api.Tests/ArtikelToleranzTests.cs`, AK 1 rot gegen den
Ist-Code; (2) Wortart in `languages` mitführen, Bedingung an `:115`; (3) rote Probe je AK; (4) `pugling-reviewer`.

**Testweg:** `backend/Pugling.Api.Tests/ArtikelToleranzTests.cs` (AK 1–4). Kein E2E nötig; `artikel-pflicht.spec.ts`
bleibt Regressionszeuge.

## Verlauf

- **2026-11-08** — angelegt aus dem Review von B-421 (Lauf AE, Sprint 23).
- **2026-11-13** — `ausformuliert`: am Code belegt; die Wortart liegt am Auflösungsort bereit.
- **2026-11-13** — `gegrillt`: Entscheidungen 1–3, `art` von `Frage` auf `Wunsch` (kein Code-Defekt, eine
  Verhaltensänderung); Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13).
- **2026-11-13** — `geschaetzt`: XS, backend, ohne Migration und Vertragsbruch.
- **2026-11-13** — `in-arbeit`: gebaut. `ExerciseContentResolver` führt die Wortart neben dem Sprachpaar mit; `MayDropArticle` lässt die Artikel-Toleranz nur für `Noun` und `Other`. Fälle AK 1–4 in `backend/Pugling.Api.Tests/ArtikelToleranzTests.cs` (Store-Vokabeln mit gesetzter Wortart). Risiko geprüft: `lehrwerk-aufbereiten/laden.py` legt Vokabeln als Inline-`items` (front/back) ohne Wortart an, also `Other` – für die Söhne ändert sich zunächst nichts (Entscheidung 2); Abhilfe am Inhalt. AK 4 trägt Mutante B437-AK1+4: die Toleranz an der Quelle zurückgenommen färbt den gesammelten Test rot.
  Rote Probe, gemessen an `cf07416f + Arbeitsbaum geändert`, 2026-11-13 21:38 (`mutant-probe.py`, Grundlinie nach dem Zurückspielen grün):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | B437-AK1+4 | backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs:115 | red | red |
  | B437-AK2 | backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs:130 | red | red |
  | B437-AK3 | backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs:130 | red | red |
- **2026-11-13** — **abgenommen** mit Commit `699b02a7` (Sprint 69, Backlog-Lauf 2026-11-13). Verifikation: Backend 1555/1555 grün (dotnet test Pugling.sln -c Release), Vitest 937/937, tsc -b sauber, Mutanten-Probe je AK-Fall rot (Tabelle oben). Review: `pugling-reviewer` ohne Befund (Toleranz an der Quelle, alle Leser sehen dieselbe Regel). Rollengang: Playwright gegen frisch gestartete Server auf Ausweichports (API 5211, Web 5184) nach dem letzten Edit – lektion, erstbegegnung, karten-umdrehen, full-flow, test-durchgefallen-ueben, uebung-abbruch, pfad, bonus, artikel-pflicht: **10 passed**. Vater und Creator sind Regressionszeugen (Suite grün, kein Pfad ihrer Ebene im Diff außer der Vater-Wand von B-433).
