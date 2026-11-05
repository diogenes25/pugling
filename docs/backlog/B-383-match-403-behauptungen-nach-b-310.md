---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/doku]
aliases: [Client-Doku verspricht 403]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: doku
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-u
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-310, B-282]
---

# B-383 · Nach B-310 versprechen Client-Doku und Story weiter 403, und ein Seed-Kommentar ist zu stark

Die Nachschau zu `b32460e` (frischer `pugling-reviewer`, read-only) fand:

1. **`Pugling.Client/CreatorApi.cs:137-138`** – `MatchProfilesAsync` nannte für ein nicht betreutes Kind weiter
   „`403 forbidden`". Das ist die Schicht, aus der die Agenten ihre Aufrufe bauen.
2. **„403 fällt aus dem Dokument"** (B-310 Entscheidung 2 und Verlauf, Protokoll, Commit-Text) stimmt nicht: der B-100-Transformer
   (`Program.cs:486`) trägt 403 für jede rollen-gegatete Aktion ein – richtig für ein Konto ohne Creator-Rolle. Gefallen ist nur das
   403 für ein **fremdes Kind**.
3. **`Seed.cs:155`** (🟢): „the seeded e-mail is not [chosen by users]" – `Adult.Email` ist per `PATCH auth/me` frei setzbar.

## User Story

Als **Entwickler**, der den Client oder eine Story liest, möchte ich die Antwort lesen, die der Server wirklich gibt.

## Ist-Stand am Code (Stand `9fd94d8`)

- Wie oben; `git show b32460e -- docs/openapi/v1.json` zeigt das 403 bei `profiles/match` nur umgestellt, nicht entfernt.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. Nur Text: Client-Doku auf 404 (403 = fehlende Rolle), Seed-Kommentar auf die tatsächliche Reichweite, B-310 mit einer
   Richtigstellung im Verlauf. Keine Code-Änderung am Verhalten.

## Akzeptanzkriterien

1. `grep -rn "403" backend/Pugling.Client/CreatorApi.cs` nennt am Match-Aufruf nur noch die fehlende Rolle.
2. B-310 trägt die Richtigstellung.

## Schätzung

`XS`, nur Doku/Kommentar. Testweg: `grep`, Build.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf T, Sprint 2 (in Lauf U, Sprint 3) und im selben Sprint behoben, ohne
  Zählung (nur Text). AK 1: `CreatorApi.cs:137-139` lautet jetzt „404 not_found … 403 only means the account lacks the Creator
  role". AK 2: Verlaufszeile in B-310. Dazu 🟢 aus derselben Nachschau: die Mutantenzählung „2 failed von 43" galt für vier
  Klassen; mit `CreatorAgentTests.Profile_fuer_*` fielen beim selben Mutanten mehr – in B-310 vermerkt. **Abgenommen**
  (Rollengang: der Entwickler ist die Rolle; Doku, kein Verhalten). Kein Limit 2: die Entgleitung liegt im Increment des
  vorigen Laufs (T), nicht dieses (U).
