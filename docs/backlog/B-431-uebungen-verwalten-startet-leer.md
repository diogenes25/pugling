---
tags: [typ/story, status/geschaetzt, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [Übungen verwalten startet leer, Meine Übungen zuerst]
status: geschaetzt
prio: P3
art: Wunsch
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08-rollengang.md#feedback-creator
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-431 · „Übungen verwalten" startet leer: erst ein Fach wählen, obwohl die Werkstatt „3 eigene Übungen" zählt

Im Rollengang vom 2026-11-08 kam der Lehrer von der Werkstatt („3 eigene Übungen · alle Übungen verwalten →")
auf `/vater/exercises` und sah: „Wähle ein Fach, um seine Übungen zu sehen." Die Zahl, die ihn hergeführt hat,
ist dort nicht zu sehen; der Bestand des Lehrers verteilt sich auf Fächer, die er erst durchprobieren muss.

## User Story

Als *Creator* möchte ich auf „Übungen verwalten" **zuerst meine eigenen Übungen** sehen, über alle Fächer, damit
die Seite dort beginnt, wo mein Material liegt – und erst danach nach Fach filtere.

## Ist-Stand am Code

- `VaterExercises.tsx:148` – `subjectId === "" ? <div className="banner">Wähle ein Fach …</div> : (…)`: ohne Fach
  wird nicht geladen.
- `GET creator/exercises` funktioniert ohne `subjectId` (Rollengang: `?take=100` lieferte 12 Zeilen mit
  `X-Total-Count`) und liefert `isOwn` je Zeile; Paging ist vorhanden (`httpPaged`, Hausregel).
- Die Werkstatt (`VaterInhalte.tsx`) zählt „eigene Übungen" aus derselben Liste.

## Die echte Lücke

Die Seite verlangt einen Filter, den die API nicht braucht.

## Entscheidungen

Getroffen vom PM in der Fable-Sitzung vom 2026-11-08 unter dem Auftrag des Nutzers, das Backlog
nachtlauffähig zu machen ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)). Vetorecht beim Nutzer.

1. **Ohne Fach lädt die Seite „meine Übungen" über alle Fächer** (Filter `isOwn`, clientseitig aus der
   gepagten Liste oder serverseitig, falls der Endpunkt ein `own`-Flag kennt – prüfen; wenn nicht, bleibt es
   clientseitig je Seite und die Überschrift sagt „Meine Übungen (Seite)"). Kosten: eine Liste ohne Fach-Spalte
   braucht die Spalte „Fach" in der Tabelle.
2. **Der Schalter „geteilte Übungen anderer Väter anzeigen" gilt weiter**: ohne Fach und mit Schalter → alle.
   Kosten: keine.
3. **Mit Fach bleibt alles wie heute.** Kosten: keine.

## Akzeptanzkriterien

1. `/vater/exercises` ohne Query zeigt die eigenen Übungen aller Fächer mit Spalte „Fach"; die Zahl entspricht
   der Werkstatt-Kachel.
2. Fach wählen → wie heute; Fach abwählen → wieder „meine".
3. Vitest `VaterExercises.test.tsx`: Fälle 1–2; Mutante „Banner statt Liste" → rot.

## Schätzung

**Größe: S** — Ladebedingung, Spalte, Überschrift, Tests. **Testweg:** `VaterExercises.test.tsx` (neu, falls
nicht vorhanden), Rollengang Creator als Fall in `creator-lehrwerk-weg.spec.ts`.

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang Creator ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)),
  Ist-Stand belegt, Entscheidungen vom PM unter dem Auftrag „nachtlauffähig machen" (Vetorecht beim Nutzer),
  geschätzt.
- **2026-11-08** — **Freigabe des Nutzers (Autonomer Modus, Opt-in):** die sieben vorgelegten Entscheidungen der Fable-Sitzung hat der Nutzer im Dialog bestätigt („Ja, so fahren“, Antwort auf die Frage nach den Entscheidungen für die erste Nacht, [Protokoll](../pm-sitzung-2026-11-08-rollengang.md)). Ein Nachtlauf darf diese Story auf Grundlage der `## Entscheidungen` bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
