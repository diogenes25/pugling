---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [verspätete Zielfeier, Erst-Stand nur aus take 5]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-10-23-nacht.md (vierter Lauf, Nachschau Sprint 6)
unverifiziert: true
grund: ""
ersetzt_durch: []
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

## Verlauf

- **2026-10-23** — angelegt in der Nachschau des vierten Laufs (Prüfer-Befund, Zeilen nicht selbst nachgemessen); ungeprüft.
