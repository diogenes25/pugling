---
tags: [typ/story, status/in-arbeit, bereich/katalog, rolle/creator]
aliases: [laden.py Buchseite, bookPages beim Import]
status: in-arbeit
prio: P2
art: Defekt
groesse: XS
wo: "doku"
migration: nein
vertragsbruch: nein
quelle: B-588 (Verfeinerung im Backlog-Lauf 2026-11-16)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-589 · Der Import löscht die gepflegte Buchseite nicht und setzt sie auf Wunsch

[B-588](B-588-uebung-zeigt-ihren-lehrwerk-bezug.md) gibt der Übung ein Feld `bookPages`. Übungen werden per
PUT als volle Darstellung geändert, ein fehlendes Feld wird gelöscht. Ein `laden.py --ersetzen`-Lauf
(Skill `lehrwerk-aufbereiten`) schickte dieses PUT ohne `bookPages` – eine von Hand gepflegte Seite ging damit
still verloren. Zuschnitt seit dem Grillen: diese Story repariert den Import; der KI-Creator ist ausgeklammert
(Entscheidung 4).

## User Story

Als **Creator** möchte ich eine korrigierte Wortliste mit `laden.py --ersetzen` nachladen, ohne dass die im
Vater-Web gepflegten Buchseiten meiner Übungen verschwinden, und für eigene Start-Übungen die Seite gleich in
`uebungen.json` angeben können, damit das Kind an der Übung „S. 24-25" sieht und im Buch nachschlagen kann.

## Ist-Stand am Code

Nachgemessen am 2026-11-16 (Worktree `backlog-lauf-1116`, HEAD `d10da011`), Zeilen vor dem Bau:

- **PUT ist Vollersatz.** `ExerciseControllerBase.cs` schreibt bei PUT `BookPages` aus dem Payload, fehlt es,
  wird es `null` (B-588, Entscheidung 4; `backend/Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs:37`).
- **Der Import kennt kein Seitenfeld.** `laden.py:452` baut den Body mit `title`, `orderIndex`, `rewardPoints`,
  `config`, `description`, `grammarTopicIds`, `source` und Metadaten – ohne `bookPages`; `laden.py:459` schickt
  ihn bei vorhandener Übung und `--ersetzen` als **PUT**. Damit löscht jeder `--ersetzen`-Lauf die Seite.
- **Die Übungsliste trägt die Seite nicht.** `laden.py` findet vorhandene Übungen über
  `GET /creator/exercises?seriesUnitId=…` (`ExerciseSummary`, `Contracts/Creator/ExerciseCatalogDtos.cs:37`,
  ohne `BookPages`); nur das Detail `GET /creator/exercises/{id}` (`ExerciseCatalogController.cs:202`,
  `ExerciseDetail` `ExerciseCatalogDtos.cs:70`) liefert sie.
- **Eingabedateien:** `uebungen.json` hat kein Seitenfeld, `check()` prüft je Übung nur `unit`, `typ`, `titel`,
  `config` und die Abkürzungen (`laden.py:218`–`284`); unbekannte Felder auf Übungsebene werden still
  ignoriert. Die Vokabel-TSV (`TSV_COLUMNS`, `laden.py:26`) und `buch.json` kennen keine Seite. Die
  Seiten-Markdowns tragen `seiten: "24-25"` (`MARKDOWN.md:36`), aber keine Zuordnung Übung → Datei.
- **KI-Creator:** `backend/Pugling.Agent.Creator` legt Übungen nur per POST an, kein PUT – dort geht nichts
  verloren, er setzt aber auch keine Seite.
- Tests: `test_laden.py` (unittest, Standardbibliothek, läuft in CI `.github/workflows/ci.yml:93`), bisher 4 Fälle
  ohne API-Zugriff.

## Die echte Lücke

`--ersetzen` vernichtet still Daten, die ein Mensch gepflegt hat – genau der Weg, den `SKILL.md` für eine
korrigierte Wortliste empfiehlt. Und eine Seite, die der Autor beim Schreiben der Start-Übung kennt, kann er nicht
mitgeben.

## Offene Punkte

