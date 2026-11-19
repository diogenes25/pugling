---
tags: [typ/story, status/gegrillt, bereich/training, rolle/student, rolle/supervisor]
aliases: [Frage an den Betreuer, Hausaufgaben-Frage, Kind fragt]
status: gegrillt
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/sofatutor-vergleich-plan.md
grund: ""
ersetzt_durch: []
wartet_auf: Bestätigung der Sonnet-Entscheidungen
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

**Beim Grillen am Code gemessen (2026-11-19):**

- **Der Hinweis-Abruf hat heute keinerlei Test-Sperre.** `NoticesAsync` filtert nur auf Kind, `Done` und `SeenByChildAt == null`
  (`Services/Shared/ExerciseFeedbackService.cs:194-216`); `TestRunningAsync` (`:24`) wird nur beim Melden benutzt. Das ist heute
  harmlos, weil der Hinweis keinen Text trägt; mit einem Antworttext würde derselbe Pfad zum Lösungskanal (Punkt 4 der Story).
- **Ein Hinweis entsteht nur über `Done`:** `FeedbackController.Update` (`Controllers/Supervisor/FeedbackController.cs:44-62`)
  setzt den Status für alle Betreuer des Kindes; `UpdateFeedbackDto` trägt nur `Status`. Die Antwort muss also an `Done` hängen.
- **Das Feld `ReplyText` steht nicht in der Lösungsfeld-Liste** (`Answer`/`Solution`/`CorrectAnswer`/`Translation`); das Tor
  `Actions_Mit_Loesungsfeld_Sind_Vor_Dem_Studenten_Gegated` fängt einen Antworttext also **nicht**. Die Sperre braucht einen
  eigenen Test.
- **Eine Meldung ist je Kind, Übung, Karte und Grund eindeutig, nicht je Position** (Zusammenführen offener Einträge,
  `ExerciseFeedbackService.cs:30-76`); die Test-Sperre muss deshalb auf die **Übung** schauen, nicht auf eine Position.

## Die echte Lücke

