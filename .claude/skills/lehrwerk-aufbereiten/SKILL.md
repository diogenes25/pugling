---
name: lehrwerk-aufbereiten
description: Ein Schulbuch beliebigen Fachs (Mathe, Latein, Englisch, Französisch, …) aus Fotos seiner Seiten aufbereiten – Claude liest jedes Foto selbst und schreibt je Foto eine einheitliche, mit Obsidian-Tags (Buch, Fach, Stufe, Kapitel, Seitentyp, Thema, Grammatik) katalogisierte Markdown-Datei, die spätere LLMs statt des Fotos lesen; md_pruefen.py prüft Vollständigkeit und Form und erzeugt den Index. Für Fremdsprachen geht es weiter in die App – Stoffnotizen, Wortlisten je Unit und Start-Übungen, laden.py prüft und lädt über die API, das Kind wird auf das Buch umgestellt. Nutze dies, wenn Buchfotos in Markdown sollen („Fotos in Markdown“, „Buch katalogisieren“, „Lehrbuch-Fotos auswerten“) oder ein neues Lehrwerk, eine weitere Unit oder Vokabelseiten in die App sollen („bereite das Buch auf“, „neue Seiten fotografiert“, „Vokabelseiten einlesen“, „Lehrwerk aus Fotos“). Nicht für reinen Text als Quelle (das ist `fill` im Skill ki-creator).
---

# Lehrwerk aus Fotos aufbereiten

**Claude liest die Seiten** (Read-Tool auf die Fotos), nicht das lokale Vision-Modell des Agenten: Das ist an
dichten Seiten gescheitert (B-459). Dass die Fotos dabei an das Modell gehen, hat der Nutzer ausdrücklich
erlaubt (B-470). Alles Weitere ist deterministisch: `md_pruefen.py` prüft das Seiten-Markdown, `laden.py`
prüft, lädt und testet; Formate in [MARKDOWN.md](MARKDOWN.md) und [FORMAT.md](FORMAT.md).

**Zwei Ausbaustufen.** Schritt 1 (Seiten-Markdown) gilt für **jedes** Fach und ist für sich ein fertiges
Ergebnis: Jedes Foto wird zu einer getaggten Markdown-Datei, die jedes spätere LLM liest, statt das Foto zu
analysieren. Die Schritte 2–7 (in die App laden) setzen auf diesem Markdown auf und gelten für **Fremdsprachen**;
für andere Fächer (Mathematik) endet der Lauf nach Schritt 1, solange der Nutzer nichts anderes sagt.

## Regeln, die nicht verhandelbar sind

1. **Urheberrecht.** In die App kommen Fakten, Wortpaare, Themen und Namen von Grammatik-Themen. **Nie**
   Aufgaben, Lesetexte, Dialoge oder Beispielsätze des Buchs; Regeln stehen in eigenen Worten, Beispiele sind
   eigene. Fotos, Kacheln und der Datenordner bleiben unter `raw/` (gitignored) und werden nie committet.
   Das **Seiten-Markdown** (Schritt 1) ist private Arbeitsnotiz unter `raw/`: dort steht Lernstoff wie
   Redemittel und Grammatik-Beispielsätze genau (MARKDOWN.md regelt, was genau und was nur beschrieben wird) –
   in die **App** gelangt davon trotzdem nur, was diese Regel erlaubt.
2. **Ein Wortpaar wird gelesen, nicht vermutet.** Ein falsches Paar lernt das Kind wochenlang. Was nicht sicher
   lesbar ist, bekommt in der Spalte `unsicher` einen Grund, statt geraten zu werden; `laden.py` lädt eine Unit
   mit unsicheren Zeilen nicht. Erst eine Kachel (`kacheln.ps1`) oder für ein einzelnes blasses Zeichen einen
   Ausschnitt (`vergleich.ps1` ohne `-Neu`) versuchen, dann den Nutzer fragen.
3. **Die PIN kommt vom Nutzer** und steht nie in einer Datei: `PUGLING_PIN` als Umgebungsvariable oder `--pin`.
   Nicht raten; ein `401 invalid_credentials` heißt nachfragen.
4. **Erst Wegwerf-DB, dann echte.** Gegen die Instanz des Nutzers wird erst geladen, wenn der Lauf gegen eine
   frische DB grün war und der Nutzer die Liste der Funde gesehen hat.

## Ablauf

### 1. Seiten nach Markdown (jedes Fach)

