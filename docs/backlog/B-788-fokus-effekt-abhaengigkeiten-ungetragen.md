---
tags: [typ/story, status/abgenommen, bereich/test, rolle/entwickler]
aliases: [Fokus-Effekt-Abhängigkeiten ungetragen]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
nachgeschaut: "2026-11-22"
quelle: "Nachtlauf B 2026-11-22, Review und Abhängigkeits-Mutanten zu B-603 (docs/pm-sitzung-2026-11-22-b.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-541]
---

# B-788 · Zwei Abhängigkeiten des Fokus-Effekts im Test trägt kein Fall

Der Fokus-Effekt in `frontend/src/sohn/SohnTest.tsx:92-101` hängt an
`[item, cursor, revealed, error, result, action.busy]`. Gemessen beim Bau von B-603 (Lauf B, 2026-11-22): Mutanten, die
genau eine Abhängigkeit aus dem Dep-Array streichen, gegen `SohnTest.test.tsx` plus `SohnPractice.test.tsx`:

| gestrichen | neue Tests (B-603) | HEAD-Tests |
|---|---|---|
| `revealed` | 1 rot | – |
| `action.busy` | **0 rot** (87/87) | **0 rot** (35/35) |
| `cursor` | **0 rot** (87/87) | **0 rot** (35/35) |

Dazu `SohnPractice.tsx` `[phase, idx]`: ohne `idx` 2 rot, ohne `phase` 13 rot.

Die Lücke stammt also nicht aus B-603 (`waitFor`), sondern bestand schon vorher. Schwerer wiegt:
`SohnTest.test.tsx:416-417` verspricht im Kommentar zu AK 3 „Rot, wenn er … nicht an `action.busy` hängt“, und dieses Rot
kommt nicht. Das ist eine Zusicherung, die nur als Kommentar existiert (Regel in der Root-`CLAUDE.md`).

Offen für die Ausformulierung:

- Ob `action.busy` und `cursor` im Dep-Array überhaupt nötig sind. Vielleicht deckt eine andere Abhängigkeit jeden
  echten Wechsel ab. Dann wären sie Ballast, und der Kommentar wäre falsch.
- Oder ob es einen Ablauf gibt, in dem nur sie wechseln. Dann fehlt ein Fall, der sie trägt.

## Ist-Stand am Code (gemessen 2026-11-22, Lauf B)

Einmal mitgeschrieben, mit welchen Werten der Fokus-Effekt läuft, im Fall AK 3
(`SohnTest.test.tsx` „steht nach einer Auswahl auf der ersten freigegebenen Option“). Spalten: Frage | `cursor` |
`revealed` | `error` | `result` | `action.busy` | Absicht:

```text
|0|false|false|false|false|false            erster Render, noch keine Frage
the horse|0|false|false|false|false|false   erste Frage
the horse|0|false|false|false|true|true     Klick: busy an, alte Frage gesperrt, kein Ziel → Absicht bleibt
the sheep|1|false|false|false|false|true    neue Frage, cursor 1 und busy aus im SELBEN Render → Fokus
```

- **`action.busy` wechselt allein nur auf `true`**, und dieser Lauf findet bewusst kein Ziel. Der Wechsel auf `false` kommt
  im selben Render wie die neue Frage: `setItem` steht im `fn` von `action.run`, `setBusy(false)` in dessen `finally`; beides
  läuft in derselben Mikrotask-Kette und wird von React gebündelt (`frontend/src/lib/useAction.ts`, `run`). Darum ist die
  Mutante „`action.busy` gestrichen“ grün: der Lauf mit der neuen Frage kommt über `item` ohnehin.
- **`cursor` wechselt nie allein:** jeder Aufruf von `setCursor` in `SohnTest.tsx` steht neben einem `setItem` (`start`,
  `resume`, `answerAndAdvance`). Einzige Lücke (Review): `start()` aus dem Ergebnisbild, wo `item` schon `null` ist; dort
  wechselt im selben Render `result`. Gelesen wird `cursor` im Effekt nicht. Das ist Ballast.
- **Der Code-Kommentar über dem Effekt** behauptete, „die neue Frage steht schon, während `action.busy` ihre Optionen noch
  sperrt“. Gemessen ist es die **alte** Frage; die Regel (Absicht erst mit Ziel verbrauchen) stimmt trotzdem und ist getragen
  (Mutante M3 unten).

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf B 2026-11-22 (Defekt, autonom grillbar), unbestätigt.*

