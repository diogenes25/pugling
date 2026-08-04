---
tags: [typ/story, status/geschaetzt, bereich/backend, bereich/frontend, rolle/student]
aliases: [Sohn-Arcade Reste B-37, Doppelte Übungssitzung, Nochmal versuchen ohne Versuch]
status: geschaetzt
prio: P3
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/backlog/B-37-uebung-abbruch-unvollendet.md
grund: ""
ersetzt_durch: []
---

# B-62 · Drei Reste aus dem B-37-Review (Sohn-Arcade)

Der `frontend-reviewer`-Lauf zu [B-37](B-37-uebung-abbruch-unvollendet.md) hat neben den behobenen
Befunden drei Stellen gefunden, die **nicht** von B-37 stammen oder deren Zuschnitt gesprengt hätten. Sie
liegen alle in der Sohn-Arcade.

## User Story

> Als **Sohn** möchte ich, dass die Arcade keine doppelten Übungssitzungen anlegt und mir keinen Knopf
> zeigt, der nur in eine Fehlermeldung führt, und als **Vater** möchte ich, dass der Ausstieg aus einer
> laufenden Runde im echten Browser geprüft ist – nicht nur behauptet.

## Ist-Stand am Code

### 1. `SohnPractice.tsx` startet die Sitzung ohne Ref-Gate

Der Start-Effekt hat nur ein `alive`-Flag ([SohnPractice.tsx:48-65](../../frontend/src/sohn/SohnPractice.tsx)):
`alive` verhindert nur das Setzen von State nach einem Unmount, nicht den bereits abgeschickten
`api.startSession`-POST. `SohnTest.tsx` hat für denselben Fall längst die `startedFor`-Ref
([SohnTest.tsx:72-81](../../frontend/src/sohn/SohnTest.tsx)) – genau das Muster aus der Memory-Notiz
„Effekt-Doppellauf: POST braucht Ref-Gate". Unter StrictMode oder einem Remount legt `SohnPractice`
also zwei `PracticeSession`-Zeilen an; die zweite bleibt bei Cursor 0 offen liegen.

**Umfang geprüft** (Auftrag aus der Idee-Fassung): `useEffect` kommt in `frontend/src/sohn/` außerdem in
`SohnTest.tsx`, `SohnShop.tsx`, `SohnApp.tsx`, `SohnHome.tsx`, `SohnSkins.tsx` und `GamificationPanels.tsx`
vor. Alle dort verbliebenen Effekte lesen nur (`useAsync`/`load()`) oder pflegen `localStorage`
(`MissionsPanel`/`BadgesGallery`, [GamificationPanels.tsx:49-60,101-…](../../frontend/src/sohn/GamificationPanels.tsx));
keiner setzt beim Mounten selbst einen POST ab. `SohnPractice.tsx` ist die **einzige** verbliebene Stelle.

Harmlos für die Pflicht (die Erledigt-Regel fragt `Cursor`/`ActiveSeconds`, nicht die Zahl der Sitzungen),
aber seit B-37 ist die Sitzung die Recheneinheit, und eine Karteileiche bei Cursor 0 ist von einem echten
Fortsetzungspunkt nicht unterscheidbar.

### 2. „Nochmal versuchen" bleibt nach dem letzten Versuch stehen

`TestResult` ([SohnTest.tsx:218-288](../../frontend/src/sohn/SohnTest.tsx)) zeigt den Knopf bei jedem
nicht bestandenen Versuch (Zeile 283: `{!result.passed && <button … onClick={onRetry}>Nochmal
versuchen</button>}`) – unabhängig davon, ob der Tagesdeckel schon erreicht ist. Ein Klick danach läuft in
`ApiErrors.TestAttemptsExhausted` ([PositionTestsController.cs:162-164](../../backend/Pugling.Api/Controllers/Student/PositionTestsController.cs))
und landet dank des Review-Nachtrags wenigstens als deutscher Satz in der Fehlerbox
(`GERMAN_PROBLEM_TEXT.test_attempts_exhausted`, [api.ts:86](../../frontend/src/lib/api.ts)).

