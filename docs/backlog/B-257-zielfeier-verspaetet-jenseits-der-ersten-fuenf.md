---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [verspätete Zielfeier, Erst-Stand nur aus take 5]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-23-nacht.md (vierter Lauf, Nachschau Sprint 6)
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-10-28"
entgangen_bei: [B-36]
---

# B-257 · Ein schon erreichtes Ziel jenseits der ersten fünf wird später verspätet gefeiert

Gefunden in der Nachschau zu Sprint 6 (B-36). `MyObjectives.tsx:23` lädt `take: 5`, sortiert nach
Fälligkeit (`ObjectiveEvaluationService.cs:67`), und bildet den Erst-Stand der gesehenen Ziele nur aus dieser
Seite. Ein Ziel, das beim ersten Öffnen schon erreicht war, aber auf Platz 6 oder später stand, wird darum
nie als gesehen gespeichert. Rutscht es später nach vorn (der Vater deaktiviert oder löscht ein früheres
Ziel), feiert die App „🏆 ZIEL GESCHAFFT! +N" für einen alten Erfolg – genau das falsche Versprechen, das
B-36 vermeiden sollte. B-36 hatte nur „ein sechstes Ziel bleibt ungefeiert" als Risiko benannt, nicht die
verspätete Feier.

Vorschlag für die rote Probe: erster Besuch mit `[obj1..obj5 offen]` (obj6 erreicht, nicht geladen), zweiter
Besuch mit `[obj6 erreicht]` → erwartet keine Feier; heute fällt der Fall. Mögliche Reparaturen (zu
entscheiden beim Ausformulieren): Erst-Stand über alle Seiten bilden, oder die Sortierung bzw. der Filter so,
dass erreichte Ziele im Fenster liegen.

Mitzunehmen beim Bau (Testlücke, kein Defekt): die Währungsweiche `o.kind === "Committed" ? "🪙" : "💎"`
(`MyObjectives.tsx:46`) und der Plural „N Etappen geschafft" (`:41`) trägt kein Testfall.

## User Story

Als Kind möchte ich nur für einen Erfolg gefeiert werden, den ich gerade geschafft habe – nicht für ein Ziel, das schon
erreicht war, als ich die Seite zum ersten Mal sah.

## Ist-Stand am Code

`frontend/src/sohn/MyObjectives.tsx` (vor dem Fix): `api.myObjectives({ take: 5 })`; `takeFreshAchieved` seedet beim
ersten Besuch still, aber nur mit den IDs dieser fünf. Der Server sortiert nach Fälligkeit und blättert über alle
aktiven Ziele (`MyObjectivesController.List`, `ToPagedList`, `MaxTake = 500`). **Gemessen** (neuer Fall mit einem Mock,
der wie der Server blättert): erster Besuch mit Ziel 6 erreicht auf Platz 6, zweiter Besuch mit Ziel 6 vorn →
`celebrate` **2-mal** gerufen (Etappe + Ziel).

## Akzeptanzkriterien

1. Ein beim ersten Besuch schon erreichtes Ziel (und seine Etappen) wird nie gefeiert, auch wenn es jenseits der
   ersten fünf stand.
2. Die Liste zeigt weiter höchstens fünf Ziele; ab dem zweiten Besuch lädt der Bildschirm nur diese Seite.
3. Mitgenommen (Testlücke): Währung je Zielart in der Liste und Plural der Etappenfeier tragen je einen Fall.

## Entscheidungen

1. Nur der **erste** Besuch (einer der beiden Gesehen-Schlüssel fehlt) lädt bis an die Server-Obergrenze 500 und
   seedet aus allen; die Anzeige schneidet auf fünf. Verworfen: jeder Besuch lädt alles (Kosten bei jedem Öffnen für
   einen Fall, der nur beim ersten zählt) und „nur Übergänge offen→erreicht feiern“ (änderte, was B-36 feiert – eine
   Produktfrage). Kosten: ein größerer Abruf genau einmal je Kind und Gerät; über 500 aktive Ziele bleibt der Rest
   ungesehen, wie bei `VaterLernstand`/`VaterWizard` an derselben Grenze.
2. Die Gesehen-Schlüssel bekommen ein `v2` (aus dem Review): ein Stand von vor dem Fix kennt nur die ersten fünf und ist
   unbrauchbar, jedes Gerät seedet einmal still neu. Kosten: eine seit dem letzten Besuch neu erreichte Etappe bleibt
   einmal ungefeiert – dieselbe Abwägung wie in `seenIds.ts` („lieber eine ausbleibende als eine falsche Feier“).

## Schätzung

XS, `frontend`, keine Migration, kein Vertragsbruch.

## Verlauf

- **2026-10-23** — angelegt in der Nachschau des vierten Laufs (Prüfer-Befund, Zeilen nicht selbst nachgemessen); ungeprüft.
- **2026-10-28** — in Lauf I, Teil 1 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)) unter Freigabe 1 ausformuliert,
  gegrillt, geschätzt (XS) und gebaut. Rote Probe gegen den Ausgangsstand: neuer Fall **1 failed** (erwartet keine
  Feier, gemessen 2 Aufrufe), übrige 11 grün. Mutanten: Liste ungekürzt → **1 failed**; jeder Besuch lädt 500 →
  **1 failed**; Plural fest „1“ → **1 failed**; Währung fest 🪙 → **1 failed**. `MyObjectives.test.tsx` **14/14**.
- **2026-10-28** — Review (`frontend-reviewer`): 🟡 ein Gerät mit dem Stand von vor dem Fix (beide Schlüssel vorhanden, nur
  aus den ersten fünf) gälte nicht als erster Besuch – AK 1 hielte nur für neue Geräte. Behoben mit Entscheidung 2;
  Fall „Gerät mit altem Stand“ gegen die Mutante „Schlüssel ohne `v2`“ → **2 failed**. 🟢 ohne Zählung: Plural-Fall
  trennt jetzt „neu erreicht“ (2) von „erreicht“ (3), Mutante → **2 failed**. Vitest **563/563**, volle E2E
  **99 passed** (`exit=0`); die verspätete Feier selbst ist nur im Komponententest belegt (braucht ein Gerät mit
  Vorgeschichte), Browser-Extension nicht genutzt. **`abgenommen`.**
- **2026-10-28** — Commit `c1a4e3f` (Lauf I, Teil 1).
- **2026-10-28** — nachgeschaut (Lauf I, Teil 2, frischer `frontend-reviewer` am Commit `c1a4e3f`): Sortierung stabil (DueDate, Id), Nachbarn Missionen/Abzeichen laden unbeblättert, keine Leser der alten Schlüssel. **Entgangen:** [B-327](B-327-ziel-spaeter-jenseits-platz-fuenf-erreicht.md) – spätere Besuche luden wieder nur fünf (Entscheidung 1 nahm an, der Fall zähle nur beim ersten); im selben Sprint behoben. 🟢 ohne Zählung: `||`/`&&` in `firstVisit` ungetestet – mit B-327 entfällt die Bedingung.
