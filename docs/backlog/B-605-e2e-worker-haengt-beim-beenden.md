---
tags: [typ/story, status/idee, bereich/test, rolle/entwickler]
aliases: [E2E-Worker hängt]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Sprint 82, Rollengang und volle E2E-Suite (pm-sitzung-2026-11-16-backlog-lauf.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-605 · Playwright-Worker hängt beim Beenden, einmal mitten im Lauf

Im Sprint 82 meldete Playwright dreimal „worker-N process did not exit within 300000ms after stop, force-killed it“:
nach dem roten Fall des ersten Rollengang-Laufs (Neustart des Workers) und am Ende der vollen Suite (166 passed,
trotzdem 5 Minuten Nachlauf). Ein voller Lauf blieb zudem bei `bilder.spec.ts:85` (32/166) über zehn Minuten ohne eine
Zeile stehen, weit über dem Test-Timeout von 60 s; nach dem Abbruch lief derselbe Stand in 21 Minuten grün durch.
Verdacht, ungeprüft: ein offener Handle im Worker (Media-Upload, Speech-/Audio-Stub oder der Vite-Proxy), der das
Beenden blockiert. Idee: `--debug`-freie Messung mit `PWDEBUG=0`, `why-is-node-running` im Worker, Wiederholung des
Hängers an `bilder.spec.ts` allein. Absichtlich nicht im Sprint: das Sprint-Ziel ist ohne ihn belegt (166 passed).

## Verlauf

- **2026-11-16** — angelegt beim Rollengang von Sprint 82.
