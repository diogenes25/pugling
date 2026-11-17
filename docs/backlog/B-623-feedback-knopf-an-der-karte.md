---
tags: [typ/story, status/ausformuliert, bereich/training, bereich/katalog, rolle/student, rolle/creator, rolle/supervisor]
aliases: [Feedback-Knopf, Rückmeldung zur Übung]
status: ausformuliert
prio: P1
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17 (Erfahrung aus der Testphase mit Friedrich und Theodor)"
grund: ""
ersetzt_durch: []
---

# B-623 · Der Lernende gibt zur Karte eine Rückmeldung, und neue Übungen berücksichtigen sie

In der Testphase zeigt sich, dass manche Übungen zu schwer oder zu leicht sind, Vokabeln abfragen, die
im Unterricht noch nicht dran waren, falsch übersetzen, zu viele Wörter auf einmal verlangen oder zu
streng prüfen. Friedrich und Theodor merken das an der Karte, haben dort aber keinen Weg, es zu sagen.
Das Anmerkungs-Widget ist ein Dev-Werkzeug und in der Sohn-App nur im Entwicklungsmodus eingebunden
(Stand laut [B-586](B-586-kind-fragt-den-betreuer-zur-aufgabe.md), hier nicht nachgeprüft).

Die Idee: An jeder Karte bzw. Übung ein Feedback-Knopf. Er öffnet eine **Vorauswahl**, damit das Kind
kaum schreiben muss, und ein **optionales Freitextfeld** für „was war gut/schlecht". Mögliche Gründe
aus dem Gespräch:

- zu schwer / zu leicht
- Vokabel ist weiter als unser Unterrichtsstoff
- falsch übersetzt
- zu viele Wörter verlangt (die Antwort ist umfangreicher als die eigentliche Vokabel)
- zu streng geprüft (Akzent/Strich/ein Buchstabe falsch ⇒ ganzer Fehler)
- das Wort hat mehrere Übersetzungen, die Eingabe passt nicht dazu
- (positiv) gut so / hat Spaß gemacht

Der Server hängt den Kontext an (Kind, Position, Übung, Item, letzte Antwort), so wie das Widget ihn heute
mitschneidet. Die Rückmeldungen sollen **wirken**: Der Vater sieht sie gesammelt, und beim Erstellen
neuer Übungen – von Hand und im [KI-Creator](../../backend/Pugling.Agent.Creator/CLAUDE.md) – fließen sie
ein (etwa: Wörter jenseits des Unterrichtsstands meiden, Antworten kürzer halten).

Die Einzelbefunde aus demselben Gespräch stehen als eigene Stories: [B-624](B-624-akzentfehler-nicht-ganzer-fehler.md),
[B-625](B-625-ein-eingabefeld-je-bedeutung.md), [B-626](B-626-antwort-verlangt-mehr-als-die-vokabel.md).

## User Story

Als **Lernender** (Friedrich, Theodor) möchte ich an einer Karte mit zwei Tipps sagen, was mit ihr nicht
stimmt oder was gut war, damit mein Betreuer davon erfährt und die nächsten Übungen besser zu mir und
meinem Unterricht passen.

Als **Supervisor** möchte ich die Rückmeldungen meiner Kinder gesammelt und mit Kontext sehen, damit ich
schlechte Übungen korrigiere und beim Erstellen neuer Übungen – selbst oder über den KI-Creator – vermeide,
was nicht gepasst hat.

## Ist-Stand am Code

Recherchiert am 2026-11-17 gegen `847a5cfd`.

- **Im Produkt hat das Kind keinen Rückmeldeweg.** Das Anmerkungs-Widget hängt in der Sohn-App nur im
  Entwicklungsbuild: `frontend/src/sohn/SohnApp.tsx:174` (`import.meta.env.DEV && <RemarkWidget …/>`). Die
  Söhne lernen gegen den Production-Build (`scripts/heimnetz.ps1`), dort fehlt es.
- **Anmerkungen (`Remark`) sind ein Entwickler-Werkzeug** mit Antwort- und Status-Workflow für Claude Code:
  `backend/Pugling.Api/Models/RemarkEntities.cs:16` (Klasse), `:34` (`Status`), `:43` (`Answer`). Sie
  schneiden aber genau den Kontext mit, den B-623 braucht: `ChildId`, `ExerciseId`, `StudyPlanId`,
  `PlanPositionId` (`RemarkEntities.cs:98`–`:110`) und `ContextJson`. Ein Feld für die **Karte** (Item) fehlt.
  Die Kategorien sind Entwickler-Kategorien (`backend/Pugling.Contracts/Common/RemarkBaseTypes.cs:8`: `Bug`,
  `Ui`, `Code`, `Content`, `Idea`, …), keine Gründe eines Lernenden. Der Endpunkt prüft den Kontext auf
  Eigentum und lässt Fremdes still fallen (`backend/Pugling.Api/Controllers/RemarksController.cs:171`–`:185`)
  – dieses Muster ist übertragbar.
