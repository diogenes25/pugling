---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/tests, bereich/qualitaet]
aliases: [Assistent ohne Durchstich, Wizard-E2E]
status: abgenommen
prio: P2
art: Aufräumen
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/testabdeckung-plan.md#e5-sperre-und-primitive-tests
nachgeschaut: "2026-08-07"
---

# B-58 · Der Lehrplan-Assistent hat keinen Durchstich

Beim Bauen von [B-53](B-53-wizard-doppelklick.md) aufgefallen: `/vater/wizard` legt Kind, Plan und alle
Positionen in einem Zug an – **kein Playwright-Test fährt ihn zu Ende.** Direkt `ausformuliert`, nicht
`idee`: der Ist-Stand unten ist an den vier E2E-Dateien nachgesehen, nicht vermutet.

## User Story

Als **Entwickler**, der am Assistenten etwas ändert, möchte ich einen Test, der ihn **zu Ende** fährt und
die angelegte Position nachliest – damit ein vertauschtes Feld im Auftrag auffällt und nicht erst bei dem
Vater, der die App zum ersten Mal öffnet.

## Ist-Stand am Code

- `e2e/feldhilfe.spec.ts:100` öffnet `/vater/wizard` (Zeile hat sich seit dem Anlegen der Story verschoben,
  Inhalt unverändert), klickt aber nur einen Feldhinweis auf; es wird nichts abgeschickt.
- `e2e/vater-von-null.spec.ts` richtet ein Szenario „von Null" ein – über den **manuellen** Weg
  (Dashboard → Neuer Plan → Positionen), nicht über den Assistenten.
- `e2e/lehrer-konto.spec.ts:36` prüft nur, dass „Assistent" in der Navigation **fehlt**.
- Damit hat der Abschluss (`VaterWizard.finish()` → `runWizardFinish`) genau eine Absicherung: die sieben
  Unit-Fälle in `src/vater/wizardFinish.test.ts` plus `tsc`. Die **Verdrahtung** des Bildschirms mit dem
  echten `api`-Objekt und dem Router prüft nichts.
- **Nachrecherchiert am 2026-08-04:** Der parallel gelaufene KI-Agenten-Sprint (B-18/B-19) hat keinen echten
  Durchstich für den Lehrplan-Assistenten selbst gebracht – beide Stories stehen weiter auf `geschaetzt`,
  nicht `in-arbeit`/`abgenommen`. `VaterWizard.tsx` (452 Zeilen, fünf Schritte `Kind → Problemfeld →
  Übungen → Feinschliff → Überblick`) und `wizardFinish.ts` sind unverändert der einzige Weg, aus
  Katalog-Übungen automatisch einen Plan zu bauen; ein automatischer LLM-Generator (B-18/B-19) existiert
  nicht daneben. Die Lücke ist also unverändert real.

## Die echte Lücke

Nicht „ein Test fehlt", sondern: der Weg **eines neuen Vaters** ist unbeobachtet. B-53 nennt ihn selbst „der
Einstiegsweg"; er schreibt in einem Klick mehr als jeder andere Bildschirm (Kind + Plan + n Positionen), und
sein Fehlschlag trifft jemanden, der die App zum ersten Mal benutzt und keinen Vergleich hat. Dass der
Doppelklick-Defekt dort bis 2026-08-01 unbemerkt lag, ist die Folge – nicht der Anlass.

Die Auslagerung nach `wizardFinish.ts` hat den Ablauf prüfbar gemacht, aber genau *seine* Naht nicht:
Was der Bildschirm aus fünf Schritten in den Auftrag schreibt, ist nur so richtig wie `tsc` das sehen kann –
und `tsc` sieht keine verwechselten Zahlenfelder (`pointsGoalMet` gegen `penaltyCoins`).

## Offene Punkte

1. ~~Ein neuer Spec oder ein Abschnitt in `vater-von-null.spec.ts`?~~ → siehe Entscheidung 1.
2. ~~Reicht „ein Kind, ein Fach, eine Übung, Fertig → Plan-Seite erscheint"? Oder muss der Test die
   Feinschliff-Werte an der angelegten Position nachlesen?~~ → siehe Entscheidung 2.
3. ~~Braucht er einen Fall für die Wiederaufnahme nach einem Fehler?~~ → siehe Entscheidung 3.

## Entscheidungen

