---
tags: [typ/story, status/abgenommen, bereich/katalog, rolle/creator]
aliases: [Foto zu Wortliste, Vokabelseiten einlesen, Lehrwerk-Import, lehrwerk-aufbereiten]
status: abgenommen
prio: P1
art: Wunsch
groesse: S
wo: "doku"
migration: nein
vertragsbruch: nein
quelle: Rollengang am Access-Lehrwerk 2026-11-10 (Kopie der pugling.db, Sohn im Handy-Format 375×812, Vater-Web)
wartet_auf: ""
nachgeschaut: ""
grund: ""
ersetzt_durch: []
---

# B-470 · Aus Fotos der Vokabelseiten wird eine Wortliste – als wiederholbares Werkzeug statt als Handarbeit unter raw/

**Befund zur Machbarkeit:** Ja, aus Buchfotos entsteht brauchbarer Lernstoff. Aus 53 Fotos von *English G
Access G9, Band 3* wurden Stoffnotizen für fünf Units, 124 Wortpaare der Unit 1 (TSV mit Abschnitt und
Hinweis), ein Fachlehrer-Profil und elf Start-Übungen; jede besteht ihren Selbsttest. Die von Hand
geschriebenen Übungen (question tags, past progressive, unregelmäßige Verben, Wegbeschreibung) treffen den
Stoff der Unit und sind die besten im Bestand.

## User Story

Als Vater möchte ich die Seiten eines neuen Schulbuchs fotografieren und daraus in einer Sitzung Stoffnotizen,
geprüfte Wortlisten und Start-Übungen bekommen, damit der KI-Creator für jedes Buch mit den echten Wörtern
arbeitet und ich nicht für jedes Buch ein eigenes Ladeskript brauche.

## Ist-Stand am Code

- `fill --images` (B-375) schreibt nur Stoffnotizen und scheitert mit dem lokalen Modell an dichten Seiten
  ([B-459](B-459-vision-modell-fuer-buchseiten.md)).
- Access lag als buchspezifisches `inhalt.py` + `laden.py` unter `raw/english/access-pugling/` (gitignored,
  ohne Prüfung): Jedes weitere Buch hätte ein neues Skript gebraucht.
- Wortpaare sind der Teil, bei dem ein Lesefehler teuer ist (ein falsches Paar wird wochenlang gelernt) und den
  das Programm nicht aus dem Kopf des Modells nehmen darf (B-460, Entscheidung 2).

## Entscheidungen

1. **Die Seiten liest Claude (Opus) in einer Sitzung, nicht das lokale Vision-Modell.** Entscheidung des Nutzers
   am 2026-11-11: Dass die Fotos dafür ins Netz gehen, ist zu vernachlässigen. Kosten: Das Lesen ist kein
   Knopf in der App, sondern eine Sitzung mit dem Skill.
2. **Werkzeug als Skill** `.claude/skills/lehrwerk-aufbereiten/` (committet, ohne Buchinhalt): `SKILL.md` (Ablauf
   und Regeln), `FORMAT.md` (Datenformat), `laden.py` (buchunabhängig: prüfen, laden, Selbsttest),
   `kacheln.ps1` (dichte Seiten in überlappende Ausschnitte), `PROMPT.md` (Startprompt für eine frische Sitzung).
   Die Buchdaten bleiben unter `raw/` (Urheberrecht).
3. **Unsicher gelesen ist ein Fehler, kein Hinweis.** Eine TSV-Zeile mit gefüllter Spalte `unsicher` stoppt das
   Laden. Raten ist damit nicht stiller Normalfall, sondern muss am Foto oder vom Nutzer geklärt werden.
4. **Erst Wegwerf-DB, dann die Instanz des Nutzers**, und vor der zweiten ein Halt mit der Liste der Funde.
5. **Kein Durchsichts-UI im Vater-Web.** Die Durchsicht geschieht in der Sitzung; das Web zeigt danach die
   angelegten Übungen wie jede andere.

## Akzeptanzkriterien

1. `laden.py` lädt jedes Buch im Format aus `FORMAT.md`, ohne buchspezifischen Code, und ist idempotent.
2. Eine unsicher markierte Zeile, ein fehlendes Pflichtfeld oder ein unbekanntes Grammatik-Thema stoppen das
   Laden mit Fundort.
3. Access lässt sich aus dem neuen Format in eine frische DB laden, mit demselben Ergebnis wie vorher.
4. Ein Buch in einer anderen Sprache (Akzente im Namen, neues Grammatik-Thema) lädt und findet sich beim
   zweiten Lauf wieder.
5. Ein erster echter Lauf an einem neuen Buch in einer frischen Sitzung mit `PROMPT.md`.

## Schätzung

