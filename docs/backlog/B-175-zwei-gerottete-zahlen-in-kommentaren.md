---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/doku]
aliases: [drei oder sechs Naechte, 25 Tests sind 34, Zahlen verrotten]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-08-13 zu B-139
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-139]
---

# B-175 · Zwei Zahlen in Kommentaren widersprechen dem, was gemessen wurde

## Fall 1 — „drei Nächte" gegen „sechs Nächte", zwei Dateien desselben Fixes

`frontend/playwright.config.ts:53`:

> …der Nachtlauf war **drei Nächte** rot, jeder Aufruf antwortete mit 500…

`frontend/e2e/global-setup.ts:32`, aus demselben Fix:

> dieselbe Suite lief hier grün, während der Nachtlauf **sechs Nächte** rot war.

B-139 hat die Zahl ausdrücklich **widerrufen**
([dort, `:55-56`](B-139-e2e-nachtlauf-login-bricht-ab-test-2.md): „seit **sechs** Nächten (05.–10.08.), nicht
seit drei — ich hatte nur die ersten vier Einträge von `gh run list` angesehen"). Nachgezogen wurde die
Story und `global-setup.ts`, nicht die `playwright.config.ts`: ihr Kommentar entstand früher (`08c176a`), die
Korrektur später (`9412dcb`/`f5037c4`).

**Fehlerszenario:** Der nächste rote Nachtlauf. Wer die Ursache sucht, liest den Kommentar am Ort des
Geschehens, rechnet „drei Nächte" zurück und datiert den Beginn auf ~08.08. — also **nach** dem Landen von
`global-setup.ts` (06.08.). Damit verschwindet genau der Punkt, den B-139 bewusst als **offen** hinterlassen
hat: der erste rote Lauf war am 05.08., die Änderung erklärt den heutigen Fehler, nicht zwingend den ersten.

**Fix:** auf „sechs Nächte (05.–10.08.)" ziehen — oder die Zahl dort streichen und auf B-139 verweisen, dann
kann sie nicht wieder verrotten. Empfehlung: **streichen und verweisen.** Eine Zahl, die an zwei Orten steht,
driftet wieder; der Kommentar braucht sie nicht, um seinen Zweck zu erfüllen (er begründet `stdout: "pipe"`).

## Fall 2 — „25 Tests" sind 34

`.github/workflows/e2e.yml:51`:

> Die Suite braucht **25 Tests** bei `workers: 1`; 30 Minuten sind reichlich, aber endlich.

Gemessen am 2026-08-13: **34 passed** in 3,6 min (voller Lauf, `npm run test:e2e`). Die Zahl begründet ein
Zeitlimit von 30 Minuten und ist damit nicht folgenlos: sie ist das Argument dafür, dass das Limit „reichlich"
ist, und wächst mit jeder neuen Spec weiter aus dem Takt.

**Fix:** Empfehlung ebenfalls **die Zahl weglassen** und den Grund nennen (Reißleine gegen einen hängenden
`webServer`-Start). Ein Zeitlimit muss nicht mit der Testzahl begründet werden, und eine Begründung, die bei
jeder neuen Spec nachzuziehen wäre, wird nicht nachgezogen — dieser Bereich hat dafür den Beleg gleich zweimal
auf einer Seite.

## Warum das eine Story ist und kein Nebensatz

Die Regel dieses Repos lautet, dass eine Zahl in einer Behauptung besonders schnell verrottet; beide Fälle
sind Belege, und beide sind in *abgenommener* Arbeit durchgekommen. Als Zeile im `## Verlauf` einer alten
Story wären sie aus der Messung verschwunden — darum eine eigene, ausdrücklich dünne Story mit
`entgangen_bei`.

**Testweg**: keiner. Ein Tor über Prosa-Zahlen wäre teurer als der Schaden; der Fix nimmt beiden Zahlen
stattdessen die Existenz.

## Verlauf

- 2026-08-13 · Aufgenommen aus der **Nachschau** zu B-139. Beide Fundstellen von mir gegengeprüft; die
  34 stammen aus dem E2E-Lauf desselben Tages.
