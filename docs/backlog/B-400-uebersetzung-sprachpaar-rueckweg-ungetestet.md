---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [Sprachpaar-Rückweg ungetestet]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-z
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-396]
---

# B-400 · Kein Test liest eine Übersetzung mit einem anderen Sprachpaar als en→de zurück

B-396 merkt sich je geladener Zeile das Sprachpaar (`configToEditorState`, `frontend/src/vater/exerciseConfig.tsx:395`). Die Gegenprobe
„nichts geändert → Bindung bleibt" baut ihre Zeile von Hand mit `origSourceLang: "en"`, der Altübungs-Fall lädt ohne Sprachen, das E2E
wechselt **weg** von en→de. Eine Mutante, die `origSourceLang: "en", origTargetLang: "de"` fest verdrahtet, überlebt darum alle Tests –
und eine `fr→de`-Übung verlöre beim bloßen Öffnen und Speichern ihre Bindung.

Zu tun: ein Fall „geladen mit z. B. fr→es, unverändert gespeichert → Bindung bleibt", gegen die Mutante gemessen.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf Y, Sprint 13 (Lauf Z, Retro Sprint 14; frischer `frontend-reviewer`,
  `git show 609b754`, 🟡). Increment von Lauf Y: **kein Limit 2** für Lauf Z.