- Fotos liegen unter `raw/<fach>/<werk>/` oder direkt unter `raw/<fach>/` (vom Nutzer abgelegt,
  schreibgeschützt). Arbeitsordner: `raw/<fach>/<werk-slug>-pugling/`, das Markdown in dessen `md/`. Gibt es
  `md/` schon, ist das ein **Nachtrag**: nur Fotos ohne Seitendatei bearbeiten (das Prüfskript nennt sie),
  vorhandene Dateien nicht neu schreiben.
- Format, Pflichtfelder, Tag-Schema und was genau bzw. nur beschrieben wird: **[MARKDOWN.md](MARKDOWN.md)** –
  vor dem ersten Foto ganz lesen. Kurz: **ein Foto = eine Datei**, gleiche Frontmatter und die fünf
  Abschnitte Überblick · Seitenelemente · Fakten · Aufgaben · Unsicher in jeder Datei.
- Reihenfolge: zuerst Umschlag und Inhaltsverzeichnis (daraus `_buch.md` mit Kapitelliste und dem
  verbindlichen Tag-Vokabular unter `## Tags`), dann die Fotos in Dateinamen-Reihenfolge. **Kein
  Inhaltsverzeichnis fotografiert:** nur die belegten Kapitel aufnehmen (Kapitelauftakt, Kopfzeilen), die übrigen
  als eine Zeile „weitere Kapitel: unbekannt (kein Inhaltsverzeichnis fotografiert)“; die Stichprobe nimmt dann
  einen Kapitelauftakt oder Rückblick statt des Inhaltsverzeichnisses.
- Der Ordner-Slug (`access-pugling`) und der Buch-Tag (`buch/english-g-access-g9-3`) dürfen verschieden sein;
  der Tag benennt das Buch eindeutig (Reihe, Band, Region).
- **Überblick zuerst:** Kontaktbögen des ganzen Fotoordners (8 Vorschaubilder je Bogen mit Dateinamen) zeigen
  Ausrichtung, Seitenart und Lücken, bevor ein Foto gelesen wird – und dem Koordinator, wie er Blöcke schneidet:

  ```bash
  pwsh -NoProfile -File .claude/skills/lehrwerk-aufbereiten/kontaktbogen.ps1 -Quelle "raw/<fach>/<werk>" -Ziel "raw/<fach>/<werk-slug>-pugling/kacheln/uebersicht"
  ```

- **Drehung je Foto**, nicht pauschal: jedes Foto zuerst ganz ansehen (EXIF und Hoch-/Querformat sagen
  nichts Verlässliches). Text auf dem Kopf → `-Drehen 180`; Zeilen laufen von oben nach unten → `-Drehen 270`;
  von unten nach oben → `-Drehen 90`. Den Wert in `drehung` festhalten. `kacheln.ps1` dreht und kachelt in
  einem Lauf; eine gedrehte Einzelkopie zum Überblick gibt `-Zeilen 1`.
- Dichte oder kleine Schrift (Vokabelteil, Grammatikteil, Inhaltsverzeichnis, Aufgabenseiten) **immer** in
  Kacheln lesen. Richtwerte für ein 3000×4000-Foto einer Doppelseite: Lehrtext/Aufgaben `-Zeilen 2 -Spalten 2`,
  Grammatik/Inhaltsverzeichnis `-Zeilen 3 -Spalten 2`, sehr dichte Seiten `-Zeilen 3 -Spalten 3`. Vokabelteil und
  Wortlisten in **Streifen** je Buchseite: `-Spalten 2 -Zeilen 8` (Wort und Übersetzung bleiben in einer Zeile;
  ein Raster mitten durch eine Listenspalte zerreißt sie). Für Lautschrift notfalls `-Zeilen 12`:

  ```bash
  pwsh -NoProfile -File .claude/skills/lehrwerk-aufbereiten/kacheln.ps1 -Quelle "raw/<fach>/<werk>/IMG_x.jpg" -Ziel "raw/<fach>/<werk-slug>-pugling/kacheln" -Zeilen 3 -Spalten 2 -Drehen 180
  ```

  Kacheln überlappen 8 %, eine angeschnittene Zeile steht in der Nachbarkachel ganz. Der Name trägt Raster und
  Drehung (`IMG_x-3x2-d180-r1c1.jpg`), ein feineres Raster überschreibt also kein gröberes. Kacheln bleiben
  unter `raw/`.