Der Deckel selbst ist eine reine Server-Konstante (`MaxAttemptsPerDay = 2`,
[PositionTestsController.cs:52](../../backend/Pugling.Api/Controllers/Student/PositionTestsController.cs)),
gezählt wird der **Start**, nicht die Abgabe (Zeilen 159-164). `SubmitResponse`
([TestDtos.cs:76-78](../../backend/Pugling.Contracts/Student/TestDtos.cs)) trägt heute **kein** Feld, aus
dem der Ergebnisbildschirm ablesen könnte, ob noch ein Versuch übrig ist – der richtige Weg (den Knopf gar
nicht erst anzubieten) braucht darum eine Vertragserweiterung.

### 3. Zwei Lücken im E2E-Durchstich

`full-flow.spec.ts:79-80` prüft nur die Sichtbarkeit von „Runde beenden" (`toBeVisible`), klickt ihn nie –
ein Klick würde den restlichen Durchstich abschneiden. Der Weg Knopf → Navigation → Heartbeat-Cleanup →
`endSession` ([SohnPractice.tsx:73-86](../../frontend/src/sohn/SohnPractice.tsx)) ist damit nur durch
Integrationstests belegt (`AntiCheatTests`), nicht im Browser.

`full-flow.spec.ts:116` (`sohn.getByRole("link", { name: /TEST/ })`) ist **ungescopet**: Jede Positions-Karte
trägt genau einen TEST-Link ([SohnHome.tsx:141-145](../../frontend/src/sohn/SohnHome.tsx)), aber sobald ein
Plan eine zweite testbare Position bekommt, findet der Locator zwei Treffer und Playwrights Strict-Mode
bricht ab. Heute geht das gut, weil `full-flow.spec.ts` nur eine Position anlegt – die Robustheit ist Zufall,
kein Vertrag.

## Die echte Lücke

Drei unabhängige, kleine Defekte – keiner verzahnt mit einem anderen (anders als bei B-37):

1. Ein vorbestehendes fehlendes Ref-Gate erzeugt Karteileichen-Sitzungen bei Remount/StrictMode.
2. Der Ergebnisbildschirm kennt die Versuchsnummer nicht und bietet einen Knopf an, der serverseitig schon
   abgewiesen wird – der Vertrag müsste sie mitliefern.
3. Der Übungs-Ausstieg ist im Browser nicht geklickt, und ein Locator ist nur durch Zufall eindeutig.

## Offene Punkte

1. ~~**Punkt 1: jetzt beheben oder zurückstellen?**~~ → **Entscheidung 1**
2. ~~**Punkt 1: Ist der Umfang tatsächlich nur `SohnPractice.tsx`?**~~ → geprüft, siehe Ist-Stand oben;
   ja, alle anderen Sohn-Effekte lesen nur oder schreiben `localStorage`.
3. ~~**Punkt 2: Wie bekommt der Bildschirm die Versuchsnummer?**~~ → **Entscheidung 2**
4. ~~**Punkt 2: Was liefert das Feld für den Vater (Vorschau, kein Deckel)?**~~ → **Entscheidung 3**
5. ~~**Punkt 3: Eigenes Spec oder Ergänzung im bestehenden Durchstich?**~~ → **Entscheidung 4**
6. ~~**Punkt 3: Login-Helfer teilen oder duplizieren?**~~ → **Entscheidung 5**
7. ~~**Bleibt das eine Story oder wird geteilt?**~~ → **Entscheidung 6**

## Entscheidungen

1. **Punkt 1 wird jetzt behoben, nach dem Muster von `SohnTest.tsx`.** `SohnPractice.tsx` bekommt dieselbe
   `startedFor`-Ref (Schlüssel `` `${planId}:${positionId}` ``), die den Effekt-Rumpf beim zweiten Lauf
   überspringt.
   *Begründung:* Das Muster liegt fertig und bewährt im Nachbar-Screen vor; der Defekt ist harmlos, aber
   seit B-37 ist die Sitzung die Recheneinheit, und eine wachsende Zahl ununterscheidbarer Karteileichen
   ist genau die Art Rauschen, die eine spätere Fortsetzen-Funktion unmöglich macht.
   *Kosten:* eine Ref plus ein Guard am Effekt-Anfang – keine neue Abhängigkeit, kein Vertrag.

