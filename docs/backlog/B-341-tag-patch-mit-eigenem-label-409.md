---
tags: [typ/story, status/idee, bereich/backend, rolle/creator, rolle/supervisor]
aliases: [Schlagwort speichern mit unverändertem Label scheitert am Synonym-409]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf K, Teil 2 in Lauf L (docs/pm-sitzung-2026-10-30-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-341 · Ein Schlagwort lässt sich nicht mehr speichern, sobald ein anderes sein Label als Synonym führt

`3190f9e` (Review-Funde Lauf K, vom Nutzer außerhalb der Schleife) hat im `PATCH` von Interessen-Schlagwörtern eine
Synonym-Prüfung ergänzt (`backend/Pugling.Api/Controllers/Creator/InterestTagsController.cs`, `Update`). Sie läuft
auch, wenn das Label **unverändert** mitgeschickt wird. Synonyme selbst werden beim Anlegen oder Ändern nicht gegen
fremde Labels geprüft.

Repro aus dem Code: Tag B „Soccer 1" anlegen, Tag A per `PATCH` das Synonym „soccer 1" geben → `200`. Ab dann scheitert
jedes Speichern von B im Formular (`frontend/src/vater/InterestTagAdmin.tsx:148` schickt immer `label`) mit `409`, auch
eine reine Farbänderung. Das bricht [B-141](B-141-interest-tag-label-dublette.md) AK 2 („eigenes Label unverändert
mitschicken → 200").

Offen und **nicht** Teil dieses Defekts: ob die Regel symmetrisch sein soll (Synonyme gegen Labels prüfen) und ob der
Slug-Vergleich (ein Synonym „soccer" sperrt das Label „SOCCER") zu B-141 AK 3 passt. Das ist eine Produktfrage zur
Entscheidung hinter B-141.

## Verlauf

- **2026-10-30** — angelegt aus der Nachschau zu Lauf K, Teil 2 (`pugling-reviewer`, 🟡, Fund in `3190f9e`). Aus Code
  gelesen. `entgangen_bei` leer: die Prüfung stammt aus keinem abgenommenen Increment einer Story.
