---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/supervisor]
aliases: [Vater-Startseite, Was mich als Vater interessiert]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: "Vater am 2026-11-18, Feedback nach der ersten echten Runde mit Friedrich"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-662 · Die Vater-Sicht zeigt nur, was den Vater interessiert: Arbeit, Tagesziel, Käufe

Der Vater will in der Oberfläche eigentlich nur drei Dinge sehen:

1. **Wie arbeiten die Kinder?** (heute geübt, wie viel, wie gut)
2. **Haben sie ihr Tagesziel erreicht?**
3. **Was wurde gekauft – und was ist einzulösen?** (offene Einlösungen freigeben, wie heute die 30 Minuten
   Spielzeit für Friedrich)

Alles andere (Übungen bauen, Katalog, Plan-Feinheiten) stört in dieser Rolle. Hängt eng an
[B-663](B-663-profilwahl-vater-oder-creator.md) (welche Rolle ist gerade aktiv).

## User Story

Als **Vater** möchte ich auf meiner Startseite genau sehen, ob und wie meine Kinder heute gelernt und ihr Tagesziel
erreicht haben und was sie gekauft haben und noch einzulösen ist, **damit** ich das am Handy in einem Blick erledige,
ohne durch Werkzeuge zu klicken, die ich in dieser Rolle nicht brauche.

## Ist-Stand am Code

**Zu 1 und 2: die Startseite zeigt es schon zum Teil.**

- **1.** „Heute" ist die erste Karte der Betreuen-Startseite: je Kind Status (kein Tagesziel, geschafft, dran, offen), Ziele
   „erfüllt / gesamt" und Punkte heute (`frontend/src/vater/VaterDashboard.tsx:57-81`). Die Daten kommen aus
   `ChildrenDashboardService.BuildAsync` (`backend/Pugling.Api/Services/Supervisor/ChildrenDashboardService.cs:18-40`) und
   haben genau die Felder `ActivePlans, GoalsTotal, GoalsMet, PointsToday, DutyDone, Practiced`
   (`backend/Pugling.Contracts/Supervisor/GoalDtos.cs:36-37`).
- **2.** „Wie viel, wie gut" geht nicht über **Punkte heute** hinaus. Eine Trefferquote, Antwortzahl oder Übungsdauer je
   Kind und Tag enthält `ChildDay` nicht (nicht gefunden). Darunter liegt die Zeitachse (`ChildrenTimeline`,
   `VaterDashboard.tsx:82`), die über Tage summiert.
- **3.** Dieselbe Seite trägt weiter eine Kinder-Tabelle mit Id, Klasse, Schulart und Interessen, das Formular „Kind
   anlegen" und die Lehrpläne-Karte (`VaterDashboard.tsx:84-150`). Diese gehören zur Verwaltung, nicht zu den drei
   Fragen.

**Zu 3: Käufe und Einlösen gibt es nur je Kind, an anderer Stelle.**

- **4.** Offene Anfragen zum Einlösen stehen im Shop-Bereich `/vater/shop`
   (`frontend/src/vater/navigation.ts:66`, `VaterShop.tsx:500-520`): erst ein Kind wählen, dann
   `api.childActivations(childId)` (`frontend/src/lib/api.ts:901`) und Freigeben/Ablehnen
   (`VaterShop.tsx:524-552`). Der Endpunkt ist je Kind (`backend/Pugling.Api/Controllers/Supervisor/ShopController.cs:457`).
- **5.** Eine Liste über **alle** Kinder und einen Zähler offener Anfragen auf der Startseite oder in der Navigation:
   **nicht gefunden** (`grep -i pending` unter `frontend/src/vater` ohne Treffer in `VaterDashboard`/`navigation.ts`).
   Für „was ist einzulösen?" ohne Kindwahl fehlt damit ein Aufruf über alle Kinder; ohne neuen Endpunkt wären es N
   Einzelabrufe.

**Zu „alles andere stört":**

- **6.** Die Trennung gibt es schon als Perspektiven: Betreuen hat sechs Bereiche, Katalog und Übungen liegen unter Erstellen,
   Pläne unter Zuweisen (`frontend/src/vater/navigation.ts:54-97`, Plan `docs/vater-perspektiven-plan.md`, Stand
   2026-07-28). Der Umschalter bleibt für ein Vater-Konto sichtbar (`frontend/src/vater/VaterApp.tsx:120`).

**Zu „auf dem Handy bedienbar?":**

- **7.** Eine Probe gibt es nur bei **768 px** (`frontend/e2e/responsive-vater.spec.ts:19`; [B-68](B-68-vater-web-responsive.md)
   ist dafür abgenommen). Eine Probe bei Telefonbreite (etwa 390 px): **nicht gefunden**. `index.css` hat keinen
   `max-width`-Medienblock für `.vater-*` (die Blöcke stehen bei `:246`, `:492`, `:712`, `:802`, davon nur einer mit
   `max-width`, und der gilt der Landingpage); die Navigation bricht nur um (`index.css:457`). Ob das auf dem Handy
   trägt, ist **nicht gemessen**.

## Die echte Lücke

