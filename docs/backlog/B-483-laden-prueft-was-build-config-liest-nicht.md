---
tags: [typ/story, status/abgenommen, bereich/werkzeug, rolle/creator]
aliases: [Alternatives groß geschrieben geht verloren, distractors ohne markup, Birkenbihl im Selbsttest immer rot]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: ".claude/skills/lehrwerk-aufbereiten"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 34)
unverifiziert: false
entgangen_bei: [B-475]
nachgeschaut: ""
grund: ""
ersetzt_durch: []
---

# B-483 · `laden.py` prüft Felder, die `build_config` anders liest

Nachschau von Sprint 34 (frischer `pugling-reviewer` an `c53291c`; `unknown_fields`/`build_config` direkt
aufgerufen, kein Serverlauf). B-475 hat die Groß/Klein-Lücke der Abkürzung `markup` geschlossen, nicht die Klasse:
`check()` und `build_config` sind zwei Leser derselben Felder mit verschiedenen Regeln.

1. **`Alternatives` groß geschrieben geht still verloren** (`laden.py:116` gegen `:284`): die Prüfung lässt es durch
   wie der Server, `build_config` liest nur `alternatives` und hängt eine zweite Liste klein geschrieben an –
   `{"target":"b.","Alternatives":["c"]}` wird `{"Alternatives":["c"],"alternatives":["b"]}`. Gelesen, nicht
   gemessen: der Server behält bei doppelten Schlüsseln den letzten, „c" ist weg, der Selbsttest bleibt grün.
   Verwandt (gemessen): `"Target"` → `KeyError`, `"alternatives": null` → `TypeError` – mitten im Lauf.
2. **`distractors` ohne `markup` besteht die Prüfung**, der Server lehnt mit `unknown_field` ab (`laden.py:117`,
   `:275`): `build_config` entfernt `distractors` nur im `markup`-Zweig.
3. **Jede Birkenbihl-Übung fällt im Selbsttest durch** (`laden.py:402`): der Typ hat keine getippte Stufe, die
   Vorschau wertet `wasKnown ?? false`, der Lader schickt `null` → 0 %. Seit B-475 auch für stehen gebliebene
   Übungen, also endet jeder Lauf über einen Ordner mit `birkenbihl` dauerhaft mit „ACHTUNG".

## Akzeptanzkriterien

1. **`build_config` liest nur die Abkürzungen** `markup`/`distractors`; jedes andere Feld geht so an den Server, wie
   es im Ordner steht. Damit gibt es außer `check()` und dem Server keinen dritten Leser mehr.
2. **`check()` lehnt ab, was der Server still anders liest:** dasselbe Feld in zwei Schreibweisen (auf jeder
   Ebene), `distractors` ohne `markup`, eine Abkürzung neben dem Feld, das sie selbst erzeugt.
3. **Der Selbsttest wertet ungetippte Typen nicht als Fehler**, sagt aber, dass er dort nur den Inhalt prüft; als
   bestanden gilt `correct == total`, nicht der gerundete Prozentwert.

## Entscheidungen (autonom gegrillt, `art: Defekt`)

1. **Die Schlusszeichen-Varianten fallen weg, statt groß/klein-tolerant gelesen zu werden.** Seit B-457 verzeiht
   der Antwortvergleich `. ? ! …` am Ende (`StageMechanics.Normalize`); die Varianten waren der einzige Grund, dass
   `build_config` Übersetzungen anfasst. Ohne sie verschwindet der zweite Leser, statt dass er eine dritte Regel
   lernt. Auch `"Target"` und `"alternatives": null` stürzen damit nicht mehr ab.
2. **Ungetippt = `wasKnown: true` plus Vermerk**, nicht ausgelassen: ein leerer Inhalt (`400`) fällt so weiter auf.
3. **Geprüft wird auf einer getippten Stufe, wenn der Typ eine hat** – nicht auf der Vorgabestufe der Übung; „nur
   Inhalt geprüft" heißt damit wirklich „der Typ kann nicht getippt werden" (Review-Fund 3).
