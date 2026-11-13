---
tags: [typ/story, status/geschaetzt, bereich/werkzeug, rolle/creator]
aliases: [kacheln.ps1 Drehung, Fotos auf dem Kopf, Übersichtsbogen]
status: geschaetzt
prio: P2
art: Wunsch
groesse: S
wo: ".claude/skills/lehrwerk-aufbereiten"
migration: nein
vertragsbruch: nein
quelle: Lauf des Skills lehrwerk-aufbereiten an Découvertes Série bleue 2 (2026-11-11)
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-474 · `kacheln.ps1` kann nicht drehen, und für die Sichtung von 56 Fotos fehlt ein Übersichtsbogen

Beim zweiten Buch lag das Buch bei 13 von 56 Fotos auf dem Kopf oder quer (Inhaltsverzeichnis, Unit-Seiten). Das
Handy schreibt dafür keine EXIF-Drehung, weil es flach über dem Tisch gehalten wurde; `kacheln.ps1` wertet nur
EXIF aus und liefert kopfstehende Kacheln. Umgangen mit einem Pillow-Einzeiler, der gedrehte Kopien nach
`gedreht/` schreibt und erst dann kachelt.

Zweiter Fund im selben Lauf: Schritt 1 des Skills („jedes Foto einmal ansehen“) kostet bei 56 Fotos 56 volle
Bilder. Ein Übersichtsbogen (12 Miniaturen je Blatt, mit Nummer beschriftet) hat für Seitenart und Drehung
gereicht – fünf Bilder statt 56.

Dritter Fund: Im Blitzlicht sind die obersten zwei bis fünf Zeilen jeder linken Vokabelseite blass. Eine
Ausschnitt-Vergrößerung (Bereich angeben, zweifach hochskaliert) hat jede dieser Zeilen geklärt; die
3-Zeilen-Kachel allein nicht.

Naheliegende Abhilfe: `-Drehung 0|90|180|270`, `-Uebersicht` (Kontaktbogen) und `-Ausschnitt x,y,b,h` in
`kacheln.ps1`, dazu je ein Satz in `SKILL.md` Schritt 1.

## User Story

Als **Creator, der ein Lehrwerk aus Fotos aufbereitet** (meist Claude im Skill `lehrwerk-aufbereiten`), möchte ich
Fotos von Hand drehen, einen Stapel als Übersichtsbogen sichten und einen blassen Bereich vergrößern können, damit ich
ohne Hilfsskripte aus dem Stegreif und mit einem Bruchteil der Bilder zu lesbaren Kacheln komme.

## Ist-Stand am Code (2026-11-13, nachgeprüft)

- `.claude/skills/lehrwerk-aufbereiten/kacheln.ps1` nimmt `Quelle`, `Ziel`, `Zeilen`, `Spalten`, `Ueberlappung`,
  `MaxKante` (Zeilen 11-20) und dreht nur nach EXIF-Tag 0x0112 (Zeilen 36-42). Ein Parameter für eine Drehung von Hand
  fehlt – genau die Lücke beim flach fotografierten Buch. (Die erste Ausformulierung nannte 11-19 und 36-39.)
- Laden, EXIF-Drehung und JPEG-Encoder (Zeilen 29-42) sind schon da und wären für Bogen und Ausschnitt dieselben.
- `SKILL.md` Schritt 1 „Bestand aufnehmen" (Zeilen 28-41) verlangt „jedes Foto einmal ansehen" (Zeile 33) und nennt den
  Kachel-Aufruf (Zeile 38). Einen Übersichtsbogen oder eine Ausschnitt-Vergrößerung gibt es im Skill nicht.

## Die echte Lücke

Drei fehlende Bildoperationen, die alle auf demselben geladenen und EXIF-gedrehten Bild arbeiten: Drehen von Hand,
Kontaktbogen über einen Ordner, vergrößerter Ausschnitt. Heute ersetzt sie ein Wegwerf-Einzeiler je Lauf.

## Offene Punkte

