---
tags: [typ/story, status/abgenommen, bereich/lehrplan, rolle/supervisor]
aliases: [Nachschau-Reste Sprint 32, Dosis wirkt nicht und wird angezeigt]
status: abgenommen
prio: P3
art: Defekt
groesse: "S"
wo: "beides"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 32)
unverifiziert: true
entgangen_bei: []
grund: ""
ersetzt_durch: []
nachgeschaut: 2026-11-13
---

# B-480 · Nachschau-Reste Sprint 32: eine Dosis, die nicht wirkt, steht weiter da

Drei 🟢 der Nachschau von Sprint 32 (frischer `pugling-reviewer`, gelesen, nicht nachgestellt) – keine Durchrutscher im
Sinn der Abnahme, die Kosten standen in den Entscheidungen; gesammelt, damit sie nicht verloren gehen:

1. **Plan-Seite zeigt eine Dosis, die nichts tut** (`PlanPositions.tsx:742`, Editor um `:228`): nach dem Umstellen einer
   Liste auf „beliebige Reihenfolge" steht weiter „· 2 neue je Tag", obwohl das Spiel sie ignoriert (B-478,
   Entscheidung 2). Abhilfe: eine *wirksame* Dosis neben `NewItemsPerDay` in der Antwort, oder ein Hinweis daneben.
2. **Überblick des Assistenten, wenn alle Übungen Listen sind** (`VaterWizard.tsx:654`): „höchstens 10 neue … (außer
   Listen)", obwohl jede Position 0 bekommt. Abhilfe: dann „keine – …".
3. **Nur per API**: `PATCH {useLeitner:false}` ohne `newItemsPerDay` auf einer umgestellten Liste scheitert an „needs the
   Leitner box", obwohl die gespeicherte Dosis nicht wirkt (`PlanPositionsController.cs:256`). Die Oberfläche trifft das
   nicht – sie leert die Dosis selbst.

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf):

1. **API-First:** `PositionResponse` trägt `DailyDoseInForce` (additiv) – die Dosis, die das Spiel anwendet. Die Regel bleibt
   an ihrer einen Stelle (`PositionPlayService.DailyDose`, jetzt auch statisch mit der Typ-Registry aufrufbar).
2. **Plan-Seite:** die Zeile nennt die wirksame Dosis; ruht sie, sagt sie „Tagesdosis ruht (Liste in beliebiger
   Reihenfolge)" statt „2 neue je Tag".
3. **Assistent:** sind alle gewählten Übungen Listen, sagt der Überblick „keine – Listen bekommen keine Tagesdosis".
4. **Punkt 3 bewusst offen:** `PATCH {useLeitner:false}` ohne Dosis auf einer umgestellten Liste scheitert weiter an der
   gespeicherten Dosis. Nur per API erreichbar (die Oberfläche leert die Dosis selbst); eine stille Leerung bräche die
   PATCH-Semantik („null = nicht angegeben").

## Akzeptanzkriterien

1. `GET …/positions/{id}` einer umgestellten Liste: `newItemsPerDay` 2, `dailyDoseInForce` 0; geordnet: 2.
2. Die Positionszeile nennt die wirksame Dosis bzw. „ruht".
3. Nur Listen gewählt: der Überblick verspricht keine Dosis.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 32 im Backlog-Lauf).
- **2026-11-13** — ausformuliert, autonom gegrillt (Defekt), geschätzt S (Tageslauf, Sprint 60).
- **2026-11-13** — gebaut (Sprint 60). AK 1 → `DosisResteTests.Dosis_Gilt_Nicht_Mehr…` (D1 → rot); AK 2 → `dosisText`
  (W1 → rot); AK 3 → `dosisZeile` (W2 → rot). Review Frontend: 🟡 Klammertext nannte einen Typ statt der Server-Regel →
  „die Übung wertet als Menge" (Zähler 1).
  - Verifikation: `dotnet test Pugling.sln -c Release` 1462/1462, Vitest 854/854, `tsc -b` sauber, Python-unittest grün,
    Playwright 140/140 auf 5201/5174 (vor den letzten Beschriftungs- und U+FEFF-Nachzügen, die Komponententests tragen);
    Mutanten nach der letzten Änderung rot (Tabellen im Protokoll, Sprints 60–62).
  - Rollengang: Vater (Plan-Seite, Assistent, Ziel-Wähler) per E2E und Komponententests; Creator (Konsole) gegen den
    In-Process-Server; Sohn Regressionszeuge. Abgenommen.
- **2026-11-13** — nachgeschaut (Sprint 63, frische `pugling-reviewer`/`frontend-reviewer` an `e37f65e`, gelesen, Vitest 102/102). Geprüft: CI-Schritt läuft (python3, Arbeitsverzeichnis, Pfade), xUnit-Leser findet die Liste, alle drei Leser stimmen Schritt für Schritt überein, `Map`-Aufrufer laden die Übung. Kein 🔴; Reste → [B-521](B-521-nachschau-reste-sprint-60-62.md).
- **2026-11-13** — Tageslauf (Sprint 68), **Punkt 3 stellvertretend entschieden**: die PATCH-Semantik bleibt (keine stille
  Leerung der Dosis), die Meldung nennt den Schritt, der die Sperre aufhebt: „…needs the Leitner box (useLeitner: true);
  without the box, send newItemsPerDay: 0 in the same request." Belegt in
  `DosisResteTests.Dosis_Ohne_Karteikasten_Nennt_Den_Ausweg_Und_Er_Geht` (Anlegen und PATCH: Meldung nennt den Ausweg, der
  Ausweg geht durch). Mutanten: Z1 (Meldung ohne Ausweg), Z2 (PATCH liest Dosis 0 statt gespeicherter), Z3 (PATCH ignoriert
  die mitgeschickte Dosis) → rot. `pugling-reviewer` (frisch): 🔴 erste Fassung hängte den Fall an den bestehenden Test
  und nahm dessen Dosis weg – seine Zusicherungen prüften danach den Ausgangszustand → eigener Test, alter unverändert;
  🟡 Anlege-Pfad ungetestet → ergänzt; 🟢 „useLeitner: true" in der Meldung → übernommen. Suite grün.
