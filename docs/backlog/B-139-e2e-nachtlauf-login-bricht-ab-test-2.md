---
tags: [typ/story, status/idee, bereich/frontend, bereich/qualitaet]
aliases: [E2E-Nightly rot, Login bricht ab Test 2, Issue 3]
status: idee
prio: P1
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: CI-Sichtung beim Push am 2026-08-10 (`gh run list`), GitHub-Issue #3
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-139 · Der E2E-Nachtlauf ist seit drei Nächten rot: ab Test 2 scheitert jeder Login

Beim Push am 2026-08-10 gesehen. Die Zustellung aus [B-26](B-26-e2e-in-ci.md) **funktioniert** —
Issue #3 („E2E-Nachtlauf ist rot", Label `e2e-nightly`) ist offen und wurde jede Nacht kommentiert.
Nur hat niemand hineingesehen.

Warum P1 vorgeschlagen: die E2E-Suite ist der einzige automatische Wächter über die *gesamte*
Oberfläche, und sie ist seit drei Nächten blind. Solange sie rot ist, sagt ein grüner Nachtlauf nichts
mehr — und der Rollengang mehrerer Abnahmen dieser Woche stützt sich auf genau diese Suite.

## Was gemessen ist

- **Rot in drei aufeinanderfolgenden Nächten**: Läufe `31238851319` (2026-08-08), `31294230873`
  (2026-08-09), `31356058214` (2026-08-10), jeder nach ~6,5 min.
- **Test 1 grün, ab Test 2 fällt alles** — `anmerkungen.spec.ts:27` besteht, danach 30 Fehlschläge,
  jeder mit gleichmäßig ~11,6 s. Eine gleichmäßige Dauer über verschiedene Specs ist ein Timeout, kein
  Bündel Einzelfehler.
- **Der Fehlschlag sitzt im Login.** Fehler 11 nennt `getByRole('heading', { name: 'Kinder' })` — die
  Abschluss-Zusicherung von `vaterLogin` (`e2e/helpers.ts:15`). Ab Test 2 kommt der Vater also nicht mehr
  hinein.
- **Lokal grün**: dieselbe Suite läuft am 2026-08-10 mehrfach mit **31/31** durch (Playwright startet
  Backend und Vite selbst).

## Eine Hypothese ist schon widerlegt

Naheliegend war der **Ratenbegrenzer**: alle Logins kommen aus derselben IP, die `login`-Policy würde nach
wenigen Versuchen mit `429` antworten, und das erklärte „erster Login geht, alle weiteren nicht" genau.
Widerlegt: `frontend/playwright.config.ts:60` setzt `RateLimiting__LoginEnabled: "false"` für den von
Playwright gestarteten Backend-Prozess, und CI benutzt dieselbe Konfiguration.

Die Ursache ist damit **offen**. Nicht geraten wird hier bewusst.

## Was als Nächstes zu tun ist

1. Das Artefakt `playwright-artefakte` am Lauf `31356058214` holen (14 Tage Aufbewahrung, läuft am
   2026-08-24 ab — **das ist die Uhr an dieser Story**). Trace und Screenshot von Test 2 sagen, was der
   Login-Aufruf tatsächlich geantwortet hat.
2. Erst danach entscheiden, ob es ein Produkt- oder ein CI-Umgebungsfehler ist. Kandidaten, die das
   Artefakt klärt: antwortet `auth/adult` mit `429`, `401` oder gar nicht; ist der Backend-Prozess nach
   Test 1 überhaupt noch am Leben; hat die geteilte Temp-DB ein Schreibproblem.
3. Ein Blick, ob der erste rote Lauf (2026-08-08) mit einer bestimmten Abnahme zusammenfällt — die Woche
   hat den Ratenbegrenzer (B-119, B-120) und die Proxy-Header (B-125) angefasst. Fällt es zusammen, trägt
   diese Story ein `entgangen_bei`.

## Verlauf

- **2026-08-10** — angelegt aus der CI-Sichtung nach dem Push. Symptome sind gemessen, die Ursache
  ausdrücklich **nicht** — eine Hypothese ist am Code widerlegt und als widerlegt notiert, statt als
  Vermutung stehenzubleiben. `unverifiziert: true`, weil der Ist-Stand am *Code* noch nicht belegt ist:
  bisher steht nur, was CI zeigt.