- **Lichtreflex** (Bücher in Schutzfolie) ist die häufigste Ursache für Unlesbares; Kacheln helfen dagegen
  nicht. Die Stelle sofort als `**unleserlich**` eintragen und im Abschlussbericht ein neues Foto vorschlagen.
- **Vollständig heißt:** jedes Element der Seite steht unter „Seitenelemente“, jeder Fakt unter „Fakten“, jede
  Aufgabe unter „Aufgaben“ – auch Übungs-, Methoden- und Anhangseiten. Nichts wird übersprungen, weil es für
  die App gerade nicht gebraucht wird; ein späteres LLM soll das Foto nie mehr öffnen müssen.
- Was nach Kachel nicht sicher lesbar ist, kommt unter „Unsicher“ (mit bester Lesart), nie geraten in die Fakten.
- Prüfen, bis keine `FEHLER` mehr kommen, dann den Index schreiben:

  ```bash
  python -I -X utf8 .claude/skills/lehrwerk-aufbereiten/md_pruefen.py "raw/<fach>/<werk-slug>-pugling/md" --fotos "raw/<fach>/<werk>" --index
  ```

  Jeder `Hinweis` wird angesehen (kurze Datei = übersprungen? schlechte Lesbarkeit ohne Unsicher-Eintrag?).
  Teillauf (nur ein Fotoblock): `--nur <erstes-foto.jpg> <letztes-foto.jpg>` erwartet nur diese Fotos;
  `--index` schreibt erst der Lauf über alles. Dateien nach einem älteren Format: zuerst
  `md_migrieren.py <md-ordner>` (fünfte Aufgabenspalte, Tag-Vokabular aus den vorhandenen Tags), den Rest meldet
  das Prüfskript.
- Das Skript sieht nur die Form; zum Schluss **drei Seitendateien gegen ihr Foto** vergleichen – je eine der
  vorhandenen Seitenarten Vokabel-/Merkseite, Aufgabenseite, Inhaltsverzeichnis (fehlt eine Art im Block:
  Register, Formentabelle oder Kapitelauftakt) – dafür die Kacheln **neu öffnen**, nicht aus der Erinnerung
  prüfen. Fehlt ein Element oder steht ein Fakt falsch, die ganze Seitenart nachbessern.
- Nach dem letzten Fotoblock `## Abdeckung` in `_buch.md` schreiben (fotografierte Seiten, Lücken, Seiten zum
  Neufotografieren) und das Tag-Vokabular auf Dubletten durchsehen. Wer dem Nutzer eine Fotoliste gibt, nennt
  je Seite zum Neufotografieren **Seiten, Grund und den Dateinamen des alten Fotos** – sonst weiß er nicht, welches
  Foto weg kann. Tipp für ihn: Schutzfolie ab oder Buch schräg zum Licht, Fußzeile mit Seitenzahl im Bild.
- Viele Seiten: auf Subagenten verteilen – nach **Dichte**, nicht nach Fotozahl: ein Block hat höchstens ~8
  dichte Doppelseiten (Vokabel-, Grammatik-, Aufgabenseiten) oder ~15 lockere (Lektion, Einstieg, Umschlag). Je
  Agent ein zusammenhängender Fotoblock mit ausdrücklich genanntem erstem und letztem Dateinamen (aus dem
  Kontaktbogen abzählen, nicht schätzen). Briefing-Vorlage – jedes Feld ausfüllen, keines offen lassen:

  ```text
  Skill lehrwerk-aufbereiten, NUR Schritt 1. Lies SKILL.md (Regeln + Schritt 1) und MARKDOWN.md ganz.
  Buch: <Titel> · Fotos: <Ordner> · Dein Block: <erstes.jpg> bis einschließlich <letztes.jpg> (<n> Fotos)
  Ausgabe: <…-pugling/md/> · Kacheln: <…-pugling/kacheln/> · _buch.md lesen, nicht ändern (Kapitel, Aufbau, Tags)
  Prüfen: md_pruefen.py <md> --fotos <Ordner> --nur <erstes.jpg> <letztes.jpg> bis 0 FEHLER
  Bericht: Prüfzeile, Dateien mit seitentyp, unsichere Stellen, benutzte neue Tags, Unklarheiten im Skill.
  ```

  Der
  **Koordinator** schreibt vorher `_buch.md` mit Kapitelliste und einem großzügig aus dem Inhaltsverzeichnis
  vorbefüllten Tag-Vokabular, `## Abdeckung` vorläufig („vorläufig, nach Kontaktbogen“). Parallele Agenten
  ändern `_buch.md` **nicht**: braucht einer einen neuen Tag, nimmt er ihn trotzdem und nennt ihn im Bericht;
  der Koordinator trägt ihn nach (oder führt ihn mit einem vorhandenen zusammen), schreibt `## Abdeckung`
  final und lässt das Prüfskript über den ganzen Ordner laufen. Nacheinander laufende Agenten dürfen
  `_buch.md` selbst ergänzen.

