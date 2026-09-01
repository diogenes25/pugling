---
tags: [typ/story, status/geschaetzt, bereich/frontend, bereich/backend, rolle/supervisor]
aliases: [pointsGoalMet ohne Obergrenze im UI, Vererbungszweig wird nie genommen]
status: geschaetzt
prio: P3
art: Defekt
groesse: M
wo: beides
migration: nein
vertragsbruch: ja
quelle: Nachschau 2026-09-01 zu B-45
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-45]
wartet_auf: ""
---

# B-192 · Die Obergrenze der Punkte-Empfehlung schützt den Weg nicht, den der Vater geht

## User Story

Als **Vater** möchte ich, dass eine unsinnig hohe Empfehlung aus einer fremden Katalog-Übung auch dann
begrenzt wird, wenn ich den Lehrplan über die Oberfläche baue — nicht nur über die API.

## Ist-Stand am Code

B-45 begrenzt die geerbte Empfehlung serverseitig auf `is > 0 and <= 200`
(`PlanPositionsController.cs:205`). Der Zweig greift aber **nur, wenn `pointsGoalMet` `null` ist**.

- `PlanPositions.tsx:185` (`pointsFrom`) hat **keine Obergrenze**.
- `PlanPositions.tsx:248` (`settingsToDto`) schickt `pointsGoalMet` **immer als Zahl**, nie `null`.

**Folge:** Das Vater-Web nimmt den Vererbungszweig des Servers nie. Eine fremde Übung mit
`rewardPoints = 100000` belegt das Feld mit 100000 vor, das wird ausdrücklich gepostet, und der Server
nimmt es unbegrenzt an — so ausdrücklich zugesichert in `PlanPositionCrudTests.cs:335-340`. Begrenzt ist
allein der API-First-Pfad.

