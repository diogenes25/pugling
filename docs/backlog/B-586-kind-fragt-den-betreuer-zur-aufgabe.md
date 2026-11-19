---
tags: [typ/story, status/ausformuliert, bereich/training, rolle/student, rolle/supervisor]
aliases: [Frage an den Betreuer, Hausaufgaben-Frage, Kind fragt]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/sofatutor-vergleich-plan.md
grund: ""
ersetzt_durch: []
---

# B-586 · Das Kind stellt zur aktuellen Aufgabe eine Frage an seine Betreuer und bekommt die Antwort in der App

sofatutors teuerstes Merkmal ist ein Mensch, der Fragen zur Aufgabe beantwortet (Hausaufgaben-Chat mit
Lehrkräften, auch per Foto, erklärend statt lösend). Pugling hat den Menschen schon – den Vater –, und seit der
Rückmeldung zur Karte (B-623, B-628, B-629) auch den **halben Kanal**: das Kind schreibt zu einer Karte einen
Text, alle Betreuer sehen ihn mit Lösung und Kinderantwort. Es fehlt der Rückweg: der Vater kann nur „erledigt"
setzen, einen Antworttext sieht das Kind nie.

## User Story

Als **Schüler** möchte ich an einer Karte, an der ich hänge, einen Satz an meinen Vater schreiben und seine Antwort
in der App wiederfinden, damit ich nicht weiter falsch tippen oder aufhören muss.
Als **Betreuer** möchte ich die Frage samt Karte, Lösung und den letzten Antworten des Kindes sehen und mit einem
kurzen Text antworten, damit ich ohne Rückfrage am Küchentisch helfen kann.

## Ist-Stand am Code

**Der Hinweg existiert (B-628/B-629, beide `abgenommen`):**

- Modell `ExerciseFeedback` mit Kind, Position, Übung, Kartenindex, Grund, Freitext `Text`, Status Open/Done und
  `SeenByChildAt` (`backend/Pugling.Api/Models/FeedbackEntities.cs:12`, `:51`, `:53`, `:66`).
- Kind meldet über `POST student/study-plans/{planId}/positions/{positionId}/feedback`
  (`Controllers/Student/ExerciseFeedbackController.cs:30`); Eingabe `CreateFeedbackDto(Reasons, Text, ItemIndex)`
  (`Pugling.Contracts/Student/FeedbackDtos.cs:12`). Ein Text allein genügt (`ExerciseFeedbackController.cs:53`).
- Sperre im laufenden Test: `TestRunningAsync` (`Services/Shared/ExerciseFeedbackService.cs:24`) liefert
  `feedback_during_test` (`Errors/ApiErrors.cs:217`); auf dem Ergebnisblatt nach dem Absenden ist die Meldung erlaubt
  (`frontend/src/sohn/SohnTest.tsx:395`).
- Betreuer-Sicht: `GET supervisor/feedback` liefert die Meldungen **aller** Kinder des Betreuers, mit Lösung
  (`Solution`) und Kinderantwort (`ChildAnswer`) – Rolle `Supervisor` (`Controllers/Supervisor/FeedbackController.cs:20`,
  `Pugling.Contracts/Supervisor/FeedbackDtos.cs:26`). Erledigt gilt für alle Betreuer des Kindes
  (`FeedbackController.cs:50`).
- Rückweg heute nur als Hinweis: `FeedbackNotice(Id, ExerciseId, ExerciseTitle, ItemIndex, Cue, Reason, DoneAt)`
  (`Pugling.Contracts/Student/FeedbackDtos.cs:35`) – **ohne Text eines Erwachsenen**; `UpdateFeedbackDto` trägt nur
  `Status` (`Pugling.Contracts/Supervisor/FeedbackDtos.cs:33`). Das Kind quittiert über `…/feedback-notices/{id}/seen`
  (`ExerciseFeedbackController.cs:106`).
- Oberfläche: Kind `frontend/src/sohn/KartenMeldung.tsx` (im Üben `SohnPractice.tsx:637`, auf dem Ergebnisblatt
  `SohnTest.tsx:395`, Hinweis auf der Startseite `SohnHome.tsx:26`), Vater `frontend/src/vater/VaterRueckmeldungen.tsx`.
