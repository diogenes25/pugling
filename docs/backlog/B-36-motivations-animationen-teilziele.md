---
tags: [typ/story, status/geschaetzt, bereich/frontend, rolle/student]
aliases: [Motivations-Animationen, Serien-Animation, Streak-Feier]
status: geschaetzt
prio: P3
art: Wunsch
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nutzer, Sitzung 2026-07-31
grund: ""
ersetzt_durch: []
---

# B-36 · Motivations-Animationen bei erreichten Teilzielen

Erreicht der Sohn ein Teilziel, soll die Oberfläche das mit einer Animation feiern statt nur mit einer
stillen Zahl. Die Recherche beim Ausformulieren hat die Story umsortiert: Das Beispiel aus der Idee (Serie
richtiger Antworten *innerhalb* einer laufenden Übung) ist bereits vollständig gebaut und gefeiert – die
echte Lücke liegt bei den **OKR-Teilzielen** (`KeyResult`/`Objective`), die heute nur ein Emoji im Fließtext
bekommen, obwohl genau die Feier-Infrastruktur dafür schon zweimal woanders im Code steht.

## User Story

Als Sohn möchte ich sehen und spüren (Konfetti, Ton, Haptik), wenn ich ein Teilziel meines großen Ziels
erreicht habe, damit der Fortschritt sich wie ein Erfolg anfühlt und nicht wie eine stille Zeile in einer
Liste.

## Ist-Stand am Code

**Das Beispiel der Idee – Serie richtiger Antworten in einer laufenden Übung – ist bereits vollständig
gebaut, unter dem Namen „Combo":**

- Server zählt die Serie live aus den bereits gespeicherten `ReviewEvent`s der laufenden
  `PracticeSession` (kein persistiertes Zählfeld nötig):
  `backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:339-344,389`.
- Bonus-Schwelle ist Position-Konfiguration mit Default „5 in Folge" –
  `backend/Pugling.Api/Models/PlanPositionEntities.cs:89,91` (`ComboThreshold = 5`, `ComboBonusPoints = 5`).
  Auslöse-Logik: `backend/Pugling.Api/Services/Shared/ScoringService.cs:92-98`
  (`ComboBonus`: jede `ComboThreshold`-te Antwort in Folge, eskalierend bei Vielfachen).
- Contract trägt Serie und Bonus bereits: `ReviewOutcome(bool WasCorrect, …, int Combo, int ComboBonus, …)`,
  `backend/Pugling.Contracts/Student/PracticeDtos.cs:75-76`.
- Frontend feiert das schon: `frontend/src/sohn/SohnPractice.tsx:99-106` ruft bei `comboBonus > 0` ein
  `celebrate("medium"|"big", …, "COMBO ×N", …)`, sonst eine kleine Pro-Treffer-Reaktion
  (`celebrate("small", …)`); HUD-Anzeige der laufenden Serie: `SohnPractice.tsx:193`.
- Die Klausur (`frontend/src/sohn/SohnTest.tsx`) feiert bewusst **nicht** pro Antwort (der Modus verrät
  Richtigkeit erst am Ende, `answerAndAdvance` bleibt stumm), sondern erst beim Abschluss:
  `SohnTest.tsx:41-49` (`celebrate(res.passed ? "big" : "small", …)`). Das entspricht bereits genau der
  Sorge aus der Idee, die Klausur dürfe keine Zwischen-Richtigkeit verraten.

**Die Feier-Infrastruktur selbst ist generisch und zentral:**

- `frontend/src/components/Celebration.tsx`: `useCelebration()`/`celebrate(tier, emoji, title?, sub?)`,
  drei Stufen (`small`/`medium`/`big`), Konfetti + Ton + Haptik
  (`frontend/src/lib/feedback.ts:84-110`), **ein** Overlay gleichzeitig (Zeile 19, State wird beim nächsten
  `celebrate(...)` überschrieben).