2. **`SubmitResponse` bekommt ein additives Feld `AttemptsRemaining` (int).** Berechnet in
   `PositionTestsController.Submit` aus derselben Tageszählung, die `Start` schon für den Deckel nutzt
   (`TestAttempts`-Anzahl des Tages ohne `BySupervisor`, `MaxAttemptsPerDay - used`, nie negativ).
   *Begründung:* Der Wert existiert serverseitig längst (`Start` zählt ihn schon für die
   `TestAttemptsExhausted`-Prüfung) – er muss nur mitgeliefert werden. Ein neues Feld statt eines Umbaus von
   `AttemptResponse`/`AttemptDetail`, weil nur der Ergebnisbildschirm ihn braucht.
   *Kosten:* additiv, **keine Migration** (nichts Neues in der DB, reine Berechnung), **kein
   Vertragsbruch** (zusätzliches Feld, bestehende Clients ignorieren es). Ein bestehender Frontend-Test
   (`TestResult.test.tsx:16`, `Omit<TestSubmitResponse, "items" | "wrongMentions">`) muss das neue Pflichtfeld
   in seinem `base`-Objekt ergänzen, sonst bricht der Typcheck – das ist gewollte Reibung, kein Risiko.

3. **Für einen Supervisor-Versuch liefert das Feld unverändert `MaxAttemptsPerDay`.** Der Deckel gilt nie
   für den Vater (`BySupervisor`), der Wert ist dort also reine Füllung, nicht Aussage.
   *Begründung:* Eine zweite, Rollen-abhängige Bedeutung desselben Feldes wäre die Art Doppeldeutigkeit,
   die B-02 schon einmal produziert hat; ein konstanter „genug Versuche"-Wert hält die Frontend-Regel
   (`attemptsRemaining <= 0` blendet aus) einfach, ohne ein zweites Flag `IsCapped` einzuführen.
   *Kosten:* ein Satz XML-Doku am Feld, der die Rollen-Asymmetrie festhält – sonst liest jemand später
   „0 heißt kein Versuch mehr" als generelle Aussage.

4. **Punkt 3 bekommt ein eigenes, kurzes E2E-Spec** (`e2e/uebung-abbruch.spec.ts`): Übung starten, „Runde
   beenden" klicken, belegen, dass die Sitzung serverseitig endet (erneuter Eintritt legt eine **neue**
   Sitzung an statt die alte fortzusetzen – anders als beim Test gibt es hier kein Resume). Der Locator-Fix
   in `full-flow.spec.ts:116` (Scope auf die Positions-Karte der Übung, Muster wie
   `full-flow.spec.ts:64`) läuft direkt im bestehenden Spec mit, weil er dort ohnehin schon steht.
   *Begründung:* Ein Klick auf „Runde beenden" mitten im bestehenden Durchstich schnitte den Rest ab (wie
   im Kommentar an Ort und Stelle vermerkt); ein eigenes kurzes Spec ist billiger als ein zweiter,
   paralleler Durchstich.
   *Kosten:* eine neue Spec-Datei mit eigenem Login-Vorlauf.

5. **`vaterLogin`/`sohnLogin` werden aus `full-flow.spec.ts` in eine gemeinsame `e2e/helpers.ts`
   extrahiert und von beiden Specs importiert.**
   *Begründung:* Ohne das müsste das neue Spec die beiden Funktionen duplizieren – die zweite Kopie wäre
   die erste Stelle, die beim nächsten Login-Umbau vergessen wird.
   *Kosten:* eine neue Datei, eine Umstellung von `function` auf `export function` an zwei Stellen; kein
   Verhaltensunterschied in `full-flow.spec.ts`.

6. **Es bleibt eine Story, kein Teilen.** Alle drei Punkte sind unabhängig **und** klein – anders als bei
   B-37 (dort wären zwei Entscheidungen einzeln sogar schädlich gewesen) hängt hier nichts aneinander; eine
   Karte (`/wayfinder`) lohnt nur, wenn Antworten weitere Fragen aufreißen, was hier nicht der Fall ist.
   *Begründung:* Drei unabhängige S-Fixes in einer Story halten den Overhead (Frontmatter, Schätzung,
   Testweg) einmal statt dreimal vor.
   *Kosten:* die Abnahme braucht drei getrennt nachvollziehbare Regressionstests statt einer großen
   Geschichte – das ist bereits im Testweg unten so vorgesehen.

## Akzeptanzkriterien

1. Ein Remount/StrictMode-Doppellauf des Sitzungs-Start-Effekts in `SohnPractice.tsx` legt nur **eine**
   `PracticeSession` an. Regressionstest ist vor der Änderung rot.