#### Neue Fotos zu einem schon erfassten Buch

Neue Seiten (Lücken) laufen wie oben. Für **neu fotografierte** Seiten (Lichtreflex, Anschnitt) gilt
(erprobt 2026-11-17 an Access 3: 12 Doppelseiten, unsichere Stellen 77 → 58):

1. **Zuordnen:** nur die neuen Fotos in den Scratchpad kopieren und per `kontaktbogen.ps1` den Seiten zuordnen;
   Doppelaufnahmen derselben Seite erkennen. Der Nutzer legt Fotos auch in neue Unterordner – Pfad nachsehen.
2. **Vergleichen statt annehmen:** je Seite `vergleich.ps1 -Alt <alt> -Neu <neu> [-DrehNeu …]` (oben alt, unten
   neu), im Grenzfall die unter „Unsicher“ der Seitendatei genannte Stelle als Ausschnitt (`-X0 -Y0 -X1 -Y1`).
   Neu gewinnt, wenn es die gemeldeten Stellen löst und sonst nicht schlechter ist; bei Doppelaufnahmen das Foto
   mit vollständigen Rändern.
3. **Austauschen:** das alte Foto und verworfene Doppelaufnahmen in den Nachbarordner `<fotoordner> ersetzt`
   verschieben – nicht löschen, und nicht in einen Unterordner des Fotoordners. Jetzt zeigt `md_pruefen.py` je
   Seite zwei Fehler (neues Foto unerfasst, altes fehlt): das ist die Arbeitsliste.
4. **Nachziehen, nicht neu schreiben:** die Seitendatei gegen das neue Foto prüfen – `fotos`, `drehung`,
   `lesbarkeit`; jede Stelle unter „Unsicher“ am neuen Foto klären (geklärt = Inhalt korrigieren und Eintrag
   streichen, ein Druckfehler des Buchs bleibt `Druckfehler?`), `unsicher` neu zählen, Wortlisten Zeile für Zeile
   gegenlesen. Briefing je Agent: Liste `Datei ← neues Foto, Drehung`, höchstens ~6 dichte Doppelseiten, sonst
   wie die Vorlage oben.
5. **Abschluss:** `_buch.md` nachführen (`fotos-ordner`, falls der Ordner umzog; `## Abdeckung`), Prüfskript mit
   `--index` über den ganzen Ordner, Fotoliste des Nutzers abhaken.

### 2. Stoff verstehen (Notizen in eigenen Worten)

Ab hier nur Fremdsprachen (siehe oben). **Quelle sind die Seitendateien aus `md/`**, nicht die Fotos; ein Foto
wird nur geöffnet, wenn eine Seitendatei eine Stelle als unsicher führt.

- `analyse/units.md`: je Unit Titel, Themen, Kommunikationsziele, Grammatik, Wortfelder – aus Inhaltsverzeichnis
  und Unit-Seiten.
- `analyse/grammatik.md`: je Grammatik-Thema die Regel in eigenen Worten, eigene Beispiele, typische Fehler.
- Unsicheres als offene Frage notieren, nicht glätten.

### 3. Dateien schreiben (siehe FORMAT.md)

- **`buch.json`**: Reihe, Klasse, Quelle, Fachlehrer-Profil (Persona + Didaktik mit typischen Fehlern), Units.
  Grammatik-Themen erst gegen den Server abgleichen
  (`GET /api/v1/creator/grammar-topics?subjectId=<fach>&take=500`) und vorhandene per Slug verwenden; nur echt
  neue unter `grammatik` anlegen lassen.
- **`vokabeln-unit<N>.tsv`** je Unit mit fotografiertem Vokabelteil, Zeile für Zeile in Buchreihenfolge:
  - `wort` und `uebersetzung` buchstabengetreu (Artikel, Femininform, Klammern, `qn`/`qc`, `sb`/`sth`, Akzente,
    Apostroph wie gedruckt).
  - `hinweis` nur für das Kind Nützliches (unregelmäßige Formen, Plural, Aussprache, Genus) und nie die
    Übersetzung selbst; Seitenzahl und Leseprobleme gehören in `notiz`.
  - Abschnitte wie im Vokabelteil; ein Einstiegsabschnitt mit nur wenigen Wörtern darf dem nächsten zugeschlagen
    werden.
