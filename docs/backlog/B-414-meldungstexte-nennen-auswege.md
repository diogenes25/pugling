---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [Meldungen mit Auswegen]
status: abgenommen
prio: P3
art: Aufräumen
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein (nur detail-Texte; der Code bleibt)
quelle: docs/pm-sitzung-2026-11-07.md#runde-2--review
grund: ""
ersetzt_durch: []
---

# B-414 · Bestehende Meldungstexte nennen noch Auswege – gegen die Meldungsregel vom 2026-11-07

Gefunden im Review zu B-409 (`frontend-reviewer`, 🟡); alter Text, nicht der Diff von B-409. Seit `5143e75` gilt (`pm-loop` Step 5):
eine Meldung nennt, was sperrt, und keine Auswege; eine Fläche, auf die sie zeigt, muss die Sperre zeigen. Zwei Texte in
`GERMAN_PROBLEM_TEXT` (`frontend/src/lib/api.ts`) zählen noch Auswege auf:

- `series_subject_in_use`: „Grammatik-Themen nimmst du … heraus", „eine Übung … kannst du nur löschen …".
- `exercise_in_use`: „Entferne sie dort zuerst." (und der Verweis, siehe [B-413](B-413-loeschsperre-unit-verweist-auf-verwendung-ohne-unit-ziele.md)).

