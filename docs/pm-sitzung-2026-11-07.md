# PM-Sitzung: Lauf AC – „Verwendung" kennt alle Sperren

**Datum:** 2026-11-07  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Ein Lauf mit B-409 („Lauf mit B-409", Auftrag im Dialog), danach Retrospektive mit Nachschau zu Sprint 19 (B-270).
Es gelten erstmals die am selben Tag entschiedenen Regeln aus `5143e75` (Proben-Skript, Fälle Weg × Wert, Meldungen ohne Auswege).

## Refinement

B-409 von `idee` bis `geschaetzt` am Code (Defekt, im Lauf gegrillt). Befund schärfer als die Notiz: hängt eine Übung nur an einer
eigenen Ziel-Etappe, verspricht „Verwendung" wörtlich „Löschen ist möglich", und `DELETE` antwortet `409`.

## Sprint 20 — Ziel & Umfang

**Sprint-Ziel:** Der Vater sieht unter „Verwendung" jede Stelle bei seinen Kindern, die das Löschen der Übung sperrt – die Anzeige
widerspricht der Löschsperre nie.
**Umfang:** B-409.
**Entwickler-Brief:** Quelle der Wahrheit bleibt `ExerciseUsageQueries.CountBlockingAsync`; die neue Ziel-Liste trägt dasselbe Prädikat
(`KeyResult.ExerciseId == id`, Kind vom Aufrufer betreut). Additives Antwortfeld `goals`, keine Migration. Danach `UsagePanel`
(`hasOwn` schließt Ziele ein, eine Zeile „Ziele"). Testweg: `ExerciseUsageScopeTests`, Vitest am Panel, E2E `verwendung-ziele.spec.ts`;
Belege mit `.claude/scripts/mutant-probe.py`.

## Iteration 20 — umgesetzt

**Backend (API-First):** `GoalUsage` + `UsageResponse.Goals` (additiv), Abfrage mit dem Prädikat der Löschsperre
(`KeyResult.ExerciseId == id`, Kind betreut). **Frontend:** `UsagePanel` zählt Ziele mit und zeigt eine Zeile „Ziele". **Doku:**
B-406 AK-Zeilen 1/2 und ein Test-Summary berichtigt, `docs/uebergabe-vokabeln.md`, OpenAPI neu.
**Verifikation:** Backend 1123/1123, Vitest 656/656, `tsc -b` sauber, Playwright 119 passed. Sieben Mutanten mit
`.claude/scripts/mutant-probe.py`, alle rot am eigenen Fall, gemessen nach dem letzten Test-Edit (Tabelle in B-409).

## Runde 2 — Review

`pugling-reviewer`: keine 🔴; 🟡 der Ausschluss der Unit-Etappen stand im Vertragstext, aber in keinem Test → Fall 3b; 🟡 Action-Summary
ohne Ziele → berichtigt. `frontend-reviewer`: keine 🔴; zwei 🟡 an altem Text derselben Meldungen → [B-413](backlog/B-413-loeschsperre-unit-verweist-auf-verwendung-ohne-unit-ziele.md),
[B-414](backlog/B-414-meldungstexte-nennen-auswege.md), bewusst nicht im Sprint (das Ziel betrifft das Löschen einer Übung). **Zähler 4 von 5.**

## Runde 3 — Abnahme

- **Vater:** Rollengang als E2E `verwendung-ziele.spec.ts` – Übung an einem eigenen Ziel, „Verwendung" nennt „Ziele: … – Achtzig Prozent
  (…)", kein „nirgends eingesetzt". Sprint-Ziel gehalten: für das Löschen einer Übung widerspricht die Anzeige der Sperre nicht mehr.
- **Creator, Sohn:** Regressionszeugen – ganze Suite und ihre Specs grün, kein Spielweg vom Diff berührt.

B-409 `abgenommen`.

## Retrospektive

**Nachschau:** Sprint 19 (B-270, `ecbe5d8`), frischer `frontend-reviewer`: ein 🟡 – „beste zuerst" ist nur gegen „nicht sortiert" belegt;
weil der Server Unbewertete in beiden Richtungen ans Ende stellt, bliebe `dir: "asc"` grün. Prämisse am Controller nachgeprüft (stimmt) →
[B-412](backlog/B-412-beste-zuerst-richtung-unbelegt.md), `entgangen_bei: [B-270]`. B-270 `nachgeschaut: 2026-11-07`. Increment eines
früheren Laufs (AB) – kein Limit 2 für diesen Lauf. Index: Nachgeschaut 343 von 344 (offen nur B-409 selbst).

**Was die eigenen Tore durchgelassen haben:**
1. Der Vertragstext versprach „Milestones on a whole unit are not listed", die Fall-Tabelle hatte den Wert „Unit-Etappe" aber nicht –
   obwohl der Ist-Stand ihn ausdrücklich nannte. Die Regel „Fälle Weg × Wert" (seit heute in `pm-loop`) hätte ihn verlangt; ich habe die
   Werte aus dem Gedächtnis aufgezählt statt aus dem Ist-Stand. Der Reviewer fand ihn. Kein neuer Mechanismus: die Regel ist einen Tag
   alt, ihr erster Einsatz; erst eine Wiederholung rechtfertigt eine Schärfung.
2. Das neue Proben-Skript brach im ersten echten Einsatz zweimal: es prüfte nur die Grundlinie des **ersten** Testbefehls (dotnet), nicht
   die von Vitest und Playwright; und es erriet das Zeilenende je Datei aus dem ersten CRLF – der Formatierungs-Hook schreibt berührte
   Zeilen als CRLF in LF-Dateien, das Skript fand die Zeile nicht und brach ab (schrieb nichts, misst nichts falsch).

**Der eine Mechanismus:** `.claude/scripts/mutant-probe.py` – Grundlinie je Testbefehl, Zeilenenden je Zeile erhalten (nur an `\n`
getrennt), beides im selben Sprint gemessen (Rundlauf byte-genau an drei gemischten Dateien, danach sieben Mutanten über dotnet, Vitest und
Playwright). Kein neues Tor – das Werkzeug wird zuverlässiger, das Verfahren ändert sich nicht.

**Bewusst ungedeckt:** dass ein Wert, den der Ist-Stand nennt, in der Fall-Tabelle fehlt – das prüft kein Grep.

## Offene Roadmap

Neu: B-412 (Defekt, aus der Nachschau), B-413 (Defekt), B-414 (Aufräumen) – B-413 und B-414 betreffen dieselben zwei Meldungen und
gehören in einen Sprint, zusammen mit B-411 (deutsche Texte nach derselben Regel). Weiter offen: B-404/B-407 (Fragen), B-408 (Wunsch),
B-410/B-411 (Tore). Nichts gepusht.
