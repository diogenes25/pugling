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

## Offene Roadmap

[docs/backlog/](backlog/README.md). Aus diesem Lauf neu: B-479, B-480 (P3-Reste der Nachschau). Vorschlag für den nächsten Lauf: B-475 (P2, Werkzeug) und B-465 nach einem kurzen Dialog über die Regeln; danach die P3-Defekte.