2. `SubmitResponse` trägt `AttemptsRemaining`. Nach dem ersten von zwei Tages-Versuchen liefert `Submit`
   `AttemptsRemaining: 1`, nach dem zweiten `0`. Ein Supervisor-Versuch liefert unabhängig vom Tages-Zähler
   einen Wert `> 0`.
3. `TestResult` zeigt „Nochmal versuchen" **nicht**, wenn `!passed && attemptsRemaining <= 0`; bei
   verbleibenden Versuchen bleibt der Knopf wie heute sichtbar und funktionsfähig.
4. Ein neues E2E-Spec klickt „Runde beenden" während einer laufenden Übungsrunde und belegt, dass die
   Sitzung serverseitig beendet wird (erneuter Eintritt legt eine neue Sitzung an, keine Fortsetzung der
   alten).
5. Der TEST-Link in `full-flow.spec.ts` ist auf die Positions-Karte der Übung gescoped und bleibt
   eindeutig, sobald ein Plan eine zweite testbare Position trägt.
6. `vaterLogin`/`sohnLogin` stehen exportiert in `e2e/helpers.ts` und werden von beiden Specs genutzt.
7. Nichts davon endet als neuer „offen:"-Vermerk irgendwo sonst.

## Schätzung

**S · `wo: beides` (Backend zuerst) · `migration: nein` · `vertragsbruch: nein`**

Drei unabhängige, je kleine Änderungen (eine Ref, ein additives Feld plus eine Bedingung, ein neues Spec
plus ein Locator-Fix) – näher am S-Anker (B-01, „`childId` aus dem Test-Pfad ziehen": eine Stelle, ein
Gedanke, dreifach) als am M-Anker (B-03, ein neuer substanzieller Pfad im `MediaSelector`). Keine Etappe,
kein Schema, kein neuer Endpunkt.

Die beiden Flags sind **nachgesehen, nicht vermutet**: `AttemptsRemaining` ist eine reine Berechnung aus
bereits vorhandenen Zeilen (`TestAttempts`-Zählung, die `Start` schon nutzt) – keine neue Spalte, also kein
Falten der Migrationskette. `Pugling.Contracts` bekommt nur ein **zusätzliches** Feld auf einem bestehenden
Response-DTO (additiv, kein Breaking Change) – die einzige Konstruktionsstelle
(`PositionTestsController.cs:426`) wird angepasst, kein zweiter Aufrufer betroffen.

### Risiken

| # | Risiko | Umgang |
| --- | --- | --- |
| R1 | `TestResult.test.tsx:16` tippt `base` als `Omit<TestSubmitResponse, "items" \| "wrongMentions">` – ein neues Pflichtfeld auf `TestSubmitResponse` bricht den Typcheck an dieser Stelle. | Erwartete Reibung, kein Blindgänger: `base` bekommt eine Zeile `attemptsRemaining: 2` (oder je Testfall passend). |
| R2 | `frontend/src/lib/contract.ts` wird aus `docs/openapi/v1.json` generiert (`npm run gen:contract`) – ein vergessener Regenerier-Lauf ließe das neue Feld im Frontend unsichtbar aussehen. | Reihenfolge im Angriffsplan einhalten: Backend bauen, Testlauf schreibt das Dokument, dann `gen:contract`, dann erst die UI-Bedingung schreiben. |
| R3 | `DocsCaptureTests` überschreibt `docs/api-examples` bei jedem Lauf – das neue Feld ändert das Submit-Beispiel. | Kein Fehler, aber im Diff zu prüfen und mitzucommitten (wie B-37/R7). |
| R4 | Das neue E2E-Spec dupliziert sonst den Login-Vorlauf und wird beim nächsten Login-Umbau vergessen. | Entscheidung 5: gemeinsame `e2e/helpers.ts` statt einer zweiten Kopie. |
| R5 | Der Ref-Gate-Fix in `SohnPractice.tsx` könnte mit dem Heartbeat-Cleanup-Effekt kollidieren (zwei Effekte, gemeinsame `session.current`-Ref). | Nur der Start-Effekt bekommt die neue Guard-Ref; der Heartbeat-Effekt (eigener `useEffect`, [SohnPractice.tsx:68-87](../../frontend/src/sohn/SohnPractice.tsx)) bleibt unverändert – dieselbe Aufteilung wie in `SohnTest.tsx` heute schon zwischen Start und `resume()`. |