- **`uebungen.json`** (optional): je Unit zwei bis vier eigene Übungen zu den Grammatik-Schwerpunkten
  (Lückentext als `markup`, unregelmäßige Formen als `grammar`, Wendungen als `translation`, Landeskunde als
  `matching`). Eigene Sätze in der Lebenswelt eines Kindes der Klasse; jede Lücke hat genau eine richtige
  Lösung (sonst alle Schreibweisen in die Klammer), und die Lösung steht nicht schon im Satz.

### 4. Prüfen

```bash
python -I -X utf8 .claude/skills/lehrwerk-aufbereiten/laden.py raw/<fach>/<werk-slug>-pugling --pruefen
```

Jeder `FEHLER` wird behoben (bei `unsicher` am Foto bzw. an der Kachel, sonst den Nutzer fragen). Jeder `Hinweis`
wird angesehen: „Wort und Übersetzung gleich“ kann im Buch so stehen (`le restaurant = das Restaurant`), „Hinweis
verrät die Übersetzung“ ist fast immer zu streichen.

### 5. Gegen eine Wegwerf-DB laden

Wie `/smoke-test`: API im Hintergrund auf Port **5280** mit `ConnectionStrings__Default="Data Source=pugling_lehrwerk.db"`
und `RateLimiting__LoginEnabled=false` (aus `backend/Pugling.Api`, `--no-launch-profile`). Der Seed hat Konto 1 mit
PIN `0000` und Kind 1.

```bash
python -I -X utf8 .claude/skills/lehrwerk-aufbereiten/laden.py raw/<fach>/<werk-slug>-pugling --base http://127.0.0.1:5280 --pin 0000 --child 1
```

Erwartet: jede Übung 100 % im Selbsttest; ein zweiter Lauf meldet nur „schon da“. Danach das Briefing ohne
Sprachmodell ansehen – **mit Profil und Unit**, damit es sicher dieses Buch und diese Unit zeigt. Ohne Ort nimmt
der Agent mit `--child` das Profil auf der Reihe des Kinder-Buchs und bricht ab, wenn mehrere Bücher des Kindes eines
haben; das Fach kommt immer aus der Reihe (B-488):

```bash
Pugling__BaseUrl=http://localhost:5280 Pugling__Pin=0000 dotnet run --project backend/Pugling.Agent.Creator -- briefing --child 1 --profile <P> --series-unit <U> --unit <U> --type Vocabulary
```

Im Briefing muss „Wortschatz dieser Übung (aus der Wortliste der Unit …)“ mit Paaren aus der TSV stehen.
Aufräumen: Server auf 5280 beenden, `backend/Pugling.Api/pugling_lehrwerk.db*` löschen.

### 6. In die echte Instanz laden (nur nach Freigabe)

Dem Nutzer vorlegen: Units, Zahl der Wörter je Unit, alle Zeilen, die einmal `unsicher` waren, und wie sie geklärt
wurden, die Start-Übungen mit Titel. Erst nach seinem Ja, mit seiner PIN und seiner Adresse (`--base`, Vorgabe
`http://127.0.0.1:5200`; nicht `localhost`: Python versucht zuerst IPv6, der Heimnetz-Server hört nur auf IPv4 –
das kostet 2 s je Anfrage, ein Lauf dauert dann eine halbe Stunde). `--child` nur, wenn das Kind auf dieses Buch
umgestellt werden soll; das biegt sein vorhandenes Buch im Fach um, statt ein zweites danebenzulegen (der Agent wählt das Buch über die Reihe; passen
mehrere Bücher, bricht er ab). Eine korrigierte Wortliste kommt mit
`--ersetzen` nach; das überschreibt die Übungen gleichen Titels. Eine im Vater-Web gepflegte Buchseite
bleibt dabei stehen, außer `uebungen.json` nennt für die Übung eine eigene (`seiten`, B-589).

### 7. Abschluss

- Kurzbericht: was gelesen wurde, was offen ist (Units ohne Vokabelfotos, unsichere Stellen, fehlende Seiten),
  und die nächsten Aufrufe des KI-Creators (`plan --child <K> --profile <P> --series-unit <U> --unit <U>`).
- Nichts aus `raw/` committen. Entsteht ein Fund am Werkzeug selbst, gehört er in `docs/backlog/`.
