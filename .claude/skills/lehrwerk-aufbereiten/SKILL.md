---
name: lehrwerk-aufbereiten
description: Ein Schulbuch aus Fotos seiner Seiten für Pugling aufbereiten – Claude liest die Fotos selbst (Inhaltsverzeichnis, Unit-Seiten, Grammatikteil, Vokabelteil), schreibt Stoffnotizen, Wortlisten je Unit und eigene Start-Übungen in einen gitignorierten Ordner unter raw/, prüft sie mit laden.py, lädt sie über die API und stellt das Kind auf das Buch um. Nutze dies, wenn ein neues Lehrwerk, eine weitere Unit oder weitere Vokabelseiten aus Fotos in die App sollen („bereite das Buch auf“, „neue Seiten fotografiert“, „Vokabelseiten einlesen“, „Lehrwerk aus Fotos“). Nicht für reinen Text als Quelle (das ist `fill` im Skill ki-creator).
---

# Lehrwerk aus Fotos aufbereiten

**Claude liest die Seiten** (Read-Tool auf die Fotos), nicht das lokale Vision-Modell des Agenten: Das ist an
dichten Seiten gescheitert (B-459). Dass die Fotos dabei an das Modell gehen, hat der Nutzer ausdrücklich
erlaubt (B-470). Alles Weitere ist deterministisch: `laden.py` prüft, lädt und testet; Datenformat in
[FORMAT.md](FORMAT.md).

## Regeln, die nicht verhandelbar sind

1. **Urheberrecht.** In die App kommen Fakten, Wortpaare, Themen und Namen von Grammatik-Themen. **Nie**
   Aufgaben, Lesetexte, Dialoge oder Beispielsätze des Buchs; Regeln stehen in eigenen Worten, Beispiele sind
   eigene. Fotos, Kacheln und der Datenordner bleiben unter `raw/` (gitignored) und werden nie committet.
2. **Ein Wortpaar wird gelesen, nicht vermutet.** Ein falsches Paar lernt das Kind wochenlang. Was nicht sicher
   lesbar ist, bekommt in der Spalte `unsicher` einen Grund, statt geraten zu werden; `laden.py` lädt eine Unit
   mit unsicheren Zeilen nicht. Erst eine Kachel (`kacheln.ps1`) versuchen, dann den Nutzer fragen.
3. **Die PIN kommt vom Nutzer** und steht nie in einer Datei: `PUGLING_PIN` als Umgebungsvariable oder `--pin`.
   Nicht raten; ein `401 invalid_credentials` heißt nachfragen.
4. **Erst Wegwerf-DB, dann echte.** Gegen die Instanz des Nutzers wird erst geladen, wenn der Lauf gegen eine
   frische DB grün war und der Nutzer die Liste der Funde gesehen hat.

## Ablauf

### 1. Bestand aufnehmen

- Fotos liegen unter `raw/<fach>/<werk>/` (vom Nutzer abgelegt, schreibgeschützt). Arbeitsordner:
  `raw/<fach>/<werk-slug>-pugling/`. Gibt es ihn schon, ist das ein **Nachtrag**: `buch.json` lesen und
  ergänzen, nichts neu erfinden.
- Jedes Foto einmal ansehen und in `analyse/fotos.md` eine Zeile schreiben: Datei, Buchseite, Art
  (Inhaltsverzeichnis · Unit-Einstieg · Unit-Seite · Grammatikteil · Vokabelteil · Sonstiges), lesbar ja/nein.
- Dichte oder kleine Schrift (Vokabelteil, Grammatikteil) **immer** in Kacheln lesen:

  ```bash
  pwsh -NoProfile -File .claude/skills/lehrwerk-aufbereiten/kacheln.ps1 -Quelle "raw/<fach>/<werk>/IMG_x.jpg" -Ziel "raw/<fach>/<werk-slug>-pugling/kacheln" -Zeilen 3
  ```

  Doppelseite quer: `-Spalten 2`. Kacheln überlappen 8 %, eine angeschnittene Zeile steht in der Nachbarkachel ganz.

### 2. Stoff verstehen (Notizen in eigenen Worten)

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
python -I -X utf8 .claude/skills/lehrwerk-aufbereiten/laden.py raw/<fach>/<werk-slug>-pugling --base http://localhost:5280 --pin 0000 --child 1
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
`http://localhost:5200`). `--child` nur, wenn das Kind auf dieses Buch umgestellt werden soll; das biegt sein
vorhandenes Buch im Fach um, statt ein zweites danebenzulegen (der Agent wählt das Buch über die Reihe; passen
mehrere Bücher, bricht er ab). Eine korrigierte Wortliste kommt mit
`--ersetzen` nach; das überschreibt die Übungen gleichen Titels.

### 7. Abschluss

- Kurzbericht: was gelesen wurde, was offen ist (Units ohne Vokabelfotos, unsichere Stellen, fehlende Seiten),
  und die nächsten Aufrufe des KI-Creators (`plan --child <K> --profile <P> --series-unit <U> --unit <U>`).
- Nichts aus `raw/` committen. Entsteht ein Fund am Werkzeug selbst, gehört er in `docs/backlog/`.
