# PM-Sitzung: Nachtlauf B – flackernde Tests (Worktree)

**Datum:** 2026-11-22  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Ein Rot der Test-Suiten heißt wieder „Fehler im Code“ und nicht „nochmal laufen lassen“.

Paralleler Lauf zum Nachtlauf A auf `main`. Auf Wunsch des Nutzers liefen zwei Nachtläufe gleichzeitig.

- **Branch:** `claude/serene-bhabha-1b1ff6`.
- **Worktree:** `.claude/worktrees/gracious-mayer-723dc2`.
- **E2E-Ports:** 5202/5175. Belegt sind 5201/5174 von Lauf A und :5200 vom Heimnetz mit der echten DB.
- **Bereich:** B-602 und B-605, danach freie Wahl. B-607 und B-782 gehören Lauf A.
- **Gebaut und abgenommen:** B-602, B-605, B-603 und B-790. B-790 kam aus der eigenen Suite.
- **Neu angelegt:** B-787 und B-788 (`idee`), außerdem B-790 (gebaut).
- Nichts nach `main` gemergt, nichts gepusht.

## Runde 1 — Einrichtung und Auswahl

- **Worktree:** Der Worktree trug ein eigenes, veraltetes `frontend/node_modules` mit Playwright 1.61.1, der
  Haupt-Checkout hat 1.65.1. Es liegt jetzt beiseite (außerhalb des Repos), an seiner Stelle steht eine Junction auf den
  Haupt-Checkout.
- **Auswahl:** Vor jeder Story wurde ihr Stand auf `main` geprüft, alle drei standen dort auf `geschaetzt`. B-603 kam
  dazu als dritte Story des roten Fadens „flackernde Tests“ (Defekt, M, `geschaetzt`). Weitere `geschaetzt`-Defekte gibt
  es außer B-659 (L) nicht. Alles Übrige ist `Wunsch` und bleibt nach README im Dialog.
- **Rollen:** Keine Rolle ist direkt betroffen. Alle drei Stories ändern nur Testcode und Testläufer-Konfiguration. Der
  Entwickler ist der Kunde, die drei Kundenrollen sind Regressionszeugen.

## PM-Synthese & Priorisierung (→ Entwickler)

| Prio | Story | Größe | Wo | Inhalt |
|---|---|---|---|---|
| P3 | [B-602](backlog/B-602-feldhilfe-ziel-rhythmus-flackert.md) | S | frontend (E2E) | Feldhilfe-Spec klickt, bevor die Trefferliste steht |
| P3 | [B-605](backlog/B-605-e2e-worker-haengt-beim-beenden.md) | S | frontend (Config) | stehender Worker kostet 5 Minuten |
| P3 | [B-603](backlog/B-603-fokus-vitests-flackern-unter-last.md) | M | frontend (Vitest) | Fokus-Zusicherungen prüfen einen Zeitpunkt |

**Roter Faden:** Jede Zusicherung wartet auf den Zustand, den sie meint, und eine rote Probe beweist das Warten.
Mehrere grüne Läufe gelten nicht als Beweis.

## Sprint B1 — Ziel & Umfang

**Sprint-Ziel:** Aus Sicht des Entwicklers fällt keine der drei Stellen mehr durch Zeitplanung. Jede Wartebedingung hat
eine deterministische rote Probe, die ohne sie fällt. Widerlegbar durch eine Probe, die mit entfernter Wartebedingung grün
bleibt.

**Umfang:** B-602, B-605, B-603, alle `geschaetzt`. Ihre Entscheidungen traf stellvertretend der Vorbereitungslauf
2026-11-19, sie sind unbestätigt. Dazu kam B-790 (Defekt XS) aus der eigenen Suite, siehe unten.

**Entwickler-Brief:**

- Kein Produktcode wird geändert.
- **B-602:** Die Wartebedingung steht einmal in `planAnlegen`. Ein Spät-Fall mit verzögerter Suche hält sie ehrlich.
- **B-605:** eine Zeile `PWTEST_CHILD_PROCESS_TIMEOUT ??= "45000"`, geprüft mit einer Wegwerf-Konfiguration außerhalb des Repos.
- **B-603:** Ein lokaler Helfer `erwarteFokus` ersetzt 24 positive Zusicherungen. Probe je Fokus-Aufruf, die Komponenten
  werden per Kopie zurückgespielt und per Blob-Hash geprüft.
