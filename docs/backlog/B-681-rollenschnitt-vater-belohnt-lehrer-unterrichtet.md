---
tags: [typ/story, status/idee, bereich/auth, bereich/shop, rolle/supervisor, rolle/creator]
aliases: [Rollenschnitt Vater/Lehrer, Vater nur Belohnung]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/leitbild-privatlehrer.md
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-681 · Rollenschnitt: der Vater belohnt, der Lehrer(-Agent) unterrichtet

Heute ist der Vater Creator **und** Supervisor: Pläne, Positionen, Ziele und Klassenarbeiten liegen unter
`supervisor/…` – zusammen mit dem Familien-Shop. Das Leitbild vom 2026-11-19 trennt das: Der **Vater** kümmert sich
nur noch um die Belohnung (Shop füllen, gekaufte und aktivierte Artikel einlösen), der **Lehrer** – im Regelfall der
KI-Agent, B-680 – analysiert den Lernstand und steuert Übungen und Pläne. Der Supervisor zerfällt damit in zwei
Zuständigkeiten, was Routen und Rollen-Gates trifft.

Zu klären beim Ausformulieren: eine neue Rolle (`Teacher`) oder das bestehende Lehrer-Konto (Creator-only) um die
Plan-Steuerung erweitern? Darf der Vater weiterhin eingreifen (Notbremse, Plan pausieren)? Wer setzt künftig den
Klausurtermin und trägt die Note ein (B-684)? Was passiert mit den laufenden Plänen der Söhne in der echten
`pugling.db` (Migration nur anhängend)?

## Verlauf

- **2026-11-19** — angelegt aus dem Leitbild-Gespräch mit dem Vater ([Leitbild](../leitbild-privatlehrer.md)).
