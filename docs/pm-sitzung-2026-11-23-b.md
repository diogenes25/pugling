# PM-Sitzung: Nachtlauf B – Flackern, Fokus-Effekt, Pflicht wird Kür (Worktree)

**Datum:** 2026-11-22/23 (Shell-Datum beim Start: 2026-11-22)  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die zwei offenen Test-Defekte aus Lauf B vom 2026-11-22 schließen, danach B-659 bauen.

Paralleler Lauf zu Nachtlauf A auf `main`. Die PO-Rolle hat der Nutzer übertragen: stellvertretend entscheiden und
protokollieren, statt anzuhalten.

- **Branch:** `claude/nachtlauf-b-dcf748`, Worktree `.claude/worktrees/nachtlauf-b-dcf748`.
- **Basis:** `ec0c705a`. Der Worktree stand beim Start auf `560bd2f0` und wurde per `--ff-only` vorgezogen.
- **Bereich:** B-787, B-788, B-659, danach frontend-/testlastige Stories. B-791 und B-670 gehören Lauf A, ebenso
  backend-fachliche Defekte (außer B-659).
- **Nummern:** Lauf B vergibt nur B-820..B-839.
- **Ports:** E2E auf :5207/:5179. :5200 ist die echte DB (Heimnetz) und blieb unberührt.
- **`frontend/node_modules`:** als Junction auf den Haupt-Checkout (Playwright 1.65.1).
- Nichts gepusht, nichts nach `main` gemergt.

## Runde 1 — Einrichtung und Auswahl

- **Rollen:** B-787 und B-788 sind Testcode plus eine Zeile im Dep-Array. Kunde ist der Entwickler; die drei
  Kundenrollen sind Regressionszeugen.
- **Befund vorab:** Drei volle Vitest-Läufe in Dateien geschrieben: 1327/1327, 1327/1327, 1327/1327. Das Flackern trat
  nicht von selbst auf, darum wurde es mit einer eingeschleusten Pause hergestellt (siehe B-787).

## PM-Synthese & Priorisierung (→ Entwickler)

| Prio | Story | Größe | Wo | Inhalt |
|---|---|---|---|---|
| P3 | [B-787](backlog/B-787-sohnprogress-vitest-flackert-unter-last.md) | XS | frontend (Vitest) | `findByText(/Plan 5/)` trifft Knopf und Zusammenfassung |
| P3 | [B-788](backlog/B-788-fokus-effekt-abhaengigkeiten-ungetragen.md) | XS | frontend | Dep-Array des Fokus-Effekts, Kommentar verspricht ein Rot |
| P3 | [B-659](backlog/B-659-pflicht-wird-kuer-bricht-die-serie.md) | L | backend + Migration | Pflicht → Kür bricht die Serie rückwirkend |

**Roter Faden Sprint B1:** Jede Zusicherung im Sohn-Bereich sagt, was sie trägt – und trägt es.

## Sprint B1 — Ziel & Umfang

**Sprint-Ziel:** Aus Sicht des Entwicklers fällt `SohnProgress.test.tsx` nicht mehr durch die Zeitplanung, und kein
Kommentar am Fokus-Effekt verspricht ein Rot, das keine Mutante liefert. Widerlegbar durch eine Pause nach dem Rendern,
die die Datei rot macht, oder eine Mutante, deren Ausgang dem Kommentar widerspricht.

**Umfang:** B-787, B-788 (beide `idee` → im Lauf autonom gegrillt, Defekt).

**Entwickler-Brief:** Erst die Meldung einfangen, dann reparieren. Kein Produktverhalten ändert sich. Proben über
`mutant-probe.py`, Treffer der Pause über die Meldung zugesichert (Retro-Lehre aus Lauf B).

## Iteration B1 — umgesetzt

### B-787 (`7b501ed4`)

- **Meldung:** Mit 50 ms Pause nach `renderProgress()` auf `ec0c705a`: 2 failed / 7 passed,
  `TestingLibraryElementError: Found multiple elements with the text: /Plan 5/` bzw. `/Plan 99/`. Die Planwahl
  (Knopf „Plan 5“) und die Zusammenfassung („Plan 5 · …“) tragen denselben Titel.