**Mildernd, und darum P3:** Die Zahl steht sichtbar im Feld; der Vater kann sie sehen und ändern. Der
Kommentar `PlanPositions.tsx:182` nennt die Asymmetrie sogar („dort zusätzlich") — die Story B-45 führt
sie aber nirgends als offenen Punkt, und ihr Abnahme-Commit beschreibt das Loch als geschlossen.

## Die echte Lücke

Eine Regel, die an einem Zustand hängt (`null`), den der einzige reale Aufrufer nie erzeugt. Der Test
belegt die Regel, nicht ihre Wirksamkeit.

## Was das Nachlesen ergänzt hat (2026-09-01, zweiter Nachtlauf)

**Offener Punkt 2 ist nicht offen — der Code hat ihn längst entschieden.** `PlanPositionsController.cs:85-88`
sagt ausdrücklich: *„The bound applies **only** to inheritance, never to a value the supervisor states
explicitly — that is their own decision and stays unbounded, as it was. That asymmetry is what makes a
tight bound safe."* Es gibt hier also nichts beim Menschen abzuholen.

**Das verschiebt die Diagnose, und zwar zum Besseren.** Der Defekt ist nicht „die Grenze fehlt im
Frontend". Er ist: **die Oberfläche verwandelt das Schweigen des Vaters in eine Aussage.** Weil
`PositionSettings.pointsGoalMet` ein `number` ist (`PlanPositions.tsx:84`), kann das Formular „habe ich
nicht gesagt" gar nicht ausdrücken — jeder Absendevorgang ist eine ausdrückliche Aussage, und für die gilt
die Grenze zu Recht nicht.

**Drei Zeilen tiefer steht das richtige Muster schon.** `PositionSettings` führt die Bonus-Werte als
Strings, mit dieser Begründung im Kommentar (`:87-92`): der leere String heißt dort **erben**, und eine
Zahl zu senden ist eine **Aussage**, die einen bewusst getunten Übungs-Vorschlag überschreiben würde.
Genau dieser Satz gilt für die Zielpunkte — nur wurde er dort nicht angewandt.

## Entscheidungen

1. **Der Server veröffentlicht den *wirksamen* Vorschlag, statt dass ihn das Frontend nachrechnet.**
   `ExerciseSummary` bekommt neben dem rohen `RewardPoints` ein `SuggestedGoalPoints` — das, was die
   Vererbung tatsächlich setzen würde (also `RewardPoints`, falls in (0, 200], sonst 20).
   *Begründung:* Das ist die einzige Fassung, die B-45s AK 4 („das Feld ist mit der Empfehlung vorbelegt")
   und diese Story („was dasteht, gilt auch") gleichzeitig erfüllt. Die naheliegenden Alternativen tun das
   nicht: Die Grenze im Frontend zu spiegeln kopiert eine Geschäftsregel samt ihrer Zahl 200 an eine
   zweite Stelle — genau die Art Dublette, an der dieses Repo wiederholt bezahlt hat. Und das Feld leer zu
   lassen (der Vorschlag dieser Story: `null` = erben) verletzt AK 4, denn der Vater sähe die Empfehlung
   dann nicht mehr — und das war der ganze Zweck von B-45.
   *Kosten:* **`vertragsbruch: ja`** — ein positioneller Record wächst, `Pugling.Client` und das Frontend
   ziehen nach, `docs/openapi/v1.json` und `src/lib/contract.ts` werden neu erzeugt. Genau derselbe
   Schnitt, den B-45 für `RewardPoints` schon einmal gemacht und dokumentiert hat.
2. **Die Regel bekommt einen Ort.** `MaxInheritedGoalPoints`/`InheritedGoalPoints` sind heute `private`
   im `PlanPositionsController` und von der Katalog-Projektion aus nicht erreichbar.
   *Begründung:* Zwei Aufrufer, eine Regel — ohne gemeinsamen Ort entstünde beim Verschieben der Grenze
   genau der stille Drift, gegen den Entscheidung 1 antritt. *Kosten:* eine Datei mehr unter
   `Services/Shared/`; die ausführliche Begründung der Zahl 200 wandert mit und bleibt lesbar.
3. **Die Projektion rechnet inline, nicht per Methodenaufruf.** Der Bauplatz ist ein
   EF-`.Select(...)`-Ausdruck (`ExerciseCatalogController.cs:103-118`); ein C#-Methodenaufruf wäre dort
   nicht übersetzbar. *Begründung:* Ein Ternär mit Konstantenbezug übersetzt und hält trotzdem **eine**
   Quelle für die Zahlen. *Kosten:* Die *Form* der Regel steht zweimal da, ihre *Zahlen* nur einmal — das
   ist die Grenze dessen, was EF hergibt, und sie gehört an die Stelle geschrieben.
4. **Ein ausdrücklich getippter Wert bleibt unbegrenzt.** *Begründung:* keine eigene — sie steht schon in
   `PlanPositionsController.cs:85-88` und ist eine Produktentscheidung, die dieser Lauf nicht antastet.
   *Kosten:* keine.

## Akzeptanzkriterien

1. Eine Übung mit `RewardPoints = 100000` belegt das Feld mit **20** vor, nicht mit 100000.
2. Eine Übung mit `RewardPoints = 30` belegt das Feld mit **30** vor — die Vererbung bleibt sichtbar.
3. Was im Feld steht, ist auch das, was nach dem Anlegen an der Position steht — in beiden Fällen.
4. Ein ausdrücklich getippter Wert über 200 wird weiterhin unverändert übernommen.
5. Backend-Integrationstest für den neuen Feldwert, Frontend-Test für die Vorbelegung, ein E2E für 1+3.
   Die rote Probe nennt erwartete und gemessene Zahl.

## Schätzung

- **Größe: M.** Anker: ein Contracts-Feld, ein neuer gemeinsamer Ort für die Regel, eine EF-Projektion,
  zwei erzeugte Artefakte (OpenAPI, `contract.ts`), die Frontend-Vorbelegung, dazu drei Testebenen.
- **Migration: nein** (kein Schema), **Vertragsbruch: ja** (siehe Entscheidung 1).
- **Testweg:** `PlanPositionCrudTests.cs` (der Fall mit 100000 steht dort schon, `:330`) ·
  `frontend/src/vater/PlanPositions.test.ts` · ein E2E-Fall in `frontend/e2e/punkte-empfehlung.spec.ts`
  (die Datei stammt aus B-45 und ist genau diese Fläche).

## Offene Punkte

1. ~~Grenze im Frontend spiegeln, oder das Feld `null` lassen?~~ → **Entscheidung 1 — und sie verwirft
   beide Vorschläge dieser Story.** Die eigene Empfehlung (`null` lassen) verletzt B-45s AK 4; das war
   beim Ausformulieren nicht mitgeprüft.
2. ~~Soll der Server auch einen ausdrücklich geschickten Wert begrenzen?~~ → **Nicht offen.** Der Code
   entscheidet es bereits ausdrücklich (`PlanPositionsController.cs:85-88`): nein, mit Begründung. Beim
   Ausformulieren übersehen — kein Fall für den Menschen.

## Verlauf

- 2026-09-01 · Aufgenommen aus der **Nachschau** zu B-45 (Nachtlauf). `entgangen_bei: [B-45]`.
  Offener Punkt 2 ist ausdrücklich eine Frage an den Menschen und wird nicht autonom entschieden.
- 2026-09-01 · **`ausformuliert` → `gegrillt` → `geschaetzt`** (zweiter Nachtlauf, Freigabe 1). Beide
  offenen Punkte **beim Nachlesen aufgelöst statt beim Menschen abgeholt**: einer war im Code längst
  entschieden, der andere kollidierte mit einem Akzeptanzkriterium von B-45. M, `vertragsbruch: ja`.