- B-623 hat die Antwort ausdrücklich ausgeklammert: „Das Kind bekommt eine Bestätigung, keine Antwort … Antworttext
  bleibt B-586" (`docs/backlog/B-623-feedback-knopf-an-der-karte.md:166-168`).

**Was die Story in ihrer Ursprungsfassung annahm und nicht mehr stimmt:**

- „Ein Nachrichten-Modell gibt es in `Models/` nicht": nicht mehr richtig, `ExerciseFeedback` ist der Unterbau.
- Das Erfassungs-Widget (`RemarksController.cs:12`) ist weiter nur ein Dev-Werkzeug
  (`frontend/src/sohn/SohnApp.tsx:194`, `import.meta.env.DEV`) und für diese Story **nicht** der Weg.
- Die Id der Widget-Zeile in der Idee (`SohnApp.tsx:174`) ist verschoben; maßgeblich ist `:194`.

**Nicht gefunden:** ein Feld für Antworttext, ein Antwort-Endpunkt, ein Foto-/Medien-Upload vom Kind
(`ExerciseMediaController.cs`/`MediaUploadForm.cs` liegen unter `creator/`, kein `student/`-Upload).

## Die echte Lücke

Schmaler als die Idee: kein neuer Kanal, kein neues Nachrichten-Modell, sondern **eine Antwort auf einen
bestehenden `ExerciseFeedback`**. Konkret fehlen (1) ein Antworttext des Betreuers mit Zeitpunkt und Absender, (2) die
Auslieferung dieses Textes ans Kind (heute trägt der Hinweis nur „erledigt"), (3) eine Entscheidung, ob eine
**Frage** (Text, Absicht „hilf mir") von einer **Meldung** (Grund „Übersetzung falsch") unterschieden wird, (4) die
Regeln zu Pflicht, Belohnung und Anti-Cheat. Die Rückmeldungs-Meldung ist bewusst wirkungslos auf Lernstand, Punkte,
Pflicht und Malus (`FeedbackEntities.cs:6-10`); die Frage erbt das.

## Offene Punkte

1. **Eigene Entität oder Erweiterung von `ExerciseFeedback`?** Empfehlung: Erweiterung um `ReplyText`, `RepliedAt`,
   `RepliedByAdultId`. Eine Frage ist ein Freitext-`ExerciseFeedback` ohne Grund, der Kontext (Karte, Lösung, letzte
   Antwort) ist schon da. Migration additiv (nullable Spalten).
2. **Mehrere Antworten (echter Verlauf) oder genau eine?** Empfehlung: eine Antwort je Meldung im ersten Schritt;
   Rückfragen stellt das Kind als neue Meldung. Ein Chat-Verlauf ist ein anderes Produkt.
3. **Wer darf antworten, wenn mehrere Betreuer die Frage sehen?** Empfehlung: der erste, der antwortet; die Antwort
   gilt für alle (wie `Done`), später antwortende Betreuer sehen die vorhandene.
4. **Anti-Cheat im Test:** Die Sperre `feedback_during_test` gilt schon. Gilt sie für den Info-Modus (`PlayMode.Info`,
   `StudyPlanBaseTypes.cs:25`) nicht, ist das gewollt? Empfehlung: unverändert lassen, dort wird nichts gewertet.
   Die Antwort des Vaters kommt ans Kind erst nach dem Test, nie während eines laufenden (gleiche Sperre beim
   Abholen des Hinweises prüfen: lieferte `NoticesAsync` einen Antworttext während eines laufenden Tests, wäre das ein
   Lösungskanal, auch wenn der Vater ihn gut meint). Der Kontext für den Vater darf `Solution` enthalten; er steht
   bereits hinter `Roles.Supervisor` (Lösungsfeld-Regel).
5. **Pflicht:** hält eine offene Frage die Pflicht an? Empfehlung: nein, der Malus läuft weiter – die Frage ist Hilfe,
   kein Aufschub (`PositionProgressService.cs:754` bucht nach Periodenschluss, ohne Feedback zu kennen).
6. **Belohnung** für eine Frage, nach der die Karte sitzt? Berührt die Münz-Ökonomie, darum nicht hier entscheiden.
   Empfehlung: keine Münzen für das Fragen selbst (sonst ein Farm-Weg); ob eine Folge-Verbesserung belohnt wird, gehört
   in die Ökonomie-Diskussion um B-535.
7. **Länge und Ton der Antwort:** Empfehlung: dieselbe Textgrenze wie `ExerciseFeedbackService.MaxTextLength` (500 Zeichen, `ExerciseFeedbackService.cs:15`), kein
   Markdown. Kein LLM im Backend; ein Entwurf entsteht offline im Creator und wird vom Vater geprüft.
8. **Abgrenzung zum Lob-Kanal [B-377](B-377-lob-kanal-des-vaters.md)** (`geschaetzt`, P3, Richtung Vater → Kind, noch
   ohne Modell): derselbe Zielort beim Kind (Hinweis beim Öffnen). Empfehlung: den Hinweis-Unterbau
   (`FeedbackNotice`) gemeinsam nutzen, die Modelle getrennt halten; B-586 zuerst, weil der Hinweis-Pfad schon steht.
9. **Foto der Heftseite:** Empfehlung: ausdrücklich zweiter Schritt als eigene Story; ein Medien-Upload vom Kind ist
   neu (Datenschutz, Größe, Anti-Cheat).
10. **Bezug zum Leitbild Privatlehrer** ([Leitbild](../leitbild-privatlehrer.md), B-680/B-681): wenn künftig der
    Lehrer-Agent statt des Vaters antwortet, ändert sich der Absender, nicht das Modell. Empfehlung: `RepliedBy` offen
    für beide Rollen halten; die Rollenfrage selbst entscheidet der Nutzer dort.

## Entscheidungen

Noch keine – Stufe `ausformuliert`.

## Akzeptanzkriterien (Entwurf)

Je Kriterium: welche Änderung macht es rot.

1. Antwortet ein Betreuer auf eine Meldung, liefert `GET student/children/{id}/feedback-notices` den Antworttext.
   Rot, sobald `FeedbackNotice`/`NoticesAsync` den Text nicht mehr trägt.
2. Eine Meldung ohne Antwort liefert beim Kind weiterhin nur „erledigt". Rot, wenn ein leerer Text als Antwort gilt.
3. Antworttext ist ein Pflichtfeld des Antwort-Aufrufs und nach oben begrenzt; leerer oder zu langer Text liefert
   `validation_error`. Rot, wenn die Prüfung fehlt.
4. Ein Betreuer eines **fremden** Kindes antwortet nicht (404, nicht 403, wie `FeedbackController.cs:59`). Rot, wenn der
   Besitzfilter fehlt.
5. Während ein Test der Position läuft, liefert der Hinweis-Pfad keinen Antworttext (Lösungskanal). Rot, wenn die
   Sperre nur beim Absenden, nicht beim Abholen greift.
6. Die Antwort ändert Münzen, Pflichtstand und Malus des Kindes nicht. Rot, sobald der Antwort-Pfad ein Hauptbuch- oder
   Fortschrittsobjekt schreibt (Regressionstest gegen `ChildPointsEntry`/`PositionGoalPenalty`).
7. Das Kind sieht die Antwort einmal; nach `seen` verschwindet sie. Rot, wenn `SeenByChildAt` nicht mehr filtert.
8. Das Vater-Web zeigt Frage, Karte, Lösung, Kinderantwort und ein Antwortfeld; der Antwortknopf ist während der
   Anfrage gesperrt (`useAction`). Rot, wenn das Feld oder die Sperre fehlt.

## Verlauf

- **2026-11-15** — angelegt (Quelle: sofatutor-Vergleich, Etappe 1 des Übernahmeplans; Sammel-Story [B-585](B-585-sofatutor-vergleich-sammel-story.md)).
- **2026-11-15** — umnummeriert von B-546 auf B-586: die Id war auf `origin/main` schon vergeben (main belegt bis B-552, der Backlog-Lauf vom 2026-11-15 bis B-584). Inhalt unverändert.
- **2026-11-19** — ausformuliert (Vorbereitungslauf): Ist-Stand gegen den Code; der Hinweg besteht seit B-628/B-629, die Lücke
  ist die Antwort des Betreuers samt Auslieferung ans Kind; zehn offene Punkte mit Empfehlung.
