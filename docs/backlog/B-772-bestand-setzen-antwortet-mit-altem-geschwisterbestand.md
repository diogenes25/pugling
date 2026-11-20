---
tags: [typ/story, status/idee, bereich/shop, rolle/supervisor]
aliases: [SetStock-Antwort veraltet]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: "Nachschau Sprint 96 (08fd05a6) in Sprint 97, 2026-11-20"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-645]
---

# B-772 · Die Antwort auf „Bestand setzen" zeigt die Geschwister mit veraltetem Bestand

`ShopService` materialisiert beim Setzen des Bestands nur die Zeile des einen Kindes (`ShopService.cs:146`), gibt aber
über `ToView` die Bestände **aller** berechtigten Kinder zurück (`:151`): Geschwister erscheinen ohne ihre fällige
Auffüllung, und ein Geschwister ohne Zeile fehlt ganz. Das Vater-Web lädt danach ohnehin neu (`VaterShop.tsx:356`) und
zeigt richtig; falsch liest es nur ein Client oder Agent, der der Antwort traut. Gefunden in der Nachschau von Sprint 96.

## Verlauf

- **2026-11-20** — angelegt aus der Nachschau von Sprint 96 (`idee`, `entgangen_bei: [B-645]`).