1. **Eigener Spec `e2e/assistent.spec.ts`, kein Abschnitt in `vater-von-null.spec.ts`.** Begründung: Ein
   Rot muss sofort sagen *welcher* Weg brach – der Assistent oder der manuelle Weg (Dashboard → Neuer Plan
   → Positionen), den `vater-von-null.spec.ts` (405 Zeilen, schon jetzt der größte E2E-Spec) erzählt. Ein
   sechster Handlungsstrang in dieser bereits dichten Datei würde die Zuordnung eines Fehlschlags weiter
   erschweren, nicht erleichtern. Kosten: der Login-Helfer (`vaterLogin`) wird ein weiteres Mal inline
   kopiert – das ist aber die bestehende Konvention, nicht ihr Bruch: alle neun heutigen E2E-Dateien
   (`anmerkungen`, `bilder`, `feldhilfe`, `freigabe`, `full-flow`, `lehrwerke`, `perspektiven`,
   `uebungstypen`, `vater-von-null`) tragen dieselbe lokale Kopie, es gibt keinen geteilten Helfer, der
   stattdessen importiert werden könnte.
2. **Ja, an der Plan-Seite nachlesen – mit bewusst untypischen Werten statt der Intensitäts-Vorgabe.**
   Begründung: Genau das ist die ungeprüfte Naht aus „Die echte Lücke" – `wizardFinish.ts` ist per `tsc`
   und sieben Unit-Fällen abgesichert, aber kein Test beweist, dass das Eingabefeld „Bestehen ab %" wirklich
   in `goalThreshold` und „Münz-Malus bei Versäumnis" wirklich in `penaltyCoins` landet und nicht
   vertauscht. `vater-von-null.spec.ts:275-276` liest genau diese Positions-Zeile schon für den manuellen
   Weg (`Malus −5`, `bestehen ab 80%`) – dieselbe Assertion-Form wird übernommen, nur mit zwei Werten, die
   von der `Normal`-Vorbelegung (80 / 5) klar abweichen (z. B. 95 / 7), damit ein vertauschtes Feld nicht
   zufällig plausibel aussieht. Kosten: keine über die ohnehin geplante Testarbeit hinaus.
3. **Kein E2E-Fall für die Fehler-Wiederaufnahme.** Begründung: Um einen Zwischenschritt (`createChild`/
   `createPlan`/`addPosition`) im Browser scheitern zu lassen, bräuchte es entweder ein `page.route`-Mock
   (wie das Konflikt-Szenario in `vater-von-null.spec.ts:57-66`) oder eine echte Server-Ablehnung (z. B.
   die leere Vokabelübung `exercise_empty`, die der Kommentar in `wizardFinish.ts:104` nennt) – und genau
   diesen Verzweigungspfad prüfen die sieben Unit-Fälle in `wizardFinish.test.ts` bereits deterministisch
   und ohne Browser-Overhead. Ein E2E-Duplikat kostet einen vollen Roundtrip, um denselben Zweig zu
   beweisen, den der Unit-Test schon flakefrei abdeckt. Zurückgestellt, nicht offen – kein neuer Aufwand in
   dieser Story.

## Akzeptanzkriterien

1. Ein neuer Playwright-Spec (`e2e/assistent.spec.ts`) meldet sich als Vater an (Seed-Konto `fid=1`,
   PIN `0000`, wie `feldhilfe.spec.ts`), durchläuft `/vater/wizard` von Schritt 1 „Kind" (**neues** Kind mit
   Lauf-Suffix, Muster `E2E-…-${RUN}` wie in `vater-von-null.spec.ts`) bis Schritt 5 „Überblick", klickt
   „✅ Lehrplan erstellen" und landet auf `/vater/plan/{id}`.
2. Schritt 4 „Feinschliff" setzt „Bestehen ab %" und „Münz-Malus bei Versäumnis" auf zwei Werte, die von
   der Intensitäts-Vorbelegung abweichen (z. B. 95 / 7 statt 80 / 5) – siehe Entscheidung 2.
3. Auf der Plan-Seite liest der Test die entstandene Positions-Zeile und prüft **beide** Werte wortgleich
   zum bestehenden Muster (`bestehen ab 95%`, `Malus −7`); ein vertauschtes Feld fällt durch.
4. Der Test zählt die abgeschickten `POST …/supervisor/children` beim Klick auf „Lehrplan erstellen"
   (`dblclick`, Muster aus `vater-von-null.spec.ts:349-359` für die Key-Result-Zählung) und verlangt genau
   **einen** – der Beleg für die B-53-Sperre am echten Knopf.
5. Der Spec läuft ohne Sonder-Setup im vorhandenen `npm run test:e2e`-Job mit.
6. Kein Fall für die Fehler-Wiederaufnahme (Entscheidung 3).

## Schätzung

- **Größe:** S – ein neuer, in sich abgeschlossener Playwright-Spec ohne Produktionscode-Änderung, gebaut
  aus drei bereits vorhandenen Mustern (Wizard-Teilnavigation aus `feldhilfe.spec.ts:100-114`,
  Positions-Zeilen-Assertion aus `vater-von-null.spec.ts:273-276`, Doppelklick-plus-POST-Zählung aus
  `vater-von-null.spec.ts:349-359`).
