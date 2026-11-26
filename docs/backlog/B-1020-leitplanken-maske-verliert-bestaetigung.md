---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor]
aliases: []
status: idee
prio: P1
art: Defekt
unverifiziert: true
quelle: docs/pm-sitzung-2026-11-25-o.md#sprint-191--ziel--umfang
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-1020 · Die Leitplanken-Maske verliert nach dem Speichern ihre Bestätigung, und kein Test prüft das Zurücklesen

Fund des `frontend-reviewer` im Sprint 191 (B-731, Nachtlauf O), nicht mehr behoben, weil der Fehlerzähler den Lauf beendete.
`LeitplankenSection` hängt per `key` an den fünf Serverwerten (`frontend/src/vater/VaterKind.tsx`); nach dem Speichern lädt
die Seite das Kind neu, der Schlüssel ändert sich, die Sektion wird neu eingehängt – und die Meldung „Leitplanken
gespeichert.“ bzw. „Leitplanken entfernt.“ verschwindet nach wenigen Millisekunden (die Datei warnt selbst davor, Z. 57–61).
Die E2E `frontend/e2e/lerntage-und-pause.spec.ts` prüft genau diese Meldung und ist damit wackelig. Zweitens: kein Test wird
rot, wenn die Maske nach dem Speichern den getippten statt den gelesenen Wert zeigt (der Mock antwortet mit den getippten
Werten; „entfernen“ ist ungetestet). Drittens: der Knopf heißt wie zwei andere „Speichern“ im selben Reiter, die E2E wählt ihn
per `.last()`. Reparaturweg laut Review: Muster `CoreDataSection` (`runFor`, Felder aus der Antwort setzen, `key` nur am Kind),
Mock mit abweichendem Serverwert, Knopf „Leitplanken speichern“.

## Verlauf

- **2026-11-25** — angelegt im Nachtlauf O (Sprint 191) aus dem Review von B-731; der Lauf endete am Fehlerzähler, bevor der
  Fund behoben werden durfte.