### Angriffsplan

**Backend zuerst** – das neue Feld muss stehen, bevor die Oberfläche darauf reagieren kann.

1. `SubmitResponse` um `AttemptsRemaining` erweitern, `Submit` berechnet und füllt es (R2 beachten:
   danach `gen:contract` laufen lassen).
2. Regressionstest in `PositionTestFlowTests` (dritter/zweiter Tages-Versuch, Supervisor-Versuch).
3. `SohnPractice.tsx`: `startedFor`-Ref am Start-Effekt ergänzen (Entscheidung 1); Vitest-Regressionstest
   für den Doppellauf.
4. `TestResult` in `SohnTest.tsx`: Retry-Knopf-Bedingung um `attemptsRemaining > 0` erweitern;
   `TestResult.test.tsx` um den Verbirg-Fall ergänzen (R1).
5. `e2e/helpers.ts` anlegen (Entscheidung 5), `full-flow.spec.ts` auf den Import umstellen, TEST-Link
   scopen.
6. `e2e/uebung-abbruch.spec.ts` neu schreiben (Entscheidung 4).
7. Verifikation: `dotnet test Pugling.sln -c Release`, `npm test` (Vitest), `npm run test:e2e`
   (Playwright), danach `pugling-reviewer` **und** `frontend-reviewer`.

### Testweg

Benannt, nicht behauptet:

- `PositionTestFlowTests` – `AttemptsRemaining` über die zwei Tages-Versuche und einen Supervisor-Versuch.
- Neuer Vitest-Test bei `SohnPractice.tsx` (Komponente/Effekt-Doppellauf, Muster: eine Sitzung erwarten,
  nicht zwei `startSession`-Aufrufe).
- `TestResult.test.tsx` – neuer Fall „verbirgt den Retry-Knopf bei `attemptsRemaining: 0`".
- `frontend/e2e/uebung-abbruch.spec.ts` (neu) – Klick auf „Runde beenden", Sitzungsende serverseitig belegt.
- `frontend/e2e/full-flow.spec.ts` – TEST-Link-Scope läuft im bestehenden Durchstich mit.

## Verlauf

- **2026-08-01** — angelegt aus dem `frontend-reviewer`-Lauf zu B-37. Bewusst nicht dort mitgemacht:
  Punkt 1 ist vorbestehend und nicht von B-37 verursacht, Punkt 2 braucht eine Vertrags-Entscheidung
  (der Bildschirm kennt die Versuchsnummer nicht), Punkt 3 ist Testarbeit an einem anderen Spec.
- **2026-08-03** — `idee` → `ausformuliert`: gegen den Code belegt. Der Umfang von Punkt 1 ist geprüft
  (einzige Stelle: `SohnPractice.tsx`, alle anderen Sohn-Effekte lesen nur oder schreiben `localStorage`);
  Punkt 2 braucht ein additives `AttemptsRemaining` auf `SubmitResponse`; Punkt 3 zerfällt in einen
  fehlenden Klick-Test und einen ungescopten Locator.
- **2026-08-03** — `ausformuliert` → `gegrillt` (autonom getroffen, Nutzerauftrag 2026-08-04): sechs
  Entscheidungen. Kern: Punkt 1 bekommt dieselbe `startedFor`-Ref wie `SohnTest.tsx`; Punkt 2 bekommt ein
  additives Vertragsfeld statt eines neuen Endpunkts, für den Vater bewusst ein konstanter Füllwert statt
  eines zweiten Flags; Punkt 3 bekommt ein eigenes Spec plus gemeinsame Login-Helfer statt Duplikation.
  Keine Teilung – alle drei Punkte sind unabhängig und klein.
- **2026-08-03** — `gegrillt` → `geschaetzt` (autonom getroffen, Nutzerauftrag 2026-08-04): **S**,
  `wo: beides`, `migration: nein`, `vertragsbruch: nein` (beides nachgesehen: keine neue Spalte, additives
  Feld statt Vertragsbruch). Fünf Risiken, das teuerste ist die Reihenfolge Backend → `gen:contract` →
  Frontend (R2) und ein bestehender Frontend-Test, der das neue Pflichtfeld nachziehen muss (R1).
