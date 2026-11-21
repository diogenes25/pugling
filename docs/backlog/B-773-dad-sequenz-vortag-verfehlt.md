---
tags: [typ/story, status/idee, bereich/gamification, bereich/frontend, rolle/student]
aliases: [Learning Dad Wutsequenz, Vortag verfehlt Animation]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nutzer, Sitzung 2026-11-21
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-773 · Learning Dad reagiert auf verfehlte Tagesziele vom Vortag (wütend → Monster → Kampfansage)

Die Clips sind fertig und im Frontend als Aktionen von `LearningDad` angelegt: `become-angry` (5 s), `become-monster`
(10 s), `starts-fight` (10 s). Nichts löst sie aus: im Sohn-Frontend und in den Vertragstypen gibt es kein Signal
„Tagesziel von gestern verfehlt". Heute lassen sie sich nur über `celebrate(…, "become-angry")` bzw. den `DadShowcase`
abspielen.

## User Story

Als *Student* möchte ich beim Öffnen der App sehen, dass Learning Dad auf meine verfehlten Tagesziele vom Vortag reagiert,
damit mir der Verlust (Malus) spürbar wird und ich heute die Tagesmission angehe.

## Hinweise für das Ausformulieren (ungeprüft, nicht der Ist-Stand)

- Backend zuerst (API-First): der Server müsste je Kind sagen, ob am Vortag Pflichtziele offen blieben (Anzahl, Malus
  in Münzen). Zu prüfen, ob die Malus-Buchung im Ledger schon genügt oder ein Feld an der Tagessicht fehlt.
- Tagesbegriff in deutscher Zeit: [B-730](B-730-tagesbegriff-in-deutscher-zeit.md).
- Verwandt: [B-748](B-748-vater-morgenuebersicht-tagesmission-lernstand-klausurchance.md) (Vater sieht „Vortag erfüllt?")
  und [B-680](B-680-taeglicher-lehrplan-je-fach-just-in-time.md) (Leitbild Privatlehrer); dieselbe Frage, andere Rolle.
- Offen: genau einmal pro Tag? Eskalationsstufen (wütend → Monster → Kampf je nach Zahl der Fehlschläge oder je nach
  Folgetag)? Überspringbar? Ton für ein Kind: Friedrich (15) und Theodor (12) reagieren verschieden auf Druck, siehe
  Memory zu Friedrichs Motivation.
- Klären, ob es eine Gegenstück-Reaktion gibt (`applause` bzw. `boxing-complete` bei erfüllten Zielen); die Clips
  existieren bereits.
- Zugänglichkeit: bei „Bewegung reduzieren" greift das Standbild; die Aussage muss zusätzlich als Text im DOM stehen.