1. **Käufe/Einlösen kindübergreifend an einem Ort**, mit Zähler in der Betreuen-Sicht (Punkte 4 und 5). Das ist die
   einzige der drei Fragen, die heute keine Antwort auf einer Seite hat.
2. **„Wie viel, wie gut" je Kind und Tag** (Punkt 2), falls Punkte nicht genügen.
3. **Eine Startseite, die nur diese Antworten zeigt**, und die Verwaltung (Kind anlegen, Kinder-Tabelle, Pläne-Karte)
   anderswohin (Punkt 3).
4. **Telefonbreite** (Punkt 7), vor allem für das Einlösen.

Die Rollentrennung aus der Idee ist **nicht** der Kern: die Perspektiven tragen sie seit Juli (Punkt 6); was B-663
daran noch ändert, steht dort.

## Offene Punkte

Je mit Empfehlung; keine Entscheidung. Die Aufteilung zwischen Vater- und Lehrer-Sicht gehört zur Richtung
„Leitbild Privatlehrer" ([B-681](B-681-rollenschnitt-vater-belohnt-lehrer-unterrichtet.md)) und ist hier nur
Randbedingung.

1. **Ersetzt die Vater-Sicht die Startseite oder kommt sie als eigene Seite?**
   *Empfehlung:* die Betreuen-Startseite (`/vater`) wird zu dieser Sicht; Kind anlegen und Kinder-Tabelle ziehen auf eine
   Seite „Kinder verwalten". So bleibt jede Route, und das Lesezeichen des Vaters gilt weiter.
2. **Ein Endpunkt über alle Kinder für offene Anfragen?** Der Weg entscheidet `wo`.
   *Empfehlung:* ja, `GET supervisor/shop/activations?status=Pending` über alle Kinder des Supervisors (ausstellergebunden:
   `SupervisorId`-Filter bleibt). Dann ist die Story `wo: beides`, Backend zuerst, und die Zahl am Navigationspunkt
   „Shop" kostet einen Aufruf. Ohne Endpunkt ginge es mit N Aufrufen, was bei drei Kindern ginge, aber nicht skaliert.
3. **Was heißt „wie gut"?** Punkte heute, Trefferquote oder Anzahl geübter Karten?
   *Empfehlung:* zuerst Punkte plus Antwortzahl; die Trefferquote nur, wenn sie ohne neue Auswertung aus
   `PositionReportService`/`MetricsService` kommt. Nicht jede Zahl, die es gibt, gehört auf die Startseite.
4. **Telefonbreite: Ziel oder nur „nicht kaputt"?**
   *Empfehlung:* Ziel nur für die Betreuen-Sicht (die drei Fragen und das Einlösen); Zuweisen und Erstellen bleiben
   Web (siehe B-663).
5. **Vorschläge ohne Entscheidung:** B-662 und B-663 greifen ineinander (Perspektive wählen, Startseite je Perspektive).
   *Empfehlung:* gemeinsam grillen; die Entscheidungen über die aktive Rolle fallen zuerst in B-663.

## Akzeptanzkriterien (Entwurf)

Je Kriterium die Änderung, die es rot macht. Entwurf; endgültig erst nach der Grill-Runde.

1. Die Betreuen-Startseite zeigt je Kind: geübt heute, Tagesziel erreicht (ja/nein, „n von m"), Punkte heute – und keine
   Kinder-Tabelle, kein Anlegeformular, keine Pläne-Karte. *Rot, wenn* eines dieser Elemente weiter auf `/vater`
   steht (`VaterDashboard.tsx:84-150`).
2. Offene Einlöse-Anfragen aller Kinder stehen auf einer Seite, ohne ein Kind zu wählen. *Rot, wenn* die Liste weiter
   nur nach Kindwahl erscheint (`VaterShop.tsx:500-520`).
3. Die Zahl offener Anfragen steht an der Navigation oder auf der Startseite und sinkt nach dem Freigeben. *Rot, wenn*
   der Zähler fehlt oder nach `approve` stehen bleibt.
4. Der Endpunkt über alle Kinder liefert nur Anfragen des eigenen Supervisors (`SupervisorId`) und nur eigener Kinder.
   *Rot, wenn* der Filter fehlt: dann sähe ein Vater die Anfragen eines anderen (`ShopController.cs:457-474` filtert je
   Kind und Aussteller).
5. Bei 390 px Breite scrollt die Betreuen-Startseite nicht seitlich, und Freigeben ist erreichbar. *Rot, wenn* die
   neue Playwright-Probe bei 390 px den Überlauf misst (heute gibt es nur 768 px, `responsive-vater.spec.ts:19`).
6. Ein Lehrer-Konto (`Creator`) sieht die Seite nicht. *Rot, wenn* `perspectivesFor` sie ihm zeigt.

## Verlauf

- 2026-11-18 · idee · aufgenommen aus dem Feedback des Vaters
- 2026-11-19 · ausformuliert (Vorbereitungslauf): Ist-Stand am Code. „Heute" zeigt Status, Ziele und Punkte schon; es fehlen
  kindübergreifende offene Einlösungen mit Zähler, „wie gut" über Punkte hinaus, eine entrümpelte Startseite und eine
  Telefon-Probe (nur 768 px belegt). Die Rollentrennung tragen die Perspektiven seit 2026-07-28. Hängt an B-663.