1. ~~Woher kommt der vorhandene Wert beim PUT?~~ → Entscheidung 1.
2. ~~Neues Feld in `uebungen.json`, und wer gewinnt?~~ → Entscheidung 2.
3. ~~Ableitung aus den Seiten-Markdowns bzw. Seite für Wortlisten aus der TSV?~~ → zurückgestellt, Entscheidung 3.
4. ~~KI-Creator?~~ → ausgeklammert, Entscheidung 4.
5. ~~Validierung offline?~~ → Entscheidung 5.

## Entscheidungen

Alle: **Stakeholder-Entscheidung im Auftrag des Nutzers (2026-11-16).** Defekt, autonom gegrillt (Freigabe des
Nutzers für den Bau im Backlog-Lauf).

1. **Vor dem PUT liest der Lader die gespeicherte Seite aus dem Detail und schickt sie mit**, wenn der Ordner für
   die Übung keine eigene nennt. Nur bei vorhandener Übung, nie beim POST. Begründung: der Server bleibt bei der
   Vollersatz-Semantik aus B-588 (kein drittes PATCH-Muster), die Liste trägt die Seite nicht. Kosten: ein GET je
   ersetzter Übung.
2. **Optionales Feld `seiten` je Übung in `uebungen.json`** (Freitext wie gedruckt, wie `seiten:` der
   Seiten-Markdowns); ein gesetzter Wert **gewinnt** über den gespeicherten, leer/Whitespace zählt als „keine".
   Ohne beides bleibt `bookPages` aus dem Body weg (Server: `null`). Begründung: wer die Seite in die Datei
   schreibt, will sie haben; der Feldname folgt dem deutschen Dateiformat. Kosten: eine von Hand im Vater-Web
   korrigierte Seite wird von einem veralteten Dateiwert wieder überschrieben.
3. **Ableitung aus den Seiten-Markdowns und eine Seite für Wortlisten (TSV/`buch.json`): zurückgestellt.** Es gibt
   keine Zuordnung Übung/Abschnitt → Seitendatei; sie zu erfinden sprengt XS. Kosten: Wortlisten-Übungen bekommen
   ihre Seite weiter nur von Hand – sie geht aber nicht mehr verloren.
4. **KI-Creator ausgeklammert.** Er macht kein PUT, also kein Datenverlust; die Seite im Briefing mitzugeben ist
   ein eigener Wunsch, keine Defektbehebung. Kosten: KI-erzeugte Übungen haben keine Seite.
5. **`--pruefen` prüft `seiten` offline:** Text mit 1–20 Zeichen nach Trim, sonst Fehler. Begründung: der Server
   antwortet sonst mitten im Lauf mit `400 validation_error` (B-588 Entscheidung 1). Kosten: keine.

## Akzeptanzkriterien

Jedes Kriterium nennt, welche Änderung es rot macht.

1. **PUT behält die gespeicherte Seite:** vorhandene Übung mit Seite „42", Ordner ohne `seiten` → PUT-Body trägt
   `bookPages: "42"`, auch über `load()` mit `--ersetzen`. **Rot**, wenn das Nachlesen fehlt oder `load()` am
   neuen Schreibweg vorbei direkt PUT ruft (Ausgangszustand).
2. **Importwert gewinnt:** `seiten: " 7 "` bei gespeicherter „42" → `bookPages: "7"`. **Rot**, wenn die
   gespeicherte Seite gewinnt oder nicht getrimmt wird.
3. **Leer ist keine Seite:** `seiten: "  "` bei gespeicherter „42" → `"42"`. **Rot** ohne Trim (der Server machte
   aus `"  "` ein `null`).
4. **Neu anlegen fragt nicht nach:** POST ohne vorhandene Übung → kein GET, ohne Wert kein `bookPages`; mit
   `seiten: "WB 12"` → `"WB 12"`. **Rot**, wenn das Nachlesen auch ohne `old` läuft. Der Teil „ohne Wert kein Feld"
   ist Absichtserklärung (der Server liest fehlend und `null` gleich).
5. **`--pruefen`:** 20 Zeichen gültig; 21 Zeichen, eine Zahl oder Whitespace → Fehler. **Rot** ohne die Prüfung.

## Schätzung

**Größe XS** – zwei kleine Funktionen und eine Prüfzeile in einem Python-Skript plus Tests, vergleichbar dem
XS-Anker B-02 (kleine Änderung plus der Test, der sie prüft), ohne Server-, Vertrags- oder Frontend-Anteil. `wo: doku` (Skill-Ordner, wie B-464/B-470),
`migration: nein`, `vertragsbruch: nein` (nutzt nur das additive `bookPages` aus B-588).

