---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [Übungen verwalten startet leer, Meine Übungen zuerst]
status: abgenommen
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
nachgeschaut: 2026-11-10
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
- **2026-11-09** — gebaut (Lauf AG, Sprint 28); Freigabe-Zeile gelesen (`05700b6`). **Entscheidung 1 an ihrer eigenen Weiche:** der
  Endpunkt kennt `mineOnly` – also serverseitig, gepagt, mit echter Gesamtzahl; „Meine Übungen (Seite)" entfällt. **Statt** des in AK 3
  genannten `VaterExercises.test.tsx` (ein nachgebauter Bildschirm mit gefälschtem fetch, gegen `frontend/CLAUDE.md`): E2E für den Weg,
  Vitest nur für die reine Funktion `fachDerZeile`.
  **Belege je Fall** – `.claude/scripts/mutant-probe.py`, gemessen nach dem letzten Edit an `a14961d + Arbeitsbaum`, 2026-11-09 17:17:

  | Fall | Test | Mutante | gemessen |
  |---|---|---|---|
  | AK 1 meine Übungen aller Fächer | `e2e/creator-katalog-ueberblick.spec.ts` Fall „Übungen verwalten …" | ME2 `mineOnly: false` (`VaterExercises.tsx:83`) | red, am Fall |
  | AK 1 Überschrift | dieselbe | ME3 immer „Übungen" (`:158`) | red, am Fall |
  | AK 1 Spalte „Fach" | dieselbe | ME4 ohne Fach je Zeile (`:351`) | red, am Fall |
  | Review: Fächer laden noch | `VaterExercises.test.ts` „behauptet nichts, solange die Fächer fehlen" | MF3 (`:292`) | red, am Fall |
  | Review: unbekannte Fach-Id | „nennt eine unbekannte Fach-Id als Nummer" | MF4 (`:293`) | red, am Fall |
  | Review: Liste scheitert | Fall „Ausfälle sagen, was fehlt" | ME6 `ListenAusfall` weg (`:183`) | red, am Fall |

  AK 2 (Fach wählen → wie heute, abwählen → wieder „meine") und die Werkstatt-Zahl hält derselbe E2E-Fall. Grundlinie grün.
- **2026-11-09** — Review `frontend-reviewer`: 🟡 „ohne Fach", solange die Fächer laden oder scheitern → `fachDerZeile` (**zählt**); 🟡 ein
  gescheitertes Laden der Liste sah aus wie ein leerer Bestand → `ListenAusfall`, Leertext nur ohne Fehler (**zählt**); 🟢 Hand-URL
  `?seriesId=` ohne Fach → hingenommen (die Überschrift sagt dann „Meine Übungen"); 🟢 ungenutzter Export zurückgenommen.
- **2026-11-09** — **nicht abgenommen: Lauf AG an Freigabe 3 beendet.** Volle Suite nach dem letzten Edit: Backend 1187/1187, Vitest 723/723, `tsc -b` sauber, Playwright **1 failed, 129 passed** – rot `e2e/typ-manifest-faellt-aus.spec.ts:40`: der Schalter „geteilte Übungen anderer Väter anzeigen" ändert beim Klick seinen Zustand nicht. Ursache (analysiert, nicht behoben): seit dem Review-Fix zu B-428 hängt der Schalter an der Adresse (`?shared=1`, `VaterExercises.tsx` `setFilter`); Fach-Wechsel und Klick gehen beide über `setSearchParams`, der zweite liest den Fach-Wert aus dem alten Render (veraltete Closure), und React Router rendert die Adress-Änderung verzögert – der kontrollierte Haken springt zurück. Reparaturweg: `setParams(prev => …)` (aktuelle Adresse statt Render-Werte). Das ist Fund 6 in Sprint 28 und überschreitet den Zähler (5); darum gestoppt, nicht repariert. Der Rest der Story ist gebaut und belegt (Tabelle oben); die Abnahme gehört dem Nutzer bzw. dem nächsten Lauf nach der Reparatur.
- **2026-11-10** — abgenommen nach der Reparatur von [B-450](B-450-schalter-geteilte-uebungen-springt-zurueck.md) (im Dialog). Backend 1187/1187, Vitest 723/723, `tsc -b` sauber, Playwright **131 passed** (ganze Suite, Server vom Lauf nach dem letzten Edit gestartet). **Rollengang** Creator = `e2e/creator-katalog-ueberblick.spec.ts` (sechs Fälle, grün); Vater/Sohn als Regressionszeugen. Commit: siehe `git log -- docs/backlog/B-431-uebungen-verwalten-startet-leer.md`.
- **2026-11-10** — Nachschau (Retro Sprint 29, Fertigstellungs-Lauf, frischer `frontend-reviewer` an den Commits `48173c0`/`7e98879`): kein Defekt; Testlücke und zwei Anzeige-Reste → [B-456](B-456-nachschau-reste-sprint-28.md).