- **Die Vermutung der Idee** (`setPlanId` ohne `waitFor`) ist damit widerlegt.
- **Reparatur:** Die Suche zielt auf die Zusammenfassung (`/Plan 5 ·/`). Mit derselben Pause in acht Fällen 9/9 grün.
- **Mutanten:** „spielbar“ und „gemerkt“ in `useChosenPlan` gestrichen, beide rot am richtigen Fall.

### B-788 (`d7b7546b`)

- **Gemessen:** Der Effekt lief im Fall AK 3 viermal. Die neue Frage, `cursor` und `busy = false` kommen in **einem**
  Render; `busy` wechselt allein nur auf `true` (und findet dort bewusst kein Ziel).
- **Entscheidung:** `cursor` raus (nicht gelesen), `action.busy` bleibt (gelesen, Sicherung gegen ein künftiges `await`
  nach `setItem`), beide Kommentare richtiggestellt.
- **Mutanten:** busy gestrichen → grün (erwartet grün), cursor zurück → grün (erwartet grün), Absicht ohne Ziel verbraucht
  → rot an AK 3 (erwartet rot).

### Verifikation

- Vitest voll: 1327/1327. `tsc -b`: sauber, nach `node scripts/gen-contract.mjs` (im frischen Worktree fehlte
  `src/lib/contract.ts`; ohne ihn meldet `tsc` 3 Fehler in fremden Dateien).
- Review `frontend-reviewer`: keine 🔴/🟡. Zwei 🟢 zum Kommentar eingearbeitet (ohne Zählung). Zähler Sprint B1: 0.

## Runde 2 — Re-Review / Abnahme Sprint B1

- **Sohn:** Maschinengang mit E2E `full-flow`, `test-durchgefallen-ueben`, `erfolge-woche-frist` und `karten-umdrehen` auf
  :5207/:5179 gegen eine Wegwerf-DB, nach der letzten Frontend-Änderung gestartet: 4 passed. Klausur und Erfolge laufen
  wie vorher. **Abgenommen.**
- **Vater, Creator:** Regressionszeugen. Kein Pfad im Diff, Vitest voll grün.
- Hinweis: Der Server des Gangs baute schon die ungecommittete Entity/Migration von B-659 mit (Arbeitsbaum). Für den
  Sohn-Pfad ohne Wirkung.

## Retrospektive Sprint B1

**Nachschau:** Sprint B1 von Lauf B 2026-11-22 (B-602 `f79b8fed`, B-605 `04c72b89`, B-603 `20ca1ce2`, B-790 `761af289`)
durch `frontend-reviewer`, je Story mit benanntem Prüfpunkt im `## Verlauf`. Kein Befund, `nachgeschaut: 2026-11-22`
an allen vier gesetzt. Index: Nachgeschaut 536 von 568. Sprint 114 von Lauf A (B-789) gehört Lauf A.

**Was die eigenen Tore durchgelassen haben:** B-787 hat seine Meldung verloren, weil der volle Lauf durch `grep` ging.
Die Ursache lag damit eine Nacht lang falsch (Vermutung `setPlanId`).

**Vorschlag (nicht gelandet, Freigabe 3):** Vitest schreibt bei jedem vollen Lauf zusätzlich einen JSON-Bericht in eine
Datei (`reporters: ['default', ['json', { outputFile }]]` in der Vitest-Konfiguration). Dann trägt jedes seltene Rot seine
Meldung, egal wie die Ausgabe gefiltert wurde. Kosten: eine gitignorte Datei je Lauf. Das wäre ein Werkzeug, kein Tor.

**Bewusst ungedeckt:** Die Fehlerklasse „Titel neben gleichnamigem Knopf“. Die Suche des Reviewers fand keine zweite
Stelle; ein Tor dafür wäre nicht treffsicher.

## Sprint B2 — Ziel & Umfang

**Sprint-Ziel:** Aus Sicht des Sohnes reißt seine Serie nur, wenn er selbst einen Tag verpasst hat – nicht, weil Papa eine
Pflicht später auf Kür stellt oder einen gespielten Plan wegräumt. Widerlegbar durch eine Pflicht, die nach dem Wechsel auf
Kür ihre erfüllten Tage verliert, oder einen gespielten Plan, der sich löschen lässt.

**Umfang:** [B-659](backlog/B-659-pflicht-wird-kuer-bricht-die-serie.md) (Defekt, L, `geschaetzt`, Entscheidungen
stellvertretend im Vorbereitungslauf 2026-11-20).