- `prefersReducedMotion()` (`frontend/src/lib/ui.ts:22-25`) wird zentral respektiert:
  `Celebration.tsx:46` (kein Konfetti-Aufbau) und `feedback.ts:104` (keine Vibration) – **jeder** Aufrufer
  von `celebrate(...)` erbt das automatisch, ohne selbst etwas zu prüfen.

**Zwei weitere Belohnungsflächen nutzen dieselbe Infrastruktur bereits, inklusive „nur einmal feiern"-Muster:**

- `frontend/src/sohn/GamificationPanels.tsx:13-38` – ein generisches „gesehene IDs in `localStorage`"-Muster
  (`isFirstLoad`/`readSeen`/`writeSeen`), das eine neu erfüllte Mission (Zeile 49-60,
  `celebrate("medium", "🎯", …)`) bzw. ein neu erreichtes Abzeichen (Zeile 101-112,
  `celebrate("big", …, "ABZEICHEN!", …)`) genau einmal über Reloads hinweg feiert. Beim allerersten Laden
  (kein `localStorage`-Key) wird still geseedet, damit Alt-Erfolge nicht nachgefeiert werden.

**Die eine Fläche, die dieses Muster NICHT hat: `frontend/src/sohn/MyObjectives.tsx`**

- Zeigt Objectives (das „große Ziel") mit ihren `KeyResult`s (den eigentlichen **Teilzielen** im
  OKR-Sinn, siehe `docs/lernziele-objectives-plan.md`) rein lesend.
- Bei einem erreichten Objective steht nur ein Emoji im Fließtext: `MyObjectives.tsx:44`
  (`o.status === "achieved" ? " · geschafft! 🎉" : …`) – kein `celebrate(...)`-Aufruf, kein Konfetti, kein
  Ton, keine Haptik. Ein einzelnes erreichtes `KeyResult` (Etappe) wird gar nicht mehr angezeigt (Zeile 49:
  `o.keyResults.filter((k) => k.status !== "achieved")`) und damit auch nicht gefeiert.
- Server liefert Ids und Status für beide Ebenen bereits fertig ausgewertet: `ObjectiveResponse`
  (`o.id`, `o.status`, `o.rewardOnComplete`, `o.kind`) und `KeyResultResponse` (`k.id`, `k.status`,
  `k.title`) über `backend/Pugling.Api/Services/Supervisor/ObjectiveService.cs:31-47`
  (`MapKr`/`MapObjective`), ausgewertet in
  `backend/Pugling.Api/Services/Shared/ObjectiveEvaluationService.cs:38,108-119`. Für die client-seitige
  „neu erreicht"-Erkennung reicht das bereits aus – kein neues Server- oder Vertragsfeld nötig.
- Nur **aktive** Objectives kommen überhaupt an (`MyObjectivesController.cs:36`,
  `.Where(o => o.Active)`) – Abschalten ist eine Vater-Entscheidung, kein automatischer Effekt des
  Erreichens. Ein gerade erreichtes Objective bleibt also sichtbar (und damit feierbar), bis der Vater es
  bewusst deaktiviert.
- `ObjectiveService.ListAsync` (`ObjectiveService.cs:100-117`) sortiert **nicht** nach Status/Fälligkeit –
  die Reihenfolge ist die der Auswertung (im Wesentlichen Anlage-Reihenfolge). Der
  Endpunkt-Kommentar in `MyObjectivesController.cs:24` nennt „open/overdue first", das trifft im Code aber
  nicht zu (Doku-Ungenauigkeit, nicht Gegenstand dieser Story). Bei mehr als fünf aktiven Objectives
  (`take: 5` in `MyObjectives.tsx:16`) könnte ein gerade erreichtes Objective aus der ersten Seite
  herausfallen, bevor es gefeiert wurde – siehe Risiken.

## Die echte Lücke