- **Übungsbewertungen (`ExerciseRating`) gibt es, aber nur zwischen Creatorn** (B-12): 1–5 Sterne und
  Kommentar, eine Zeile je Creator und Übung, Eigentümer dürfen nicht bewerten
  (`backend/Pugling.Api/Controllers/Creator/ExerciseRatingsController.cs:11`–`:23`, `Roles.Creator`; Entität
  `backend/Pugling.Api/Models/LearnEntities.cs:230`). Für das Kind passt das nicht: falsche Rolle, keine
  Karte, kein Grund, nur eine Bewertung je Person.
- **Die Karte ist adressierbar.** Im Üben über den `ItemIndex` der Sitzung
  (`backend/Pugling.Contracts/Student/PracticeDtos.cs:113`, `ReviewDto(int ItemIndex, …)`); dahinter stehen
  `ExerciseItem` und Vokabel (`backend/Pugling.Api/Models/ItemProgressEntities.cs:26`–`:32`: `ItemId`,
  `ExerciseId`, `VocabularyId`). Die Sohn-App bewertet in `frontend/src/sohn/SohnPractice.tsx:250` und
  rendert die Karte in `:518` (`<Lernkarte …>`). Der Abschlusstest läuft getrennt in
  `frontend/src/sohn/SohnTest.tsx` (`POST …/answer`,
  `backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:252`).
- **Der KI-Creator kennt keine Erfahrungen mit vorhandenen Übungen.** Sein Briefing
  (`backend/Pugling.Agent.Creator/Briefing/CreatorBriefing.cs:17`–`:35`) trägt Profil, Material, Kind,
  Wortliste, vorhandene Übungstitel, Interessen und Grammatik. Angrenzend:
  [B-604](B-604-sprachuebungen-fragen-nur-sprache.md) will mit „Die Antwort ist ein Eintrag der Wortliste“
  ebenfalls in die Regeln des Agenten.
- **Verwandte Stories:** [B-586](B-586-kind-fragt-den-betreuer-zur-aufgabe.md) (Frage an Papa *mit*
  Antwort, `idee`), [B-590](B-590-lehrwerk-bezug-im-vater-plan.md) („schon durchgenommen“),
  [B-536](B-536-aufbau-uebungen-statt-versagen.md) (Stoff zu schwer).

## Die echte Lücke

Es gibt keinen Kanal vom Kind zum Inhalt. Die beiden vorhandenen Bausteine liegen jeweils daneben:
`Remark` hat den Kontext, ist aber ein Werkzeug für den Entwickler (Antwort-Workflow, Entwickler-Kategorien,
im Production-Build unsichtbar, keine Karte). `ExerciseRating` hängt an der Übung, gilt aber nur zwischen
Creatorn und kennt keinen Grund. Es fehlen:

- ein Student-Endpunkt mit festen Gründen und Freitext, an Übung **und** Karte,
- eine Sicht für den Supervisor,
- ein Weg, auf dem die Rückmeldungen das Erstellen neuer Übungen erreichen.

## Offene Punkte

1. **Neue Entität oder `Remark` erweitern?** Empfehlung: **neue Entität** (Arbeitsname `ExerciseFeedback`)
   unter `student/…`. `Remark` trägt den Antwort-Workflow für Claude Code und den Export nach
   `docs/anmerkungen/`; Rückmeldungen der Lernenden dort zu mischen, verwässert beides. Kosten: eine
   Migration, ein Eigentums-Check nach dem Muster von `RemarksController.cs:171`.
2. **Woran hängt sie: Übung, Karte oder beides?** Empfehlung: Übung und Position **Pflicht**, Karte
   (`ItemId`, damit auch `VocabularyId`) **optional**. „Falsch übersetzt“ betrifft ein Wort, „zu schwer“
   oft die ganze Übung.
