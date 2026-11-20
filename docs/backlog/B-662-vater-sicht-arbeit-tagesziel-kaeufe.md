---
tags: [typ/story, status/verworfen, bereich/frontend, rolle/supervisor]
aliases: [Vater-Startseite, Was mich als Vater interessiert]
status: verworfen
prio: P2
art: Wunsch
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: "Vater am 2026-11-18, Feedback nach der ersten echten Runde mit Friedrich"
grund: "geteilt, faktisch XL: Sammel-Endpunkt, Antwortzahl-Feld und Startseiten-Umbau samt 15 betroffenen Specs sind drei einzeln baubare Stücke (Backend zuerst)"
ersetzt_durch: [B-747, B-770, B-749]
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

Alle Punkte entschieden; die Empfehlung steht durchgestrichen daneben.

1. ~~**Ersetzt die Vater-Sicht die Startseite oder kommt sie als eigene Seite?** *Empfehlung:* die Betreuen-Startseite (`/vater`) wird zu dieser Sicht; Kind anlegen und Kinder-Tabelle ziehen auf eine Seite „Kinder verwalten". So bleibt jede Route, und das Lesezeichen des Vaters gilt weiter.~~ → Entscheidung 1.
2. ~~**Ein Endpunkt über alle Kinder für offene Anfragen?** Der Weg entscheidet `wo`. *Empfehlung:* ja, `GET supervisor/shop/activations?status=Pending` über alle Kinder des Supervisors (ausstellergebunden: `SupervisorId`-Filter bleibt). Dann ist die Story `wo: beides`, Backend zuerst, und die Zahl am Navigationspunkt „Shop" kostet einen Aufruf. Ohne Endpunkt ginge es mit N Aufrufen, was bei drei Kindern ginge, aber nicht skaliert.~~ → Entscheidung 2.
3. ~~**Was heißt „wie gut"?** Punkte heute, Trefferquote oder Anzahl geübter Karten? *Empfehlung:* zuerst Punkte plus Antwortzahl; die Trefferquote nur, wenn sie ohne neue Auswertung aus `PositionReportService`/`MetricsService` kommt. Nicht jede Zahl, die es gibt, gehört auf die Startseite.~~ → Entscheidung 3.
4. ~~**Telefonbreite: Ziel oder nur „nicht kaputt"?** *Empfehlung:* Ziel nur für die Betreuen-Sicht (die drei Fragen und das Einlösen); Zuweisen und Erstellen bleiben Web (siehe B-663).~~ → Entscheidung 4.
5. ~~**Vorschläge ohne Entscheidung:** B-662 und B-663 greifen ineinander (Perspektive wählen, Startseite je Perspektive). *Empfehlung:* gemeinsam grillen; die Entscheidungen über die aktive Rolle fallen zuerst in B-663.~~ → Entscheidung 5.

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet) auf Anweisung des Nutzers (Empfehlung übernommen), unbestätigt.*

## Entscheidungen

Entscheidung 6 ist die Rahmenentscheidung B, Entscheidung 7 die Rahmenentscheidung C (führend B-681). Die Frontmatter-Angabe `wo` wird beim Schätzen auf `beides` gesetzt (Entscheidung 2).