- Reihenfolge: B-602 und B-605 parallel, weil die Dateien getrennt sind, danach B-603.

## Iteration B1 — umgesetzt

### B-605 (`04c72b89`)

- **Änderung:** eine Zeile mit Kommentar in `frontend/playwright.config.ts`.
- **Probe:** eine Wegwerf-Konfiguration, die die echte importiert. Sie braucht ein `package.json` mit `"type":"module"`,
  sonst scheitert sie an `import.meta`. Dazu ein worker-skopiertes Fixture, das im Abbau 120 s blockiert.
- **Mit der Zeile:** Meldung nach 45 s.
- **Ohne die Zeile:** nach 97 s noch keine Meldung.
- **Offener Handle statt Blockade:** Der Worker endet sofort, ohne Meldung. Damit ist belegt, dass die Meldung „kein
  Herzschlag“ heißt.

### B-602 (`f79b8fed`)

- **Änderung:** `planAnlegen` wartet auf die Radiogruppe „Übung wählen“. Neuer Spät-Fall mit 1,5 s Verzögerung.
- **Review-Fund 🔴, eingearbeitet:** Der übernommene Glob `exercises?**` griff nie. Beim Erstladen sucht die Seite ohne
  Filter, die URL hat dann kein `?`. Meine erste rote Probe war also nur Latenz.
- **Korrektur:** ein Pfad-Prädikat und ein Trefferzähler, der selbst zugesichert wird.
- **Neu gemessen:** echt grün. Ohne Wartezeile rot an `aria-expanded`. Mit „(N Treffer)“ als Wartebedingung ebenfalls rot.
- **Wiederholung:** `--repeat-each=3` über die Datei, 12/12 grün.

### B-603 (`20ca1ce2`)

- **Änderung:** 16 + 8 Stellen laufen über `erwarteFokus`. Die drei verneinenden behalten ihren Takt und bekommen einen
  Kommentar.
- **Probe je Fokus-Aufruf (8 Aufrufe):** Mit 50 ms Verzögerung ist die neue Form 87/87 grün. Die alte Form fällt bei 7
  von 8 Aufrufen. Entfernt man den Aufruf, fällt die neue Form bei 7 von 8 Aufrufen.
- **Der achte Aufruf** (`SohnPractice.tsx:370`, Fokus nach „leichter“): Er ist durch die E2E-Spec
  `lernkarte-leichter.spec.ts:72` getragen. Gemessen mit einer E2E-Mutante, die Spec fällt an `:72`.
- **Blob-Hash:** nach jeder Probe gleich HEAD.
- **Review 🟢:** Zwei Punkte sind eingearbeitet. Der dritte wurde gemessen. Dabei fanden sich zwei Abhängigkeiten des
  Fokus-Effekts (`action.busy`, `cursor`), die schon vor dieser Story kein Fall trug. Daraus wird **B-788**.
- **Voller Vitest-Lauf:** einmal 1303/1304, danach zweimal 1304/1304. Das Rot lag im zurückgestellten
  `SohnProgress`-Flackern, jetzt mit Fallnamen. Daraus wird **B-787**.
- **Typcheck:** `tsc --noEmit` sauber.

### Volle E2E-Suite (5202/5175, Release)

**Erster Lauf** (Stand: B-602 und B-605 gebaut, 00:35–00:49): 183 passed, 2 failed, 13,6 Minuten. Es kam keine Meldung
„did not exit“.

- **`erstbegegnung.spec.ts:74`:** „3 neue Wörter kennengelernt“ nicht sichtbar. Einzeln lief die Spec `--repeat-each=2`
  zweimal grün. Das ist ein Flackern im vollen Lauf mit geteilter DB, ohne Bezug zum Diff. Eine Story dafür gibt es nicht,
  der Befund steht hier, bis er wiederkehrt.