- **Wo:** frontend (reiner E2E-Test; kein Backend-, Contract- oder Client-Code betroffen).
- **Migration:** nein – keine Schemaänderung.
- **Vertragsbruch:** nein – der Test konsumiert nur bestehende Endpunkte über die UI, `Pugling.Contracts`
  bleibt unberührt.
- **Risiken:**
  - Abhängig vom Seed (Fach „Englisch" mit vorhandenen Katalog-Übungen in Schritt 3) – dasselbe Risiko
    tragen `feldhilfe.spec.ts` und `vater-von-null.spec.ts` bereits, kein neues.
  - Playwright-Timing beim Doppelklick-Zähler – bereits im B-53-Test gelöstes Muster (`dblclick` +
    Request-Listener + Zählung nach der Erfolgsmeldung), keine neue Unsicherheit.
  - Kind-Name braucht den Lauf-Suffix, sonst Kollision bei wiederholten Läufen gegen dieselbe Temp-DB –
    überall sonst im Repo schon so gelöst.
- **Angriffsplan** (kein Backend-Anteil, daher nur der Frontend-Weg):
  1. `frontend/e2e/assistent.spec.ts` anlegen, `vaterLogin`-Helfer wie in den Nachbardateien lokal kopieren.
  2. Wizard-Navigation aus `feldhilfe.spec.ts:100-114` übernehmen, statt abzubrechen bis „Feinschliff"
     fortsetzen (Schritt 1: neues Kind statt Popover-Test).
  3. Feinschliff-Werte abweichend von der Vorbelegung setzen, `POST …/supervisor/children` mitzählen,
     „✅ Lehrplan erstellen" per `dblclick` auslösen.
  4. Auf der Plan-Seite die Positions-Zeile lesen (Muster `vater-von-null.spec.ts:273-276`) und beide Werte
     sowie die POST-Zahl prüfen.
  5. Lokal `npx playwright test assistent` grün bekommen, danach im vollen `npm run test:e2e`-Job.
- **Testweg:** der neue Spec selbst, im bestehenden Playwright-Job (`npm run test:e2e`, CI-Job aus
  `.github/workflows/ci.yml`) – kein zusätzlicher Testweg nötig, das *ist* die Lieferung.

## Verlauf

- **2026-08-01** — angelegt beim Bauen von E5'/[B-53](B-53-wizard-doppelklick.md); Ist-Stand direkt an den
  vier E2E-Dateien belegt, die den Assistenten erwähnen.
- **2026-08-04** — gegrillt: Ist-Stand gegen den heutigen Code nachgesehen (eine Zeilennummer in
  `feldhilfe.spec.ts` korrigiert, Inhalt sonst unverändert; B-18/B-19 haben keinen Durchstich gebracht),
  alle drei offenen Punkte in Entscheidungen überführt, Akzeptanzkriterien finalisiert (autonom getroffen,
  Nutzerauftrag).
- **2026-08-04** — geschätzt: `groesse: S`, `wo: frontend`, `migration: nein`, `vertragsbruch: nein`,
  Risiken, Angriffsplan und Testweg ergänzt (autonom getroffen, Nutzerauftrag).
- **2026-08-06** — gebaut (Nachtlauf 2, Sprint 2): neuer Spec `frontend/e2e/assistent.spec.ts` — neues
  Kind (Lauf-Suffix), Fach Englisch, eine seed-stabile Übung (Suche nach „environment"), Feinschliff
  bewusst von der Vorbelegung abweichend (95%/7 statt 80/5), Doppelklick auf „✅ Lehrplan erstellen" mit
  Zählung der `POST …/supervisor/children` (genau 1), Positions-Zeile geprüft auf beide Werte.
  **Gegenprobe (kein Produktivcode für diese Story, aber der Fehlerklasse wegen durchgeführt):**
  `goalThreshold`/`penaltyCoins` in `VaterWizard.tsx` testweise vertauscht → Test sofort rot
  (`Received: "…bestehen ab 7% … Malus −95…"` statt 95/7), zurückgenommen (`git diff` danach leer).
  **Rollengang:** dieser Spec selbst ist der Rollengang (pm-loop: eine E2E, die den Weg fährt, zählt als
  Rollengang) — echter Browser gegen echten Server, derselbe Weg, den ein Vater ginge.
  `npm run test:e2e` → **27/28 grün** (einziger Ausfall: der vorbestehende B-109-Flake in
  `full-flow.spec.ts`, unverändert). `frontend-reviewer` bestätigte Selektoren, Konventionstreue und dass
  die Assertions gegen den echten Render-Code (`PlanPositions.tsx:500,506`) und die echte Sperre
  (`VaterWizard.tsx`) geprüft sind, kein Blocker.
- **2026-08-07** — Nachschau (Nachtlauf): geprüft, ob `frontend/e2e/assistent.spec.ts` weiterhin als eigene
  Spec-Datei existiert — hält. Kein Fund.