**Entwickler-Brief:** Quelle der Wahrheit ist ein Helfer `DutyCadenceOn` (die Senke aller Tagesfragen), die Abrechnung bleibt
roh (Entscheidung 5). Migration nur angehängt, an einer Kopie der echten DB geprobt. Lauf A ist über die Migration in den
Notizen informiert (Snapshot-Kollision beim Merge möglich).

## Iteration B2 — umgesetzt (`edaacfd9`)

- **Backend:** Spalten `DutyUntil`/`CadenceUntil`, Helfer an fünf Lesestellen, `CloseDutyAsync` im PATCH (vor allen anderen
  Zuweisungen), Löschsperre über den vorhandenen `PlanHasPlayDataAsync` (B-673) mit dem vorhandenen Code `plan_has_data`.
- **Frontend:** deutscher Satz zu `plan_has_data` nennt beide Sperren, den Ausweg „Deaktivieren“ nur am Löschen;
  Bestätigungsfrage vor dem Löschen kündigt die Sperre an; neue Spec `plan-loeschen-gesperrt.spec.ts`.
- **Rote Probe vor dem Fix:** 7 failed / 5 passed, wie erwartet (Einzelheiten im Verlauf der Story).
- **Mutanten:** 15 gemessen, 14 rot am benannten Fall, 1 erwartet grün (Fortschrittsbalken eines offenen Ziels innerhalb
  einer Spanne, als ungetragen beschriftet).
- **Migration an einer Kopie der echten DB:** angewendet, 204 Positionen ohne Spanne.
- **Review `pugling-reviewer`:** 2× 🟡 behoben und gezählt (Zähler B2: 2 von 5), 🟢 eingearbeitet bzw. als
  [B-820](backlog/B-820-laufende-nacharbeit-zur-kuer-bricht-die-serie.md) angelegt.
- **Voller Lauf:** Backend 2041/2041 (Release), Vitest 1327/1327, `tsc`, Format sauber. Der volle Lauf fand außerdem
  `ActivatedAtMigrationTests` rot (lud die ganze Entity auf einem älteren Migrationsstand) – behoben per Projektion.

## Runde 3 — Re-Review / Abnahme Sprint B2

- **Vater** (seine Ebene hat sich geändert): Maschinengang `plan-loeschen-gesperrt` – er liest auf Deutsch, warum der
  gespielte Plan nicht gelöscht wird und dass „Deaktivieren“ hilft; der Plan bleibt. `plan-umhaengen-gesperrt` grün mit dem
  geänderten Satz. Den Wechsel Pflicht → Kür hat niemand in der Oberfläche geklickt; Ersatz ist der PATCH des
  Positions-Formulars in den Integrationstests. **Abgenommen**, mit diesem benannten Ersatz.
- **Sohn:** Seine Serie, Tagesansicht und Tageskiste folgen der Historie (Integrationstests); `full-flow`, `pfad`,
  `heute-dran-nach-antworten`, `erfolge-woche-frist`, beide Belohnungs-Specs grün. **Abgenommen.**
- **Creator:** Regressionszeuge, kein Pfad im Diff, Suite grün.
- 8 passed auf :5207/:5179, Server aus dem Endstand.

## Retrospektive Sprint B2

**Nachschau:** Sprint B1 (B-787 `7b501ed4`, B-788 `d7b7546b`) nachgesehen, Prüfpunkte im Verlauf beider Stories; kein
Befund, `nachgeschaut` gesetzt.

**Was die eigenen Tore durchgelassen haben:**

- Der Helfer wich von Entscheidung 2 ab („danach `None`“), und AK 9 trug die Abweichung nicht – gefunden vom Review, nicht
  von der Mutanten-Probe, weil ich die Mutante für genau diese Zeile nicht geschrieben hatte. Danach M14 plus Einzelfall.
- Der geteilte Fehlercode `plan_has_data` bekam einen Ausweg, der nur für eine der beiden Sperren gilt – gefunden vom
  Review. Die Regel „Ausweg nur für die Sperre, die er hebt“ steht in `pm-loop` Step 5; ich habe sie auf den Satz, nicht auf
  den Code angewendet.
- `ActivatedAtMigrationTests` fiel an der neuen Spalte; gefunden vom vollen Lauf.