- **`plan-umhaengen-gesperrt.spec.ts:23`:** **fest rot**, strict mode mit zwei „Bearbeiten“-Knöpfen, einzeln zweimal rot.
  Das Rot liegt schon auf der Basis `9abb6aa6`, also auch auf `main`. Lauf A ist informiert.
  - Daraus wurde **[B-790](backlog/B-790-plan-umhaengen-spec-trifft-zwei-bearbeiten.md)** (Defekt XS), im selben Sprint
    gebaut (`761af289`): Der Knopf wird jetzt über die Kopfzeile des Plans adressiert.
  - Gemessen: alt 3 von 3 rot, neu 3 von 3 grün. Eine Mutante, die eine Tabellenzeile trifft, wird rot.
  - Die Spec war am 21.11. nur durch den Ablauf grün: Der Klick kam, bevor die Positionsliste stand. Das passt zum roten
    Faden, darum nahm der Sprint die Story auf.

**Zweiter Lauf** auf dem Endstand mit B-790 (00:55–01:08): **185/185 passed**, 12,5 Minuten, ohne „did not exit“. Für
B-605 stehen damit 2 von 5 vollen Läufen ohne Hänger.

## Runde 2 — Rollengang Sprint B1

Der Rollengang ist die Maschine: die volle E2E-Suite auf dem Endstand, Server frisch gestartet auf 5202/5175.

- **Entwickler (Kunde) – abgenommen:** „Die drei Stellen fallen nicht mehr durch Zeitplanung. Jede Wartebedingung hat
  eine Probe, die ohne sie fällt, und bei B-602 beweist die Probe jetzt auch, dass die Verzögerung ankam.“ Belege:
  - die Proben je Story (Verlauf der Stories),
  - Vitest 1304/1304 (zweimal), E2E 185/185.
- **Vater – Regressionszeuge:** E2E 185/185, darunter `plan-umhaengen-gesperrt` (B-790) und `feldhilfe`. Im Diff liegt
  kein Produktcode des Vater-Webs.
- **Sohn – Regressionszeuge:** dieselbe Suite, dazu `SohnPractice`/`SohnTest` 87/87. Die Komponenten sind nach allen
  Proben byte-gleich zu HEAD.
- **Creator – Regressionszeuge:** dieselbe Suite. Im Diff liegt keine Creator-Fläche.
- Sprint-Ziel gehalten. Widerlegt wäre es durch eine Probe, die mit entfernter Wartebedingung grün bleibt. Gemessen ist
  das Gegenteil, und zwar für jede Stelle.

## Retrospektive

**Nachschau:** nicht hier, mit Absicht.

- Sprint 109 (B-609) ist ein Sprint der Folge auf `main`.
- Lauf A führt diese Folge im selben Zeitraum fort und hat schon weitere Stories abgenommen (B-607, B-782).
- Hätte Lauf B dieselben Story-Dateien angefasst, gäbe es beim Merge Konflikte, dazu doppelte `nachgeschaut`-Einträge.
- In den gemeinsamen Notizen der Sitzungsgruppe steht das ausdrücklich.
- `backlog-index.sh` lief nicht, weil `INDEX.md` sonst gegen `main` kollidiert. Nach dem Merge einmal neu erzeugen.

**Was die eigenen Tore durchgelassen haben:**

1. **B-602: eine rote Probe, die aus dem falschen Grund rot war.** Die Story hatte das Risiko selbst benannt (Risiko 1:
   „greift die Route doch nicht, besteht der neue Fall auch ohne Wartebedingung“). Die Probe fiel trotzdem rot und sah
   damit gültig aus. Der Grund war natürliche Latenz, die Verzögerung kam nie an. Erst der Review hat das gefunden.
   Das ist dieselbe Klasse wie bei B-202 („rote Probe am richtigen Ort“): Es zählt nicht, *ob* eine Probe fällt,
   sondern *warum*.
2. **B-603: ein Kommentar verspricht ein Rot, das es nie gab** (`SohnTest.test.tsx:416-417`). Diese Lücke stammt nicht aus
   diesem Sprint. Gefunden hat sie die Abhängigkeits-Mutante, die der Review angeregt hat.

**Der eine Mechanismus (Prosa, im Skill):** Eine Probe, die eine Verzögerung oder Umleitung einschleust (`page.route`,
`setTimeout`-Mutante), muss in derselben Messung zeigen, dass die Einschleusung getroffen hat. Möglich sind ein
zugesicherter Zähler oder eine Log-Zeile. Erst dann zählt ein Rot.