4. **Neben `markup` keine eigene `wordBank`**: die Bank kommt nur aus `distractors`, sonst fehlt einer Lücke
   ihre Lösung in der Bank (Review-Fund 2).
5. Die 🟢-Reste unten bleiben offen, außer `scorePercent` (AK 3), `FORMAT.md:125` (Entscheidung 1), dem
   fehlenden `typ` und den doppelten Kacheln (Review-Funde 5–6).

## Reste aus derselben Nachschau (🟢)

- Falsche JSON-Typen (`"tasks": ["faire"]`, `"ordered": "true"`) und fehlende Pflichtfelder bestehen die Prüfung.
- `scorePercent` rundet 199/200 auf 100 – `correct != total` wäre das ehrliche Maß.
- Eine leere Übung (`"tasks": []`) beendet den Selbsttest mit 400, statt einzeln rot zu zählen.
- Stehen gebliebene Übungen werden in der Fassung des Servers geprüft, nicht der des Ordners; die Meldung sagt das
  nicht.
- `FORMAT.md:125` „der Server vergleicht die Satzzeichen streng" ist seit B-457 falsch; die Varianten ohne
  Schlusszeichen in `build_config` sind damit überflüssig.
- `LadenFieldListTests` steigt nicht in Objekte, die keine Liste sind, und nicht in `Dictionary`-Felder; `SHORTCUTS`
  hält der Test nicht gegen `build_config`.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 34 im Backlog-Lauf).
- **2026-11-12** — ausformuliert, gegrillt (autonom, `art: Defekt`) und geschätzt (S) im Sprint 36; in Arbeit.
  Die Klasse statt der drei Fälle: `grep` nach jedem Leser eines Config-Felds in `laden.py` (Vorschlag CR) fand
  genau zwei – `unknown_fields` und `build_config`; Entscheidung 1 nimmt den zweiten bis auf die Abkürzungen weg.
- **2026-11-12** — Fälle und Belege. Offline-Probe (`probe483.py` im Scratchpad, ruft `check`/`build_config` direkt):
  - AK 1: `Alternatives` groß bleibt in `build_config` unverändert; `Target` und `alternatives: null` kein Absturz.
  - AK 2: doppelte Schreibweise oben in `items[]` und verschachtelt in `sentences[].decoding[]` → Fehler, eine
    Schreibweise allein → kein Fehler; `distractors` ohne `markup` → Fehler, mit → keiner; `markup` + `text`,
    `markup` + `Gaps`, `distractors` + `wordBank` → Fehler; `markup` + eigene `wordBank` ohne `distractors` → keiner.
  - Beide echten Ordner (`access-pugling`, `decouvertes-serie-bleue-2-pugling`) bestehen `--pruefen` weiter.
  - AK 1 und 3 gegen einen Wegwerf-Server (`:5280`, frische DB, danach gelöscht): neuer Lader → Birkenbihl
    `100 % 1/1 (ohne getippte Stufe – nur Inhalt geprüft)`, gespeichert `alternatives: ["Where's the station?"]`;
    alter Lader (`HEAD`) am selben Ordner → Birkenbihl `0 % 0/1`, „ACHTUNG", und mit `--ersetzen` bleibt nur
    `["Where is the station"]` – Punkt 1 der Story damit **gemessen**, nicht mehr nur gelesen.
  - Ungetestet: `correct != total` (199/200 lässt sich ohne eine Übung mit 200 Atomen nicht bauen; gelesen).