3. **Feste Gründe.** Empfehlung: eine geschlossene Liste im Vertrag, Mehrfachauswahl, Freitext optional,
   aber mindestens ein Grund **oder** ein Text. Vorschlag: `TooHard`, `TooEasy`, `AheadOfClass` (weiter als
   der Unterricht), `WrongTranslation`, `TooMuchAsked` (mehr als die Vokabel verlangt), `TooStrict` (Akzent,
   ein Buchstabe), `SeveralMeanings`, `Liked` (gut so). Ein neuer Grund ist eine Vertragsänderung – gewollt,
   damit die Gründe auswertbar bleiben.
4. **Auch im Abschlusstest?** Empfehlung: im Üben jederzeit, im Test **erst nach der Antwort** auf die
   Karte (die Klausur ist strikt eins nach dem anderen), außerdem auf dem Ergebnisblatt.
5. **Wirkung auf Lernstand und Pflicht.** Empfehlung: **keine** – kein Überspringen, keine Wertung, kein
   Kasten. „Zu schwer“ darf keine Karte loswerden (Anti-Cheat). Kosten: Dass sich etwas tut, merkt das Kind
   erst über den Vater.
6. **Wer sieht sie?** Empfehlung: **alle Supervisor** des Kindes (Multi-Supervisor) im Vater-Web, mit Kind,
   Übung, Karte, Antwort des Kindes, Zeitpunkt und Status *offen/erledigt*. Eine Creator-Sicht je Übung
   lohnt erst, wenn fremde Kinder fremde Übungen spielen (Lehrer-Konto) – zurückstellen.
7. **Bekommt das Kind eine Antwort?** Empfehlung: **nein**, das ist
   [B-586](B-586-kind-fragt-den-betreuer-zur-aufgabe.md). Höchstens ein „Papa hat's gesehen“, wenn der Vater
   die Rückmeldung auf *erledigt* setzt. Zwei Knöpfe an der Karte sind einer zu viel: B-586 käme später als
   dritte Möglichkeit in denselben Dialog.
8. **Wie erreicht sie das Erstellen neuer Übungen?** Empfehlung: **abspalten.** B-623 liefert Erfassen,
   Vater-Sicht und einen lesbaren Endpunkt. Eine Folge-Story bringt die Rückmeldungen als Regeln ins Briefing
   des KI-Creators (`CreatorBriefing.cs:17`, etwa „nicht über Unit X hinaus“, „Antworten kürzer“) und als
   Hinweis an die Übung im Creator-Editor. Grund: Wie sie im Prompt wirken (nur für dieses Kind oder für
   alle), ist eine eigene Produktfrage und würde die Story sonst verdoppeln.
9. **Mehrfach-Abgabe.** Empfehlung: erlaubt, aber je Kind, Karte und Grund nur einmal *offen*. Ein zweites
   „zu schwer“ an derselben Karte wird zusammengelegt statt verdoppelt.

## Akzeptanzkriterien (Entwurf)

1. An jeder Karte im Üben gibt es einen Knopf „Rückmeldung“. Er öffnet die festen Gründe zur Mehrfachauswahl
   und ein optionales Textfeld; Absenden ohne Grund **und** ohne Text ist gesperrt.
2. Ein `POST` unter `student/…` legt die Rückmeldung mit Kind, Plan, Position, Übung und – falls angegeben –
   Karte an. Kontext-Ids, die nicht zum Kind gehören, werden abgewiesen (Integrationstest: fremdes Kind,
   fremde Position).
3. Die Rückmeldung ändert weder Kasten, Punkte, Pflicht noch den Sitzungs-Cursor (Integrationstest: Zustand
   vorher gleich nachher).
4. Im Abschlusstest ist der Knopf erst nach der Antwort auf die Karte aktiv.
5. Jeder Supervisor des Kindes sieht die Rückmeldungen im Vater-Web, filterbar nach Kind und Status, mit
   Übung, Karte und Antwort. Er kann sie auf *erledigt* setzen und von dort die Übung öffnen.
6. Ein Endpunkt liefert die Rückmeldungen je Übung bzw. Unit für den Supervisor lesbar – die Grundlage der
   Folge-Story für den KI-Creator.
7. Nach dem Absenden zeigt die Sohn-App eine kurze Bestätigung und bleibt auf derselben Karte.

## Verlauf

- **2026-11-17** — angelegt aus dem Chat mit dem Vater.
- **2026-11-17** — ausformuliert: Ist-Stand gegen den Code (`Remark`, `ExerciseRating`, Karten-Adresse,
  Briefing des KI-Creators), neun offene Punkte mit Empfehlung.
