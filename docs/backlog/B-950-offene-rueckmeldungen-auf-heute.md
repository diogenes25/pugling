---
tags: [typ/story, status/geschaetzt, bereich/rueckmeldung, bereich/frontend, rolle/supervisor, rolle/student]
aliases: []
status: geschaetzt
prio: P1
art: Wunsch
groesse: S
wo: "frontend"
migration: "nein"
vertragsbruch: "nein"
quelle: "Rollengang Runde 138 an einer Kopie der echten DB (docs/pm-sitzung-2026-11-24.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-950 · Die Vater-Startseite „Heute“ zeigt offene Kartenmeldungen der Kinder

Beobachtet an einer Kopie der echten DB (2026-11-24): Theodor hat Karten gemeldet – „zu streng“ zweimal (21.11.), „zu viel verlangt“
(19./20.11., zweimal gezählt) und „falsche Übersetzung“ (19.11.). Alle vier stehen auf `Open`. Die Meldungen leben unter
„Rückmeldungen“ in der Navigation; die Startseite, die der Vater täglich öffnet, erwähnt sie nicht.

## User Story

Als Vater möchte ich auf „Heute“ sehen, dass ein Kind eine Karte gemeldet hat und noch keine Antwort bekam, damit sich das Kind
gehört fühlt, statt dass die Meldung liegen bleibt.

## Ist-Stand am Code (2026-11-24, `f4324888`)

- `GET supervisor/feedback?status=Open` liefert die offenen Meldungen der eigenen Kinder (`api.listFeedback`,
  `frontend/src/lib/api.ts:503-508`).
- `frontend/src/vater/VaterDashboard.tsx` hat die Abschnitte „Heute“ und „Einlösen“ und liest keine Rückmeldungen.

## Die echte Lücke

Was der Vater nicht auf der Startseite sieht, existiert für ihn kaum; die Meldung des Kindes hat dort keinen Platz.

## Entscheidungen

1. **Nutzer, 2026-11-24:** in den Sprint.
2. Ein Abschnitt „Rückmeldungen“ zwischen „Heute“ und „Einlösen“, nur wenn etwas offen ist: je Kind „Name: N offene
   Rückmeldungen“ mit Link nach `/vater/rueckmeldungen`. Ohne offene Meldung steht nichts (keine leere Überschrift). Ein Ladefehler
   zeigt den Abschnitt mit `ListenAusfall`, damit er nicht still fehlt.
3. Gezählt wird je Meldung (Zeile), nicht nach `Count` – eine wiederholte Meldung ist eine offene Frage.

## Akzeptanzkriterien

1. Zwei offene Meldungen von Kind A und eine von Kind B ergeben zwei Zeilen „A: 2 …“ und „B: 1 …“ mit Link.
2. Ohne offene Meldung erscheint kein Abschnitt.
3. Die Abfrage fragt nur `Open` ab (erledigte zählen nicht).
4. Ein Ladefehler zeigt den Abschnitt mit Fehler und Wiederholen.

## Schätzung

- **Größe:** S, frontend, keine Migration, kein Vertragsbruch.
- **Testweg:** Vitest an `VaterDashboard` (AK 1–4), E2E-Zusicherung in einer bestehenden Rückmeldungs-Spec.

## Angriffsplan

`useAsync(() => api.listFeedback(undefined, "Open"))`, reine Gruppierfunktion exportiert und getestet.

## Verlauf

- **2026-11-24** — angelegt im Rollengang Runde 138, vom Nutzer in den Sprint genommen, `geschaetzt` für Sprint 138.
