---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor]
aliases: [Lernstand Pager nach Kinderwechsel, Inventar-Satz ohne Bezug]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Nachschau Sprint 44 in der Retro von Sprint 45)
unverifiziert: false
entgangen_bei: [B-499]
grund: ""
ersetzt_durch: []
---

# B-504 · Nachschau-Reste Sprint 44: Seitenzähler nach Kinderwechsel, stumme Zählabfrage, Inventar-Satz ohne Bezug

Nachschau von Sprint 44 (frischer `frontend-reviewer` an `a719ed7`, gelesen, nicht gefahren). Kein 🔴/🟡; drei 🟢:

1. `frontend/src/vater/VaterLernstand.tsx:58`: `skip` überlebt einen Kinderwechsel (kein `key={childId}`). Kind 1 auf
   Seite 2, Wechsel per URL zu Kind 2 mit 3 schwachen Wörtern → leere Seite, „es sitzt alles 👍". Praktisch nur per
   Hand-URL erreichbar. Fix: `skip` beim Wechsel auf 0 (Muster `prevOnlyWeak`) oder `key={childId}`.
2. `:92`: scheitert nur die Zählabfrage, steht dauerhaft der neutrale Satz ohne Fehler und ohne „Erneut laden"
   (`fehler={words.error ?? anyWord.error}`).
3. `insufficient_inventory` im Vater-Banner (`VaterShop.tsx` `ChildShopView`) steht über der Tabelle, „Davon …" hat
   kein Bezugswort, und dass die Anfrage offen bleibt, sagt niemand (Wortgleichheit: die Arcade-Fassung müsste mit).

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 44, Retro Sprint 45).
