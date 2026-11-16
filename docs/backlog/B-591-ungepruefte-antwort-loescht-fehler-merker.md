---
tags: [typ/story, status/idee, bereich/training, rolle/student]
aliases: [LastMissed ungeprüft, Selbstauskunft im gewerteten Pfad]
status: idee
prio: P3
art: Frage
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-219 (Fix-Prüfung im Backlog-Lauf 2026-11-16)
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-591 · Löscht eine ungeprüfte „gewusst"-Antwort den Fehler-Merker der milden Abstufung?

[B-219](B-219-verzeihende-abstufung-je-position.md) stuft bei gesetzter Option nach dem ersten Fehler nur eine Box
zurück, nach zwei Fehlern in Folge hart. Der Merker `PositionItemProgress.LastMissed` folgt laut Entscheidung 11 nur
geprüften Antworten; der Selbstauskunfts-Pfad (`climbs`) lässt ihn seit dem Fix unverändert.

Offen bleibt der gewertete Pfad (`PositionPracticeController`, Aufruf von `ApplyReview` ~538): Ist die Tagesstufe
selbst nicht getippt und `RequireTypedTest` aus, wertet er ein ungeprüftes `WasKnown` – und löscht damit den Merker.
Folge ab jeder Box: 4 → Fehler 3 → „gewusst" 4 → Fehler 3 statt 1. Frage an den Vater: soll der Merker dort auch
stehen bleiben, oder ist eine ungeprüfte Stufe bewusst gleichwertig?

## Verlauf

- **2026-11-16** — angelegt aus der Invarianten-Prüfung des B-219-Fixes; nicht gebaut, weil es eine Produktfrage ist.