**Vorschlag (nicht gelandet, Freigabe 3):** Ein Tor in `SchemaGuardTests`, das in `*MigrationTests.cs` nach dem
Zurücksetzen auf einen älteren Stand kein Laden ganzer Entitäten erlaubt (nur `Select`-Projektionen). Jede künftige Spalte
bricht sonst wieder einen alten Migrationstest. Vor dem Scharfstellen messen: heute 5 Klassen, davon 1 betroffen gewesen.

**Bewusst ungedeckt:** Die Fehlerklasse „Entscheidung sagt X, Code tut Y, kein Fall unterscheidet“ deckt nur eine Mutante
je Entscheidungs-Satz. Kein Mechanismus erzwingt, dass es sie gibt.

**Werkzeug-Lehre:** `DocsCaptureTests` schreibt im Worktree alle `docs/api-examples` mit LF neu; `git diff
--ignore-cr-at-eol` trennt das Rauschen vom echten Unterschied (hier nur `docs/openapi/v1.json`).

## Sprint B3 — Ziel & Umfang

**Sprint-Ziel:** Aus Sicht des Sohnes führt eine erledigte Aufgabe nicht ins Leere: wer sie noch einmal machen will, landet mit
einem Tippen in der Runde, die dafür Münzen zahlt – oder liest, was ihm dafür noch fehlt. Widerlegbar durch einen Leerzustand,
der nur „Nichts fällig“ sagt, oder einen Knopf, der in eine andere Übung führt oder Münzen verspricht, die nicht kommen.

**Umfang:** [B-660](backlog/B-660-tagesaufgaben-beliebig-oft-mit-sinkenden-muenzen.md) (`Wunsch`, M, `geschaetzt` →
baubar nach Freigabe 8). Beim Bauen geteilt, wie die Story es in Risiko 1 vorsah: das freie Üben ist
[B-821](backlog/B-821-freies-ueben-info-modus-in-der-sohn-app.md) (`idee`, `Wunsch` – die offene Frage „was zeigt eine
getippte Stufe ohne Rückmeldung?“ geben die Entscheidungen nicht her, Freigabe 8: notieren statt entscheiden).

**Auswahl:** Nach B-659 blieben im Bereich dieses Laufs keine `Defekt`/`Aufräumen` mehr (B-670, B-791 gehören Lauf A; B-649
ist Datenbereinigung der echten DB mit offener Produktfrage; B-820 ist backend-fachlich und steht neben B-659). B-660 ist der
höchst priorisierte `geschaetzt`e Frontend-`Wunsch` ohne `wartet_auf` und ohne unbebaute Backend-Vorstufe.

## Iteration B3 — umgesetzt (`199fc39f`)

- **Frontend:** `NochEinmal.tsx` im Leerzustand von `SohnPractice`; drei Lagen nach Pool, Sperre und Deckel, wie die
  Bonus-Liste.
- **Backend:** nur ein Fall, der AK 7 festhält (eine Bonus-Runde lässt den Kasten der Position unberührt).
- **Tests:** 11 Vitest-Fälle an `NochEinmal`, 1 an `SohnPractice`; Spec `nochmal-nach-pflicht` (beide Lagen der Sperre,
  Kartenliste per `page.route` leer, Treffer gezählt).
- **Mutanten:** 12, alle rot am benannten Fall.
- **Review + Selbst-Check:** 2 gezählte Funde (Deckel, Sperre vor Pool), beide behoben; Zähler B3: 2 von 5.
- **Voller Lauf:** Vitest 1340/1340, `tsc` sauber, `ExtraTrainingTests` 36/36.

## Runde 4 — Re-Review / Abnahme Sprint B3

- **Sohn:** Maschinengang `nochmal-nach-pflicht` (erst gesperrt mit der Zahl der offenen Ziele, dann in die Bonus-Runde dieser
  Übung) plus `bonus`, `bonus-deckel-voll`, `full-flow`, `pfad`, `karten-umdrehen` – 6 passed. **Abgenommen**, mit einer
  offenen **Gefühlsprüfung am Gerät** (fühlt sich der Weg für Friedrich wie „noch einmal“ an?).
- **Vater, Creator:** Regressionszeugen (Münzregeln unverändert, kein Pfad im Diff).

## Retrospektive Sprint B3

**Nachschau:** Sprint B2 (B-659 `edaacfd9`) mit drei benannten Prüfpunkten im Verlauf der Story; kein Befund.