1. **`cursor` fliegt aus dem Dep-Array.** *Begründung:* nie gelesen, wechselt nie ohne `item` oder `result`. *Kosten:* keine; `exhaustive-deps`
   fordert es nicht. *Verworfen:* einen Fall bauen, der es trägt (es gibt keinen Ablauf).
2. **`action.busy` bleibt im Dep-Array und wird als ungetragen beschriftet.** *Begründung:* Der Effekt liest es; es zu
   streichen hieße, eine Abhängigkeit zu verschweigen, und `exhaustive-deps` würde es anmahnen. *Kosten:* Eine Mutante, die
   es streicht, bleibt grün – das steht jetzt ausdrücklich am Effekt und am Fall AK 3, statt dort ein Rot zu versprechen.
   *Verworfen:* einen Fall mit erzwungenem Zwischen-Render bauen – er prüfte einen Zustand, den die App nie erreicht.
3. **Beide Kommentare werden richtiggestellt:** der Code-Kommentar (alte statt neue Frage) und der Test-Kommentar zu AK 3
   (verspricht kein Rot mehr für `action.busy`). Regel aus der Root-`CLAUDE.md`: eine Zusicherung, die nur als Kommentar
   existiert, wird umgeschrieben oder als Absichtserklärung beschriftet.

## Akzeptanzkriterien

1. Kein Kommentar verspricht ein Rot, das keine Mutante liefert: Mutante „`action.busy` gestrichen“ ist grün und so beschriftet.
2. `cursor` steht nicht mehr im Dep-Array; `SohnTest.test.tsx` und `SohnPractice.test.tsx` bleiben grün, und die Mutante, die es
   zurückbringt, ändert nichts (grün).
3. Die Regel „Absicht erst mit Ziel verbrauchen“ bleibt getragen: verbraucht der Effekt die Absicht auch ohne Ziel, fällt AK 3.

## Schätzung

**XS** – eine Zeile Produktcode (Dep-Array), zwei Kommentare. `wo: frontend`.

## Verlauf

- **2026-11-22** — angelegt (Nachtlauf B, Abhängigkeits-Mutanten zu B-603).
- **2026-11-22** — ausformuliert, gegrillt, geschaetzt (Nachtlauf B 2026-11-23, Sprint B1; Defekt, autonom): Ablauf des
  Effekts mitgeschrieben (Tabelle oben), beide offenen Fragen beantwortet: `cursor` ist Ballast, `action.busy` ist gelesen,
  aber von keinem Ablauf getragen.
- **2026-11-22** — in-arbeit: `SohnTest.test.tsx` + `SohnPractice.test.tsx` 87/87 grün. Mutanten-Probe nach der letzten Änderung:

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | M1-busy-gestrichen | frontend/src/sohn/SohnTest.tsx:106 | green | green |
  | M2-cursor-zurueck | frontend/src/sohn/SohnTest.tsx:106 | green | green |
  | M3-absicht-ohne-ziel-verbraucht (`if (!target) { focusIntent.current = false; return; }`) | frontend/src/sohn/SohnTest.tsx:103 | red | red (Fall AK 3) |

- **2026-11-22** — Review `frontend-reviewer`: keine 🔴/🟡. Zwei 🟢 zum Kommentar eingearbeitet (ohne Zählung): „`cursor`
  wechselt nie ohne `item`“ stimmt bei `start()` aus dem Ergebnisbild nicht wörtlich (dort wechselt `result`); und warum
  `action.busy` als Sicherung stehen bleibt (ein künftiges `await` nach `setItem` in `run`). Batching-Argument im echten
  Browser bestätigt (reine Mikrotask-Kette vor dem geplanten Render).
- **2026-11-22** — abgenommen (d7b7546b). Rollengang Sprint B1 als Maschinengang: E2E `full-flow`, `test-durchgefallen-ueben`, `erfolge-woche-frist`, `karten-umdrehen` auf :5207/:5179 gegen eine Wegwerf-DB, 4 passed; Vitest voll 1327/1327. Sohn: Klausur und Erfolge laufen wie zuvor (kein sichtbarer Unterschied – reine Test-/Abhängigkeitsänderung); Vater und Creator: Regressionszeugen, kein Pfad im Diff.

- **2026-11-22** — Nachschau (Retro Sprint B2): `git show d7b7546b`: Dep-Array genau um `cursor` gekürzt, `action.busy` steht; die Kommentare entsprechen der Mutanten-Tabelle; Vitest voll nach Sprint B2 1327/1327; kein Befund.
