---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Arcade-Sperrcodes ohne Text]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-08.md
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-415 · Sechs 409-Codes, die nur das Kind erreicht, haben in der Arcade keinen Text

Gefunden beim Ausformulieren von [B-411](B-411-tor-409-codes-haben-deutschen-text.md) (2026-11-08). Die Arcade übersetzt Fehler über
`problem.*` in i18n (`frontend/src/lib/chromeText.tsx`, `PROBLEM_KEYS`: heute vier Codes), nicht über `GERMAN_PROBLEM_TEXT`. Diese
409-Codes erreicht nur das Kind, und keiner steht in `PROBLEM_KEYS`: `skin_already_unlocked`, `shop_insufficient_stock`,
`media_no_alternative`, `media_not_on_card`, `interest_wish_limit_reached`, `concurrency_conflict`. Auf Deutsch fällt `useErrorMessage`
auf `errorMessage` zurück (englische Server-Zeile mit Trace-Id), auf Englisch zeigt es die Server-Zeile direkt.

Beim Ausformulieren zuerst je Code prüfen, ob die Arcade ihn überhaupt erreichen kann (manche sperrt die Oberfläche vorher, etwa ein
schon freigeschalteter Skin) – ein unerreichbarer Code braucht keinen Text, aber einen Grund auf der Ausnahmeliste von B-411s Tor.

## Verlauf

- **2026-11-08** — angelegt beim Ausformulieren von B-411 (Lauf AD, Sprint 21); bewusst nicht im Sprint (anderer Übersetzungsweg, andere Rolle).
