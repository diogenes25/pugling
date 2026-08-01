---
tags: [typ/story, status/ausformuliert, bereich/katalog, bereich/auth, bereich/training, rolle/creator, rolle/supervisor, rolle/student]
aliases: [Lehrer-Hausaufgaben, Klassen, Beitrittscode, Zuweiser ohne Betreuungsauftrag]
status: ausformuliert
prio: P3
art: Wunsch
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

- **Die Identität ist gebaut, anders als der alte Entwurf sagte**: Lehrer = Erwachsener ohne
  Betreuungsauftrag (nur Creator-Profil), angelegt über
  [AccountService.cs:34](../../backend/Pugling.Api/Auth/AccountService.cs#L34); das Anlegen ist
  **nicht nachrüstend**, ein bestehendes Konto behält seine Rollen. Eine `Teacher`-Entität gibt es
  bewusst nicht ([lehrer-konto-plan.md](../lehrer-konto-plan.md)).
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
  existieren bereits.

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

1. **Eigenes Auftragsobjekt oder `StudyPlan` mit Urheber-Spalte?** *Empfehlung: eigenes Objekt* (Auftrag)
   plus ein lehrer-erzeugter Plan-Container je Kind als Materialisierung. Ein `CreatedByAdultId` am Plan
   allein trüge die Klasse, die Frist und die Auswahlregel nicht.
2. **Frist am Auftrag oder neue `GoalCadence.Once` + `DueDate` an der Position?** *Empfehlung: Frist am
   Auftrag, Position bleibt `None`* — kein Eingriff in die Perioden-Arithmetik. Kosten: „überfällig" ist
   dann eine Eigenschaft des Auftrags, nicht der Position, und die Tagesmission muss sie separat holen.
3. **Was genau sieht der Lehrer?** *Empfehlung: je Kind nur erledigt ja/nein + Zeitpunkt zu genau diesem
   Auftrag* — kein Lernstand, kein Wallet, keine anderen Pläne, kein Name der Geschwister.
4. **Wer willigt ein?** *Empfehlung: das Einlösen des Beitrittscodes durch Kind/Elternteil ist der
   Einwilligungsakt*; der Supervisor sieht die Verknüpfung und kann sie einseitig lösen.
5. **Darf der Vater eine Hausaufgabe doch bezahlen?** *Empfehlung: ja, als Opt-in* — er darf an der
   materialisierten Position Punkte setzen. Das erhält das bisherige Kriterium „der Supervisor behält die
   Kontrolle über Punkte und Malus".
6. **Braucht es die Klasse, oder reicht eine Verknüpfung Lehrer ⇢ Kind?** *Empfehlung: Klasse* — „jedes
   Kind sein eigenes Set" entsteht gerade daraus, dass **ein** Auftrag an eine Gruppe je Kind anders
   auflöst.
7. **Was passiert bei Fristablauf?** *Empfehlung: Status „überfällig", kein Malus.* Der Stick bleibt beim
   Supervisor, sonst greift ein Fremder in die Familien-Ökonomie ein.
8. **Erscheint die Hausaufgabe in der Tagesmission des Sohns?** *Empfehlung: ja, als eigener Abschnitt und
   sichtbar als „von der Schule"* — sonst konkurriert sie unsichtbar mit der Pflicht des Vaters.

## Entscheidungen

Aus früheren Runden, hier nur als Spur — die Stufe `gegrillt` ist mit der Neu-Rahmung **nicht** mehr
erreicht:

1. ~~Eigene `Teacher`-Entität samt `Roles.Lehrer` und `tid`-Claim~~ (Entwurf 2026-07-05) — **überholt**:
   gebaut wurde das Creator-only-Konto auf der `Adult`-Zeile.
2. ~~Kern der Story ist die Ownership-Umkehr an `AuthAccess`/`ChildOwnershipFilter`~~ — **ersetzt**
   (Nutzer, 2026-08-01) durch das additive Auftragsobjekt: der Lehrer bekommt nie Zugriff *auf das Kind*,
   nur auf *seinen Auftrag*.

## Akzeptanzkriterien (Entwurf)

1. Ein Lehrer-Konto legt eine **Klasse** an und gibt einen **Beitrittscode** aus.
2. Kind/Elternteil tritt per Code bei; der Lehrer erhält dadurch **keinen** `SupervisorLink` — die
   kindbezogenen Supervisor-Endpunkte antworten ihm weiterhin `403`.
3. Der Lehrer erteilt der Klasse einen Auftrag aus **Fach/Kapitel + Frist**; für jedes Mitglied entstehen
   Positionen, deren Übungsauswahl sich zwischen zwei Kindern mit verschiedenen Interessen **unterscheidet**.
4. Die entstandenen Positionen buchen **keine** Punkte und **keinen** Malus.
5. Der Lehrer sieht je Kind erledigt/nicht erledigt samt Zeitpunkt — und **nichts darüber hinaus**
   (Wallet, Lernstand, fremde Pläne bleiben `403`).
6. Der Supervisor sieht, was von außen zugewiesen wurde, und kann die Verknüpfung lösen.
7. Ein Regressionstest hält fest, dass ein Lehrer-Konto keinen Weg zu `children/{id}` (PIN!),
   `children/{id}/points` und `children/{id}/supervisors` hat.

## Verlauf

- **2026-07-30** — als Sammel-Story geerntet; Abweichung Entwurf ↔ gebaute Identität festgehalten.
- **2026-08-01** — auf `ausformuliert` **zurückgestuft** und neu gerahmt (Nutzer-Entscheidung im
  Rollen-Durchgang): Auftragsobjekt statt Ownership-Umkehr, punktfrei zulässig, Individualisierung nach
  Interessen. Die Schätzung (L, `migration: ja`) ist damit **hinfällig** und entfernt — sie maß den alten
  Kern. Neue Abhängigkeit: [B-46](B-46-interessenbasierte-uebungen.md) und
  [B-18](B-18-auto-lehrplan-generator.md) gehen voraus.
