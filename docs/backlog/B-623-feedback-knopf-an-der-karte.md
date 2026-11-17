---
tags: [typ/story, status/gegrillt, bereich/training, bereich/katalog, bereich/agent, rolle/student, rolle/creator, rolle/supervisor]
aliases: [Feedback-Knopf, Rückmeldung zur Übung]
status: gegrillt
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

Alle in der Grill-Runde vom 2026-11-17 entschieden – siehe Entscheidungen.

1. ~~**Neue Entität oder `Remark` erweitern?**~~ → Entscheidung 9.
2. ~~**Woran hängt sie: Übung, Karte oder beides?**~~ → Entscheidung 9.
3. ~~**Feste Gründe.**~~ → Entscheidung 4.
4. ~~**Auch im Abschlusstest?**~~ → Entscheidung 5 (abweichend von der Empfehlung „nach der Antwort“: nur
   Ergebnisblatt).
5. ~~**Wirkung auf Lernstand und Pflicht.**~~ → Entscheidung 6.
6. ~~**Wer sieht sie?**~~ → Entscheidung 7 (Creator-Sicht nicht zurückgestellt, sondern Teil der Story).
7. ~~**Bekommt das Kind eine Antwort?**~~ → Entscheidung 8.
8. ~~**Wie erreicht sie das Erstellen neuer Übungen?**~~ → Entscheidungen 1–3 (abweichend von der Empfehlung
   „abspalten“: gehört in diese Story).
9. ~~**Mehrfach-Abgabe.**~~ → Entscheidung 9.

## Entscheidungen

Grill-Runde mit dem Vater, 2026-11-17.

1. **Erfassen, Sehen und Wirken sind eine Story.** Der Vater will, dass Rückmeldungen das Erstellen neuer
   Übungen erreichen, und nicht nur über ihn wirken. Abweichend von der Empfehlung „abspalten“.
   *Kosten:* Die Story wird etwa doppelt so groß (Backend, Vater-Web, Sohn-App **und** `Pugling.Agent.Creator`);
   die Wirkung im Prompt wird ohne Erfahrungswerte aus echten Rückmeldungen entworfen.
2. **Die Reichweite hängt am Grund.** Gründe, die am Kind liegen (zu schwer, zu leicht, hatten wir noch
   nicht, gut so), wirken nur, wenn für **dieses Kind** erstellt wird (`--child`). Gründe, die an der Übung
   liegen (falsch übersetzt, zu viel verlangt, zu streng bewertet, mehrere Bedeutungen), wirken für
   **alle**, auch beim Erstellen für ein Profil. Begründung: Theodor und Friedrich lernen verschiedene Bücher
   in verschiedenem Tempo; ein Übersetzungsfehler ist dagegen für jeden einer. *Kosten:* Jeder Grund trägt
   fest eine Reichweite; ein neuer Grund braucht diese Zuordnung mit.
3. **„Wirken“ heißt: beim Erstellen neuer Übungen und als Hinweis im Creator-Editor.** Der KI-Creator nimmt
   die passenden Rückmeldungen in sein Briefing (`CreatorBriefing.cs:17`) und leitet daraus Regeln ab; an der
   Übung im Creator-Editor steht, was gemeldet wurde. Bestehende Übungen ändert niemand automatisch – kein
   Reparatur-Modus des Agenten, keine Sperre der Karte. Begründung: Der Agent läuft nur auf Aufruf, eine
   Sperre wäre ein Selbstbetrug-Hebel. *Kosten:* Eine falsch übersetzte Karte bleibt im Plan, bis der Vater
   sie von Hand korrigiert.
4. **Acht feste Gründe, Mehrfachauswahl, Freitext optional;** abschicken geht mit mindestens einem Grund
   **oder** Text. Die Liste ist geschlossen im Vertrag:

   | Knopf (Text für das Kind) | Arbeitsname | Reichweite |
   | --- | --- | --- |
   | Zu schwer | `TooHard` | nur dieses Kind |
   | Zu leicht | `TooEasy` | nur dieses Kind |
   | Hatten wir noch nicht im Unterricht | `AheadOfClass` | nur dieses Kind |
   | Falsch übersetzt | `WrongTranslation` | alle |
   | Zu viel verlangt (mehr als die Vokabel) | `TooMuchAsked` | alle |
   | Zu streng bewertet (Akzent, ein Buchstabe) | `TooStrict` | alle |
   | Wort hat mehrere Bedeutungen | `SeveralMeanings` | alle |
   | Gut so 👍 | `Liked` | nur dieses Kind |

   „Zu streng bewertet“ wirkt für alle, obwohl die Strenge eine Server-Regel ist (B-422/4): Die Meldungen
   sind dann der Beleg für [B-624](B-624-akzentfehler-nicht-ganzer-fehler.md), und der KI-Creator wählt
   Antworten mit wenig Akzent-Fallen. *Kosten:* Ein neuer Grund ist eine Vertragsänderung.
5. **Im Abschlusstest nur auf dem Ergebnisblatt,** je Karte neben der Lösung; im Üben jederzeit. Begründung:
   Die ärgerlichsten Fälle sieht das Kind erst mit der Lösung, und ein Knopf mitten im Test lenkt ab.
   *Kosten:* „Zu schwer“ lässt sich erst nach dem Test melden.
