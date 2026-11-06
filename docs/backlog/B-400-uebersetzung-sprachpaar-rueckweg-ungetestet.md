---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [Sprachpaar-Rückweg ungetestet]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-z
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-05"
entgangen_bei: [B-396]
---

# B-400 · Kein Test liest eine Übersetzung mit einem anderen Sprachpaar als en→de zurück

B-396 merkt sich je geladener Zeile das Sprachpaar (`configToEditorState`, `frontend/src/vater/exerciseConfig.tsx:395`). Die Gegenprobe
„nichts geändert → Bindung bleibt" baut ihre Zeile von Hand mit `origSourceLang: "en"`, der Altübungs-Fall lädt ohne Sprachen, das E2E
wechselt **weg** von en→de. Eine Mutante, die `origSourceLang: "en", origTargetLang: "de"` fest verdrahtet, überlebt darum alle Tests –
und eine `fr→de`-Übung verlöre beim bloßen Öffnen und Speichern ihre Bindung.

Zu tun: ein Fall „geladen mit z. B. fr→es, unverändert gespeichert → Bindung bleibt", gegen die Mutante gemessen.

## Entscheidungen

1. **(Defekt, im Lauf gegrillt)** Nur ein Testfall, kein Code: das Verhalten stimmt, es war unbelegt.

## Akzeptanzkriterien

| Fall | Erwartet | Beleg | Mutante |
|---|---|---|---|
| geladen fr→es, unverändert gespeichert | Bindung 9 bleibt | `exerciseConfig.test.ts` „ein geladenes Sprachpaar fr→es bleibt beim Speichern gebunden" | `origSourceLang: "en", origTargetLang: "de"` fest – gemessen: rot nur dieser Fall („expected null to be 9") |

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf Y, Sprint 13 (Lauf Z, Retro Sprint 14; frischer `frontend-reviewer`,
  `git show 609b754`, 🟡). Increment von Lauf Y: **kein Limit 2** für Lauf Z.
- **2026-11-05** — gebaut in Lauf Z, Sprint 15. Test grün gegen den heutigen Code (kein Defekt im Verhalten, sondern im Beleg); die
  Mutante aus der Nachschau gemessen: 1 rot von 15, genau der neue Fall; zurück → 15/15.
- **2026-11-05** — **abgenommen.** Kein Laufzeit-Code, darum kein Klickweg; Vitest 15/15 in der Datei, volle Playwright-Suite 117 passed als Regression. Volle Suite 1115/1115. Commit: siehe nächster Eintrag.
- **2026-11-05** — Commit `176eaf5`.
- **2026-11-05** — **Nachschau** (Lauf AA, Retro Sprint 16; frischer `pugling-reviewer`, `git show 176eaf5`): geprüft Mutanten-Zeilen am Code (fr→es-Mutante, `clearing`-Mutanten, vertauschte Zweige, Gegenprobe), wann `clearing` greift (`subjectId: null` ohne `clearSubject` = kein Wechsel; fachlose Reihe ohne Prüfung), weitere Erzeuger fachloser Reihen (nur B-404). Nichts entgangen.
