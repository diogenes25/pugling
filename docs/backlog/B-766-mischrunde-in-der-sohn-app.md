---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/gamification, rolle/student, bereich/frontend]
aliases: [Mischwiederholung Sohn-App]
status: geschaetzt
prio: P2
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "B-218"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-766 · Mischrunde in der Sohn-App: Einstieg im Bonus-Training, Runde spielen, Ergebnis

Die Oberfläche zu [B-218](B-218-mischwiederholung-ueber-alle-positionen.md): ein Einstieg im Bonus-Training, die Runde mit Karten aus mehreren Positionen, ein Ergebnis mit Münzen und Deckelstand.
**Baut nach B-764 und B-765.**

## User Story

Als *Schüler* möchte ich eine Mischrunde starten und spielen, die mir sagt, was fällig war und was sie gebracht hat.

## Ist-Stand am Code

- Bonus-Seite und Lauf: `frontend/src/sohn/SohnBonus.tsx` (Liste `:112`, Lauf `SohnBonusRun` `:160`), Routen `bonus` und `bonus/:exerciseId` (`frontend/src/sohn/SohnApp.tsx:183-184`).
- Eine Mischrunde in der Oberfläche: **nicht gefunden**.
- **Fremde, ungecommittete Arbeit am 2026-11-20:** `SohnApp.tsx` und andere Dateien unter `frontend/src/sohn/` (LearningDad); die neue Route hängt an `SohnApp.tsx`. Erst bauen, wenn sie committet ist.

## Die echte Lücke

Einstieg, Spielbildschirm und Ergebnis; alle Wege liefern B-764 und B-765.

## Entscheidungen

*Aus B-218 abgeleitet, stellvertretend, unbestätigt (Vorbereitungslauf 2026-11-20, Sonnet).*

1. **Der Einstieg steht neben den Bonus-Übungen und ist gesperrt, solange das Bonus-Training gesperrt ist** (B-218 Entscheidung 1): dieselbe Sperrzeile wie dort. *Kosten:* ein Kind vor den Tageszielen sieht nur den Grund.
2. **Das Ergebnis nennt den Deckelstand über beide Quellen** (B-765 Risiko 1). *Kosten:* ein Feld mehr in der Antwort.
3. **Texte nennen die Mischrunde freiwillig und ohne Malus** (B-218, Entscheidung des Nutzers vom 2026-10-23).

## Akzeptanzkriterien

Neu aus den Entscheidungen; B-218 hatte keine Oberflächen-Kriterien.

1. Der Einstieg erscheint nur, wenn Karten fällig sind und die Sperre offen ist; bei gesperrtem Bonus-Training steht der Sperrgrund. *Rot, wenn der Einstieg bei Sperre klickbar ist (Vitest an `SohnBonus`).*
2. Das Starten ist beim Senden gesperrt (zweimal tippen startet einen Lauf). *Rot, wenn `disabled={busy}` fehlt.*
3. Das Ergebnis zeigt Münzen und Deckelstand; bei Deckel 0 ohne Münzen steht der Grund. *Rot, wenn der Deckelstand fehlt.*
4. Ein vom Server abgewiesener Start (Sperre, nichts fällig) zeigt die Meldung am Platz. *Rot, wenn der Fehler verschluckt wird.*

## Schätzung

**Größe: M** – eine neue Route, ein Spielbildschirm nach dem Muster von `SohnBonusRun`, ein Ergebnis.

- **`wo: frontend`, `migration: nein`, `vertragsbruch: nein`.**
- **Risiken:** (1) Karten aus mehreren Positionen mit verschiedenen Stufen im selben Bildschirm – der Spielbildschirm muss je Karte die Stufe kennen; (2) Ergebnisbild teilt Bausteine mit dem Bonus-Ergebnis;
  (3) fremde Arbeit an `SohnApp.tsx`.

### Angriffsplan

1. Client-Methoden und Typen aus dem erzeugten Vertrag; i18n Deutsch und Englisch.
2. Einstieg in `SohnBonus`, Route und Bildschirm `SohnMischrunde.tsx` (Bausteine von `SohnBonusRun` wiederverwenden).
3. Vitest, E2E, `frontend-reviewer`.

### Testweg

- `SohnBonus.test.tsx`: AK 1, 2, 4; neuer `SohnMischrunde.test.tsx` für AK 3.
- E2E: `frontend/e2e/bonus.spec.ts` um den Weg „fällige Karten in zwei Positionen, Mischrunde, Münzen“ ergänzen oder `mischrunde.spec.ts` anlegen (Vorbild `bonus.spec.ts`, Kind mit eigenem Fach je Plan); das ist der Rollengang als Sohn.
- Vor der Abnahme: `frontend-reviewer`.

## Verlauf

- 2026-11-20 · aus B-218 geteilt und geschaetzt (Vorbereitungslauf, Sonnet): M, frontend, keine Migration, kein Vertragsbruch. Baut nach B-764 und B-765; fremde Arbeit an `SohnApp.tsx` abwarten.