Schmaler als die Idee: kein neuer Kanal, kein neues Nachrichten-Modell, sondern **eine Antwort auf einen
bestehenden `ExerciseFeedback`**. Konkret fehlen (1) ein Antworttext des Betreuers mit Zeitpunkt und Absender, (2) die
Auslieferung dieses Textes ans Kind (heute trägt der Hinweis nur „erledigt"), (3) eine Entscheidung, ob eine
**Frage** (Text, Absicht „hilf mir") von einer **Meldung** (Grund „Übersetzung falsch") unterschieden wird, (4) die
Regeln zu Pflicht, Belohnung und Anti-Cheat. Die Rückmeldungs-Meldung ist bewusst wirkungslos auf Lernstand, Punkte,
Pflicht und Malus (`FeedbackEntities.cs:6-10`); die Frage erbt das.

## Offene Punkte

1. ~~**Eigene Entität oder Erweiterung von `ExerciseFeedback`?**~~ → Entscheidung 1.
2. ~~**Mehrere Antworten oder genau eine?**~~ → Entscheidung 2.
3. ~~**Wer darf antworten, wenn mehrere Betreuer die Frage sehen?**~~ → Entscheidung 3.
4. ~~**Anti-Cheat im Test:**~~ → Entscheidung 4.
5. **Pflicht:** hält eine offene Frage die Pflicht an? **Zurückgestellt** → Z1.
6. **Belohnung** für eine Frage, nach der die Karte sitzt? **Zurückgestellt** → Z2.
7. ~~**Länge und Ton der Antwort:**~~ → Entscheidung 5.
8. ~~**Abgrenzung zum Lob-Kanal B-377:**~~ → Entscheidung 6.
9. ~~**Foto der Heftseite:**~~ → Entscheidung 7.
10. **Bezug zum Leitbild Privatlehrer:** wer antwortet, wenn nicht der Vater? **Zurückgestellt** → Z3.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

1. **`ExerciseFeedback` wird um `ReplyText`, `RepliedAt` und `RepliedByAdultId` erweitert; eine „Frage“ ist eine Meldung nur mit
   Text, es gibt keine zweite Art.** Eine Meldung allein mit Text ist schon erlaubt (`ExerciseFeedbackController.cs:53`), und der
   Kontext (Karte, Lösung, Kinderantwort) liegt in der Betreuer-Sicht bereits vor (`FeedbackResponse` mit `Solution`, `ChildAnswer`).
   Alle drei Spalten sind nullable und additiv. Verworfen: eine eigene Frage-/Antwort-Entität (zweiter Kanal neben dem
   bestehenden). Kosten: eine Migration an die Kette (`migration: ja`, nachzusehen bei `geschaetzt`), eine neue Beziehung für
   `RepliedByAdultId` (bewusste Zeile im Schema-Tor); Meldungen mit Grund („Übersetzung falsch“) können ebenfalls eine Antwort
   bekommen. Ausgeschlossener Fall: eine Frage, die vom Fehlerbericht unterscheidbar sein muss; gegen die User Story („einen Satz an
   meinen Vater schreiben“) ist das unschädlich, weil der Text allein schon die Frage ist.
2. **Genau eine Antwort je Meldung; eine Rückfrage des Kindes ist eine neue Meldung.** Verworfen: ein Verlauf (anderes Produkt).
   Kosten: kein Hin und Her im selben Faden. Ausgeschlossener Fall: ein Gespräch mit drei Nachrichten. Gegen die User Story
   („seine Antwort in der App wiederfinden“) trägt das, weil nur eine Antwort verlangt ist.
3. **Die Antwort kommt über `PATCH supervisor/feedback/{id}`: `UpdateFeedbackDto` bekommt ein optionales `ReplyText`, erlaubt nur
   zusammen mit `Status = Done`.** Die erste Antwort gilt für alle Betreuer des Kindes (wie `Done`); ein anderer Betreuer bekommt
   danach `409` mit einem neuen Code `feedback_replied`, der Antwortende selbst darf seinen Text ersetzen, solange das Kind ihn
   nicht gesehen hat. Leerer oder nur aus Leerzeichen bestehender Text ist `validation_error`. Begründung: keine neue Action, die
   Eigentumsprüfung (404 bei fremdem Kind, `FeedbackController.cs:58-60`) bleibt dieselbe. Verworfen: ein eigener
   `…/reply`-Endpunkt. Kosten: ein neuer `ApiErrors`-Code, zwei Ausnahmeregeln. Ausgeschlossener Fall: zwei Betreuer
   antworten widersprüchlich; gegen die User Story („kurzer Text“ eines Helfers) reicht eine Antwort.
4. **Der Antworttext wird erst geliefert, wenn kein Test des Kindes (nicht vom Betreuer, nicht abgeschlossen, heutiger Tag) auf einer
   Position derselben Übung läuft; der Hinweis „erledigt“ bleibt sichtbar, der Text ist bis dahin `null`.** Die Sperre sitzt im
   Abruf (`NoticesAsync`), nicht nur im Schreiben. Der Info-Modus bleibt unberührt (dort wird nichts gewertet). Begründung: ohne
   sie wäre der Abruf mit Text ein Lösungskanal, den das Lösungsfeld-Tor nicht sieht (siehe Ist-Stand). Verworfen: die Sperre nur
   beim Antworten (der Vater antwortet nachmittags, das Kind sitzt abends im Test). Kosten: eine Abfrage mehr je Hinweis; die
   Antwort kommt bis zum Testende verspätet. Ausgeschlossener Fall: ein Kind, das mitten im Test Hilfe bekommt; gegen die User
   Story („nicht weiter falsch tippen“) heißt das auch: die Antwort erscheint erst beim nächsten Öffnen der Startseite, nicht
   mitten in der Übung.
5. **Die Antwort hat dieselbe Grenze wie die Meldung (`MaxTextLength = 500`, `ExerciseFeedbackService.cs`), nur Klartext; ein Entwurf
   entsteht offline im Creator und wird vom Betreuer geprüft.** Kein LLM im Backend. Kosten: lange Erklärungen passen nicht.
   Ausgeschlossener Fall: eine ausführliche, formatierte Erklärung.
6. **Der Hinweis-Unterbau (`FeedbackNotice`, Abholen und `…/seen`) wird mit B-377 gemeinsam genutzt, die Modelle bleiben getrennt;
   B-586 kommt zuerst.** `FeedbackNotice` bekommt ein optionales `ReplyText`. Kosten: B-377 muss später dasselbe Notice-DTO
   erweitern. Ausgeschlossener Fall: ein gemeinsames Nachrichten-Modell für Antwort und Lob.
7. **Das Foto der Heftseite ist nicht Teil dieser Story.** Ein Medien-Upload vom Kind ist neu (Datenschutz, Größe, Anti-Cheat;
   ein `student/`-Upload existiert nicht). Kosten: kein Bild zur Frage. Ausgeschlossener Fall: „Frage per Foto“ aus dem
   sofatutor-Vorbild. Eine Folge-Story ist bei Bestätigung anzulegen (nicht in diesem Lauf, der nur diese Datei ändert).

**Zurückgestellt – nicht entschieden (Rollenaufteilung, Münzen, Malus):**

- **Z1 · Hält eine offene Frage die Pflicht an?** Berührt die Malus-Regeln. *Empfehlung:* nein, der Malus läuft weiter; die Frage
  ist Hilfe, kein Aufschub (`PositionProgressService.cs:754` bucht nach Periodenschluss, ohne Feedback zu kennen). Das
  Akzeptanzkriterium 6 hält nur den heutigen Zustand fest (der Antwortpfad schreibt kein Hauptbuch-Objekt).
- **Z2 · Belohnung für eine Frage, nach der die Karte sitzt?** Berührt die Münz-Ökonomie. *Empfehlung:* keine Münzen für das Fragen
  selbst (sonst ein Farm-Weg); ob eine Folge-Verbesserung belohnt wird, gehört in die Diskussion um B-535.
- **Z3 · Wer antwortet, wenn der Lehrer-Agent statt des Vaters antwortet?** Berührt die Rollenaufteilung Vater/Lehrer (Leitbild,
  B-680/B-681). Heute ist die Antwort an `Roles.Supervisor` gebunden (`FeedbackController.cs:20`); ein reines Lehrer-Konto könnte nicht
  antworten. *Empfehlung:* `RepliedByAdultId` trägt jeden `Adult`, der Absender wechselt, das Modell nicht; wer antworten darf,
  entscheidet der Nutzer im Leitbild-Lauf.

## Akzeptanzkriterien

Je Kriterium: welche Änderung macht es rot.

1. `PATCH supervisor/feedback/{id}` mit `Status = Done` und `ReplyText` auf eine Meldung nur mit Text: `GET
   student/children/{id}/feedback-notices` liefert den Text, wenn kein Test läuft. Rot, sobald `FeedbackNotice`/`NoticesAsync`
   den Text nicht trägt.
2. `Done` ohne `ReplyText` liefert beim Kind `ReplyText = null`; ein leerer oder nur aus Leerzeichen bestehender Text ist
   `validation_error`; `ReplyText` mit `Status = Open` ist `validation_error`. Rot, wenn ein leerer Text als Antwort gilt.
3. Ein Text über 500 Zeichen liefert `validation_error`. Rot, wenn die Grenze fehlt.
4. Ein Betreuer eines **fremden** Kindes bekommt `404` (nicht `403`). Rot, wenn der Besitzfilter fehlt.
5. Läuft ein Test des Kindes auf einer Position derselben Übung, liefert der Abruf den Hinweis mit `ReplyText = null`; nach
   Abschluss des Tests steht der Text da. Rot, wenn die Sperre nur beim Antworten oder gar nicht im Abruf sitzt.
6. Der Antwortpfad schreibt kein `ChildPointsEntry` und keinen `PositionGoalPenalty` (Regressionstest). Rot, sobald er ein
   Hauptbuch- oder Fortschrittsobjekt schreibt. (Hält nur den heutigen Zustand fest, siehe Z1.)
7. Das Kind sieht die Antwort einmal; nach `…/seen` verschwindet der Hinweis. Rot, wenn `SeenByChildAt` nicht mehr filtert.
8. Ein zweiter Betreuer bekommt auf eine beantwortete Meldung `409 feedback_replied`, der erste Text bleibt; der Antwortende darf
   seinen Text vor `seen` ersetzen, danach nicht. Rot, wenn die zweite Antwort überschreibt oder der Antwortende nach `seen`
   noch ersetzen darf.
9. Das Vater-Web zeigt Frage, Karte, Lösung, Kinderantwort und ein Antwortfeld; der Antwortknopf ist während der Anfrage gesperrt
   (`useAction`, `disabled={busy}`). Rot, wenn das Feld oder die Sperre fehlt.
10. Der Hinweis auf der Sohn-Startseite zeigt den Antworttext (`KartenMeldung`/`SohnHome`, Komponententest). Rot, wenn das Frontend
    `replyText` aus dem Vertrag nicht darstellt.

## Verlauf

- **2026-11-15** — angelegt (Quelle: sofatutor-Vergleich, Etappe 1 des Übernahmeplans; Sammel-Story [B-585](B-585-sofatutor-vergleich-sammel-story.md)).
- **2026-11-15** — umnummeriert von B-546 auf B-586: die Id war auf `origin/main` schon vergeben (main belegt bis B-552, der Backlog-Lauf vom 2026-11-15 bis B-584). Inhalt unverändert.
- **2026-11-19** — ausformuliert (Vorbereitungslauf): Ist-Stand gegen den Code; der Hinweg besteht seit B-628/B-629, die Lücke
  ist die Antwort des Betreuers samt Auslieferung ans Kind; zehn offene Punkte mit Empfehlung.
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt, `wartet_auf` gesetzt): Entscheidungen 1 bis 7;
  **drei Punkte zurückgestellt** (Z1 Pflicht/Malus, Z2 Belohnung/Münzen, Z3 Absender Lehrer-Agent), weil sie Malus, Münz-Ökonomie
  und Rollenaufteilung berühren. Am Code belegt: Der Hinweis-Abruf hat keine Test-Sperre, die ein Antworttext braucht;
  das Lösungsfeld-Tor fängt `ReplyText` nicht. Akzeptanzkriterien final.