1. ~~Drehen: ein Parameter `-Drehung 90|180|270` je Aufruf, oder eine Liste `datei=grad` für gemischte Stapel?~~ → Entscheidung 1.
2. ~~Übersichtsbogen und Ausschnitt als eigene Skripte neben `kacheln.ps1` oder als Schalter desselben?~~ → Entscheidung 2.

## Entscheidungen

1. **Ein Parameter `-Drehung 0|90|180|270` je Aufruf** (`ValidateSet`, Vorgabe 0), angewandt **nach** der EXIF-Drehung,
   im Uhrzeigersinn. Begründung: der Bogen zeigt, welche Fotos gedreht sind; die kachelt man dann je Datei bzw. als
   Unterordner mit einem Aufruf – eine Mini-Sprache `datei=grad` wäre Parser-Code für einen seltenen Fall. Kosten: ein
   gemischter Stapel braucht mehrere Aufrufe.
2. **Schalter desselben Skripts**, als zwei Parameter-Sets: `-Uebersicht` (Kontaktbogen: 4×3 Miniaturen je Blatt, je
   Miniatur Dateiname bzw. laufende Nummer eingeblendet, Blätter `uebersicht-<n>.jpg`) und `-Ausschnitt x,y,b,h`
   (Bruchteile 0–1 des gedrehten Bilds, zweifach hochskaliert, gekappt durch `MaxKante`, Datei `<name>-ausschnitt.jpg`).
   Begründung: Laden, EXIF, Drehung und Encoder sind identisch; zwei Skripte hießen drei Kopien davon. Bruchteile statt
   Pixel, weil die Kacheln/Bögen verkleinert gesehen werden und Pixel des Originals dort nicht ablesbar sind. Kosten: das
   Skript wächst von 62 auf grob 130 Zeilen.
3. **Ziel bleibt unter `raw/`** (Kommentar Zeile 9) – Bögen und Ausschnitte sind Buchausschnitte wie die Kacheln.
   Kosten: keine.

