---
tags: [typ/story, status/idee, bereich/test, rolle/entwickler]
aliases: [SohnProgress-Vitest flackert]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: "Nachtlauf B 2026-11-22, voller Vitest-Lauf bei B-603 (docs/pm-sitzung-2026-11-22-b.md)"
grund: ""
ersetzt_durch: []
---

# B-787 · `SohnProgress.test.tsx` flackert im vollen Vitest-Lauf

Die Story B-603 hat das Flackern in `SohnProgress.test.tsx` bewusst zurückgestellt (Entscheidung 4). Begründung: Es gab
keinen Fallnamen. Jetzt gibt es einen: Im vollen Lauf am 2026-11-22 (Worktree von Lauf B, B-603 schon umgestellt) fiel

`SohnProgress mit einer gemerkten Plan-Id, die nicht mehr gilt > nimmt den spielbaren Plan und merkt ihn sich`
(`frontend/src/sohn/SohnProgress.test.tsx:66`)

Der Lauf endete 1 failed / 1303 passed. Allein lief die Datei dreimal 9/9 grün, zwei weitere volle Läufe 1304/1304.

**Fehlermeldung nicht mitgeschnitten:** Der Lauf ging durch `grep`. Wer das nächste Rot sieht, schreibt den Lauf in eine
Datei und trägt die Zeile hier ein.

Vermutung, nur gelesen: Der Fall prüft direkt nach `findByText(/Plan 5/)`, ohne `waitFor`. Gemeint sind
`overview`-Aufrufe und `setPlanId` (`:72-73`). `setPlanId` kommt aus einem `useEffect` in `useChosenPlan`
(`if (chosen && chosen.id !== planId) setPlanId(chosen.id)`). Das ist dasselbe Muster wie bei B-603: ein Zeitpunkt wird
geprüft statt eines Zustands. Ob es wirklich die Ursache ist, entscheidet erst die Fehlermeldung.