Das steht als Satz in `pm-loop` Step 5 neben der Regel zu `mutant-probe.py`. Es ist **kein neues Tor**: Ein Tor
müsste jede `page.route` in Specs auf einen Zähler prüfen. Das wäre vor dem Scharfstellen zu messen (wie viele Routen gibt
es, wie viele haben einen Zähler) und gehört in die Entscheidung des Nutzers. Der Lauf geht darum nach der Retro weiter.

**Bewusst ungedeckt:** Ein Glob, der in einer anderen Spec heute schon nicht greift, bleibt unentdeckt, bis jemand dort
misst. Kandidat ist `planbau-ladefenster.spec.ts:58`: Dort greift er nur, weil vorher ein Filter gesetzt ist.

**Werkzeug-Lehren dieses Laufs:**

- Der Stop-Hook dieser Sitzung startet `dotnet test` im **Haupt**-Checkout. Er läuft, sobald dort `.cs`-Dateien von Lauf A
  von HEAD abweichen, und kollidierte einmal mit dessen Testlauf (MSB3027). Ich habe deshalb lange Wartezeiten über einen
  Monitor überbrückt statt über Turnenden. Lauf A steht es in den Notizen.
- Ein Python-Ersatz mit `"\n"` in einer Datei mit CRLF findet nichts. Die Edit-Werkzeuge schreiben CRLF zurück. Darum
  vergleicht ein Probe-Skript erst nach dem Normalisieren der Zeilenenden.

## Halt nach Sprint B1

Kein neues Tor, und das Inkrement trug keinen Defekt. Ein weiterer Sprint wäre also erlaubt. Der Lauf hält trotzdem,
weil kein Kandidat im Bereich dieses Laufs übrig ist:

- **B-787:** Es fehlt die Fehlermeldung. Ohne sie wäre jede Reparatur geraten, so hält es B-603 in Entscheidung 4 fest.
- **B-788:** Die Frage dahinter ist, ob `action.busy` und `cursor` im Dep-Array überhaupt nötig sind. Gelesen: Beide
  Zustandswechsel fallen in der Regel in denselben Render wie `item` (Batching). Ob es einen Ablauf gibt, in dem nur sie
  wechseln, ist eine Entwurfsfrage am Produktcode und keine Test-Reparatur. Sie gehört ausformuliert, bevor jemand baut.
- **B-659** (Defekt, L, backend) trifft die Serie und die Pflicht. Der parallele Lauf A arbeitet auf `main` in
  benachbarten Flächen (Shop, Termine), und eine Migration auf zwei Branches würde kollidieren. Die Story gehört in einen
  Lauf ohne Parallelbetrieb.
- Alle übrigen offenen Stories sind `Wunsch`/`Frage` oder `idee`. Die bleiben nach README im Dialog.

## Offene Roadmap (priorisiert)  ·  Konkreter Änderungsstand (für Review)

**Offen:**

- [docs/backlog/](backlog/), neu sind [B-787](backlog/B-787-sohnprogress-vitest-flackert-unter-last.md) und
  [B-788](backlog/B-788-fokus-effekt-abhaengigkeiten-ungetragen.md), beide `idee`, Defekt.
- B-605 AK 3 bleibt eine Absichtserklärung. Die Ursache des Hängers vom 2026-11-16 ist offen. Der Zähler für „nicht
  reproduzierbar“ beginnt mit dem vollen Lauf dieses Sprints.

**Commits auf `claude/serene-bhabha-1b1ff6`:**

| Commit | Inhalt |
|---|---|
| `e03f24d8` | Status in-arbeit |
| `04c72b89` | B-605 |
| `f79b8fed` | B-602 |
| `a4901275` | B-787 neu |
| `fad28e9b` | B-788 neu |
| `20ca1ce2` | B-603 |
| `854372e5` | B-790 neu |
| `761af289` | B-790 |
| danach | Abnahme und Protokoll |

**Beim Merge nach `main` beachten:**

- Die Nummern B-787, B-788 und B-790 sind in den Notizen für Lauf B reserviert. Lauf A hat seine B-787 schon auf
  B-789 umnummeriert (`d5f344c4` auf `main`). Nach dem Merge
  `ls docs/backlog | grep -o 'B-[0-9]*' | sort | uniq -d` prüfen.
- `INDEX.md` danach neu erzeugen.
