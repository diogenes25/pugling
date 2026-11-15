---
tags: [typ/story, status/abgenommen, bereich/gamification, bereich/api, rolle/student, rolle/supervisor]
aliases: [Bonus-Sperre nennt noch die volle Pflicht]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: beides
migration: nein
vertragsbruch: nein
quelle: "Nachschau Sprint 73 (Retro-Pflicht), Backlog-Lauf 2026-11-14 – docs/pm-sitzung-2026-11-14-backlog-lauf.md"
unverifiziert: false
grund: ""
nachgeschaut: 2026-11-15
ersetzt_durch: []
entgangen_bei: [B-535]
wartet_auf: ""
---

# B-548 · Swagger und Texte nennen für Kiste und Bonus-Training noch die volle Pflicht

B-535 hat Tagesbox und Bonus-Sperre von `DutyDone` (alle Ziele, auch Wochenziele) auf `RewardsUnlocked` (nur die
Tagesziele, ohne Tagesziele alle) umgestellt; der Sprint-Review zog die Texte nur an `ApiErrors`, am Service und an
`extra_training_locked` nach. Am Stand `e1a3323b` versprechen noch die alte Regel: die Swagger-Doku
`backend/Pugling.Contracts/Student/ExtraTrainingDtos.cs:22-24` („`Unlocked` is false until the day's duty is met …
the open duties in `Outstanding`“ – `Outstanding` trägt seit B-535 nur die offenen Tagesziele, und „duty“ ist im
Vertrag `DutyDone`); die Kommentare an der Kistenvergabe `PositionPracticeController.cs:612` und
`PositionTestsController.cs:445` („once the day's duty is fully met“ – geprüft wird `RewardsUnlocked`); die
englische Arcade `frontend/src/i18n/en.ts:149` und `:151` („Today's duty first – then the bonus training opens.“,
während `extra_training_locked` daneben schon „today's daily goals“ sagt); das Vater-Web
`frontend/src/vater/VaterKind.tsx:368` („Nach erfüllter Tagespflicht …“ – die Plan-Auswertung `VaterPlanDetail.tsx:98`
nennt einen Tag mit offenem Wochenziel „offen“). Fehlerszenario: Ein Agent oder Client, der nach Swagger baut, schaltet das Bonus-Training
über `dutyDone` frei und sperrt es an jedem Tag mit offenem Wochenziel – genau der Zustand, den B-535 behoben hat;
der Vater liest im Vater-Web eine Regel, die die Arcade nicht anwendet. Keine Buchung ist falsch.
Die Markdown-Doku (`wiki/05-punkte-und-bonus.md`, `docs/tutorial-*.md`) ist in der Nachschau schon berichtigt.

## Verlauf

- **2026-11-14** — angelegt aus der Nachschau zu Sprint 73 (Backlog-Lauf 2026-11-14), Stellen am Stand `HEAD` gelesen.
- **2026-11-14** — **abgenommen** (Sprint 75). Texte nachgezogen: `ExtraTrainingDtos.cs` (Swagger), Kommentare an der
  Kistenvergabe in `PositionPracticeController`/`PositionTestsController`, `en.ts` („Today's daily goals first“),
  `VaterKind.tsx` (Bonus-Absatz: Wochenziele zählen nicht). volle Suite 1628/1628 (Release, nach den Review-Fixes), Vitest 1033/1033 (zwei volle Läufe), `tsc -b` grün; Rollengang Sohn als Playwright gegen frisch gestartete Server nach dem letzten Edit: 8 passed (`truhe`, `belohnung-trotz-wochenziel`, `bonus`, `bonus-deckel-voll`, `pfad`, `lektion`, `full-flow`, `interessen`); Reviews `pugling-reviewer` + `frontend-reviewer`, zehn Funde im Sprint behoben; Commit `f96ec5c6`. Kein eigener Test: reine Texte, die der Code schon
  einhält (B-535-Tests); geprüft per grep, dass keine Stelle mehr „duty“ für die Belohnungsregel sagt.
- **2026-11-14** — Sprint 76, Review-Funde (Frontend): Bonus-Einstieg unterscheidet Tages- und Wochenziele wie die Truhe (`bonus.teaserLockedWeek`, de+en); deutsches „Tagespflicht“ in `bonus.teaserLocked`/`lockedDuty` und `extra_training_locked` (i18n und `lib/api.ts`) auf „Tagesziele“ angeglichen; `VaterKind.tsx` ergänzt „– ohne Tagesziele zählen alle Ziele“; E2E `bonus.spec.ts` und `belohnung-trotz-wochenziel.spec.ts` nachgezogen. Vitest: 1 Fall in `SohnHome.test.tsx`.
- **2026-11-15** — Nachschau im Gesamt-Review des Laufs (Backend + Frontend über `40dd9bfa..HEAD`): Funde aus dem Zusammenspiel im Lauf behoben (Lernkontrolle vom Server `learningCheck`, Truhe/Bonus-Texte aus `rewardsUnlocked`, Agent prüft `canAssign` vor dem Schreiben, `--cadence None` abgelehnt, Truhen-Meldung und Tutorial-Halbsatz); siehe Protokoll „Abschluss“.
