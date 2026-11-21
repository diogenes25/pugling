---
tags: [typ/story, status/idee, bereich/shop, rolle/supervisor]
aliases: [Einmal-Termin mit Uhrzeit füllt erneut]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: "Nachschau Sprint 100 (a0b73724) im Nachtlauf 2026-11-21, docs/pm-sitzung-2026-11-21.md"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-776]
---

# B-782 · Ein „einmalig“-Termin mit Uhrzeit füllt beim Bearbeiten im Vater-Web noch einmal auf

Seit B-776 leert ein **geänderter** Termin an einem „einmalig“-Angebot den Auffüll-Zeitpunkt aller Bestände
(`ShopController.UpdateListing`, `resetsOnceInstant`, Vergleich `nextRefillAt != listing.RefillAtUtc`). Das Vater-Web
schickt beim Speichern immer `<Datum>T00:00:00Z` (`frontend/src/vater/VaterShop.tsx:228`) und liest den Termin auf das
Datum zugeschnitten ein (`VaterShop.tsx:423`).

Legt ein API-Aufrufer (Agent, Skript) ein „einmalig“-Angebot mit Uhrzeit an, zum Beispiel `2026-11-12T10:00:00Z`, und
der Vater ändert später im Vater-Web nur den Preis, dann geht `2026-11-12T00:00:00Z` zurück. Das ist ein anderer Wert,
also leert der PATCH die Zeitpunkte, und jedes Kind wird noch einmal aufgefüllt, weil Mitternacht schon vorbei ist.
Derselbe Effekt entsteht bei einem Termin, der mit einem anderen Offset als `Z` geschickt wurde (Review-Hinweis 4 von
Sprint 100).

Am Code nachgelesen, nicht im Browser gesehen. Heute schreibt nur das Vater-Web `RefillAtUtc`: gegrept in `Seed.cs`,
`Pugling.Agent.*`, `scripts/` und `frontend/src`. Der Pfad braucht darum einen fremden Aufrufer.

Mögliche Reparatur (zu entscheiden): den Termin serverseitig auf UTC-Mitternacht normalisieren (er ist fachlich ein
Tag), oder im Vater-Web den geladenen Wert unverändert zurückschicken, solange das Datum nicht geändert wurde. Rote
Probe: POST „einmalig“ mit `T10:00:00Z`, Termin vorbei, ausverkauft, dann PATCH nur `coinPrice` mit
`refillAtUtc = <Datum>T00:00:00Z`. Erwartet ist Bestand 0, gemessen würde vermutlich `MaxStock`.

## Verlauf

- **2026-11-21** — angelegt aus der Nachschau von Sprint 100 (`idee`, `entgangen_bei: [B-776]`).
