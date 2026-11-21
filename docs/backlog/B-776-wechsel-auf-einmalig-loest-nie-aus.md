---
tags: [typ/story, status/idee, bereich/shop, rolle/supervisor, rolle/student]
aliases: [Wechsel auf einmalig löst nie aus]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: "Nachschau Sprint 97 / B-771 im Nachtlauf 2026-11-21 (pugling-reviewer), docs/pm-sitzung-2026-11-21.md"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-771]
---

# B-776 · Ein Angebot, das von wiederkehrend auf „einmalig“ wechselt, füllt vorhandene Bestände nie auf

`DueRefills(Once, …)` zahlt nur aus, wenn der Bestand **keinen** Auffüll-Zeitpunkt hat
(`backend/Pugling.Api/Services/Shared/ShopRefillSchedule.cs:24`). Der PATCH lässt bei einem Wechsel auf „einmalig“ die
Bestände bewusst unangetastet (B-771 Entscheidung 2, Kommentar `ShopController.cs:291-292`: „Once keeps its empty
instant“). Das gilt aber nur für einen Bestand, der unter „keine“ entstand. Ein Bestand aus einer täglichen, wöchentlichen
oder monatlichen Zeit hat immer einen Zeitpunkt, und der Termin feuert dann nie.

**Szenario (am Code nachgerechnet, nicht im Browser gesehen):** Angebot „täglich“, `MaxStock 2`, das Kind kauft am 20.11.
alles. Der Vater stellt am 20.11. auf „einmalig zum 25.11.“. Am 25.11. ergibt `DueRefills` 0, der Bestand bleibt 0, und
`NextRefillAtUtc` zeigt dem Vater „keine weitere Auffüllung“. Ein Geschwisterkind, das erst danach in die Zielgruppe kommt,
startet über `StartStock` ohne Zeitpunkt und wird am 25. aufgefüllt. Zwei Kinder desselben Angebots laufen auseinander.
Dasselbe passiert bei einem **neuen Termin** an einem schon ausgelösten „einmalig“.

Entgangen bei B-771: Dort deckt AK 4 nur „keine → einmalig“ ab. Das Verhalten selbst stammt aus B-645. Möglicher Fix
(zu entscheiden): beim Wechsel auf „einmalig“ mit Termin in der Zukunft und bei einem neuen Termin erst die alte Regel
buchen, dann den Zeitpunkt aller Bestände leeren und ihren Stamp bumpen. Rote Probe: täglich → einmalig mit Termin in
fünf Tagen nach einem Ausverkauf, erwartet 0 bis zum Termin und danach `MaxStock`.

## Verlauf

- **2026-11-21** — angelegt aus der Nachschau von B-771 im Nachtlauf (`idee`, `entgangen_bei: [B-771]`).
