---
tags: [typ/story, status/abgenommen, bereich/training, bereich/gamification, rolle/student, bereich/frontend]
aliases: [Mischwiederholung Sohn-App]
status: abgenommen
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
- 2026-11-24 · Nachprüfung Ist-Stand (Nachtlauf H): die fremde Arbeit an `SohnApp.tsx` vom 2026-11-20 ist committet (Worktree an `a782b4b2` sauber) – baubar.
- 2026-11-24 · in-arbeit → gebaut (Nachtlauf H, Sprint 131, Freigabe 8). `frontend/src/sohn/SohnMischrunde.tsx`: Einstieg `MischrundeEinstieg` im offenen Zweig von `SohnBonus` (nur bei fälligen Karten oder offener Runde, liest die Sperre selbst; Start über `useAction.runFor` mit `disabled={busy}`, Fehler am Platz über `StatusBanner`, die gestartete Runde reist als Router-State mit), Route `bonus/misch` vor `bonus/:exerciseId`, Spielseite `SohnMischrunde` nach dem Muster `SohnBonusRun` (Ref-Gate beim Einhängen, Antworten je Runde im `sessionStorage`, Fokus wie B-671/B-778), `Ergebnis` mit Münzen, Tagesdeckel über beide Quellen (Entscheidung 2) und Grund ohne Münzen; Texte „freiwillig, ohne Malus" (Entscheidung 3). Client `mixedReview`/`startMixedReview`/`submitMixedReview`, Typen als `S[…]`-Aliase, i18n `mix` de/en.
- 2026-11-24 · AK-Fälle → Test → Mutante (`frontend/src/sohn/SohnMischrunde.test.tsx`, Probe `.claude/scripts/mutant-probe.py`, gemessen an `d00b56d4 + Arbeitsbaum`, 03:05, ohne Mutante 13/13 grün): AK 1 → „steht neben den Bonus-Übungen, wenn Karten fällig sind", „fehlt, wenn nichts fällig ist" (F3 → rot), „zeigt bei gesperrtem Bonus-Training den Grund" (sichert den Sperrgrund der Seite), „bietet allein gerendert bei Sperre keinen Start an" (F2 → rot) · AK 2 → „schickt bei einem Doppeltipp einen Start und sperrt den Knopf" (F1 `disabled` weg → rot) · AK 3 → „nennt die Münzen und den Tagesdeckel über beide Quellen" (F5 → rot), „nennt bei Deckel 0 ohne Münzen den Grund" (F6 → rot), „nennt bei heute schon gesehenen Wörtern den Grund", „nennt bei nur selbst eingeschätzten Treffern deren eigenen Grund" (F7 → rot) · AK 4 → „nennt mixed_review_empty am Einstieg" (F4 → rot) · Spielseite → „spielt die vom Einstieg mitgegebene Runde ohne zweiten Start" (F8 State nicht verbraucht → rot, F9 State ignoriert → rot), „startet ohne mitgegebene Runde selbst", „räumt die Antworten … mixed_review_stale" (F10 → rot). 10/10 rot am benannten Test, Grundlinie grün. Testweg wich ab: AK 1, 2, 4 liegen in der neuen Datei statt in `SohnBonus.test.tsx` (dort nur ein Mock ergänzt, damit die Übersicht nicht ins Netz greift).
- 2026-11-24 · Wächter `src/liveRegions.test.ts` rot (erwartet 0 Funde, gemessen 1: `StatusBanner` nach einem `return null` – die Live-Region entstünde mit der Meldung). Behoben: der Einstieg rendert die Region immer und nur die Karte bedingt. **Fehlerzähler Sprint 131: 1.**
- 2026-11-24 · Review `frontend-reviewer`: keine 🔴. 🟡 1 behoben – die mitgegebene Runde blieb im Verlaufs-State, ein „Zurück" nach dem Ergebnis hätte eine abgegebene Runde noch einmal durchspielen lassen; jetzt nach dem Lesen per `replace` verbraucht (F8). 🟡 2 behoben – ohne Münzen nannte das Ergebnis „heute schon gesehen" auch für nur selbst eingeschätzte Treffer; `Ergebnis` bekommt die geprüft gefragten Karten und hat einen eigenen Grund (F7). 🟡 3 behoben – Vitest für die Spielseite (mitgegebene Runde, Neustart, `mixed_review_stale`). **Fehlerzähler Sprint 131: 4** (Wächter + drei 🟡). 🟢 ohne Zählung: Teaser bei offener Runde ohne fällige Karten eigener Text, „übersprungen" mit Titel als Platzhalter, unsichtbarer `StatusBanner` in der Lektion entfernt, Kommentar am Sperr-Fall; nicht übernommen: auf `view.typed` statt `aufgabenform` verzweigen (gleiches Muster wie das Bonus-Training, heute deckungsgleich).
- 2026-11-24 · Verifikation nach der letzten Änderung: `tsc -b` grün; Vitest gesamt 1396/1396 (127 Dateien); E2E auf Ausweichports 5221/5194: `mischrunde.spec.ts` (neu, Rollengang als Sohn) + `bonus.spec.ts`, `bonus-deckel.spec.ts`, `bonus-deckel-voll.spec.ts`, `bonus-satz-je-karte.spec.ts` – 5 passed. Ein früherer Lauf zeigte einmal einen roten Fokus-Fall in `SohnBonus.test.tsx` (B-778, „nach Aufdecken steht der Fokus auf dem ersten Urteil") im Paket mit anderen Dateien; allein 34/34, im Paket dreimal 62/62 – nicht reproduziert, Code dort unberührt, ohne Zählung.
- 2026-11-24 · **abgenommen** (Nachtlauf H, Sprint 131). Commit `41c720d0`. Rollengang als E2E `frontend/e2e/mischrunde.spec.ts` (Sohn: Bonus-Training öffnen, Einstieg „4 fällige Karten aus all deinen Plänen“, Runde mit Karten aus zwei Fächern, Ergebnis 4/4 mit Tagesdeckel und Grund, danach kein Einstieg mehr) – grün gegen den echten Server auf einer Wegwerf-DB. `frontend-reviewer` gelaufen (3 🟡 behoben). Bleibt dem Menschen: das Gefühl der Runde am Gerät (Mischung der Fächer, Länge 10).
