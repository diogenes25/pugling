---
tags: [typ/story, status/idee, bereich/shop, rolle/supervisor, rolle/student]
aliases: [Sofort-Auffüllung ohne Zeitpunkt]
status: idee
prio: P2
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

# B-771 · Ein Bestand ohne Auffüll-Zeitpunkt füllt bei wiederkehrender Auffüllung sofort auf

Ein Kind-Bestand ohne `LastRefilledAtUtc` schuldet bei jeder wiederkehrenden Art (täglich, zweimal täglich, wöchentlich,
monatlich) **sofort** eine Auffüllung: `ShopRefillSchedule.DueRefills` gibt für `lastUtc == null` pauschal 1 zurück
(`backend/Pugling.Api/Services/Shared/ShopRefillSchedule.cs:24`). Ohne Zeitpunkt entsteht ein Bestand auf zwei Wegen:

- als Startbestand eines Angebots mit „keine Auffüllung" (`StartStock` gibt dafür `null`, `:96`), das der Vater später
  per PATCH auf eine wiederkehrende Art umstellt – der PATCH setzt die Kind-Bestände nicht neu;
- aus der Migration `ShopStockPerChild`, die ein altes `LastRefilledAtUtc = NULL` eines wöchentlichen Angebots übernimmt.

**Folge (am Code nachgelesen, nicht im Browser gesehen):** Der Vater legt „Taschengeld" am 5. ohne Auffüllung an,
Friedrich kauft alles, der Vater stellt am 19. auf „monatlich" – beim nächsten Lesen steht der volle Bestand wieder da,
und Friedrich kauft im selben Monat ein zweites Mal. B-645 verspricht „genau einmal je Monat".

Gefunden in der Nachschau von Sprint 96 (Code-Lesung durch `pugling-reviewer`); die Tests verfehlen es, weil
`ShopRefillScheduleTests.cs:60` `Daily, null → 1` als Absicht festhält, kein Test die Art an einem Angebot mit
Beständen wechselt und der Migrationstest kein auffüllendes Angebot mit `MaxStock > 0` und leerem Zeitpunkt hat.

Mögliche Reparaturen (zu entscheiden): fällige Auffüllungen ab `lastUtc ?? listing.CreatedAt` zählen, oder beim Wechsel
der Art alle Kind-Bestände des Angebots auf „jetzt" stempeln. Rote Probe: PATCH „keine" → „monatlich" nach einem
Ausverkauf erwartet Bestand 0 bis zum Monatsersten.

## Verlauf

- **2026-11-20** — angelegt aus der Nachschau von Sprint 96 (`idee`, `entgangen_bei: [B-645]`).
