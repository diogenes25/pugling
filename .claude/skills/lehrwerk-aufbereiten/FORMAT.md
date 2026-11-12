# Datenformat eines aufbereiteten Lehrwerks

Ein Ordner `raw/<fach>/<werk>-pugling/` (gitignored, wie die Fotos daneben). `laden.py` liest genau diese Dateien.

```text
raw/france/decouvertes-2-pugling/
  buch.json              Pflicht
  vokabeln-unit1.tsv     eine je Unit, deren Vokabelseiten fotografiert sind
  vokabeln-unit2.tsv
  uebungen.json          optional: eigene Start-Übungen
  analyse/               Lesenotizen je Foto (Markdown, eigene Worte), nur für Menschen
  kacheln/               Ausschnitte dichter Seiten aus kacheln.ps1
```

## buch.json

```json
{
  "reihe": {
    "name": "Découvertes Série bleue 2",
    "verlag": "Klett",
    "fach": "Französisch",
    "schularten": "Gymnasium",
    "source": "fr",
    "target": "de",
    "notes": "Klasse 7, 2. Lernjahr. Aus welchen Seiten die Notizen stammen (eigene Zusammenfassung)."
  },
  "klasse": 7,
  "quelle": "Découvertes Série bleue 2",
  "profil": {
    "name": "Französisch Klasse 7 Gymnasium – Découvertes 2",
    "persona": "Du bist … (≤ 2000 Zeichen; Niveau, Ton, nie Aufgaben oder Texte des Buchs übernehmen)",
    "didactics": "Ziele je Unit und typische Fehler, die Übungen gezielt üben sollen (≤ 2000 Zeichen)",
    "defaultTypes": ["Vocabulary", "Cloze", "Grammar", "Translation"]
  },
  "grammatik": [
    { "label": "Le passé composé avec être", "synonyme": ["passé composé être"] }
  ],
  "units": [
    {
      "nr": 1,
      "label": "Unité 1 Bienvenue à Paris",
      "kurz": "Unité 1",
      "topics": ["Wegbeschreibung in der Stadt", "…"],
      "grammar": "Regeln in eigenen Worten, mit eigenen Beispielen (≤ 2000 Zeichen)",
      "grammatik": ["Le passé composé", "le-futur-proche"],
      "wortfelder": "Stadt, Verkehrsmittel, Wegbeschreibung",
      "abschnitte": { "Volet 1": "Volet 1 – Au café" }
    }
  ],
  "kind": {
    "unit": 1,
    "klassenarbeit": { "titel": "Vokabeltest Unité 1", "thema": "Découvertes 2, Unité 1: Wörter Volet 1–3" }
  }
}
```

| Feld | Bedeutung |
|---|---|
| `reihe.fach` | muss als Fach auf dem Server stehen (Seed: Englisch, Französisch, Mathe, Erdkunde) |
| `reihe.schularten` | Schulart-Name wie im Vertrag (`Gymnasium`, `Realschule` …); der erste Eintrag geht ans Kind |
| `reihe.source` / `target` | Lernsprache / Muttersprache als Kürzel (`fr`/`de`) |
| `grammatik` | Themen, die es im Fach noch **nicht** gibt; sie werden angelegt. Vorhandene nicht wiederholen |
| `units[].grammatik` | Verweise auf Themen per Label **oder** Slug (`le-passe-compose`); ein unbekanntes stoppt das Laden mit der Liste der vorhandenen |
| `units[].grammar` | Stoffnotiz der Grammatik. Neuer Stoff der Unit steht als **`NEU <Label des Themas>`** (z. B. `NEU question tags: …`), Wiederholtes ohne den Marker – der KI-Lehrplan übt die markierten Themen zuerst (B-471). Ein Marker je Thema, und hinter `NEU` steht das **Label** wörtlich (`NEU passive`, nicht `NEU the passive`) – sonst greift er nicht |
| `units[].kurz` | Präfix der Übungstitel (`Unité 1 · Wörter …`); Vorgabe `Unit <nr>` |
| `units[].abschnitte` | lesbare Namen der Abschnitte aus der TSV (sonst steht der rohe Name im Titel) |
| `kind` | nur mit `--child`: aktuelle Unit des Kindes und, falls im Fach eine Klassenarbeit *geplant* ist, deren Titel/Thema; sie bekommt die Wortlisten der Unit |

Die Stoffnotiz zum Wortschatz einer Unit baut das Skript selbst: `Wortfelder` plus jede Zeile der TSV mit einem
`hinweis` als „Stolperstein“. Ohne TSV steht dort, dass die Wortliste noch fehlt.

## vokabeln-unit\<N\>.tsv

Tab-getrennt, UTF-8, genau diese sechs Spalten in dieser Reihenfolge:

| Spalte | Inhalt | sieht das Kind? |
|---|---|---|
| `abschnitt` | Abschnitt des Vokabelteils (`Volet 1`, `Atelier`, `Part A` …); je Abschnitt entsteht **eine** Vokabelübung | im Titel |
| `wort` | wie im Buch: mit Artikel (`la ville`, `l'hôtel (m.)`), Femininform (`content,e`), `qn`/`qc`, Akzente exakt | ja |
| `uebersetzung` | wie im Buch; mehrere Bedeutungen mit `,` oder `;` (jede einzelne zählt als richtig, B-462) | ja |
| `hinweis` | nur, was dem Kind beim Lernen hilft: unregelmäßige Formen, Plural, Aussprache, falsche Freunde. Darf die Übersetzung **nicht** nennen | ja |
| `notiz` | Seitenzahl, Lesbarkeit, Rückfragen – bleibt intern | nein |
| `unsicher` | leer, wenn sicher gelesen; sonst **warum** („Akzent unleserlich“, „Zeile angeschnitten“). Eine nicht leere Zeile stoppt das Laden | nein |

Eine Unit braucht mindestens 3 Wörter, damit der KI-Creator sie als Wortliste nimmt; sonst erfindet er wieder
eigene Paare.

## uebungen.json (optional)

Eine Liste eigener Übungen. Erlaubt sind Fakten (Landeskunde, Wortpaare) und eigene Sätze zum Stoff, **nie**
Aufgaben, Texte, Dialoge oder Beispielsätze des Buchs.

```json
[
  {
    "unit": 1, "typ": "cloze", "titel": "Unité 1 · Passé composé mit être", "reihenfolge": 10,
    "grammatik": ["Le passé composé avec être"], "beschreibung": "Setze das Verb im passé composé ein.",
    "config": {
      "markup": "Hier, nous [sommes allés|sommes allées] (aller) au cinéma.",
      "distractors": ["allons", "avons allé"]
    }
  },
  {
    "unit": 1, "typ": "grammar", "titel": "Unité 1 · Participes passés", "reihenfolge": 11,
    "config": { "instruction": "Schreibe das participe passé.", "tasks": [{ "prompt": "faire", "answer": "fait" }] }
  },
  {
    "unit": 1, "typ": "translation", "titel": "Unité 1 · Nach dem Weg fragen", "reihenfolge": 12,
    "config": { "sourceLang": "de", "targetLang": "fr",
                "items": [{ "source": "Wo ist der Bahnhof?", "target": "Où est la gare ?", "alternatives": ["Où se trouve la gare ?"] }] }
  },
  {
    "unit": 1, "typ": "matching", "titel": "Unité 1 · Pariser Sehenswürdigkeiten", "reihenfolge": 13,
    "config": { "instruction": "Was gehört zusammen?", "pairs": [{ "left": "la tour Eiffel", "right": "1889 eröffnet" }] }
  }
]
```

- `typ` ist die Route der Creator-API (`vocabulary`, `cloze`, `grammar`, `matching`, `translation`, `reading`,
  `list`, `birkenbihl`); `config` geht unverändert an sie, bis auf zwei Abkürzungen:
  - **Lückentext als `markup`**: `[Lösung|andere Schreibweise]` im Text, die Grundform in runden Klammern dahinter.
    Daraus werden Platzhalter und Lücken; mit `distractors` entsteht eine alphabetische Wortbank (Lösungen +
    Ablenker, je Wort eine Kachel ohne Rücksicht auf Groß/Klein). Ohne `distractors` tippt das Kind frei. Beide
    Abkürzungen nur genau so klein geschrieben; `distractors` als Liste von Wörtern und nur zusammen mit `markup`;
    neben `markup` weder `text`, `gaps` noch eine eigene `wordBank` – die Wortbank kommt dann nur aus `distractors`.
  - Alles andere geht unverändert an den Server. Schlusszeichen muss keine Lösung doppelt tragen: der
    Antwortvergleich verzeiht `. ? ! …` am Ende (B-457).
- **Felder je Typ:** `config` darf nur die Felder des Vertrags tragen; `--pruefen` lehnt jedes andere ab, mit
  Pfad und der Liste der erlaubten (Liste `FIELDS` in `laden.py`). Vor allem: Regelaufgaben (`grammar`) haben je
  Aufgabe nur `prompt`, `answer`, `ruleHint` – **keine `alternatives`**, anders als Lückentext, Übersetzung und
  Liste. Groß/Klein, Schlusszeichen und den typografischen Apostroph (`qu’il` = `qu'il`) gleicht der
  Antwortvergleich ohnehin aus; eine echte zweite Lösung braucht einen anderen Typ. Ein Feld zweimal in
  verschiedener Schreibweise (`alternatives` und `Alternatives`) ist ein Fehler – der Server behielte nur eines.
- **Selbsttest:** Nach dem Laden spielt der Lader jede Übung des Ordners im Testmodus mit ihren eigenen Lösungen.
  Geprüft wird auf einer getippten Stufe, auch wenn die Übung auf Selbsteinschätzung steht. Typen ganz ohne
  getippte Stufe (`birkenbihl`) lassen sich so nicht prüfen; ihre Zeile sagt „nur Inhalt geprüft".
- `reihenfolge` legt die Position in der Unit fest; die Wortlisten belegen 1, 2, 3 …, eigene Übungen ab 10.
- Felder, die der Vertrag nicht kennt, lehnt der Server mit `400 unknown_field` ab, statt sie zu verschlucken.