6. **Keine Wirkung auf Lernstand, Punkte, Pflicht, Malus oder Sitzungs-Cursor.** Anti-Cheat: Eine Meldung
   darf keine Antwort und keine Karte loswerden. Das nachträgliche Umbuchen nach Bestätigung durch den Vater
   („berechtigt“) ist eine eigene Story: [B-627](B-627-berechtigte-meldung-bucht-nach.md). *Kosten:* Bis zur
   Korrektur zahlt das Kind für einen Fehler, den es nicht gemacht hat.
7. **Zwei Sichten.** Alle **Supervisor** des Kindes (Multi-Supervisor) sehen dessen Rückmeldungen vollständig
   im Vater-Web – Kind, Übung, Karte, Antwort, Freitext, Zeitpunkt – und setzen sie auf *erledigt* (gilt für
   alle Betreuer). Der **Ersteller** der Übung sieht im Creator-Editor an seiner Übung die gezählten Gründe
   **ohne** Freitext und **ohne** Kind. Begründung: Ein Lehrer-Konto darf den Freitext fremder Kinder nicht
   lesen, braucht aber die Gründe zum Reparieren. *Kosten:* zwei Sichten auf dieselben Daten.
8. **Das Kind bekommt eine Bestätigung, keine Antwort.** Nach dem Absenden „Danke, ist angekommen“; setzt ein
   Betreuer die Meldung auf *erledigt*, sieht das Kind einen kurzen Hinweis („Papa hat deine Meldung zu
   *voyage* erledigt ✔“). Antworttext bleibt [B-586](B-586-kind-fragt-den-betreuer-zur-aufgabe.md).
   Begründung: Ohne Rückkanal hört das Kind auf zu melden. *Kosten:* ein Hinweis-Platz in der Sohn-App und ein
   Lesestatus je Kind.
9. **Technik nach Empfehlung.** Eine **neue Entität** (Arbeitsname `ExerciseFeedback`) statt `Remark`, weil
   `Remark` der Antwort-Workflow für Claude Code ist; Migration additiv. Übung und Position sind **Pflicht**,
   die Karte (`ItemId`, damit `VocabularyId`) **optional**. Derselbe Grund desselben Kindes an derselben Karte
   wird, solange er offen ist, **zusammengelegt** (Zähler hoch, neuer Freitext angehängt). *Kosten:* eine
   Migration; die Häufigkeit steht im Zähler, nicht in der Zeilenzahl.

## Akzeptanzkriterien

1. An jeder Karte im Üben gibt es einen Knopf „Rückmeldung“. Er öffnet die acht Gründe aus Entscheidung 4 zur
   Mehrfachauswahl und ein optionales Textfeld; Absenden ohne Grund **und** ohne Text ist gesperrt.
2. Ein `POST` unter `student/…` legt die Rückmeldung mit Kind, Plan, Position, Übung und – falls angegeben –
   Karte an. Kontext-Ids, die nicht zum Kind gehören, werden abgewiesen (Integrationstest: fremdes Kind,
   fremde Position, fremde Karte).
3. Die Rückmeldung ändert weder Kasten, Punkte, Pflicht, Malus noch den Sitzungs-Cursor (Integrationstest:
   Zustand vorher gleich nachher).
4. Im Abschlusstest gibt es den Knopf nur auf dem Ergebnisblatt, je Karte; während des Tests nicht.
5. Ein zweiter gleicher Grund desselben Kindes an derselben Karte erzeugt keine zweite offene Zeile, sondern
   erhöht den Zähler und hängt einen neuen Freitext an.
6. Jeder Supervisor des Kindes sieht die Rückmeldungen im Vater-Web, filterbar nach Kind und Status, mit
   Übung, Karte, Antwort und Freitext; er kann sie auf *erledigt* setzen und von dort die Übung öffnen. Ein
   Supervisor eines anderen Kindes sieht sie nicht.
7. Der Ersteller sieht im Creator-Editor an seiner Übung die Gründe gezählt, ohne Freitext und ohne Kind.
8. Das Briefing des KI-Creators enthält die Rückmeldungen nach Entscheidung 2: beim Erstellen für ein Kind
   dessen kindbezogene **und** alle übungsbezogenen Gründe der Unit, beim Erstellen für ein Profil nur die
   übungsbezogenen; der Prompt leitet daraus Regeln ab (Testfall: „Hatten wir noch nicht“ von Kind A erscheint
   nicht im Briefing für Kind B, „Falsch übersetzt“ erscheint in beiden).
9. Nach dem Absenden zeigt die Sohn-App „Danke, ist angekommen“ und bleibt auf derselben Karte; nach
   *erledigt* sieht das Kind einen einmaligen Hinweis.

## Verlauf

- **2026-11-17** — angelegt aus dem Chat mit dem Vater.
- **2026-11-17** — ausformuliert: Ist-Stand gegen den Code (`Remark`, `ExerciseRating`, Karten-Adresse,
  Briefing des KI-Creators), neun offene Punkte mit Empfehlung.
- **2026-11-17** — gegrillt mit dem Vater: neun Entscheidungen; abweichend von den Empfehlungen gehört das
  Wirken beim Erstellen in diese Story (E1) und der Test bekommt den Knopf nur auf dem Ergebnisblatt (E5).
  Folge-Story B-627 (Nachbuchen nach „berechtigt“) angelegt.