- **2026-11-12** — Mutanten (Python, gegen die Offline-Probe; `mutant-probe.py` misst nur `dotnet test`):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | doppelte Schreibweise erst ab drei | `unknown_fields` | red | red (2 Fälle) |
  | Kollision nur exakt geschrieben | `check` | red | red (2 Fälle) |
  | `markup` schreibt kein `gaps` | `SHORTCUT_WRITES` | red | red |
  | `distractors` schreibt keine `wordBank` | `SHORTCUT_WRITES` | red | red |
  | `distractors` ohne `markup` erlaubt | `check` | red | red |
  | `build_config` fasst Übersetzungen wieder an | `build_config` | red | red |

  (Erste Fassung, vor dem Review.)
- **2026-11-12** — `pugling-reviewer`: kein 🔴, drei 🟡, alle behoben – **Funde 1–3 des Sprints 36 (zählen)**:
  (1) die *Form* von `markup`/`distractors` las nur `build_config` (`"distractors": "go, gone"` wurde zu Kacheln aus
  Einzelbuchstaben, eine Zahl brach mitten im Lauf); (2) `markup` neben einer eigenen `wordBank` ging durch;
  (3) „ungetippt" hing an der Vorgabestufe der Übung – eine im Vater-Web auf Selbsteinschätzung gestellte
  Vokabelübung hätte grün „nur Inhalt geprüft" gemeldet. Dazu 🟢 behoben: fehlender `typ` stürzte `check()` ab,
  `went`/`Went` gaben zwei gleiche Kacheln. 🟢 offen: der Selbsttest spielt den Lückentext auf der freien Stufe,
  nicht auf der Wortbank, die das Kind sieht.
- **2026-11-12** — Belege nach den Review-Fixes. Offline-Probe jetzt 18 Fälle (neu: `distractors` als Text, mit Zahl,
  mit Leerem; `markup` als Liste; `markup` + eigene `wordBank`; ohne `typ`; Bank ohne Doppelkachel) – alle grün.
  Wegwerf-Server (`:5280`, frische DB, danach gelöscht): Vokabelübung per `PUT … defaultStage: 2` auf
  Selbsteinschätzung gestellt → neuer Lader `100 % 4/4 Vokabel-Probe` ohne Vermerk (geprüft auf Stufe 4, Freitext);
  Mutante „nur die Vorgabestufe" → `… (ohne getippte Stufe – nur Inhalt geprüft)`. `LadenFieldListTests` 6/6.
- **2026-11-12** — Mutanten nach der letzten Änderung (Python, gegen die Offline-Probe):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | doppelte Schreibweise erst ab drei | `unknown_fields` | red | red |
  | Kollision nur exakt geschrieben | `check` | red | red |
  | `markup` schreibt kein `gaps` | `SHORTCUT_WRITES` | red | red |
  | `markup` schreibt keine `wordBank` | `SHORTCUT_WRITES` | red | red |
  | `distractors` ohne `markup` erlaubt | `check` | red | red |
  | `build_config` fasst Übersetzungen wieder an | `build_config` | red | red |
  | `markup` beliebigen Typs | `check` | red | red |
  | `distractors` keine Liste erlaubt | `check` | red | red |
  | leere `distractors` erlaubt | `check` | red | red |
  | ohne `typ` weiterprüfen | `check` | red | red (Absturz) |
  | Bank unterscheidet Groß/Klein | `build_config` | red | red |
  | nur die Vorgabestufe | `typed_stage` | Vermerk | Vermerk (Serverlauf) |

  Eine Zeile **verworfen statt gemessen**: `"distractors": {"wordbank"}` in `SHORTCUT_WRITES` blieb als Mutante
  grün – `distractors` gibt es nur mit `markup`, dessen Eintrag die `wordBank` schon abdeckt. Die Zeile ist entfernt.
- **2026-11-12** — **abgenommen.** Review-Zähler Sprint 36: 3 von 5. **Rollengang (Creator):** echter Lader-Lauf gegen
  den Wegwerf-Server nach der letzten Änderung; beide echten Ordner bestehen `--pruefen`. Backend unverändert.
- **2026-11-12** — Commit der Abnahme: `4a2e170`.