1. **Die Betreuen-Startseite `/vater` wird zur Vater-Sicht; Kind anlegen, Kinder-Tabelle und Pläne-Karte ziehen auf eine eigene Verwaltungsseite unter `/vater`.** Begründung: So bleibt jede Route, und das Lesezeichen des Vaters gilt weiter; die drei Fragen des Vaters sind die Startseite. Kosten: ein Klick mehr zum Anlegen eines Kindes; die Verwaltungsseite braucht einen Navigationspunkt. Verworfen: eine zusätzliche Seite neben der alten Startseite – die Startseite bliebe voll. Schließt aus: einen Vater, der beim Öffnen zuerst durch Verwaltung blättert, bevor er sieht, ob das Kind gelernt hat.
2. **Es gibt einen Endpunkt über alle Kinder für offene Einlösungen:** `GET supervisor/shop/activations?status=Pending`, ausstellergebunden (`SupervisorId`-Filter) und auf eigene Kinder begrenzt; die Zahl offener Anfragen steht am Navigationspunkt „Shop“. Die Story ist damit `wo: beides`, Backend zuerst, das Antwortfeld ist additiv (kein Vertragsbruch). Begründung: Ohne ihn wären es N Einzelabrufe je Seitenaufruf; die je-Kind-Variante (`ShopController.cs:457`) bleibt bestehen. Kosten: ein neuer Endpunkt mit eigener Eigentumsprüfung und Test. Verworfen: N Abrufe im Frontend. Schließt aus: ein Einlösen, das erst nach der Wahl eines Kindes sichtbar wird (die User Story verlangt es ohne Klick).
3. **„Wie gut“ heißt Punkte heute und Zahl der heute beantworteten Aufgaben je Kind;** die Trefferquote erscheint nur, wenn sie ohne neue Auswertung aus `PositionReportService` oder `MetricsService` kommt. Begründung: Nicht jede vorhandene Zahl gehört auf die Startseite; die Antwortzahl gibt es in `ChildDay` bisher nicht und ist ein additives Feld. Kosten: ein neues Feld und eine Zählung über `ItemReviewEvent` und `TestItemResult`. Verworfen: Trefferquote als Pflichtzahl. Schließt aus: eine Qualitätsaussage („gut“ im Sinn von richtig) auf der Startseite im ersten Bau.
4. **Die Telefonbreite ist Ziel nur für die Betreuen-Sicht (die drei Fragen und das Einlösen);** Zuweisen und Erstellen bleiben Web (B-663). Eine Playwright-Probe bei 390 px kommt zu der bestehenden bei 768 px. Begründung: Der Vater erledigt am Handy nur Betreuen; der Rest ist am Rechner. Kosten: eine zusätzliche E2E-Probe; Zuweisen und Erstellen bleiben auf dem Handy unbedient. Verworfen: das ganze Vater-Web auf dem Handy bedienbar machen. Schließt aus: einen Vater, der unterwegs einen Plan anlegt.
5. **B-663 wird vor B-662 gebaut;** die Betreuen-Startseite entsteht in der Perspektive, die B-663 festlegt. Begründung: Die Perspektiv-Weiche fällt mit der Telefonbreite zusammen, und die Entscheidungen über die aktive Rolle fallen zuerst in B-663. Kosten: B-662 kann nicht vor B-663 abgenommen werden. Verworfen: beide gemeinsam in einer Story. Schließt aus: eine Startseite, die ihre Perspektive noch nicht kennt.
6. **Der Vater setzt Leitplanken und hat eine Notbremse, er legt keine Tagesmengen fest (Rahmen B, führend B-681).** Je Kind setzt der Vater einmal Boden und Deckel für Pflichtaufgaben und neue Wörter, den Malus-Betrag und Pausen (B-644 Entscheidungen 2 bis 5 und 8) und behält die Notbremse: eine nicht begonnene Aufgabe streichen oder eine eigene dazulegen (B-644 Entscheidung 7). Er baut keinen Plan und bestimmt keine Tagesmenge. Begründung: Ein Planer braucht eine menschliche Grenze gegen Fehlplanung; die vier Zahlen sind die eigenen Entscheidungen des Vaters vom 2026-11-17 und bleiben unberührt; Belohnung, Shop und Einlösen bleiben seine Aufgabe, die Tagesarbeit nicht. Kosten: Der Vater trägt weiter vier Zahlen und eine Pause je Kind; „nur Belohnung“ aus dem Leitbild ist damit nicht wörtlich erfüllt (abgewichen von B-680 Punkt 3, der Boden und Deckel zu Planer-Voreinstellungen machte). Verworfen: Boden und Deckel als reine Planer-Voreinstellung – hübe B-644 Entscheidung 4 auf und ließe den Planer ohne harte Grenze. Schließt aus: einen Vater, der gar nicht mehr in die Mengen eingreifen kann; die User Story „ich möchte die Plansteuerung nicht mehr selbst tragen“ gilt für die tägliche Arbeit, nicht für die Grenzen. (Gilt für den Inhalt der Vater-Sicht: Betreuen, Einlösen, Notbremse – keine Plan-Maske.)
7. **Es gibt keine neue Rolle `Teacher` und keine Zuordnung `TeacherLink` (Rahmen C, führend B-681).** Plan-, Positions-, Klassenarbeits- und Shop-Schreibrouten bleiben `Roles.Supervisor`; ein Lehrer-Konto (Creator ohne Betreuungsauftrag, `TeacherAccountsController.cs:12`) steuert keine Pläne. Begründung: Rahmen A macht den Planer zu Server-Code ohne Identität, ein Lehrer-Zugang zu einem Kind wird also nicht gebraucht; er bräuchte Schema (Migration), den Umbau von rund 33 Gate-Stellen (grep 2026-11-19) und eine zweite Eigentumsprüfung neben `SupervisorOwnsChildAsync`. Kosten: Ein menschlicher Lehrer mit Plan-Steuerung für ein Kind ist nicht möglich; kommt er, ist ein eigenes `TeacherLink` (nicht `SupervisorLink`, damit der Shop zu bleibt) die vorgemerkte Lösung, aber heute nicht gebaut. Verworfen: Gate `AnyAdult` plus `TeacherLink` jetzt (Empfehlung 1 und 2 aus B-681). Schließt aus: die Lesart der User Story von B-681, ein Lehrer-Konto steuere Pläne eines Kindes ohne Wallet-Sicht; erfüllt wird sie dadurch, dass der Planer kein Wallet liest und kein Konto hat. (Gilt für Kriterium 6: ein Lehrer-Konto sieht die Seite nicht.)
8. **Das „Tagesziel erreicht“ der Startseite liest heute `GoalsMet` und `GoalsTotal` und liest nach dem Bau des Planers (B-644 E3) dieselben Felder aus dem Tagesplan.** Begründung: Die Anzeige soll beim Wechsel nicht umgebaut werden. Kosten: Die Bedeutung von „Ziele erfüllt / gesamt“ ändert sich still von Position zu Pflichtaufgabe. Verworfen: eine zweite Anzeige für den Planer. Schließt aus: zwei konkurrierende Tagesziel-Anzeigen.

