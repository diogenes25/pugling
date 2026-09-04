---
tags: [typ/story, status/verworfen, bereich/frontend]
aliases: [flackernder E2E, bilder.spec instabil]
status: verworfen
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Verifikation zu B-210 (2026-09-04)
unverifiziert: true
grund: "Dublette zu B-153, das denselben Fall seit 2026-08-11 fuehrt und die Ursache belegt hat"
ersetzt_durch: [B-153]
entgangen_bei: []
wartet_auf: ""
---

# B-213 · `bilder.spec.ts` fällt im vollen Lauf gelegentlich, allein nie

Beim ersten vollen `playwright test` während der Verifikation zu B-210 fiel
`e2e/bilder.spec.ts:77` („Vater bebildert eine Vokabel, der Sohn sieht sein Bild und kann es
wechseln") — **54 passed, 1 failed**. Derselbe Fall allein nachgefahren: grün. Der volle Lauf danach,
unverändert: **56/56 grün**.

Ein Zusammenhang mit dem B-210-Diff ist ausgeschlossen: `bilder.spec.ts` fasst den Bearbeiten-Dialog
nicht an (kein Treffer für „Bearbeiten"/`ed-title`/„Inhalt speichern"), und Playwright fährt die Specs
alphabetisch — `bilder` lief also **vor** der neuen Spec, konnte deren Daten in der geteilten DB gar
nicht sehen.

**Warum das trotzdem eine Story ist:** Ein Testtor, das gelegentlich ohne Grund rot wird, erzieht dazu,
Rot nicht mehr ernst zu nehmen. Genau davon lebt das Tor aber.

## Offene Punkte

1. Woran hängt es? Verdacht auf einen Wartepunkt, der auf einen Zustand statt auf eine Wirkung wartet —
   die Meldung des ersten Laufs wurde nicht festgehalten, es gibt also noch keinen Fundort.
   *Empfehlung: den vollen Lauf mit `--repeat-each` oder mehreren Durchgängen wiederholen und die
   Fehlermeldung sichern, bevor irgendetwas geändert wird.*
2. Ist es die geteilte DB (Reihenfolge-Abhängigkeit) oder eine Zeitfrage in der Spec selbst?

## Verlauf

- **2026-09-04** — aufgenommen aus der Verifikation zu B-210 und **noch am selben Tag verworfen**:
  [B-153](B-153-bilder-spec-flackert-im-vollen-lauf.md) führt denselben Fall seit dem 2026-08-11 und hat
  am 2026-08-14 die Ursache mit erhaltener Fehlermeldung belegt (`strict mode violation`, der Bildbezug
  trifft zwei Elemente). Die Beobachtung von heute ist dort als dritte Sichtung nachgetragen.

  **Warum die Dublette entstand:** Ich habe den Fund beim Verifizieren aufgeschrieben, ohne den Index nach
  dem Symptom zu durchsuchen — und dabei sogar eine Ursachen-Vermutung („geteilte DB?") als offenen Punkt
  formuliert, die B-153 bereits ausdrücklich widerlegt hatte. Ein Fund beim Bauen wird eine eigene Story
  (README, „Ein Fund beim Bauen…"); *ob es sie schon gibt*, steht dort nicht — es ist die Grep vorher.
