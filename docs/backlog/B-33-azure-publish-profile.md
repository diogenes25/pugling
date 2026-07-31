---
tags: [typ/story, status/verworfen, bereich/doku]
aliases: [Azure-Publish-Profile-Secret]
status: verworfen
prio: P3
art: Wunsch
quelle: memory/codequalitaet-gates.md
grund: bewusste Nicht-Aufgabe für eine Code-Sitzung (Nutzer-Entscheidung)
---

# B-33 · Azure-Secret `AZURE_WEBAPP_PUBLISH_PROFILE` fehlt

Das Deploy-Secret ist nicht gesetzt, der Azure-Deploy-Workflow ist ohnehin stillgelegt (Trigger raus, Datei
bleibt — Commit `4eadba8`).

**Verworfen, nicht vergessen:** Laut Rückmeldung des Nutzers bewusst offen und keine Aufgabe für eine
Code-Sitzung — das Secret setzt ein Mensch im Azure-Portal. Steht hier, damit es nicht in jeder Sichtung
erneut als „offener Punkt" auftaucht. Der Betriebsschritt vor dem nächsten Deploy hängt an **B-07**, nicht
hier.

## Verlauf

- **2026-07-30** — geerntet und sofort `verworfen` mit Grund.