Alle drei: Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13,
[Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

## Akzeptanzkriterien

1. `-Drehung 180` auf ein Bild mit rotem Pixel oben links liefert eine Kachel (`-Zeilen 1`) mit rotem Pixel unten
   rechts. *Rot macht ihn:* der Parameter wird ignoriert oder vor der EXIF-Drehung angewandt.
2. `-Drehung 90` auf ein 200×100-Bild liefert eine 100×200-Kachel. *Rot macht ihn:* falscher `RotateFlipType`.
3. Ohne `-Drehung` bleiben die Kacheln eines Bilds byte-gleich zu heute (gleiche Maße, gleiche Dateinamen).
   *Rot macht ihn:* eine Vorgabe ≠ 0 oder ein geänderter Ausgabename.
4. `-Uebersicht` über einen Ordner mit 13 Bildern schreibt genau zwei Blätter `uebersicht-1.jpg`, `uebersicht-2.jpg`.
   *Rot macht ihn:* falsche Blatt-Rechnung (12 je Blatt).
5. `-Ausschnitt 0,0,0.5,0.25` auf ein 400×400-Bild liefert ein 400×200-Bild (200×100 doppelt). *Rot macht ihn:*
   Pixel statt Bruchteile oder fehlende Hochskalierung.
6. `SKILL.md` Schritt 1 nennt Bogen, Drehung und Ausschnitt je mit einem Aufruf-Beispiel. *Rot macht ihn:* nur Prüfung
   per Lesen (Doku) – ausdrücklich als Absichtserklärung, kein Tor.

## Schätzung

**Größe: S** — ein PowerShell-Skript um zwei Parameter-Sets und einen Parameter erweitern, drei Sätze in `SKILL.md`.

- **Risiken:** `System.Drawing` unter `pwsh` ist Windows-only (wie heute schon); Text in den Bogen zeichnen braucht eine
  vorhandene Schrift (`Arial`/`Segoe UI`, sonst `GenericSansSerif`). Kein Repo-Code, keine CI-Abdeckung – der Testweg
  ist ein Befehl.
- **Angriffsplan:** (1) `-Drehung` (kleinster Schritt, sofort nützlich); (2) gemeinsames Laden+Drehen in eine Funktion
  ziehen; (3) `-Ausschnitt`; (4) `-Uebersicht`; (5) `SKILL.md` Schritt 1. Kein Backend betroffen.
- **Testweg (reproduzierbarer Befehl, Windows, aus dem Repo-Root):** erzeugt Testbilder in `$env:TEMP`, ruft das Skript
  und prüft Maße und Pixel; jede Zeile `False` ist ein roter Fall.

  ```powershell
  pwsh -NoProfile -Command {
    Add-Type -AssemblyName System.Drawing
    $d = Join-Path $env:TEMP "b474-$(Get-Random)"; New-Item -ItemType Directory $d, "$d/stapel" | Out-Null
    $b = New-Object System.Drawing.Bitmap 200, 100; $b.SetPixel(0, 0, [System.Drawing.Color]::Red); $b.Save("$d/a.png"); $b.Dispose()
    $s = '.claude/skills/lehrwerk-aufbereiten/kacheln.ps1'
    & $s -Quelle "$d/a.png" -Ziel "$d/r180" -Zeilen 1 -Drehung 180 | Out-Null
    $k = [System.Drawing.Bitmap]::FromFile("$d/r180/a-r1c1.jpg"); ($k.GetPixel($k.Width-1, $k.Height-1).R -gt 200); $k.Dispose()   # AK 1
    & $s -Quelle "$d/a.png" -Ziel "$d/r90" -Zeilen 1 -Drehung 90 | Out-Null
    $k = [System.Drawing.Image]::FromFile("$d/r90/a-r1c1.jpg"); ($k.Width -eq 100 -and $k.Height -eq 200); $k.Dispose()          # AK 2
    1..13 | ForEach-Object { Copy-Item "$d/a.png" "$d/stapel/$_.png" }
    & $s -Quelle "$d/stapel" -Ziel "$d/bogen" -Uebersicht | Out-Null
    ((Get-ChildItem "$d/bogen" -Filter 'uebersicht-*.jpg').Count -eq 2)                                                          # AK 4
    $q = New-Object System.Drawing.Bitmap 400, 400; $q.Save("$d/q.png"); $q.Dispose()
    & $s -Quelle "$d/q.png" -Ziel "$d/aus" -Ausschnitt 0,0,0.5,0.25 | Out-Null
    $k = [System.Drawing.Image]::FromFile("$d/aus/q-ausschnitt.jpg"); ($k.Width -eq 400 -and $k.Height -eq 200); $k.Dispose()   # AK 5
  }
  ```

  AK 3 per Vergleich der Kachel-Hashes (`Get-FileHash`) eines Fotos vor und nach der Änderung (alter Stand per
  `git show HEAD:.claude/skills/lehrwerk-aufbereiten/kacheln.ps1`). Danach ein kurzer Lauf des Skills an zwei echten
  Fotos unter `raw/` als Rollengang.

## Verlauf

- **2026-11-11** — angelegt (Lauf an Découvertes Série bleue 2).
- **2026-11-13** — ausformuliert (Tageslauf, letzte Minuten): Ist-Stand am Skript belegt, zwei Schnitt-Fragen.
- **2026-11-13** — Ist-Stand nachgeprüft (Zeilen 11-20 und 36-42 berichtigt, `SKILL.md`-Stellen ergänzt), User Story und
  „Die echte Lücke" ergänzt; `unverifiziert` entfernt (Backlog-Lauf).
- **2026-11-13** — gegrillt: `-Drehung` je Aufruf, Bogen und Ausschnitt als Schalter desselben Skripts, Ziel unter `raw/`.
  Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13,
  [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).
- **2026-11-13** — geschaetzt: S, Werkzeug (`.claude/skills/lehrwerk-aufbereiten`), keine Migration, kein Vertragsbruch.