## Akzeptanzkriterien

Final. Je Kriterium die Änderung, die es rot macht.

1. Die Betreuen-Startseite zeigt je Kind: geübt heute, Tagesziel („n von m“) und Punkte heute – und keine Kinder-Tabelle, kein Anlegeformular, keine Pläne-Karte. *Rot, wenn* eines dieser Elemente weiter auf `/vater` steht (`VaterDashboard.tsx`).
2. Offene Einlöse-Anfragen aller Kinder stehen auf einer Seite, ohne ein Kind zu wählen. *Rot, wenn* die Liste weiter nur nach Kindwahl erscheint (`VaterShop.tsx`).
3. Die Zahl offener Anfragen steht an der Navigation und sinkt nach dem Freigeben. *Rot, wenn* der Zähler fehlt oder nach `approve` stehen bleibt.
4. Der Endpunkt über alle Kinder liefert nur Anfragen des eigenen Supervisors (`SupervisorId`) und nur eigener Kinder. *Rot, wenn* der Filter fehlt (ein fremder Vater sähe Anfragen).
5. Die Antwortzahl je Kind ist die Zahl der heute gewerteten Antworten (`ItemReviewEvent` und `TestItemResult`), nicht aus Punkten abgeleitet: ein Kind mit Bonus-Punkten und null Antworten zeigt 0. *Rot, wenn* die Zahl aus `PointsToday` entsteht.
6. Bei 390 px Breite scrollt die Betreuen-Startseite nicht seitlich, und Freigeben ist erreichbar. *Rot, wenn* die neue Playwright-Probe bei 390 px einen Überlauf misst.
7. *Absichtserklärung.* Ein Lehrer-Konto (nur Creator) sieht die Seite nicht. *Rot, wenn* `perspectivesFor` sie ihm zeigt.

## Schätzung

Nicht geschätzt, sondern geteilt: `wo` wird `beides` (Entscheidung 2); Backend mit zwei neuen Stücken, eine Startseite, die fünfzehn
E2E-Specs berührt, und eine 390-px-Probe wären ein XL. Zerlegung nach „Teilen und Zusammenlegen":

1. [B-747](B-747-einloesungen-aller-kinder-endpunkt.md) · Endpunkt `supervisor/shop/activations` über alle Kinder (S, backend; Entscheidung 2).
2. [B-770](B-770-antwortzahl-je-kind-und-tag.md) · `ChildDay.AnswersToday` (S, backend; Entscheidung 3).
3. [B-749](B-749-vater-startseite-entruempeln-und-einloesen.md) · Startseite, Verwaltungsseite, Zähler, Handy-Probe (L, frontend; Entscheidungen 1, 4 bis 8).

Bau-Reihenfolge: [B-663](B-663-profilwahl-vater-oder-creator.md) zuerst (Entscheidung 5), dann B-747 und B-770, zuletzt B-749.

## Verlauf

- 2026-11-18 · idee · aufgenommen aus dem Feedback des Vaters
- 2026-11-19 · ausformuliert (Vorbereitungslauf): Ist-Stand am Code. „Heute" zeigt Status, Ziele und Punkte schon; es fehlen
  kindübergreifende offene Einlösungen mit Zähler, „wie gut" über Punkte hinaus, eine entrümpelte Startseite und eine
  Telefon-Probe (nur 768 px belegt). Die Rollentrennung tragen die Perspektiven seit 2026-07-28. Hängt an B-663.
- 2026-11-20 · gegrillt (Vorbereitungslauf, Sonnet, stellvertretend auf Anweisung des Nutzers, unbestätigt): alle fünf Punkte entschieden. Kern: `/vater` wird die Vater-Sicht, Verwaltung zieht um, Endpunkt für offene Einlösungen über alle Kinder, „wie gut“ = Punkte plus Antwortzahl, Telefonbreite nur Betreuen, B-663 zuerst.
- 2026-11-20 · verworfen (geteilt, Vorbereitungslauf, Sonnet): faktisch XL, zerlegt in B-747, B-770, B-749; Eintrittsbedingung `gegrillt` geprüft und erfüllt. B-663 bleibt vor dem Frontend-Teil.
