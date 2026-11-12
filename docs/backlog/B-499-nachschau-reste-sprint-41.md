---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor]
aliases: [Lernstand Leerfall zählt gewertete Antworten, Übung löschen kündigt Wirkung an]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Nachschau Sprint 41 in der Retro von Sprint 42)
unverifiziert: false
entgangen_bei: [B-467, B-418]
grund: ""
ersetzt_durch: []
---

# B-499 · Nachschau Sprint 41: der Leerfall des Lernstands misst noch immer nicht, was er sagt – und „Übung löschen" kündigt an, was nie passiert

Nachschau von Sprint 41 (frischer `frontend-reviewer` an `1c7b567`; sechs Vitest-Dateien des Commits 55/55; gelesen und
gegen den Server nachgerechnet, nicht im Browser).

1. **🟡 „Das Kind hat noch keine Frage beantwortet" stimmt nicht** (`frontend/src/vater/VaterLernstand.tsx:106`,
   Kommentar `:63-66`). Die Liste liest `ItemProgress`, und eine Zeile entsteht nur bei `countsForMastery`
   (`ItemProgressService.cs:51`); gewertet wird nur `(typed || !RequireTypedTest) && due && !alreadyScoredToday &&
   !displayOnly && !selfClaimRung` (`PositionPracticeController.cs:402-403`). Ein Kind, das neue Wörter auf der
   Selbsteinschätzungs-Stufe (B-476) beantwortet, hat Antworten, aber keine Zeile. Gemessen ist „gewertete Antwort".
   Dazu: der neutrale Zweig (lädt/scheitert) hat keinen Test – die Mutante „immer Erfolg" überlebt, weil `findByText` auf
   den Endzustand wartet; „Nochmal" in `ListenAusfall` lädt `anyWord` nicht neu; `anyWord` liest `stale` nicht (kurz der
   Erfolg des vorigen Kindes bei Zurück/Vor).
2. **🟡 Dieselbe Form wie B-418:** `frontend/src/vater/VaterExercises.tsx:343` fragt „Zuordnungen in Lehrplänen können
   betroffen sein" – der Server lehnt jede benutzte Übung ab (`ExerciseControllerBase.cs:504-507`, `exercise_in_use`).
3. **🟢 Skin-Kauf mit zu wenig Gems** zeigt dem Kind die Rohzeile (`SohnSkins.tsx:76` sperrt nicht wie `SohnShop.tsx:227`;
   `400 insufficient_gems` steht in keiner Tabelle – das 409-Tor aus B-411 sieht 400er nicht). Nur per Rennen:
   `insufficient_coins`, `shop_listing_inactive`, `insufficient_inventory`.
4. **🟢** Arcade-Fehler aus `useAsync` gehen über `errorMessage` statt `useErrorMessage` (`SohnHome.tsx:30/64`,
   `SohnProgress.tsx:16/30`, `SohnKonto.tsx:30`) – älter, praktisch folgenlos.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 41, Retro Sprint 42 im Nachtlauf).
