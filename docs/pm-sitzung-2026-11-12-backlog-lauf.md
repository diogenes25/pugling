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

## Produktentscheidungen dieses Laufs (zum Umstoßen)

1. **Schlusszeichen zählen beim Vergleich nicht** – auch ein *anderes* (`?` statt `.`), auch bei Ordnungszahlen
   (`3` für `3.`). B-457, Entscheidungen 1–4.
2. **16 px für jedes Eingabefeld, auch im Vater-Web** (B-466, 2).
3. **Rundenköpfe brechen am Handy in bis zu drei Zeilen um**, statt Etiketten zu kürzen (B-466, 1).
4. **Der Assistent gibt keiner Liste eine Tagesdosis** – auch keiner geordneten (B-477, 1).
5. **Eine Liste, die der Creator nachträglich auf „beliebige Reihenfolge" stellt, behält ihre Dosis gespeichert, aber
   sie wirkt nicht**; der Creator wird dafür nicht gesperrt (B-478, 2).

## Offene Roadmap

[docs/backlog/](backlog/README.md).
