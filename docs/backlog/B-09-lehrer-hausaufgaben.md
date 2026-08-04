---
tags: [typ/story, status/geschaetzt, bereich/katalog, bereich/auth, bereich/training, rolle/creator, rolle/supervisor, rolle/student]
aliases: [Lehrer-Hausaufgaben, Klassen, Beitrittscode, Zuweiser ohne Betreuungsauftrag]
status: geschaetzt
prio: P3
art: Wunsch
groesse: L
wo: backend
migration: ja
vertragsbruch: nein
quelle: docs/lehrer-konto-plan.md
---

# B-09 · Lehrer erteilt Hausaufgaben: zuweisen mit Frist, ohne Betreuungsauftrag

**Am 2026-08-01 neu gerahmt** (Nutzer-Entscheidung, Rollen-Durchgang). Der Kern ist nicht mehr die
Ownership-Umkehr an den bestehenden Filtern, sondern: Der Lehrer erteilt **Aufträge** (Fach/Kapitel +
Frist) und sieht deren **Erledigung** — die Übungen dahinter wählt das System je Kind nach dessen
Interessen, sodass jedes Kind sein eigenes Hausaufgaben-Set bekommt. **Punktfrei ist ausdrücklich in
Ordnung**; Belohnung und Malus bleiben beim Supervisor.

Die naheliegende Kurzformel „ein Lehrer ist ein Supervisor ohne Shop" ist als *Intuition* richtig, als
*Definition* aber gefährlich — siehe Ist-Stand: Der Shop ist der kleinste Unterschied. Die Story
definiert ihn darum positiv: **ein Zuweiser mit Frist-Kontrolle**, mit genau zwei Fähigkeiten (Auftrag
erteilen, Erledigung sehen) und ohne jede Kind-Hoheit.

## User Story

Als Lehrer möchte ich einer Klasse eine Aufgabe zu einem Fach/Kapitel mit Frist erteilen, deren Übungen je
Kind nach dessen Interessen ausgewählt werden, damit jedes Kind sein eigenes Hausaufgaben-Set bekommt und
ich sehe, wer sie erledigt hat — ohne für eines dieser Kinder einen Betreuungsauftrag zu brauchen.

## Ist-Stand am Code

