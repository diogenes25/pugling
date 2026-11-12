---
tags: [typ/story, status/idee, bereich/werkzeug, rolle/creator]
aliases: [Alternatives groß geschrieben geht verloren, distractors ohne markup, Birkenbihl im Selbsttest immer rot]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: ".claude/skills/lehrwerk-aufbereiten"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 34)
unverifiziert: true
entgangen_bei: [B-475]
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