Nicht die Serie beim Üben (die ist längst gebaut) und nicht die Klausur (die feiert schon richtig,
nämlich gar nicht pro Antwort) – sondern **`MyObjectives.tsx` besitzt keine Feier**, obwohl das exakt
gleiche Baumuster („gesehene IDs in `localStorage`" + `celebrate(...)`) zweimal nebenan im selben Ordner
für Missionen und Auszeichnungen bereits steht. Es fehlt also keine neue Fähigkeit, sondern eine dritte
Anwendung eines bestehenden, bewährten Musters.

## Offene Punkte

- ~~Welche Teilziele es geben soll (Serie, Halbzeit, fehlerfreie Runde, …)~~ → siehe Entscheidung 1: die
  Serie ist bereits gebaut und gefeiert (Combo); Ziel dieser Story sind KeyResult-/Objective-Meilensteine.
- ~~Wer die Serie zählt (Server oder Client)~~ → siehe Entscheidung 3: kein Zählen nötig, der Server
  liefert den Status bei jedem Abruf fertig ausgewertet, der Client diff't nur gegen den zuletzt
  gesehenen Stand (wie bei Missionen/Auszeichnungen).
- ~~Wo die Animation läuft, insbesondere die Klausur-Frage~~ → siehe Ist-Stand: die Klausur feiert bereits
  korrekt nur am Ende, nicht pro Antwort; diese Story betrifft eine andere Fläche (`MyObjectives.tsx` auf
  der Sohn-Startseite), nicht den laufenden Übungs-/Klausur-Zyklus.
- ~~`prefersReducedMotion`~~ → siehe Entscheidung 4: zentral in `Celebration.tsx` bereits erledigt, kein
  neuer Code nötig.
- ~~Verhältnis zu den bestehenden Belohnungen (Dopplung?)~~ → siehe Entscheidung 5: keine Dopplung, weil
  Objectives/KeyResults heute die einzige Belohnungsfläche ohne jede Feier sind.
- Granularität: feiert jedes einzelne KeyResult, nur das ganze Objective, oder beides gestuft? → siehe
  Entscheidung 2.
- Reihenfolge bei gleichzeitigem Erreichen (letztes KeyResult schließt zugleich das Objective ab) → siehe
  Entscheidung 6.

## Entscheidungen

1. **Scope-Korrektur: Ziel dieser Story sind KeyResult-/Objective-Meilensteine, nicht die Serie im
   Übungs-Zyklus.** Begründung: Die Serie („5 richtig hintereinander") ist unter dem Namen Combo bereits
   vollständig serverseitig gezählt, im Contract exponiert und im Frontend gefeiert (siehe Ist-Stand) –
   hier weiterzubauen würde eine bestehende Fähigkeit duplizieren. Die einzige Belohnungsfläche ohne jede
   Feier ist `MyObjectives.tsx`. Kosten: Die ursprüngliche Beispiel-Idee (Serie als Teilziel) wird nicht
   weiterverfolgt; falls ausdrücklich zusätzlich gewünscht (z. B. eine Serie ganz ohne Punktebonus, wenn
   `ComboThreshold`/`ComboBonusPoints` auf 0 stehen), wäre das eine eigene, neue Story.
2. **Granularität: zweistufig – jedes neu erreichte `KeyResult` feiert `medium` (🎯), das vollständige
   Erreichen des `Objective` (alle KeyResults `achieved`) feiert zusätzlich `big` (🏆) mit der
   Belohnungsvorschau (`rewardOnComplete` + 🪙/💎 je nach `kind`).** Begründung: `KeyResult` ist im
   OKR-Fachmodell das Teilziel, `Objective` das große Ziel – zwei Stufen spiegeln das und passen zur
   bereits etablierten `medium`/`big`-Konvention (Missionen = `medium`, Auszeichnungen = `big`). Kosten:
   zwei Trigger-Pfade statt einem im selben Effekt, etwas mehr Testfläche (siehe Akzeptanzkriterien 1-2).
3. **Erkennungsmuster: dasselbe „gesehene IDs in `localStorage`"-Muster wie `MissionsPanel`/
   `BadgesGallery`** (`GamificationPanels.tsx:13-38`), zwei neue Keys
   `pugling.seenKeyResults.<childId>` und `pugling.seenObjectives.<childId>`. Begründung: bereits zweimal
   im Code bewährt, robust über Reloads, still geseedet beim allerersten Laden. Kosten: zwei zusätzliche
   `localStorage`-Keys pro Kind (vernachlässigbar); kein Server- oder Vertragsänderung nötig, weil Ids und
   Status schon geliefert werden.
4. **`prefersReducedMotion`: keine neue Arbeit.** Wird zentral in `Celebration.tsx:46`/`feedback.ts:104`
   bereits respektiert; jeder `celebrate(...)`-Aufruf erbt das automatisch. Begründung/Kosten: reine
   Wiederverwendung, keine.
5. **Kein Dopplungs-Risiko.** Objectives/KeyResults zeigen heute außer dem stillen Text
   (`MyObjectives.tsx:44`) nichts – anders als Combo/Missionen/Auszeichnungen, die schon gefeiert werden.
   Begründung/Kosten: keine, es gibt nichts zu entdoppeln.
6. **Reihenfolge bei gleichzeitigem Erreichen: erst das KeyResult-`celebrate`, danach – falls zutreffend
   – das Objective-`celebrate` im selben Effekt-Durchlauf auslösen.** Begründung: `useCelebration` hält
   bewusst nur **ein** Overlay gleichzeitig und überschreibt den State bei jedem `celebrate(...)`-Aufruf
   (`Celebration.tsx:19,26-33`) – bei synchron aufeinanderfolgenden Aufrufen im selben Effekt gewinnt
   automatisch der letzte, also die größere (Objective-)Feier, ohne eigene Koordinationslogik. Kosten:
   keine zusätzliche Arbeit, aber die Aufrufreihenfolge im Code muss stimmen – ein Test deckt das ab
   (Akzeptanzkriterium 2).

## Akzeptanzkriterien

1. Erreicht ein sichtbares `KeyResult` seit dem letzten Abruf neu den Status `achieved`, feiert die
   Oberfläche einmalig `celebrate("medium", "🎯", <Titel/Scope des KeyResult>)`.
2. Wird dabei zugleich das zugehörige `Objective` vollständig erreicht (`status === "achieved"`), feiert
   die Oberfläche zusätzlich `celebrate("big", "🏆", "ZIEL GESCHAFFT!", <rewardOnComplete + 🪙/💎>)` – und
   genau diese größere Feier bleibt sichtbar (siehe Entscheidung 6).
3. Der allererste Aufruf von `MyObjectives` bei einem Kind (kein `localStorage`-Eintrag vorhanden) feiert
   nichts nach, sondern seedet nur den gesehenen Stand – wie bei Missionen/Auszeichnungen.
4. Ein bereits gesehener, weiterhin `achieved` bleibender Stand (Reload ohne neuen Fortschritt) feiert
   nicht erneut.
5. Die Serie/Combo im laufenden Übungs- und Klausur-Zyklus (`SohnPractice.tsx`, `SohnTest.tsx`,
   `ScoringService.cs`) bleibt unverändert – kein Code dort wird angefasst.
6. `prefersReducedMotion` wird respektiert, weil die neue Feier ausschließlich über das bestehende
   `celebrate(...)` läuft (kein neuer, paralleler Animationspfad).

## Schätzung

**Größe: S** – ein neuer `useEffect` mit dem bereits zweimal im selben Ordner vorhandenen
„gesehene-IDs"-Muster, angewendet auf eine dritte, bereits bestehende Komponente. Kein Server-, Vertrags-
oder Migrations-Anteil (`wo: frontend`, `migration: nein`, `vertragsbruch: nein`) – vergleichbar mit
B-54 (Schreib-Primitive in einer bestehenden Sohn-Komponente nachziehen).

**Risiken:**

- **Seitengröße von `myObjectives({ take: 5 })`:** Bei mehr als fünf aktiven Objectives ist die
  Sortierung nicht nach Status (siehe Ist-Stand, `ObjectiveService.ListAsync` sortiert nicht wie der
  Endpunkt-Kommentar behauptet) – ein gerade erreichtes Objective könnte theoretisch außerhalb der ersten
  Seite liegen und ungefeiert bleiben. Akzeptierter Edge-Case (bestehendes Pagination-Verhalten, nicht neu
  eingeführt durch diese Story); kein Blocker für S.
- **`localStorage` fehlt** (Privatmodus): geerbtes, bereits dokumentiertes Verhalten
  (`GamificationPanels.tsx:35-37`) – dann bleibt es beim stillen Seed, keine Feier, aber auch kein Fehler.

**Angriffsplan:**

1. `useEffect` in `MyObjectives.tsx` ergänzen, exakt nach dem Muster von `MissionsPanel`/`BadgesGallery`
   (zwei `localStorage`-Keys, KeyResult zuerst, Objective danach im selben Durchlauf).
2. `useSohn()` in `MyObjectives.tsx` importieren, um `childId` und `celebrate` zu beziehen (bisher nicht
   importiert).
3. Komponententest ergänzen (siehe Testweg).

**Testweg:** Neuer Vitest/RTL-Komponententest für `MyObjectives.tsx` (Vorbild: `TestResult.test.tsx`, dort
als reine Props-Komponente geprüft; hier zusätzlich `vi.mock("./SohnApp")` für `useSohn`), der die vier
Akzeptanzkriterien 1-4 gegen einen gemockten `api.myObjectives(...)`-Rückgabewert über zwei Render-Zyklen
prüft (erster Aufruf seedet, zweiter Aufruf mit einem neu erreichten KeyResult/Objective feiert). Kein
neuer E2E nötig – `SohnPractice`/`SohnTest`/Combo bleiben unverändert (Akzeptanzkriterium 5), und es gibt
aktuell noch keinen E2E-Pfad, der Objectives auf der Sohn-Seite überhaupt durchspielt (out of scope, kein
Rückschritt).

## Verlauf

- **2026-07-31** — vom Nutzer direkt aufgenommen (ungeprüft, keine Recherche — das ist der nächste Schritt).
- **2026-08-03** — ausformuliert: gegen den Code recherchiert. Die Serie ("Combo") ist bereits vollständig
  gebaut und gefeiert (`ScoringService.ComboBonus`, `SohnPractice.tsx`); Missionen/Auszeichnungen ebenso
  (`GamificationPanels.tsx`). Die einzige unbediente Fläche ist `MyObjectives.tsx` (KeyResult/Objective).
  Fünf der sechs ursprünglichen offenen Punkte damit beantwortet, zwei neue (Granularität, Reihenfolge)
  ergeben sich erst aus dem engeren Zuschnitt.
- **2026-08-03** — gegrillt: autonom getroffen, Nutzerauftrag 2026-08-04. Sechs Entscheidungen nummeriert
  (Scope-Korrektur, Granularität, Erkennungsmuster, `prefersReducedMotion`, Dopplungs-Check, Reihenfolge
  bei gleichzeitigem Erreichen), alle mit Begründung und Kosten; Akzeptanzkriterien final.
- **2026-08-03** — geschätzt: autonom getroffen, Nutzerauftrag 2026-08-04. Größe S, `wo: frontend`,
  `migration: nein`, `vertragsbruch: nein`, Angriffsplan und Testweg (neuer RTL-Komponententest,
  `TestResult.test.tsx` als Vorbild) festgelegt.