**Was die eigenen Tore durchgelassen haben:** Der neue Block las nur den Wert der Übung, nicht den Tagesdeckel und nicht die
Reihenfolge Pool/Sperre. Beides stand schon richtig in der Nachbar-Komponente `SohnBonus.tsx` (`capReached`, Pool auch bei
Sperre). Gefunden hat es zuerst mein Selbst-Check, dann das Review; kein Tor.

**Vorschlag (nicht gelandet, Freigabe 3):** In `pm-loop` Step 4 die Regel „an der Senke, nicht an den Quellen“ um einen Satz
ergänzen: *Eine neue Fläche, die Daten einer vorhandenen Fläche anders zeigt, liest zuerst deren Entscheidungszweige*
(hier: Bonus-Liste → Leerzustand). Prosa, kein Tor – ein Tor müsste „gleiche Daten“ erkennen.

**Bewusst ungedeckt:** Die Gefühlsfrage aus der Quelle (Friedrich wollte „noch einmal“, nicht „Bonus-Training“); nur am Gerät.

## Halt nach Sprint B3

Kein neues Tor; die Review-Funde sind behoben und unter dem Fünfer-Zähler. Der Lauf hält trotzdem, weil im eigenen Bereich
nichts Baubares ohne neue Produktentscheidung übrig ist:

- **`Defekt`/`Aufräumen` außerhalb von Lauf A:** keine. B-820 (laufende Nacharbeit zur Kür) ist neu und backend-fachlich; sie
  fasst dieselbe Senke an wie B-659 und gehört sinnvollerweise nach dem Merge in einen eigenen Sprint.
- **Frontend-`Wunsch` auf `geschaetzt`:** B-704, B-743, B-757, B-762, B-763, B-766, B-769 hängen an unbebauten
  Backend-Vorstufen (B-700..703, B-741/742, B-752, B-760, B-764/765, B-768); B-619, B-622, B-655 tragen `wartet_auf`;
  B-638 (Formeln, L) braucht eine neue Bibliothek samt Service-Worker – ein eigener Lauf mit Gerätecheck.
- **B-821** wartet auf die Entscheidung oben (Dialog).

## Offene Roadmap (priorisiert)  ·  Konkreter Änderungsstand (für Review)

**Offen aus diesem Lauf:** [B-820](backlog/B-820-laufende-nacharbeit-zur-kuer-bricht-die-serie.md) (Defekt, idee),
[B-821](backlog/B-821-freies-ueben-info-modus-in-der-sohn-app.md) (Wunsch, idee, Frage an den Nutzer), die Gefühlsprüfung zu
B-660 am Gerät und drei Retro-Vorschläge (B1: Vitest-JSON-Bericht; B2: Tor gegen Entitäts-Laden in Migrationstests; B3: Satz
in `pm-loop` Step 4).

**Commits auf `claude/nachtlauf-b-dcf748`** (Basis `ec0c705a`):

| Commit | Inhalt |
|---|---|
| `7b501ed4` | B-787 |
| `d7b7546b` | B-788 |
| `13fdb7bd` | Abnahme B1, Nachschau, Protokoll |
| `edaacfd9` | B-659 (Migration `DutyUntilCadenceUntil`), B-820 neu |
| `68aa61d9` | Abnahme B2, Protokoll |
| `199fc39f` | B-660, B-821 neu |
| danach | Abnahme B3, Protokoll |

**Beim Merge nach `main` beachten:**

- **Migration:** `20261122081510_DutyUntilCadenceUntil` hängt an `20261119065732_ShopStockPerChild`. Hat Lauf A eine eigene
  Migration angelegt, kollidiert `PuglingDbContextModelSnapshot.cs`: mergen, dann die spätere Migration auf der früheren neu
  erzeugen (die Kette wird nur verlängert). Danach `dotnet test` (die Migrationstests laden seit B-659 projiziert).
- **Heimnetz:** Der nächste Start spielt die Migration in die echte `pugling.db` ein (an einer Kopie geprobt: 204 Positionen,
  keine Spanne gesetzt).
- **Nummern:** B-820 und B-821 kommen aus dem Bereich von Lauf B; nach dem Merge
  `ls docs/backlog | grep -o 'B-[0-9]*' | sort | uniq -d` prüfen.
- `INDEX.md` danach neu erzeugen (dieser Lauf hat es bewusst nicht angefasst).
- `scripts/_plan-api.ps1:89` rät noch „ggf. löschen“; seit B-659 antwortet das für gespielte Pläne mit 409.
