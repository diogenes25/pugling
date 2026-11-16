---
tags: [typ/story, status/idee, bereich/test, rolle/entwickler]
aliases: [Fokus-Vitests flackern]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Sprint 82, Verifikation von B-598 (pm-sitzung-2026-11-16-backlog-lauf.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-603 · Fokus-Vitests flackern im vollen Lauf

Im Sprint 82 fiel im vollen Vitest-Lauf je einmal ein Fokus-Fall rot, der allein grün ist:
`SohnPractice.test.tsx` („setzt den Fokus nach Weiter auf einer Kennenlernkarte …“, einmal, während parallel die
E2E-Suite lief) und `SohnTest.test.tsx` („steht nach „Nochmal versuchen“ im ersten Bedienbaren der neuen Frage“, in einem
von drei vollen Läufen ohne andere Last). Beide prüfen `document.activeElement` nach einem einzigen
`await act(async () => {})`; ein Fokus-Effekt, der erst nach einem weiteren Render greift, ist unter Last dann noch nicht
gelaufen. Der Stimmen-Hook aus B-597 ist in diesen Fällen still (`speechSynthesis` fehlt in happy-dom), also vermutlich
nicht die Ursache. Idee: auf den Fokus warten (`waitFor`), statt einen Takt anzunehmen. Absichtlich nicht im Sprint, weil
das Sprint-Ziel ohne ihn erreicht wird.

## Verlauf

- **2026-11-16** — angelegt bei der Verifikation von B-598 (Sprint 82).
