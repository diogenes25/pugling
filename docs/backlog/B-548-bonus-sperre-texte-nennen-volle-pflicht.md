---
tags: [typ/story, status/idee, bereich/gamification, bereich/api, rolle/student, rolle/supervisor]
aliases: [Bonus-Sperre nennt noch die volle Pflicht]
status: idee
prio: P3
art: Defekt
groesse: XS
wo: beides
migration: nein
vertragsbruch: nein
quelle: "Nachschau Sprint 73 (Retro-Pflicht), Backlog-Lauf 2026-11-14 – docs/pm-sitzung-2026-11-14-backlog-lauf.md"
unverifiziert: false
grund: ""
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