- **Die Identität ist gebaut, anders als der alte Entwurf sagte — und weiter als der letzte Stand dieser
  Story**: Lehrer = Erwachsener ohne Betreuungsauftrag (nur Creator-Profil), über
  [AccountService.cs:34](../../backend/Pugling.Api/Auth/AccountService.cs#L34) (`EnsureForTeacherAsync`)
  idempotent angelegt, **nicht nachrüstend** — ein bestehendes Konto behält seine Rollen. Es gibt bereits
  einen anonym erreichbaren Registrierungs-Endpunkt dafür:
  [TeacherAccountsController.cs:44](../../backend/Pugling.Api/Controllers/Creator/TeacherAccountsController.cs#L44)
  (`POST creator/teacher-accounts`, `[AllowAnonymous]`) plus den eigentümer-geprüften Lese-Endpunkt
  ([:73](../../backend/Pugling.Api/Controllers/Creator/TeacherAccountsController.cs#L73)). Eine
  `Teacher`-Entität gibt es bewusst nicht ([lehrer-konto-plan.md](../lehrer-konto-plan.md)) — **der Plan
  dort ist an dieser einen Stelle veraltet**: sein Abschnitt „Offen" nennt für Hausaufgaben weiterhin
  „inklusive `Teacher`-Entität … und Ownership-Umkehr", was die Neu-Rahmung vom 2026-08-01 (siehe
  `## Entscheidungen`) bereits verworfen hat. Diese Story korrigiert das nicht im Plandokument selbst
  (nicht Gegenstand dieses Durchgangs), hält die Abweichung aber hier fest, damit niemand dem alten Satz
  folgt.
- **Zugriff hängt heute ausschließlich am Betreuungsauftrag**:
  [AuthAccess.cs:98](../../backend/Pugling.Api/Auth/AuthAccess.cs#L98) (`SupervisorOwnsChildAsync` über
  `SupervisorLinks`), durchgesetzt vom
  [ChildOwnershipFilter.cs:13](../../backend/Pugling.Api/Auth/ChildOwnershipFilter.cs#L13).
- **Die Supervisor-Rolle ist grobkörnig.** Sie am Lehrer zu setzen (auch „ohne Shop") öffnete unter
  anderem [ChildrenController.cs:20](../../backend/Pugling.Api/Controllers/Supervisor/ChildrenController.cs#L20):
  Kind anlegen/ändern/löschen **inklusive PIN**, Betreuer verwalten (sich also selbst weitere Rechte
  geben), Punkte verschenken und den Kontostand lesen — dazu Pläne, Ziele, Missionen, Stundenplan,
  Klassenarbeiten, Lehrbücher, Interessen. Die Negativliste wäre länger als die Positivliste, und eine
  vergessene Prüfung wäre ein **stiller** Rechte-Überschuss.
- **Ein Plan hat keinen Urheber**: [StudyPlanEntities.cs:53](../../backend/Pugling.Api/Models/StudyPlanEntities.cs#L53)
  trägt nur `ChildId`; „von wem stammt diese Zuweisung" hat heute keinen Ort.
- **Die Kadenz kennt keinen Einmal-Fall**:
  [PlanPositionBaseTypes.cs:4](../../backend/Pugling.Contracts/Common/PlanPositionBaseTypes.cs#L4) hat
  `None | Daily | Weekly`, ein `DueDate` existiert nirgends. Perioden werden als
  `(Taktung, Perioden-Anfang)` gerechnet, die Taktung liegt als **Momentaufnahme** auf der Log-Zeile
  ([PlanPositionEntities.cs:136](../../backend/Pugling.Api/Models/PlanPositionEntities.cs#L136)) — ein
  Einmal-Fall greift also in die Perioden-Arithmetik ein, nicht nur ins Formular.
- **Punktfreiheit braucht keinen Sonderweg**: `Cadence = None` (freies Üben ohne Pflicht),
  `PointsGoalMet = 0`, `PenaltyCoins = 0` sind vorhandene Werte
  ([PlanPositionEntities.cs:45](../../backend/Pugling.Api/Models/PlanPositionEntities.cs#L45),
  [:78](../../backend/Pugling.Api/Models/PlanPositionEntities.cs#L78),
  [:85](../../backend/Pugling.Api/Models/PlanPositionEntities.cs#L85)).
- **Mehrere spielbare Pläne sind erlaubt.** Es gibt **keinen** Server-Guard „ein aktiver Plan je Kind";
  [StudentPlansController.cs:38](../../backend/Pugling.Api/Controllers/Student/StudentPlansController.cs#L38)
  listet alle laufenden, gewarnt wird nur im Vater-Web
  ([VaterPlaene.tsx:91](../../frontend/src/vater/VaterPlaene.tsx#L91)). Ein eigener
  Hausaufgaben-Container kollidiert also nicht mit der Spiel-Engine.
- **Die kindindividuelle Auswahl fehlt beidseitig**: Interessen sind heute supervisor-gepflegt
  ([ChildInterestsController.cs:26](../../backend/Pugling.Api/Controllers/Supervisor/ChildInterestsController.cs#L26)),
  und eine Übung trägt kein Zielgruppen-Merkmal (→ [B-46](B-46-interessenbasierte-uebungen.md)). Die
  Auflösung „Fach/Kapitel + Profil → konkrete Übungen je Kind" baut
  [B-18](B-18-auto-lehrplan-generator.md); Vorfilter am Katalog (Klassenstufe, Schulart, Kategorie)
  existieren bereits (`ExerciseCatalogController.Search`).
- **Nachrecherchiert (Auftrag dieses Durchgangs): B-46 und B-18 sind inzwischen beide `geschaetzt`**
  (nicht mehr `idee`, wie eine ältere Notiz behauptete) — B-46 liefert künftig die
  `Exercise`↔`InterestTag`-Referenz (Größe L, `migration: ja`), B-18 die serverseitige
  Fach/Kapitel/Klassenstufe/Schulart/Quelle-Filterung samt Massen-Übernahme (Größe S, `migration: nein`).
  Beide sind selbst noch **nicht gebaut** (`in-arbeit`/`abgenommen` fehlt), blockieren B-09 als
  eigenständige, unabhängig geschätzte Stories aber nicht mehr strukturell. Ebenso nachrecherchiert: die
  geteilte Übungs-Bibliothek ist bereits weiter, als der alte Plan unterstellte — `Exercise.ExecutePublic`
  plus `PATCH creator/exercises/{id}/sharing` (B-11) sind seit Commit `88ed858` gebaut, nur das
  Fach/Kapitel-Eigentum (B-13) ist noch offen (`geschaetzt`, M, `migration: ja`). Für **diese** Story ist
  das nachrangig, weil der Kern seit der Neu-Rahmung ohnehin nicht mehr die Ownership-Umkehr ist (siehe
  `## Entscheidungen`, Punkt 2).

## Die echte Lücke

Nicht die Rechteumkehr an den bestehenden Ownership-Filtern — die wäre der teuerste und riskanteste Weg
zum Ziel. Es fehlen drei Dinge:

1. Ein **Auftragsobjekt mit Urheber und Frist** (Klasse/Kind + Fach/Kapitel + Auswahlregel), das je Kind
   punktfreie Positionen materialisiert.
2. Eine **auftragsskopierte Erledigungssicht** — der Lehrer sieht diesen Auftrag, nicht das Kind.
3. Die **Individualisierung**, die diese Story sich leiht: ohne B-46 und B-18 bleibt nur „eine konkrete
   Übung zuweisen", also die halbe Idee.

Damit bleibt die bestehende Auth-Wand unverändert stehen und bekommt eine **additive** daneben, statt ein
Loch hineingeschnitten zu bekommen.

## Offene Punkte

Punkte 1–8 aus der ursprünglichen Ausformulierung, plus zwei erst beim Schätzen sichtbar gewordene
(9–10, ohne die die Größe nicht ehrlich zu benennen wäre) — alle zehn sind mit dieser Runde nummerierte
Entscheidungen geworden:

1. ~~Eigenes Auftragsobjekt oder `StudyPlan` mit Urheber-Spalte?~~ → siehe Entscheidung 3.
2. ~~Frist am Auftrag oder neue `GoalCadence.Once` + `DueDate` an der Position?~~ → siehe Entscheidung 4.
3. ~~Was genau sieht der Lehrer?~~ → siehe Entscheidung 5.
4. ~~Wer willigt ein?~~ → siehe Entscheidung 6.
5. ~~Darf der Vater eine Hausaufgabe doch bezahlen?~~ → siehe Entscheidung 7.
6. ~~Braucht es die Klasse, oder reicht eine Verknüpfung Lehrer ⇢ Kind?~~ → siehe Entscheidung 8.
7. ~~Was passiert bei Fristablauf?~~ → siehe Entscheidung 9.
8. ~~Erscheint die Hausaufgabe in der Tagesmission des Sohns?~~ → siehe Entscheidung 10.
9. ~~Wie individualisiert v1, wenn B-46/B-18 noch nicht gebaut sind?~~ → siehe Entscheidung 11.
10. ~~Braucht diese Etappe schon eine Lehrer-/Vater-Oberfläche?~~ → siehe Entscheidung 12.

## Entscheidungen

Entscheidungen 1–2 stammen aus früheren Runden und stehen nur als Spur; 3–12 sind das Ergebnis dieser
Runde (2026-08-04, autonom getroffen, Nutzerauftrag) und lösen die zehn „Offenen Punkte" oben ab:

1. ~~Eigene `Teacher`-Entität samt `Roles.Lehrer` und `tid`-Claim~~ (Entwurf 2026-07-05) — **überholt**:
   gebaut wurde das Creator-only-Konto auf der `Adult`-Zeile.
2. ~~Kern der Story ist die Ownership-Umkehr an `AuthAccess`/`ChildOwnershipFilter`~~ — **ersetzt**
   (Nutzer, 2026-08-01) durch das additive Auftragsobjekt: der Lehrer bekommt nie Zugriff *auf das Kind*,
   nur auf *seinen Auftrag*.
3. **Eigenes Auftragsobjekt statt Urheber-Spalte am `StudyPlan`.** Vier neue Entities nach dem Muster von
   `SupervisorLink`/`Adult` ([AdminEntities.cs](../../backend/Pugling.Api/Models/AdminEntities.cs)):
   `TeacherClass` (Lehrer + Name + `JoinCode`), `ClassMembership` (Klasse ⇢ Kind, der
   Einwilligungs-Datensatz), `Assignment` (Klasse + Fach/Kapitel + Frist) und `AssignmentCompletion`
   (Auftrag ⇢ Kind ⇢ der für dieses Kind materialisierte `StudyPlan` + Erledigt-Zeitpunkt). Begründung:
   Ein `CreatedByAdultId` am `StudyPlan` trüge weder die Klasse noch die Frist noch die Auswahlregel — es
   würde den Container mit lehrerfremden Feldern überladen, die für einen vater-erzeugten Plan sinnlos
   sind. Ein eigenständiges Modell hält die bestehende `StudyPlan`/`PlanPosition`-Tabelle **komplett
   unverändert** (kein Feld, keine Migration daran) und hängt sich nur über `AssignmentCompletion.StudyPlanId`
   ein — additiv im Wortsinn. Kosten: vier neue Tabellen statt einer Spalte, damit eine eigene
   Migrations-Etappe (siehe Schätzung) und vier neue Zeilen in den `SchemaGuardTests`-Listen.
4. **Frist sitzt am `Assignment` (`DueDate`), die Position bleibt `Cadence.None`.** Begründung: Ein neuer
   `GoalCadence.Once`-Wert griffe in die Perioden-Arithmetik ein
   ([PlanPositionEntities.cs:136-141](../../backend/Pugling.Api/Models/PlanPositionEntities.cs#L136-L141)
   dokumentiert genau die Gefahr: die Taktung ist eine Momentaufnahme auf der Log-Zeile, ein dritter Wert
   müsste jede Stelle prüfen, die heute nur `Daily`/`Weekly` unterscheidet) — ein rein additiver Auftrag
   darf diese bestehende, gut verstandene Logik nicht anfassen. Kosten: „überfällig" ist eine Eigenschaft
   des `Assignment`, nicht der `PlanPosition` — die Tagesmission (Entscheidung 10) muss sie separat neben
   der Positions-Fälligkeit holen, statt sie geschenkt aus `PositionPlayService` zu bekommen.
5. **Der Lehrer sieht je Kind ausschließlich erledigt/nicht erledigt + Zeitpunkt, zu genau diesem
   `Assignment`.** Begründung: Jede weitere Auskunft (Lernstand, Wallet, andere Pläne, Geschwister) wäre
   ein stiller Rechte-Überschuss genau der Art, die die Supervisor-Rolle laut Ist-Stand ungeeignet macht
   (`ChildrenController.cs:20` öffnet beim vollen Supervisor-Grant Kind-CRUD **inklusive PIN**). Ein
   eigener, schmaler Endpunkt hält die Positivliste kurz, statt eine Negativliste am großen Supervisor-Grant
   pflegen zu müssen. Kosten: ein eigener Projektions-Typ (`AssignmentCompletionResponse`), keine
   Wiederverwendung der bestehenden `ChildResponse`.
6. **Der Supervisor löst den Beitrittscode für sein Kind ein, nicht das Kind selbst.** Begründung: Der
   PIN-Login des Kindes ist niedrigschwellig gedacht (Leitner-Übung, kein Vertragsabschluss); eine externe
   Bindung an einen fremden Erwachsenen ist eine Entscheidung mit Tragweite, die beim Supervisor liegen
   sollte — symmetrisch dazu, dass laut Akzeptanzkriterium 6 auch nur der Supervisor die Verknüpfung wieder
   lösen kann. Endpunkt folgt dem bestehenden Muster kindbezogener Ressourcen: `POST
   supervisor/children/{childId}/class-memberships` mit `[ServiceFilter(typeof(ChildOwnershipFilter))]`
   (CLAUDE.md „Eigentum"). Kosten: das Kind selbst hat keinen direkten Beitritts-Weg — für den seltenen
   Fall eines Kindes ohne mitwirkenden Supervisor bleibt kein Weg (als Rand-Fall hingenommen, dieselbe
   Grenze gilt heute schon für jedes Kind-Onboarding).
7. **Der Supervisor darf eine materialisierte, punktfreie Position nachträglich bepunkten (Opt-in), der
   Lehrer nie.** Begründung: erhält die bestehende Trennschärfe „der Supervisor behält die Kontrolle über
   Punkte und Malus" wörtlich; die Position ist eine gewöhnliche `PlanPosition` unter einem gewöhnlichen,
   vom Kind bereits gesehenen `StudyPlan` — der bestehende `PlanPositionsController`-Update-Pfad
   (Supervisor, ownership-geprüft über den Plan) deckt das **ohne jede Neuerung** ab, weil `AssignmentCompletion`
   nur verweist, statt eine Parallel-Tabelle für Punkte zu führen. Kosten: keine zusätzlichen — es ist die
   Abwesenheit einer Sonderregel.
8. **Die Klasse ist Pflichtbestandteil, keine 1:1-Verknüpfung Lehrer ⇢ Kind.** Begründung: „jedes Kind
   bekommt sein eigenes Hausaufgaben-Set" ist laut User Story der Kernnutzen — er entsteht erst dadurch,
   dass **ein** Auftrag an eine Gruppe je Mitglied unterschiedlich auflöst; eine 1:1-Verknüpfung wäre nur
   „eine Übung einem Kind zuweisen" in einem neuen Gewand und würde den eigentlichen Anspruch der Story
   unterlaufen. Kosten: zwei zusätzliche Entities (`TeacherClass`, `ClassMembership`) statt einer.
9. **Fristablauf setzt den Status „überfällig", löst aber keinen Malus aus.** Begründung: Ein
   `PenaltyCoins`-Abzug durch einen Auftrag, den ein Fremder ohne Betreuungsauftrag erteilt hat, verletzte
   die im Root-`CLAUDE.md` festgeschriebene Reward-Ökonomie („der Supervisor **erzwingt** Lernerfolg") an
   ihrer empfindlichsten Stelle – ein Dritter griffe in die Familien-Wallet ein. `PenaltyCoins = 0` an der
   materialisierten Position (Entscheidung 4) erzwingt das bereits strukturell, diese Entscheidung
   benennt nur die Konsequenz für den Anzeige-Status. Kosten: keine – Fristablauf ist reine Anzeigelogik
   (`DueDate < heute` am `Assignment`), kein neuer Buchungspfad.
10. **Die Hausaufgabe erscheint in der Tagesmission des Sohns als eigener, sichtbar mit „von der Schule"
    markierter Abschnitt.** Begründung: Ohne Kennzeichnung konkurriert sie unsichtbar mit der Pflicht des
    Supervisors und verwischt gerade die Trennung, die diese Story herstellen soll (kein Malus, keine
    Betreuung); „von der Schule" macht die Herkunft so beiläufig lesbar wie im Vater-Web den Plan-Namen.
    Kosten: eine zusätzliche, additive Abfrage in der Tagesmissions-/Übersichts-Sicht
    (`PlanOverviewController`), die `AssignmentCompletion.StudyPlanId` gegen die aktiven Pläne des Kindes
    spiegelt – kein Eingriff in die bestehende Tages-/Wochen-Rollup-Logik.
11. **v1 individualisiert über einen deterministisch gemischten Teil-Pool je Kind, nicht über echte
    Interessen-Passung.** Begründung: [B-46](B-46-interessenbasierte-uebungen.md) (Interessen-Tag an der
    Übung) und [B-18](B-18-auto-lehrplan-generator.md) (Feinschliff des Auto-Generators) sind beide erst
    `geschaetzt`, nicht gebaut — B-09 auf ihre Fertigstellung zu verketten hieße, eine bereits als „nicht
    mehr blockierend" erkannte Abhängigkeit (siehe Ist-Stand) doch wieder zur harten Blockade zu machen.
    Der bestehende Fach/Kapitel/Klassenstufe/Schulart-Filter (`ExerciseCatalogController.Search`-Muster)
    liefert schon heute einen Übungs-Pool; die Materialisierung zieht daraus je Kind eine mit `ChildId` als
    Seed deterministisch gemischte Teilmenge — zwei Kinder derselben Klasse bekommen so nachweisbar
    **unterschiedliche** Übungen, ohne dass eine Interessen-Tabelle existieren muss. Kosten: Die
    „nach Interessen"-Aussage der User Story ist bis B-46/B-18 gebaut sind nur „pro Kind unterschiedlich",
    nicht „nach Interessen eingekleidet" — Akzeptanzkriterium 3 unten benennt das ausdrücklich als
    bewussten Zwischenstand, keine stille Abschwächung.
12. **Kein Frontend in dieser Story — reine Backend-API, wie B-46/B-18 es vorleben.** Begründung: API-First
    heißt hier wörtlich „erst die API"; ein Lehrer-Web und eine Vater-Web-Ansicht für die Verknüpfungen
    sind eine eigenständige, separat schätzbare Oberflächen-Story (Präzedenz: B-46 ist ebenfalls
    `wo: backend` ohne Frontend, B-18 baut nur die serverseitige Lücke plus einen bereits vorhandenen
    Wizard-Screen). Ohne diesen Schnitt würde die Größe in Richtung XL kippen (vier neue Entities **plus**
    zwei neue React-Screens plus E2E). Kosten: Der Lehrer bedient sich in dieser Etappe nur über
    Swagger/REST-Client — kein bedienbares Produkt-Feature, bis eine Folge-Story die Oberfläche baut.

## Akzeptanzkriterien

1. Ein Lehrer-Konto (`POST creator/teacher-accounts`, bereits vorhanden) legt über `POST
   creator/classes` eine **Klasse** an und erhält einen **Beitrittscode** zurück.
2. Der Supervisor löst den Code für sein Kind ein (`POST supervisor/children/{childId}/class-memberships`,
   Entscheidung 6); der Lehrer erhält dadurch **keinen** `SupervisorLink` — die kindbezogenen
   Supervisor-Endpunkte (`children/{id}`, `.../points`, `.../supervisors`) antworten ihm weiterhin
   `403`/`404`.
3. Der Lehrer erteilt der Klasse einen Auftrag aus **Fach/Kapitel + Frist** (`POST
   creator/classes/{id}/assignments`); für jedes Mitglied entsteht ein eigener, punktfreier
   `StudyPlan` mit `PlanPosition`s aus dem bestehenden Fach/Kapitel/Klassenstufe/Schulart-Filter, deren
   Auswahl sich zwischen zwei Kindern **nachweisbar unterscheidet** (deterministischer Seed = `ChildId`,
   Entscheidung 11). Echte Interessen-Passung ist ein bewusster, in dieser Story **nicht** enthaltener
   Fast-Follow, sobald [B-46](B-46-interessenbasierte-uebungen.md)/[B-18](B-18-auto-lehrplan-generator.md)
   gebaut sind.
4. Die entstandenen Positionen buchen **keine** Punkte und **keinen** Malus (`Cadence.None`,
   `PointsGoalMet = 0`, `PenaltyCoins = 0`).
5. Der Lehrer sieht je Kind erledigt/nicht erledigt samt Zeitpunkt (`GET
   creator/classes/{id}/assignments/{id}/completions`) — und **nichts darüber hinaus** (Wallet, Lernstand,
   fremde Pläne, Geschwister bleiben unerreichbar).
6. Der Supervisor sieht die Klassen-Verknüpfungen seines Kindes (`GET
   supervisor/children/{childId}/class-memberships`) und kann eine Verknüpfung lösen (`DELETE
   .../class-memberships/{id}`).
7. Ein Regressionstest (`TeacherHomeworkTests`) hält fest, dass ein Lehrer-Konto über **keinen** Weg —
   auch nicht über die neuen Klassen-/Auftrags-Endpunkte — an `children/{id}` (PIN!),
   `children/{id}/points` oder `children/{id}/supervisors` kommt.
8. Kein Frontend gehört zu dieser Story (Entscheidung 12); alle Endpunkte sind über Swagger/REST-Client
   bedienbar und in `docs/api-examples/` dokumentiert (`DocsCaptureTests`).

## Schätzung

**Größe: L** — vier neue Entities (`TeacherClass`, `ClassMembership`, `Assignment`,
`AssignmentCompletion`) plus eine Materialisierungs-Service-Logik, die über drei Ebenen greift
(Creator: Klasse/Auftrag anlegen + Erledigungssicht; Supervisor: Beitritt/Lösen; Student: Tagesmissions-
Kennzeichnung „von der Schule") — vergleichbar mit einer DB-Umbau-Etappe wie E6, am oberen Rand von L,
aber durch den Verzicht auf Individualisierungs-Logik (Entscheidung 11) und Frontend (Entscheidung 12)
bewusst unter der XL-Schwelle gehalten. Ohne diese beiden Schnitte wäre die Story XL gewesen (vier neue
Tabellen **plus** die B-46/B-18-Fertigstellung als harte Voraussetzung **plus** zwei neue React-Screens).

- **`wo`:** backend. Eine Lehrer-/Vater-Oberfläche ist eine eigene Folge-Story (Entscheidung 12).
- **`migration`:** ja — vier neue Tabellen, die Migrationskette wird neu gefaltet (`rm -rf
  Data/Migrations` + `migrations add InitialCreate`, CLAUDE.md „EF-Migrationen").
- **`vertragsbruch`:** nein — ausschließlich neue, additive Endpunkte und DTOs in `Pugling.Contracts`
  (`Creator/ClassDtos.cs`, `Creator/AssignmentDtos.cs` o. ä.); kein bestehender Vertrag ändert sich.

**Risiken:**

- **„Erledigt" ist ein neuer Begriff, der noch keinen fertigen Baustein hat.** Ohne Pflicht-Kadenz
  (`Cadence.None`) greift die bestehende „Ziel erreicht"-Logik aus `PositionProgressService` nicht direkt
  — es muss geklärt werden, ob eine punktfreie Position als „erledigt" gilt, sobald ihr `GoalThreshold`
  einmalig erreicht wurde (Analogie zum bestehenden Prozent-Maßstab), oder ob ein neuer, schmalerer Check
  nötig ist. Unklar gelassen, wird aus der Story ein Etikettenschwindel („erledigt" zeigt nichts
  Verlässliches).
- **Neue Eindeutigkeit `TeacherClass.JoinCode`** braucht nach Konvention (`backend/Pugling.Api/CLAUDE.md`
  „Schema & Migrationen") eine Vorprüfung plus einen `ApiErrors`-Code bei Kollision, eine begrenzte
  String-Länge (`…Code` nach dem `…Key`/`…Slug`-Muster, 128) und eine `SchemaGuardTests`-Zeile für die
  neue Eindeutigkeit — sonst wird aus einem doppelt vergebenen Code ein 500 statt eines sprechenden 409.
- **Vier neue Beziehungen** brauchen vier bewusste Zeilen in den `SchemaGuardTests`-Listen (G1–G9,
  CLAUDE.md „Schema-Änderungen laufen gegen gepinnte Listen") — leicht vergessen, weil keine der vier
  Tabellen eine bestehende Tabelle *ändert*, nur neu daneben steht.
- **Die „nach Interessen"-Aussage der User Story ist bis B-46/B-18 gebaut nur teilweise eingelöst**
  (Entscheidung 11) — das muss in jeder Kommunikation nach außen (Doku, Demo) als bewusster
  Zwischenstand benannt werden, sonst wirkt das Ergebnis wie eine gebrochene Zusage.
- **Kein Scheduler für Fristablauf**: „überfällig" ist reine Lesezeit-Logik (`DueDate < heute`), analog
  zum bestehenden Malus-Muster ohne Scheduler (`PositionProgressService.SettleClosedPeriodsAsync` rechnet
  lazy bei Login/Kauf ab) — hier gibt es nicht einmal eine Buchung nachzuholen, nur eine Anzeige.

**Angriffsplan** (Backend zuerst, wie `wo: backend` es ohnehin vorgibt):

1. Neue Entities (`Models/ClassroomEntities.cs`: `TeacherClass`, `ClassMembership`, `Assignment`,
   `AssignmentCompletion`) + Migration (Kette neu falten) + `SchemaGuardTests` um die vier neuen
   Beziehungen und die `JoinCode`-Eindeutigkeit ergänzen.
2. `Pugling.Contracts`: additive Response-/Request-Records für Klasse, Mitgliedschaft, Auftrag,
   Erledigungssicht.
3. Creator-Endpunkte: `ClassesController` (anlegen/lesen/Code), `AssignmentsController` (anlegen → ruft
   die Materialisierung, Erledigungssicht lesen).
4. `AssignmentMaterializationService`: pro Klassenmitglied den bestehenden Fach/Kapitel/Klassenstufe/
   Schulart-Filter (`ExerciseCatalogController`-Muster) ausführen, mit `ChildId` als Seed mischen, einen
   punktfreien `StudyPlan` + `PlanPosition`s anlegen, `AssignmentCompletion` verknüpfen.
5. Supervisor-Endpunkte unter `children/{childId}/class-memberships` (Beitritt per Code, Liste, Lösen) —
   `[ServiceFilter(typeof(ChildOwnershipFilter))]` wie jede andere kindbezogene Ressource.
6. Student-Sicht: `PlanOverviewController` (Tagesmission) additiv um die „von der Schule"-Kennzeichnung
   erweitern (Join gegen `AssignmentCompletion.StudyPlanId`).
7. `Pugling.Client`: je neuem Endpunkt eine Zeile, kein neues HTTP-Plumbing.
8. Regressionstests für die Auth-Wand (Akzeptanzkriterium 7) + Endpunkt-Abdeckung.

**Testweg:** neue Testklasse `TeacherHomeworkTests.cs` (Muster: `TeacherAccountTests.cs`) deckt: Klasse
anlegen, Beitritt per Code (inkl. falscher/fremder Code → `404`/`409`), Auftrag materialisiert je Kind
unterschiedliche Positionen, Positionen tragen `PointsGoalMet = 0`/`PenaltyCoins = 0`, Lehrer-Sicht zeigt
nur erledigt/Zeitpunkt, und die vier gesperrten Routen aus Akzeptanzkriterium 7 bleiben `403`/`404` — auch
über die neuen Klassen-/Auftrags-Routen erreicht. `SchemaGuardTests` um die vier neuen Beziehungen und die
`JoinCode`-Eindeutigkeit ergänzen. Kein E2E nötig (kein Frontend, Entscheidung 12); vor dem Commit ein
gezielter `/smoke-test`-Durchlauf der neuen Endpunkte per REST-Client.

## Verlauf

- **2026-07-30** — als Sammel-Story geerntet; Abweichung Entwurf ↔ gebaute Identität festgehalten.
- **2026-08-01** — auf `ausformuliert` **zurückgestuft** und neu gerahmt (Nutzer-Entscheidung im
  Rollen-Durchgang): Auftragsobjekt statt Ownership-Umkehr, punktfrei zulässig, Individualisierung nach
  Interessen. Die Schätzung (L, `migration: ja`) ist damit **hinfällig** und entfernt — sie maß den alten
  Kern. Neue Abhängigkeit: [B-46](B-46-interessenbasierte-uebungen.md) und
  [B-18](B-18-auto-lehrplan-generator.md) gehen voraus.
- **2026-08-04** — gegrillt: Ist-Stand nachrecherchiert (Registrierungs-Endpunkt `TeacherAccountsController`
  bereits gebaut, B-46/B-18/B-11/B-13 inzwischen `geschaetzt` statt `idee` — der generierte Index war
  veraltet, die Story-Dateien selbst nicht); alle acht ursprünglichen offenen Punkte plus zwei beim
  Schätzen sichtbar gewordene in zwölf nummerierte Entscheidungen überführt (Datenmodell, Frist-Ort,
  Lehrer-Sicht, Einwilligung, Vater-Bepunktung, Klassenpflicht, Fristablauf, Tagesmission,
  Individualisierungs-Schnitt, Frontend-Schnitt); Akzeptanzkriterien final gemacht (autonom getroffen,
  Nutzerauftrag).
- **2026-08-04** — geschätzt: `groesse: L`, `wo: backend`, `migration: ja`, `vertragsbruch: nein`, Risiken,
  Angriffsplan (Backend-Reihenfolge über Entities → Contracts → Creator-Endpunkte →
  Materialisierungs-Service → Supervisor-Endpunkte → Student-Tagesmission → Client → Regressionstests) und
  Testweg (`TeacherHomeworkTests.cs`, `SchemaGuardTests`-Ergänzung, `/smoke-test`) festgelegt; kein
  XL-Split nötig, weil Individualisierung und Frontend bewusst ausgeklammert sind (autonom getroffen,
  Nutzerauftrag).
