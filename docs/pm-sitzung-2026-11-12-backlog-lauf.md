---
tags: [typ/protokoll, bereich/pm]
aliases: [Backlog-Lauf 2026-11-12]
---

# PM-Sitzung: Backlog-Lauf nach B-476

**Datum:** 2026-11-12 (Start 2026-11-11 spät)  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Das Backlog abarbeiten, sobald die parallele Sitzung (B-476) fertig ist – Auftrag des Nutzers: „Treffe
Entscheidungen selber nach bestem Wissen und Gewissen."

**Auslegung des Auftrags.** Es gilt das Nachtlauf-Verfahren ([nachtlauf.md](nachtlauf.md), Freigaben 1–10). Der Satz
„Entscheidungen selber treffen" deckt die Defekte und das Aufräumen, die ohnehin autonom grillbar sind, und alle
Ermessensfragen *innerhalb* einer Story. Er wird **nicht** als Freigabe gelesen, `Wunsch`- und `Frage`-Stories ohne
eigene Freigabe-Zeile zu entscheiden: Die Söhne lernen seit dem 2026-11-11 echt auf der App (Root-`CLAUDE.md`, „Fallstricke → EF-Migrationen"), und
Produktrichtung soll nicht nachts entstehen.
Jede Entscheidung, die ich getroffen habe, steht unten „zum Umstoßen".

Gearbeitet auf `auto/fertigstellung` (dort liegt B-476); nichts gepusht.

## Runde 1 — Auswahl

Kein frischer Rollengang mit allen drei Rollen: die parallele Sitzung hatte den Sohn-Sitz gerade gefahren (B-476),
und das Backlog trägt sechs P2-Defekte aus dem echten Prüfgang am Access-Lehrwerk (2026-11-10). Diese wurden von einem
Menschen am laufenden Produkt beobachtet und gehen vor. Zwei davon treffen den Sohn direkt beim Lernen:

- **Sohn (beobachtet, B-457):** „Ich hab's richtig übersetzt und nur den Punkt vergessen – falsch?"
- **Sohn (beobachtet, B-466):** „Am Handy steht der Lückentext als ein Brei da, und ‚Später weiter' ist halb weg."

Die übrigen (B-465, B-471, B-473, B-475) betreffen den KI-Creator bzw. das Lehrwerk-Werkzeug – ein anderes
Themengebiet, darum ein eigener Sprint.

## Sprint 31 — Ziel & Umfang

**Sprint-Ziel:** *Der Sohn bekommt für eine richtige Antwort ohne Schlusszeichen seine Punkte, und er kann einen
Lückentext am Handy Satz für Satz lesen, ohne dass der Ausweg aus dem Bild fällt oder das Feld zoomt.*
**Umfang:** B-457 (XS, backend), B-466 (S, frontend) – beide `art: Defekt`, autonom gegrillt.
**Entwickler-Brief:** Quelle der Wahrheit ist `StageMechanics.Normalize` (der eine Vergleich aller Typen); kein
Typ bekommt eine eigene Regel. Frontend nur CSS/Klassen, kein Vertrag. Guards: Antwort nur aus Satzzeichen bleibt
lösbar; ein Komma im Satz bleibt. Keine Migration. Testweg: `SchlusszeichenTests`, `lueckentext-am-handy.spec.ts`
(375 × 812), volle Suiten.

## Iteration 31 — umgesetzt

- **B-457:** `Normalize` schneidet `. ? ! …` am Ende ab (auch mehrere, auch mit Leerzeichen dazwischen), außer die
  Antwort besteht nur daraus. Gilt damit für Üben, Test, Vorschau, Listen, Zuordnung, Grammatik und das Entdoppeln der
  Auswahlknöpfe. Die Editor-Warnung „Lösung fehlt in der Auswahl" vergleicht im Frontend jetzt genauso.
- **B-466:** Lückentext mit `white-space: pre-line`; Rundenkopf (Test und Übung) bricht um; Eingabefelder 16 px.
- Review-Zähler des Sprints: **3** (B-457 🟡 Listen-AK ungetestet; B-466 🟡 Zusicherung auf den Ausgangszustand;
  🟡 Frontend-Kopie ungetestet) – alle behoben, unter dem Limit 5.

## Runde 2 — Rollengang / Abnahme

Alle Läufe am Stand nach der letzten Änderung, der E2E-Server von Playwright frisch gestartet: Backend **1328/1328**,
Vitest **748/748**, E2E **137 passed** (135 + 2 neue). Mutanten: fünf von fünf rot (Tabellen in B-457 und B-466).

- **Sohn – abgenommen.** Gang als Maschine: `lueckentext-am-handy.spec.ts` bei 375 × 812 – drei Sätze stehen
  untereinander, „Später weiter" liegt auch neben „Frage 1 / 10 · Selbstcheck" im Bild, das Lösungsfeld hat 16 px.
  B-457 über die API-Probe der Vater-Vorschau und den Katalog-Check. *„Den Punkt vergesse ich immer – jetzt zählt's."*
  **Offen für einen Menschen:** ob iOS-Safari bei 16 px wirklich nicht mehr zoomt (nur ein iPhone misst das).
- **Vater – Regressionszeuge.** Seine Specs in der vollen Suite grün; berührt hat der Sprint nur die Editor-Warnung
  „Lösung fehlt in der Auswahl" (jetzt wie der Server) und die Feldgröße (15 → 16 px).
- **Creator – Regressionszeuge.** Katalog- und Vorschau-Tests grün; kein Pfad im Diff außer dem geteilten Vergleich.

Commits: `97daf9f` (B-457), `bc2c18a` (B-466).

## Retrospektive

**Nachschau:** Sprint 29/30 (B-04, B-440, B-451, B-452, B-453, B-454) von einem frischen `pugling-reviewer` an
`595b429`/`1079a96`, je Story mit benanntem Prüfpunkt (in den Stories). **Ein Durchrutscher:** der Assistent schickt
die Tagesdosis auch an ungeordnete Listen → 400 mitten im Anlegen
([B-477](backlog/B-477-assistent-dosis-auf-ungeordneter-liste.md), `entgangen_bei: [B-04]`, P2). Vier kleine Reste in
[B-478](backlog/B-478-nachschau-reste-sprint-29.md). Der B-440-Rest (Frontend-Kopie faltet den Apostroph nicht) ist mit
`97daf9f` schon behoben. `nachgeschaut: 2026-11-12` an allen sechs. Offen bleiben neun weitere, in Dialog-Sitzungen
abgenommene Stories (B-375, B-458, B-460–B-464, B-468, B-472) – nicht Vorsprint dieses Laufs.

**Was die eigenen Tore durchgelassen haben:** zwei 🟡 aus dem Review, beide über *Tests*, nicht über Verhalten.
(1) Eine Zusicherung auf den Ausgangszustand: Die Kopfzeilen-Prüfung im ersten E2E war vor dem Fix grün. Ich hatte das
selbst gemessen und im Verlauf notiert, die Zeile aber stehen lassen. Die Regel steht im Root-`CLAUDE.md`, gegriffen
hat sie erst über den Reviewer – zum fünften Mal gemessen. (2) **Regel-Zwillinge laufen auseinander:**
`normalizeAnswer` im Frontend ist eine Kopie von `StageMechanics.Normalize`. B-440 hat das Original geändert und die
Kopie vergessen (Nachschau), und meine Angleichung war zuerst ungetestet (Review). Beides ist dieselbe Klasse.

**Mechanismus – nur vorgeschlagen (Freigabe 3), nicht gelandet:**
**(CO)** Eine gemeinsame Fallliste für den Antwortvergleich (z. B. `docs/api-examples/answer-normalize.json`:
`given`, `expected`, `equal`), gelesen von einem xUnit-Theory **und** von `exerciseConfig.test.ts`. Ändert jemand nur
eine Seite, fällt die andere. Kosten: eine Datei, zwei Leser. Was es nicht deckt: Zwillinge anderer Regeln (es gibt
weitere, z. B. `gapProblem` im Editor).

**Bewusst ungedeckt:** der iOS-Zoom (Gerät). Und dass ich eine als „vorher grün" gemessene Zusicherung trotzdem
stehen lasse, deckt kein Werkzeug – `mutant-probe.py` misst je Test, nicht je Zusicherung.

**Lauf-Grenzen:** Review-Zähler 3 von 5, kein Tor gelandet → der Lauf geht weiter.

## Sprint 32 — Ziel & Umfang

**Sprint-Ziel:** *Der Vater kann im Assistenten jede Übung wählen, ohne dass das Anlegen mittendrin abbricht, und die
Tagesdosis tut auch nach einer Änderung an Übung oder Pool, was sie verspricht.*
**Umfang:** B-477 (XS, frontend, P2) und B-478 (S, backend, P3, Punkt 3 zurückgestellt) – beide aus der Nachschau von
Sprint 29, `art: Defekt`, autonom gegrillt.
**Entwickler-Brief:** Quelle der Wahrheit für die wirksame Dosis ist eine Stelle (`PositionPlayService.DailyDose`),
die alle vier Leser fragen; das Ablehnen beim Zuweisen (B-04, Entscheidung 9) bleibt. Keine Migration, kein Vertrag.
Testweg: `DosisResteTests`, `wizardFinish.test.ts`, `assistent.spec.ts`, volle Suiten.

## Iteration 32 — umgesetzt

- **B-477:** Der Assistent schickt Listen die Dosis 0 und sagt es am Feld („nicht für Listen") und im Überblick.
- **B-478:** „eingeführt" zählt nur im Pool; die Dosis wirkt nicht, solange eine Liste als Menge wertet (auch wenn der
  Creator sie nach dem Zuweisen umstellt), und die Position bleibt dann bearbeitbar; zwei Kommentare berichtigt.
- Review-Zähler des Sprints: **3** (B-477 🟡 stille Ausnahme in der Oberfläche; B-478 🟡 Position nach dem Umstellen
  nicht speicherbar; 🟡 Grenzfehler `<=` von der Probe nicht gefangen) – alle behoben, unter dem Limit 5.
- Mutanten: elf Zeilen, zehn rot wie erwartet, eine (Review-Sperre) erwartet grün, weil am ersten Tag alles fällig ist
  (Tabellen in B-477 und B-478).

## Runde 3 — Rollengang / Abnahme (Sprint 32)

Am Stand nach der letzten Änderung: Backend **1330/1330**, Vitest **749/749**, E2E **137 passed** – im zweiten Lauf.
Der erste Lauf hatte **136 passed, 1 failed** (`creator-lehrwerk-weg.spec.ts`); die Spec berührt weder Dosis noch
Assistent, einzeln und dreifach parallel grün. Die Ursache ist nicht gemessen – kein Freispruch, nur kein Befund.

- **Vater – abgenommen.** `assistent.spec.ts` fährt den Assistenten bis zum Plan; das Feld sagt „(nicht für Listen)",
  der Überblick „(außer Listen)". *„Ich will einfach alles anhaken können, und er soll es anlegen."* Die Liste im
  Abschluss ist im Vitest belegt, nicht im Browser gefahren.
- **Sohn – abgenommen (über die API).** `DosisResteTests` spielt als Kind: „eingeführt" stimmt nach dem Verkleinern,
  eine umgestellte Liste fragt alle Einträge. *„Wenn da 2 von 5 steht, will ich, dass es 2 sind."*
- **Creator – Regressionszeuge.** Er darf eine zugewiesene Liste weiter umstellen (keine Sperre); seine Specs grün.

Commits: `6d885f5` (B-477), `d98e697` (B-478).

## Retrospektive (Sprint 32)

**Nachschau:** Sprint 31 (B-457, B-466) von einem frischen `frontend-reviewer` an `97daf9f`/`bc2c18a`, je Story mit
benanntem Prüfpunkt. **Ein Durchrutscher:** die Zusicherung „Seite scrollt nicht seitlich" im Kopfzeilen-Test kann nicht
fallen – `.sohn-body` scrollt selbst ([B-479](backlog/B-479-nachschau-reste-sprint-31.md), `entgangen_bei: [B-466,
B-457]`, mit fünf 🟢-Grenzfällen). `nachgeschaut: 2026-11-12` an beiden.

**Was die eigenen Tore durchgelassen haben:** dreimal dieselbe Klasse über zwei Sprints – eine **Zusicherung, die nie
fällt**. In Sprint 31 hat der Reviewer eine aus dem ersten Test geholt; dieselbe Form stand im zweiten Test und kam durch
die Abnahme, weil `mutant-probe.py` je *Test* misst: Die Mutante „Kopf ohne Umbruch" fiel am Knopf-Rand, und die
`scrollWidth`-Zeile daneben ritt unbemerkt mit. In Sprint 32 fand der Reviewer erneut eine Probe ohne Grenzfall (`<=`).
Der Sprint selbst: zwei 🟡 über Verhalten (stille Ausnahme in der Oberfläche, gesperrte Bearbeitung) – beides gefunden
von den Reviewern, beide Male die Folge einer Regeländerung, deren *anderen* Weg ich nicht gegangen war.

**Mechanismus – nur vorgeschlagen (Freigabe 3), nicht gelandet:**
**(CP)** `mutant-probe.py` nimmt statt Testnamen auch **Meldungstexte von Zusicherungen** in `fails` (`expect(…, "die
Seite scrollt nicht seitlich")`): rot zählt dann nur, wenn genau diese Zusicherung fällt. Wer jede Zusicherung mit
eigener Meldung schreibt (in den Playwright-Specs üblich), kann so je Zeile messen, ob sie trägt. Kosten: eine
Erweiterung des Skripts; xUnit-Zusicherungen ohne Meldung bleiben grob.

**Lauf-Grenzen:** Review-Zähler 3 von 5, kein Tor gelandet → der Lauf darf weiter.

## Sprint 33 — Ziel & Umfang

**Sprint-Ziel:** *Der Creator, der für ein Kind einen Lehrplan erzeugen lässt, bekommt ihn im Buch des Kindes und mit
dem neuen Grammatikstoff der Unit zuerst – nicht in irgendeiner Reihe und nicht alphabetisch.*
**Umfang:** B-471 (S), B-473 (S) – beide `art: Defekt`, KI-Creator, autonom gegrillt.
**Entwickler-Brief:** Ablage und Briefing müssen dasselbe Profil meinen; „bestpassend" heißt auf der Reihe eines
Kinder-Buchs (`series_match`), alles andere ist eine Fehlbedienung. Die Markierung „NEU <Label>" der Stoffnotiz ist die
Quelle für „neu"; die Konvention gehört in `FORMAT.md`. Keine Migration, kein Vertrag. Testweg: `CreatorAgentTests`.

## Iteration 33 — umgesetzt

- **B-473:** `create/briefing/plan/exam --child` ohne Ortsangabe legen in der aktuellen Unit des Kinder-Buchs ab; ohne
  Profil zum Buch oder bei zwei gleich guten Büchern meldet der Agent eine Fehlbedienung mit Liste.
- **B-471:** Das Briefing nennt die Grammatik-Themen „neu zuerst"; ein Lehrplan mit zwei Lückentexten übt damit
  question tags und past progressive statt zweier Wiederholungsthemen.
- Review-Zähler des Sprints: **2** (🟡 das geratene Regal über Profile ohne Buch-Bezug; 🟡 Präfix-Label) – behoben.
- Mutanten: neun von neun rot (Tabellen in B-471 und B-473). Commit `e94aa1e` (gemeinsam, geteilte Testdatei).

## Runde 4 — Rollengang / Abnahme (Sprint 33)

Backend **1335/1335** am Stand nach der letzten Änderung; das Frontend ist seit dem E2E-Lauf von Sprint 32
unverändert (137 passed).

- **Creator – abgenommen, ohne Live-Lauf.** `CreatorAgentTests` fährt die Konsole mit einem Schein-Modell gegen die
  In-Process-API: die Übung landet in Unit 2 des Buchs, ein Kind ohne Buch-Profil und eines mit zwei Büchern bekommen
  eine Fehlbedienung, das Briefing nennt den neuen Stoff zuerst. *„Wenn ich nur --child sage, will ich sein Buch –
  und lieber eine Rückfrage als eine falsche Reihe."* **Nicht gefahren:** ein echter Lauf gegen Ollama und die API –
  die echte DB gehört den Kindern, und ein zweiter Server auf 5200 ist verboten.
- **Vater – Regressionszeuge.** Lehrpläne aus dem Agenten entstehen weiter inaktiv; kein Pfad im Vater-Web berührt.
- **Sohn – Regressionszeuge.** Kein Spielpfad im Diff.

## Retrospektive (Sprint 33)

**Nachschau:** Sprint 32 (B-477, B-478) von einem frischen `pugling-reviewer` an `6d885f5`/`d98e697`, je Story mit
benanntem Prüfpunkt. **Kein Durchrutscher**; drei 🟢 (unwirksame Dosis wird angezeigt, Überblick bei nur Listen,
`useLeitner:false` per API) in [B-480](backlog/B-480-nachschau-reste-sprint-32.md). `nachgeschaut: 2026-11-12` an beiden.

**Was die eigenen Tore durchgelassen haben:** Mein erster Fix für B-473 nahm „bestpassendes Profil" wörtlich und hat
nicht gelesen, was `profiles/match` liefert – nämlich jedes Profil, das nach Klassenstufe und Schulart passt, auch mit
0 Punkten. Damit wäre das geratene Regal nur verschoben worden. Die Regel dafür gibt es schon (README, „Ausformulieren
heißt gegen den Code belegen": eine Entscheidung, die sich auf eine andere Größe beruft, belegt sie an deren
Schreiber); ich habe sie beim Ausformulieren nicht angewandt, der Reviewer hat es gefunden.

**Mechanismus:** keiner neu. Die Regel steht da, und ihr Netz (der Reviewer) hat gehalten; ein Tor „jede Entscheidung
nennt `Datei:Zeile` ihrer Fremdgröße" wäre Prosa-Prüfung ohne Maschine. Bewusst ungedeckt: der Live-Lauf des Agenten.

## Abschluss des Laufs

Drei Sprints, sechs Stories abgenommen (B-457, B-466, B-477, B-478, B-471, B-473), Review-Zähler 3 / 3 / 2. Kein Tor
gelandet; zwei Vorschläge (CO), (CP) liegen zur Entscheidung. Der Lauf endet hier aus eigenem Ermessen, nicht an einem
Limit: die übrigen P2 sind `Frage`/`Wunsch` ohne Freigabe-Zeile (B-469, B-207, B-208, B-39, B-423 …) oder brauchen eine
Nachbesprechung (B-465: welche KI-Lückentext-Mängel sind Regel, welche Prüfung durch den Vater), B-475 betrifft das
Werkzeug unter `.claude/skills`.

### Liste für den Menschen

- **iPhone:** zoomt der Lückentext-Test mit 16-px-Feldern beim Antippen noch? (B-466)
- **Access-Notizen:** zwei NEU-Marker greifen nicht, weil sie das Label anders nennen („NEU conditional 2" gegen
  „Conditional sentences (if-clauses)", „NEU the passive" gegen „Passive"); Daten unter `raw/` und in der echten DB –
  nicht angefasst. (B-471)
- **KI-Creator live:** `pugling-creator briefing --child 1 --type Cloze` gegen eine Kopie der DB – die Ablage muss im
  Access-Buch stehen. (B-473)
- **Ausrollen:** die Änderungen wirken bei den Kindern erst nach einem Neustart des Heimnetz-Servers.
- **Vorschläge (CO), (CP)** aus den Retros: gemeinsame Fallliste für den Antwortvergleich; `mutant-probe.py` je
  Zusicherungsmeldung.
- Ein roter E2E-Lauf von `creator-lehrwerk-weg.spec.ts` (Sprint 32) ist unerklärt; einzeln und 3× parallel grün.
- **Vorschlag (CQ)** aus Sprint 34: automatischer Test für das Verhalten von `laden.py`.
- **Vorschlag (CR)** aus Sprint 35: bei einem Review-Fund erst alle Leser derselben Größe suchen.
- **Nächster Kandidat B-483** (P2, Durchrutscher aus B-475) – in Sprint 36 abgenommen.
- **Nächster Kandidat B-484** (P2, Durchrutscher aus B-481) – in Sprint 37 abgenommen.
- **Nächster Kandidat B-487** (P2, Durchrutscher aus B-483) – in Sprint 38 abgenommen.
- **Nächster Kandidat B-488** (P2, Durchrutscher aus B-484) – in Sprint 39 abgenommen.
- **Nächster Kandidat B-491** (P2, Durchrutscher aus B-487) – wartet, siehe „Halt nach diesem Sprint" (Sprint 39);
  daneben B-490 (P2, Auswahlfrage ohne Antwort).
- **Vorschlag (CS)** aus Sprint 38: die Regel an die Senke legen, nicht an die Quellen.
- **Vorschläge entschieden** (nach Sprint 39): (CR)+(CS) im Skill gelandet; (CQ) → B-492, (CO) → B-493, (CP) → B-494.

## Sprint 34 — Ziel & Umfang

Fortsetzung nach der Rückmeldung des Nutzers („Ja, weiter").

**Sprint-Ziel:** *Wer ein Lehrwerk mit `laden.py` lädt, erfährt vor dem ersten Aufruf von jedem Feld, das der Server
ablehnen würde, und nach jedem Lauf, ob alle Übungen des Ordners ihre eigenen Lösungen bestehen – auch nach einem
Abbruch.*
**Umfang:** B-475 (S) – `art: Defekt`, Werkzeug, autonom gegrillt.
**Entwickler-Brief:** Quelle der Wahrheit sind die Config-Typen in `Pugling.Contracts` und die Route jedes
Controllers; der Lader spiegelt sie, ein xUnit-Test hält den Spiegel. Keine Migration, kein Vertrag. Testweg:
`LadenFieldListTests` plus echter Lauf gegen einen Wegwerf-Server.

## Iteration 34 — umgesetzt

- **B-475:** `--pruefen` lehnt unbekannte Felder je Typ ab (jede Tiefe, Groß/Klein wie der Server, Abkürzungen nur
  exakt); der Selbsttest läuft über alle Übungen des Ordners; `FORMAT.md` nennt die Felder je Typ. Die offene
  Apostroph-Frage ist gemessen: `qu’il` = `qu'il` auch bei Regelaufgaben.
- Review-Zähler des Sprints: **3** (🟡 `Markup` groß geschrieben; 🟡 `decoding[]` ungeprüft; 🟡 Route → Typ von Hand
  gepflegt) – behoben. Mutanten: sechs von sechs rot; das Python-Verhalten nur von Hand gegengeprüft.

## Runde 5 — Rollengang / Abnahme (Sprint 34)

Backend **1338/1338** am Stand nach der letzten Änderung; Frontend unverändert.

- **Creator – abgenommen, echter Lauf.** `laden.py` gegen einen Wegwerf-Server (Port 5280, frische DB): Lauf 1 legt
  Wortliste und Regelaufgabe an, Lauf 2 mit einer weiteren Übung prüft alle drei; ein Ordner mit `alternatives` an
  einer Regelaufgabe stoppt mit Pfad und Feldliste. *„Ein grüner Lauf heißt jetzt: alles im Ordner besteht – nicht
  nur das, was diesmal neu war."*
- **Vater, Sohn – Regressionszeugen.** Kein Pfad im Diff; Suite grün.

## Retrospektive (Sprint 34)

**Nachschau:** Sprint 33 (B-471, B-473) von einem frischen `pugling-reviewer` an `e94aa1e`. **Ein Durchrutscher bei
B-473:** `--child` mit `--series` rät weiter die Unit, und zwei Fächer des Kindes ohne exakten Gleichstand wählen
still; dazu ein Test, dessen rote Messung an der DB-Belegung hing → [B-481](backlog/B-481-ki-creator-raet-unit-und-fach-beim-kind.md)
(P2, `entgangen_bei: B-473`). Drei 🟢 → [B-482](backlog/B-482-nachschau-reste-sprint-33.md). `nachgeschaut:
2026-11-12` an beiden.

**Was die eigenen Tore durchgelassen haben:** Der B-473-Durchrutscher ist dieselbe Fehlerklasse wie der Fund im
Review von Sprint 33 – ich habe den Fall der Story („ganz ohne Ortsangabe") geschlossen und nicht die Nachbarfälle
desselben Begriffs („geratenes Regal"). In Sprint 34 hat das eigene Tor (der Drift-Test) meine erste Feldliste sofort
korrigiert (`refs`); das Review fand die Stellen, an denen zwei Leser derselben Felder verschiedene Regeln hatten
(`unknown_fields` ohne Groß/Klein, `build_config` exakt).

**Mechanismus:** keiner gelandet. Vorschlag **(CQ)**: `laden.py` hat keinen automatischen Test, nur das Tor über
seine Feldliste; sein Verhalten (Selbsttest über alle, Feldprüfung) ist in diesem Sprint von Hand gegengeprüft. Ein
kleiner Python-Testlauf in CI würde das tragen – ein neues Tor, darum nur vorgeschlagen.

## Sprint 35 — Ziel & Umfang

**Sprint-Ziel:** *Wer dem KI-Creator ein Kind nennt, bekommt die Übung in der Unit, aus der ihr Stoff kommt, und
eine Rückfrage statt eines geratenen Fachs, sobald das Kind mehrere Bücher mit Profil hat.*
**Umfang:** B-481 (S) – `art: Defekt`, Durchrutscher aus B-473, autonom gegrillt.
**Entwickler-Brief:** eine Stelle nach dem Auflösen der Reihe statt drei Zweige; „mehrere Bücher" zählt verschiedene
Reihen unter den `series_match`-Treffern. Keine Migration, kein Vertrag. Testweg: `CreatorAgentTests`.

## Iteration 35 — umgesetzt

- **B-481:** Unit ohne `--series-unit` = `--unit`, sonst die aktuelle des Kindes in der Reihe, sonst die erste;
  zwei Bücher mit Profil sind eine Fehlbedienung, auch mit verschiedenen Punkten; ein fremdes Kind bekommt weiter die
  Fehlbedienung des Briefings.
- Review-Zähler des Sprints: **2** (🟡 roher 403/404; 🟡 `--unit` überstimmt) – behoben. Mutanten: sechs von sechs rot.

## Runde 6 — Rollengang / Abnahme (Sprint 35)

Backend **1343/1343** am Stand nach der letzten Änderung; Frontend unverändert.

- **Creator – abgenommen, ohne Live-Lauf.** `CreatorAgentTests` fährt die Konsole gegen die In-Process-API:
  `--series` und `--profile` landen bei der Unit des Kindes, `--unit` geht vor, zwei Bücher verlangen die Wahl.
  *„Theodor hat Englisch und Latein – dann will ich gefragt werden, nicht überrascht."* **Nicht gefahren:** Ollama
  und die echte DB.
- **Vater, Sohn – Regressionszeugen.** Kein Pfad im Diff; Suite grün.

## Retrospektive (Sprint 35)

**Nachschau:** Sprint 34 (B-475) von einem frischen `pugling-reviewer` an `c53291c`. **Ein Durchrutscher:** `check()`
und `build_config` lesen dieselben Felder mit verschiedenen Regeln (`Alternatives` groß geschrieben geht verloren,
`distractors` ohne `markup` besteht die Prüfung), und Birkenbihl fällt im Selbsttest seit B-475 bei jedem Lauf durch
→ [B-483](backlog/B-483-laden-prueft-was-build-config-liest-nicht.md) (P2, `entgangen_bei: B-475`), samt sechs 🟢.
`nachgeschaut: 2026-11-12` an B-475.

**Was die eigenen Tore durchgelassen haben:** Zum zweiten Mal in Folge findet die Nachschau einen Durchrutscher
derselben Form: der Review-Fund wurde als *Fall* behoben, nicht als *Klasse* (B-473: „ohne Ortsangabe" geschlossen,
die Nachbarn mit `--series`/`--unit` offen; B-475: `Markup` exakt, `Alternatives` nicht). Auch das Review dieses
Sprints fand wieder einen Nachbarn (`--unit`). Die Regel dafür steht im README („ein geänderter geteilter Zustand
bekommt eine Zeile je *Aktion*, gefunden per `grep`") – ich habe nach den *Optionen* gegriffen, die die Story nennt,
statt nach allen Lesern des geänderten Werts.

**Mechanismus:** keiner gelandet. Vorschlag **(CR)**: bei einem Review-Fund vor der Reparatur `grep` nach allen
Lesern derselben Größe und das Ergebnis als Zeile in den `## Verlauf` – Prosa-Disziplin; ein Tor dafür sähe ich nur
als Feld im Review-Auftrag. Bewusst ungedeckt: der Live-Lauf des Agenten.

**Halt nach diesem Sprint:** Der Lauf erzeugt seine Arbeit inzwischen selbst – jede Nachschau findet in meinem
eigenen vorigen Sprint einen Durchrutscher, und der nächste Kandidat (B-483) ist wieder einer. Weiterlaufen hieße,
die Qualitätsschwelle am eigenen Nachlauf zu messen; ich höre hier auf und lege das vor.

## Sprint 36 — Ziel & Umfang

Fortsetzung auf Zuruf des Nutzers („Ja, B-483") nach dem Halt von Sprint 35.

**Sprint-Ziel:** *Wer einen Lehrwerk-Ordner lädt, bekommt am Server genau, was `--pruefen` bestanden hat, und ein
Selbsttest, der nur dort grün ist, wo er wirklich etwas geprüft hat.*
**Umfang:** B-483 (S) – `art: Defekt`, Durchrutscher aus B-475, autonom gegrillt.
**Entwickler-Brief:** erst `grep` nach jedem Leser eines Config-Felds (Vorschlag CR an sich selbst angewandt), dann
den zweiten Leser wegnehmen statt ihm eine dritte Regel beizubringen. Kein Backend, keine Migration.

## Iteration 36 — umgesetzt

- **B-483:** `build_config` liest nur noch `markup`/`distractors` (die Schlusszeichen-Varianten sind seit B-457
  überflüssig); `check()` lehnt doppelte Schreibweisen, `distractors` ohne `markup`, falsche Formen der Abkürzungen
  und eine Abkürzung neben einem Feld ab, das sie selbst bestimmt; der Selbsttest prüft auf einer getippten Stufe
  des Typs und vermerkt, wo es keine gibt.
- Review-Zähler des Sprints: **3** – behoben. Mutanten: elf von elf rot, eine Zeile als tragend-los entfernt.

## Runde 7 — Rollengang / Abnahme (Sprint 36)

- **Creator – abgenommen.** Lader gegen einen Wegwerf-Server nach der letzten Änderung: Birkenbihl, Übersetzung mit
  `Alternatives`, Lückentext und eine auf Selbsteinschätzung gestellte Vokabelübung, alle grün, nur Birkenbihl mit
  Vermerk; der alte Lader verlor am selben Ordner `Where's the station?` und meldete Birkenbihl mit 0 %. Beide
  echten Ordner bestehen `--pruefen`. *„Was durch die Prüfung geht, kommt so an – und grün heißt geprüft."*
- **Vater, Sohn – Regressionszeugen.** Kein Backend im Diff; `LadenFieldListTests` 6/6.

## Retrospektive (Sprint 36)

**Nachschau:** Sprint 35 (B-481) von einem frischen `pugling-reviewer` an `d941ca1`. **Ein Durchrutscher, zum
dritten Mal in Folge derselben Form:** die Unit wählt jetzt eine Stelle, Profil und Reihe ohne Profilreihe aber
weiter zwei (`BriefingBuilder.ResolveProfileAsync` nach Punkten; werkunabhängiges Profil → erstes Regal)
→ [B-484](backlog/B-484-ki-creator-profil-und-reihe-aus-zwei-pfaden.md) (P2, `entgangen_bei: B-481`).
`nachgeschaut: 2026-11-12` an B-481.

**Was die eigenen Tore durchgelassen haben:** In diesem Sprint habe ich (CR) vorab angewandt – `grep` nach allen
Lesern eines Config-Felds – und es hat die Hälfte getragen: der zweite Leser fiel weg. Das Review fand trotzdem drei
🟡, alle in Lesern, die keine *Felder*, sondern *Formen und Zustände* lesen (Typ eines Werts, Stufe der Übung).
Die Klasse ist also nicht „wer liest das Feld", sondern „wer entscheidet über dieselbe Größe" – bei B-481 genauso
(Unit an einer Stelle, Profil an zwei).

**Mechanismus:** keiner gelandet; Vorschlag (CR) geschärft: vor der Reparatur *jede Entscheidung über dieselbe Größe*
suchen, nicht nur ihre Leser. Bewusst ungedeckt: der Selbsttest spielt Lückentexte nicht auf der Wortbank.

**Halt nach diesem Sprint**, aus demselben Grund wie nach Sprint 35: der nächste Kandidat (B-484) ist wieder ein
Durchrutscher aus meinem eigenen vorigen Sprint.

## Sprint 37 — Ziel & Umfang

Fortsetzung auf Zuruf des Nutzers („Ja, B-484").

**Sprint-Ziel:** *Wer dem KI-Creator ein Kind und einen Ort nennt, bekommt den Lehrer und den Stoff genau dieses
Werks – nie den eines anderen, nur weil der mehr Punkte hat.*
**Umfang:** B-484 (M) – `art: Defekt`, Durchrutscher aus B-481, autonom gegrillt.
**Entwickler-Brief:** erst jede *Entscheidung* über Reihe, Unit und Profil suchen (geschärftes CR), dann die zweite
Stelle wegnehmen: das Briefing liest den Stoff aus dem Auftrag. Keine Migration, kein Vertrag.

## Iteration 37 — umgesetzt

- **B-484:** `ResolveMaterialAsync` entfällt, Werk und Unit sind `MaterialFacts` in jedem Briefing; ein ungenanntes
  Profil nur auf der Zielreihe oder ohne Reihe; ein werkunabhängiges `--profile` nimmt das Buch des Kindes im Fach;
  `--profile` fremder Reihe neben `--series` und zwei verschiedene Units sind Fehlbedienungen.
- Review-Zähler des Sprints: **4** – behoben. Mutanten: 15 von 15 rot (ein erster Lauf wegen einer parallelen
  Sitzung verworfen).
- **Parallele Sitzung:** auf demselben Branch committete eine zweite Sitzung (B-485, B-486). Ihr Test-Tor lief in meine
  Mutanten-Probe; ich habe danach jede Probe im selben Zug abgewartet und nur meine Dateien committet.

## Runde 8 — Rollengang / Abnahme (Sprint 37)

Volle Suite **1354/1354** nach der letzten Änderung.

- **Creator – abgenommen, ohne Live-Lauf.** `CreatorAgentTests` fährt die Konsole gegen die In-Process-API: mit Ort
  spricht der Lehrer dieses Buchs, ohne ihn ein werkunabhängiger, nie der fremde; Stoff und Quelle sind immer das
  Werk der Ablage. *„Ich nenne das Buch – dann will ich auch dessen Lehrer und dessen Stoff."* **Nicht gefahren:**
  Ollama und die echte DB.
- **Vater, Sohn – Regressionszeugen.** Kein Pfad im Diff; Suite grün.

## Retrospektive (Sprint 37)

**Nachschau:** Sprint 36 (B-483) von einem frischen `pugling-reviewer` an `4a2e170`. **Ein Durchrutscher, zum vierten
Mal in Folge:** B-483 verbot die Wortbank neben `markup`, weil ihr eine Lösung fehlen kann – der zweite Weg zu einer
Wortbank (`text`/`gaps`/`wordBank` von Hand), den `FORMAT.md` selbst empfiehlt, bleibt offen
→ [B-487](backlog/B-487-laden-wortbank-ohne-loesung.md) (P2, `entgangen_bei: B-483`). `nachgeschaut` an B-483.

**Was die eigenen Tore durchgelassen haben:** Die Suche nach jeder Entscheidung über dieselbe Größe (CR, geschärft)
hat in diesem Sprint vorab fünf Stellen gefunden – und eine davon falsch eingeschätzt (`PrimaryTextbook` „legt nichts
ab"; tatsächlich speist es die Quelle der Übung). Das Review fand sie. Und B-487 zeigt dieselbe Form wie zuvor: eine
Regel an *einem* Weg zum Ergebnis, nicht an *allen*. Die Lücke ist also nicht das Suchen, sondern das Einordnen
dessen, was die Suche findet – geprüft wird dort nur von Hand.

**Mechanismus:** keiner gelandet. Bewusst ungedeckt: der Live-Lauf des Agenten; die Wortbank im Selbsttest des Laders.

**Halt nach diesem Sprint**, wie nach Sprint 35 und 36: der nächste Kandidat (B-487) ist wieder ein Durchrutscher aus
meinem eigenen vorigen Sprint. Vier Nachschauen in Folge mit einem Fund heißen: das Verfahren findet sie zuverlässig,
aber erst eine Runde zu spät; wer weitermachen lässt, bekommt je Sprint einen kleineren Rest.

## Sprint 38 — Ziel & Umfang

Fortsetzung auf Zuruf des Nutzers („Ja, B-487").

**Sprint-Ziel:** *Der Sohn kann jede Lücke auf der Wortbank-Stufe lösen – gleich, wer die Bank wie geschrieben hat.*
**Umfang:** B-487 (S) – `art: Defekt`, Durchrutscher aus B-483, autonom gegrillt.
**Entwickler-Brief:** diesmal nicht die Schreibwege zählen, sondern die Stelle suchen, durch die *jede* Bank zum Kind
geht (`Choices` über `CardFacets`), und die Regel dorthin legen; „passend" entscheidet der `AnswerGrader`. Lader-Reste
mitnehmen. Keine Migration, kein Vertrag.

## Iteration 38 — umgesetzt

- **B-487:** der Pool der Wortbank-Stufe enthält für jede Lücke eine Kachel, die die Bewertung annimmt – fehlt sie, wird
  die Lösung ergänzt, für alle Lücken (gleicher Pool je Karte). Lader: `distractors` nur als nicht-leere Liste, kein
  Ablenker gleich einer anderen Schreibweise, Kacheln nach der Bewertungsregel zusammengelegt.
- Kopie der echten DB: keine Lückentext-Übung mit Wortbank – nichts zu reparieren.
- Review-Zähler: **0**. Mutanten 7/7 (C#) und 8/8 (Lader) rot; zwei Läufe wegen des Test-Tors der parallelen Sitzung
  verworfen.

## Runde 9 — Rollengang / Abnahme (Sprint 38)

Volle Suite **1380/1380** nach der letzten Änderung.

- **Sohn – abgenommen als Maschinengang.** `ClozePlayTests` holt die Karten der Wortbank-Stufe über die Route des Kindes:
  eine Bank ohne die Lösung bietet sie trotzdem an, auf jeder Karte dieselben Kacheln. *„Wenn das Wort nicht dabei
  ist, kann ich's nicht wählen – jetzt ist es immer dabei."* **Nicht gefahren:** Browser, echte DB.
- **Creator – abgenommen (Lader offline).** `check()` lehnt leere und `null`-Ablenker und einen Ablenker ab, der eine
  zweite richtige Kachel wäre.
- **Vater – Regressionszeuge.** Kein Pfad im Diff; Suite grün.

## Retrospektive (Sprint 38)

**Nachschau:** Sprint 37 (B-484) von einem frischen `pugling-reviewer` an `1c90c8d`. **Ein Durchrutscher, zum fünften
Mal in Folge:** das Fach eines werkunabhängigen Profils prüft nur der abgeleitete Pfad, nicht der genannte Ort – und
die neue Fehlermeldung schickt genau dorthin → [B-488](backlog/B-488-ki-creator-profil-fach-neben-genannter-reihe.md)
(P2, `entgangen_bei: B-484`). `nachgeschaut` an B-484.

**Was die eigenen Tore durchgelassen haben:** B-484 hat (CR) angewandt und die Stellen *gefunden* – aber eine Regel an
den Pfaden verteilt, die es gab. B-488 ist wieder eine Regel an einem von zwei Pfaden. In diesem Sprint ging es anders:
statt die Schreibwege der Wortbank zu zählen (vier, plus der Speicher als fünfter), sitzt die Regel an der einen
Stelle, durch die jede Bank zum Kind geht. Das Review fand dort kein 🟡.

**Mechanismus:** keiner gelandet. Vorschlag **(CS)** zum Dialog: vor einer Regel die *Senke* suchen – die eine
Stelle, durch die jedes Ergebnis geht – und die Regel dort ablegen statt an den Quellen; wo es keine Senke gibt
(B-488: zwei Pfade zum Request), erst eine schaffen. Kein Tor, weil „Senke" sich nicht greppen lässt.

**Halt nach diesem Sprint**, wie nach Sprint 35–37: der nächste Kandidat (B-488) ist wieder ein Durchrutscher aus
meinem eigenen vorigen Sprint.

## Sprint 39 — Ziel & Umfang

Fortsetzung auf Zuruf des Nutzers („Ja, B-488").

**Sprint-Ziel:** *Wer dem KI-Creator ein Profil und einen Ort nennt, die nicht zusammenpassen, bekommt vor dem ersten
Modellaufruf eine Fehlbedienung – auf jedem Befehl und jedem direkten Aufruf.*
**Umfang:** B-488 (S) – `art: Defekt`, Durchrutscher aus B-484, autonom gegrillt.
**Entwickler-Brief:** (CS) anwenden: die Senke ist das Briefing (`create`, `plan`, `exam` und direkte Aufrufe passieren
es vor dem ersten Schreiben). Dort *eine* Funktion für „Profil passt zu Reihe und Fach", benannt wie ungenannt; die
zweite Prüfung in der Konsole entfällt. Keine Migration, kein Vertrag.

## Iteration 39 — umgesetzt

- **B-488:** `ProfileFit` im Briefing; das Fach der Übung ist das der Reihe; ein genannter Ort wird nicht mehr in der
  Profilreihe gesucht (Rest der Nachschau). Der B-308-Test fährt die Planer jetzt direkt, weil die Konsole früher
  abbricht.
- Review-Zähler: **2** (Meldung mit einem Ausweg, den es nicht gibt; veraltete Summary) – behoben.
- Mutanten 8/8 rot; ein Lauf verworfen (drei Zeilen ohne Compilerfehler „kompiliert nicht", eine falsche Erwartung).
- **Nummernkollision:** die parallele Sitzung vergab B-489 in derselben Zeit; meine Story aus der Nachschau heißt
  darum B-491.

## Runde 10 — Rollengang / Abnahme (Sprint 39)

Volle Suite **1385/1385** nach der letzten Änderung.

- **Creator – abgenommen, ohne Live-Lauf.** `CreatorAgentTests`: Latein-Profil neben Englisch-Reihe ist auf `create`,
  `exam` und `plan` eine Fehlbedienung ohne Modellaufruf; die Meldung nennt beide Fächer und einen Ausweg, den es gibt.
  *„Ich hab das falsche Profil erwischt – und er sagt mir, welches Fach die Reihe hat."* **Nicht gefahren:** Ollama,
  echte DB.
- **Vater, Sohn – Regressionszeugen.** Kein Pfad im Diff; Suite grün.

## Retrospektive (Sprint 39)

**Nachschau:** Sprint 38 (B-487) von einem frischen `pugling-reviewer` an `9d648bb`. **Ein Durchrutscher, zum sechsten
Mal in Folge** – aber ein anderer als zuvor: die Senke war richtig gewählt (der Reviewer bestätigt: jede Bank geht durch
`Choices`), *innerhalb* der Senke fehlte eine Eingangsbereinigung. Leere Einträge zählen als Bank und bleiben als
Kachel; eine Bank nur aus ihnen verrät die Lösung → [B-491](backlog/B-491-wortbank-aus-leeren-kacheln.md)
(P2, `entgangen_bei: B-487`). Nachbarklasse ohne Bezug zu B-487: [B-490](backlog/B-490-auswahl-ohne-richtige-antwort.md)
(Auswahlfragen ohne die Antwort). `nachgeschaut` an B-487.

**Was die eigenen Tore durchgelassen haben:** (CS) hat in diesem Sprint getragen – das Review fand keinen Weg an der
Senke vorbei, nur Meldungs- und Doku-🟡. Was durchrutscht, ist jetzt eine Stufe kleiner: nicht mehr „ein Weg ohne
Regel", sondern „ein Randwert des Eingangs an der richtigen Stelle". Den hätte ein Fall „leer/Leerzeichen" je neuer
Eingabeliste gefunden; das Vater-Web und `ChoicesOf` filtern ihn, ich habe nicht nachgesehen, wie es die Nachbarn tun.

**Mechanismus:** keiner gelandet. (CS) bestätigt sich als Vorschlag; ergänzt um: an der Senke die Bereinigung des
Eingangs von den Nachbarn derselben Art übernehmen (hier `ChoicesOf`), nicht neu erfinden.

**Halt nach diesem Sprint**, wie nach Sprint 35–38: der nächste Kandidat (B-491) ist wieder ein Durchrutscher aus
meinem eigenen vorigen Sprint.

## Entscheidung über die Retro-Vorschläge (Nutzer, nach Sprint 39)

Auf „Erst Vorschläge klären" hat der Nutzer entschieden:

- **(CR)+(CS) – ja, in den pm-loop-Skill.** Gelandet als Absatz „Put a rule at the sink, not at the sources" in
  Schritt 4 und als Satz im Review-Auftrag von Schritt 5
  ([.claude/skills/pm-loop/SKILL.md](../.claude/skills/pm-loop/SKILL.md)).
- **(CQ) – ja, in CI** → [B-492](backlog/B-492-lader-test-in-ci.md).
- **(CO) – ja, mit drei Lesern** (Server, Frontend, Lader) → [B-493](backlog/B-493-fallliste-antwortvergleich.md);
  baut auf B-492.
- **(CP) – später** → [B-494](backlog/B-494-mutantenprobe-je-zusicherung.md) (P3).

B-492 und B-493 sind neue Tore: je ein eigener Sprint, danach Halt, damit der Nutzer sie sieht.

## Produktentscheidungen dieses Laufs (zum Umstoßen)

1. **Schlusszeichen zählen beim Vergleich nicht** – auch ein *anderes* (`?` statt `.`), auch bei Ordnungszahlen
   (`3` für `3.`). B-457, Entscheidungen 1–4.
2. **16 px für jedes Eingabefeld, auch im Vater-Web** (B-466, 2).
3. **Rundenköpfe brechen am Handy in bis zu drei Zeilen um**, statt Etiketten zu kürzen (B-466, 1).
4. **Der Assistent gibt keiner Liste eine Tagesdosis** – auch keiner geordneten (B-477, 1).
5. **Eine Liste, die der Creator nachträglich auf „beliebige Reihenfolge" stellt, behält ihre Dosis gespeichert, aber
   sie wirkt nicht**; der Creator wird dafür nicht gesperrt (B-478, 2).
6. **Neuer Grammatikstoff ist, was die Stoffnotiz mit „NEU <Label>" markiert** – kein Feld am Thema (B-471, 1).
7. **`--child` ohne Ortsangabe: Profil auf einem Kinder-Buch oder Fehlbedienung**, nie die erste Reihe (B-473, 1–2).
8. **Der Lader prüft Felder offline gegen einen Spiegel des Vertrags**, nicht zur Laufzeit gegen Swagger; der
   Selbsttest läuft über alle Übungen des Ordners statt über eine Buchführung der Abbrüche (B-475, 1–2).
9. **Zwei Bücher des Kindes mit Profil verlangen `--profile`**, auch wenn die Punkte verschieden sind; `--unit` legt
   die Übung dort ab, wo ihr Stoff herkommt (B-481, 1–2).
10. **Der Lader schreibt keine Lösungsvarianten mehr dazu**, und neben `markup` gibt es keine eigene Wortbank (B-483,
    1 und 4).
11. **Ein ungenanntes Profil spricht nur auf der Zielreihe oder ohne Reihe**; Stoff und Quelle sind immer das Werk
    der Ablage, das freie Kinderbuch nie (B-484, 2 und 5).
12. **Eine Wortbank ohne Lösung wird beim Ausspielen ergänzt, nicht beim Schreiben abgelehnt** (B-487, 1 und 2).
13. **Das Fach einer KI-Übung ist das ihrer Reihe; ob ein Profil passt, entscheidet nur das Briefing** (B-488, 1–3).

## Offene Roadmap

[docs/backlog/](backlog/README.md). Aus diesem Lauf neu: B-479, B-480 (P3-Reste der Nachschau). Vorschlag für den nächsten Lauf: B-475 (P2, Werkzeug) und B-465 nach einem kurzen Dialog über die Regeln; danach die P3-Defekte.
