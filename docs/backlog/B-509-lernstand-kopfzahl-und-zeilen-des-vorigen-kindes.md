---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor]
aliases: [Lernstand stale Kopfzahl, Pager des vorigen Kindes]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Nachschau Sprint 46 in der Retro von Sprint 47)
unverifiziert: false
entgangen_bei: [B-504]
grund: ""
ersetzt_durch: []
---

# B-509 · Lernstand: Kopfzahl, Zeilen und Pager zeigen nach einem Kinderwechsel das vorige Kind

Nachschau von Sprint 46 (frischer `frontend-reviewer` an `3618e63`, gelesen, Vitest 13/13 der beiden Dateien). Ein 🟡,
drei 🟢. B-504 hat den Leerfall an `!words.stale` gebunden; die übrigen Aussagen aus `words.data` nicht – dieselbe Klasse
wie B-499/B-502/B-504, an anderer Stelle derselben Komponente.

1. 🟡 `frontend/src/vater/VaterLernstand.tsx` (`WeakWords`): Überschrift `Wörter (total)`, die Zeilen und
   `<Pager total=…>` lesen `words.data` ohne `stale`. Kind 1 hat 30 Wörter, Wechsel per URL zu Kind 2, dessen Liste
   scheitert: unter „Lernstand · Kind 2" stehen „Wörter (30)", 25 Zeilen von Kind 1 und „1–25 von 30" unter dem
   Fehlerkasten; „Weiter ›" ist bedienbar (`busy={words.loading}` ist falsch) und fragt Kind 2 mit `skip=25` ab – der
   Zustand, den B-504 AK 1 verhindern sollte. Ohne Fehler kurz im Ladefenster. Kein Test fährt „Wechsel, dann Fehler".
2. 🟢 `:98-101`: der Fehlerkasten der Zählung erscheint auch ohne „nur schwache", wo der Leerfall die Zählung nicht
   braucht – zwei Sätze untereinander, die sich zu widersprechen scheinen.
3. 🟢 `:63`: `openWord` überlebt den Kinderwechsel (anders als `skip`); dieselbe `vocabularyId` beim neuen Kind ist
   ungefragt aufgeklappt, im Ladefenster rendert `WordDetail` mit `childId={neu}` und dem alten Wort.
4. 🟢 `frontend/src/vater/VaterShop.test.tsx:31`, `:65`: die Klicks auf „Freigeben" tragen nur, weil `window.confirm`
   in happy-dom `true` liefert; ausdrücklich setzen (`vi.stubGlobal`), sonst fällt der Test bei einem Update mit
   irreführender Meldung.

Bezug: [B-505](B-505-leerfall-an-der-seite-statt-am-bestand.md) (Leerfall an der Seite in acht anderen Listen).

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 46, Retro Sprint 47). Punkt 1 am Code gelesen: Überschrift, Zeilen und Pager
  ohne `stale`-Prüfung.