### Risiken

| Risiko | Gegenmittel |
| --- | --- |
| `load()` ist nur gegen eine laufende API ausführbar | Fake-Server im Test (`mock.patch.object(laden, "Api", …)`) fährt `load()` offline durch. |
| Ein Lauf gegen einen Server vor B-588 kennt `bookPages` nicht (`unknown_field`) | Feld nur senden, wenn ein Wert da ist; der GET liefert dort kein `bookPages` → `None` → nicht gesendet. |
| Stale `.pyc` verfälscht die Mutanten-Probe (gleich große Mutanten in derselben Sekunde) | Probe mit `python -B` und gelöschtem `__pycache__` (gemessen: ohne `-B` zeigte ein Mutant die Fehler des vorigen). |

### Angriffsplan

1. `laden.py`: `MAX_PAGES`, Prüfung in `check()`, `import_pages(e)`, `save_exercise(api, route, old, body, pages)`;
   `load()` schreibt über `save_exercise`.
2. `test_laden.py`: Klasse `Buchseite` mit Fake-API und Fake-Server.
3. `FORMAT.md` (Feld `seiten`, Beispiel), `SKILL.md` (Hinweis bei `--ersetzen`).

### Testweg

`python -B -I -m unittest discover -s .claude/skills/lehrwerk-aufbereiten -p "test_*.py"` (wie CI); je AK ein
Mutant mit roter Probe. Kein `dotnet`-Anteil, kein Server.

## Verlauf

- **2026-11-16** — angelegt als Folge aus der Verfeinerung von B-588; als `Defekt` geführt, weil B-588 den
  Datenverlust bei `--ersetzen` erst möglich macht. `entgangen_bei` bleibt leer: Abspaltung einer
  Grill-Entscheidung (B-588 Entscheidung 9), kein Durchrutscher.
- **2026-11-16** — `ausformuliert`: Ist-Stand am Code belegt (`laden.py:452`/`:459`, Detail vs. Liste).
- **2026-11-16** — `gegrillt`: Entscheidungen 1–5 autonom (Defekt), Stakeholder-Entscheidung im Auftrag des
  Nutzers.
- **2026-11-16** — `geschaetzt`: XS, `doku`, keine Migration, kein Vertragsbruch.
- **2026-11-16** — `in-arbeit`: gebaut in `laden.py` (`check()` `:288`, `import_pages` `:365`, `save_exercise`
  `:371`, Aufruf in `load()` `:482`); `test_laden.py` 4 → 10 Fälle, grün. Rote Probe je Mutant mit `python -B`:
- **2026-11-16** — AK 1, PUT ohne eigene Seite → `test_put_ohne_eigene_seite_behaelt_die_gespeicherte`
  (`test_laden.py:82`), Mutant „GET-Zeile in `save_exercise` entfernt" → rot (dazu AK-3- und `load()`-Fall).
- **2026-11-16** — AK 1, Verdrahtung in `load()` → `test_ersetzen_ueber_load_behaelt_die_gespeicherte_seite`
  (`test_laden.py:117`), Mutant „`load()` ruft wieder direkt PUT/POST" (Ausgangszustand) → rot.
- **2026-11-16** — AK 2, Importwert gewinnt → `test_seite_aus_dem_ordner_gewinnt` (`test_laden.py:89`), Mutant
  „`if old:` statt `if old and pages is None:`" → rot; Mutant „ohne `strip`" → rot.
- **2026-11-16** — AK 3, Whitespace → `test_leere_seite_im_ordner_zaehlt_als_keine` (`test_laden.py:95`), Mutant
  „`pages or None` ohne `strip`" → rot.
- **2026-11-16** — AK 4, POST ohne Nachfrage → `test_neu_anlegen_fragt_nicht_nach_und_setzt_nur_die_eigene_seite`
  (`test_laden.py:101`), Mutant „`if pages is None:` ohne `old`" → rot.
- **2026-11-16** — AK 5, `--pruefen` → `test_pruefen_lehnt_ungueltige_seite_ab` (`test_laden.py:155`), Mutant
  „Prüfung abgeschaltet (`if False and …`)" → rot. Offen bis `abgenommen`: Review und ein Lauf gegen eine
  Wegwerf-Instanz.
