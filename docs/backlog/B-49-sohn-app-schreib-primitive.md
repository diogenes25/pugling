---
tags: [typ/story, status/idee, bereich/frontend, bereich/qualitaet]
aliases: [Sohn-App ohne useAction]
status: idee
prio: P3
art: Aufräumen
quelle: B-43
unverifiziert: true
---

# B-49 · Die Sohn-App benutzt die geteilten Schreib-Primitive nicht

`frontend/CLAUDE.md` führt „`useAction` + `StatusBanner` für jede Mutation" als Norm – unter `src/sohn/` gibt
es aber **keinen einzigen** `useAction`-Treffer (nachgesehen beim Grillen von
[B-43](B-43-frontend-komponententests.md)). `SohnShop.tsx` trägt stattdessen eigenes `busy`/`msg` samt
`flash()`-Timeout und `if (busy) return` (`SohnShop.tsx:52`) – also genau die try/catch/finally-Kaskade, deren
Vervielfachung `useAction` beseitigen sollte. Folge: Die `useRef`-Sperre aus B-43 wirkt in der Sohn-App
**nicht**, und die Rückmeldung läuft dort an `StatusBanner` vorbei – samt dessen Live-Region, also ohne die
Screenreader-Ansage.

**Ungeprüft:** ob das Absicht ist – die Sohn-Oberfläche ist bewusst eine andere Welt (Dark-Arcade, `Celebration`
statt Banner, `flash()` als Einblendung), ein schlichtes `StatusBanner` könnte dort fachlich falsch sein. Zu
klären ist also, was übernommen wird (die Sperre und der Fehlerweg) und was bewusst eigen bleibt (die
Darstellung). Ebenso ungeprüft: wie viele der 11 Sohn-Dateien überhaupt schreiben.

## Verlauf

- **2026-07-31** — geerntet beim Grillen der vier Test-Stories (Nebenbefund aus der B-43-Recherche).