S: ein Skill aus Python- und PowerShell-Skript plus Doku, kein Produktcode, keine Migration. Testweg: Wegwerf-DB
auf Port 5280 (frischer Seed), Access und ein synthetisches französisches Testbuch.

## Verlauf

- **2026-11-10** — angelegt (Prüfgang am Access-Lehrwerk: Übungen, Vater-Sicht, Handy).
- **2026-11-11** — Nutzerentscheidung (Entscheidung 1) und gebaut. Belege, Wegwerf-DB auf Port 5280:
  - **AK 3:** Access wurde nach `buch.json` + `vokabeln-unit1.tsv` (neue Spalten) + `uebungen.json` überführt; die
    alten Dateien liegen unter `raw/english/access-pugling/alt/`. Frisch geladen: Reihe 5, Units 7–11, Profil 1,
    Übungen 13–23 (dieselben Ids wie im echten Bestand), jede 100 % im Selbsttest. Kind 1 steht auf Access
    Unit 1, Klassenarbeit 1 hat 4 Wortlisten. Zweiter Lauf: 11 × „schon da“. Die Lückentexte kommen aus der
    Klammer-Schreibweise identisch an.
  - **AK 4:** „Testwerk Série bleue 9“ (Französisch, eigene Testdaten): Slug wie auf dem Server
    (`slugify` bildet `InterestSlug.From` nach, vorher hätte „Série“ die Reihe bei jedem Lauf neu angelegt).
    Vorhandenes Thema per Label und Slug, neues angelegt, 100 % im Selbsttest, zweiter Lauf ohne neue Reihe.
    `--ersetzen` zog eine geänderte Übersetzung nach.
  - **AK 2:** Eine Zeile mit `unsicher` = „Akzent unleserlich“ stoppte mit Zeilennummer und Exit 1.
  - **Briefing:** `briefing --child 1 --profile <P> --series-unit <U> --unit <U>` zeigt für Access und das
    Testbuch die Wortliste der Unit. Ohne diese Angaben griff der Agent zur falschen Reihe:
    [B-473](B-473-ki-creator-ablage-ignoriert-buch-des-kindes.md).
  - `kacheln.ps1` an einem Access-Foto: zwei aufrechte Kacheln (1800 × 1392, 1800 × 1296), EXIF-Drehung beachtet.
  - Offen ist AK 5, deshalb `wartet_auf`.
- **2026-11-13** — **abgenommen.** AK 5 belegt:
  - **Der Lauf:** Am 2026-11-11 (14:34–14:52, laut Zeitstempel der Dateien) hat eine eigene Sitzung *Découvertes Série
    bleue 2* aus 56 Fotos aufbereitet. Ergebnis in `raw/france/decouvertes-serie-bleue-2-pugling/`: 9 Units, 127 Wörter
    in 3 Listen, 28 Start-Übungen. Die Funde am Werkzeug sind
    [B-474](B-474-kacheln-ohne-drehung-und-uebersicht.md) (Drehen, Übersichtsbogen, Ausschnitt; ausformuliert) und
    [B-475](B-475-laden-abbruch-laesst-uebungen-ungetestet.md) (abgenommen, `c53291c`). Dass die Sitzung genau
    `PROMPT.md` benutzt hat, ist nicht protokolliert. Belegt sind nur der Lauf, sein Ergebnis und seine Funde.
  - **Nachmessung mit dem heutigen Werkzeug** (nach B-475/B-483): `--pruefen` bestanden. Gegen einen
    Wegwerf-Server auf Port 5280 mit frischer DB (nicht 5200, nicht die echte DB) war Lauf 1 grün, alle Übungen 100 %
    im Selbsttest. In Lauf 2 und Lauf 3 standen alle 34 Übungen schon da (34 × 100 %), nichts wurde neu angelegt.
    Das Briefing `--profile 1 --series-unit 8 --unit 8` zeigt Unité 1 mit der Wortliste der Unit und den vorhandenen
    Übungen. Der Wächter `LadenFieldListTests` ist grün (3/3). Danach wurden der Server beendet und die DB gelöscht.
  - **Rollengang (Creator):** die drei Ladeläufe gegen die laufende Wegwerf-App plus das Briefing. Einen Rollengang
    als Sohn gab es nicht, weil die Story kein Produktcode ist. Reviewer: keiner (`wo: doku`).
  - **Commits:** `fa97950` (Skill), `85eab19` (Bytecode-Cache).
  - **Nicht Teil dieser Story und nicht geprüft:** ob das Buch schon in der echten Instanz steht (der Lauf hält davor
    an und fragt). Außerdem tragen Profil und Kind im Lauf „Klasse 9“, während `PROMPT.md` als Beispiel „Klasse 7“
    nennt (ob das Absicht war, steht so in `buch.json`: Band 2, in Klasse 9 eingesetzt).
