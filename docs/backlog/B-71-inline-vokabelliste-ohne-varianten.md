---
tags: [typ/story, status/idee, bereich/frontend, bereich/katalog, lerntechnik/vokabeln, rolle/creator]
aliases: [Inline-Vokabeln ohne Varianten, Übungs-Editor Alternativen]
status: idee
prio: P3
art: Wunsch
quelle: docs/backlog/B-65-vokabel-mehrere-uebersetzungen.md (Review-Nebenbefund)
unverifiziert: true
---

# B-71 · Die Inline-Vokabelliste im Übungs-Editor kann keine gleichwertigen Übersetzungen anlegen

Vokabeln lassen sich auf zwei Wegen anlegen: im Vokabel-Store (`/vater/vokabeln`) und inline beim
Erstellen einer Vokabelübung (`VocabItem` mit front/back/hint). Seit
[B-65](B-65-vokabel-mehrere-uebersetzungen.md) trägt der erste Weg ein Feld für gleichwertige
Übersetzungen, der zweite nicht — wer so autort, muss für jede Variante ein zweites Mal in den Store
gehen. Da der Inline-Weg der bequemere ist, entstehen die Einträge, die den Defekt auslösen, weiter
genau dort.

Offen ist, ob das Feld an die Inline-Zeile gehört (mehr Formular in einer ohnehin dichten Maske) oder ob
der Weg stattdessen auf den Store verweisen sollte.

## Verlauf

- **2026-08-02** — aufgenommen als Nebenbefund des `frontend-reviewer` beim Bau von B-65; nicht
  selbst nachgeprüft.
