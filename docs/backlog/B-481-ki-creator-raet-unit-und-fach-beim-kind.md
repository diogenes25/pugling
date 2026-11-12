---
tags: [typ/story, status/idee, bereich/ki, rolle/creator]
aliases: [--child mit --series rät die Unit, zwei Fächer des Kindes ohne Rückfrage]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 33)
unverifiziert: true
entgangen_bei: [B-473]
grund: ""
ersetzt_durch: []
---

# B-481 · Der KI-Creator rät beim Kind weiter die Unit und das Fach

Nachschau von Sprint 33 (frischer `pugling-reviewer` an `e94aa1e`, gelesen, nicht nachgestellt). B-473 hat das
geratene Regal nur für den Fall „`--child` ganz ohne Ortsangabe" geschlossen; daneben liegen zwei Wege, auf denen der
Agent still falsch ablegt – derselbe Fehler, den B-473 abstellen sollte:

1. **`--child` mit `--series`, ohne `--series-unit`** (`AgentCommands.cs:430`): der Kind-Zweig läuft nur ohne beide
   Angaben, also landet `create --child 1 --series <Access>` in der *ersten* Unit der Reihe statt in der aktuellen
   des Kindes. Hilfetext (`:670`) und README lesen sich, als sei der Fall gelöst.
2. **Zwei Bücher in verschiedenen Fächern** (`AgentCommands.cs:489`): nur ein *exakter* Punktgleichstand führt zur
   Rückfrage. Hat das Englisch-Profil Klassengrenzen und das Latein-Profil keine, gewinnt Englisch mit einem Punkt
   Vorsprung – das Fach wird still gewählt. Theodor hat genau diese zwei Bücher.
3. **Test unscharf**: `Ohne_Profil_zum_Buch_des_Kindes_…` prüft nur `"--profile"` in der Meldung; die
   Gleichstand-Meldung enthält das auch. Unter der Mutante „jedes Profil mit Reihe zählt" bleibt er in der geteilten
   Test-DB grün, sobald genug gleich gute `FreshProfileAsync`-Profile herumliegen – die rote Messung in B-473 hing
   an Reihenfolge und DB-Bestand. Abhilfe: auf „kennt kein Creator-Profil" prüfen.

Naheliegende Abhilfe: (1) mit `childId` und ohne `--series-unit` das Buch des Kindes in der gewünschten Reihe
nehmen; (2) jede zweite *verschiedene* Reihe unter den `series_match`-Treffern ist eine Fehlbedienung mit Liste,
nicht nur ein Gleichstand an der Spitze.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 33 im Backlog-Lauf).
