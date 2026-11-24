---
tags: [typ/story, status/idee, bereich/klassenarbeit, rolle/supervisor]
aliases: []
status: idee
prio: P2
art: Defekt
groesse: ""
wo: "beides"
migration: ""
vertragsbruch: ""
unverifiziert: true
quelle: "Nachschau Sprint 135 in der Retro von Sprint 137 (docs/pm-sitzung-2026-11-24.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-941]
wartet_auf: ""
---

# B-947 · Nach dem Start verschwindet der Hinweis „N Übungen ohne Ausführ-Recht ausgelassen“, ohne dass die Übung je zur Pflicht wird

Am Code bestätigt (2026-11-24, `cfebfe60`): `ClassTestPrepService.NotExecutableCountAsync`
(`backend/Pugling.Api/Services/Shared/ClassTestPrepService.cs:142-153`) zählt gegen den **Live-Stand** der Rechte. Der Start
läuft aber genau einmal (`ReconcileAsync`, `:196-198`, Anspruch über `PrepStartedOn`) und friert das Bündel ein.

So passiert es: Ein Bündel hat eine öffentliche Übung A und eine zurückgezogene Übung B. Der erste Login im Fenster startet die
Pflicht nur mit A. Der Vater sieht „1 Übung ohne Ausführ-Recht ausgelassen“ und holt sich beim Lehrer eine Freigabe für B. Danach
fällt der Zähler auf 0, und der Hinweis verschwindet. B wird aber nie zur Position, und nichts sagt das dem Vater. Der Hinweis rät
ihm also einen Ausweg, der nach dem Start nichts bewirkt. Das ist dieselbe Fehlerklasse wie die Ratschläge in B-508…B-516.

Zu entscheiden (Dialog, darum `idee`):

1. Nachziehen: Eine nach dem Start erteilte Freigabe macht B nachträglich zur Position.
2. Ehrlich zählen: Nach dem Start zählt der Hinweis „Bündel minus Positionen“ und nennt als Grund „nach dem Start
   hinzugekommen“ statt „ohne Ausführ-Recht“.

Ein Test muss die Reihenfolge „Start mit gemischtem Bündel, dann Freigabe“ fahren.

## Verlauf

- **2026-11-24** — angelegt aus der Nachschau von Sprint 135 (Reviewer-Fund, von mir am Code nachgelesen). Bewusst nicht in Sprint
  137 genommen: Dessen Ziel hängt nicht daran, und der Ausweg ist eine Produktentscheidung.
