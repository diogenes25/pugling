---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [errorMessage deutsch in englischer Arcade, GERMAN_PROBLEM_TEXT]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Review zu B-304 in Lauf G, Teil 1 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-314 · Fehlermeldungen in der englischen Arcade bleiben deutsch

`errorMessage` (`frontend/src/lib/api.ts`, Tabelle `GERMAN_PROBLEM_TEXT`) übersetzt die englischen `detail`-Texte des
Servers ins Deutsche – auch auf den Arcade-Bildschirmen, die seit [B-304](B-304-arcade-rahmen-auf-englisch.md)
englisch sprechen (Shop, Skins, „Das bin ich“). Ein englisches Kind liest bei einem gescheiterten Kauf also eine
deutsche Meldung. Der Helfer ist geteilt (auch das Vater-Web ruft ihn) und lag darum außerhalb von B-304.

**Zu entscheiden (`Wunsch`):** Bekommt die Arcade die Server-Meldungen englisch (der Server liefert sie ohnehin
englisch) oder eigene Schlüssel je `code`? Und wie bleibt das Vater-Web dabei deutsch (B-304, AK 3)?

## Verlauf

- **2026-10-27** — angelegt aus dem Review zu B-304 (🟢, `frontend-reviewer`), Lauf G Teil 1
  ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)). Bewusst nicht im Sprint: B-304 hat die geteilten Helfer
  ausdrücklich unberührt gelassen.