Beim Ausformulieren zuerst per `grep` alle Einträge der Tabelle und die englischen `detail`-Texte der Sperren (`ExerciseUsageQueries.Explain`:
„Remove it there first.") gegen die Regel lesen – nicht nur diese zwei. Berührt [B-411](B-411-tor-409-codes-haben-deutschen-text.md), das
neue Texte nach derselben Regel schreibt; die Reihenfolge der beiden klärt die Schätzung.

## User Story

Als Vater möchte ich in einer Sperrmeldung lesen, was sperrt – nicht einen Ausweg, der selbst wieder gesperrt sein kann.

## Ist-Stand am Code

Die Regel, gegen die gelesen wird, ist die am 2026-11-07 entschiedene (BZ, `docs/pm-sitzung-2026-11-05.md`, letzter Abschnitt):
**keine Auswege über die eigene Sperre hinaus**; eine Fläche, auf die verwiesen wird, muss die Sperre zeigen. Das Entfernen dessen,
was genau diese Sperre hält, darf genannt werden. (`pm-loop` sagte bis 2026-11-08 „never a way out" – schärfer als entschieden,
berichtigt.)

Gelesen per `grep` über alle Einträge von `GERMAN_PROBLEM_TEXT` (`frontend/src/lib/api.ts:82-145`) und die englischen `detail`-Texte
aller 409-Sperren:

| Text | Ausweg | über eine zweite Sperre? |
|---|---|---|
| DE `exercise_in_use` | „Entferne sie dort zuerst." | ja – eine gespielte Position ist `position_has_data` |
| DE `exercise_items_locked` | „Für alles andere nimm die Übung erst aus den Plänen." | ja – dieselbe |
| DE `series_subject_in_use` | „kannst du nur löschen – und das nur, solange …", Verweis „Verwendung" | ja – `exercise_in_use` |
| DE `subject_in_use` | „Entferne erst diese Einträge" | nein – Ziel-/Stundenplan-Einträge sind die Sperre selbst |
| DE `interest_tag_in_use` | „Nimm es zuerst im Bearbeiten-Dialog heraus; fremde nur ihr Autor" | nein – die Einkleidung ist die Sperre selbst |
| EN `ExerciseUsageQueries.Explain` | „Remove it there first." | ja – `position_has_data` |
| EN `ExerciseItemsLocked` (`ExerciseControllers.cs:309`) | „remove it from plans first for other changes" | ja |
| EN `SeriesSubjectInUse`, Leeren (`TextbookSeriesController.cs:204-208`) | „Remove them …; an exercise can be deleted only while …; switching … works once …" | ja – Löschen und Wechsel |
| EN `SeriesSubjectInUse`, Wechsel (`:209-210`) | „remove them before changing" | teilweise – eine Art lässt sich nicht herausnehmen |
| EN `GrammarTopicInUse` | „Remove it from them first." | nein |
| EN Unit/Reihe löschen | „remove it there first" | ja – geht in B-413 auf |

Dazu der Kopfkommentar der Tabelle: „Codes, die einen Nutzer treffen und ihm sagen, was zu tun ist" – widerspricht der Regel.

## Entscheidungen

1. **Nur die Zeilen mit „ja" werden umgeschrieben**; die beiden „nein"-Texte bleiben. *Entscheidung im Lauf (Aufräumen), getroffen
   von Claude*, folgt der Nutzerentscheidung (BZ). Kosten: der Vater liest bei `series_subject_in_use` nicht mehr, dass er eine Übung
   mit Art nur löschen kann – das sagt ihm die Löschsperre selbst, wenn er es versucht.
2. Der Kopfkommentar sagt künftig „was sperrt".

## Akzeptanzkriterien

| Fall | Text | erwartet |
|---|---|---|
| 1 | DE `exercise_in_use` | nennt Lehrplan/Klassenarbeit/Ziel und „Verwendung", **kein** „Entferne" |
| 2 | DE `exercise_items_locked` | nennt die Sperre und „anhängen geht", **kein** „aus den Plänen" |
| 3 | DE `series_subject_in_use` | nennt Grammatik-Themen und Art, **kein** „löschen", **kein** „Verwendung" |
| 4 | EN `Explain` | ohne „Remove it there first" (Integrationsfall) |
| 5 | EN `SeriesSubjectInUse` (Leeren, Wechsel) | ohne „deleted only while"/„switching" bzw. ohne „remove them" für Arten |
| 6 | EN `ExerciseItemsLocked` | ohne „remove it from plans" |

## Schätzung

**Größe: S** — Texte und ihre Zusicherungen. Risiko: bestehende Zusicherungen auf die alten Sätze (`errorMessage.test.ts`,
`ReiheFachwechselTests`, `ExerciseUsageScopeTests`) werden zuerst auf die neue Form umgestellt (rot), dann die Texte.

## Verlauf

- **2026-11-07** — angelegt aus dem Review zu B-409 (Lauf AC, Sprint 20).
- **2026-11-08** — ausformuliert, gegrillt (Aufräumen, im Lauf entschieden, beschriftet) und geschätzt; Sprint 21 mit B-411/B-413.
- **2026-11-08** — gebaut. Zusicherungen zuerst auf die neue Form (rot gegen die alten Sätze), dann die Texte: DE `exercise_in_use`, `exercise_items_locked`, `series_subject_in_use`; EN `Explain`, `ExerciseItemsLocked`, `SeriesSubjectInUse` (Leeren/Wechsel). Ein zuerst geschriebener EN-Satz „a category cannot be taken off an exercise“ war falsch (der Voll-`PUT` leert die Art, nur der Dialog nicht) und flog vor dem Commit wieder raus. Belege: Fall 1 ← B414-DE1, Fall 2 ← B414-DE3, Fall 3 ← B414-DE2/-DE4, Fall 4 ← B414-1, Fall 5 ← B414-3/-4, Fall 6 ← B414-2.
- **2026-11-08** — abgenommen. Backend 1126/1126, Vitest 675/675, `tsc -b` sauber, Playwright **119 passed** (ganze Suite, Server vom Lauf nach dem letzten Edit gestartet; der erste Lauf war 117/2 – siehe Sprint-Protokoll). Mutanten mit `.claude/scripts/mutant-probe.py`, gemessen nach dem letzten Test-Edit an `c910cf8 + Arbeitsbaum`, 2026-11-08: 34 von 34 rot am eigenen Fall, Grundlinie danach grün (Tabelle im Protokoll `docs/pm-sitzung-2026-11-08.md`). **Zähler 4 von 5** (Sprint 21, beide Reviewer je zwei 🟡). Commit: siehe `git log -- docs/backlog/B-414-meldungstexte-nennen-auswege.md` (Lauf AD, Sprint 21). Review: `series_subject_in_use` war beim **Entfernen** als Tatsache falsch (dort zählt jede Übung) → als Bedingung neu, eigene Zusicherung und Mutante B414-DE4; `exercise_items_locked` sagt „steckt in einem Lehrplan“ (die Sperre gilt für jede Position). Der Satz verlor dabei zuerst „bisherigen“ und brach `reihe-fachwechsel-gesperrt.spec.ts` – vom Playwright-Lauf gefunden, das Wort steht wieder. **Rollengang** Vater = `reihe-fachwechsel-gesperrt.spec.ts`, `creator-lehrwerk-weg.spec.ts`.
